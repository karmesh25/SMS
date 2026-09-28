namespace ABR.Application.Common;

public static class SubscriptionLicense
{
    public static readonly DateOnly DefaultExpiryDate = new(2028, 7, 20);

    public static bool Enabled { get; private set; } = true;
    public static DateOnly ExpiryDate { get; private set; } = DefaultExpiryDate;

    public const string ExpiredMessage = "License expired. Please contact your administrator.";

    /// <summary>
    /// Applies runtime overrides (typically from appsettings). Null values leave the current setting unchanged.
    /// </summary>
    public static void Configure(bool? enabled = null, DateOnly? expiryDate = null)
    {
        if (enabled.HasValue)
            Enabled = enabled.Value;

        if (expiryDate.HasValue)
            ExpiryDate = expiryDate.Value;
    }

    public static bool IsExpiredOn(DateOnly today, bool? enabledOverride = null)
    {
        var enabled = enabledOverride ?? Enabled;
        return enabled && today > ExpiryDate;
    }

    public static bool IsExpired =>
        IsExpiredOn(DateOnly.FromDateTime(DateTime.UtcNow));
}
