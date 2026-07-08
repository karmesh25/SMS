namespace ABR.Application.DTOs.Accounting;

public class JournalVoucherLineUpsertDto
{
    public Guid SubLedgerId { get; set; }
    public string EntryType { get; set; } = "dr";
    public decimal Amount { get; set; }
    public int LineNo { get; set; }
}

public class CreateJournalVoucherDto
{
    public Guid SiteId { get; set; }
    public DateOnly VoucherDate { get; set; }
    public string? Narration { get; set; }
    public IReadOnlyList<JournalVoucherLineUpsertDto> Lines { get; set; } = Array.Empty<JournalVoucherLineUpsertDto>();
}

public class UpdateJournalVoucherDto
{
    public DateOnly VoucherDate { get; set; }
    public string? Narration { get; set; }
    public IReadOnlyList<JournalVoucherLineUpsertDto> Lines { get; set; } = Array.Empty<JournalVoucherLineUpsertDto>();
}

public class JournalVoucherLineDto
{
    public Guid Id { get; set; }
    public Guid SubLedgerId { get; set; }
    public string EntryType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int LineNo { get; set; }
    public string SubLedgerName { get; set; } = string.Empty;
    public string MainLedgerName { get; set; } = string.Empty;
}

public class JournalVoucherDto
{
    public Guid Id { get; set; }
    public Guid SiteId { get; set; }
    public string VoucherNo { get; set; } = string.Empty;
    public DateOnly VoucherDate { get; set; }
    public string? Narration { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public IReadOnlyList<JournalVoucherLineDto> Lines { get; set; } = Array.Empty<JournalVoucherLineDto>();
}

public class JournalVoucherFilterDto
{
    public Guid SiteId { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class PagedJournalVouchersDto
{
    public IReadOnlyList<JournalVoucherDto> Items { get; set; } = Array.Empty<JournalVoucherDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class JournalVoucherLedgerExportRequestDto
{
    public Guid SiteId { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
}
