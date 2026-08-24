using System.Diagnostics.CodeAnalysis;
using MapAndMuster.Domain.Common;

namespace MapAndMuster.Domain.Identity;

/// <summary>
/// How UTC timestamps are formatted for the owning user. Stored instants remain UTC.
/// </summary>
public enum DateTimeDisplayFormat
{
    /// <summary>
    /// January 5, 2027, 12:34:52 PM EST. The default.
    /// </summary>
    MonthDayYear12h = 0,

    /// <summary>
    /// 5 January 2027, 12:34:52 PM EST.
    /// </summary>
    DayMonthYear12h = 1,

    /// <summary>
    /// January 5, 2027, 12:34:52 EST.
    /// </summary>
    MonthDayYear24h = 2,

    /// <summary>
    /// 2027-01-05 12:34:52 PM EST.
    /// </summary>
    IsoSortable12h = 3,

    /// <summary>
    /// 2027-01-05 12:34:52 EST.
    /// </summary>
    IsoSortable24h = 4,

    /// <summary>
    /// 1/5/2027, 12:34:52 PM EST.
    /// </summary>
    NumericUs12h = 5,

    /// <summary>
    /// 5/1/2027, 12:34:52 EST.
    /// </summary>
    NumericEu24h = 6,
}

/// <summary>
/// Parses and lists supported timestamp display formats.
/// </summary>
public static class DateTimeDisplayFormats
{
    /// <summary>The profile and display default.</summary>
    public const DateTimeDisplayFormat Default = DateTimeDisplayFormat.MonthDayYear12h;

    /// <summary>
    /// Every supported format, in display order.
    /// </summary>
    public static IReadOnlyList<DateTimeDisplayFormat> All { get; } =
    [
        DateTimeDisplayFormat.MonthDayYear12h,
        DateTimeDisplayFormat.DayMonthYear12h,
        DateTimeDisplayFormat.MonthDayYear24h,
        DateTimeDisplayFormat.IsoSortable12h,
        DateTimeDisplayFormat.IsoSortable24h,
        DateTimeDisplayFormat.NumericUs12h,
        DateTimeDisplayFormat.NumericEu24h,
    ];

    /// <summary>
    /// Parses a display format. Blank values become the default.
    /// </summary>
    public static bool TryParse(
        string? value,
        [NotNullWhen(false)] out DomainError? error,
        out DateTimeDisplayFormat format)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            format = Default;
            return true;
        }

        if (Enum.TryParse(value.Trim(), ignoreCase: true, out format) && Enum.IsDefined(format))
        {
            return true;
        }

        format = Default;
        error = new DomainError(
            "profile.dateTimeDisplayFormat.invalid",
            "Choose a supported date and time format.",
            "dateTimeDisplayFormat");
        return false;
    }
}
