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
    /// Gets or sets the from address.
    /// </summary>
    public string FromAddress { get; set; } = "campaign@localhost";
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
