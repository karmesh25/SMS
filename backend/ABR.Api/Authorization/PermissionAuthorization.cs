using ABR.Application.Common;
using Microsoft.AspNetCore.Authorization;

namespace ABR.Api.Authorization;

public sealed class PermissionRequirement(string module, PermissionLevel level) : IAuthorizationRequirement
{
    public string Module { get; } = module;
    public PermissionLevel Level { get; } = level;
}

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var role = context.User.FindFirst("role")?.Value
            ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        if (string.Equals(role, SystemRoleNames.SuperAdmin, StringComparison.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var permissionsClaim = context.User.FindFirst("permissions")?.Value;
        if (string.IsNullOrWhiteSpace(permissionsClaim))
            return Task.CompletedTask;

        try
        {
            var permissions = System.Text.Json.JsonSerializer.Deserialize<List<PermissionClaim>>(permissionsClaim);
            var perm = permissions?.FirstOrDefault(p => p.ModuleKey == requirement.Module);
            if (perm is null)
                return Task.CompletedTask;

            var allowed = requirement.Level == PermissionLevel.Manage
                ? perm.CanManage
                : perm.CanView;

            if (allowed)
                context.Succeed(requirement);
        }
        catch
        {
            // Deny on malformed claim
        }

        return Task.CompletedTask;
    }

    private sealed class PermissionClaim
    {
        public string ModuleKey { get; set; } = string.Empty;
        public bool CanView { get; set; }
        public bool CanManage { get; set; }
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute, IAuthorizationRequirementData
{
    public RequirePermissionAttribute(string module, PermissionLevel level)
    {
        Module = module;
        Level = level;
    }

    public string Module { get; }
    public PermissionLevel Level { get; }

    public IEnumerable<IAuthorizationRequirement> GetRequirements()
    {
        yield return new PermissionRequirement(Module, Level);
    }
}
