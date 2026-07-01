namespace ABR.Application.Common;

public static class SubscriptionLicense
{
    public static readonly bool Enabled = true;
    public static readonly DateOnly ExpiryDate = new(2028, 7, 20);

    public const string ExpiredMessage = "License expired. Please contact your administrator.";

    public static bool IsExpiredOn(DateOnly today, bool? enabledOverride = null)
    {
        var enabled = enabledOverride ?? Enabled;
        return enabled && today > ExpiryDate;
    }

    public static bool IsExpired =>
        IsExpiredOn(DateOnly.FromDateTime(DateTime.UtcNow));
}
