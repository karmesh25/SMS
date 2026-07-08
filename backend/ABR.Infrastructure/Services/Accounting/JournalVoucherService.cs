using System.Text.Json;
using ABR.Application.DTOs.Accounting;
using ABR.Application.Interfaces;
using ABR.Domain.Entities;
using ABR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ABR.Infrastructure.Services.Accounting;

public sealed class JournalVoucherService : IJournalVoucherService
{
    private const string TotalsMismatchMessage = "Debit and Credit totals must match before saving.";

    private readonly AbrDbContext _context;
    private readonly IAuditLogService _auditLog;

    public JournalVoucherService(AbrDbContext context, IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    public async Task<PagedJournalVouchersDto> GetListAsync(JournalVoucherFilterDto filter, CancellationToken cancellationToken = default)
    {
        var q = BaseQuery().Where(v => v.SiteId == filter.SiteId && !v.IsDeleted);
        if (filter.DateFrom.HasValue)
            q = q.Where(v => v.VoucherDate >= filter.DateFrom.Value);
        if (filter.DateTo.HasValue)
            q = q.Where(v => v.VoucherDate <= filter.DateTo.Value);

        var total = await q.CountAsync(cancellationToken);
        var vouchers = await q.OrderByDescending(v => v.VoucherDate).ThenByDescending(v => v.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync(cancellationToken);

        return new PagedJournalVouchersDto
        {
            Items = vouchers.Select(MapVoucher).ToList(),
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<JournalVoucherDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await BaseQuery().FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted, cancellationToken);
        return entity is null ? null : MapVoucher(entity);
    }

    public async Task<JournalVoucherDto> CreateAsync(CreateJournalVoucherDto dto, Guid? userId, CancellationToken cancellationToken = default)
    {
        var (totalDebit, totalCredit) = ValidateAndComputeTotals(dto.Lines);
        var voucherNo = await GenerateVoucherNoAsync(dto.SiteId, dto.VoucherDate, cancellationToken);

        var entity = new JournalVoucher
        {
            SiteId = dto.SiteId,
            VoucherNo = voucherNo,
            VoucherDate = dto.VoucherDate,
            Narration = dto.Narration,
            TotalDebit = totalDebit,
            TotalCredit = totalCredit,
            Lines = dto.Lines
                .OrderBy(l => l.LineNo)
                .Select(l => new JournalVoucherLine
                {
                    SubLedgerId = l.SubLedgerId,
                    EntryType = l.EntryType,
                    Amount = l.Amount,
                    LineNo = l.LineNo
                })
                .ToList()
        };

        _context.JournalVouchers.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditLog.LogAsync(userId, "Create", "journal_vouchers", entity.Id, newValues: JsonSerializer.Serialize(dto), cancellationToken: cancellationToken);
        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<JournalVoucherDto?> UpdateAsync(Guid id, UpdateJournalVoucherDto dto, Guid? userId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.JournalVouchers
            .Include(v => v.Lines)
            .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted, cancellationToken);
        if (entity is null)
            return null;

        var oldValues = JsonSerializer.Serialize(new
        {
            entity.VoucherDate,
            entity.Narration,
            entity.TotalDebit,
            entity.TotalCredit,
            Lines = entity.Lines.Select(l => new { l.SubLedgerId, l.EntryType, l.Amount, l.LineNo })
        });

        var (totalDebit, totalCredit) = ValidateAndComputeTotals(dto.Lines);

        entity.VoucherDate = dto.VoucherDate;
        entity.Narration = dto.Narration;
        entity.TotalDebit = totalDebit;
        entity.TotalCredit = totalCredit;

        _context.JournalVoucherLines.RemoveRange(entity.Lines);
        entity.Lines = dto.Lines.OrderBy(l => l.LineNo).Select(l => new JournalVoucherLine
        {
            SubLedgerId = l.SubLedgerId,
            EntryType = l.EntryType,
            Amount = l.Amount,
            LineNo = l.LineNo
        }).ToList();

        await _context.SaveChangesAsync(cancellationToken);
        await _auditLog.LogAsync(userId, "Update", "journal_vouchers", entity.Id, oldValues, JsonSerializer.Serialize(dto), cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid? userId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.JournalVouchers.FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted, cancellationToken);
        if (entity is null)
            return false;

        entity.IsDeleted = true;
        entity.DeletedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        await _auditLog.LogAsync(userId, "Delete", "journal_vouchers", id, cancellationToken: cancellationToken);
        return true;
    }

    public async Task<DailyEntryExcelFileDto> ExportLedgerExcelAsync(JournalVoucherLedgerExportRequestDto request, CancellationToken cancellationToken = default)
    {
        var rows = await QueryExportRows(request, cancellationToken);
        var content = JournalVoucherExportBuilder.BuildExcel(rows);
        return new DailyEntryExcelFileDto
        {
            Content = content,
            FileName = $"journal-voucher-ledger-{DateOnly.FromDateTime(DateTime.UtcNow):yyyy-MM-dd}.xlsx"
        };
    }

    public async Task<DailyEntryExcelFileDto> ExportLedgerPdfAsync(JournalVoucherLedgerExportRequestDto request, CancellationToken cancellationToken = default)
    {
        var siteName = await _context.Sites.Where(s => s.Id == request.SiteId).Select(s => s.SiteName).FirstAsync(cancellationToken);
        var rows = await QueryExportRows(request, cancellationToken);
        var content = JournalVoucherExportBuilder.BuildPdf(rows, siteName);
        return new DailyEntryExcelFileDto
        {
            Content = content,
            FileName = $"journal-voucher-ledger-{DateOnly.FromDateTime(DateTime.UtcNow):yyyy-MM-dd}.pdf",
            ContentType = "application/pdf"
        };
    }

    private IQueryable<JournalVoucher> BaseQuery() =>
        _context.JournalVouchers
            .Include(v => v.Lines)
            .ThenInclude(l => l.SubLedger)
            .ThenInclude(s => s.MainLedger);

    private static JournalVoucherDto MapVoucher(JournalVoucher voucher) => new()
    {
        Id = voucher.Id,
        SiteId = voucher.SiteId,
        VoucherNo = voucher.VoucherNo,
        VoucherDate = voucher.VoucherDate,
        Narration = voucher.Narration,
        TotalDebit = voucher.TotalDebit,
        TotalCredit = voucher.TotalCredit,
        Lines = voucher.Lines.OrderBy(l => l.LineNo).Select(l => new JournalVoucherLineDto
        {
            Id = l.Id,
            SubLedgerId = l.SubLedgerId,
            EntryType = l.EntryType,
            Amount = l.Amount,
            LineNo = l.LineNo,
            SubLedgerName = l.SubLedger.LedgerName,
            MainLedgerName = l.SubLedger.MainLedger.LedgerName
        }).ToList()
    };

    private static (decimal TotalDebit, decimal TotalCredit) ValidateAndComputeTotals(IReadOnlyList<JournalVoucherLineUpsertDto> lines)
    {
        if (lines.Count < 2)
            throw new InvalidOperationException("At least 2 lines are required.");

        var debit = lines.Where(l => l.EntryType == "dr").Sum(l => l.Amount);
        var credit = lines.Where(l => l.EntryType == "cr").Sum(l => l.Amount);
        if (debit != credit)
            throw new InvalidOperationException(TotalsMismatchMessage);

        return (debit, credit);
    }

    private async Task<string> GenerateVoucherNoAsync(Guid siteId, DateOnly voucherDate, CancellationToken cancellationToken)
    {
        var year = voucherDate.Year;
        var prefix = $"JV-{year}-";
        var maxNo = await _context.JournalVouchers
            .Where(v => v.SiteId == siteId && v.VoucherDate.Year == year && v.VoucherNo.StartsWith(prefix))
            .Select(v => v.VoucherNo)
            .ToListAsync(cancellationToken);

        var current = 0;
        foreach (var voucherNo in maxNo)
        {
            var suffix = voucherNo[prefix.Length..];
            if (int.TryParse(suffix, out var number) && number > current)
                current = number;
        }

        return $"{prefix}{(current + 1):D5}";
    }

    private async Task<IReadOnlyList<JournalVoucherExportRow>> QueryExportRows(JournalVoucherLedgerExportRequestDto request, CancellationToken cancellationToken)
    {
        var q = BaseQuery().Where(v => v.SiteId == request.SiteId && !v.IsDeleted);
        if (request.DateFrom.HasValue)
            q = q.Where(v => v.VoucherDate >= request.DateFrom.Value);
        if (request.DateTo.HasValue)
            q = q.Where(v => v.VoucherDate <= request.DateTo.Value);

        var vouchers = await q.OrderBy(v => v.VoucherDate).ThenBy(v => v.VoucherNo).ToListAsync(cancellationToken);
        return vouchers.SelectMany(v => v.Lines.OrderBy(l => l.LineNo).Select(l => new JournalVoucherExportRow
        {
            VoucherDate = v.VoucherDate,
            VoucherNo = v.VoucherNo,
            Narration = v.Narration,
            LineNo = l.LineNo,
            EntryType = l.EntryType,
            Amount = l.Amount,
            MainLedgerName = l.SubLedger.MainLedger.LedgerName,
            SubLedgerName = l.SubLedger.LedgerName
        })).ToList();
    }
}
