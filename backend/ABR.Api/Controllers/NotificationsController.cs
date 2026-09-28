using System.Security.Claims;
using ABR.Api.Authorization;
using ABR.Application.Common;
using ABR.Application.DTOs.Notifications;
using ABR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABR.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications) => _notifications = notifications;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserNotificationDto>>>> GetList(
        [FromQuery] Guid? siteId,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (!IsAdminOrSuperAdmin())
            return Forbid();

        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(ApiResponse<IReadOnlyList<UserNotificationDto>>.Fail("Unauthorized."));

        var items = await _notifications.GetForCurrentUserAsync(userId.Value, siteId, take, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<UserNotificationDto>>.Ok(items));
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<ApiResponse<int>>> GetUnreadCount(
        [FromQuery] Guid? siteId,
        CancellationToken cancellationToken = default)
    {
        if (!IsAdminOrSuperAdmin())
            return Ok(ApiResponse<int>.Ok(0));

        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(ApiResponse<int>.Fail("Unauthorized."));

        var count = await _notifications.GetUnreadCountAsync(userId.Value, siteId, cancellationToken);
        return Ok(ApiResponse<int>.Ok(count));
    }

    [HttpPost("{id:guid}/read")]
    public async Task<ActionResult<ApiResponse<object>>> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        if (!IsAdminOrSuperAdmin())
            return Forbid();

        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(ApiResponse<object>.Fail("Unauthorized."));

        var ok = await _notifications.MarkReadAsync(id, userId.Value, cancellationToken);
        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Notification not found."));

        return Ok(ApiResponse<object>.Ok(new { }, "Marked as read."));
    }

    [HttpPost("read-all")]
    public async Task<ActionResult<ApiResponse<object>>> MarkAllRead(
        [FromQuery] Guid? siteId,
        CancellationToken cancellationToken = default)
    {
        if (!IsAdminOrSuperAdmin())
            return Forbid();

        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(ApiResponse<object>.Fail("Unauthorized."));

        var count = await _notifications.MarkAllReadAsync(userId.Value, siteId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { count }, "All marked as read."));
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<ActionResult<ApiResponse<RestoreNotificationResultDto>>> Restore(Guid id, CancellationToken cancellationToken)
    {
        if (!IsAdminOrSuperAdmin())
            return Forbid();

        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(ApiResponse<RestoreNotificationResultDto>.Fail("Unauthorized."));

        try
        {
            var result = await _notifications.RestoreFromNotificationAsync(id, userId.Value, cancellationToken);
            if (result is null)
                return NotFound(ApiResponse<RestoreNotificationResultDto>.Fail("Notification not found."));

            return Ok(ApiResponse<RestoreNotificationResultDto>.Ok(result, result.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<RestoreNotificationResultDto>.Fail(ex.Message));
        }
    }

    private bool IsAdminOrSuperAdmin()
    {
        var role = User.FindFirstValue("role") ?? User.FindFirstValue(ClaimTypes.Role);
        return string.Equals(role, SystemRoleNames.Admin, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, SystemRoleNames.SuperAdmin, StringComparison.OrdinalIgnoreCase);
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
