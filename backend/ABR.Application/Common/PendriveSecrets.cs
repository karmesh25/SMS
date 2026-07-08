namespace ABR.Application.Common;

public sealed class PendriveSecrets
{
    public PendriveConnectionStrings ConnectionStrings { get; set; } = new();
    public PendriveJwtSettings Jwt { get; set; } = new();
    public PendriveSecuritySettings Security { get; set; } = new();
}

public sealed class PendriveConnectionStrings
{
    public string DefaultConnection { get; set; } = string.Empty;
}

public sealed class PendriveJwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "ABR.Api";
    public string Audience { get; set; } = "ABR.Frontend";
    public int ExpiryHours { get; set; } = 8;
}

public sealed class PendriveSecuritySettings
{
    public string LicenseSecret { get; set; } = string.Empty;
}
