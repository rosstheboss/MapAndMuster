namespace Campaign.Application.Ports;

/// <summary>
/// Queues transactional email that must not block account state changes.
/// </summary>
public interface IEmailOutbox
{
    /// <summary>
    /// Queues an email confirmation message.
    /// </summary>
    /// <param name="email">The recipient email address.</param>
    /// <param name="userId">The account identifier.</param>
    /// <param name="token">The confirmation token. Must not be logged.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the message is queued.</returns>
    Task QueueEmailConfirmationAsync(string email, Guid userId, string token, CancellationToken cancellationToken);

    /// <summary>
    /// Queues a password reset message.
    /// </summary>
    /// <param name="email">The recipient email address.</param>
    /// <param name="userId">The account identifier.</param>
    /// <param name="token">The reset token. Must not be logged.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the message is queued.</returns>
    Task QueuePasswordResetAsync(string email, Guid userId, string token, CancellationToken cancellationToken);

    /// <summary>
    /// Queues a campaign or chat notice. The body must not include hidden orders or private chat text.
    /// </summary>
    Task QueueUserNoticeAsync(
        string email,
        Guid userId,
        string subject,
        string body,
        string path,
        CancellationToken cancellationToken);
}
