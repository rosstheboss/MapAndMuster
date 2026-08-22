namespace Campaign.Infrastructure.Email;

/// <summary>
/// Email delivery settings used by the outbox processor.
/// </summary>
public sealed class EmailOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Email";

    /// <summary>
    /// Gets or sets the delivery provider. Use <see cref="EmailProviders.Smtp"/> or <see cref="EmailProviders.Resend"/>.
    /// </summary>
    public string Provider { get; set; } = EmailProviders.Smtp;

    /// <summary>
    /// Gets or sets the SMTP host. Empty disables SMTP delivery.
    /// </summary>
    public string SmtpHost { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SMTP port.
    /// </summary>
    public int SmtpPort { get; set; } = 1025;

    /// <summary>
    /// Gets or sets the from address. Real providers usually require this to match a verified domain.
    /// </summary>
    public string FromAddress { get; set; } = "campaign@localhost";

    /// <summary>
    /// Gets or sets the optional from display name.
    /// </summary>
    public string FromName { get; set; } = "Campaign";

    /// <summary>
    /// Gets or sets the optional SMTP username. Leave empty for unauthenticated local catchers such as Mailpit.
    /// </summary>
    public string SmtpUsername { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional SMTP password. Store this in user secrets, never in source control.
    /// </summary>
    public string SmtpPassword { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the SMTP connection uses SSL or STARTTLS.
    /// </summary>
    public bool EnableSsl { get; set; }

    /// <summary>
    /// Gets Resend API settings.
    /// </summary>
    public ResendEmailOptions Resend { get; set; } = new();

    /// <summary>
    /// Gets whether Resend is the selected provider.
    /// </summary>
    public bool UsesResend =>
        string.Equals(Provider, EmailProviders.Resend, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets whether enough settings exist to attempt delivery.
    /// </summary>
    public bool IsDeliveryConfigured =>
        UsesResend ? !string.IsNullOrWhiteSpace(Resend.ApiKey) : !string.IsNullOrWhiteSpace(SmtpHost);
}

/// <summary>
/// Public web origin used to build email links.
/// </summary>
public sealed class PublicWebOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "PublicWeb";

    /// <summary>
    /// Gets or sets the public Angular origin, such as http://localhost:4200.
    /// </summary>
    public string Origin { get; set; } = "http://localhost:4200";
}
