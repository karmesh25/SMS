using ABR.Application.DTOs.MasterData;
using ABR.Application.Interfaces;
using ABR.Domain.Entities;
using ABR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace ABR.Infrastructure.Services.MasterData;

public sealed class SiteService : ISiteService
{
    private const string SitesCacheKey = "masterdata:sites:all";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(7);

    private readonly AbrDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly bool _isProduction;

    public SiteService(AbrDbContext context, IMemoryCache cache, IConfiguration configuration)
    {
        _context = context;
        _cache = cache;
        _isProduction = IsProductionEnvironment(configuration);
    }

    public async Task<IReadOnlyList<SiteDto>> GetAllAsync(
        Guid? userId = null,
        bool isSuperAdmin = false,
        CancellationToken cancellationToken = default)
    {
        if (isSuperAdmin || userId is null)
        {
            var allSites = await _cache.GetOrCreateAsync(SitesCacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                return await QuerySiteDtos().ToListAsync(cancellationToken);
            }) ?? [];

            return FilterSitesForUser(allSites, userId, isSuperAdmin);
        }

        var allowedSiteIds = await _context.UserSiteAccesses
            .AsNoTracking()
            .Where(a => a.UserId == userId.Value && a.CanRead)
            .Select(a => a.SiteId)
            .ToListAsync(cancellationToken);

        var userSites = await QuerySiteDtos()
            .Where(s => allowedSiteIds.Contains(s.Id))
            .ToListAsync(cancellationToken);

        return FilterSitesForUser(userSites, userId, isSuperAdmin);
    }

    private IReadOnlyList<SiteDto> FilterSitesForUser(
        IReadOnlyList<SiteDto> sites,
        Guid? userId,
        bool isSuperAdmin)
    {
        if (!_isProduction)
            return sites;

        if (sites.Count == 0)
            return sites;

        var sandboxOnly = !isSuperAdmin
            && userId is not null
            && sites.All(s => s.IsSandbox);

        if (sandboxOnly)
            return sites;

        return sites.Where(s => !s.IsSandbox).ToList();
    }

    private IQueryable<SiteDto> QuerySiteDtos() =>
        _context.Sites.AsNoTracking()
            .OrderBy(s => s.SiteName)
            .Select(s => new SiteDto
            {
                Id = s.Id,
                SiteName = s.SiteName,
                StartDate = s.StartDate,
                Address = s.Address,
                IsActive = s.IsActive,
                IsSandbox = s.IsSandbox
            });

    public async Task<SiteDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var site = await _context.Sites.FindAsync([id], cancellationToken);
        return site is null ? null : MapSite(site);
    }

    public async Task<SiteDto> CreateAsync(CreateSiteDto dto, CancellationToken cancellationToken = default)
    {
        var site = new Site
        {
            SiteName = dto.SiteName,
            StartDate = dto.StartDate,
            Address = dto.Address,
            IsActive = true
        };
        _context.Sites.Add(site);
        await _context.SaveChangesAsync(cancellationToken);
        _cache.Remove(SitesCacheKey);
        return MapSite(site);
    }

    public async Task<SiteDto?> UpdateAsync(Guid id, UpdateSiteDto dto, CancellationToken cancellationToken = default)
    {
        var site = await _context.Sites.FindAsync([id], cancellationToken);
        if (site is null) return null;

        site.SiteName = dto.SiteName;
        site.StartDate = dto.StartDate;
        site.Address = dto.Address;
        site.IsActive = dto.IsActive;
        await _context.SaveChangesAsync(cancellationToken);
        _cache.Remove(SitesCacheKey);
        return MapSite(site);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var site = await _context.Sites.FindAsync([id], cancellationToken);
        if (site is null) return false;

        site.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
        _cache.Remove(SitesCacheKey);
        return true;
    }

    private static SiteDto MapSite(Site s) => new()
    {
        Id = s.Id,
        SiteName = s.SiteName,
        StartDate = s.StartDate,
        Address = s.Address,
        IsActive = s.IsActive,
        IsSandbox = s.IsSandbox
    };

    private static bool IsProductionEnvironment(IConfiguration configuration)
    {
        var environment = configuration["ASPNETCORE_ENVIRONMENT"]
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "Production";

        return string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class WingService : IWingService
{
    private readonly AbrDbContext _context;

    public WingService(AbrDbContext context) => _context = context;

    public async Task<IReadOnlyList<WingDto>> GetBySiteAsync(Guid siteId, string? type = "wing", CancellationToken cancellationToken = default)
    {
        var query = _context.Wings.Where(w => w.SiteId == siteId);

        query = type?.ToLowerInvariant() switch
        {
            "plot" => query.Where(w => w.IsPlot),
            "all" => query,
            _ => query.Where(w => !w.IsPlot)
        };

        return await query
            .OrderBy(w => w.WingName)
            .Select(w => new WingDto
            {
                Id = w.Id,
                SiteId = w.SiteId,
                WingName = w.WingName,
                Floors = w.Floors,
                FlatsPerFloor = w.FlatsPerFloor,
                Shops = w.Shops,
                IsBungalow = w.IsBungalow,
                IsPlot = w.IsPlot,
                FlatCount = w.Flats.Count
            }).ToListAsync(cancellationToken);
    }

    public async Task<WingDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var wing = await _context.Wings.Include(w => w.Flats).FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        return wing is null ? null : MapWing(wing);
    }

    public async Task<WingDto> CreateAsync(CreateWingDto dto, CancellationToken cancellationToken = default)
    {
        var wing = new Wing
        {
            SiteId = dto.SiteId,
            WingName = dto.WingName.Trim().ToUpperInvariant(),
            Floors = dto.Floors,
            FlatsPerFloor = dto.FlatsPerFloor,
            Shops = dto.Shops,
            IsBungalow = dto.IsBungalow,
            IsPlot = false
        };

        _context.Wings.Add(wing);
        await _context.SaveChangesAsync(cancellationToken);

        var flats = GenerateFlats(wing);
        _context.Flats.AddRange(flats);
        await _context.SaveChangesAsync(cancellationToken);

        wing.Flats = flats;
        return MapWing(wing);
    }

    public async Task<WingDto> CreatePlotAsync(CreatePlotDto dto, CancellationToken cancellationToken = default)
    {
        var wing = new Wing
        {
            SiteId = dto.SiteId,
            WingName = dto.PlotName.Trim().ToUpperInvariant(),
            Floors = 1,
            FlatsPerFloor = dto.PlotCount,
            Shops = 0,
            IsBungalow = false,
            IsPlot = true
        };

        _context.Wings.Add(wing);
        await _context.SaveChangesAsync(cancellationToken);

        var flats = GenerateFlats(wing);
        _context.Flats.AddRange(flats);
        await _context.SaveChangesAsync(cancellationToken);

        wing.Flats = flats;
        return MapWing(wing);
    }

    public async Task<WingDto?> UpdatePlotAsync(Guid id, UpdatePlotDto dto, CancellationToken cancellationToken = default)
    {
        var wing = await _context.Wings.Include(w => w.Flats).FirstOrDefaultAsync(w => w.Id == id && w.IsPlot, cancellationToken);
        if (wing is null) return null;

        var oldName = wing.WingName;
        var newName = dto.PlotName.Trim().ToUpperInvariant();

        if (!string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase))
        {
            foreach (var flat in wing.Flats)
            {
                if (flat.FlatNo.StartsWith(oldName, StringComparison.OrdinalIgnoreCase))
                    flat.FlatNo = newName + flat.FlatNo[oldName.Length..];
            }
        }

        wing.WingName = newName;
        wing.FlatsPerFloor = dto.PlotCount;

        await _context.SaveChangesAsync(cancellationToken);
        return MapWing(wing);
    }

    public async Task<WingDto?> UpdateAsync(Guid id, UpdateWingDto dto, CancellationToken cancellationToken = default)
    {
        var wing = await _context.Wings.Include(w => w.Flats).FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (wing is null) return null;

        var oldWingName = wing.WingName;
        var newWingName = dto.WingName.Trim().ToUpperInvariant();

        if (!string.Equals(oldWingName, newWingName, StringComparison.OrdinalIgnoreCase))
        {
            foreach (var flat in wing.Flats)
            {
                if (flat.FlatNo.StartsWith(oldWingName, StringComparison.OrdinalIgnoreCase))
                    flat.FlatNo = newWingName + flat.FlatNo[oldWingName.Length..];
            }
        }

        wing.WingName = newWingName;
        wing.Floors = dto.Floors;
        wing.FlatsPerFloor = dto.FlatsPerFloor;
        wing.Shops = dto.Shops;
        wing.IsBungalow = dto.IsBungalow;
        await _context.SaveChangesAsync(cancellationToken);
        return MapWing(wing);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var wing = await _context.Wings
            .Include(w => w.Flats)
            .ThenInclude(f => f.Bookings)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (wing is null) return false;

        if (wing.Flats.Any(f => f.Status == "booked" || f.Bookings.Any(b => b.Status == "active")))
            throw new InvalidOperationException("Cannot delete wing with booked flats.");

        _context.Flats.RemoveRange(wing.Flats);
        _context.Wings.Remove(wing);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    internal static List<Flat> GenerateFlats(Wing wing)
    {
        var flats = new List<Flat>();

        if (wing.IsPlot)
        {
            for (var i = 1; i <= wing.FlatsPerFloor; i++)
            {
                flats.Add(new Flat
                {
                    WingId = wing.Id,
                    FlatNo = $"{wing.WingName}{i}",
                    Sqft = 0,
                    FlatType = "plot",
                    Status = "available"
                });
            }
            return flats;
        }

        if (wing.IsBungalow)
        {
            for (var i = 1; i <= wing.FlatsPerFloor; i++)
            {
                flats.Add(new Flat
                {
                    WingId = wing.Id,
                    FlatNo = $"{wing.WingName}{i}",
                    Sqft = 0,
                    FlatType = "bungalow",
                    Status = "available"
                });
            }
            return flats;
        }

        for (var floor = 1; floor <= wing.Floors; floor++)
        {
            var positionWidth = Math.Max(2, wing.FlatsPerFloor.ToString().Length);
            for (var pos = 1; pos <= wing.FlatsPerFloor; pos++)
            {
                flats.Add(new Flat
                {
                    WingId = wing.Id,
                    FlatNo = $"{wing.WingName}{floor}{pos.ToString().PadLeft(positionWidth, '0')}",
                    Sqft = 0,
                    FlatType = "flat",
                    Status = "available"
                });
            }
        }

        for (var shop = 1; shop <= wing.Shops; shop++)
        {
            flats.Add(new Flat
            {
                WingId = wing.Id,
                FlatNo = $"{wing.WingName}S{shop:D2}",
                Sqft = 0,
                FlatType = "shop",
                Status = "available"
            });
        }

        return flats;
    }

    private static WingDto MapWing(Wing w) => new()
    {
        Id = w.Id,
        SiteId = w.SiteId,
        WingName = w.WingName,
        Floors = w.Floors,
        FlatsPerFloor = w.FlatsPerFloor,
        Shops = w.Shops,
        IsBungalow = w.IsBungalow,
        IsPlot = w.IsPlot,
        FlatCount = w.Flats.Count
    };
}

public sealed class FlatService : IFlatService
{
    private readonly AbrDbContext _context;

    public FlatService(AbrDbContext context) => _context = context;

    public async Task<FlatGridDto> GetGridByWingAsync(Guid wingId, CancellationToken cancellationToken = default)
    {
        var wing = await _context.Wings
            .Include(w => w.Flats)
            .FirstOrDefaultAsync(w => w.Id == wingId, cancellationToken)
            ?? throw new KeyNotFoundException("Wing not found.");

        var flats = wing.Flats.OrderBy(f => f.FlatNo).Select(f => MapFlat(f, wing)).ToList();

        return new FlatGridDto
        {
            WingId = wing.Id,
            WingName = wing.WingName,
            Floors = wing.Floors,
            FlatsPerFloor = wing.FlatsPerFloor,
            IsBungalow = wing.IsBungalow,
            IsPlot = wing.IsPlot,
            BookedCount = flats.Count(f => f.Status == "booked"),
            AvailableCount = flats.Count(f => f.Status == "available"),
            CancelledCount = flats.Count(f => f.Status == "cancelled"),
            Flats = flats
        };
    }

    public async Task<IReadOnlyList<FlatDto>> GetByWingAsync(Guid wingId, CancellationToken cancellationToken = default)
    {
        var wing = await _context.Wings
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == wingId, cancellationToken)
            ?? throw new KeyNotFoundException("Wing not found.");

        var flats = await _context.Flats
            .Where(f => f.WingId == wingId)
            .OrderBy(f => f.FlatNo)
            .ToListAsync(cancellationToken);

        return flats.Select(f => MapFlat(f, wing)).ToList();
    }

    internal static FlatDto MapFlat(Flat f, Wing wing)
    {
        return new FlatDto
        {
            Id = f.Id,
            WingId = f.WingId,
            FlatNo = f.FlatNo,
            Sqft = f.Sqft,
            FlatType = f.FlatType,
            Status = f.Status,
            Floor = ResolveFloor(f, wing)
        };
    }

    internal static int ResolveFloor(Flat flat, Wing wing)
    {
        var flatType = flat.FlatType?.ToLowerInvariant();
        if (wing.IsPlot || wing.IsBungalow || flatType is "shop" or "bungalow" or "plot")
            return 0;

        var suffix = ExtractTowerSuffix(flat.FlatNo, wing);
        if (suffix is null)
            return 0;

        var maxFloorDigits = wing.Floors.ToString().Length;
        for (var floorDigits = maxFloorDigits; floorDigits >= 1; floorDigits--)
        {
            if (suffix.Length <= floorDigits)
                continue;

            if (!int.TryParse(suffix[..floorDigits], out var floor))
                continue;

            if (floor < 1 || floor > wing.Floors)
                continue;

            var positionPart = suffix[floorDigits..];
            if (positionPart.Length == 0 || !positionPart.All(char.IsDigit))
                continue;

            if (!int.TryParse(positionPart, out var position))
                continue;

            if (position >= 1 && position <= wing.FlatsPerFloor)
                return floor;
        }

        return 0;
    }

    private static string? ExtractTowerSuffix(string flatNo, Wing wing)
    {
        if (flatNo.StartsWith(wing.WingName, StringComparison.OrdinalIgnoreCase))
        {
            var suffix = flatNo[wing.WingName.Length..];
            return suffix.Length > 0 && suffix[0] != 'S' ? suffix : null;
        }

        var numericSuffix = new string(flatNo.SkipWhile(c => !char.IsDigit(c)).TakeWhile(char.IsDigit).ToArray());
        return numericSuffix.Length > 0 ? numericSuffix : null;
    }
}
