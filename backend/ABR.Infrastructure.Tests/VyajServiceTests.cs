using ABR.Application.DTOs.Vyaj;
using ABR.Domain.Entities;
using ABR.Infrastructure.Services.Vyaj;
using Microsoft.EntityFrameworkCore;

namespace ABR.Infrastructure.Tests;

public class VyajServiceTests
{
    [Fact]
    public async Task GetPartiesAsync_FiltersBySite()
    {
        await using var context = TestDbContextFactory.Create();
        var siteA = Guid.NewGuid();
        var siteB = Guid.NewGuid();

        context.Sites.AddRange(
            new Site { Id = siteA, SiteName = "A", IsActive = true },
            new Site { Id = siteB, SiteName = "B", IsActive = true });
        context.VyajParties.AddRange(
            new VyajParty { SiteId = siteA, Name = "Party A" },
            new VyajParty { SiteId = siteB, Name = "Party B" });
        await context.SaveChangesAsync();

        var service = new VyajService(context);
        var parties = await service.GetPartiesAsync(siteA);

        Assert.Single(parties);
        Assert.Equal("Party A", parties[0].Name);
    }

    [Fact]
    public async Task DeletePartyAsync_SoftDeletesPartyAndChildren()
    {
        await using var context = TestDbContextFactory.Create();
        var siteId = Guid.NewGuid();
        context.Sites.Add(new Site { Id = siteId, SiteName = "Tapi", IsActive = true });

        var partyId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        context.VyajParties.Add(new VyajParty { Id = partyId, SiteId = siteId, Name = "Test" });
        context.VyajEntries.Add(new VyajEntry
        {
            Id = entryId,
            PartyId = partyId,
            Principal = 10_000,
            RatePercent = 2,
            RateBasis = "month",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });
        context.VyajPayments.Add(new VyajPayment
        {
            Id = paymentId,
            EntryId = entryId,
            Amount = 100,
            PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PaymentType = "interest"
        });
        await context.SaveChangesAsync();

        var service = new VyajService(context);
        await service.DeletePartyAsync(partyId);

        var deletedParty = await context.VyajParties.FindAsync(partyId);
        var deletedEntry = await context.VyajEntries.FindAsync(entryId);
        var deletedPayment = await context.VyajPayments.FindAsync(paymentId);

        Assert.NotNull(deletedParty);
        Assert.True(deletedParty!.IsDeleted);
        Assert.NotNull(deletedEntry);
        Assert.True(deletedEntry!.IsDeleted);
        Assert.NotNull(deletedPayment);
        Assert.True(deletedPayment!.IsDeleted);
    }

    [Fact]
    public async Task GetPartyDetailAsync_ExcludesClosedEntriesFromTotals()
    {
        await using var context = TestDbContextFactory.Create();
        var siteId = Guid.NewGuid();
        context.Sites.Add(new Site { Id = siteId, SiteName = "Tapi", IsActive = true });

        var party = new VyajParty { SiteId = siteId, Name = "Lender" };
        party.Entries.Add(new VyajEntry
        {
            Principal = 100_000,
            RatePercent = 1,
            RateBasis = "flat",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            IsClosed = false
        });
        party.Entries.Add(new VyajEntry
        {
            Principal = 50_000,
            RatePercent = 1,
            RateBasis = "flat",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            IsClosed = true
        });
        context.VyajParties.Add(party);
        await context.SaveChangesAsync();

        var service = new VyajService(context);
        var detail = await service.GetPartyDetailAsync(party.Id);

        Assert.Equal(1_000, detail.TotalVyajDue);
        Assert.Equal(100_000, detail.TotalPrincipalDue);
        Assert.Equal(2, detail.Entries.Count);
    }

    [Fact]
    public async Task CreatePaymentAsync_ReducesVyajDue()
    {
        await using var context = TestDbContextFactory.Create();
        var siteId = Guid.NewGuid();
        var partyId = Guid.NewGuid();
        var entryId = Guid.NewGuid();

        context.Sites.Add(new Site { Id = siteId, SiteName = "Tapi", IsActive = true });
        context.VyajParties.Add(new VyajParty { Id = partyId, SiteId = siteId, Name = "Borrower" });
        context.VyajEntries.Add(new VyajEntry
        {
            Id = entryId,
            PartyId = partyId,
            Principal = 100_000,
            RatePercent = 1,
            RateBasis = "flat",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });
        await context.SaveChangesAsync();

        var service = new VyajService(context);
        await service.CreatePaymentAsync(new CreateVyajPaymentDto
        {
            EntryId = entryId,
            Amount = 300,
            PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PaymentType = "interest"
        });

        var detail = await service.GetPartyDetailAsync(partyId);
        Assert.Equal(700, detail.TotalVyajDue);
    }
}
