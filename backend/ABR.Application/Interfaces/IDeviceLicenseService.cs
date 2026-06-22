using ABR.Application.DTOs.Device;

namespace ABR.Application.Interfaces;

public interface IDeviceLicenseService
{
    Task<IReadOnlyList<DeviceLicenseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<DeviceLicenseDto> AuthorizeAsync(AuthorizeDeviceRequest request, Guid authorizedById, CancellationToken cancellationToken = default);
    Task<bool> ToggleActiveAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DeviceVerifyResponseDto> VerifyAsync(CancellationToken cancellationToken = default);
}

public sealed class DeviceVerifyResponseDto
{
    public string Result { get; set; } = string.Empty;
    public string FingerprintHash { get; set; } = string.Empty;
    public bool IsValid { get; set; }
}
