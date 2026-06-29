namespace ABR.Application.DTOs.Auth;

public class LicenseStatusDto
{
    public bool IsValid { get; set; }
    public string ExpiryDate { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
