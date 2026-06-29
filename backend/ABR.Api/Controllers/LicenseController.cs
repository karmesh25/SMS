using ABR.Application.Common;
using ABR.Application.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABR.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/license")]
public class LicenseController : ControllerBase
{
    [HttpGet("status")]
    public ActionResult<ApiResponse<LicenseStatusDto>> GetStatus()
    {
        var expired = SubscriptionLicense.IsExpired;
        var status = new LicenseStatusDto
        {
            IsValid = !expired,
            ExpiryDate = SubscriptionLicense.ExpiryDate.ToString("yyyy-MM-dd"),
            Message = expired ? SubscriptionLicense.ExpiredMessage : "License is active."
        };

        return Ok(ApiResponse<LicenseStatusDto>.Ok(status));
    }
}
