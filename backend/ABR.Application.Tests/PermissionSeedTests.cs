using ABR.Application.Common;
using ABR.Infrastructure;

namespace ABR.Application.Tests;

public class PermissionSeedTests
{
    [Theory]
    [InlineData(SystemRoleNames.SuperAdmin, 14, 14)]
    [InlineData(SystemRoleNames.Admin, 12, 12)]
    [InlineData(SystemRoleNames.OfficeStaff, 6, 4)]
    [InlineData(SystemRoleNames.ViewOnly, 3, 0)]
    public void SeededRoles_MatchLegacyModuleCounts(string roleName, int expectedViewCount, int expectedManageCount)
    {
        var (view, manage) = RoleSeeder.GetDefaultPermissions(roleName);

        Assert.Equal(expectedViewCount, view.Count);
        Assert.Equal(expectedManageCount, manage.Count);
    }

    [Fact]
    public void AdminRole_CannotManageUsersOrDevices()
    {
        var (_, manage) = RoleSeeder.GetDefaultPermissions(SystemRoleNames.Admin);

        Assert.DoesNotContain(AppModules.Users, manage);
        Assert.DoesNotContain(AppModules.Devices, manage);
    }

    [Fact]
    public void ViewOnlyRole_HasNoManagePermissions()
    {
        var (view, manage) = RoleSeeder.GetDefaultPermissions(SystemRoleNames.ViewOnly);

        Assert.Contains(AppModules.Vyaj, view);
        Assert.Empty(manage);
    }
}
