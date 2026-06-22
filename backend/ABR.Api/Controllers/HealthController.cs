using ABR.Application.Common;
using ABR.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ABR.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly AbrDbContext _context;

    public HealthController(AbrDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<HealthDto>>> Get(CancellationToken cancellationToken)
    {
        var dbConnected = false;
        try
        {
            dbConnected = await _context.Database.CanConnectAsync(cancellationToken);
        }
        catch
        {
            dbConnected = false;
        }

        return Ok(ApiResponse<HealthDto>.Ok(new HealthDto
        {
            Status = dbConnected ? "healthy" : "degraded",
            Version = "1.0.0-phase8",
            Timestamp = DateTimeOffset.UtcNow,
            DatabaseConnected = dbConnected
        }));
    }
}
