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
    /// Inserts every notice whose dedupe key is new for its user, in one round trip.
    /// </summary>
    /// <param name="notifications">The notices to insert.</param>
    /// <param name="utcNow">The creation instant.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The dedupe keys that were inserted.</returns>
    /// <remarks>
    /// A campaign fan-out writes one notice per member. Calling <see cref="TryAddAsync"/> for each
    /// costs a dedupe query and a transaction per member. The default implementation does exactly
    /// that so test doubles keep working.
    /// </remarks>
    async Task<IReadOnlySet<string>> TryAddManyAsync(
        IReadOnlyList<NewUserNotification> notifications,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notifications);
        var accepted = new HashSet<string>(StringComparer.Ordinal);
        foreach (var notification in notifications)
        {
            if (await TryAddAsync(notification, utcNow, cancellationToken).ConfigureAwait(false))
            {
                accepted.Add(notification.DedupeKey);
            }
        }

        return accepted;
    }

    /// <summary>
    /// Lists unread notices for the home board, newest first.
    /// </summary>
    Task<IReadOnlyList<UserNotification>> ListUnreadAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Marks a notice read when it belongs to the user.
    /// </summary>
    Task<bool> MarkReadAsync(Guid notificationId, Guid userId, DateTimeOffset utcNow, CancellationToken cancellationToken);

    /// <summary>
    /// Marks every unread notice for the user as read.
    /// </summary>
    Task<int> MarkAllReadAsync(Guid userId, DateTimeOffset utcNow, CancellationToken cancellationToken);
}
