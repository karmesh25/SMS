namespace ABR.Application.Interfaces;

public enum DeviceVerifyResult
{
    Valid,
    InvalidDevice,
    FileNotFound,
    FileTampered,
    LockDisabled
}

public interface IDeviceFingerprintService
{
    string GetCurrentFingerprint();
    DeviceHardwareParts GetCurrentHardwareParts();
    DeviceVerifyResult VerifyLicense();
    Task StoreLicenseAsync(string[] allowedFingerprints, CancellationToken cancellationToken = default);
    bool IsDeviceLockEnforced();
}

public sealed class DeviceHardwareParts
{
    public string MotherboardSerial { get; init; } = string.Empty;
    public string CpuId { get; init; } = string.Empty;
    public string DiskSerial { get; init; } = string.Empty;
    public string MacAddress { get; init; } = string.Empty;

    public string[] ToArray() => [MotherboardSerial, CpuId, DiskSerial, MacAddress];
}

public sealed class DeviceVerifyResponse
{
    public DeviceVerifyResult Result { get; init; }
    public string FingerprintHash { get; init; } = string.Empty;
    public bool IsValid => Result is DeviceVerifyResult.Valid or DeviceVerifyResult.LockDisabled;
}
