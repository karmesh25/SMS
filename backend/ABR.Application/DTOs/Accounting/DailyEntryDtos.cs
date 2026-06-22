namespace ABR.Application.DTOs.Accounting;

public class CreateDailyEntryDto
{
    public Guid SiteId { get; set; }
    public string EntryType { get; set; } = "aavak";
    public DateOnly EntryDate { get; set; }
    public Guid MainLedgerId { get; set; }
    public Guid SubLedgerId { get; set; }
    public decimal Amount { get; set; }
    public string CashBank { get; set; } = "Cash";
    public string? Description { get; set; }
}

public class UpdateDailyEntryDto
{
    public string EntryType { get; set; } = "aavak";
    public DateOnly EntryDate { get; set; }
    public Guid MainLedgerId { get; set; }
    public Guid SubLedgerId { get; set; }
    public decimal Amount { get; set; }
    public string CashBank { get; set; } = "Cash";
    public string? Description { get; set; }
}

public class DailyEntryDto
{
    public Guid Id { get; set; }
    public Guid SiteId { get; set; }
    public string EntryType { get; set; } = string.Empty;
    public DateOnly EntryDate { get; set; }
    public Guid MainLedgerId { get; set; }
    public string MainLedgerName { get; set; } = string.Empty;
    public Guid SubLedgerId { get; set; }
    public string SubLedgerName { get; set; } = string.Empty;
    public string? FlatNo { get; set; }
    public decimal Amount { get; set; }
    public string CashBank { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class DailyEntryFilterDto
{
    public Guid SiteId { get; set; }
    public string? EntryType { get; set; }
    public Guid? MainLedgerId { get; set; }
    public Guid? SubLedgerId { get; set; }
    public string? FlatNo { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class ProfitSummaryDto
{
    public decimal TotalAavak { get; set; }
    public decimal TotalJavak { get; set; }
    public decimal Profit { get; set; }
}

public class BankBalanceDto
{
    public Guid BankAccountId { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string AccountNo { get; set; } = string.Empty;
    public string CashBankLabel { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public decimal Balance { get; set; }
}

public class BalanceSummaryDto
{
    public decimal CashBalance { get; set; }
    public IReadOnlyList<BankBalanceDto> BankBalances { get; set; } = Array.Empty<BankBalanceDto>();
}

public class PagedDailyEntriesDto
{
    public IReadOnlyList<DailyEntryDto> Items { get; set; } = Array.Empty<DailyEntryDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
