using ABR.Application.DTOs.Reports;
using ABR.Application.Interfaces;
using ABR.Infrastructure.Persistence;
using ABR.Infrastructure.Services.Accounting;
using Microsoft.EntityFrameworkCore;

namespace ABR.Infrastructure.Services.Reports;

public sealed class ReportService : IReportService
{
    private readonly AbrDbContext _context;

    public ReportService(AbrDbContext context) => _context = context;

    public async Task<PagedReportDto<AllEntryReportRowDto>> GetAllEntryAsync(AllEntryReportFilterDto filter, CancellationToken cancellationToken = default)
    {
        var allRows = await BuildAllEntryRowsAsync(filter, cancellationToken);
        var total = allRows.Count;
        var items = allRows
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList();

        return new PagedReportDto<AllEntryReportRowDto>
        {
            Items = items,
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<IReadOnlyList<AllEntryReportRowDto>> GetAllEntryForExportAsync(AllEntryReportFilterDto filter, CancellationToken cancellationToken = default)
        => await BuildAllEntryRowsAsync(filter, cancellationToken);

    private async Task<List<AllEntryReportRowDto>> BuildAllEntryRowsAsync(AllEntryReportFilterDto filter, CancellationToken cancellationToken)
    {
        var q = _context.DailyEntries
            .AsNoTracking()
            .Include(e => e.MainLedger)
            .Include(e => e.SubLedger).ThenInclude(s => s.Flat)
            .Where(e => e.SiteId == filter.SiteId && !e.IsDeleted)
            .Where(e => e.EntryDate >= filter.DateFrom && e.EntryDate <= filter.DateTo);

        if (filter.MainLedgerId.HasValue)
            q = q.Where(e => e.MainLedgerId == filter.MainLedgerId.Value);
        if (filter.SubLedgerId.HasValue)
            q = q.Where(e => e.SubLedgerId == filter.SubLedgerId.Value);
        if (!string.IsNullOrWhiteSpace(filter.FlatNo))
            q = q.Where(e => e.SubLedger.Flat != null && e.SubLedger.Flat.FlatNo.Contains(filter.FlatNo));

        var entries = await q.OrderBy(e => e.EntryDate).ThenBy(e => e.CreatedAt).ToListAsync(cancellationToken);

        var byDate = entries.GroupBy(e => e.EntryDate).OrderBy(g => g.Key);
        var allRows = new List<AllEntryReportRowDto>();

        foreach (var group in byDate)
        {
            var aavak = group.Where(e => e.EntryType == "aavak").ToList();
            var javak = group.Where(e => e.EntryType == "javak").ToList();
            var max = Math.Max(aavak.Count, javak.Count);
            if (max == 0) max = 1;

            for (var i = 0; i < max; i++)
            {
                var a = i < aavak.Count ? aavak[i] : null;
                var j = i < javak.Count ? javak[i] : null;
                allRows.Add(new AllEntryReportRowDto
                {
                    Date = group.Key,
                    AavakLedger = a?.MainLedger.LedgerName,
                    AavakSubLedger = a?.SubLedger.LedgerName,
                    AavakFlatNo = a?.SubLedger.Flat?.FlatNo,
                    AavakCashBank = a?.CashBank,
                    AavakAmount = a?.Amount,
                    AavakDescription = a?.Description,
                    JavakLedger = j?.MainLedger.LedgerName,
                    JavakSubLedger = j?.SubLedger.LedgerName,
                    JavakCashBank = j?.CashBank,
                    JavakAmount = j?.Amount,
                    JavakDescription = j?.Description
                });
            }
        }

        return allRows;
    }

    public async Task<BalanceSheetReportDto> GetBalanceSheetAsync(BalanceSheetFilterDto filter, CancellationToken cancellationToken = default)
    {
        var site = await _context.Sites.AsNoTracking().FirstOrDefaultAsync(s => s.Id == filter.SiteId, cancellationToken)
            ?? throw new KeyNotFoundException("Site not found.");

        var q = _context.DailyEntries
            .AsNoTracking()
            .Include(e => e.MainLedger)
            .Where(e => e.SiteId == filter.SiteId && !e.IsDeleted);

        if (filter.DateFrom.HasValue)
            q = q.Where(e => e.EntryDate >= filter.DateFrom.Value);
        if (filter.DateTo.HasValue)
            q = q.Where(e => e.EntryDate <= filter.DateTo.Value);
        if (filter.MainLedgerId.HasValue)
            q = q.Where(e => e.MainLedgerId == filter.MainLedgerId.Value);

        var entries = await q.ToListAsync(cancellationToken);

        var aavakItems = entries
            .Where(e => e.EntryType == "aavak")
            .GroupBy(e => e.MainLedger.LedgerName)
            .Select(g => new BalanceSheetLedgerItemDto { LedgerName = g.Key, TotalAmount = g.Sum(e => e.Amount) })
            .OrderBy(x => x.LedgerName)
            .ToList();

        var javakItems = entries
            .Where(e => e.EntryType == "javak")
            .GroupBy(e => e.MainLedger.LedgerName)
            .Select(g => new BalanceSheetLedgerItemDto { LedgerName = g.Key, TotalAmount = g.Sum(e => e.Amount) })
            .OrderBy(x => x.LedgerName)
            .ToList();

        var totalAavak = aavakItems.Sum(x => x.TotalAmount);
        var totalJavak = javakItems.Sum(x => x.TotalAmount);

        return new BalanceSheetReportDto
        {
            SiteName = site.SiteName,
            DateFrom = filter.DateFrom,
            DateTo = filter.DateTo,
            AavakItems = aavakItems,
            TotalAavak = totalAavak,
            JavakItems = javakItems,
            TotalJavak = totalJavak,
            Profit = totalAavak - totalJavak
        };
    }

    public async Task<PagedReportDto<TillDateReportRowDto>> GetTillDateAsync(TillDateReportFilterDto filter, CancellationToken cancellationToken = default)
    {
        var ordered = await BuildTillDateRowsAsync(filter, cancellationToken);
        var total = ordered.Count;
        var items = ordered.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToList();

        return new PagedReportDto<TillDateReportRowDto>
        {
            Items = items,
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<IReadOnlyList<TillDateReportRowDto>> GetTillDateForExportAsync(TillDateReportFilterDto filter, CancellationToken cancellationToken = default)
        => await BuildTillDateRowsAsync(filter, cancellationToken);

    private async Task<List<TillDateReportRowDto>> BuildTillDateRowsAsync(TillDateReportFilterDto filter, CancellationToken cancellationToken)
    {
        var asOf = filter.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var site = await _context.Sites.AsNoTracking().FirstOrDefaultAsync(s => s.Id == filter.SiteId, cancellationToken)
            ?? throw new KeyNotFoundException("Site not found.");

        var bookings = await _context.Bookings
            .AsNoTracking()
            .Include(b => b.Flat).ThenInclude(f => f.Wing)
            .Include(b => b.MemberSubLedger)
            .Include(b => b.Broker)
            .Include(b => b.Installments)
            .Where(b => b.Flat.Wing.SiteId == filter.SiteId && b.Status == "active")
            .ToListAsync(cancellationToken);

        var subLedgerIds = bookings.Select(b => b.MemberSubLedgerId).Distinct().ToList();
        var paymentEntries = await _context.DailyEntries
            .AsNoTracking()
            .Where(e => e.SiteId == filter.SiteId && !e.IsDeleted && e.EntryType == "aavak" && subLedgerIds.Contains(e.SubLedgerId))
            .GroupBy(e => e.SubLedgerId)
            .Select(g => new
            {
                SubLedgerId = g.Key,
                TotalPaid = g.Sum(e => e.Amount),
                LastPaymentDate = g.Max(e => e.EntryDate)
            })
            .ToListAsync(cancellationToken);

        var paymentMap = paymentEntries.ToDictionary(x => x.SubLedgerId);

        var rows = new List<TillDateReportRowDto>();
        foreach (var b in bookings)
        {
            paymentMap.TryGetValue(b.MemberSubLedgerId, out var pay);
            var totalPaid = pay?.TotalPaid ?? 0;
            var lastPayment = pay?.LastPaymentDate;
            var remainingPerCondition = b.Installments.Sum(i => i.DueAmount - i.PaidAmount);
            var totalRemaining = b.TotalPrice - totalPaid;
            var daysFromLast = lastPayment.HasValue
                ? asOf.DayNumber - lastPayment.Value.DayNumber
                : (int?)null;
            var daysFromBooking = asOf.DayNumber - b.BookingDate.DayNumber;
            var pct = b.TotalPrice > 0 ? Math.Round(totalPaid / b.TotalPrice * 100, 2) : 0;

            if (filter.ExtraReturnOnly && totalPaid <= b.TotalPrice)
                continue;
            if (filter.MovementType == "no-movement" && filter.DaysFromLastPayment.HasValue)
            {
                if (!daysFromLast.HasValue || daysFromLast.Value <= filter.DaysFromLastPayment.Value)
                    continue;
            }

            rows.Add(new TillDateReportRowDto
            {
                SiteName = site.SiteName,
                WingName = b.Flat.Wing.WingName,
                FlatNo = b.Flat.FlatNo,
                MemberName = b.MemberSubLedger.LedgerName,
                CustomerContact = b.CustomerContact,
                BrokerName = b.Broker?.Name,
                BrokerContact = b.Broker?.ContactNo,
                BookingDate = b.BookingDate,
                Sqft = b.Sqft,
                Rate = b.Rate,
                TotalPrice = b.TotalPrice,
                TotalPaid = totalPaid,
                RemainingAsPerCondition = remainingPerCondition,
                TotalRemaining = totalRemaining,
                LastPaymentDate = lastPayment,
                DaysFromLastPayment = daysFromLast,
                DaysFromBooking = daysFromBooking,
                PercentagePaid = pct,
                DastavejDate = b.DastavejDate,
                ServiceTax = b.ServiceTax
            });
        }

        return rows.OrderBy(r => r.WingName).ThenBy(r => r.FlatNo).ToList();
    }

    public async Task<IReadOnlyList<MonthwiseReportRowDto>> GetMonthwiseAsync(MonthwiseReportFilterDto filter, CancellationToken cancellationToken = default)
    {
        var q = _context.DailyEntries
            .AsNoTracking()
            .Where(e => e.SiteId == filter.SiteId && !e.IsDeleted);

        if (filter.DateFrom.HasValue)
            q = q.Where(e => e.EntryDate >= filter.DateFrom.Value);
        if (filter.DateTo.HasValue)
            q = q.Where(e => e.EntryDate <= filter.DateTo.Value);

        var entries = await q.ToListAsync(cancellationToken);

        return entries
            .GroupBy(e => new { e.EntryDate.Year, e.EntryDate.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g =>
            {
                var aavak = g.Where(e => e.EntryType == "aavak").Sum(e => e.Amount);
                var javak = g.Where(e => e.EntryType == "javak").Sum(e => e.Amount);
                return new MonthwiseReportRowDto
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    MonthLabel = new DateOnly(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    AavakTotal = aavak,
                    JavakTotal = javak,
                    Net = aavak - javak
                };
            })
            .ToList();
    }

    public async Task<BankStatementReportDto> GetBankStatementAsync(BankStatementFilterDto filter, CancellationToken cancellationToken = default)
    {
        var bank = await _context.BankAccounts.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == filter.BankAccountId && b.SiteId == filter.SiteId, cancellationToken)
            ?? throw new KeyNotFoundException("Bank account not found.");

        var label = DailyEntryService.BankCashBankLabel(bank.BankName, bank.AccountNo);

        var q = _context.DailyEntries
            .AsNoTracking()
            .Where(e => e.SiteId == filter.SiteId && !e.IsDeleted && e.CashBank == label);

        if (filter.DateFrom.HasValue)
            q = q.Where(e => e.EntryDate >= filter.DateFrom.Value);
        if (filter.DateTo.HasValue)
            q = q.Where(e => e.EntryDate <= filter.DateTo.Value);

        var entries = await q.OrderBy(e => e.EntryDate).ThenBy(e => e.CreatedAt).ToListAsync(cancellationToken);

        var balance = bank.OpeningBalance;
        var rows = new List<BankStatementRowDto>();

        foreach (var e in entries)
        {
            var credit = e.EntryType == "aavak" ? e.Amount : 0;
            var debit = e.EntryType == "javak" ? e.Amount : 0;
            balance += credit - debit;
            rows.Add(new BankStatementRowDto
            {
                EntryDate = e.EntryDate,
                Description = e.Description,
                Debit = debit,
                Credit = credit,
                Balance = balance,
                EntryType = e.EntryType
            });
        }

        return new BankStatementReportDto
        {
            BankName = bank.BankName,
            AccountNo = bank.AccountNo,
            OpeningBalance = bank.OpeningBalance,
            Rows = rows,
            ClosingBalance = balance
        };
    }

    public async Task<SellDetailsReportDto> GetSellDetailsAsync(SellDetailsFilterDto filter, CancellationToken cancellationToken = default)
    {
        var q = _context.Bookings
            .AsNoTracking()
            .Include(b => b.Flat).ThenInclude(f => f.Wing)
            .Include(b => b.MemberSubLedger)
            .Where(b => b.Flat.Wing.SiteId == filter.SiteId);

        if (!string.IsNullOrWhiteSpace(filter.Status))
            q = q.Where(b => b.Status == filter.Status);
        else
            q = q.Where(b => b.Status == "active");

        if (filter.WingId.HasValue)
            q = q.Where(b => b.Flat.WingId == filter.WingId.Value);

        var bookings = await q.OrderBy(b => b.Flat.FlatNo).ToListAsync(cancellationToken);
        var subIds = bookings.Select(b => b.MemberSubLedgerId).Distinct().ToList();

        var paidMap = await _context.DailyEntries
            .AsNoTracking()
            .Where(e => e.SiteId == filter.SiteId && !e.IsDeleted && e.EntryType == "aavak" && subIds.Contains(e.SubLedgerId))
            .GroupBy(e => e.SubLedgerId)
            .Select(g => new { SubLedgerId = g.Key, Paid = g.Sum(e => e.Amount) })
            .ToDictionaryAsync(x => x.SubLedgerId, x => x.Paid, cancellationToken);

        var items = bookings.Select(b =>
        {
            var paid = paidMap.GetValueOrDefault(b.MemberSubLedgerId);
            return new SellDetailsRowDto
            {
                FlatNo = b.Flat.FlatNo,
                WingName = b.Flat.Wing.WingName,
                MemberName = b.MemberSubLedger.LedgerName,
                BookingDate = b.BookingDate,
                TotalPrice = b.TotalPrice,
                Paid = paid,
                Remaining = b.TotalPrice - paid,
                Status = b.Status
            };
        }).ToList();

        return new SellDetailsReportDto
        {
            Items = items,
            TotalPrice = items.Sum(i => i.TotalPrice),
            TotalPaid = items.Sum(i => i.Paid),
            TotalRemaining = items.Sum(i => i.Remaining)
        };
    }

    public async Task<IReadOnlyList<InstallmentReportRowDto>> GetInstallmentAsync(InstallmentReportFilterDto filter, CancellationToken cancellationToken = default)
    {
        var q = _context.Bookings
            .AsNoTracking()
            .Include(b => b.Flat).ThenInclude(f => f.Wing)
            .Include(b => b.MemberSubLedger)
            .Include(b => b.Installments)
            .Where(b => b.Flat.Wing.SiteId == filter.SiteId);

        if (filter.BookingId.HasValue)
            q = q.Where(b => b.Id == filter.BookingId.Value);
        else if (!string.IsNullOrWhiteSpace(filter.FlatNo))
            q = q.Where(b => b.Flat.FlatNo.Contains(filter.FlatNo));

        var booking = await q.FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Booking not found.");

        return booking.Installments
            .OrderBy(i => i.SortOrder)
            .Select(i => new InstallmentReportRowDto
            {
                FlatNo = booking.Flat.FlatNo,
                MemberName = booking.MemberSubLedger.LedgerName,
                MilestoneName = i.MilestoneName,
                SortOrder = i.SortOrder,
                DueAmount = i.DueAmount,
                PaidAmount = i.PaidAmount,
                Remaining = i.DueAmount - i.PaidAmount,
                DueDate = i.DueDate,
                PaidDate = i.PaidDate,
                Status = i.Status
            })
            .ToList();
    }
}
