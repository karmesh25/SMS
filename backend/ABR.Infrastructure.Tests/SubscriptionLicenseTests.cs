using ABR.Application.Common;

namespace ABR.Infrastructure.Tests;

public class SubscriptionLicenseTests
{
    // Derive from the configured expiry so the test survives future extensions.
    private static readonly DateOnly Expiry = SubscriptionLicense.ExpiryDate;

    [Fact]
    public void IsExpiredOn_ReturnsFalse_WhenDateIsOnOrBeforeExpiry()
    {
        Assert.False(SubscriptionLicense.IsExpiredOn(Expiry));
        Assert.False(SubscriptionLicense.IsExpiredOn(Expiry.AddDays(-1)));
        Assert.False(SubscriptionLicense.IsExpiredOn(Expiry.AddYears(-1)));
    }

    [Fact]
    public void IsExpiredOn_ReturnsTrue_WhenDateIsAfterExpiry()
    {
        Assert.True(SubscriptionLicense.IsExpiredOn(Expiry.AddDays(1)));
        Assert.True(SubscriptionLicense.IsExpiredOn(Expiry.AddYears(1)));
    }

    [Fact]
    public void IsExpiredOn_ReturnsFalse_WhenLicenseDisabled()
    {
        Assert.False(SubscriptionLicense.IsExpiredOn(Expiry.AddYears(5), enabledOverride: false));
    }
}
