namespace Campaign.Domain.Play;

/// <summary>
/// Game systems whose army-list text the application may try to parse.
/// </summary>
public static class ArmyListGameSystems
{
    /// <summary>Stored identifier for Warhammer: The Old World.</summary>
    public const string WarhammerTheOldWorld = "WarhammerTheOldWorld";

    /// <summary>Player-facing label for Warhammer: The Old World.</summary>
    public const string WarhammerTheOldWorldDisplayName = "Warhammer: The Old World";

    /// <summary>
    /// Whether this game system currently has a list parser.
    /// </summary>
    /// <param name="gameSystem">The stored game-system identifier.</param>
    /// <returns><see langword="true"/> when automatic parsing is implemented.</returns>
    public static bool CanParse(string? gameSystem)
    {
        return string.Equals(gameSystem, WarhammerTheOldWorld, StringComparison.Ordinal);
    }
}
