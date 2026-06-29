using ABR.Application.DTOs.Accounting;
using ABR.Application.Interfaces;
using ABR.Domain.Entities;
using ABR.Infrastructure.Persistence;
using ABR.Infrastructure.Services.Accounting;
using ABR.Infrastructure.Services.MasterData;
using ClosedXML.Excel;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace ABR.Infrastructure.Tests;

public class DailyEntryExcelServiceTests
{
    [Fact]
    public void ParseImport_AcceptsValidSampleFormat()
    {
        var bytes = DailyEntryExcelParser.BuildSampleWorkbook();
        using var stream = new MemoryStream(bytes);

        var (validRows, errors) = DailyEntryExcelParser.ParseImport(stream);

        Assert.Equal(2, validRows.Count);
        Assert.Empty(errors);
        Assert.Equal("aavak", validRows[0].EntryType);
        Assert.Equal("javak", validRows[1].EntryType);
        Assert.Equal(100m, validRows[0].Amount);
        Assert.Equal(100m, validRows[1].Amount);
    }

    [Fact]
    public void ParseImport_RejectsCloseColumn()
    {
        var bytes = BuildWorkbook(ws =>
        {
            WriteHeaders(ws, 1, "DATE", "CLOSE", "A/J", "MAIN", "SUB", "REMARK", "CR", "DR");
            ws.Cell(2, 1).Value = "01-02-2026";
            ws.Cell(2, 3).Value = "A";
            ws.Cell(2, 4).Value = "UNN";
            ws.Cell(2, 6).Value = 100m;
        });

        using var stream = new MemoryStream(bytes);
        var ex = Assert.Throws<InvalidOperationException>(() => DailyEntryExcelParser.ParseImport(stream));
        Assert.Contains("CLOSE", ex.Message);
    }

    [Fact]
    public void ParseImport_RowWithBothCrAndDrFails()
    {
        var bytes = BuildWorkbook(ws =>
        {
            WriteHeaders(ws, 1);
            ws.Cell(2, 1).Value = "01-02-2026";
            ws.Cell(2, 2).Value = "A";
            ws.Cell(2, 3).Value = "UNN";
            ws.Cell(2, 4).Value = "PARTH";
            ws.Cell(2, 6).Value = 100m;
            ws.Cell(2, 7).Value = 50m;
        });

        using var stream = new MemoryStream(bytes);
        var (_, errors) = DailyEntryExcelParser.ParseImport(stream);

        Assert.Single(errors);
        Assert.Contains("CR or DR", errors[0].Message);
    }

    [Fact]
    public async Task ImportAsync_AutoCreatesMainAndSubLedgers()
    {
        await using var context = TestDbContextFactory.Create();
        var siteId = Guid.NewGuid();
        context.Sites.Add(new Site { Id = siteId, SiteName = "Test", IsActive = true });
        await context.SaveChangesAsync();

        var bytes = BuildWorkbook(ws =>
        {
            WriteHeaders(ws, 1);
            ws.Cell(2, 1).Value = "01-02-2026";
            ws.Cell(2, 2).Value = "A";
            ws.Cell(2, 3).Value = "UNN";
            ws.Cell(2, 4).Value = "PARTH";
            ws.Cell(2, 6).Value = 100m;
        });

        var service = CreateExcelService(context);
        using var stream = new MemoryStream(bytes);
        var result = await service.ImportAsync(siteId, stream, null);

        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Single(context.MainLedgers.Where(m => m.SiteId == siteId));
        Assert.Single(context.SubLedgers);
        Assert.Single(context.DailyEntries);
    }

    [Fact]
    public async Task ExportLedgerExcel_ComputesRunningBalance()
    {
        await using var context = TestDbContextFactory.Create();
        var siteId = Guid.NewGuid();
        var site = new Site { Id = siteId, SiteName = "Test", IsActive = true };
        var main = new MainLedger { SiteId = siteId, LedgerName = "UNN" };
        var sub = new SubLedger { MainLedger = main, LedgerName = "PARTH" };
        context.Sites.Add(site);
        context.MainLedgers.Add(main);
        context.SubLedgers.Add(sub);
        context.DailyEntries.AddRange(
            new DailyEntry
            {
                SiteId = siteId,
                EntryType = "aavak",
                EntryDate = new DateOnly(2026, 2, 1),
                MainLedger = main,
                SubLedger = sub,
                Amount = 100m,
                CashBank = "Cash"
            },
            new DailyEntry
            {
                SiteId = siteId,
                EntryType = "javak",
                EntryDate = new DateOnly(2026, 2, 1),
                MainLedger = main,
                SubLedger = sub,
                Amount = 100m,
                CashBank = "Cash"
            });
        await context.SaveChangesAsync();

        var service = CreateExcelService(context);
        var file = await service.ExportLedgerExcelAsync(new DailyEntryLedgerExportRequestDto { SiteId = siteId });

        using var workbook = new XLWorkbook(new MemoryStream(file.Content));
        var ws = workbook.Worksheet(1);
        Assert.Equal(100m, ws.Cell(2, 8).GetValue<decimal>());
        Assert.Equal(0m, ws.Cell(3, 8).GetValue<decimal>());
    }

    private static DailyEntryExcelService CreateExcelService(AbrDbContext context)
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var mainService = new MainLedgerService(context, cache);
        var subService = new SubLedgerService(context);
        var audit = new Mock<IAuditLogService>();
        return new DailyEntryExcelService(context, mainService, subService, audit.Object);
    }

    private static void WriteHeaders(IXLWorksheet ws, int row, params string[] headers)
    {
        for (var i = 0; i < headers.Length; i++)
            ws.Cell(row, i + 1).Value = headers[i];
    }

    private static void WriteHeaders(IXLWorksheet ws, int row)
        => WriteHeaders(ws, row, "DATE", "A/J", "MAIN", "SUB", "REMARK", "CR", "DR");

    private static byte[] BuildWorkbook(Action<IXLWorksheet> configure)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Import");
        configure(ws);
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }
}
