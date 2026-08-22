using System.Collections.Frozen;

namespace MapAndMuster.Domain.Identity;

/// <summary>
/// Usernames that collide with chat recipients, audience keywords, or system identities.
/// Comparison is case-insensitive.
/// </summary>
public static class ReservedUsernames
{
    private static readonly FrozenSet<string> Names = FrozenSet.ToFrozenSet(
        [
            "admin",
            "administrator",
            "all",
            "alliance",
            "allies",
            "ally",
            "announcement",
            "anonymous",
            "anybody",
            "bot",
            "broadcast",
            "channel",
            "chat",
            "direct",
            "everyone",
            "faction",
            "game",
            "global",
            "group",
            "guest",
            "help",
            "here",
            "log",
            "mention",
            "mod",
            "moderator",
            "news",
            "none",
            "null",
            "official",
            "owner",
            "party",
            "player",
            "players",
            "private",
            "public",
            "root",
            "self",
            "server",
            "staff",
            "support",
            "system",
            "team",
            "unknown",
            "user",
            "users",
            "world",
            "you",
        ],
        StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns whether the username is reserved for chat or system keywords.
    /// </summary>
    /// <param name="raw">The candidate username.</param>
    /// <returns><see langword="true"/> when the name is reserved.</returns>
    public static bool Contains(string? raw)
    {
        return !string.IsNullOrWhiteSpace(raw) && Names.Contains(raw.Trim());
    }

    /// <summary>
    /// Builds the field-scoped error used when a username is reserved.
    /// </summary>
    /// <returns>The domain error.</returns>
    public static Common.DomainError Error()
    {
        return new Common.DomainError("username.reserved", "That username is reserved.", "username");
    }
}
