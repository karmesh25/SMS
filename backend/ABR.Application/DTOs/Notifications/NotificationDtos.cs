namespace ABR.Application.DTOs.Notifications;

public class UserNotificationDto
{
    public Guid Id { get; set; }
    public Guid SiteId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? ActorUsername { get; set; }
    public bool CanRestore { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
}

public class CreateChangeNotificationRequest
{
    public Guid SiteId { get; set; }
    public Guid? ActorUserId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
}

public class RestoreNotificationResultDto
{
    public bool Restored { get; set; }
    public bool AlreadyActive { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = string.Empty;
}
