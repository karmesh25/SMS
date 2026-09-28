using System.Text.Json;

namespace ABR.Application.Common;

/// <summary>Parses the JWT <c>permissions</c> claim (camelCase or PascalCase).</summary>
public static class JwtPermissionClaims
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static bool HasAccess(string? permissionsJson, string module, PermissionLevel level)
    {
        if (string.IsNullOrWhiteSpace(permissionsJson) || string.IsNullOrWhiteSpace(module))
            return false;

        try
        {
            var permissions = JsonSerializer.Deserialize<List<PermissionClaim>>(permissionsJson, JsonOptions);
            var perm = permissions?.FirstOrDefault(p =>
                string.Equals(p.ModuleKey, module, StringComparison.OrdinalIgnoreCase));
            if (perm is null)
                return false;

            return level == PermissionLevel.Manage ? perm.CanManage : perm.CanView;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private sealed class PermissionClaim
    {
        public string ModuleKey { get; set; } = string.Empty;
        public bool CanView { get; set; }
        public bool CanManage { get; set; }
    }
}
