using ABR.Application.DTOs.Accounting;

namespace ABR.Application.Interfaces;

public interface IDailyEntryExcelService
{
    Task<DailyEntryExcelFileDto> GetSampleAsync(CancellationToken cancellationToken = default);
    Task<DailyEntryImportResultDto> ImportAsync(Guid siteId, Stream fileStream, Guid? userId, CancellationToken cancellationToken = default);
    Task<DailyEntryExcelFileDto> ExportLedgerExcelAsync(DailyEntryLedgerExportRequestDto request, CancellationToken cancellationToken = default);
}
