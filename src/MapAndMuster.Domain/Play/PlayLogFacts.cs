using System.Diagnostics.CodeAnalysis;

namespace MapAndMuster.Domain.Play;

/// <summary>
/// Compact payloads stored on a play-log message so later display can name structures
/// and distinguish how a Backstab broke an alliance.
/// </summary>
public static class PlayLogFacts
{
    /// <summary>Prefix for a Pillage that removed the structure.</summary>
    public const string DestroyedStructurePrefix = "destroyed:";

    /// <summary>Co-located allied forces, which starts a battle.</summary>
    public const string BetrayalAttack = "attack";

    /// <summary>Empty allied land whose structure was auto-pillaged.</summary>
    public const string BetrayalPillage = "pillage";

    /// <summary>Empty allied land whose structure was destroyed.</summary>
    public const string BetrayalDestroy = "destroy";

    /// <summary>Empty allied land claimed without auto-pillage.</summary>
    public const string BetrayalClaim = "claim";

    /// <summary>Marks a structure name as destroyed by Pillage.</summary>
    public static string DestroyedStructure(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return DestroyedStructurePrefix + name;
    }

    /// <summary>Returns whether <paramref name="message"/> is a destroyed-structure snapshot.</summary>
    public static bool TryReadDestroyedStructure(string? message, [NotNullWhen(true)] out string? name)
    {
        if (message is not null
            && message.StartsWith(DestroyedStructurePrefix, StringComparison.Ordinal)
            && message.Length > DestroyedStructurePrefix.Length)
        {
            name = message[DestroyedStructurePrefix.Length..];
            return true;
        }

        name = null;
        return false;
    }

    /// <summary>Encodes a Backstab cause for later display.</summary>
    public static string Betrayal(string kind, Guid? victimUserId, Guid factionId, string? structureName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        var victim = victimUserId is { } id ? id.ToString("N") : "-";
        var core = $"{kind}:{victim}:{factionId:N}";
        return string.IsNullOrWhiteSpace(structureName) ? core : $"{core}:{structureName}";
    }

    /// <summary>Reads a Backstab cause encoded by <see cref="Betrayal"/>.</summary>
    public static bool TryReadBetrayal(
        string? message,
        [NotNullWhen(true)] out string? kind,
        out Guid? victimUserId,
        out Guid factionId,
        out string? structureName)
    {
        kind = null;
        victimUserId = null;
        factionId = default;
        structureName = null;
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var parts = message.Split(':', 4);
        if (parts.Length < 3
            || parts[0] is not (BetrayalAttack or BetrayalPillage or BetrayalDestroy or BetrayalClaim)
            || !Guid.TryParseExact(parts[2], "N", out factionId))
        {
            return false;
        }

        kind = parts[0];
        if (parts[1] != "-" && Guid.TryParseExact(parts[1], "N", out var victim))
        {
            victimUserId = victim;
        }

        if (parts.Length > 3 && !string.IsNullOrWhiteSpace(parts[3]))
        {
            structureName = parts[3];
        }

        return true;
    }
}
