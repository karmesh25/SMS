using ABR.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace ABR.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<ApiResponse<HealthDto>> Get()
    {
        return Ok(ApiResponse<HealthDto>.Ok(new HealthDto
        {
            Status = "healthy",
            Version = "1.0.0-phase0",
            Timestamp = DateTimeOffset.UtcNow
        }));
    }
}
