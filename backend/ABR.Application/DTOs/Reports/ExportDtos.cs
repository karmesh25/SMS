namespace ABR.Application.DTOs.Reports;

public class ReportExportRequestDto
{
    public string ReportType { get; set; } = string.Empty;
    public Guid SiteId { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public Guid? MainLedgerId { get; set; }
    public Guid? SubLedgerId { get; set; }
    public string? FlatNo { get; set; }
    public int? DaysFromLastPayment { get; set; }
    public DateOnly? AsOfDate { get; set; }
    public string MovementType { get; set; } = "all";
    public bool ExtraReturnOnly { get; set; }
    public Guid? BankAccountId { get; set; }
    public Guid? WingId { get; set; }
    public string? Status { get; set; }
    public Guid? BookingId { get; set; }
}

public class ReportExportResultDto
{
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}
