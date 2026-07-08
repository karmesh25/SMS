using ABR.Application.DTOs.Accounting;

namespace ABR.Application.Interfaces;

public interface IJournalVoucherService
{
    Task<PagedJournalVouchersDto> GetListAsync(JournalVoucherFilterDto filter, CancellationToken cancellationToken = default);
    Task<JournalVoucherDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<JournalVoucherDto> CreateAsync(CreateJournalVoucherDto dto, Guid? userId, CancellationToken cancellationToken = default);
    Task<JournalVoucherDto?> UpdateAsync(Guid id, UpdateJournalVoucherDto dto, Guid? userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, Guid? userId, CancellationToken cancellationToken = default);
    Task<DailyEntryExcelFileDto> ExportLedgerExcelAsync(JournalVoucherLedgerExportRequestDto request, CancellationToken cancellationToken = default);
    Task<DailyEntryExcelFileDto> ExportLedgerPdfAsync(JournalVoucherLedgerExportRequestDto request, CancellationToken cancellationToken = default);
}
