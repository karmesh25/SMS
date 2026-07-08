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
[Route("api/journal-vouchers")]
public class JournalVoucherController : ControllerBase
{
    private readonly IJournalVoucherService _service;
    private readonly IExportFileStorage _exportStorage;
    private readonly IValidator<CreateJournalVoucherDto> _createValidator;
    private readonly IValidator<UpdateJournalVoucherDto> _updateValidator;

    public JournalVoucherController(
        IJournalVoucherService service,
        IExportFileStorage exportStorage,
        IValidator<CreateJournalVoucherDto> createValidator,
        IValidator<UpdateJournalVoucherDto> updateValidator)
    {
        _service = service;
        _exportStorage = exportStorage;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    [RequirePermission(AppModules.JournalVoucher, PermissionLevel.View)]
    public async Task<ActionResult<ApiResponse<PagedJournalVouchersDto>>> GetList([FromQuery] JournalVoucherFilterDto filter, CancellationToken cancellationToken)
    {
        var result = await _service.GetListAsync(filter, cancellationToken);
        return Ok(ApiResponse<PagedJournalVouchersDto>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(AppModules.JournalVoucher, PermissionLevel.View)]
    public async Task<ActionResult<ApiResponse<JournalVoucherDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        if (result is null)
            return NotFound(ApiResponse<JournalVoucherDto>.Fail("Journal voucher not found."));
        return Ok(ApiResponse<JournalVoucherDto>.Ok(result));
    }

    [HttpPost]
    [RequirePermission(AppModules.JournalVoucher, PermissionLevel.Manage)]
    public async Task<ActionResult<ApiResponse<JournalVoucherDto>>> Create([FromBody] CreateJournalVoucherDto dto, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<JournalVoucherDto>.Fail("Validation failed.", validation.Errors.Select(e => e.ErrorMessage).ToList()));

        var result = await _service.CreateAsync(dto, GetUserId(), cancellationToken);
        return Ok(ApiResponse<JournalVoucherDto>.Ok(result, "Journal voucher created."));
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(AppModules.JournalVoucher, PermissionLevel.Manage)]
    public async Task<ActionResult<ApiResponse<JournalVoucherDto>>> Update(Guid id, [FromBody] UpdateJournalVoucherDto dto, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<JournalVoucherDto>.Fail("Validation failed.", validation.Errors.Select(e => e.ErrorMessage).ToList()));

        var result = await _service.UpdateAsync(id, dto, GetUserId(), cancellationToken);
        if (result is null)
            return NotFound(ApiResponse<JournalVoucherDto>.Fail("Journal voucher not found."));
        return Ok(ApiResponse<JournalVoucherDto>.Ok(result, "Journal voucher updated."));
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(AppModules.JournalVoucher, PermissionLevel.Manage)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _service.DeleteAsync(id, GetUserId(), cancellationToken);
        if (!deleted)
            return NotFound(ApiResponse<object>.Fail("Journal voucher not found."));
        return Ok(ApiResponse<object>.Ok(new { }, "Journal voucher deleted."));
    }

    [HttpGet("export/ledger-excel")]
    [RequirePermission(AppModules.JournalVoucher, PermissionLevel.View)]
    public async Task<IActionResult> ExportLedgerExcel([FromQuery] JournalVoucherLedgerExportRequestDto request, CancellationToken cancellationToken)
    {
        if (request.SiteId == Guid.Empty)
            return BadRequest(new { message = "SiteId is required." });

        var file = await _service.ExportLedgerExcelAsync(request, cancellationToken);
        return await ExportDownloadResults.FromBytesAsync(_exportStorage, file.Content, file.ContentType, file.FileName, cancellationToken);
    }

    [HttpGet("export/ledger-pdf")]
    [RequirePermission(AppModules.JournalVoucher, PermissionLevel.View)]
    public async Task<IActionResult> ExportLedgerPdf([FromQuery] JournalVoucherLedgerExportRequestDto request, CancellationToken cancellationToken)
    {
        if (request.SiteId == Guid.Empty)
            return BadRequest(new { message = "SiteId is required." });

        var file = await _service.ExportLedgerPdfAsync(request, cancellationToken);
        return await ExportDownloadResults.FromBytesAsync(_exportStorage, file.Content, file.ContentType, file.FileName, cancellationToken);
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
