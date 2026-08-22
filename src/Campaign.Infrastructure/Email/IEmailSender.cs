namespace Campaign.Infrastructure.Email;

/// <summary>
/// Delivers a composed email. Application code queues through <c>IEmailOutbox</c>; this port is for the outbox processor.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Sends one message. Implementations must not log secret tokens, API keys, or connection strings.
    /// </summary>
    /// <param name="message">The composed message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the provider accepts the message.</returns>
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
