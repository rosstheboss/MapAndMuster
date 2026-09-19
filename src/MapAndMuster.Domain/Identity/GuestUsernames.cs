using System.Text.RegularExpressions;
using MapAndMuster.Domain.Common;

namespace MapAndMuster.Domain.Identity;

/// <summary>
/// Allocated guest handles of the form Guest001. Real accounts cannot register these names.
/// </summary>
public static partial class GuestUsernames
{
    /// <summary>
    /// Returns whether the username is reserved for temporary guest sessions.
    /// </summary>
    /// <param name="raw">The candidate username.</param>
    /// <returns><see langword="true"/> when the name matches an allocated guest handle.</returns>
    public static bool Matches(string? raw)
    {
        return !string.IsNullOrWhiteSpace(raw) && AllocatedPattern().IsMatch(raw.Trim());
    }

    /// <summary>
    /// Builds the field-scoped error used when a player tries to register a guest handle.
    /// </summary>
    /// <returns>The domain error.</returns>
    public static DomainError Error()
    {
        return new DomainError("username.reserved", "That username is reserved.", "username");
    }

    [GeneratedRegex("^Guest[0-9]+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AllocatedPattern();
}
