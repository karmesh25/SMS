using ABR.Application.DTOs.Vyaj;

namespace ABR.Application.Interfaces;

public interface IVyajService
{
    Task<IReadOnlyList<VyajPartySummaryDto>> GetPartiesAsync(Guid siteId, CancellationToken cancellationToken = default);
    Task<VyajPartyDetailDto> GetPartyDetailAsync(Guid partyId, CancellationToken cancellationToken = default);
    Task<VyajPartySummaryDto> CreatePartyAsync(CreateVyajPartyDto dto, CancellationToken cancellationToken = default);
    Task<VyajPartySummaryDto> UpdatePartyAsync(Guid partyId, UpdateVyajPartyDto dto, CancellationToken cancellationToken = default);
    Task DeletePartyAsync(Guid partyId, CancellationToken cancellationToken = default);
    Task<VyajEntryDto> CreateEntryAsync(CreateVyajEntryDto dto, CancellationToken cancellationToken = default);
    Task<VyajEntryDto> ToggleEntryClosedAsync(Guid entryId, ToggleVyajEntryClosedDto dto, CancellationToken cancellationToken = default);
    Task DeleteEntryAsync(Guid entryId, CancellationToken cancellationToken = default);
    Task<VyajPaymentDto> CreatePaymentAsync(CreateVyajPaymentDto dto, CancellationToken cancellationToken = default);
    Task DeletePaymentAsync(Guid paymentId, CancellationToken cancellationToken = default);
}
