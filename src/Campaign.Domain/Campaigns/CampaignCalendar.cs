using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Campaign.Domain.Common;
using Campaign.Domain.Identity;

namespace Campaign.Domain.Campaigns;

/// <summary>
/// Converts campaign wall-clock times using the creator-chosen IANA time zone.
/// Instants are stored and compared in UTC.
/// </summary>
public static class CampaignCalendar
{
    private static readonly string[] LocalFormats =
    [
        "yyyy-MM-dd'T'HH:mm",
        "yyyy-MM-dd'T'HH:mm:ss",
        "yyyy-MM-dd HH:mm",
        "yyyy-MM-dd HH:mm:ss",
    ];

    /// <summary>
    /// Resolves the system zone for an IANA identifier.
    /// </summary>
    /// <param name="timeZone">The IANA zone.</param>
    /// <returns>The system time-zone information.</returns>
    public static TimeZoneInfo ZoneFor(IanaTimeZone timeZone)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        if (string.Equals(timeZone.Id, IanaTimeZone.UtcId, StringComparison.OrdinalIgnoreCase))
        {
            return TimeZoneInfo.Utc;
        }

        return TimeZoneInfo.FindSystemTimeZoneById(timeZone.Id);
    }

    /// <summary>
    /// Parses a local wall-clock start time in the campaign time zone.
    /// </summary>
    /// <param name="raw">The local date and time without an offset.</param>
    /// <param name="timeZone">The campaign time zone.</param>
    /// <param name="startsUtc">The UTC instant when parsing succeeds.</param>
    /// <param name="error">The validation error when parsing fails.</param>
    /// <returns><see langword="true"/> when the value is a valid local time in the zone.</returns>
    public static bool TryParseLocalStart(
        string? raw,
        IanaTimeZone timeZone,
        out DateTimeOffset startsUtc,
        [NotNullWhen(false)] out DomainError? error)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        startsUtc = default;
        error = null;

        if (string.IsNullOrWhiteSpace(raw))
        {
            error = new DomainError(
                "startsAtLocal.invalid",
                "Start date and time is not filled in.",
                "startsAtLocal");
            return false;
        }

        if (!DateTime.TryParseExact(
                raw.Trim(),
                LocalFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var local))
        {
            error = new DomainError(
                "startsAtLocal.invalid",
                "Start date and time must be a valid date and time.",
                "startsAtLocal");
            return false;
        }

        var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        var zone = ZoneFor(timeZone);
        if (zone.IsInvalidTime(unspecified))
        {
            error = new DomainError(
                "startsAtLocal.invalid",
                "Start date and time falls into a daylight-saving gap in the selected time zone.",
                "startsAtLocal");
            return false;
        }

        var utc = TimeZoneInfo.ConvertTimeToUtc(unspecified, zone);
        startsUtc = new DateTimeOffset(utc, TimeSpan.Zero);
        return true;
    }

    /// <summary>
    /// Adds a duration to a UTC instant using calendar rules in the campaign time zone.
    /// </summary>
    /// <param name="utc">The starting instant.</param>
    /// <param name="timeZone">The campaign time zone.</param>
    /// <param name="duration">The duration to add.</param>
    /// <returns>The resulting UTC instant.</returns>
    public static DateTimeOffset Add(DateTimeOffset utc, IanaTimeZone timeZone, ScheduleDuration duration)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        ArgumentNullException.ThrowIfNull(duration);
        var zone = ZoneFor(timeZone);
        var local = TimeZoneInfo.ConvertTime(utc, zone);
        var advanced = DateTime.SpecifyKind(local.DateTime, DateTimeKind.Unspecified);
        advanced = duration.Unit switch
        {
            DurationUnit.Minutes => advanced.AddMinutes(duration.Amount),
            DurationUnit.Hours => advanced.AddHours(duration.Amount),
            DurationUnit.Days => advanced.AddDays(duration.Amount),
            DurationUnit.Weeks => advanced.AddDays(7L * duration.Amount),
            DurationUnit.Months => advanced.AddMonths(duration.Amount),
            _ => throw new ArgumentOutOfRangeException(nameof(duration)),
        };

        while (zone.IsInvalidTime(advanced))
        {
            advanced = advanced.AddMinutes(1);
        }

        var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(advanced, zone);
        return new DateTimeOffset(utcDateTime, TimeSpan.Zero);
    }

    /// <summary>
    /// Formats a UTC instant as a local wall-clock value in the campaign time zone.
    /// </summary>
    /// <param name="utc">The instant.</param>
    /// <param name="timeZone">The campaign time zone.</param>
    /// <returns>A value suitable for a datetime-local control.</returns>
    public static string FormatLocal(DateTimeOffset utc, IanaTimeZone timeZone)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        var local = TimeZoneInfo.ConvertTime(utc, ZoneFor(timeZone));
        return local.ToString("yyyy-MM-dd'T'HH:mm", CultureInfo.InvariantCulture);
    }
}
