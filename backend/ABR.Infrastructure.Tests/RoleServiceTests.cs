using ABR.Application.Common;
using ABR.Application.DTOs.Roles;
using ABR.Domain.Entities;
using ABR.Infrastructure;
using ABR.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace ABR.Infrastructure.Tests;

public class RoleServiceTests
{
    [Fact]
    public async Task CreateAsync_PersistsCustomRoleWithPermissions()
    {
        await using var context = TestDbContextFactory.Create();
        await SeedSystemRolesAsync(context);

        var service = new RoleService(context);
        var created = await service.CreateAsync(new CreateRoleRequest
        {
            Name = "Ledger Clerk",
            Description = "Ledgers only",
            Permissions =
            [
                new RolePermissionDto { ModuleKey = AppModules.Ledgers, CanView = true, CanManage = true },
                new RolePermissionDto { ModuleKey = AppModules.Reports, CanView = true, CanManage = false }
            ]
        });

        Assert.Equal("Ledger Clerk", created.Name);
        Assert.False(created.IsSystem);
        Assert.True(created.Permissions.First(p => p.ModuleKey == AppModules.Ledgers).CanManage);
        Assert.True(created.Permissions.First(p => p.ModuleKey == AppModules.Reports).CanView);
        Assert.False(created.Permissions.First(p => p.ModuleKey == AppModules.Reports).CanManage);
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenSystemRole()
    {
        await using var context = TestDbContextFactory.Create();
        await SeedSystemRolesAsync(context);

        var service = new RoleService(context);
        var adminRole = (await service.GetAllAsync()).First(r => r.Name == SystemRoleNames.Admin);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync(adminRole.Id));
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenUsersAssigned()
    {
        await using var context = TestDbContextFactory.Create();
        await SeedSystemRolesAsync(context);

        var service = new RoleService(context);
        var custom = await service.CreateAsync(new CreateRoleRequest
        {
            Name = "Temp Role",
            Permissions = [new RolePermissionDto { ModuleKey = AppModules.Dashboard, CanView = true }]
        });

        context.Users.Add(new User
        {
            Username = "temp",
            Email = "temp@test.local",
            PasswordHash = "hash",
            Role = custom.Name,
            RoleId = custom.Id,
            IsActive = true
        });
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync(custom.Id));
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletes_WhenNoUsersAssigned()
    {
        await using var context = TestDbContextFactory.Create();
        await SeedSystemRolesAsync(context);

        var service = new RoleService(context);
        var custom = await service.CreateAsync(new CreateRoleRequest
        {
            Name = "Disposable",
            Permissions = [new RolePermissionDto { ModuleKey = AppModules.Dashboard, CanView = true }]
        });

        var deleted = await service.DeleteAsync(custom.Id);
        Assert.True(deleted);
        Assert.Null(await service.GetByIdAsync(custom.Id));
    }

    private static async Task SeedSystemRolesAsync(Persistence.AbrDbContext context)
    {
        await RoleSeeder.EnsureRolesAsync(context, Mock.Of<ILogger>());
    }
}
