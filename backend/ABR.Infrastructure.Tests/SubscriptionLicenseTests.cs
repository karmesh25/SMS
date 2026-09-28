using ABR.Application.Common;

namespace ABR.Infrastructure.Tests;

public class SubscriptionLicenseTests
{
    public SubscriptionLicenseTests()
    {
        // Reset to defaults so tests do not leak Configure() side effects between runs.
        SubscriptionLicense.Configure(enabled: true, expiryDate: SubscriptionLicense.DefaultExpiryDate);
    }

    [Fact]
    public void IsExpiredOn_ReturnsFalse_WhenDateIsOnOrBeforeExpiry()
    {
        var expiry = SubscriptionLicense.ExpiryDate;
        Assert.False(SubscriptionLicense.IsExpiredOn(expiry));
        Assert.False(SubscriptionLicense.IsExpiredOn(expiry.AddDays(-1)));
        Assert.False(SubscriptionLicense.IsExpiredOn(expiry.AddYears(-1)));
    }

    [Fact]
    public void IsExpiredOn_ReturnsTrue_WhenDateIsAfterExpiry()
    {
        var expiry = SubscriptionLicense.ExpiryDate;
        Assert.True(SubscriptionLicense.IsExpiredOn(expiry.AddDays(1)));
        Assert.True(SubscriptionLicense.IsExpiredOn(expiry.AddYears(1)));
    }

    [Fact]
    public void IsExpiredOn_ReturnsFalse_WhenLicenseDisabled()
    {
        Assert.False(SubscriptionLicense.IsExpiredOn(
            SubscriptionLicense.ExpiryDate.AddYears(5),
            enabledOverride: false));
    }

    [Fact]
    public void Configure_UpdatesExpiryDateFromOverride()
    {
        var newExpiry = new DateOnly(2030, 1, 15);
        SubscriptionLicense.Configure(expiryDate: newExpiry);
        Assert.Equal(newExpiry, SubscriptionLicense.ExpiryDate);
        Assert.False(SubscriptionLicense.IsExpiredOn(newExpiry));
        Assert.True(SubscriptionLicense.IsExpiredOn(newExpiry.AddDays(1)));
    }
}
