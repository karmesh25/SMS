using System.Text.Json;
using ABR.Application.Common;
using ABR.Application.DTOs.Notifications;
using ABR.Application.Interfaces;
using ABR.Domain.Entities;
using ABR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ABR.Infrastructure.Services;

public sealed class NotificationService : INotificationService
{
    private static readonly HashSet<string> RestorableEntityTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "vyaj_entry",
        "vyaj_payment"
    };

    private readonly AbrDbContext _context;

    public NotificationService(AbrDbContext context) => _context = context;

    public bool IsOlderThanSixMonths(DateOnly businessDate)
    {
        // TEMP for testing: 2 days. Restore to AddMonths(-6) after notification QA.
        var threshold = DateOnly.FromDateTime(DateTime.Today).AddDays(-2);
        return businessDate < threshold;
    }

    public async Task NotifyOldRecordChangeAsync(CreateChangeNotificationRequest request, CancellationToken cancellationToken = default)
    {
        if (request.SiteId == Guid.Empty || request.EntityId == Guid.Empty)
            return;

        var adminRoleNames = new[] { SystemRoleNames.Admin, SystemRoleNames.SuperAdmin };

        var recipientIds = await _context.Users
            .AsNoTracking()
            .Where(u => u.IsActive && adminRoleNames.Contains(u.Role))
            .Where(u =>
                u.Role == SystemRoleNames.SuperAdmin
                || u.SiteAccesses.Any(a => a.SiteId == request.SiteId))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        if (request.ActorUserId.HasValue)
            recipientIds = recipientIds.Where(id => id != request.ActorUserId.Value).ToList();

        if (recipientIds.Count == 0)
            return;

        var message = request.Message;
        if (request.ActorUserId.HasValue && !message.Contains(" by ", StringComparison.OrdinalIgnoreCase))
        {
            var actorName = await _context.Users.AsNoTracking()
                .Where(u => u.Id == request.ActorUserId.Value)
                .Select(u => u.Username)
                .FirstOrDefaultAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(actorName))
                message = $"{message.TrimEnd('.')} by {actorName}.";
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var recipientId in recipientIds)
        {
            _context.UserNotifications.Add(new UserNotification
            {
                SiteId = request.SiteId,
                RecipientUserId = recipientId,
                Module = request.Module,
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                Title = request.Title,
                Message = message,
                OldValues = request.OldValues,
                NewValues = request.NewValues,
                IsRead = false,
                ActorUserId = request.ActorUserId,
                CreatedAt = now
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserNotificationDto>> GetForCurrentUserAsync(
        Guid userId,
        Guid? siteId,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var q = _context.UserNotifications
            .AsNoTracking()
            .Include(n => n.ActorUser)
            .Where(n => !n.IsDeleted && n.RecipientUserId == userId);

        if (siteId.HasValue && siteId.Value != Guid.Empty)
            q = q.Where(n => n.SiteId == siteId.Value);

        var items = await q
            .OrderByDescending(n => n.CreatedAt)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(cancellationToken);

        var result = new List<UserNotificationDto>(items.Count);
        foreach (var n in items)
        {
            var canRestore = n.ResolvedAt is null
                && RestorableEntityTypes.Contains(n.EntityType)
                && await IsEntitySoftDeletedAsync(n.EntityType, n.EntityId, cancellationToken);
            result.Add(Map(n, canRestore));
        }

        return result;
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, Guid? siteId, CancellationToken cancellationToken = default)
    {
        var q = _context.UserNotifications
            .AsNoTracking()
            .Where(n => !n.IsDeleted && n.RecipientUserId == userId && !n.IsRead);

        if (siteId.HasValue && siteId.Value != Guid.Empty)
            q = q.Where(n => n.SiteId == siteId.Value);

        return await q.CountAsync(cancellationToken);
    }

    public async Task<bool> MarkReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var n = await _context.UserNotifications
            .FirstOrDefaultAsync(x => x.Id == notificationId && !x.IsDeleted && x.RecipientUserId == userId, cancellationToken);
        if (n is null) return false;

        n.IsRead = true;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> MarkAllReadAsync(Guid userId, Guid? siteId, CancellationToken cancellationToken = default)
    {
        var q = _context.UserNotifications
            .Where(n => !n.IsDeleted && n.RecipientUserId == userId && !n.IsRead);

        if (siteId.HasValue && siteId.Value != Guid.Empty)
            q = q.Where(n => n.SiteId == siteId.Value);

        var items = await q.ToListAsync(cancellationToken);
        foreach (var n in items)
            n.IsRead = true;

        await _context.SaveChangesAsync(cancellationToken);
        return items.Count;
    }

    public async Task<RestoreNotificationResultDto?> RestoreFromNotificationAsync(
        Guid notificationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var notification = await _context.UserNotifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && !n.IsDeleted && n.RecipientUserId == userId, cancellationToken);
        if (notification is null)
            return null;

        if (!RestorableEntityTypes.Contains(notification.EntityType))
        {
            return new RestoreNotificationResultDto
            {
                Restored = false,
                AlreadyActive = false,
                Message = "This notification cannot be restored.",
                EntityId = notification.EntityId,
                EntityType = notification.EntityType
            };
        }

        var stillDeleted = await IsEntitySoftDeletedAsync(notification.EntityType, notification.EntityId, cancellationToken);
        if (!stillDeleted)
        {
            await MarkRelatedResolvedAsync(notification, userId, cancellationToken);
            return new RestoreNotificationResultDto
            {
                Restored = false,
                AlreadyActive = true,
                Message = "Record is already active.",
                EntityId = notification.EntityId,
                EntityType = notification.EntityType
            };
        }

        if (string.Equals(notification.EntityType, "vyaj_entry", StringComparison.OrdinalIgnoreCase))
            await RestoreVyajEntryAsync(notification, cancellationToken);
        else
            await RestoreVyajPaymentAsync(notification.EntityId, cancellationToken);

        await MarkRelatedResolvedAsync(notification, userId, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new RestoreNotificationResultDto
        {
            Restored = true,
            AlreadyActive = false,
            Message = "Record restored successfully.",
            EntityId = notification.EntityId,
            EntityType = notification.EntityType
        };
    }

    private async Task MarkRelatedResolvedAsync(UserNotification seed, Guid resolvedByUserId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var siblings = await _context.UserNotifications
            .Where(n =>
                !n.IsDeleted
                && n.ResolvedAt == null
                && n.SiteId == seed.SiteId
                && n.Module == seed.Module
                && n.EntityType == seed.EntityType
                && n.EntityId == seed.EntityId)
            .ToListAsync(cancellationToken);

        foreach (var n in siblings)
        {
            n.ResolvedAt = now;
            n.ResolvedByUserId = resolvedByUserId;
            n.IsRead = true;
        }
    }

    private async Task RestoreVyajEntryAsync(UserNotification notification, CancellationToken cancellationToken)
    {
        var entry = await _context.VyajEntries
            .Include(e => e.Payments)
            .FirstOrDefaultAsync(e => e.Id == notification.EntityId, cancellationToken)
            ?? throw new KeyNotFoundException("Vyaj entry not found.");

        var paymentIds = TryReadPaymentIds(notification.OldValues);
        var cascadeDeletedAt = entry.DeletedAt;

        entry.IsDeleted = false;
        entry.DeletedAt = null;

        foreach (var payment in entry.Payments.Where(p => p.IsDeleted))
        {
            var restore = paymentIds is not null
                ? paymentIds.Contains(payment.Id)
                : cascadeDeletedAt.HasValue && payment.DeletedAt == cascadeDeletedAt;

            if (!restore && paymentIds is null)
                restore = true; // legacy deletes without shared timestamp: restore all soft-deleted payments on entry

            if (!restore)
                continue;

            payment.IsDeleted = false;
            payment.DeletedAt = null;
        }
    }

    private async Task RestoreVyajPaymentAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        var payment = await _context.VyajPayments
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken)
            ?? throw new KeyNotFoundException("Vyaj payment not found.");

        payment.IsDeleted = false;
        payment.DeletedAt = null;
    }

    private async Task<bool> IsEntitySoftDeletedAsync(string entityType, Guid entityId, CancellationToken cancellationToken)
    {
        if (string.Equals(entityType, "vyaj_entry", StringComparison.OrdinalIgnoreCase))
        {
            return await _context.VyajEntries.AnyAsync(e => e.Id == entityId && e.IsDeleted, cancellationToken);
        }

        if (string.Equals(entityType, "vyaj_payment", StringComparison.OrdinalIgnoreCase))
        {
            return await _context.VyajPayments.AnyAsync(p => p.Id == entityId && p.IsDeleted, cancellationToken);
        }

        return false;
    }

    private static HashSet<Guid>? TryReadPaymentIds(string? oldValues)
    {
        if (string.IsNullOrWhiteSpace(oldValues))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(oldValues);
            if (!doc.RootElement.TryGetProperty("paymentIds", out var arr) || arr.ValueKind != JsonValueKind.Array)
                return null;

            var ids = new HashSet<Guid>();
            foreach (var el in arr.EnumerateArray())
            {
                if (el.ValueKind == JsonValueKind.String && Guid.TryParse(el.GetString(), out var id))
                    ids.Add(id);
            }

            return ids;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static UserNotificationDto Map(UserNotification n, bool canRestore) => new()
    {
        Id = n.Id,
        SiteId = n.SiteId,
        Module = n.Module,
        EntityType = n.EntityType,
        EntityId = n.EntityId,
        Title = n.Title,
        Message = n.Message,
        OldValues = n.OldValues,
        NewValues = n.NewValues,
        IsRead = n.IsRead,
        CreatedAt = n.CreatedAt,
        ActorUsername = n.ActorUser?.Username,
        CanRestore = canRestore,
        ResolvedAt = n.ResolvedAt
    };
}
