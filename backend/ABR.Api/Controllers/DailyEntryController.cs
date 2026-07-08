using System.Security.Claims;
using ABR.Api.Authorization;
using ABR.Api.Helpers;
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
    private readonly IDailyEntryExcelService _excelService;
    private readonly IExportFileStorage _exportStorage;
    private readonly IValidator<CreateDailyEntryDto> _createValidator;
    private readonly IValidator<UpdateDailyEntryDto> _updateValidator;

    public DailyEntryController(
        IDailyEntryService service,
        IDailyEntryExcelService excelService,
        IExportFileStorage exportStorage,
        IValidator<CreateDailyEntryDto> createValidator,
        IValidator<UpdateDailyEntryDto> updateValidator)
    {
        _service = service;
        _excelService = excelService;
        _exportStorage = exportStorage;
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

    [RequirePermission(AppModules.DailyEntry, PermissionLevel.Manage)]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<DailyEntryDto>>> Create([FromBody] CreateDailyEntryDto dto, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<DailyEntryDto>.Fail("Validation failed.", validation.Errors.Select(e => e.ErrorMessage).ToList()));

        var entry = await _service.CreateAsync(dto, GetUserId(), cancellationToken);
        return Ok(ApiResponse<DailyEntryDto>.Ok(entry, "Entry created."));
    }

    [RequirePermission(AppModules.DailyEntry, PermissionLevel.Manage)]
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

    [RequirePermission(AppModules.DailyEntry, PermissionLevel.Manage)]
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

    [RequirePermission(AppModules.DailyEntry, PermissionLevel.Manage)]
    [HttpGet("import/sample")]
    public async Task<IActionResult> DownloadImportSample(CancellationToken cancellationToken)
    {
        var file = await _excelService.GetSampleAsync(cancellationToken);
        return await ExportDownloadResults.FromBytesAsync(
            _exportStorage,
            file.Content,
            file.ContentType,
            file.FileName,
            cancellationToken);
    }

    [RequirePermission(AppModules.DailyEntry, PermissionLevel.Manage)]
    [HttpPost("import")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<DailyEntryImportResultDto>>> ImportExcel(
        [FromQuery] Guid siteId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (siteId == Guid.Empty)
            return BadRequest(ApiResponse<DailyEntryImportResultDto>.Fail("SiteId is required."));
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse<DailyEntryImportResultDto>.Fail("Excel file is required."));
        if (!string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse<DailyEntryImportResultDto>.Fail("Only .xlsx files are supported."));

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _excelService.ImportAsync(siteId, stream, GetUserId(), cancellationToken);
            var message = result.ImportedCount > 0
                ? $"Imported {result.ImportedCount} entries."
                : "No entries were imported.";
            return Ok(ApiResponse<DailyEntryImportResultDto>.Ok(result, message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<DailyEntryImportResultDto>.Fail(ex.Message));
        }
    }

    [HttpGet("export/ledger-excel")]
    [RequirePermission(AppModules.DailyEntry, PermissionLevel.View)]
    public async Task<IActionResult> ExportLedgerExcel(
        [FromQuery] DailyEntryLedgerExportRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request.SiteId == Guid.Empty)
            return BadRequest(new { message = "SiteId is required." });

        var file = await _excelService.ExportLedgerExcelAsync(request, cancellationToken);
        return await ExportDownloadResults.FromBytesAsync(
            _exportStorage,
            file.Content,
            file.ContentType,
            file.FileName,
            cancellationToken);
    }

    [HttpGet("export/ledger-pdf")]
    [RequirePermission(AppModules.DailyEntry, PermissionLevel.View)]
    public async Task<IActionResult> ExportLedgerPdf(
        [FromQuery] DailyEntryLedgerExportRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request.SiteId == Guid.Empty)
            return BadRequest(new { message = "SiteId is required." });

        var file = await _excelService.ExportLedgerPdfAsync(request, cancellationToken);
        return await ExportDownloadResults.FromBytesAsync(
            _exportStorage,
            file.Content,
            file.ContentType,
            file.FileName,
            cancellationToken);
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
