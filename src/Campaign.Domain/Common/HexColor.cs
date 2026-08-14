using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Campaign.Domain.Common;

/// <summary>
/// Normalizes six-digit hex colors used for factions, terrain, and overlays.
/// </summary>
public static partial class HexColor
{
    /// <summary>
    /// Attempts to parse a #RRGGBB color and returns it in uppercase.
    /// </summary>
    /// <param name="raw">The supplied color.</param>
    /// <param name="color">The normalized color when parsing succeeds.</param>
    /// <returns><see langword="true"/> when the value is a six-digit hex color.</returns>
    public static bool TryNormalize(string? raw, [NotNullWhen(true)] out string? color)
    {
        color = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var trimmed = raw.Trim();
        if (!Pattern().IsMatch(trimmed))
        {
            return false;
        }

        color = trimmed.ToUpperInvariant();
        return true;
    }

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();
}
