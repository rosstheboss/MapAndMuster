using MapAndMuster.Application.Notifications;

namespace MapAndMuster.Application.Ports;

/// <summary>
/// Persistence for in-app user notifications.
/// </summary>
public interface IUserNotificationStore
{
    /// <summary>
    /// Inserts a notice when the dedupe key is new for that user.
    /// </summary>
    Task<bool> TryAddAsync(NewUserNotification notification, DateTimeOffset utcNow, CancellationToken cancellationToken);

    /// <summary>
    /// Lists unread notices for the home board, newest first.
    /// </summary>
    Task<IReadOnlyList<UserNotification>> ListUnreadAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Marks a notice read when it belongs to the user.
    /// </summary>
    Task<bool> MarkReadAsync(Guid notificationId, Guid userId, DateTimeOffset utcNow, CancellationToken cancellationToken);
}
