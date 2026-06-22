using ABR.Domain.Entities;
using ABR.Infrastructure.Services.Accounting;
using Moq;
using ABR.Application.Interfaces;

namespace ABR.Infrastructure.Tests;

public class DailyEntryServiceTests
{
    [Fact]
    public async Task GetProfitAsync_ComputesAavakMinusJavak()
    {
        await using var context = TestDbContextFactory.Create();
        var siteId = Guid.NewGuid();
        var mainLedgerId = Guid.NewGuid();
        var subLedgerId = Guid.NewGuid();

        context.DailyEntries.AddRange(
            new DailyEntry { SiteId = siteId, EntryType = "aavak", Amount = 150_000, MainLedgerId = mainLedgerId, SubLedgerId = subLedgerId, EntryDate = DateOnly.FromDateTime(DateTime.UtcNow) },
            new DailyEntry { SiteId = siteId, EntryType = "aavak", Amount = 50_000, MainLedgerId = mainLedgerId, SubLedgerId = subLedgerId, EntryDate = DateOnly.FromDateTime(DateTime.UtcNow) },
            new DailyEntry { SiteId = siteId, EntryType = "javak", Amount = 80_000, MainLedgerId = mainLedgerId, SubLedgerId = subLedgerId, EntryDate = DateOnly.FromDateTime(DateTime.UtcNow) });
        await context.SaveChangesAsync();

        var audit = new Mock<IAuditLogService>();
        var service = new DailyEntryService(context, audit.Object);

        var profit = await service.GetProfitAsync(siteId);

        Assert.Equal(200_000m, profit.TotalAavak);
        Assert.Equal(80_000m, profit.TotalJavak);
        Assert.Equal(120_000m, profit.Profit);
    }

    [Fact]
    public async Task GetProfitAsync_IgnoresDeletedEntries()
    {
        await using var context = TestDbContextFactory.Create();
        var siteId = Guid.NewGuid();
        var mainLedgerId = Guid.NewGuid();
        var subLedgerId = Guid.NewGuid();

        context.DailyEntries.AddRange(
            new DailyEntry { SiteId = siteId, EntryType = "aavak", Amount = 100_000, MainLedgerId = mainLedgerId, SubLedgerId = subLedgerId, EntryDate = DateOnly.FromDateTime(DateTime.UtcNow) },
            new DailyEntry { SiteId = siteId, EntryType = "aavak", Amount = 999_999, MainLedgerId = mainLedgerId, SubLedgerId = subLedgerId, EntryDate = DateOnly.FromDateTime(DateTime.UtcNow), IsDeleted = true });
        await context.SaveChangesAsync();

        var audit = new Mock<IAuditLogService>();
        var service = new DailyEntryService(context, audit.Object);

        var profit = await service.GetProfitAsync(siteId);

        Assert.Equal(100_000m, profit.TotalAavak);
        Assert.Equal(100_000m, profit.Profit);
    }

    [Fact]
    public async Task GetProfitAsync_ReturnsZeroWhenNoEntries()
    {
        await using var context = TestDbContextFactory.Create();
        var audit = new Mock<IAuditLogService>();
        var service = new DailyEntryService(context, audit.Object);

        var profit = await service.GetProfitAsync(Guid.NewGuid());

        Assert.Equal(0m, profit.TotalAavak);
        Assert.Equal(0m, profit.TotalJavak);
        Assert.Equal(0m, profit.Profit);
    }
}
