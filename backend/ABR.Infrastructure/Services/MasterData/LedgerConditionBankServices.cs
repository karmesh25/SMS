using ABR.Application.DTOs.MasterData;
using ABR.Application.Interfaces;
using ABR.Domain.Entities;
using ABR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ABR.Infrastructure.Services.MasterData;

public sealed class MainLedgerService : IMainLedgerService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(7);

    private readonly AbrDbContext _context;
    private readonly IMemoryCache _cache;

    public MainLedgerService(AbrDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    private static string CacheKey(Guid siteId) => $"masterdata:main-ledgers:site:{siteId}";

    public async Task<IReadOnlyList<MainLedgerDto>> GetBySiteAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAsync(CacheKey(siteId), async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return await _context.MainLedgers
                .Where(l => l.SiteId == siteId)
                .OrderBy(l => l.LedgerName)
                .Select(l => new MainLedgerDto
                {
                    Id = l.Id,
                    SiteId = l.SiteId,
                    LedgerName = l.LedgerName,
                    Description = l.Description
                }).ToListAsync(cancellationToken);
        }) ?? [];
    }

    public async Task<MainLedgerDto> CreateAsync(CreateMainLedgerDto dto, CancellationToken cancellationToken = default)
    {
        var ledger = new MainLedger
        {
            SiteId = dto.SiteId,
            LedgerName = dto.LedgerName,
            Description = dto.Description
        };
        _context.MainLedgers.Add(ledger);
        await _context.SaveChangesAsync(cancellationToken);
        _cache.Remove(CacheKey(dto.SiteId));
        return Map(ledger);
    }

    public async Task<MainLedgerDto?> UpdateAsync(Guid id, CreateMainLedgerDto dto, CancellationToken cancellationToken = default)
    {
        var ledger = await _context.MainLedgers.FindAsync([id], cancellationToken);
        if (ledger is null) return null;

        ledger.LedgerName = dto.LedgerName;
        ledger.Description = dto.Description;
        await _context.SaveChangesAsync(cancellationToken);
        _cache.Remove(CacheKey(ledger.SiteId));
        return Map(ledger);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ledger = await _context.MainLedgers
            .Include(l => l.SubLedgers)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (ledger is null) return false;

        if (ledger.SubLedgers.Count > 0)
            throw new InvalidOperationException("Cannot delete main ledger with sub-ledgers.");
        if (await _context.DailyEntries.AnyAsync(e => e.MainLedgerId == id && !e.IsDeleted, cancellationToken))
            throw new InvalidOperationException("Cannot delete main ledger with daily entries.");

        _context.MainLedgers.Remove(ledger);
        await _context.SaveChangesAsync(cancellationToken);
        _cache.Remove(CacheKey(ledger.SiteId));
        return true;
    }

    private static MainLedgerDto Map(MainLedger l) => new()
    {
        Id = l.Id,
        SiteId = l.SiteId,
        LedgerName = l.LedgerName,
        Description = l.Description
    };
}

public sealed class SubLedgerService : ISubLedgerService
{
    private readonly AbrDbContext _context;

    public SubLedgerService(AbrDbContext context) => _context = context;

    public async Task<IReadOnlyList<SubLedgerDto>> GetByMainLedgerAsync(Guid mainLedgerId, CancellationToken cancellationToken = default)
    {
        var subs = await _context.SubLedgers
            .Include(s => s.Flat)
            .Where(s => s.MainLedgerId == mainLedgerId)
            .OrderBy(s => s.LedgerName)
            .ToListAsync(cancellationToken);

        return subs.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<SubLedgerDto>> SearchByFlatNoAsync(Guid siteId, string flatNo, CancellationToken cancellationToken = default)
    {
        var subs = await _context.SubLedgers
            .Include(s => s.Flat)
            .Include(s => s.MainLedger)
            .Where(s => s.MainLedger.SiteId == siteId && s.Flat != null && s.Flat.FlatNo.Contains(flatNo))
            .OrderBy(s => s.Flat!.FlatNo)
            .ToListAsync(cancellationToken);

        return subs.Select(Map).ToList();
    }

    public async Task<SubLedgerDto> CreateAsync(CreateSubLedgerDto dto, CancellationToken cancellationToken = default)
    {
        var sub = new SubLedger
        {
            MainLedgerId = dto.MainLedgerId,
            LedgerName = dto.LedgerName,
            FlatId = dto.FlatId
        };
        _context.SubLedgers.Add(sub);
        await _context.SaveChangesAsync(cancellationToken);

        if (dto.FlatId.HasValue)
        {
            sub.Flat = await _context.Flats.FindAsync([dto.FlatId.Value], cancellationToken);
        }

        return Map(sub);
    }

    public async Task<SubLedgerDto?> UpdateAsync(Guid id, CreateSubLedgerDto dto, CancellationToken cancellationToken = default)
    {
        var sub = await _context.SubLedgers.Include(s => s.Flat).FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (sub is null) return null;

        sub.LedgerName = dto.LedgerName;
        sub.FlatId = dto.FlatId;
        await _context.SaveChangesAsync(cancellationToken);
        return Map(sub);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sub = await _context.SubLedgers.FindAsync([id], cancellationToken);
        if (sub is null) return false;

        if (await _context.DailyEntries.AnyAsync(e => e.SubLedgerId == id && !e.IsDeleted, cancellationToken))
            throw new InvalidOperationException("Cannot delete sub ledger with daily entries.");
        if (await _context.Bookings.AnyAsync(b => b.MemberSubLedgerId == id, cancellationToken))
            throw new InvalidOperationException("Cannot delete sub ledger linked to bookings.");

        _context.SubLedgers.Remove(sub);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static SubLedgerDto Map(SubLedger s) => new()
    {
        Id = s.Id,
        MainLedgerId = s.MainLedgerId,
        LedgerName = s.LedgerName,
        FlatId = s.FlatId,
        FlatNo = s.Flat?.FlatNo
    };
}

public sealed class ConditionService : IConditionService
{
    private readonly AbrDbContext _context;

    public ConditionService(AbrDbContext context) => _context = context;

    public async Task<IReadOnlyList<ConditionDto>> GetBySiteAsync(Guid siteId, CancellationToken cancellationToken = default) =>
        await _context.Conditions
            .Where(c => c.SiteId == siteId)
            .OrderBy(c => c.ConditionName)
            .Select(c => new ConditionDto
            {
                Id = c.Id,
                SiteId = c.SiteId,
                ConditionName = c.ConditionName,
                ConditionType = c.ConditionType,
                ItemCount = c.Items.Count
            }).ToListAsync(cancellationToken);

    public async Task<ConditionDto> CreateAsync(CreateConditionDto dto, CancellationToken cancellationToken = default)
    {
        var condition = new Condition
        {
            SiteId = dto.SiteId,
            ConditionName = dto.ConditionName,
            ConditionType = dto.ConditionType
        };
        _context.Conditions.Add(condition);
        await _context.SaveChangesAsync(cancellationToken);

        return new ConditionDto
        {
            Id = condition.Id,
            SiteId = condition.SiteId,
            ConditionName = condition.ConditionName,
            ConditionType = condition.ConditionType,
            ItemCount = 0
        };
    }

    public async Task<IReadOnlyList<ConditionItemDto>> GetItemsAsync(Guid conditionId, CancellationToken cancellationToken = default)
    {
        var items = await _context.ConditionItems
            .Where(i => i.ConditionId == conditionId)
            .OrderBy(i => i.SortOrder)
            .ToListAsync(cancellationToken);

        return items.Select(MapItem).ToList();
    }

    public async Task<ConditionItemDto> AddItemAsync(Guid conditionId, CreateConditionItemDto dto, CancellationToken cancellationToken = default)
    {
        var item = new ConditionItem
        {
            ConditionId = conditionId,
            MilestoneName = dto.MilestoneName,
            Percentage = dto.Percentage,
            Amount = dto.Amount,
            DueAfterDays = dto.DueAfterDays,
            SortOrder = dto.SortOrder
        };
        _context.ConditionItems.Add(item);
        await _context.SaveChangesAsync(cancellationToken);
        return MapItem(item);
    }

    public async Task<ConditionDto?> UpdateAsync(Guid id, UpdateConditionDto dto, CancellationToken cancellationToken = default)
    {
        var condition = await _context.Conditions.FindAsync([id], cancellationToken);
        if (condition is null) return null;

        condition.ConditionName = dto.ConditionName;
        condition.ConditionType = dto.ConditionType;
        await _context.SaveChangesAsync(cancellationToken);

        return new ConditionDto
        {
            Id = condition.Id,
            SiteId = condition.SiteId,
            ConditionName = condition.ConditionName,
            ConditionType = condition.ConditionType,
            ItemCount = await _context.ConditionItems.CountAsync(i => i.ConditionId == id, cancellationToken)
        };
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var condition = await _context.Conditions.FindAsync([id], cancellationToken);
        if (condition is null) return false;

        if (await _context.Bookings.AnyAsync(b => b.ConditionId == id, cancellationToken))
            throw new InvalidOperationException("Cannot delete condition used by bookings.");

        var items = await _context.ConditionItems.Where(i => i.ConditionId == id).ToListAsync(cancellationToken);
        _context.ConditionItems.RemoveRange(items);
        _context.Conditions.Remove(condition);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ConditionItemDto?> UpdateItemAsync(Guid itemId, UpdateConditionItemDto dto, CancellationToken cancellationToken = default)
    {
        var item = await _context.ConditionItems.FindAsync([itemId], cancellationToken);
        if (item is null) return null;

        if (await _context.Bookings.AnyAsync(b => b.ConditionId == item.ConditionId, cancellationToken))
            throw new InvalidOperationException("Cannot edit items for condition used by bookings.");

        item.MilestoneName = dto.MilestoneName;
        item.Percentage = dto.Percentage;
        item.Amount = dto.Amount;
        item.DueAfterDays = dto.DueAfterDays;
        item.SortOrder = dto.SortOrder;
        await _context.SaveChangesAsync(cancellationToken);
        return MapItem(item);
    }

    public async Task<bool> DeleteItemAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        var item = await _context.ConditionItems.FindAsync([itemId], cancellationToken);
        if (item is null) return false;

        if (await _context.Bookings.AnyAsync(b => b.ConditionId == item.ConditionId, cancellationToken))
            throw new InvalidOperationException("Cannot delete items for condition used by bookings.");

        _context.ConditionItems.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static ConditionItemDto MapItem(ConditionItem i) => new()
    {
        Id = i.Id,
        ConditionId = i.ConditionId,
        MilestoneName = i.MilestoneName,
        Percentage = i.Percentage,
        Amount = i.Amount,
        DueAfterDays = i.DueAfterDays,
        SortOrder = i.SortOrder
    };
}

public sealed class BankAccountService : IBankAccountService
{
    private readonly AbrDbContext _context;

    public BankAccountService(AbrDbContext context) => _context = context;

    public async Task<IReadOnlyList<BankAccountDto>> GetBySiteAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        var banks = await _context.BankAccounts
            .Where(b => b.SiteId == siteId)
            .OrderBy(b => b.BankName)
            .ToListAsync(cancellationToken);

        return banks.Select(Map).ToList();
    }

    public async Task<BankAccountDto> CreateAsync(CreateBankAccountDto dto, CancellationToken cancellationToken = default)
    {
        var bank = new BankAccount
        {
            SiteId = dto.SiteId,
            BankName = dto.BankName,
            AccountNo = dto.AccountNo,
            IfscCode = dto.IfscCode,
            Branch = dto.Branch,
            OpeningBalance = dto.OpeningBalance,
            IsActive = true
        };
        _context.BankAccounts.Add(bank);
        await _context.SaveChangesAsync(cancellationToken);
        return Map(bank);
    }

    public async Task<BankAccountDto?> UpdateAsync(Guid id, UpdateBankAccountDto dto, CancellationToken cancellationToken = default)
    {
        var bank = await _context.BankAccounts.FindAsync([id], cancellationToken);
        if (bank is null) return null;

        bank.BankName = dto.BankName;
        bank.AccountNo = dto.AccountNo;
        bank.IfscCode = dto.IfscCode;
        bank.Branch = dto.Branch;
        bank.OpeningBalance = dto.OpeningBalance;
        bank.IsActive = dto.IsActive;
        await _context.SaveChangesAsync(cancellationToken);
        return Map(bank);
    }

    public async Task<bool> ToggleActiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var bank = await _context.BankAccounts.FindAsync([id], cancellationToken);
        if (bank is null) return false;

        bank.IsActive = !bank.IsActive;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static BankAccountDto Map(BankAccount b) => new()
    {
        Id = b.Id,
        SiteId = b.SiteId,
        BankName = b.BankName,
        AccountNo = b.AccountNo,
        IfscCode = b.IfscCode,
        Branch = b.Branch,
        OpeningBalance = b.OpeningBalance,
        IsActive = b.IsActive
    };
}

public sealed class BrokerService : IBrokerService
{
    private readonly AbrDbContext _context;

    public BrokerService(AbrDbContext context) => _context = context;

    public async Task<IReadOnlyList<BrokerDto>> GetBySiteAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        var brokers = await _context.Brokers
            .Where(b => b.SiteId == siteId)
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);

        return brokers.Select(Map).ToList();
    }

    public async Task<BrokerDto> CreateAsync(CreateBrokerDto dto, CancellationToken cancellationToken = default)
    {
        var broker = new Broker
        {
            SiteId = dto.SiteId,
            Name = dto.Name,
            ContactNo = dto.ContactNo,
            ContactNo2 = dto.ContactNo2,
            Address = dto.Address
        };
        _context.Brokers.Add(broker);
        await _context.SaveChangesAsync(cancellationToken);
        return Map(broker);
    }

    public async Task<BrokerDto?> UpdateAsync(Guid id, CreateBrokerDto dto, CancellationToken cancellationToken = default)
    {
        var broker = await _context.Brokers.FindAsync([id], cancellationToken);
        if (broker is null) return null;

        broker.Name = dto.Name;
        broker.ContactNo = dto.ContactNo;
        broker.ContactNo2 = dto.ContactNo2;
        broker.Address = dto.Address;
        await _context.SaveChangesAsync(cancellationToken);
        return Map(broker);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var broker = await _context.Brokers.FindAsync([id], cancellationToken);
        if (broker is null) return false;

        if (await _context.Bookings.AnyAsync(b => b.BrokerId == id, cancellationToken))
            throw new InvalidOperationException("Cannot delete broker with bookings.");

        _context.Brokers.Remove(broker);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static BrokerDto Map(Broker b) => new()
    {
        Id = b.Id,
        SiteId = b.SiteId,
        Name = b.Name,
        ContactNo = b.ContactNo,
        ContactNo2 = b.ContactNo2,
        Address = b.Address
    };
}
