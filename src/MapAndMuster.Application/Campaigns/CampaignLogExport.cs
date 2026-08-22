using System.Globalization;
using System.Text;
using MapAndMuster.Application.Play;
using MapAndMuster.Domain.Identity;

namespace MapAndMuster.Application.Campaigns;

/// <summary>
/// File format for a campaign log download.
/// </summary>
public enum CampaignLogExportFormat
{
    /// <summary>A readable text log.</summary>
    Text = 0,

    /// <summary>A comma-separated table.</summary>
    Csv = 1,
}

/// <summary>
/// A generated campaign log file for download or later outbound use.
/// </summary>
public sealed class ExportedCampaignLog
{
    /// <summary>Gets the UTF-8 file bytes.</summary>
    public required byte[] Content { get; init; }

    /// <summary>Gets the MIME type, including charset.</summary>
    public required string ContentType { get; init; }

    /// <summary>Gets the suggested download file name.</summary>
    public required string DownloadName { get; init; }
}

/// <summary>
/// Command to download public chat and/or game-log facts.
/// </summary>
public sealed class ExportCampaignLogCommand
{
    /// <summary>Gets the campaign identifier.</summary>
    public required Guid CampaignId { get; init; }

    /// <summary>Gets the authenticated user identifier.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets whether the caller is a system administrator.</summary>
    public required bool IsAdministrator { get; init; }

    /// <summary>Gets whether public chat lines are included.</summary>
    public required bool IncludePublicChat { get; init; }

    /// <summary>Gets whether game-log facts are included.</summary>
    public required bool IncludeGameLog { get; init; }

    /// <summary>Gets the requested file format.</summary>
    public required CampaignLogExportFormat Format { get; init; }
}

/// <summary>
/// Builds a single public-chat and/or game-log file. Private chats are never included.
/// </summary>
public static class CampaignLogExport
{
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// Parses <c>txt</c> or <c>csv</c> from a query string.
    /// </summary>
    public static bool TryParseFormat(string? raw, out CampaignLogExportFormat format)
    {
        format = CampaignLogExportFormat.Text;
        if (string.IsNullOrWhiteSpace(raw) || raw.Equals("txt", StringComparison.OrdinalIgnoreCase)
            || raw.Equals("text", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (raw.Equals("csv", StringComparison.OrdinalIgnoreCase))
        {
            format = CampaignLogExportFormat.Csv;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Selects public chat and/or game-log facts. Private chats are omitted even when the viewer can see them.
    /// </summary>
    public static IReadOnlyList<PlayLogEntryDetail> Select(
        IReadOnlyList<PlayLogEntryDetail> entries,
        bool includePublicChat,
        bool includeGameLog)
    {
        ArgumentNullException.ThrowIfNull(entries);
        return
        [
            .. entries.Where(entry =>
            {
                if (entry.IsPrivate)
                {
                    return false;
                }

                if (string.Equals(entry.Kind, "PlayerChat", StringComparison.OrdinalIgnoreCase))
                {
                    return includePublicChat;
                }

                return includeGameLog;
            }),
        ];
    }

    /// <summary>
    /// Writes the selected log entries as text or CSV using the campaign time zone for display timestamps.
    /// </summary>
    public static ExportedCampaignLog Write(
        string campaignName,
        string timeZoneId,
        IReadOnlyList<PlayLogEntryDetail> entries,
        CampaignLogExportFormat format)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(campaignName);
        ArgumentException.ThrowIfNullOrWhiteSpace(timeZoneId);
        ArgumentNullException.ThrowIfNull(entries);

        var body = format == CampaignLogExportFormat.Csv
            ? WriteCsv(timeZoneId, entries)
            : WriteText(timeZoneId, entries);
        var extension = format == CampaignLogExportFormat.Csv ? "csv" : "txt";
        return new ExportedCampaignLog
        {
            Content = Utf8.GetBytes(body),
            ContentType = format == CampaignLogExportFormat.Csv
                ? "text/csv; charset=utf-8"
                : "text/plain; charset=utf-8",
            DownloadName = $"{FileSlug(campaignName)}-log.{extension}",
        };
    }

    /// <summary>
    /// Formats a UTC instant the same way the campaign log shows it.
    /// </summary>
    public static string FormatDisplayTimestamp(DateTimeOffset occurredUtc, string timeZoneId)
    {
        var zone = ResolveZone(timeZoneId);
        var local = TimeZoneInfo.ConvertTime(occurredUtc, zone);
        var clock = local.ToString("yyyy-MM-dd hh:mm:ss tt", CultureInfo.InvariantCulture);
        return $"({clock} {Abbreviation(zone, local)})";
    }

    private static string WriteText(string timeZoneId, IReadOnlyList<PlayLogEntryDetail> entries)
    {
        var builder = new StringBuilder();
        foreach (var entry in entries)
        {
            builder.Append(FormatDisplayTimestamp(entry.OccurredUtc, timeZoneId));
            builder.Append(' ');
            builder.Append(entry.Originator);
            builder.Append(": ");
            builder.Append(entry.Summary.Replace("\r\n", "\n", StringComparison.Ordinal));
            builder.Append('\n');
        }

        return builder.ToString();
    }

    private static string WriteCsv(string timeZoneId, IReadOnlyList<PlayLogEntryDetail> entries)
    {
        var builder = new StringBuilder();
        builder.Append("OccurredUtc,LocalTimestamp,Source,Kind,Originator,Summary\r\n");
        foreach (var entry in entries)
        {
            builder.Append(Csv(entry.OccurredUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss'Z'", CultureInfo.InvariantCulture)));
            builder.Append(',');
            builder.Append(Csv(FormatDisplayTimestamp(entry.OccurredUtc, timeZoneId)));
            builder.Append(',');
            builder.Append(Csv(Source(entry)));
            builder.Append(',');
            builder.Append(Csv(entry.Kind));
            builder.Append(',');
            builder.Append(Csv(entry.Originator));
            builder.Append(',');
            builder.Append(Csv(entry.Summary));
            builder.Append("\r\n");
        }

        return builder.ToString();
    }

    private static string Source(PlayLogEntryDetail entry)
    {
        return string.Equals(entry.Kind, "PlayerChat", StringComparison.OrdinalIgnoreCase)
            ? "PublicChat"
            : "GameLog";
    }

    private static string Csv(string value)
    {
        var sanitized = value.Replace("\r\n", "\n", StringComparison.Ordinal);
        if (sanitized.StartsWith('=') || sanitized.StartsWith('+') || sanitized.StartsWith('-') || sanitized.StartsWith('@'))
        {
            sanitized = "'" + sanitized;
        }

        if (sanitized.Contains('"') || sanitized.Contains(',') || sanitized.Contains('\n') || sanitized.Contains('\r'))
        {
            return "\"" + sanitized.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
        }

        return sanitized;
    }

    internal static string FileSlug(string name)
    {
        var builder = new StringBuilder();
        foreach (var character in name.Trim())
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                continue;
            }

            if (builder.Length > 0 && builder[^1] != '-')
            {
                builder.Append('-');
            }
        }

        var slug = builder.ToString().Trim('-');
        return slug.Length == 0 ? "campaign" : slug;
    }

    private static TimeZoneInfo ResolveZone(string timeZoneId)
    {
        if (string.Equals(timeZoneId, IanaTimeZone.UtcId, StringComparison.OrdinalIgnoreCase))
        {
            return TimeZoneInfo.Utc;
        }

        return TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var zone)
            ? zone
            : TimeZoneInfo.Utc;
    }

    private static string Abbreviation(TimeZoneInfo zone, DateTimeOffset local)
    {
        if (zone.Equals(TimeZoneInfo.Utc)
            || string.Equals(zone.Id, IanaTimeZone.UtcId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(zone.Id, "Etc/UTC", StringComparison.OrdinalIgnoreCase))
        {
            return "UTC";
        }

        var name = zone.IsDaylightSavingTime(local) ? zone.DaylightName : zone.StandardName;
        if (name.Length is >= 2 and <= 5 && name.All(static character => char.IsLetter(character)))
        {
            return name.ToUpperInvariant();
        }

        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            return string.Concat(parts.Select(static part => char.ToUpperInvariant(part[0])));
        }

        var offset = zone.GetUtcOffset(local);
        var sign = offset < TimeSpan.Zero ? "-" : "+";
        return $"{sign}{Math.Abs(offset.Hours):00}:{Math.Abs(offset.Minutes):00}";
    }
}
