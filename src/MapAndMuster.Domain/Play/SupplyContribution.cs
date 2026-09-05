namespace MapAndMuster.Domain.Play;

/// <summary>
/// One line in a player's current supply total: a territory, structure, bonus, temporary pool, or penalty.
/// </summary>
/// <param name="Kind">The source category.</param>
/// <param name="TerritoryId">The related territory, when the source is a holding or location bonus.</param>
/// <param name="Points">Signed points this source adds to the displayed total. Penalties are negative.</param>
/// <param name="SourceName">Structure name, special-rule key, or a fixed label such as terrain.</param>
/// <param name="IsAllied">Whether the holding belongs to an ally rather than the player.</param>
public sealed record SupplyContribution(
    SupplyContributionKind Kind,
    Guid? TerritoryId,
    int Points,
    string SourceName,
    bool IsAllied);

/// <summary>
/// Categories shown in a supply-point breakdown.
/// </summary>
public enum SupplyContributionKind
{
    /// <summary>Configured supply from a connected territory's terrain.</summary>
    TerritoryTerrain,

    /// <summary>Configured supply from an operational structure on a connected territory.</summary>
    TerritoryStructure,

    /// <summary>A special-rule bonus, including path-independent holdings.</summary>
    SpecialRule,

    /// <summary>Free supply granted for the current round.</summary>
    RoundFree,

    /// <summary>Remaining player-pool temporary supply.</summary>
    Temporary,

    /// <summary>Map supply subtracted because the player currently has split forces.</summary>
    SplitPenalty,

    /// <summary>A mission attacker/defender supply adjustment for a specific battle.</summary>
    MissionAdvantage,
}
