using System.Net.Mail;

namespace MapAndMuster.Infrastructure.Email;

/// <summary>
/// Delivers mail through SMTP, including unauthenticated local catchers.
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;

    /// <summary>
    /// Initializes the sender.
    /// </summary>
    /// <param name="options">SMTP options.</param>
    public SmtpEmailSender(EmailOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <inheritdoc />
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(_options.SmtpHost);

        using var client = SmtpClientFactory.Create(_options);
        using var mail = new MailMessage
        {
            From = CreateFromAddress(),
            Subject = message.Subject,
            Body = message.Body,
        };
        mail.To.Add(message.To);
        await client.SendMailAsync(mail, cancellationToken).ConfigureAwait(false);
    }

    private MailAddress CreateFromAddress()
    {
        var displayName = EmailAddressFormatter.SanitizeDisplayName(_options.FromName);
        return displayName.Length == 0
            ? new MailAddress(_options.FromAddress)
            : new MailAddress(_options.FromAddress, displayName);
    }
}
