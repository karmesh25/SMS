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
        var parties = await PartyQuery()
            .Where(p => p.SiteId == siteId && !p.IsDeleted)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return parties.Select(MapPartySummary).ToList();
    }

    public async Task<VyajPartyDetailDto> GetPartyDetailAsync(Guid partyId, CancellationToken cancellationToken = default)
    {
        var party = await PartyQuery()
            .Where(p => p.Id == partyId && !p.IsDeleted)
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
            MainLedgerId = party.MainLedgerId,
            SubLedgerId = party.SubLedgerId,
            MainLedgerName = party.MainLedger?.LedgerName,
            SubLedgerName = party.SubLedger?.LedgerName,
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
        await EnsureLedgersValidAsync(dto.SiteId, dto.MainLedgerId, dto.SubLedgerId, cancellationToken);

        var party = new VyajParty
        {
            SiteId = dto.SiteId,
            Name = dto.Name.Trim(),
            Notes = dto.Notes?.Trim(),
            MainLedgerId = dto.MainLedgerId,
            SubLedgerId = dto.SubLedgerId
        };

        _context.VyajParties.Add(party);
        await _context.SaveChangesAsync(cancellationToken);

        return (await GetPartiesAsync(dto.SiteId, cancellationToken)).First(p => p.Id == party.Id);
    }

    public async Task<VyajPartySummaryDto> UpdatePartyAsync(Guid partyId, UpdateVyajPartyDto dto, CancellationToken cancellationToken = default)
    {
        var party = await _context.VyajParties
            .Include(p => p.Entries.Where(e => !e.IsDeleted))
            .ThenInclude(e => e.Payments.Where(pay => !pay.IsDeleted))
            .Include(p => p.MainLedger)
            .Include(p => p.SubLedger)
            .FirstOrDefaultAsync(p => p.Id == partyId && !p.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException("Vyaj party not found.");

        await EnsureLedgersValidAsync(party.SiteId, dto.MainLedgerId, dto.SubLedgerId, cancellationToken);

        party.Name = dto.Name.Trim();
        party.Notes = dto.Notes?.Trim();
        party.MainLedgerId = dto.MainLedgerId;
        party.SubLedgerId = dto.SubLedgerId;

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
        _ = await _context.VyajParties
            .FirstOrDefaultAsync(p => p.Id == dto.PartyId && !p.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException("Vyaj party not found.");

        var rateBasis = dto.RateBasis.ToLowerInvariant();
        var period = rateBasis == "month" ? dto.RatePeriodMonths : null;

        var entry = new VyajEntry
        {
            PartyId = dto.PartyId,
            Principal = dto.Principal,
            RatePercent = dto.RatePercent,
            RateBasis = rateBasis,
            RatePeriodMonths = period,
            EmiAmount = dto.EmiAmount,
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
        _ = await LoadEntryAsync(dto.EntryId, cancellationToken);

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

    private IQueryable<VyajParty> PartyQuery() =>
        _context.VyajParties
            .AsNoTracking()
            .Include(p => p.MainLedger)
            .Include(p => p.SubLedger)
            .Include(p => p.Entries.Where(e => !e.IsDeleted))
            .ThenInclude(e => e.Payments.Where(pay => !pay.IsDeleted));

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

    private async Task EnsureLedgersValidAsync(Guid siteId, Guid? mainLedgerId, Guid? subLedgerId, CancellationToken cancellationToken)
    {
        if (mainLedgerId.HasValue)
        {
            var mainOk = await _context.MainLedgers.AnyAsync(
                m => m.Id == mainLedgerId.Value && m.SiteId == siteId,
                cancellationToken);
            if (!mainOk)
                throw new InvalidOperationException("Main ledger not found for this site.");
        }

        if (subLedgerId.HasValue)
        {
            var subOk = await _context.SubLedgers.AnyAsync(
                s => s.Id == subLedgerId.Value && s.MainLedger.SiteId == siteId
                    && (!mainLedgerId.HasValue || s.MainLedgerId == mainLedgerId.Value),
                cancellationToken);
            if (!subOk)
                throw new InvalidOperationException("Sub ledger not found for this main ledger/site.");
        }
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
            MainLedgerId = party.MainLedgerId,
            SubLedgerId = party.SubLedgerId,
            MainLedgerName = party.MainLedger?.LedgerName,
            SubLedgerName = party.SubLedger?.LedgerName,
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
            RatePeriodMonths = entry.RatePeriodMonths,
            EmiAmount = entry.EmiAmount,
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
            payments,
            ratePeriodMonths: entry.RatePeriodMonths);
    }
}
