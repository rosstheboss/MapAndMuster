namespace MapAndMuster.Infrastructure.Email;

/// <summary>
/// Named email delivery providers selected through <c>Email:Provider</c>.
/// </summary>
public static class EmailProviders
{
    /// <summary>
    /// SMTP delivery, including local catchers such as Mailpit.
    /// </summary>
    public const string Smtp = "Smtp";

    /// <summary>
    /// Resend HTTP API delivery.
    /// </summary>
    public const string Resend = "Resend";
}
