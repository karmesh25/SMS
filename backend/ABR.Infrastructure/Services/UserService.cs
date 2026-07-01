using ABR.Application.Common;
using ABR.Application.DTOs.Auth;
using ABR.Application.DTOs.Users;
using ABR.Application.Interfaces;
using ABR.Domain.Entities;
using ABR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ABR.Infrastructure.Services;

public sealed class UserService : IUserService
{
    private static readonly string[] AdminModules =
    [
        AppModules.Sites, AppModules.Wings, AppModules.Conditions, AppModules.Ledgers,
        AppModules.Banks, AppModules.Brokers
    ];

    private readonly AbrDbContext _context;

    public UserService(AbrDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await QueryUsers().OrderBy(u => u.Username).ToListAsync(cancellationToken);
        return users.Select(MapUser).ToList();
    }

    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await QueryUsers().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        return user is null ? null : MapUser(user);
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (await _context.Users.AnyAsync(u => u.Username == request.Username, cancellationToken))
            throw new InvalidOperationException("Username already exists.");

        var role = await _context.AppRoles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == request.RoleId && !r.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Role not found.");

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12),
            RoleId = role.Id,
            Role = role.Name,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        AddSiteAccesses(user.Id, request.SiteIds, role.Permissions);
        await _context.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(user.Id, cancellationToken))!;
    }

    public async Task<UserDto?> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .Include(u => u.SiteAccesses)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user is null)
            return null;

        var role = await _context.AppRoles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == request.RoleId && !r.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Role not found.");

        user.Email = request.Email;
        user.RoleId = role.Id;
        user.Role = role.Name;

        _context.UserSiteAccesses.RemoveRange(user.SiteAccesses);
        AddSiteAccesses(user.Id, request.SiteIds, role.Permissions);
        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> ToggleActiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FindAsync([id], cancellationToken);
        if (user is null)
            return false;

        user.IsActive = !user.IsActive;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ForcePasswordResetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FindAsync([id], cancellationToken);
        if (user is null)
            return false;

        user.ForcePasswordChange = true;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private IQueryable<User> QueryUsers() =>
        _context.Users
            .Include(u => u.SiteAccesses).ThenInclude(sa => sa.Site)
            .Include(u => u.AppRole).ThenInclude(r => r.Permissions);

    private void AddSiteAccesses(Guid userId, IEnumerable<Guid> siteIds, IEnumerable<RolePermission> permissions)
    {
        var (canWrite, canDelete) = GetSiteAccessFlags(permissions);
        foreach (var siteId in siteIds.Distinct())
        {
            _context.UserSiteAccesses.Add(new UserSiteAccess
            {
                UserId = userId,
                SiteId = siteId,
                CanRead = true,
                CanWrite = canWrite,
                CanDelete = canDelete
            });
        }
    }

    private static (bool CanWrite, bool CanDelete) GetSiteAccessFlags(IEnumerable<RolePermission> permissions)
    {
        var list = permissions.ToList();
        var canWrite = list.Any(p => p.CanManage);
        var canDelete = list.Any(p => p.CanManage && AdminModules.Contains(p.ModuleKey));
        return (canWrite, canDelete);
    }

    private static UserDto MapUser(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        RoleId = user.RoleId,
        Role = user.Role,
        IsActive = user.IsActive,
        ForcePasswordChange = user.ForcePasswordChange,
        LastLoginAt = user.LastLoginAt,
        SiteAccess = user.SiteAccesses.Select(sa => new SiteAccessDto
        {
            SiteId = sa.SiteId,
            SiteName = sa.Site?.SiteName ?? string.Empty,
            CanRead = sa.CanRead,
            CanWrite = sa.CanWrite,
            CanDelete = sa.CanDelete
        }).ToList(),
        Permissions = user.AppRole?.Permissions.Select(p => new ModulePermissionDto
        {
            ModuleKey = p.ModuleKey,
            CanView = p.CanView,
            CanManage = p.CanManage
        }).ToList() ?? []
    };
}
