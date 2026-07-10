using ABR.Application.Interfaces;
using ABR.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace ABR.Infrastructure.Tests;

public class DeviceFingerprintServiceTests
{
    [Fact]
    public void IsDeviceLockEnforced_ReadsConfiguration()
    {
        var enabled = CreateService(enforceDeviceLock: true);
        var disabled = CreateService(enforceDeviceLock: false);

        Assert.True(enabled.IsDeviceLockEnforced());
        Assert.False(disabled.IsDeviceLockEnforced());
    }

    [Fact]
    public void VerifyLicense_ReturnsLockDisabled_WhenNotEnforced()
    {
        var service = CreateService(enforceDeviceLock: false);
        Assert.Equal(DeviceVerifyResult.LockDisabled, service.VerifyLicense());
    }

    [Fact]
    public void GetCurrentFingerprint_ReturnsStableLowercaseHexHash()
    {
        var service = CreateService(enforceDeviceLock: false);
        var first = service.GetCurrentFingerprint();
        var second = service.GetCurrentFingerprint();

        Assert.Equal(first, second);
        Assert.Equal(64, first.Length);
        Assert.Matches("^[0-9a-f]{64}$", first);
    }

    [Fact]
    public async Task VerifyLicense_ReturnsValid_ForStoredCurrentDeviceFingerprint()
    {
        var licensePath = Path.Combine(Path.GetTempPath(), $"abr-test-{Guid.NewGuid():N}.lic");
        var service = CreateService(enforceDeviceLock: true, licensePath);
        try
        {
            var fingerprint = service.GetCurrentFingerprint();
            await service.StoreLicenseAsync(new[] { fingerprint });

            // Regression guard: the encrypted license file must actually validate
            // the authorized device (previously it never matched — Parts were empty).
            Assert.Equal(DeviceVerifyResult.Valid, service.VerifyLicense());
        }
        finally
        {
            if (File.Exists(licensePath)) File.Delete(licensePath);
        }
    }

    [Fact]
    public async Task VerifyLicense_ReturnsInvalidDevice_ForUnknownFingerprint()
    {
        var licensePath = Path.Combine(Path.GetTempPath(), $"abr-test-{Guid.NewGuid():N}.lic");
        var service = CreateService(enforceDeviceLock: true, licensePath);
        try
        {
            await service.StoreLicenseAsync(new[] { new string('a', 64) });
            Assert.Equal(DeviceVerifyResult.InvalidDevice, service.VerifyLicense());
        }
        finally
        {
            if (File.Exists(licensePath)) File.Delete(licensePath);
        }
    }

    private static DeviceFingerprintService CreateService(bool enforceDeviceLock, string? licenseFilePath = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:EnforceDeviceLock"] = enforceDeviceLock.ToString(),
                ["Security:LicenseSecret"] = "test-license-secret-value",
                ["Security:LicenseFilePath"] = licenseFilePath
                    ?? Path.Combine(Path.GetTempPath(), $"abr-test-{Guid.NewGuid():N}.lic")
            })
            .Build();

        return new DeviceFingerprintService(config, Mock.Of<ILogger<DeviceFingerprintService>>());
    }
}
