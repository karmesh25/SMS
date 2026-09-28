using ABR.Application.DTOs.Notifications;

namespace ABR.Application.Interfaces;

public interface INotificationService
{
    /// <summary>True when businessDate is older than the change-alert threshold from today (local calendar). TEMP: 2 days for testing; production rule is 6 months.</summary>
    bool IsOlderThanSixMonths(DateOnly businessDate);

    Task NotifyOldRecordChangeAsync(CreateChangeNotificationRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserNotificationDto>> GetForCurrentUserAsync(Guid userId, Guid? siteId, int take = 50, CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(Guid userId, Guid? siteId, CancellationToken cancellationToken = default);

    Task<bool> MarkReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);

    Task<int> MarkAllReadAsync(Guid userId, Guid? siteId, CancellationToken cancellationToken = default);

    Task<RestoreNotificationResultDto?> RestoreFromNotificationAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);
}
