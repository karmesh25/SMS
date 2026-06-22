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

    private static DeviceFingerprintService CreateService(bool enforceDeviceLock)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:EnforceDeviceLock"] = enforceDeviceLock.ToString(),
                ["Security:LicenseSecret"] = "test-license-secret-value",
                ["Security:LicenseFilePath"] = Path.Combine(Path.GetTempPath(), $"abr-test-{Guid.NewGuid():N}.lic")
            })
            .Build();

        return new DeviceFingerprintService(config, Mock.Of<ILogger<DeviceFingerprintService>>());
    }
}
