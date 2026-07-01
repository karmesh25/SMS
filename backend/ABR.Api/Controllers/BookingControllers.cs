using System.Security.Claims;
using ABR.Api.Authorization;
using ABR.Application.Common;
using ABR.Application.DTOs.Booking;
using ABR.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABR.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/bookings")]
public class BookingController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly IValidator<CreateBookingDto> _createValidator;
    private readonly IValidator<UpdateBookingDto> _updateValidator;
    private readonly IValidator<CancelBookingDto> _cancelValidator;

    public BookingController(
        IBookingService bookingService,
        IValidator<CreateBookingDto> createValidator,
        IValidator<UpdateBookingDto> updateValidator,
        IValidator<CancelBookingDto> cancelValidator)
    {
        _bookingService = bookingService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _cancelValidator = cancelValidator;
    }

    [HttpGet("{siteId:guid}")]
    public async Task<ActionResult<ApiResponse<PagedResultDto<BookingListDto>>>> GetBySite(
        Guid siteId,
        [FromQuery] BookingQueryDto query,
        CancellationToken cancellationToken)
    {
        var result = await _bookingService.GetBySiteAsync(siteId, query, cancellationToken);
        return Ok(ApiResponse<PagedResultDto<BookingListDto>>.Ok(result));
    }

    [HttpGet("detail/{id:guid}")]
    public async Task<ActionResult<ApiResponse<BookingDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var booking = await _bookingService.GetByIdAsync(id, cancellationToken);
        if (booking is null) return NotFound(ApiResponse<BookingDto>.Fail("Booking not found."));
        return Ok(ApiResponse<BookingDto>.Ok(booking));
    }

    [HttpGet("by-flat/{flatId:guid}")]
    public async Task<ActionResult<ApiResponse<BookingDto>>> GetByFlat(Guid flatId, CancellationToken cancellationToken)
    {
        var booking = await _bookingService.GetByFlatIdAsync(flatId, cancellationToken);
        if (booking is null) return NotFound(ApiResponse<BookingDto>.Fail("No active booking for this flat."));
        return Ok(ApiResponse<BookingDto>.Ok(booking));
    }

    [HttpGet("flat-detail/{flatId:guid}")]
    public async Task<ActionResult<ApiResponse<FlatDetailDto>>> GetFlatDetail(Guid flatId, CancellationToken cancellationToken)
    {
        var flat = await _bookingService.GetFlatDetailAsync(flatId, cancellationToken);
        if (flat is null) return NotFound(ApiResponse<FlatDetailDto>.Fail("Flat not found."));
        return Ok(ApiResponse<FlatDetailDto>.Ok(flat));
    }

    [RequirePermission(AppModules.Booking, PermissionLevel.Manage)]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<BookingDto>>> Create([FromBody] CreateBookingDto dto, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<BookingDto>.Fail("Validation failed.", validation.Errors.Select(e => e.ErrorMessage).ToList()));

        try
        {
            var booking = await _bookingService.CreateAsync(dto, GetUserId(), cancellationToken);
            return Ok(ApiResponse<BookingDto>.Ok(booking, "Booking created."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<BookingDto>.Fail(ex.Message));
        }
    }

    [RequirePermission(AppModules.Booking, PermissionLevel.Manage)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<BookingDto>>> Update(Guid id, [FromBody] UpdateBookingDto dto, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<BookingDto>.Fail("Validation failed.", validation.Errors.Select(e => e.ErrorMessage).ToList()));

        try
        {
            var booking = await _bookingService.UpdateAsync(id, dto, GetUserId(), cancellationToken);
            if (booking is null) return NotFound(ApiResponse<BookingDto>.Fail("Booking not found."));
            return Ok(ApiResponse<BookingDto>.Ok(booking, "Booking updated."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<BookingDto>.Fail(ex.Message));
        }
    }

    [RequirePermission(AppModules.Booking, PermissionLevel.Manage)]
    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<BookingDto>>> Cancel(Guid id, [FromBody] CancelBookingDto dto, CancellationToken cancellationToken)
    {
        var validation = await _cancelValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<BookingDto>.Fail("Validation failed.", validation.Errors.Select(e => e.ErrorMessage).ToList()));

        try
        {
            var booking = await _bookingService.CancelAsync(id, dto, GetUserId(), cancellationToken);
            if (booking is null) return NotFound(ApiResponse<BookingDto>.Fail("Booking not found."));
            return Ok(ApiResponse<BookingDto>.Ok(booking, "Booking cancelled."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<BookingDto>.Fail(ex.Message));
        }
    }

    [HttpGet("dastavej/{siteId:guid}")]
    [RequirePermission(AppModules.Dastavej, PermissionLevel.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DastavejBookingListDto>>>> GetDastavejList(Guid siteId, CancellationToken cancellationToken)
    {
        var list = await _bookingService.GetDastavejListAsync(siteId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<DastavejBookingListDto>>.Ok(list));
    }

    [RequirePermission(AppModules.Dastavej, PermissionLevel.Manage)]
    [HttpPut("{id:guid}/dastavej-satakhat")]
    public async Task<ActionResult<ApiResponse<BookingDto>>> UpdateDastavej(Guid id, [FromBody] UpdateDastavejDto dto, CancellationToken cancellationToken)
    {
        var booking = await _bookingService.UpdateDastavejAsync(id, dto, GetUserId(), cancellationToken);
        if (booking is null) return NotFound(ApiResponse<BookingDto>.Fail("Booking not found."));
        return Ok(ApiResponse<BookingDto>.Ok(booking, "Dastavej details updated."));
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}

[ApiController]
[Authorize]
[Route("api/installments")]
public class InstallmentController : ControllerBase
{
    private readonly IInstallmentService _installmentService;
    private readonly IValidator<RecordPaymentDto> _validator;

    public InstallmentController(IInstallmentService installmentService, IValidator<RecordPaymentDto> validator)
    {
        _installmentService = installmentService;
        _validator = validator;
    }

    [HttpGet("{bookingId:guid}")]
    public async Task<ActionResult<ApiResponse<InstallmentSummaryDto>>> GetByBooking(Guid bookingId, CancellationToken cancellationToken)
    {
        try
        {
            var summary = await _installmentService.GetByBookingAsync(bookingId, cancellationToken);
            return Ok(ApiResponse<InstallmentSummaryDto>.Ok(summary));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<InstallmentSummaryDto>.Fail("Booking not found."));
        }
    }

    [RequirePermission(AppModules.Booking, PermissionLevel.Manage)]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<InstallmentDto>>> RecordPayment([FromBody] RecordPaymentDto dto, CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<InstallmentDto>.Fail("Validation failed.", validation.Errors.Select(e => e.ErrorMessage).ToList()));

        try
        {
            var result = await _installmentService.RecordPaymentAsync(dto, GetUserId(), cancellationToken);
            return Ok(ApiResponse<InstallmentDto>.Ok(result, "Payment recorded."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<InstallmentDto>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<InstallmentDto>.Fail(ex.Message));
        }
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
