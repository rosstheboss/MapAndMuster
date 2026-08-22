using System.Net;

namespace Campaign.Infrastructure.Email;

/// <summary>
/// Builds identity and notice emails from outbox payloads. Does not include secret values in subjects.
/// </summary>
public static class OutboxEmailComposer
{
    /// <summary>
    /// Composes a deliverable message for an outbox payload.
    /// </summary>
    /// <param name="type">The outbox message type.</param>
    /// <param name="payload">The payload.</param>
    /// <param name="webOptions">Public web origin options used to build links.</param>
    /// <returns>The composed message.</returns>
    public static EmailMessage Compose(string type, OutboxEmailPayload payload, PublicWebOptions webOptions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(webOptions);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload.Email);

        var origin = webOptions.Origin.TrimEnd('/');
        if (type == EmailOutbox.UserNoticeType)
        {
            var path = payload.Path ?? "/";
            var link = path.StartsWith('/') ? $"{origin}{path}" : $"{origin}/{path}";
            var subject = payload.Subject ?? "Campaign notice";
            var body = $"{payload.Body ?? string.Empty} Open: {link}";
            return new EmailMessage(payload.Email, subject, body);
        }

        var encodedToken = WebUtility.UrlEncode(payload.Token);
        if (type == EmailOutbox.ConfirmEmailType)
        {
            var link = $"{origin}/confirm-email?userId={payload.UserId}&token={encodedToken}";
            return new EmailMessage(payload.Email, "Confirm your campaign account", $"Confirm your email by opening this link: {link}");
        }

        var resetLink = $"{origin}/reset-password?userId={payload.UserId}&token={encodedToken}";
        return new EmailMessage(payload.Email, "Reset your campaign password", $"Reset your password by opening this link: {resetLink}");
    }
}
