using ABR.Application.Common;

namespace ABR.Api.Middleware;

public sealed class SubscriptionLicenseMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (SubscriptionLicense.IsExpired && !IsExemptPath(context.Request))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(SubscriptionLicense.ExpiredMessage));
            return;
        }

        await next(context);
    }

    private static bool IsExemptPath(HttpRequest request)
    {
        if (!HttpMethods.IsGet(request.Method))
            return false;

        var path = request.Path.Value ?? string.Empty;
        return path.Equals("/api/license/status", StringComparison.OrdinalIgnoreCase);
    }
}
