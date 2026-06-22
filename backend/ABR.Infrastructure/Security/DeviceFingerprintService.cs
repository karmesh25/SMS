using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ABR.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ABR.Infrastructure.Security;

public sealed class DeviceFingerprintService : IDeviceFingerprintService
{
    private const int RequiredMatchCount = 3;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DeviceFingerprintService> _logger;

    public DeviceFingerprintService(IConfiguration configuration, ILogger<DeviceFingerprintService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public bool IsDeviceLockEnforced() =>
        _configuration.GetValue("Security:EnforceDeviceLock", false);

    public string GetCurrentFingerprint()
    {
        var parts = GetCurrentHardwareParts();
        return ComputeCombinedHash(parts.ToArray());
    }

    public DeviceHardwareParts GetCurrentHardwareParts()
    {
        return new DeviceHardwareParts
        {
            MotherboardSerial = HashPart(ReadWmiValue("Win32_BaseBoard", "SerialNumber")),
            CpuId = HashPart(ReadWmiValue("Win32_Processor", "ProcessorId")),
            DiskSerial = HashPart(ReadWmiValue("Win32_DiskDrive", "SerialNumber", "Index='0'")),
            MacAddress = HashPart(ReadMacAddress())
        };
    }

    public DeviceVerifyResult VerifyLicense()
    {
        if (!IsDeviceLockEnforced())
            return DeviceVerifyResult.LockDisabled;

        var licensePath = ResolveLicensePath();
        if (!File.Exists(licensePath))
            return DeviceVerifyResult.FileNotFound;

        try
        {
            var encrypted = File.ReadAllBytes(licensePath);
            var json = Decrypt(encrypted);
            var payload = JsonSerializer.Deserialize<LicensePayload>(json)
                ?? throw new InvalidOperationException("Invalid license payload.");

            if (payload.AllowedDevices.Count == 0)
                return DeviceVerifyResult.FileTampered;

            var current = GetCurrentHardwareParts();
            var currentParts = current.ToArray();

            foreach (var device in payload.AllowedDevices)
            {
                if (FuzzyMatch(currentParts, device.Parts))
                    return DeviceVerifyResult.Valid;
            }

            return DeviceVerifyResult.InvalidDevice;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Device license verification failed.");
            return DeviceVerifyResult.FileTampered;
        }
    }

    public async Task StoreLicenseAsync(string[] allowedFingerprints, CancellationToken cancellationToken = default)
    {
        var payload = new LicensePayload
        {
            AllowedDevices = allowedFingerprints.Select(f => new LicensedDevice
            {
                FingerprintHash = f,
                Parts = Array.Empty<string>()
            }).ToList()
        };

        var json = JsonSerializer.Serialize(payload);
        var encrypted = Encrypt(json);
        var licensePath = ResolveLicensePath();
        var directory = Path.GetDirectoryName(licensePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        await File.WriteAllBytesAsync(licensePath, encrypted, cancellationToken);
    }

    private string ResolveLicensePath()
    {
        var configured = _configuration["Security:LicenseFilePath"] ?? "config/device.lic";
        return Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(AppContext.BaseDirectory, configured);
    }

    private static bool FuzzyMatch(string[] currentParts, string[] storedParts)
    {
        if (storedParts.Length == 0)
            return false;

        var matches = 0;
        var count = Math.Min(currentParts.Length, storedParts.Length);
        for (var i = 0; i < count; i++)
        {
            if (!string.IsNullOrWhiteSpace(currentParts[i]) &&
                !string.IsNullOrWhiteSpace(storedParts[i]) &&
                string.Equals(currentParts[i], storedParts[i], StringComparison.OrdinalIgnoreCase))
            {
                matches++;
            }
        }

        return matches >= RequiredMatchCount;
    }

    private static string ComputeCombinedHash(string[] parts)
    {
        var combined = string.Join("|", parts);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(combined))).ToLowerInvariant();
    }

    private static string HashPart(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim()))).ToLowerInvariant();
    }

    private static string ReadWmiValue(string wmiClass, string property, string? condition = null)
    {
        if (!OperatingSystem.IsWindows())
            return $"DEV-{wmiClass}-{property}";

        try
        {
            var query = condition is null
                ? $"SELECT {property} FROM {wmiClass}"
                : $"SELECT {property} FROM {wmiClass} WHERE {condition}";

            using var searcher = new ManagementObjectSearcher(query);
            foreach (var obj in searcher.Get().Cast<ManagementObject>())
            {
                var value = obj[property]?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
        }
        catch
        {
            return string.Empty;
        }

        return string.Empty;
    }

    private static string ReadMacAddress()
    {
        if (!OperatingSystem.IsWindows())
            return "DEV-MAC";

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT MACAddress FROM Win32_NetworkAdapter WHERE MACAddress IS NOT NULL AND NOT PNPDeviceID LIKE 'ROOT%'");
            foreach (var obj in searcher.Get().Cast<ManagementObject>())
            {
                var mac = obj["MACAddress"]?.ToString();
                if (!string.IsNullOrWhiteSpace(mac))
                    return mac.Replace(":", string.Empty, StringComparison.Ordinal);
            }
        }
        catch
        {
            return string.Empty;
        }

        return string.Empty;
    }

    private byte[] Encrypt(string plainText)
    {
        var secret = _configuration["Security:LicenseSecret"]
            ?? throw new InvalidOperationException("Security:LicenseSecret is not configured.");
        var salt = Encoding.UTF8.GetBytes("ABR-Device-License-Salt-v1");
        using var derive = new Rfc2898DeriveBytes(secret, salt, 100_000, HashAlgorithmName.SHA256);
        var key = derive.GetBytes(32);
        var iv = RandomNumberGenerator.GetBytes(16);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        return iv.Concat(cipherBytes).ToArray();
    }

    private string Decrypt(byte[] cipherWithIv)
    {
        var secret = _configuration["Security:LicenseSecret"]
            ?? throw new InvalidOperationException("Security:LicenseSecret is not configured.");
        var salt = Encoding.UTF8.GetBytes("ABR-Device-License-Salt-v1");
        using var derive = new Rfc2898DeriveBytes(secret, salt, 100_000, HashAlgorithmName.SHA256);
        var key = derive.GetBytes(32);

        var iv = cipherWithIv.Take(16).ToArray();
        var cipherBytes = cipherWithIv.Skip(16).ToArray();

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }

    private sealed class LicensePayload
    {
        public List<LicensedDevice> AllowedDevices { get; set; } = new();
    }

    private sealed class LicensedDevice
    {
        public string FingerprintHash { get; set; } = string.Empty;
        public string[] Parts { get; set; } = Array.Empty<string>();
    }
}
