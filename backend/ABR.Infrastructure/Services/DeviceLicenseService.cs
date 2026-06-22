using ABR.Application.DTOs.Auth;
using ABR.Application.DTOs.Device;
using ABR.Application.Interfaces;
using ABR.Domain.Entities;
using ABR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ABR.Infrastructure.Services;

public sealed class DeviceLicenseService : IDeviceLicenseService
{
    private readonly AbrDbContext _context;
    private readonly IDeviceFingerprintService _fingerprintService;

    public DeviceLicenseService(AbrDbContext context, IDeviceFingerprintService fingerprintService)
    {
        _context = context;
        _fingerprintService = fingerprintService;
    }

    public async Task<DeviceVerifyResponseDto> VerifyAsync(CancellationToken cancellationToken = default)
    {
        var fingerprint = _fingerprintService.GetCurrentFingerprint();
        var result = _fingerprintService.VerifyLicense();

        if (result is DeviceVerifyResult.Valid or DeviceVerifyResult.LockDisabled)
        {
            return new DeviceVerifyResponseDto
            {
                Result = result.ToString(),
                FingerprintHash = fingerprint,
                IsValid = true
            };
        }

        var dbAuthorized = await _context.DeviceLicenses
            .AnyAsync(d => d.IsActive && d.FingerprintHash == fingerprint, cancellationToken);

        if (dbAuthorized)
        {
            var activeLicense = await _context.DeviceLicenses
                .Where(d => d.IsActive && d.FingerprintHash == fingerprint)
                .FirstOrDefaultAsync(cancellationToken);

            if (activeLicense is not null)
            {
                activeLicense.LastVerifiedAt = DateTimeOffset.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }

            return new DeviceVerifyResponseDto
            {
                Result = DeviceVerifyResult.Valid.ToString(),
                FingerprintHash = fingerprint,
                IsValid = true
            };
        }

        return new DeviceVerifyResponseDto
        {
            Result = result.ToString(),
            FingerprintHash = fingerprint,
            IsValid = false
        };
    }

    public async Task<IReadOnlyList<DeviceLicenseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.DeviceLicenses
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new DeviceLicenseDto
            {
                Id = d.Id,
                DeviceName = d.DeviceName,
                FingerprintHash = d.FingerprintHash,
                IsActive = d.IsActive,
                LastVerifiedAt = d.LastVerifiedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<DeviceLicenseDto> AuthorizeAsync(
        AuthorizeDeviceRequest request,
        Guid authorizedById,
        CancellationToken cancellationToken = default)
    {
        var license = new DeviceLicense
        {
            DeviceName = request.DeviceName,
            FingerprintHash = request.FingerprintHash.ToLowerInvariant(),
            IsActive = true,
            AuthorizedById = authorizedById
        };

        _context.DeviceLicenses.Add(license);
        await _context.SaveChangesAsync(cancellationToken);

        var activeFingerprints = await _context.DeviceLicenses
            .Where(d => d.IsActive)
            .Select(d => d.FingerprintHash)
            .ToArrayAsync(cancellationToken);
        await _fingerprintService.StoreLicenseAsync(activeFingerprints, cancellationToken);

        return new DeviceLicenseDto
        {
            Id = license.Id,
            DeviceName = license.DeviceName,
            FingerprintHash = license.FingerprintHash,
            IsActive = license.IsActive,
            LastVerifiedAt = license.LastVerifiedAt
        };
    }

    public async Task<bool> ToggleActiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var license = await _context.DeviceLicenses.FindAsync([id], cancellationToken);
        if (license is null)
            return false;

        license.IsActive = !license.IsActive;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
