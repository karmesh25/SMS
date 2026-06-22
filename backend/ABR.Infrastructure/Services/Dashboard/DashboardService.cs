using ABR.Application.DTOs.Dashboard;
using ABR.Application.Interfaces;
using ABR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ABR.Infrastructure.Services.Dashboard;

public sealed class DashboardService : IDashboardService
{
    private readonly AbrDbContext _context;
    private readonly IDailyEntryService _dailyEntryService;

    public DashboardService(AbrDbContext context, IDailyEntryService dailyEntryService)
    {
        _context = context;
        _dailyEntryService = dailyEntryService;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        var siteExists = await _context.Sites.AsNoTracking().AnyAsync(s => s.Id == siteId, cancellationToken);
        if (!siteExists)
            throw new KeyNotFoundException("Site not found.");

        var flats = await _context.Flats
            .AsNoTracking()
            .Where(f => f.Wing.SiteId == siteId)
            .Select(f => new { f.Status, f.WingId })
            .ToListAsync(cancellationToken);

        var totalFlats = flats.Count;
        var bookedFlats = flats.Count(f => f.Status == "booked");
        var availableFlats = flats.Count(f => f.Status == "available");
        var cancelledFlats = flats.Count(f => f.Status == "cancelled");
        var bookingPct = totalFlats > 0
            ? Math.Round((decimal)bookedFlats / totalFlats * 100, 2)
            : 0;

        var wings = await _context.Wings
            .AsNoTracking()
            .Where(w => w.SiteId == siteId)
            .OrderBy(w => w.WingName)
            .Select(w => new { w.Id, w.WingName })
            .ToListAsync(cancellationToken);

        var wingSummary = wings.Select(w =>
        {
            var wingFlats = flats.Where(f => f.WingId == w.Id).ToList();
            var total = wingFlats.Count;
            var booked = wingFlats.Count(f => f.Status == "booked");
            var available = wingFlats.Count(f => f.Status == "available");
            var pct = total > 0 ? Math.Round((decimal)booked / total * 100, 2) : 0;
            return new WingSummaryDto
            {
                WingId = w.Id,
                WingName = w.WingName,
                Total = total,
                Booked = booked,
                Available = available,
                BookingPercentage = pct
            };
        }).ToList();

        var profit = await _dailyEntryService.GetProfitAsync(siteId, cancellationToken);

        var activeBookings = await _context.Bookings
            .AsNoTracking()
            .Where(b => b.Flat.Wing.SiteId == siteId && b.Status == "active")
            .Select(b => new { b.MemberSubLedgerId, b.TotalPrice })
            .ToListAsync(cancellationToken);

        var subLedgerIds = activeBookings.Select(b => b.MemberSubLedgerId).Distinct().ToList();
        var paidMap = subLedgerIds.Count == 0
            ? new Dictionary<Guid, decimal>()
            : await _context.DailyEntries
                .AsNoTracking()
                .Where(e => e.SiteId == siteId && !e.IsDeleted && e.EntryType == "aavak" && subLedgerIds.Contains(e.SubLedgerId))
                .GroupBy(e => e.SubLedgerId)
                .Select(g => new { SubLedgerId = g.Key, Paid = g.Sum(e => e.Amount) })
                .ToDictionaryAsync(x => x.SubLedgerId, x => x.Paid, cancellationToken);

        var totalOutstanding = activeBookings.Sum(b =>
        {
            paidMap.TryGetValue(b.MemberSubLedgerId, out var paid);
            var remaining = b.TotalPrice - paid;
            return remaining > 0 ? remaining : 0;
        });

        var recentEntries = await _context.DailyEntries
            .AsNoTracking()
            .Include(e => e.MainLedger)
            .Include(e => e.SubLedger)
            .Where(e => e.SiteId == siteId && !e.IsDeleted)
            .OrderByDescending(e => e.EntryDate)
            .ThenByDescending(e => e.CreatedAt)
            .Take(5)
            .Select(e => new RecentEntryDto
            {
                Id = e.Id,
                EntryDate = e.EntryDate,
                EntryType = e.EntryType,
                MainLedgerName = e.MainLedger.LedgerName,
                SubLedgerName = e.SubLedger.LedgerName,
                Amount = e.Amount
            })
            .ToListAsync(cancellationToken);

        return new DashboardSummaryDto
        {
            TotalFlats = totalFlats,
            BookedFlats = bookedFlats,
            AvailableFlats = availableFlats,
            CancelledFlats = cancelledFlats,
            BookingPercentage = bookingPct,
            TotalAavak = profit.TotalAavak,
            TotalJavak = profit.TotalJavak,
            NetProfit = profit.Profit,
            TotalOutstanding = totalOutstanding,
            WingSummary = wingSummary,
            RecentEntries = recentEntries
        };
    }
}
