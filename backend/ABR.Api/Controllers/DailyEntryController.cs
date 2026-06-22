using System.Security.Claims;
using ABR.Api.Authorization;
using ABR.Application.Common;
using ABR.Application.DTOs.Accounting;
using ABR.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABR.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/daily-entries")]
public class DailyEntryController : ControllerBase
{
    private readonly IDailyEntryService _service;
    private readonly IValidator<CreateDailyEntryDto> _createValidator;
    private readonly IValidator<UpdateDailyEntryDto> _updateValidator;

    public DailyEntryController(
        IDailyEntryService service,
        IValidator<CreateDailyEntryDto> createValidator,
        IValidator<UpdateDailyEntryDto> updateValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedDailyEntriesDto>>> GetList([FromQuery] DailyEntryFilterDto filter, CancellationToken cancellationToken)
    {
        var result = await _service.GetListAsync(filter, cancellationToken);
        return Ok(ApiResponse<PagedDailyEntriesDto>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DailyEntryDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var entry = await _service.GetByIdAsync(id, cancellationToken);
        if (entry is null) return NotFound(ApiResponse<DailyEntryDto>.Fail("Entry not found."));
        return Ok(ApiResponse<DailyEntryDto>.Ok(entry));
    }

    [RequireRole("SuperAdmin", "Admin", "OfficeStaff")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<DailyEntryDto>>> Create([FromBody] CreateDailyEntryDto dto, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<DailyEntryDto>.Fail("Validation failed.", validation.Errors.Select(e => e.ErrorMessage).ToList()));

        var entry = await _service.CreateAsync(dto, GetUserId(), cancellationToken);
        return Ok(ApiResponse<DailyEntryDto>.Ok(entry, "Entry created."));
    }

    [RequireRole("SuperAdmin", "Admin", "OfficeStaff")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DailyEntryDto>>> Update(Guid id, [FromBody] UpdateDailyEntryDto dto, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<DailyEntryDto>.Fail("Validation failed.", validation.Errors.Select(e => e.ErrorMessage).ToList()));

        var entry = await _service.UpdateAsync(id, dto, GetUserId(), cancellationToken);
        if (entry is null) return NotFound(ApiResponse<DailyEntryDto>.Fail("Entry not found."));
        return Ok(ApiResponse<DailyEntryDto>.Ok(entry, "Entry updated."));
    }

    [RequireRole("SuperAdmin", "Admin", "OfficeStaff")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _service.DeleteAsync(id, GetUserId(), cancellationToken);
        if (!deleted) return NotFound(ApiResponse<object>.Fail("Entry not found."));
        return Ok(ApiResponse<object>.Ok(new { }, "Entry deleted."));
    }

    [HttpGet("profit/{siteId:guid}")]
    public async Task<ActionResult<ApiResponse<ProfitSummaryDto>>> GetProfit(Guid siteId, CancellationToken cancellationToken)
    {
        var profit = await _service.GetProfitAsync(siteId, cancellationToken);
        return Ok(ApiResponse<ProfitSummaryDto>.Ok(profit));
    }

    [HttpGet("balance/{siteId:guid}")]
    public async Task<ActionResult<ApiResponse<BalanceSummaryDto>>> GetBalance(Guid siteId, CancellationToken cancellationToken)
    {
        var balance = await _service.GetBalanceAsync(siteId, cancellationToken);
        return Ok(ApiResponse<BalanceSummaryDto>.Ok(balance));
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
