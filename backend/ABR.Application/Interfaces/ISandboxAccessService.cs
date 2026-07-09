namespace ABR.Application.Interfaces;

public interface ISandboxAccessService
{
    Task<bool> IsSandboxSiteAsync(Guid siteId, CancellationToken cancellationToken = default);

    Task<bool> IsSandboxOnlyUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> CanWriteToSiteAsync(
        Guid? userId,
        Guid siteId,
        bool isSuperAdmin,
        CancellationToken cancellationToken = default);
}
