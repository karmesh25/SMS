using ABR.Application.Common;
using ABR.Infrastructure;

namespace ABR.Application.Tests;

public class PermissionSeedTests
{
    // Derive from the module registry so these don't drift when a module is added.
    private static readonly int ModuleCount = AppModules.All.Length;

    [Fact]
    public void SuperAdmin_HasEveryModule_ViewAndManage()
    {
        var (view, manage) = RoleSeeder.GetDefaultPermissions(SystemRoleNames.SuperAdmin);

        Assert.Equal(ModuleCount, view.Count);
        Assert.Equal(ModuleCount, manage.Count);
    }

    [Fact]
    public void Admin_HasAllModulesExceptUsersAndDevices()
    {
        var (view, manage) = RoleSeeder.GetDefaultPermissions(SystemRoleNames.Admin);

        Assert.Equal(ModuleCount - 2, view.Count);
        Assert.Equal(ModuleCount - 2, manage.Count);
        Assert.DoesNotContain(AppModules.Users, view);
        Assert.DoesNotContain(AppModules.Devices, view);
    }

    [Fact]
    public void OfficeStaff_HasCuratedOperationalModules()
    {
        var (view, manage) = RoleSeeder.GetDefaultPermissions(SystemRoleNames.OfficeStaff);

        var expectedView = new[]
        {
            AppModules.Dashboard, AppModules.Booking, AppModules.DailyEntry,
            AppModules.JournalVoucher, AppModules.Dastavej, AppModules.Vyaj, AppModules.Reports
        };
        var expectedManage = new[]
        {
            AppModules.Booking, AppModules.DailyEntry, AppModules.JournalVoucher,
            AppModules.Dastavej, AppModules.Vyaj
        };

        Assert.Equal(expectedView.OrderBy(m => m), view.OrderBy(m => m));
        Assert.Equal(expectedManage.OrderBy(m => m), manage.OrderBy(m => m));
    }

    [Fact]
    public void ViewOnly_HasReadOnlyCuratedModules()
    {
        var (view, manage) = RoleSeeder.GetDefaultPermissions(SystemRoleNames.ViewOnly);

        var expectedView = new[]
        {
            AppModules.Dashboard, AppModules.JournalVoucher, AppModules.Vyaj, AppModules.Reports
        };

        Assert.Equal(expectedView.OrderBy(m => m), view.OrderBy(m => m));
        Assert.Empty(manage);
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
