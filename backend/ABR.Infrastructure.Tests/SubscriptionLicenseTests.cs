using ABR.Application.Common;

namespace ABR.Infrastructure.Tests;

public class SubscriptionLicenseTests
{
    [Fact]
    public void IsExpiredOn_ReturnsFalse_WhenDateIsOnOrBeforeExpiry()
    {
        Assert.False(SubscriptionLicense.IsExpiredOn(new DateOnly(2026, 7, 20)));
        Assert.False(SubscriptionLicense.IsExpiredOn(new DateOnly(2026, 1, 1)));
    }

    [Fact]
    public void IsExpiredOn_ReturnsTrue_WhenDateIsAfterExpiry()
    {
        Assert.True(SubscriptionLicense.IsExpiredOn(new DateOnly(2026, 7, 21)));
        Assert.True(SubscriptionLicense.IsExpiredOn(new DateOnly(2027, 1, 1)));
    }

    [Fact]
    public void IsExpiredOn_ReturnsFalse_WhenLicenseDisabled()
    {
        Assert.False(SubscriptionLicense.IsExpiredOn(new DateOnly(2030, 1, 1), enabledOverride: false));
    }
}
