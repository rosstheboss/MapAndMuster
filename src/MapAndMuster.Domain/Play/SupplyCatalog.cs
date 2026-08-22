using MapAndMuster.Domain.Campaigns;

namespace MapAndMuster.Domain.Play;

/// <summary>
/// Catalog facts used to calculate per-player supply.
/// </summary>
public sealed class SupplyCatalog
{
    /// <summary>
    /// Initializes a supply catalog.
    /// </summary>
    public SupplyCatalog(
        IReadOnlyDictionary<Guid, int> terrainSupplyByType,
        IReadOnlyDictionary<Guid, StructureSupplyRules> structures,
        int splitForceSupplyPenaltyPercent,
        IReadOnlyList<RoundArmyEscalationSetup> armyEscalations,
        IReadOnlyDictionary<Guid, Guid> factionByPlayer,
        IReadOnlyDictionary<Guid, string?> allyGroupByFaction,
        IReadOnlySet<Guid> brokenAllyFactionIds,
        SpecialRuleContext? specialRules = null,
        IReadOnlyDictionary<Guid, string?>? subfactionByPlayer = null,
        bool splitForceSupplyPenaltyIsPercent = HuntInEstaliaDefaults.SplitForceSupplyPenaltyIsPercent)
    {
        ArgumentNullException.ThrowIfNull(terrainSupplyByType);
        ArgumentNullException.ThrowIfNull(structures);
        ArgumentNullException.ThrowIfNull(armyEscalations);
        ArgumentNullException.ThrowIfNull(factionByPlayer);
        ArgumentNullException.ThrowIfNull(allyGroupByFaction);
        ArgumentNullException.ThrowIfNull(brokenAllyFactionIds);
        ArgumentOutOfRangeException.ThrowIfNegative(splitForceSupplyPenaltyPercent);
        TerrainSupplyByType = terrainSupplyByType;
        Structures = structures;
        SplitForceSupplyPenaltyPercent = splitForceSupplyPenaltyPercent;
        SplitForceSupplyPenaltyIsPercent = splitForceSupplyPenaltyIsPercent;
        ArmyEscalations = armyEscalations;
        FactionByPlayer = factionByPlayer;
        AllyGroupByFaction = allyGroupByFaction;
        BrokenAllyFactionIds = brokenAllyFactionIds;
        SpecialRules = specialRules ?? SpecialRuleContext.None;
        SubfactionByPlayer = subfactionByPlayer ?? new Dictionary<Guid, string?>();
    }

    /// <summary>Gets supply points for each terrain type.</summary>
    public IReadOnlyDictionary<Guid, int> TerrainSupplyByType { get; }

    /// <summary>Gets supply rules for each structure type.</summary>
    public IReadOnlyDictionary<Guid, StructureSupplyRules> Structures { get; }

    /// <summary>Gets the amount subtracted from map supply when a player has split forces.</summary>
    public int SplitForceSupplyPenaltyPercent { get; }

    /// <summary>Gets whether the split-force supply penalty is a percent of map supply.</summary>
    public bool SplitForceSupplyPenaltyIsPercent { get; }

    /// <summary>Gets per-round army escalation.</summary>
    public IReadOnlyList<RoundArmyEscalationSetup> ArmyEscalations { get; }

    /// <summary>Gets each player's chosen faction.</summary>
    public IReadOnlyDictionary<Guid, Guid> FactionByPlayer { get; }

    /// <summary>Gets ally-group names by faction.</summary>
    public IReadOnlyDictionary<Guid, string?> AllyGroupByFaction { get; }

    /// <summary>Gets factions that left their ally group.</summary>
    public IReadOnlySet<Guid> BrokenAllyFactionIds { get; }

    /// <summary>Gets mechanical special rules.</summary>
    public SpecialRuleContext SpecialRules { get; }

    /// <summary>Gets each player's chosen subfaction.</summary>
    public IReadOnlyDictionary<Guid, string?> SubfactionByPlayer { get; }
}

/// <summary>
/// Supply values for one structure type.
/// </summary>
/// <param name="SupplyPoints">Ongoing map supply while the structure is operational.</param>
/// <param name="PillageSupplyPoints">Temporary supply awarded when the structure is pillaged.</param>
/// <param name="DestroySupplyPoints">Temporary supply awarded when the structure is destroyed.</param>
public sealed record StructureSupplyRules(int SupplyPoints, int PillageSupplyPoints, int DestroySupplyPoints);
