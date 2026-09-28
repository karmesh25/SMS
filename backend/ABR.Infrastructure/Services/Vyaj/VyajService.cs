using System.Text.Json;
using ABR.Application.DTOs.Accounting;
using ABR.Application.DTOs.Notifications;
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
    private readonly INotificationService _notifications;

    public VyajService(AbrDbContext context, INotificationService notifications)
    {
        _context = context;
        _notifications = notifications;
    }

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
            PartyKind = NormalizePartyKind(party.PartyKind),
            MainLedgerId = party.MainLedgerId,
            SubLedgerId = party.SubLedgerId,
            BookingId = party.BookingId,
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

        var kind = NormalizePartyKind(dto.PartyKind);

        var party = new VyajParty
        {
            SiteId = dto.SiteId,
            Name = dto.Name.Trim(),
            Notes = dto.Notes?.Trim(),
            PartyKind = kind,
            MainLedgerId = dto.MainLedgerId,
            SubLedgerId = dto.SubLedgerId,
            BookingId = null
        };

        _context.VyajParties.Add(party);
        await _context.SaveChangesAsync(cancellationToken);

        return (await GetPartiesAsync(dto.SiteId, cancellationToken)).First(p => p.Id == party.Id);
    }

    public async Task<VyajPartySummaryDto> UpdatePartyAsync(Guid partyId, UpdateVyajPartyDto dto, Guid? actorUserId = null, CancellationToken cancellationToken = default)
    {
        var party = await _context.VyajParties
            .Include(p => p.Entries.Where(e => !e.IsDeleted))
            .ThenInclude(e => e.Payments.Where(pay => !pay.IsDeleted))
            .Include(p => p.MainLedger)
            .Include(p => p.SubLedger)
            .FirstOrDefaultAsync(p => p.Id == partyId && !p.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException("Vyaj party not found.");

        await EnsureLedgersValidAsync(party.SiteId, dto.MainLedgerId, dto.SubLedgerId, cancellationToken);

        var oldValues = JsonSerializer.Serialize(new { party.Name, party.Notes, party.PartyKind, party.MainLedgerId, party.SubLedgerId });
        var hasOldEntry = party.Entries.Any(e => !e.IsDeleted && _notifications.IsOlderThanSixMonths(e.StartDate));

        party.Name = dto.Name.Trim();
        party.Notes = dto.Notes?.Trim();
        party.PartyKind = NormalizePartyKind(dto.PartyKind);
        party.MainLedgerId = dto.MainLedgerId;
        party.SubLedgerId = dto.SubLedgerId;
        party.BookingId = null;

        await _context.SaveChangesAsync(cancellationToken);

        if (hasOldEntry)
        {
            await _notifications.NotifyOldRecordChangeAsync(new CreateChangeNotificationRequest
            {
                SiteId = party.SiteId,
                ActorUserId = actorUserId,
                Module = "vyaj",
                EntityType = "vyaj_party",
                EntityId = party.Id,
                Title = "Old vyaj party updated",
                Message = $"Party \"{party.Name}\" with older entries was updated.",
                OldValues = oldValues,
                NewValues = JsonSerializer.Serialize(dto)
            }, cancellationToken);
        }

        return MapPartySummary(party);
    }

    public async Task DeletePartyAsync(Guid partyId, Guid? actorUserId = null, CancellationToken cancellationToken = default)
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
            .Include(p => p.Entries.Where(e => !e.IsDeleted))
            .ThenInclude(e => e.Payments.Where(pay => !pay.IsDeleted))
            .FirstOrDefaultAsync(p => p.Id == dto.PartyId && !p.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException("Vyaj party not found.");

        var openEntries = party.Entries.Where(e => !e.IsDeleted && !e.IsClosed).ToList();
        var hasOpenEntries = openEntries.Count > 0;

        decimal principal;
        var isContinuation = false;
        if (dto.Principal is > 0)
        {
            principal = dto.Principal.Value;
        }
        else if (hasOpenEntries)
        {
            principal = openEntries.Sum(e => ComputeTotals(e).PrincipalDue);
            if (principal <= 0)
                throw new InvalidOperationException("No remaining principal to continue. Enter a principal amount.");
            isContinuation = true;
        }
        else
        {
            throw new InvalidOperationException("Principal is required for the first vyaj entry.");
        }

        if (dto.EmiAmount.HasValue && dto.EmiAmount.Value >= principal)
            throw new InvalidOperationException("First EMI must be less than principal.");

        if (isContinuation)
        {
            foreach (var prior in openEntries)
                prior.IsClosed = true;
        }

        var rateBasis = dto.RateBasis.ToLowerInvariant();
        var period = rateBasis == "month" ? dto.RatePeriodMonths : null;
        var customDays = rateBasis == "custom" ? dto.CustomDays : null;
        if (rateBasis == "custom" && (customDays is null or <= 0))
            throw new InvalidOperationException("Custom days is required for custom rate basis.");

        var entry = new VyajEntry
        {
            PartyId = dto.PartyId,
            Principal = principal,
            RatePercent = dto.RatePercent,
            RateBasis = rateBasis,
            RatePeriodMonths = period,
            CustomDays = customDays,
            EmiAmount = dto.EmiAmount,
            StartDate = dto.StartDate,
            IsClosed = false
        };

        _context.VyajEntries.Add(entry);

        if (dto.EmiAmount is > 0)
        {
            _context.VyajPayments.Add(new VyajPayment
            {
                Entry = entry,
                PaymentDate = dto.StartDate,
                Amount = dto.EmiAmount.Value,
                PaymentType = "principal"
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        var loaded = await LoadEntryAsync(entry.Id, cancellationToken);
        return MapEntry(loaded);
    }

    public async Task<VyajEntryDto> ToggleEntryClosedAsync(Guid entryId, ToggleVyajEntryClosedDto dto, Guid? actorUserId = null, CancellationToken cancellationToken = default)
    {
        var entry = await LoadEntryAsync(entryId, cancellationToken);
        entry.IsClosed = dto.IsClosed;
        await _context.SaveChangesAsync(cancellationToken);

        if (_notifications.IsOlderThanSixMonths(entry.StartDate))
        {
            await _notifications.NotifyOldRecordChangeAsync(new CreateChangeNotificationRequest
            {
                SiteId = entry.Party.SiteId,
                ActorUserId = actorUserId,
                Module = "vyaj",
                EntityType = "vyaj_entry",
                EntityId = entry.Id,
                Title = dto.IsClosed ? "Old vyaj entry closed" : "Old vyaj entry reopened",
                Message = $"Vyaj entry for \"{entry.Party.Name}\" starting {entry.StartDate:yyyy-MM-dd} was {(dto.IsClosed ? "closed" : "reopened")}.",
                NewValues = JsonSerializer.Serialize(dto)
            }, cancellationToken);
        }

        return MapEntry(entry);
    }

    public async Task DeleteEntryAsync(Guid entryId, Guid? actorUserId = null, CancellationToken cancellationToken = default)
    {
        var entry = await LoadEntryAsync(entryId, cancellationToken);
        var siteId = entry.Party.SiteId;
        var partyName = entry.Party.Name;
        var startDate = entry.StartDate;
        var paymentIds = entry.Payments.Where(p => !p.IsDeleted).Select(p => p.Id).ToList();
        SoftDeleteEntry(entry);
        await _context.SaveChangesAsync(cancellationToken);

        if (_notifications.IsOlderThanSixMonths(startDate))
        {
            await _notifications.NotifyOldRecordChangeAsync(new CreateChangeNotificationRequest
            {
                SiteId = siteId,
                ActorUserId = actorUserId,
                Module = "vyaj",
                EntityType = "vyaj_entry",
                EntityId = entryId,
                Title = "Old vyaj entry deleted",
                Message = $"Vyaj entry for \"{partyName}\" starting {startDate:yyyy-MM-dd} was deleted.",
                OldValues = JsonSerializer.Serialize(new
                {
                    partyId = entry.PartyId,
                    partyName,
                    startDate,
                    paymentIds
                })
            }, cancellationToken);
        }
    }

    public async Task<VyajPaymentDto> CreatePaymentAsync(CreateVyajPaymentDto dto, Guid? actorUserId = null, CancellationToken cancellationToken = default)
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

        if (_notifications.IsOlderThanSixMonths(entry.StartDate))
        {
            await _notifications.NotifyOldRecordChangeAsync(new CreateChangeNotificationRequest
            {
                SiteId = entry.Party.SiteId,
                ActorUserId = actorUserId,
                Module = "vyaj",
                EntityType = "vyaj_payment",
                EntityId = payment.Id,
                Title = "Payment on old vyaj entry",
                Message = $"A {payment.PaymentType} payment of {payment.Amount:0.00} was recorded on entry starting {entry.StartDate:yyyy-MM-dd}.",
                NewValues = JsonSerializer.Serialize(dto)
            }, cancellationToken);
        }

        return MapPayment(payment);
    }

    public async Task DeletePaymentAsync(Guid paymentId, Guid? actorUserId = null, CancellationToken cancellationToken = default)
    {
        var payment = await _context.VyajPayments
            .Include(p => p.Entry!).ThenInclude(e => e.Party)
            .FirstOrDefaultAsync(p => p.Id == paymentId && !p.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException("Vyaj payment not found.");

        payment.IsDeleted = true;
        payment.DeletedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        if (payment.Entry is not null && _notifications.IsOlderThanSixMonths(payment.Entry.StartDate))
        {
            await _notifications.NotifyOldRecordChangeAsync(new CreateChangeNotificationRequest
            {
                SiteId = payment.Entry.Party.SiteId,
                ActorUserId = actorUserId,
                Module = "vyaj",
                EntityType = "vyaj_payment",
                EntityId = paymentId,
                Title = "Payment deleted on old vyaj entry",
                Message = $"A payment on \"{payment.Entry.Party.Name}\" entry starting {payment.Entry.StartDate:yyyy-MM-dd} was deleted."
            }, cancellationToken);
        }
    }

    public async Task<int> CapitalizeUnpaidMonthsAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var lastCompletedMonthEnd = new DateOnly(today.Year, today.Month, 1).AddDays(-1);
        if (lastCompletedMonthEnd < new DateOnly(1900, 1, 1))
            return 0;

        var entries = await _context.VyajEntries
            .Include(e => e.Payments.Where(p => !p.IsDeleted))
            .Where(e => !e.IsDeleted && !e.IsClosed)
            .ToListAsync(cancellationToken);

        var created = 0;

        foreach (var entry in entries)
        {
            if (entry.StartDate > lastCompletedMonthEnd)
                continue;

            var basis = entry.RateBasis?.ToLowerInvariant() ?? "month";
            if (string.Equals(basis, "flat", StringComparison.OrdinalIgnoreCase))
                created += CapitalizeFlatUnpaidMonths(entry, lastCompletedMonthEnd);
            else if (string.Equals(basis, "custom", StringComparison.OrdinalIgnoreCase))
                created += CapitalizeCustomDayPeriods(entry, lastCompletedMonthEnd);
            else
                created += CapitalizePeriodUnpaid(entry, lastCompletedMonthEnd);
        }

        if (created > 0)
            await _context.SaveChangesAsync(cancellationToken);

        return created;
    }

    private int CapitalizeFlatUnpaidMonths(VyajEntry entry, DateOnly lastCompletedMonthEnd)
    {
        var created = 0;
        var cursor = new DateOnly(entry.StartDate.Year, entry.StartDate.Month, 1);
        if (entry.StartDate.Day > 1)
            cursor = cursor.AddMonths(1);

        while (true)
        {
            var monthEnd = cursor.AddMonths(1).AddDays(-1);
            if (monthEnd > lastCompletedMonthEnd)
                break;

            cursor = cursor.AddMonths(1);
            if (monthEnd < entry.StartDate)
                continue;

            var payments = entry.Payments.Where(p => !p.IsDeleted).ToList();
            if (payments.Any(p =>
                    string.Equals(p.PaymentType, "capitalize", StringComparison.OrdinalIgnoreCase)
                    && p.PaymentDate == monthEnd))
                continue;

            var paymentTuples = payments
                .Select(p => (p.Amount, p.PaymentType, p.PaymentDate))
                .ToList();

            var totals = VyajCalculationService.CalculateEntryTotals(
                entry.Principal,
                entry.RatePercent,
                entry.RateBasis,
                entry.StartDate,
                paymentTuples,
                monthEnd,
                entry.RatePeriodMonths);

            var unpaid = VyajCalculationService.RoundMoney(Math.Max(0, totals.VyajDue));
            if (unpaid <= 0)
                continue;

            var capitalize = new VyajPayment
            {
                EntryId = entry.Id,
                PaymentDate = monthEnd,
                Amount = unpaid,
                PaymentType = "capitalize"
            };
            _context.VyajPayments.Add(capitalize);
            entry.Payments.Add(capitalize);
            created++;
        }

        return created;
    }

    private int CapitalizePeriodUnpaid(VyajEntry entry, DateOnly lastCompletedMonthEnd)
    {
        var created = 0;
        var periodMonths = VyajCalculationService.ResolveCompoundPeriodMonths(entry.RateBasis, entry.RatePeriodMonths);
        var periodStart = entry.StartDate;

        while (true)
        {
            var periodEnd = periodStart.AddMonths(periodMonths).AddDays(-1);
            if (periodEnd > lastCompletedMonthEnd)
                break;

            var payments = entry.Payments.Where(p => !p.IsDeleted).ToList();
            if (payments.Any(p =>
                    string.Equals(p.PaymentType, "capitalize", StringComparison.OrdinalIgnoreCase)
                    && p.PaymentDate == periodEnd))
            {
                periodStart = periodEnd.AddDays(1);
                continue;
            }

            var interestInPeriod = payments
                .Where(p => string.Equals(p.PaymentType, "interest", StringComparison.OrdinalIgnoreCase)
                            && p.PaymentDate >= periodStart
                            && p.PaymentDate <= periodEnd)
                .Sum(p => p.Amount);

            var paymentTuples = payments
                .Select(p => (p.Amount, p.PaymentType, p.PaymentDate))
                .ToList();

            var periodAccrual = VyajCalculationService.CalculateMonthAccrual(
                entry.Principal,
                entry.RatePercent,
                entry.RateBasis,
                entry.StartDate,
                periodStart,
                periodEnd,
                paymentTuples,
                entry.RatePeriodMonths);

            var unpaid = VyajCalculationService.RoundMoney(Math.Max(0, periodAccrual - interestInPeriod));
            if (unpaid > 0)
            {
                var capitalize = new VyajPayment
                {
                    EntryId = entry.Id,
                    PaymentDate = periodEnd,
                    Amount = unpaid,
                    PaymentType = "capitalize"
                };
                _context.VyajPayments.Add(capitalize);
                entry.Payments.Add(capitalize);
                created++;
            }

            periodStart = periodEnd.AddDays(1);
        }

        return created;
    }

    private int CapitalizeCustomDayPeriods(VyajEntry entry, DateOnly lastCompletedMonthEnd)
    {
        var created = 0;
        var periodDays = entry.CustomDays is > 0 ? entry.CustomDays.Value : 0;
        if (periodDays <= 0)
            return 0;

        var periodStart = entry.StartDate;
        while (true)
        {
            var periodEnd = periodStart.AddDays(periodDays - 1);
            if (periodEnd > lastCompletedMonthEnd)
                break;

            var payments = entry.Payments.Where(p => !p.IsDeleted).ToList();
            if (payments.Any(p =>
                    string.Equals(p.PaymentType, "capitalize", StringComparison.OrdinalIgnoreCase)
                    && p.PaymentDate == periodEnd))
            {
                periodStart = periodEnd.AddDays(1);
                continue;
            }

            var interestInPeriod = payments
                .Where(p => string.Equals(p.PaymentType, "interest", StringComparison.OrdinalIgnoreCase)
                            && p.PaymentDate >= periodStart
                            && p.PaymentDate <= periodEnd)
                .Sum(p => p.Amount);

            var paymentTuples = payments
                .Select(p => (p.Amount, p.PaymentType, p.PaymentDate))
                .ToList();

            var periodAccrual = VyajCalculationService.CalculateMonthAccrual(
                entry.Principal,
                entry.RatePercent,
                entry.RateBasis,
                entry.StartDate,
                periodStart,
                periodEnd,
                paymentTuples,
                entry.RatePeriodMonths);

            var unpaid = VyajCalculationService.RoundMoney(Math.Max(0, periodAccrual - interestInPeriod));
            if (unpaid > 0)
            {
                var capitalize = new VyajPayment
                {
                    EntryId = entry.Id,
                    PaymentDate = periodEnd,
                    Amount = unpaid,
                    PaymentType = "capitalize"
                };
                _context.VyajPayments.Add(capitalize);
                entry.Payments.Add(capitalize);
                created++;
            }

            periodStart = periodEnd.AddDays(1);
        }

        return created;
    }

    public async Task<IReadOnlyList<VyajRateBasisOptionDto>> GetRateOptionsAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        var items = await _context.VyajRateBasisOptions.AsNoTracking()
            .Where(o => o.SiteId == siteId && !o.IsDeleted)
            .OrderBy(o => o.DayCount)
            .ThenBy(o => o.Label)
            .ToListAsync(cancellationToken);

        return items.Select(MapRateOption).ToList();
    }

    public async Task<VyajRateBasisOptionDto> CreateRateOptionAsync(CreateVyajRateBasisOptionDto dto, CancellationToken cancellationToken = default)
    {
        await EnsureSiteExistsAsync(dto.SiteId, cancellationToken);
        var option = new VyajRateBasisOption
        {
            SiteId = dto.SiteId,
            Label = dto.Label.Trim(),
            DayCount = dto.DayCount,
            IsActive = true
        };
        _context.VyajRateBasisOptions.Add(option);
        await _context.SaveChangesAsync(cancellationToken);
        return MapRateOption(option);
    }

    public async Task<VyajRateBasisOptionDto> UpdateRateOptionAsync(Guid id, UpdateVyajRateBasisOptionDto dto, CancellationToken cancellationToken = default)
    {
        var option = await _context.VyajRateBasisOptions
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException("Rate option not found.");

        option.Label = dto.Label.Trim();
        option.DayCount = dto.DayCount;
        option.IsActive = dto.IsActive;
        await _context.SaveChangesAsync(cancellationToken);
        return MapRateOption(option);
    }

    public async Task DeleteRateOptionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var option = await _context.VyajRateBasisOptions
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException("Rate option not found.");

        option.IsDeleted = true;
        option.DeletedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<DailyEntryExcelFileDto> ExportExcelAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        var (siteName, parties) = await LoadExportDataAsync(siteId, cancellationToken);
        var content = VyajExportBuilder.BuildExcel(siteName, parties);
        return new DailyEntryExcelFileDto
        {
            Content = content,
            FileName = $"vyaj-khata-{DateOnly.FromDateTime(DateTime.UtcNow):yyyy-MM-dd}.xlsx"
        };
    }

    public async Task<DailyEntryExcelFileDto> ExportPdfAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        var (siteName, parties) = await LoadExportDataAsync(siteId, cancellationToken);
        var content = VyajExportBuilder.BuildPdf(siteName, parties);
        return new DailyEntryExcelFileDto
        {
            Content = content,
            FileName = $"vyaj-khata-{DateOnly.FromDateTime(DateTime.UtcNow):yyyy-MM-dd}.pdf",
            ContentType = "application/pdf"
        };
    }

    private async Task<(string SiteName, IReadOnlyList<VyajPartyDetailDto> Parties)> LoadExportDataAsync(
        Guid siteId,
        CancellationToken cancellationToken)
    {
        var siteName = await _context.Sites.AsNoTracking()
            .Where(s => s.Id == siteId)
            .Select(s => s.SiteName)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(siteName))
            throw new InvalidOperationException("Site not found.");

        var summaries = await GetPartiesAsync(siteId, cancellationToken);
        var parties = new List<VyajPartyDetailDto>();
        foreach (var summary in summaries)
            parties.Add(await GetPartyDetailAsync(summary.Id, cancellationToken));

        return (siteName, parties);
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
            .Include(e => e.Party)
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

    private static string NormalizePartyKind(string? kind) =>
        string.Equals(kind, "asset", StringComparison.OrdinalIgnoreCase) ? "asset" : "direct";

    private static void SoftDeleteParty(VyajParty party)
    {
        party.IsDeleted = true;
        party.DeletedAt = DateTimeOffset.UtcNow;

        foreach (var entry in party.Entries.Where(e => !e.IsDeleted))
            SoftDeleteEntry(entry);
    }

    private static void SoftDeleteEntry(VyajEntry entry)
    {
        var now = DateTimeOffset.UtcNow;
        entry.IsDeleted = true;
        entry.DeletedAt = now;

        foreach (var payment in entry.Payments.Where(p => !p.IsDeleted))
        {
            payment.IsDeleted = true;
            payment.DeletedAt = now;
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
            PartyKind = NormalizePartyKind(party.PartyKind),
            MainLedgerId = party.MainLedgerId,
            SubLedgerId = party.SubLedgerId,
            BookingId = party.BookingId,
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
            CustomDays = entry.CustomDays,
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

    private static VyajRateBasisOptionDto MapRateOption(VyajRateBasisOption option) => new()
    {
        Id = option.Id,
        SiteId = option.SiteId,
        Label = option.Label,
        DayCount = option.DayCount,
        IsActive = option.IsActive
    };

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
            .Select(p => (p.Amount, p.PaymentType, p.PaymentDate));

        return VyajCalculationService.CalculateEntryTotals(
            entry.Principal,
            entry.RatePercent,
            entry.RateBasis,
            entry.StartDate,
            payments,
            ratePeriodMonths: entry.RatePeriodMonths);
    }
}
