namespace Campaign.Infrastructure.Email;

/// <summary>
/// Formats a From header without allowing newline injection.
/// </summary>
public static class EmailAddressFormatter
{
    /// <summary>
    /// Returns <c>Name &lt;address&gt;</c> when a display name is configured; otherwise the address alone.
    /// </summary>
    /// <param name="options">The email options.</param>
    /// <returns>The From value to send to SMTP or Resend.</returns>
    public static string FormatFrom(EmailOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.FromAddress);

        var address = options.FromAddress.Trim();
        var name = SanitizeDisplayName(options.FromName);
        return name.Length == 0 ? address : $"{name} <{address}>";
    }

    /// <summary>
    /// Returns a display name safe to place in an email header.
    /// </summary>
    /// <param name="value">The configured display name.</param>
    /// <returns>The sanitized name, or empty when none is usable.</returns>
    public static string SanitizeDisplayName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal)
            .Replace("<", string.Empty, StringComparison.Ordinal)
            .Replace(">", string.Empty, StringComparison.Ordinal)
            .Trim();
    }
}
