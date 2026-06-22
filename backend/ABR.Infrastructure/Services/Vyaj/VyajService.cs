using ABR.Application.DTOs.Vyaj;
using ABR.Application.Interfaces;
using ABR.Application.Services.Vyaj;
using ABR.Domain.Entities;
using ABR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ABR.Infrastructure.Services.Vyaj;

public sealed class VyajService : IVyajService
{
    private readonly AbrDbContext _context;

    public VyajService(AbrDbContext context) => _context = context;

    public async Task<IReadOnlyList<VyajPartySummaryDto>> GetPartiesAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        var parties = await _context.VyajParties
            .AsNoTracking()
            .Where(p => p.SiteId == siteId && !p.IsDeleted)
            .Include(p => p.Entries.Where(e => !e.IsDeleted))
            .ThenInclude(e => e.Payments.Where(pay => !pay.IsDeleted))
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return parties.Select(MapPartySummary).ToList();
    }

    public async Task<VyajPartyDetailDto> GetPartyDetailAsync(Guid partyId, CancellationToken cancellationToken = default)
    {
        var party = await _context.VyajParties
            .AsNoTracking()
            .Where(p => p.Id == partyId && !p.IsDeleted)
            .Include(p => p.Entries.Where(e => !e.IsDeleted))
            .ThenInclude(e => e.Payments.Where(pay => !pay.IsDeleted))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Vyaj party not found.");

        var entries = party.Entries
            .OrderByDescending(e => e.StartDate)
            .ThenByDescending(e => e.CreatedAt)
            .Select(MapEntry)
            .ToList();

        var openEntries = entries.Where(e => !e.IsClosed).ToList();

        return new VyajPartyDetailDto
        {
            Id = party.Id,
            SiteId = party.SiteId,
            Name = party.Name,
            Notes = party.Notes,
            TotalVyajDue = openEntries.Sum(e => e.VyajDue),
            TotalGrossVyaj = openEntries.Sum(e => e.GrossVyaj),
            TotalVyajPaid = openEntries.Sum(e => e.InterestPaid),
            TotalPrincipalDue = openEntries.Sum(e => e.PrincipalDue),
            Entries = entries
        };
    }

    public async Task<VyajPartySummaryDto> CreatePartyAsync(CreateVyajPartyDto dto, CancellationToken cancellationToken = default)
    {
        await EnsureSiteExistsAsync(dto.SiteId, cancellationToken);

        var party = new VyajParty
        {
            SiteId = dto.SiteId,
            Name = dto.Name.Trim(),
            Notes = dto.Notes?.Trim()
        };

        _context.VyajParties.Add(party);
        await _context.SaveChangesAsync(cancellationToken);

        return MapPartySummary(party);
    }

    public async Task<VyajPartySummaryDto> UpdatePartyAsync(Guid partyId, UpdateVyajPartyDto dto, CancellationToken cancellationToken = default)
    {
        var party = await _context.VyajParties
            .Include(p => p.Entries.Where(e => !e.IsDeleted))
            .ThenInclude(e => e.Payments.Where(pay => !pay.IsDeleted))
            .FirstOrDefaultAsync(p => p.Id == partyId && !p.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException("Vyaj party not found.");

        party.Name = dto.Name.Trim();
        party.Notes = dto.Notes?.Trim();

        await _context.SaveChangesAsync(cancellationToken);
        return MapPartySummary(party);
    }

    public async Task DeletePartyAsync(Guid partyId, CancellationToken cancellationToken = default)
    {
        var party = await _context.VyajParties
            .Include(p => p.Entries)
            .ThenInclude(e => e.Payments)
            .FirstOrDefaultAsync(p => p.Id == partyId && !p.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException("Vyaj party not found.");

        SoftDeleteParty(party);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<VyajEntryDto> CreateEntryAsync(CreateVyajEntryDto dto, CancellationToken cancellationToken = default)
    {
        var party = await _context.VyajParties
            .FirstOrDefaultAsync(p => p.Id == dto.PartyId && !p.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException("Vyaj party not found.");

        var entry = new VyajEntry
        {
            PartyId = dto.PartyId,
            Principal = dto.Principal,
            RatePercent = dto.RatePercent,
            RateBasis = dto.RateBasis.ToLowerInvariant(),
            StartDate = dto.StartDate,
            IsClosed = false
        };

        _context.VyajEntries.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);

        return MapEntry(entry);
    }

    public async Task<VyajEntryDto> ToggleEntryClosedAsync(Guid entryId, ToggleVyajEntryClosedDto dto, CancellationToken cancellationToken = default)
    {
        var entry = await LoadEntryAsync(entryId, cancellationToken);
        entry.IsClosed = dto.IsClosed;
        await _context.SaveChangesAsync(cancellationToken);
        return MapEntry(entry);
    }

    public async Task DeleteEntryAsync(Guid entryId, CancellationToken cancellationToken = default)
    {
        var entry = await LoadEntryAsync(entryId, cancellationToken);
        SoftDeleteEntry(entry);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<VyajPaymentDto> CreatePaymentAsync(CreateVyajPaymentDto dto, CancellationToken cancellationToken = default)
    {
        var entry = await LoadEntryAsync(dto.EntryId, cancellationToken);

        var payment = new VyajPayment
        {
            EntryId = dto.EntryId,
            PaymentDate = dto.PaymentDate,
            Amount = dto.Amount,
            PaymentType = dto.PaymentType.ToLowerInvariant()
        };

        _context.VyajPayments.Add(payment);
        await _context.SaveChangesAsync(cancellationToken);

        return MapPayment(payment);
    }

    public async Task DeletePaymentAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        var payment = await _context.VyajPayments
            .FirstOrDefaultAsync(p => p.Id == paymentId && !p.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException("Vyaj payment not found.");

        payment.IsDeleted = true;
        payment.DeletedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<VyajEntry> LoadEntryAsync(Guid entryId, CancellationToken cancellationToken)
    {
        return await _context.VyajEntries
            .Include(e => e.Payments.Where(p => !p.IsDeleted))
            .FirstOrDefaultAsync(e => e.Id == entryId && !e.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException("Vyaj entry not found.");
    }

    private async Task EnsureSiteExistsAsync(Guid siteId, CancellationToken cancellationToken)
    {
        var exists = await _context.Sites.AnyAsync(s => s.Id == siteId && s.IsActive, cancellationToken);
        if (!exists)
            throw new KeyNotFoundException("Site not found.");
    }

    private static void SoftDeleteParty(VyajParty party)
    {
        party.IsDeleted = true;
        party.DeletedAt = DateTimeOffset.UtcNow;

        foreach (var entry in party.Entries.Where(e => !e.IsDeleted))
            SoftDeleteEntry(entry);
    }

    private static void SoftDeleteEntry(VyajEntry entry)
    {
        entry.IsDeleted = true;
        entry.DeletedAt = DateTimeOffset.UtcNow;

        foreach (var payment in entry.Payments.Where(p => !p.IsDeleted))
        {
            payment.IsDeleted = true;
            payment.DeletedAt = DateTimeOffset.UtcNow;
        }
    }

    private static VyajPartySummaryDto MapPartySummary(VyajParty party)
    {
        var openEntries = party.Entries.Where(e => !e.IsDeleted && !e.IsClosed).ToList();
        decimal vyajDue = 0;
        decimal principalDue = 0;

        foreach (var entry in openEntries)
        {
            var totals = ComputeTotals(entry);
            vyajDue += totals.VyajDue;
            principalDue += totals.PrincipalDue;
        }

        return new VyajPartySummaryDto
        {
            Id = party.Id,
            SiteId = party.SiteId,
            Name = party.Name,
            Notes = party.Notes,
            VyajDue = vyajDue,
            PrincipalDue = principalDue,
            OpenEntryCount = openEntries.Count
        };
    }

    private static VyajEntryDto MapEntry(VyajEntry entry)
    {
        var totals = ComputeTotals(entry);
        var payments = entry.Payments
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.CreatedAt)
            .Select(MapPayment)
            .ToList();

        return new VyajEntryDto
        {
            Id = entry.Id,
            PartyId = entry.PartyId,
            Principal = entry.Principal,
            RatePercent = entry.RatePercent,
            RateBasis = entry.RateBasis,
            StartDate = entry.StartDate,
            IsClosed = entry.IsClosed,
            GrossVyaj = totals.GrossVyaj,
            InterestPaid = totals.InterestPaid,
            PrincipalPaid = totals.PrincipalPaid,
            VyajDue = totals.VyajDue,
            PrincipalDue = totals.PrincipalDue,
            Payments = payments
        };
    }

    private static VyajPaymentDto MapPayment(VyajPayment payment) => new()
    {
        Id = payment.Id,
        EntryId = payment.EntryId,
        PaymentDate = payment.PaymentDate,
        Amount = payment.Amount,
        PaymentType = payment.PaymentType
    };

    private static VyajEntryTotals ComputeTotals(VyajEntry entry)
    {
        var payments = entry.Payments
            .Where(p => !p.IsDeleted)
            .Select(p => (p.Amount, p.PaymentType));

        return VyajCalculationService.CalculateEntryTotals(
            entry.Principal,
            entry.RatePercent,
            entry.RateBasis,
            entry.StartDate,
            payments);
    }
}
