using System.Security.Claims;
using ABR.Api.Authorization;
using ABR.Application.Common;
using ABR.Application.DTOs.Device;
using ABR.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABR.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/device")]
public class DeviceController : ControllerBase
{
    private readonly IDeviceLicenseService _deviceLicenseService;
    private readonly IValidator<AuthorizeDeviceRequest> _authorizeValidator;

    public DeviceController(
        IDeviceLicenseService deviceLicenseService,
        IValidator<AuthorizeDeviceRequest> authorizeValidator)
    {
        _deviceLicenseService = deviceLicenseService;
        _authorizeValidator = authorizeValidator;
    }

    [AllowAnonymous]
    [HttpGet("verify")]
    public async Task<ActionResult<ApiResponse<DeviceVerifyDto>>> Verify(CancellationToken cancellationToken)
    {
        var result = await _deviceLicenseService.VerifyAsync(cancellationToken);
        return Ok(ApiResponse<DeviceVerifyDto>.Ok(new DeviceVerifyDto
        {
            Result = result.Result,
            FingerprintHash = result.FingerprintHash,
            IsValid = result.IsValid
        }));
    }

    [RequirePermission(AppModules.Devices, PermissionLevel.Manage)]
    [HttpGet("licenses")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DeviceLicenseDto>>>> GetLicenses(CancellationToken cancellationToken)
    {
        var licenses = await _deviceLicenseService.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<DeviceLicenseDto>>.Ok(licenses));
    }

    [RequirePermission(AppModules.Devices, PermissionLevel.Manage)]
    [HttpPost("authorize")]
    public async Task<ActionResult<ApiResponse<DeviceLicenseDto>>> Authorize(
        [FromBody] AuthorizeDeviceRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _authorizeValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<DeviceLicenseDto>.Fail("Validation failed.", validation.Errors.Select(e => e.ErrorMessage).ToList()));

        var userId = Guid.Parse(User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException());

        var license = await _deviceLicenseService.AuthorizeAsync(request, userId, cancellationToken);
        return Ok(ApiResponse<DeviceLicenseDto>.Ok(license, "Device authorized."));
    }

    [RequirePermission(AppModules.Devices, PermissionLevel.Manage)]
    [HttpPut("licenses/{id:guid}/toggle")]
    public async Task<ActionResult<ApiResponse<object>>> ToggleActive(Guid id, CancellationToken cancellationToken)
    {
        var updated = await _deviceLicenseService.ToggleActiveAsync(id, cancellationToken);
        if (!updated)
            return NotFound(ApiResponse<object>.Fail("Device license not found."));

        return Ok(ApiResponse<object>.Ok(new { }, "Device status updated."));
    }
}
