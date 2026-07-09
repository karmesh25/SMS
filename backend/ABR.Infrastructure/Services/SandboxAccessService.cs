using ABR.Application.Interfaces;
using ABR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ABR.Infrastructure.Services;

public sealed class SandboxAccessService : ISandboxAccessService
{
    private readonly AbrDbContext _context;
    private readonly bool _isProduction;

    public SandboxAccessService(AbrDbContext context, IConfiguration configuration)
    {
        _context = context;
        _isProduction = string.Equals(
            configuration["ASPNETCORE_ENVIRONMENT"]
                ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? "Production",
            "Production",
            StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> IsSandboxSiteAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        return await _context.Sites
            .AsNoTracking()
            .Where(s => s.Id == siteId)
            .Select(s => s.IsSandbox)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> IsSandboxOnlyUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var accessibleSites = await _context.UserSiteAccesses
            .AsNoTracking()
            .Where(a => a.UserId == userId && a.CanWrite)
            .Join(
                _context.Sites.AsNoTracking(),
                access => access.SiteId,
                site => site.Id,
                (_, site) => site.IsSandbox)
            .ToListAsync(cancellationToken);

        return accessibleSites.Count > 0 && accessibleSites.All(isSandbox => isSandbox);
    }

    public async Task<bool> CanWriteToSiteAsync(
        Guid? userId,
        Guid siteId,
        bool isSuperAdmin,
        CancellationToken cancellationToken = default)
    {
        if (!_isProduction)
            return true;

        if (!await IsSandboxSiteAsync(siteId, cancellationToken))
            return true;

        if (userId is null)
            return false;

        if (isSuperAdmin)
            return false;

        return await IsSandboxOnlyUserAsync(userId.Value, cancellationToken);
    }
}
