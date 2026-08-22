using System.Net;
using System.Net.Mail;

namespace MapAndMuster.Infrastructure.Email;

/// <summary>
/// Creates SMTP clients from configured email options.
/// </summary>

public static class SmtpClientFactory
#pragma warning disable IDE0055
{
#pragma warning restore IDE0055
    /// <summary>
    /// Creates a client for the configured host. Authentication is used only when a username is set.
    /// </summary>
    /// <param name="options">The email options.</param>
    /// <returns>A configured SMTP client. The caller owns disposal.</returns>
#pragma warning disable SYSLIB0014

    public static SmtpClient Create(EmailOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.SmtpHost);

        var client = new SmtpClient(options.SmtpHost, options.SmtpPort)
        {
            DeliveryMethod = SmtpDeliveryMethod.Network,
            EnableSsl = options.EnableSsl,
            Timeout = 15000,
        };

        if (!string.IsNullOrWhiteSpace(options.SmtpUsername))
        {
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(options.SmtpUsername, options.SmtpPassword);
        }

        return client;
    }

#pragma warning restore SYSLIB0014
}
