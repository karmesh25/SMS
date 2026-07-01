using ABR.Application.Common;
using ABR.Application.DTOs.Roles;
using ABR.Domain.Entities;
using ABR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ABR.Infrastructure;

public static class RoleSeeder
{
    public static async Task EnsureRolesAsync(AbrDbContext context, ILogger logger, CancellationToken cancellationToken = default)
    {
        if (await context.AppRoles.AnyAsync(cancellationToken))
        {
            await BackfillUserRoleIdsAsync(context, cancellationToken);
            return;
        }

        logger.LogInformation("Seeding system roles and permissions...");

        var roleMap = new Dictionary<string, AppRole>();
        foreach (var name in SystemRoleNames.All)
        {
            var role = new AppRole
            {
                Name = name,
                Description = $"System role: {name}",
                IsSystem = true
            };
            context.AppRoles.Add(role);
            roleMap[name] = role;
        }

        await context.SaveChangesAsync(cancellationToken);

        foreach (var (roleName, role) in roleMap)
        {
            var (viewModules, manageModules) = GetDefaultPermissions(roleName);
            foreach (var module in AppModules.All)
            {
                context.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    ModuleKey = module,
                    CanView = viewModules.Contains(module),
                    CanManage = manageModules.Contains(module)
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        await BackfillUserRoleIdsAsync(context, cancellationToken);
        logger.LogInformation("System roles seeded.");
    }

    public static async Task BackfillUserRoleIdsAsync(AbrDbContext context, CancellationToken cancellationToken = default)
    {
        if (!await context.AppRoles.AnyAsync(cancellationToken))
            return;

        if (context.Database.IsRelational() &&
            context.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
        {
            await BackfillUserRoleIdsWithSqlAsync(context, cancellationToken);
            return;
        }

        await BackfillUserRoleIdsWithEfAsync(context, cancellationToken);
    }

    private static async Task BackfillUserRoleIdsWithSqlAsync(AbrDbContext context, CancellationToken cancellationToken)
    {
        // EF cannot reliably match NULL role_id in SQL when RoleId is mapped as non-nullable Guid.
        await context.Database.ExecuteSqlRawAsync(
            """
            UPDATE users AS u
            SET role_id = ar.id
            FROM app_roles AS ar
            WHERE ar.name = u.role
              AND ar.is_deleted = FALSE
              AND (
                u.role_id IS NULL
                OR u.role_id = '00000000-0000-0000-0000-000000000000'
                OR NOT EXISTS (SELECT 1 FROM app_roles x WHERE x.id = u.role_id)
              );
            """,
            cancellationToken);

        await context.Database.ExecuteSqlRawAsync(
            """
            UPDATE users AS u
            SET role_id = ar.id,
                role = ar.name
            FROM app_roles AS ar
            WHERE ar.name = 'ViewOnly'
              AND ar.is_deleted = FALSE
              AND (
                u.role_id IS NULL
                OR u.role_id = '00000000-0000-0000-0000-000000000000'
                OR NOT EXISTS (SELECT 1 FROM app_roles x WHERE x.id = u.role_id)
              );
            """,
            cancellationToken);
    }

    private static async Task BackfillUserRoleIdsWithEfAsync(AbrDbContext context, CancellationToken cancellationToken)
    {
        var roles = await context.AppRoles.ToDictionaryAsync(r => r.Name, r => r, cancellationToken);
        var validRoleIds = roles.Values.Select(r => r.Id).ToHashSet();
        var users = await context.Users.ToListAsync(cancellationToken);
        var updated = false;

        foreach (var user in users.Where(u => u.RoleId == Guid.Empty || !validRoleIds.Contains(u.RoleId)))
        {
            if (roles.TryGetValue(user.Role, out var role))
            {
                user.RoleId = role.Id;
                updated = true;
            }
            else if (roles.TryGetValue(SystemRoleNames.ViewOnly, out var fallback))
            {
                user.RoleId = fallback.Id;
                user.Role = fallback.Name;
                updated = true;
            }
        }

        if (updated)
            await context.SaveChangesAsync(cancellationToken);
    }

    public static (HashSet<string> View, HashSet<string> Manage) GetDefaultPermissions(string roleName)
    {
        var all = AppModules.All.ToHashSet();
        var adminModules = all.Except([AppModules.Users, AppModules.Devices]).ToHashSet();
        var officeView = new HashSet<string>
        {
            AppModules.Dashboard, AppModules.Booking, AppModules.DailyEntry,
            AppModules.Dastavej, AppModules.Vyaj, AppModules.Reports
        };
        var officeManage = new HashSet<string>
        {
            AppModules.Booking, AppModules.DailyEntry, AppModules.Dastavej, AppModules.Vyaj
        };
        var viewOnlyView = new HashSet<string>
        {
            AppModules.Dashboard, AppModules.Vyaj, AppModules.Reports
        };

        return roleName switch
        {
            SystemRoleNames.SuperAdmin => (all, all),
            SystemRoleNames.Admin => (adminModules, adminModules),
            SystemRoleNames.OfficeStaff => (officeView, officeManage),
            SystemRoleNames.ViewOnly => (viewOnlyView, []),
            _ => ([AppModules.Dashboard], [])
        };
    }

    public static List<RolePermissionDto> ToPermissionDtos(IEnumerable<RolePermission> permissions) =>
        permissions.Select(p => new RolePermissionDto
        {
            ModuleKey = p.ModuleKey,
            CanView = p.CanView,
            CanManage = p.CanManage
        }).ToList();
}
