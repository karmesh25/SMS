using ABR.Api.Authorization;
using ABR.Application.Common;
using ABR.Application.DTOs.Vyaj;
using ABR.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABR.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/vyaj")]
public class VyajController : ControllerBase
{
    private readonly IVyajService _service;
    private readonly IValidator<CreateVyajPartyDto> _createPartyValidator;
    private readonly IValidator<UpdateVyajPartyDto> _updatePartyValidator;
    private readonly IValidator<CreateVyajEntryDto> _createEntryValidator;
    private readonly IValidator<CreateVyajPaymentDto> _createPaymentValidator;

    public VyajController(
        IVyajService service,
        IValidator<CreateVyajPartyDto> createPartyValidator,
        IValidator<UpdateVyajPartyDto> updatePartyValidator,
        IValidator<CreateVyajEntryDto> createEntryValidator,
        IValidator<CreateVyajPaymentDto> createPaymentValidator)
    {
        _service = service;
        _createPartyValidator = createPartyValidator;
        _updatePartyValidator = updatePartyValidator;
        _createEntryValidator = createEntryValidator;
        _createPaymentValidator = createPaymentValidator;
    }

    [HttpGet("parties")]
    [RequirePermission(AppModules.Vyaj, PermissionLevel.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<VyajPartySummaryDto>>>> GetParties([FromQuery] Guid siteId, CancellationToken cancellationToken)
    {
        if (siteId == Guid.Empty)
            return BadRequest(ApiResponse<IReadOnlyList<VyajPartySummaryDto>>.Fail("siteId is required."));

        var result = await _service.GetPartiesAsync(siteId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<VyajPartySummaryDto>>.Ok(result));
    }

    [HttpGet("parties/{id:guid}")]
    [RequirePermission(AppModules.Vyaj, PermissionLevel.View)]
    public async Task<ActionResult<ApiResponse<VyajPartyDetailDto>>> GetPartyDetail(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.GetPartyDetailAsync(id, cancellationToken);
            return Ok(ApiResponse<VyajPartyDetailDto>.Ok(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<VyajPartyDetailDto>.Fail(ex.Message));
        }
    }

    [HttpPost("parties")]
    [RequirePermission(AppModules.Vyaj, PermissionLevel.Manage)]
    public async Task<ActionResult<ApiResponse<VyajPartySummaryDto>>> CreateParty([FromBody] CreateVyajPartyDto dto, CancellationToken cancellationToken)
    {
        var validation = await _createPartyValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<VyajPartySummaryDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));

        try
        {
            var result = await _service.CreatePartyAsync(dto, cancellationToken);
            return Ok(ApiResponse<VyajPartySummaryDto>.Ok(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<VyajPartySummaryDto>.Fail(ex.Message));
        }
    }

    [HttpPut("parties/{id:guid}")]
    [RequirePermission(AppModules.Vyaj, PermissionLevel.Manage)]
    public async Task<ActionResult<ApiResponse<VyajPartySummaryDto>>> UpdateParty(Guid id, [FromBody] UpdateVyajPartyDto dto, CancellationToken cancellationToken)
    {
        var validation = await _updatePartyValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<VyajPartySummaryDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));

        try
        {
            var result = await _service.UpdatePartyAsync(id, dto, cancellationToken);
            return Ok(ApiResponse<VyajPartySummaryDto>.Ok(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<VyajPartySummaryDto>.Fail(ex.Message));
        }
    }

    [HttpDelete("parties/{id:guid}")]
    [RequirePermission(AppModules.Vyaj, PermissionLevel.Manage)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteParty(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _service.DeletePartyAsync(id, cancellationToken);
            return Ok(ApiResponse<object>.Ok(new { deleted = true }));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPost("entries")]
    [RequirePermission(AppModules.Vyaj, PermissionLevel.Manage)]
    public async Task<ActionResult<ApiResponse<VyajEntryDto>>> CreateEntry([FromBody] CreateVyajEntryDto dto, CancellationToken cancellationToken)
    {
        var validation = await _createEntryValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<VyajEntryDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));

        try
        {
            var result = await _service.CreateEntryAsync(dto, cancellationToken);
            return Ok(ApiResponse<VyajEntryDto>.Ok(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<VyajEntryDto>.Fail(ex.Message));
        }
    }

    [HttpPatch("entries/{id:guid}/closed")]
    [RequirePermission(AppModules.Vyaj, PermissionLevel.Manage)]
    public async Task<ActionResult<ApiResponse<VyajEntryDto>>> ToggleEntryClosed(Guid id, [FromBody] ToggleVyajEntryClosedDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.ToggleEntryClosedAsync(id, dto, cancellationToken);
            return Ok(ApiResponse<VyajEntryDto>.Ok(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<VyajEntryDto>.Fail(ex.Message));
        }
    }

    [HttpDelete("entries/{id:guid}")]
    [RequirePermission(AppModules.Vyaj, PermissionLevel.Manage)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteEntry(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _service.DeleteEntryAsync(id, cancellationToken);
            return Ok(ApiResponse<object>.Ok(new { deleted = true }));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPost("payments")]
    [RequirePermission(AppModules.Vyaj, PermissionLevel.Manage)]
    public async Task<ActionResult<ApiResponse<VyajPaymentDto>>> CreatePayment([FromBody] CreateVyajPaymentDto dto, CancellationToken cancellationToken)
    {
        var validation = await _createPaymentValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<VyajPaymentDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));

        try
        {
            var result = await _service.CreatePaymentAsync(dto, cancellationToken);
            return Ok(ApiResponse<VyajPaymentDto>.Ok(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<VyajPaymentDto>.Fail(ex.Message));
        }
    }

    [HttpDelete("payments/{id:guid}")]
    [RequirePermission(AppModules.Vyaj, PermissionLevel.Manage)]
    public async Task<ActionResult<ApiResponse<object>>> DeletePayment(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _service.DeletePaymentAsync(id, cancellationToken);
            return Ok(ApiResponse<object>.Ok(new { deleted = true }));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
    }
}
