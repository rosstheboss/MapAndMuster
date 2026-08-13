using System.Diagnostics.CodeAnalysis;
using Campaign.Domain.Common;

namespace Campaign.Domain.Identity;

/// <summary>
/// An optional IANA time zone used only for displaying stored UTC instants.
/// Timestamps remain UTC in persistence.
/// </summary>
public sealed class IanaTimeZone
{
    /// <summary>
    /// Maximum length of a time-zone identifier.
    /// </summary>
    public const int MaxLength = 64;

    /// <summary>
    /// The UTC identifier used when the owner has not chosen a zone.
    /// </summary>
    public const string UtcId = "UTC";

    private IanaTimeZone(string id)
    {
        Id = id;
    }

    /// <summary>
    /// Gets the IANA time-zone identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Attempts to parse an optional time-zone preference. Empty input means no selection.
    /// </summary>
    /// <param name="raw">The raw identifier, or empty to leave the preference unset.</param>
    /// <param name="timeZone">The parsed zone when a value is supplied and valid; otherwise <see langword="null"/>.</param>
    /// <param name="error">The validation error when the value is present but invalid.</param>
    /// <returns><see langword="true"/> when the input is empty or a known IANA zone.</returns>
    public static bool TryCreateOptional(
        string? raw,
        out IanaTimeZone? timeZone,
        [NotNullWhen(false)] out DomainError? error)
    {
        timeZone = null;
        error = null;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return true;
        }

        var trimmed = raw.Trim();
        if (trimmed.Length > MaxLength)
        {
            error = new DomainError("timeZone.invalid", "Choose a valid time zone, or leave it blank to display UTC.");
            return false;
        }

        if (string.Equals(trimmed, UtcId, StringComparison.OrdinalIgnoreCase))
        {
            timeZone = new IanaTimeZone(UtcId);
            return true;
        }

        if (!TimeZoneInfo.TryFindSystemTimeZoneById(trimmed, out _))
        {
            error = new DomainError("timeZone.invalid", "Choose a valid time zone, or leave it blank to display UTC.");
            return false;
        }

        timeZone = new IanaTimeZone(trimmed);
        return true;
    }

    /// <summary>
    /// Returns the identifier used to format timestamps, defaulting to UTC when unset.
    /// </summary>
    /// <param name="timeZone">The stored preference.</param>
    /// <returns>An IANA identifier.</returns>
    public static string DisplayId(IanaTimeZone? timeZone)
    {
        return timeZone?.Id ?? UtcId;
    }
}
