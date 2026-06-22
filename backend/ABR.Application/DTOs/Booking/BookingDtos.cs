namespace ABR.Application.DTOs.Booking;

public class BookingDto
{
    public Guid Id { get; set; }
    public Guid FlatId { get; set; }
    public string FlatNo { get; set; } = string.Empty;
    public string WingName { get; set; } = string.Empty;
    public Guid SiteId { get; set; }
    public Guid MemberSubLedgerId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public Guid? BrokerId { get; set; }
    public string? BrokerName { get; set; }
    public Guid ConditionId { get; set; }
    public string ConditionName { get; set; } = string.Empty;
    public DateOnly BookingDate { get; set; }
    public string? CustomerContact { get; set; }
    public decimal Sqft { get; set; }
    public decimal Rate { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal BrokeragePct { get; set; }
    public decimal BrokerageAmount { get; set; }
    public string CustomerType { get; set; } = string.Empty;
    public bool IsArjaMarjaSell { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateOnly? CancelDate { get; set; }
    public DateOnly? DastavejDate { get; set; }
    public DateOnly? SatakhatDate { get; set; }
    public string? DocumentNumber { get; set; }
    public decimal? ServiceTax { get; set; }
    public string? Notes { get; set; }
}

public class BookingListDto
{
    public Guid Id { get; set; }
    public string FlatNo { get; set; } = string.Empty;
    public string WingName { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public DateOnly BookingDate { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CreateBookingDto
{
    public Guid FlatId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public Guid ConditionId { get; set; }
    public Guid? BrokerId { get; set; }
    public DateOnly BookingDate { get; set; }
    public string? CustomerContact { get; set; }
    public decimal Sqft { get; set; }
    public decimal Rate { get; set; }
    public decimal BrokeragePct { get; set; }
    public string CustomerType { get; set; } = "real";
    public bool IsArjaMarjaSell { get; set; }
    public string? Notes { get; set; }
}

public class UpdateBookingDto
{
    public string MemberName { get; set; } = string.Empty;
    public Guid ConditionId { get; set; }
    public Guid? BrokerId { get; set; }
    public DateOnly BookingDate { get; set; }
    public string? CustomerContact { get; set; }
    public decimal Sqft { get; set; }
    public decimal Rate { get; set; }
    public decimal BrokeragePct { get; set; }
    public string CustomerType { get; set; } = "real";
    public bool IsArjaMarjaSell { get; set; }
    public string? Notes { get; set; }
}

public class CancelBookingDto
{
    public DateOnly CancelDate { get; set; }
    public string? Notes { get; set; }
}

public class BookingQueryDto
{
    public Guid? WingId { get; set; }
    public string? Status { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class PagedResultDto<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class InstallmentDto
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public string MilestoneName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public decimal DueAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly? PaidDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class InstallmentSummaryDto
{
    public Guid BookingId { get; set; }
    public decimal TotalDue { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalRemaining { get; set; }
    public decimal PercentagePaid { get; set; }
    public int? DaysSinceLastPayment { get; set; }
    public int DaysFromBooking { get; set; }
    public IReadOnlyList<InstallmentDto> Milestones { get; set; } = Array.Empty<InstallmentDto>();
}

public class RecordPaymentDto
{
    public Guid BookingInstallmentId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly PaidDate { get; set; }
    public string? Notes { get; set; }
}

public class FlatDetailDto
{
    public Guid Id { get; set; }
    public string FlatNo { get; set; } = string.Empty;
    public decimal Sqft { get; set; }
    public string Status { get; set; } = string.Empty;
    public string WingName { get; set; } = string.Empty;
}

public class UpdateDastavejDto
{
    public DateOnly? DastavejDate { get; set; }
    public DateOnly? SatakhatDate { get; set; }
    public string? DocumentNumber { get; set; }
    public decimal? ServiceTax { get; set; }
    public string? Notes { get; set; }
}

public class DastavejBookingListDto
{
    public Guid Id { get; set; }
    public string FlatNo { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public DateOnly BookingDate { get; set; }
    public DateOnly? DastavejDate { get; set; }
    public DateOnly? SatakhatDate { get; set; }
    public string? DocumentNumber { get; set; }
    public decimal? ServiceTax { get; set; }
    public string Status { get; set; } = string.Empty;
}
