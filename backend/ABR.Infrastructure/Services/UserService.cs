using ABR.Application.DTOs.Auth;
using ABR.Application.DTOs.Users;
using ABR.Application.Interfaces;
using ABR.Domain.Entities;
using ABR.Domain.Enums;
using ABR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ABR.Infrastructure.Services;

public sealed class UserService : IUserService
{
    private readonly AbrDbContext _context;

    public UserService(AbrDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _context.Users
            .Include(u => u.SiteAccesses)
            .ThenInclude(sa => sa.Site)
            .OrderBy(u => u.Username)
            .ToListAsync(cancellationToken);

        return users.Select(MapUser).ToList();
    }

    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .Include(u => u.SiteAccesses)
            .ThenInclude(sa => sa.Site)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        return user is null ? null : MapUser(user);
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (await _context.Users.AnyAsync(u => u.Username == request.Username, cancellationToken))
            throw new InvalidOperationException("Username already exists.");

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12),
            Role = request.Role,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        foreach (var siteId in request.SiteIds.Distinct())
        {
            _context.UserSiteAccesses.Add(new UserSiteAccess
            {
                UserId = user.Id,
                SiteId = siteId,
                CanRead = true,
                CanWrite = request.Role is nameof(UserRole.SuperAdmin) or nameof(UserRole.Admin) or nameof(UserRole.OfficeStaff),
                CanDelete = request.Role is nameof(UserRole.SuperAdmin) or nameof(UserRole.Admin)
            });
        }

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

        user.Email = request.Email;
        user.Role = request.Role;

        _context.UserSiteAccesses.RemoveRange(user.SiteAccesses);
        foreach (var siteId in request.SiteIds.Distinct())
        {
            _context.UserSiteAccesses.Add(new UserSiteAccess
            {
                UserId = user.Id,
                SiteId = siteId,
                CanRead = true,
                CanWrite = request.Role is nameof(UserRole.SuperAdmin) or nameof(UserRole.Admin) or nameof(UserRole.OfficeStaff),
                CanDelete = request.Role is nameof(UserRole.SuperAdmin) or nameof(UserRole.Admin)
            });
        }

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

    private static UserDto MapUser(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
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
        }).ToList()
    };
}
