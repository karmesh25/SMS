using ABR.Api.Authorization;
using ABR.Application.Common;
using ABR.Application.DTOs.Dashboard;
using ABR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABR.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _service;

    public DashboardController(IDashboardService service) => _service = service;

    [HttpGet("summary")]
    [RequireRole("SuperAdmin", "Admin", "OfficeStaff", "ViewOnly")]
    public async Task<ActionResult<ApiResponse<DashboardSummaryDto>>> GetSummary([FromQuery] Guid siteId, CancellationToken cancellationToken)
    {
        if (siteId == Guid.Empty)
            return BadRequest(ApiResponse<DashboardSummaryDto>.Fail("siteId is required."));

        try
        {
            var result = await _service.GetSummaryAsync(siteId, cancellationToken);
            return Ok(ApiResponse<DashboardSummaryDto>.Ok(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<DashboardSummaryDto>.Fail(ex.Message));
        }
    }
}
