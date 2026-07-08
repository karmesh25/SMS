using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ABR.Application.Common;

namespace ABR.Infrastructure.Security;

public static class SecretsProtector
{
    private static readonly byte[] KeyDerivationSalt = Encoding.UTF8.GetBytes("ABR-Secrets-Salt-v1");
    private const int KeyIterations = 100_000;

    public static byte[] Encrypt(string plainText, string masterPassword)
    {
        if (string.IsNullOrWhiteSpace(masterPassword))
            throw new ArgumentException("Master password is required.", nameof(masterPassword));

        var key = DeriveKey(masterPassword);
        var iv = RandomNumberGenerator.GetBytes(16);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        return iv.Concat(cipherBytes).ToArray();
    }

    public static string Decrypt(byte[] cipherWithIv, string masterPassword)
    {
        if (string.IsNullOrWhiteSpace(masterPassword))
            throw new ArgumentException("Master password is required.", nameof(masterPassword));

        if (cipherWithIv.Length <= 16)
            throw new CryptographicException("Invalid secrets file.");

        var key = DeriveKey(masterPassword);
        var iv = cipherWithIv.Take(16).ToArray();
        var cipherBytes = cipherWithIv.Skip(16).ToArray();

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor();
        try
        {
            var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (CryptographicException)
        {
            throw new CryptographicException("Invalid master password or corrupted secrets file.");
        }
    }

    public static PendriveSecrets GenerateNew(string dbPassword)
    {
        return new PendriveSecrets
        {
            ConnectionStrings = new PendriveConnectionStrings
            {
                DefaultConnection =
                    $"Host=127.0.0.1;Port=5433;Database=abr_db;Username=postgres;Password={dbPassword}"
            },
            Jwt = new PendriveJwtSettings
            {
                SecretKey = GenerateRandomToken(64),
                Issuer = "ABR.Api",
                Audience = "ABR.Frontend",
                ExpiryHours = 8
            },
            Security = new PendriveSecuritySettings
            {
                LicenseSecret = GenerateRandomToken(64)
            }
        };
    }

    public static string Serialize(PendriveSecrets secrets) =>
        JsonSerializer.Serialize(secrets, JsonOptions);

    public static PendriveSecrets Deserialize(string json) =>
        JsonSerializer.Deserialize<PendriveSecrets>(json, JsonOptions)
        ?? throw new InvalidOperationException("Invalid secrets payload.");

    public static string GenerateDbPassword() => GenerateRandomToken(32);

    public static string? ExtractDbPassword(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return null;

        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var idx = part.IndexOf('=');
            if (idx <= 0)
                continue;

            if (string.Equals(part[..idx].Trim(), "Password", StringComparison.OrdinalIgnoreCase))
                return part[(idx + 1)..].Trim();
        }

        return null;
    }

    public static IReadOnlyDictionary<string, string?> ToConfigurationMap(PendriveSecrets secrets) =>
        new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = secrets.ConnectionStrings.DefaultConnection,
            ["Jwt:SecretKey"] = secrets.Jwt.SecretKey,
            ["Jwt:Issuer"] = secrets.Jwt.Issuer,
            ["Jwt:Audience"] = secrets.Jwt.Audience,
            ["Jwt:ExpiryHours"] = secrets.Jwt.ExpiryHours.ToString(),
            ["Security:LicenseSecret"] = secrets.Security.LicenseSecret
        };

    private static string GenerateRandomToken(int length)
    {
        const string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_";
        var bytes = RandomNumberGenerator.GetBytes(length);
        var chars = new char[length];
        for (var i = 0; i < length; i++)
            chars[i] = alphabet[bytes[i] % alphabet.Length];
        return new string(chars);
    }

    private static byte[] DeriveKey(string masterPassword)
    {
        using var derive = new Rfc2898DeriveBytes(masterPassword, KeyDerivationSalt, KeyIterations, HashAlgorithmName.SHA256);
        return derive.GetBytes(32);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
}
