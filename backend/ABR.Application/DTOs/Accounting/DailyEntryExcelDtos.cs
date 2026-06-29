namespace ABR.Application.DTOs.Accounting;

public class DailyEntryImportErrorDto
{
    public int RowNumber { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class DailyEntryImportResultDto
{
    public int ImportedCount { get; set; }
    public int FailedCount { get; set; }
    public IReadOnlyList<DailyEntryImportErrorDto> Errors { get; set; } = Array.Empty<DailyEntryImportErrorDto>();
}

public class DailyEntryLedgerExportRequestDto
{
    public Guid SiteId { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
}

public class DailyEntryExcelFileDto
{
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
}
