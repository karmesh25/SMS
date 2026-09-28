using ABR.Application.Common;

namespace ABR.Application.Tests;

public class JwtPermissionClaimsTests
{
    private const string CamelCaseClaim =
        """[{"moduleKey":"wings","canView":true,"canManage":true},{"moduleKey":"dashboard","canView":true,"canManage":false}]""";

    private const string PascalCaseClaim =
        """[{"ModuleKey":"wings","CanView":true,"CanManage":true}]""";

    [Fact]
    public void HasAccess_CamelCaseClaim_AllowsWingsManage()
    {
        Assert.True(JwtPermissionClaims.HasAccess(CamelCaseClaim, AppModules.Wings, PermissionLevel.Manage));
        Assert.True(JwtPermissionClaims.HasAccess(CamelCaseClaim, AppModules.Wings, PermissionLevel.View));
    }

    [Fact]
    public void HasAccess_CamelCaseClaim_DeniesManageWithoutFlag()
    {
        Assert.False(JwtPermissionClaims.HasAccess(CamelCaseClaim, AppModules.Dashboard, PermissionLevel.Manage));
        Assert.True(JwtPermissionClaims.HasAccess(CamelCaseClaim, AppModules.Dashboard, PermissionLevel.View));
    }

    [Fact]
    public void HasAccess_PascalCaseClaim_StillWorks()
    {
        Assert.True(JwtPermissionClaims.HasAccess(PascalCaseClaim, AppModules.Wings, PermissionLevel.Manage));
    }

    [Fact]
    public void HasAccess_MissingModule_ReturnsFalse()
    {
        Assert.False(JwtPermissionClaims.HasAccess(CamelCaseClaim, AppModules.Banks, PermissionLevel.Manage));
    }

    [Fact]
    public void HasAccess_NullOrMalformed_ReturnsFalse()
    {
        Assert.False(JwtPermissionClaims.HasAccess(null, AppModules.Wings, PermissionLevel.Manage));
        Assert.False(JwtPermissionClaims.HasAccess("", AppModules.Wings, PermissionLevel.Manage));
        Assert.False(JwtPermissionClaims.HasAccess("not-json", AppModules.Wings, PermissionLevel.Manage));
    }
}
