using System.Diagnostics.CodeAnalysis;
using MapAndMuster.Domain.Common;

namespace MapAndMuster.Domain.Identity;

/// <summary>
/// An optional name suffix chosen from a fixed English list, including Roman numerals through X.
/// </summary>
public static class NameSuffix
{
    /// <summary>
    /// Canonical suffix values offered to users.
    /// </summary>
    public static readonly IReadOnlyList<string> AllowedValues =
    [
        "Jr.",
        "Sr.",
        "I",
        "II",
        "III",
        "IV",
        "V",
        "VI",
        "VII",
        "VIII",
        "IX",
        "X",
    ];

    /// <summary>
    /// Attempts to parse an optional suffix. Empty input means no suffix.
    /// </summary>
    /// <param name="raw">The raw suffix.</param>
    /// <param name="suffix">The canonical suffix when present and valid.</param>
    /// <param name="error">The validation error when the value is present but not allowed.</param>
    /// <returns><see langword="true"/> when the input is empty or an allowed suffix.</returns>
    public static bool TryCreateOptional(
        string? raw,
        out string? suffix,
        [NotNullWhen(false)] out DomainError? error)
    {
        suffix = null;
        error = null;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return true;
        }

        var trimmed = raw.Trim();
        foreach (var allowed in AllowedValues)
        {
            if (IsMatch(trimmed, allowed))
            {
                suffix = allowed;
                return true;
            }
        }

        error = new DomainError(
            "suffix.invalid",
            "Suffix must be one of Jr., Sr., or Roman numerals I through X.",
            "suffix");
        return false;
    }

    private static bool IsMatch(string raw, string allowed)
    {
        if (string.Equals(raw, allowed, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (allowed.EndsWith('.')
            && string.Equals(raw, allowed.TrimEnd('.'), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
