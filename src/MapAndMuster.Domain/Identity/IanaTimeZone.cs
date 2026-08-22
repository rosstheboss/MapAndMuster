using System.Diagnostics.CodeAnalysis;
using MapAndMuster.Domain.Common;

namespace MapAndMuster.Domain.Identity;

/// <summary>
/// An IANA time zone used only for displaying stored UTC instants.
/// Timestamps remain UTC in persistence. Registration and profile updates require a zone.
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
    /// Attempts to parse a required time-zone preference.
    /// </summary>
    /// <param name="raw">The raw identifier.</param>
    /// <param name="timeZone">The parsed zone when valid.</param>
    /// <param name="error">The validation error when creation fails.</param>
    /// <returns><see langword="true"/> when the value is a known IANA zone.</returns>
    public static bool TryCreate(
        string? raw,
        [NotNullWhen(true)] out IanaTimeZone? timeZone,
        [NotNullWhen(false)] out DomainError? error)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            timeZone = null;
            error = new DomainError("timeZone.invalid", "Time zone is not filled in.", "timeZoneId");
            return false;
        }

        return TryParse(raw, out timeZone, out error);
    }

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

        return TryParse(raw, out timeZone, out error);
    }

    private static bool TryParse(
        string raw,
        [NotNullWhen(true)] out IanaTimeZone? timeZone,
        [NotNullWhen(false)] out DomainError? error)
    {
        timeZone = null;
        var trimmed = raw.Trim();
        if (trimmed.Length > MaxLength)
        {
            error = new DomainError("timeZone.invalid", "Choose a valid time zone.", "timeZoneId");
            return false;
        }

        if (string.Equals(trimmed, UtcId, StringComparison.OrdinalIgnoreCase))
        {
            timeZone = new IanaTimeZone(UtcId);
            error = null;
            return true;
        }

        if (!TimeZoneInfo.TryFindSystemTimeZoneById(trimmed, out _))
        {
            error = new DomainError("timeZone.invalid", "Choose a valid time zone.", "timeZoneId");
            return false;
        }

        timeZone = new IanaTimeZone(trimmed);
        error = null;
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
