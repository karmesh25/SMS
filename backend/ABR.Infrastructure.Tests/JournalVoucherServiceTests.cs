using ABR.Application.DTOs.Accounting;
using ABR.Application.Interfaces;
using ABR.Domain.Entities;
using ABR.Infrastructure.Services.Accounting;
using Moq;

namespace ABR.Infrastructure.Tests;

public class JournalVoucherServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenTotalsMismatch_Throws()
    {
        await using var context = TestDbContextFactory.Create();
        var siteId = Guid.NewGuid();
        var site = new Site { Id = siteId, SiteName = "Test Site", IsActive = true };
        var main = new MainLedger { SiteId = siteId, LedgerName = "Main" };
        var sub1 = new SubLedger { MainLedger = main, LedgerName = "Sub 1" };
        var sub2 = new SubLedger { MainLedger = main, LedgerName = "Sub 2" };
        context.Sites.Add(site);
        context.MainLedgers.Add(main);
        context.SubLedgers.AddRange(sub1, sub2);
        await context.SaveChangesAsync();

        var audit = new Mock<IAuditLogService>();
        var service = new JournalVoucherService(context, audit.Object);

        var dto = new CreateJournalVoucherDto
        {
            SiteId = siteId,
            VoucherDate = new DateOnly(2026, 7, 8),
            Narration = "Mismatch",
            Lines =
            [
                new JournalVoucherLineUpsertDto { SubLedgerId = sub1.Id, EntryType = "dr", Amount = 100m, LineNo = 1 },
                new JournalVoucherLineUpsertDto { SubLedgerId = sub2.Id, EntryType = "cr", Amount = 80m, LineNo = 2 }
            ]
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(dto, null));
        Assert.Contains("Debit and Credit totals must match before saving.", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_GeneratesVoucherNumberPerSiteAndYear()
    {
        await using var context = TestDbContextFactory.Create();
        var siteId = Guid.NewGuid();
        var site = new Site { Id = siteId, SiteName = "Test Site", IsActive = true };
        var main = new MainLedger { SiteId = siteId, LedgerName = "Main" };
        var sub1 = new SubLedger { MainLedger = main, LedgerName = "Sub 1" };
        var sub2 = new SubLedger { MainLedger = main, LedgerName = "Sub 2" };
        context.Sites.Add(site);
        context.MainLedgers.Add(main);
        context.SubLedgers.AddRange(sub1, sub2);
        await context.SaveChangesAsync();

        var audit = new Mock<IAuditLogService>();
        var service = new JournalVoucherService(context, audit.Object);

        var first = await service.CreateAsync(new CreateJournalVoucherDto
        {
            SiteId = siteId,
            VoucherDate = new DateOnly(2026, 7, 8),
            Lines =
            [
                new JournalVoucherLineUpsertDto { SubLedgerId = sub1.Id, EntryType = "dr", Amount = 100m, LineNo = 1 },
                new JournalVoucherLineUpsertDto { SubLedgerId = sub2.Id, EntryType = "cr", Amount = 100m, LineNo = 2 }
            ]
        }, null);

        var second = await service.CreateAsync(new CreateJournalVoucherDto
        {
            SiteId = siteId,
            VoucherDate = new DateOnly(2026, 7, 9),
            Lines =
            [
                new JournalVoucherLineUpsertDto { SubLedgerId = sub1.Id, EntryType = "dr", Amount = 50m, LineNo = 1 },
                new JournalVoucherLineUpsertDto { SubLedgerId = sub2.Id, EntryType = "cr", Amount = 50m, LineNo = 2 }
            ]
        }, null);

        var nextYear = await service.CreateAsync(new CreateJournalVoucherDto
        {
            SiteId = siteId,
            VoucherDate = new DateOnly(2027, 1, 1),
            Lines =
            [
                new JournalVoucherLineUpsertDto { SubLedgerId = sub1.Id, EntryType = "dr", Amount = 20m, LineNo = 1 },
                new JournalVoucherLineUpsertDto { SubLedgerId = sub2.Id, EntryType = "cr", Amount = 20m, LineNo = 2 }
            ]
        }, null);

        Assert.Equal("JV-2026-00001", first.VoucherNo);
        Assert.Equal("JV-2026-00002", second.VoucherNo);
        Assert.Equal("JV-2027-00001", nextYear.VoucherNo);
    }
}
