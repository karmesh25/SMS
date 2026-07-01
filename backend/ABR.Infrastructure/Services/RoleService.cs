using ABR.Application.Common;
using ABR.Application.DTOs.Roles;
using ABR.Application.Interfaces;
using ABR.Domain.Entities;
using ABR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ABR.Infrastructure.Services;

public sealed class RoleService : IRoleService
{
    private readonly AbrDbContext _context;

    public RoleService(AbrDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<RoleDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _context.AppRoles
            .AsNoTracking()
            .Where(r => !r.IsDeleted)
            .Include(r => r.Permissions)
            .OrderBy(r => r.IsSystem ? 0 : 1)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);

        var userCounts = await _context.Users
            .Where(u => u.IsActive)
            .GroupBy(u => u.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count, cancellationToken);

        return roles.Select(r => MapRole(r, userCounts.GetValueOrDefault(r.Id))).ToList();
    }

    public async Task<RoleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await _context.AppRoles
            .AsNoTracking()
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);

        if (role is null)
            return null;

        var userCount = await _context.Users.CountAsync(u => u.RoleId == id && u.IsActive, cancellationToken);
        return MapRole(role, userCount);
    }

    public async Task<RoleDto> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        if (await _context.AppRoles.AnyAsync(r => r.Name == request.Name && !r.IsDeleted, cancellationToken))
            throw new InvalidOperationException("Role name already exists.");

        var role = new AppRole
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            IsSystem = false
        };

        _context.AppRoles.Add(role);
        await _context.SaveChangesAsync(cancellationToken);
        ApplyPermissions(role.Id, request.Permissions);
        await _context.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(role.Id, cancellationToken))!;
    }

    public async Task<RoleDto?> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var role = await _context.AppRoles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);

        if (role is null)
            return null;

        if (await _context.AppRoles.AnyAsync(r => r.Id != id && r.Name == request.Name && !r.IsDeleted, cancellationToken))
            throw new InvalidOperationException("Role name already exists.");

        role.Name = request.Name.Trim();
        role.Description = request.Description?.Trim();
        _context.RolePermissions.RemoveRange(role.Permissions);
        ApplyPermissions(role.Id, request.Permissions);

        var users = await _context.Users.Where(u => u.RoleId == id).ToListAsync(cancellationToken);
        foreach (var user in users)
            user.Role = role.Name;

        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await _context.AppRoles.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);
        if (role is null)
            return false;

        if (role.IsSystem)
            throw new InvalidOperationException("System roles cannot be deleted.");

        if (await _context.Users.AnyAsync(u => u.RoleId == id, cancellationToken))
            throw new InvalidOperationException("Cannot delete a role that is assigned to users.");

        role.IsDeleted = true;
        role.DeletedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private void ApplyPermissions(Guid roleId, List<RolePermissionDto> permissions)
    {
        var byModule = permissions
            .Where(p => AppModules.All.Contains(p.ModuleKey))
            .GroupBy(p => p.ModuleKey)
            .ToDictionary(g => g.Key, g => g.Last());

        foreach (var module in AppModules.All)
        {
            byModule.TryGetValue(module, out var perm);
            var canView = perm?.CanView ?? false;
            var canManage = perm?.CanManage ?? false;
            if (canManage)
                canView = true;

            _context.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                ModuleKey = module,
                CanView = canView,
                CanManage = canManage
            });
        }
    }

    private static RoleDto MapRole(AppRole role, int userCount) => new()
    {
        Id = role.Id,
        Name = role.Name,
        Description = role.Description,
        IsSystem = role.IsSystem,
        UserCount = userCount,
        Permissions = role.Permissions
            .OrderBy(p => Array.IndexOf(AppModules.All, p.ModuleKey))
            .Select(p => new RolePermissionDto
            {
                ModuleKey = p.ModuleKey,
                CanView = p.CanView,
                CanManage = p.CanManage
            }).ToList()
    };
}
