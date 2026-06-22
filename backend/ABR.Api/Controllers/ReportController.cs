using ABR.Application.Common;
using ABR.Application.DTOs.Reports;
using ABR.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABR.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/reports")]
public class ReportController : ControllerBase
{
    private readonly IReportService _service;
    private readonly IValidator<AllEntryReportFilterDto> _allEntryValidator;
    private readonly IValidator<BalanceSheetFilterDto> _balanceSheetValidator;
    private readonly IValidator<TillDateReportFilterDto> _tillDateValidator;
    private readonly IValidator<MonthwiseReportFilterDto> _monthwiseValidator;
    private readonly IValidator<BankStatementFilterDto> _bankStatementValidator;
    private readonly IValidator<SellDetailsFilterDto> _sellDetailsValidator;
    private readonly IValidator<InstallmentReportFilterDto> _installmentValidator;

    public ReportController(
        IReportService service,
        IValidator<AllEntryReportFilterDto> allEntryValidator,
        IValidator<BalanceSheetFilterDto> balanceSheetValidator,
        IValidator<TillDateReportFilterDto> tillDateValidator,
        IValidator<MonthwiseReportFilterDto> monthwiseValidator,
        IValidator<BankStatementFilterDto> bankStatementValidator,
        IValidator<SellDetailsFilterDto> sellDetailsValidator,
        IValidator<InstallmentReportFilterDto> installmentValidator)
    {
        _service = service;
        _allEntryValidator = allEntryValidator;
        _balanceSheetValidator = balanceSheetValidator;
        _tillDateValidator = tillDateValidator;
        _monthwiseValidator = monthwiseValidator;
        _bankStatementValidator = bankStatementValidator;
        _sellDetailsValidator = sellDetailsValidator;
        _installmentValidator = installmentValidator;
    }

    [HttpGet("all-entry")]
    public async Task<ActionResult<ApiResponse<PagedReportDto<AllEntryReportRowDto>>>> GetAllEntry([FromQuery] AllEntryReportFilterDto filter, CancellationToken cancellationToken)
    {
        var validation = await _allEntryValidator.ValidateAsync(filter, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<PagedReportDto<AllEntryReportRowDto>>.Fail("Validation failed.", validation.Errors.Select(e => e.ErrorMessage).ToList()));

        var result = await _service.GetAllEntryAsync(filter, cancellationToken);
        return Ok(ApiResponse<PagedReportDto<AllEntryReportRowDto>>.Ok(result));
    }

    [HttpGet("balance-sheet")]
    public async Task<ActionResult<ApiResponse<BalanceSheetReportDto>>> GetBalanceSheet([FromQuery] BalanceSheetFilterDto filter, CancellationToken cancellationToken)
    {
        var validation = await _balanceSheetValidator.ValidateAsync(filter, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<BalanceSheetReportDto>.Fail("Validation failed.", validation.Errors.Select(e => e.ErrorMessage).ToList()));

        var result = await _service.GetBalanceSheetAsync(filter, cancellationToken);
        return Ok(ApiResponse<BalanceSheetReportDto>.Ok(result));
    }

    [HttpGet("till-date")]
    public async Task<ActionResult<ApiResponse<PagedReportDto<TillDateReportRowDto>>>> GetTillDate([FromQuery] TillDateReportFilterDto filter, CancellationToken cancellationToken)
    {
        var validation = await _tillDateValidator.ValidateAsync(filter, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<PagedReportDto<TillDateReportRowDto>>.Fail("Validation failed.", validation.Errors.Select(e => e.ErrorMessage).ToList()));

        var result = await _service.GetTillDateAsync(filter, cancellationToken);
        return Ok(ApiResponse<PagedReportDto<TillDateReportRowDto>>.Ok(result));
    }

    [HttpGet("monthwise")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MonthwiseReportRowDto>>>> GetMonthwise([FromQuery] MonthwiseReportFilterDto filter, CancellationToken cancellationToken)
    {
        var validation = await _monthwiseValidator.ValidateAsync(filter, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<IReadOnlyList<MonthwiseReportRowDto>>.Fail("Validation failed.", validation.Errors.Select(e => e.ErrorMessage).ToList()));

        var result = await _service.GetMonthwiseAsync(filter, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<MonthwiseReportRowDto>>.Ok(result));
    }

    [HttpGet("bank-statement")]
    public async Task<ActionResult<ApiResponse<BankStatementReportDto>>> GetBankStatement([FromQuery] BankStatementFilterDto filter, CancellationToken cancellationToken)
    {
        var validation = await _bankStatementValidator.ValidateAsync(filter, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<BankStatementReportDto>.Fail("Validation failed.", validation.Errors.Select(e => e.ErrorMessage).ToList()));

        try
        {
            var result = await _service.GetBankStatementAsync(filter, cancellationToken);
            return Ok(ApiResponse<BankStatementReportDto>.Ok(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<BankStatementReportDto>.Fail(ex.Message));
        }
    }

    [HttpGet("sell-details")]
    public async Task<ActionResult<ApiResponse<SellDetailsReportDto>>> GetSellDetails([FromQuery] SellDetailsFilterDto filter, CancellationToken cancellationToken)
    {
        var validation = await _sellDetailsValidator.ValidateAsync(filter, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<SellDetailsReportDto>.Fail("Validation failed.", validation.Errors.Select(e => e.ErrorMessage).ToList()));

        var result = await _service.GetSellDetailsAsync(filter, cancellationToken);
        return Ok(ApiResponse<SellDetailsReportDto>.Ok(result));
    }

    [HttpGet("installment")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<InstallmentReportRowDto>>>> GetInstallment([FromQuery] InstallmentReportFilterDto filter, CancellationToken cancellationToken)
    {
        var validation = await _installmentValidator.ValidateAsync(filter, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<IReadOnlyList<InstallmentReportRowDto>>.Fail("Validation failed.", validation.Errors.Select(e => e.ErrorMessage).ToList()));

        try
        {
            var result = await _service.GetInstallmentAsync(filter, cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<InstallmentReportRowDto>>.Ok(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<IReadOnlyList<InstallmentReportRowDto>>.Fail(ex.Message));
        }
    }
}
