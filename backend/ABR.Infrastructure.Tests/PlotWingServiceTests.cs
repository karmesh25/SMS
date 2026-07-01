using ABR.Application.DTOs.MasterData;
using ABR.Domain.Entities;
using ABR.Infrastructure.Services.MasterData;
using Microsoft.EntityFrameworkCore;

namespace ABR.Infrastructure.Tests;

public class PlotWingServiceTests
{
    [Fact]
    public void GenerateFlats_CreatesPlotUnits_WhenIsPlot()
    {
        var wing = new Wing
        {
            Id = Guid.NewGuid(),
            WingName = "SCHEME-A",
            Floors = 1,
            FlatsPerFloor = 5,
            IsPlot = true
        };

        var flats = WingService.GenerateFlats(wing);

        Assert.Equal(5, flats.Count);
        Assert.All(flats, f =>
        {
            Assert.Equal("plot", f.FlatType);
            Assert.Equal(0m, f.Sqft);
            Assert.Equal("available", f.Status);
        });
        Assert.Equal("SCHEME-A1", flats[0].FlatNo);
        Assert.Equal("SCHEME-A5", flats[4].FlatNo);
    }

    [Fact]
    public async Task CreatePlotAsync_PersistsPlotWingAndUnits()
    {
        await using var context = TestDbContextFactory.Create();
        var site = new Site { SiteName = "Test Site", IsActive = true };
        context.Sites.Add(site);
        await context.SaveChangesAsync();

        var service = new WingService(context);
        var created = await service.CreatePlotAsync(new CreatePlotDto
        {
            SiteId = site.Id,
            PlotName = "PLOT-B",
            PlotCount = 3
        });

        Assert.True(created.IsPlot);
        Assert.Equal(3, created.FlatCount);
        Assert.Equal("PLOT-B", created.WingName);

        var units = await context.Flats.Where(f => f.WingId == created.Id).ToListAsync();
        Assert.Equal(3, units.Count);
        Assert.All(units, u => Assert.Equal("plot", u.FlatType));
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenPlotHasBookedUnit()
    {
        await using var context = TestDbContextFactory.Create();
        var site = new Site { SiteName = "Test Site", IsActive = true };
        context.Sites.Add(site);
        await context.SaveChangesAsync();

        var service = new WingService(context);
        var plot = await service.CreatePlotAsync(new CreatePlotDto
        {
            SiteId = site.Id,
            PlotName = "PLOT-C",
            PlotCount = 2
        });

        var flat = await context.Flats.FirstAsync(f => f.WingId == plot.Id);
        flat.Status = "booked";
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync(plot.Id));
    }

    [Fact]
    public void ResolveFloor_ReturnsZero_ForPlotUnits()
    {
        var wing = new Wing { WingName = "PLOT-A", Floors = 1, FlatsPerFloor = 4, IsPlot = true };
        var flat = new Flat { FlatNo = "PLOT-A2", FlatType = "plot", WingId = wing.Id };

        var floor = FlatService.ResolveFloor(flat, wing);
        Assert.Equal(0, floor);
    }
}
