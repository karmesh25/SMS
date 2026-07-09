using System.Security.Claims;
using System.Text.Json;
using ABR.Application.Common;
using ABR.Application.Interfaces;

namespace ABR.Api.Middleware;

public sealed class SandboxWriteGuardMiddleware(
    RequestDelegate next,
    IHostEnvironment environment)
{
    private static readonly HashSet<string> WriteMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Post,
        HttpMethods.Put,
        HttpMethods.Delete,
        HttpMethods.Patch
    };

    public async Task InvokeAsync(HttpContext context, ISandboxAccessService sandboxAccess)
    {
        if (!environment.IsProduction()
            || !WriteMethods.Contains(context.Request.Method)
            || IsExemptPath(context.Request))
        {
            await next(context);
            return;
        }

        var siteId = await TryResolveSiteIdAsync(context);
        if (siteId is null || siteId == Guid.Empty)
        {
            await next(context);
            return;
        }

        var userId = ResolveUserId(context.User);
        var isSuperAdmin = string.Equals(
            context.User.FindFirstValue("role"),
            SystemRoleNames.SuperAdmin,
            StringComparison.OrdinalIgnoreCase);

        if (!await sandboxAccess.CanWriteToSiteAsync(userId, siteId.Value, isSuperAdmin, context.RequestAborted))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail("Sandbox site is not writable for this account."));
            return;
        }

        await next(context);
    }

    private static bool IsExemptPath(HttpRequest request)
    {
        var path = request.Path.Value ?? string.Empty;
        return path.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/license", StringComparison.OrdinalIgnoreCase);
    }

    private static Guid? ResolveUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    private static async Task<Guid?> TryResolveSiteIdAsync(HttpContext context)
    {
        if (context.Request.Query.TryGetValue("siteId", out var queryValue)
            && Guid.TryParse(queryValue.ToString(), out var querySiteId))
        {
            return querySiteId;
        }

        if (context.Request.RouteValues.TryGetValue("siteId", out var routeValue)
            && Guid.TryParse(routeValue?.ToString(), out var routeSiteId))
        {
            return routeSiteId;
        }

        if (!context.Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) ?? true)
            return null;

        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync(context.RequestAborted);
        context.Request.Body.Position = 0;

        if (string.IsNullOrWhiteSpace(body))
            return null;

        try
        {
            using var document = JsonDocument.Parse(body);
            if (TryGetGuidProperty(document.RootElement, "siteId", out var siteId))
                return siteId;
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private static bool TryGetGuidProperty(JsonElement element, string propertyName, out Guid siteId)
    {
        siteId = Guid.Empty;
        if (element.ValueKind != JsonValueKind.Object)
            return false;

        foreach (var property in element.EnumerateObject())
        {
            if (!property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (property.Value.ValueKind == JsonValueKind.String
                && Guid.TryParse(property.Value.GetString(), out siteId))
            {
                return true;
            }
        }

        return false;
    }
}
