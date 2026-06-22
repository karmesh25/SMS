using System.Text.Json;
using ABR.Application.DTOs.Accounting;
using ABR.Application.Interfaces;
using ABR.Domain.Entities;
using ABR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ABR.Infrastructure.Services.Accounting;

public sealed class DailyEntryService : IDailyEntryService
{
    public const string CashLabel = "Cash";

    private readonly AbrDbContext _context;
    private readonly IAuditLogService _auditLog;

    public DailyEntryService(AbrDbContext context, IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    public static string BankCashBankLabel(string bankName, string accountNo) => $"{bankName} - {accountNo}";

    public async Task<PagedDailyEntriesDto> GetListAsync(DailyEntryFilterDto filter, CancellationToken cancellationToken = default)
    {
        var q = BaseQuery().Where(e => e.SiteId == filter.SiteId && !e.IsDeleted);

        if (!string.IsNullOrWhiteSpace(filter.EntryType))
            q = q.Where(e => e.EntryType == filter.EntryType);
        if (filter.MainLedgerId.HasValue)
            q = q.Where(e => e.MainLedgerId == filter.MainLedgerId.Value);
        if (filter.SubLedgerId.HasValue)
            q = q.Where(e => e.SubLedgerId == filter.SubLedgerId.Value);
        if (filter.DateFrom.HasValue)
            q = q.Where(e => e.EntryDate >= filter.DateFrom.Value);
        if (filter.DateTo.HasValue)
            q = q.Where(e => e.EntryDate <= filter.DateTo.Value);
        if (!string.IsNullOrWhiteSpace(filter.FlatNo))
            q = q.Where(e => e.SubLedger.Flat != null && e.SubLedger.Flat.FlatNo.Contains(filter.FlatNo));

        var total = await q.CountAsync(cancellationToken);
        var entries = await q
            .OrderByDescending(e => e.EntryDate)
            .ThenByDescending(e => e.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedDailyEntriesDto
        {
            Items = entries.Select(MapEntry).ToList(),
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<DailyEntryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await BaseQuery().FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);
        return entry is null ? null : MapEntry(entry);
    }

    public async Task<DailyEntryDto> CreateAsync(CreateDailyEntryDto dto, Guid? userId, CancellationToken cancellationToken = default)
    {
        var entry = new DailyEntry
        {
            SiteId = dto.SiteId,
            EntryType = dto.EntryType,
            EntryDate = dto.EntryDate,
            MainLedgerId = dto.MainLedgerId,
            SubLedgerId = dto.SubLedgerId,
            Amount = dto.Amount,
            CashBank = dto.CashBank,
            Description = dto.Description
        };

        _context.DailyEntries.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditLog.LogAsync(userId, "Create", "daily_entries", entry.Id, newValues: JsonSerializer.Serialize(dto), cancellationToken: cancellationToken);

        return (await GetByIdAsync(entry.Id, cancellationToken))!;
    }

    public async Task<DailyEntryDto?> UpdateAsync(Guid id, UpdateDailyEntryDto dto, Guid? userId, CancellationToken cancellationToken = default)
    {
        var entry = await _context.DailyEntries.FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);
        if (entry is null) return null;

        var oldValues = JsonSerializer.Serialize(new
        {
            entry.EntryType,
            entry.EntryDate,
            entry.MainLedgerId,
            entry.SubLedgerId,
            entry.Amount,
            entry.CashBank,
            entry.Description
        });

        entry.EntryType = dto.EntryType;
        entry.EntryDate = dto.EntryDate;
        entry.MainLedgerId = dto.MainLedgerId;
        entry.SubLedgerId = dto.SubLedgerId;
        entry.Amount = dto.Amount;
        entry.CashBank = dto.CashBank;
        entry.Description = dto.Description;

        await _context.SaveChangesAsync(cancellationToken);
        await _auditLog.LogAsync(userId, "Update", "daily_entries", entry.Id, oldValues, JsonSerializer.Serialize(dto), cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid? userId, CancellationToken cancellationToken = default)
    {
        var entry = await _context.DailyEntries.FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);
        if (entry is null) return false;

        entry.IsDeleted = true;
        entry.DeletedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        await _auditLog.LogAsync(userId, "Delete", "daily_entries", entry.Id, cancellationToken: cancellationToken);

        return true;
    }

    public async Task<ProfitSummaryDto> GetProfitAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        var entries = await _context.DailyEntries
            .Where(e => e.SiteId == siteId && !e.IsDeleted)
            .GroupBy(e => e.EntryType)
            .Select(g => new { Type = g.Key, Total = g.Sum(e => e.Amount) })
            .ToListAsync(cancellationToken);

        var aavak = entries.FirstOrDefault(x => x.Type == "aavak")?.Total ?? 0;
        var javak = entries.FirstOrDefault(x => x.Type == "javak")?.Total ?? 0;

        return new ProfitSummaryDto
        {
            TotalAavak = aavak,
            TotalJavak = javak,
            Profit = aavak - javak
        };
    }

    public async Task<BalanceSummaryDto> GetBalanceAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        var entries = await _context.DailyEntries
            .Where(e => e.SiteId == siteId && !e.IsDeleted)
            .ToListAsync(cancellationToken);

        var cashNet = entries
            .Where(e => e.CashBank == CashLabel)
            .Sum(e => e.EntryType == "aavak" ? e.Amount : -e.Amount);

        var banks = await _context.BankAccounts
            .Where(b => b.SiteId == siteId && b.IsActive)
            .OrderBy(b => b.BankName)
            .ToListAsync(cancellationToken);

        var bankBalances = banks.Select(b =>
        {
            var label = BankCashBankLabel(b.BankName, b.AccountNo);
            var net = entries
                .Where(e => e.CashBank == label)
                .Sum(e => e.EntryType == "aavak" ? e.Amount : -e.Amount);

            return new BankBalanceDto
            {
                BankAccountId = b.Id,
                BankName = b.BankName,
                AccountNo = b.AccountNo,
                CashBankLabel = label,
                OpeningBalance = b.OpeningBalance,
                Balance = b.OpeningBalance + net
            };
        }).ToList();

        return new BalanceSummaryDto
        {
            CashBalance = cashNet,
            BankBalances = bankBalances
        };
    }

    public async Task CreateFromInstallmentAsync(Guid siteId, Guid mainLedgerId, Guid subLedgerId, decimal amount, DateOnly entryDate, string milestoneName, Guid? userId, CancellationToken cancellationToken = default)
    {
        await CreateAsync(new CreateDailyEntryDto
        {
            SiteId = siteId,
            EntryType = "aavak",
            EntryDate = entryDate,
            MainLedgerId = mainLedgerId,
            SubLedgerId = subLedgerId,
            Amount = amount,
            CashBank = CashLabel,
            Description = $"Installment: {milestoneName}"
        }, userId, cancellationToken);
    }

    private IQueryable<DailyEntry> BaseQuery() =>
        _context.DailyEntries
            .Include(e => e.MainLedger)
            .Include(e => e.SubLedger).ThenInclude(s => s.Flat);

    private static DailyEntryDto MapEntry(DailyEntry e) => new()
    {
        Id = e.Id,
        SiteId = e.SiteId,
        EntryType = e.EntryType,
        EntryDate = e.EntryDate,
        MainLedgerId = e.MainLedgerId,
        MainLedgerName = e.MainLedger.LedgerName,
        SubLedgerId = e.SubLedgerId,
        SubLedgerName = e.SubLedger.LedgerName,
        FlatNo = e.SubLedger.Flat?.FlatNo,
        Amount = e.Amount,
        CashBank = e.CashBank,
        Description = e.Description
    };
}
