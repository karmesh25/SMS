namespace ABR.Application.DTOs.Reports;

public class PagedReportDto<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class AllEntryReportFilterDto
{
    public Guid SiteId { get; set; }
    public Guid? MainLedgerId { get; set; }
    public Guid? SubLedgerId { get; set; }
    public string? FlatNo { get; set; }
    public DateOnly DateFrom { get; set; }
    public DateOnly DateTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class AllEntryReportRowDto
{
    public DateOnly Date { get; set; }
    public string? AavakLedger { get; set; }
    public string? AavakSubLedger { get; set; }
    public string? AavakFlatNo { get; set; }
    public string? AavakCashBank { get; set; }
    public decimal? AavakAmount { get; set; }
    public string? AavakDescription { get; set; }
    public string? JavakLedger { get; set; }
    public string? JavakSubLedger { get; set; }
    public string? JavakCashBank { get; set; }
    public decimal? JavakAmount { get; set; }
    public string? JavakDescription { get; set; }
}

public class BalanceSheetFilterDto
{
    public Guid SiteId { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public Guid? MainLedgerId { get; set; }
}

public class BalanceSheetLedgerItemDto
{
    public string LedgerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
}

public class BalanceSheetReportDto
{
    public string SiteName { get; set; } = string.Empty;
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public IReadOnlyList<BalanceSheetLedgerItemDto> AavakItems { get; set; } = Array.Empty<BalanceSheetLedgerItemDto>();
    public decimal TotalAavak { get; set; }
    public IReadOnlyList<BalanceSheetLedgerItemDto> JavakItems { get; set; } = Array.Empty<BalanceSheetLedgerItemDto>();
    public decimal TotalJavak { get; set; }
    public decimal Profit { get; set; }
}

public class TillDateReportFilterDto
{
    public Guid SiteId { get; set; }
    public int? DaysFromLastPayment { get; set; }
    public DateOnly? AsOfDate { get; set; }
    public string MovementType { get; set; } = "all";
    public bool ExtraReturnOnly { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class TillDateReportRowDto
{
    public string SiteName { get; set; } = string.Empty;
    public string WingName { get; set; } = string.Empty;
    public string FlatNo { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public string? CustomerContact { get; set; }
    public string? BrokerName { get; set; }
    public string? BrokerContact { get; set; }
    public DateOnly BookingDate { get; set; }
    public decimal Sqft { get; set; }
    public decimal Rate { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal RemainingAsPerCondition { get; set; }
    public decimal TotalRemaining { get; set; }
    public DateOnly? LastPaymentDate { get; set; }
    public int? DaysFromLastPayment { get; set; }
    public int DaysFromBooking { get; set; }
    public decimal PercentagePaid { get; set; }
    public DateOnly? DastavejDate { get; set; }
    public decimal? ServiceTax { get; set; }
}

public class MonthwiseReportFilterDto
{
    public Guid SiteId { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
}

public class MonthwiseReportRowDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthLabel { get; set; } = string.Empty;
    public decimal AavakTotal { get; set; }
    public decimal JavakTotal { get; set; }
    public decimal Net { get; set; }
}

public class BankStatementFilterDto
{
    public Guid SiteId { get; set; }
    public Guid BankAccountId { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
}

public class BankStatementRowDto
{
    public DateOnly EntryDate { get; set; }
    public string? Description { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
    public string EntryType { get; set; } = string.Empty;
}

public class BankStatementReportDto
{
    public string BankName { get; set; } = string.Empty;
    public string AccountNo { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public IReadOnlyList<BankStatementRowDto> Rows { get; set; } = Array.Empty<BankStatementRowDto>();
    public decimal ClosingBalance { get; set; }
}

public class SellDetailsFilterDto
{
    public Guid SiteId { get; set; }
    public Guid? WingId { get; set; }
    public string? Status { get; set; }
}

public class SellDetailsRowDto
{
    public string FlatNo { get; set; } = string.Empty;
    public string WingName { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public DateOnly BookingDate { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal Paid { get; set; }
    public decimal Remaining { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class SellDetailsReportDto
{
    public IReadOnlyList<SellDetailsRowDto> Items { get; set; } = Array.Empty<SellDetailsRowDto>();
    public decimal TotalPrice { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalRemaining { get; set; }
}

public class InstallmentReportFilterDto
{
    public Guid SiteId { get; set; }
    public Guid? BookingId { get; set; }
    public string? FlatNo { get; set; }
}

public class InstallmentReportRowDto
{
    public string FlatNo { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public string MilestoneName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public decimal DueAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Remaining { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly? PaidDate { get; set; }
    public string Status { get; set; } = string.Empty;
}
