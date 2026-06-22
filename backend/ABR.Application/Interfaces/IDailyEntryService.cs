using ABR.Application.DTOs.Accounting;

namespace ABR.Application.Interfaces;

public interface IDailyEntryService
{
    Task<PagedDailyEntriesDto> GetListAsync(DailyEntryFilterDto filter, CancellationToken cancellationToken = default);
    Task<DailyEntryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DailyEntryDto> CreateAsync(CreateDailyEntryDto dto, Guid? userId, CancellationToken cancellationToken = default);
    Task<DailyEntryDto?> UpdateAsync(Guid id, UpdateDailyEntryDto dto, Guid? userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, Guid? userId, CancellationToken cancellationToken = default);
    Task<ProfitSummaryDto> GetProfitAsync(Guid siteId, CancellationToken cancellationToken = default);
    Task<BalanceSummaryDto> GetBalanceAsync(Guid siteId, CancellationToken cancellationToken = default);
    Task CreateFromInstallmentAsync(Guid siteId, Guid mainLedgerId, Guid subLedgerId, decimal amount, DateOnly entryDate, string milestoneName, Guid? userId, CancellationToken cancellationToken = default);
}
