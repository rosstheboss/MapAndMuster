namespace MapAndMuster.Infrastructure.Email;

/// <summary>
/// Resend API settings. Store the API key in user secrets or environment configuration, never source control.
/// </summary>
public sealed class ResendEmailOptions
{
    /// <summary>
    /// Gets or sets the Resend API key. Empty disables Resend delivery.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}
