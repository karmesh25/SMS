using System.Text.Json;
using ABR.Application.DTOs.Accounting;
using ABR.Application.DTOs.MasterData;
using ABR.Application.Interfaces;
using ABR.Domain.Entities;
using ABR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ABR.Infrastructure.Services.Accounting;

public sealed class DailyEntryExcelService : IDailyEntryExcelService
{
    private const int MaxFileBytes = 5 * 1024 * 1024;

    private readonly AbrDbContext _context;
    private readonly IMainLedgerService _mainLedgerService;
    private readonly ISubLedgerService _subLedgerService;
    private readonly IAuditLogService _auditLog;

    public DailyEntryExcelService(
        AbrDbContext context,
        IMainLedgerService mainLedgerService,
        ISubLedgerService subLedgerService,
        IAuditLogService auditLog)
    {
        _context = context;
        _mainLedgerService = mainLedgerService;
        _subLedgerService = subLedgerService;
        _auditLog = auditLog;
    }

    public Task<DailyEntryExcelFileDto> GetSampleAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var content = DailyEntryExcelParser.BuildSampleWorkbook();
        return Task.FromResult(new DailyEntryExcelFileDto
        {
            Content = content,
            FileName = "daily-entry-import-sample.xlsx"
        });
    }

    public async Task<DailyEntryImportResultDto> ImportAsync(
        Guid siteId,
        Stream fileStream,
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        await EnsureSiteExistsAsync(siteId, cancellationToken);

        using var ms = new MemoryStream();
        await fileStream.CopyToAsync(ms, cancellationToken);
        if (ms.Length == 0)
            throw new InvalidOperationException("Uploaded file is empty.");
        if (ms.Length > MaxFileBytes)
            throw new InvalidOperationException("File exceeds maximum size of 5 MB.");

        ms.Position = 0;
        var (validRows, rowErrors) = DailyEntryExcelParser.ParseImport(ms);
        var errors = rowErrors.Select(e => new DailyEntryImportErrorDto { RowNumber = e.RowNumber, Message = e.Message }).ToList();
        var imported = 0;

        var mainCache = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var subCache = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in validRows)
        {
            try
            {
                var mainId = await ResolveMainLedgerAsync(siteId, row.MainLedgerName, mainCache, cancellationToken);
                var subKey = $"{mainId}:{row.SubLedgerName}";
                var subId = await ResolveSubLedgerAsync(mainId, row.SubLedgerName, subCache, subKey, cancellationToken);

                var entry = new DailyEntry
                {
                    SiteId = siteId,
                    EntryType = row.EntryType,
                    EntryDate = row.EntryDate,
                    MainLedgerId = mainId,
                    SubLedgerId = subId,
                    Amount = row.Amount,
                    CashBank = DailyEntryService.CashLabel,
                    Description = row.Description
                };

                _context.DailyEntries.Add(entry);
                await _context.SaveChangesAsync(cancellationToken);
                await _auditLog.LogAsync(
                    userId,
                    "Create",
                    "daily_entries",
                    entry.Id,
                    newValues: JsonSerializer.Serialize(new
                    {
                        Source = "excel-import",
                        row.RowNumber,
                        row.EntryType,
                        row.MainLedgerName,
                        row.SubLedgerName,
                        row.Amount
                    }),
                    cancellationToken: cancellationToken);
                imported++;
            }
            catch (Exception ex)
            {
                errors.Add(new DailyEntryImportErrorDto
                {
                    RowNumber = row.RowNumber,
                    Message = ex.Message
                });
            }
        }

        return new DailyEntryImportResultDto
        {
            ImportedCount = imported,
            FailedCount = errors.Count,
            Errors = errors.OrderBy(e => e.RowNumber).ToList()
        };
    }

    public async Task<DailyEntryExcelFileDto> ExportLedgerExcelAsync(
        DailyEntryLedgerExportRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await EnsureSiteExistsAsync(request.SiteId, cancellationToken);

        var q = _context.DailyEntries
            .AsNoTracking()
            .Include(e => e.MainLedger)
            .Include(e => e.SubLedger)
            .Where(e => e.SiteId == request.SiteId && !e.IsDeleted);

        if (request.DateFrom.HasValue)
            q = q.Where(e => e.EntryDate >= request.DateFrom.Value);
        if (request.DateTo.HasValue)
            q = q.Where(e => e.EntryDate <= request.DateTo.Value);

        var entries = await q
            .OrderBy(e => e.EntryDate)
            .ThenBy(e => e.CreatedAt)
            .Select(e => new LedgerExportRow
            {
                EntryDate = e.EntryDate,
                EntryType = e.EntryType,
                MainLedgerName = e.MainLedger.LedgerName,
                SubLedgerName = e.SubLedger.LedgerName,
                Description = e.Description,
                Amount = e.Amount
            })
            .ToListAsync(cancellationToken);

        var content = DailyEntryExcelParser.BuildExportWorkbook(entries);
        var suffix = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        return new DailyEntryExcelFileDto
        {
            Content = content,
            FileName = $"daily-entry-ledger-{suffix}.xlsx"
        };
    }

    private async Task<Guid> ResolveMainLedgerAsync(
        Guid siteId,
        string name,
        Dictionary<string, Guid> cache,
        CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(name, out var cached))
            return cached;

        var existing = await _context.MainLedgers
            .Where(m => m.SiteId == siteId && m.LedgerName.ToLower() == name.ToLower())
            .Select(m => m.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing != Guid.Empty)
        {
            cache[name] = existing;
            return existing;
        }

        var created = await _mainLedgerService.CreateAsync(new CreateMainLedgerDto
        {
            SiteId = siteId,
            LedgerName = name
        }, cancellationToken);
        cache[name] = created.Id;
        return created.Id;
    }

    private async Task<Guid> ResolveSubLedgerAsync(
        Guid mainLedgerId,
        string name,
        Dictionary<string, Guid> cache,
        string cacheKey,
        CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(cacheKey, out var cached))
            return cached;

        var existing = await _context.SubLedgers
            .Where(s => s.MainLedgerId == mainLedgerId && s.LedgerName.ToLower() == name.ToLower())
            .Select(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing != Guid.Empty)
        {
            cache[cacheKey] = existing;
            return existing;
        }

        var created = await _subLedgerService.CreateAsync(new CreateSubLedgerDto
        {
            MainLedgerId = mainLedgerId,
            LedgerName = name
        }, cancellationToken);
        cache[cacheKey] = created.Id;
        return created.Id;
    }

    private async Task EnsureSiteExistsAsync(Guid siteId, CancellationToken cancellationToken)
    {
        if (!await _context.Sites.AnyAsync(s => s.Id == siteId, cancellationToken))
            throw new InvalidOperationException("Site not found.");
    }
}
