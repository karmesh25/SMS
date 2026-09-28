using ABR.Application.DTOs.Accounting;
using ABR.Application.DTOs.Vyaj;

namespace ABR.Application.Interfaces;

public interface IVyajService
{
    Task<IReadOnlyList<VyajPartySummaryDto>> GetPartiesAsync(Guid siteId, CancellationToken cancellationToken = default);
    Task<VyajPartyDetailDto> GetPartyDetailAsync(Guid partyId, CancellationToken cancellationToken = default);
    Task<VyajPartySummaryDto> CreatePartyAsync(CreateVyajPartyDto dto, CancellationToken cancellationToken = default);
    Task<VyajPartySummaryDto> UpdatePartyAsync(Guid partyId, UpdateVyajPartyDto dto, Guid? actorUserId = null, CancellationToken cancellationToken = default);
    Task DeletePartyAsync(Guid partyId, Guid? actorUserId = null, CancellationToken cancellationToken = default);
    Task<VyajEntryDto> CreateEntryAsync(CreateVyajEntryDto dto, CancellationToken cancellationToken = default);
    Task<VyajEntryDto> ToggleEntryClosedAsync(Guid entryId, ToggleVyajEntryClosedDto dto, Guid? actorUserId = null, CancellationToken cancellationToken = default);
    Task DeleteEntryAsync(Guid entryId, Guid? actorUserId = null, CancellationToken cancellationToken = default);
    Task<VyajPaymentDto> CreatePaymentAsync(CreateVyajPaymentDto dto, Guid? actorUserId = null, CancellationToken cancellationToken = default);
    Task DeletePaymentAsync(Guid paymentId, Guid? actorUserId = null, CancellationToken cancellationToken = default);
    /// <summary>Capitalizes unpaid completed months into principal. Returns number of capitalize payments created.</summary>
    Task<int> CapitalizeUnpaidMonthsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VyajRateBasisOptionDto>> GetRateOptionsAsync(Guid siteId, CancellationToken cancellationToken = default);
    Task<VyajRateBasisOptionDto> CreateRateOptionAsync(CreateVyajRateBasisOptionDto dto, CancellationToken cancellationToken = default);
    Task<VyajRateBasisOptionDto> UpdateRateOptionAsync(Guid id, UpdateVyajRateBasisOptionDto dto, CancellationToken cancellationToken = default);
    Task DeleteRateOptionAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DailyEntryExcelFileDto> ExportExcelAsync(Guid siteId, CancellationToken cancellationToken = default);
    Task<DailyEntryExcelFileDto> ExportPdfAsync(Guid siteId, CancellationToken cancellationToken = default);
}
