namespace Campaign.Infrastructure.Email;

/// <summary>
/// SMTP settings used by the outbox processor. Production providers remain an operations decision.
/// </summary>
public sealed class EmailOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Email";

    /// <summary>
    /// Gets or sets the SMTP host. Empty disables delivery.
    /// </summary>
    public string SmtpHost { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SMTP port.
    /// </summary>
    public int SmtpPort { get; set; } = 1025;

    /// <summary>
    /// Gets or sets the from address. Real SMTP providers usually require this to match the authenticated account.
    /// </summary>
    public string FromAddress { get; set; } = "campaign@localhost";

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
