namespace ABR.Application.DTOs.Device;

public class DeviceLicenseDto
{
    public Guid Id { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string FingerprintHash { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset? LastVerifiedAt { get; set; }
}

public class AuthorizeDeviceRequest
{
    public string DeviceName { get; set; } = string.Empty;
    public string FingerprintHash { get; set; } = string.Empty;
}

public class DeviceVerifyDto
{
    public string Result { get; set; } = string.Empty;
    public string FingerprintHash { get; set; } = string.Empty;
    public bool IsValid { get; set; }
}
