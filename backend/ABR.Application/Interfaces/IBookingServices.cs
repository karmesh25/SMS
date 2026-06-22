using ABR.Application.DTOs.Booking;

namespace ABR.Application.Interfaces;

public interface IBookingService
{
    Task<PagedResultDto<BookingListDto>> GetBySiteAsync(Guid siteId, BookingQueryDto query, CancellationToken cancellationToken = default);
    Task<BookingDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BookingDto?> GetByFlatIdAsync(Guid flatId, CancellationToken cancellationToken = default);
    Task<FlatDetailDto?> GetFlatDetailAsync(Guid flatId, CancellationToken cancellationToken = default);
    Task<BookingDto> CreateAsync(CreateBookingDto dto, Guid? userId, CancellationToken cancellationToken = default);
    Task<BookingDto?> UpdateAsync(Guid id, UpdateBookingDto dto, Guid? userId, CancellationToken cancellationToken = default);
    Task<BookingDto?> CancelAsync(Guid id, CancelBookingDto dto, Guid? userId, CancellationToken cancellationToken = default);
    Task<BookingDto?> UpdateDastavejAsync(Guid id, UpdateDastavejDto dto, Guid? userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DastavejBookingListDto>> GetDastavejListAsync(Guid siteId, CancellationToken cancellationToken = default);
}

public interface IInstallmentService
{
    Task<InstallmentSummaryDto> GetByBookingAsync(Guid bookingId, CancellationToken cancellationToken = default);
    Task<InstallmentDto> RecordPaymentAsync(RecordPaymentDto dto, Guid? userId, CancellationToken cancellationToken = default);
}

public interface IAuditLogService
{
    Task LogAsync(Guid? userId, string action, string tableName, Guid recordId, string? oldValues = null, string? newValues = null, CancellationToken cancellationToken = default);
}
