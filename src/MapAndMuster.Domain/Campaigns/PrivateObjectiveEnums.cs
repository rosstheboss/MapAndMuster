namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// Who may be assigned a private objective.
/// </summary>
public enum PrivateObjectiveHolderKind
{
    /// <summary>One occupying player.</summary>
    Player = 0,

    /// <summary>One campaign faction.</summary>
    Faction = 1,

    /// <summary>One ally group.</summary>
    AllyGroup = 2,
}

/// <summary>
/// How a private objective is scored.
/// </summary>
public enum PrivateObjectiveScoringKind
{
    /// <summary>A holder claims it; a manager must approve points.</summary>
    Manual = 0,

    /// <summary>Map facts complete it without a claim.</summary>
    Automatic = 1,
}

/// <summary>
/// Map criterion for an automatic private objective.
/// </summary>
public enum PrivateObjectiveAutomaticKind
{
    /// <summary>No automatic criterion.</summary>
    None = 0,

    /// <summary>Currently control at least a configured number of territories.</summary>
    ControlTerritoryCount = 1,

    /// <summary>Currently control the listed territories, or at least the required count of them.</summary>
    ControlNamedTerritories = 2,

    /// <summary>Currently control at least a configured number of a structure type.</summary>
    ControlStructureType = 3,

    /// <summary>Currently control at least a configured number of pillaged structures of a type.</summary>
    PillageStructureType = 4,

    /// <summary>Have destroyed at least a configured number of a structure type.</summary>
    DestroyStructureType = 5,

    /// <summary>Have won at least a configured number of finalized battles.</summary>
    BattleWinCount = 6,

    /// <summary>Have lost at least a configured number of finalized battles.</summary>
    BattleLossCount = 7,

    /// <summary>Have recorded at least a configured number of player-chosen retreats.</summary>
    PlayerRetreatCount = 8,

    /// <summary>Currently occupy the same territory as a relic, or a territory with a direct adjacency to it.</summary>
    AdjacentToRelic = 9,

    /// <summary>Have completed at least a configured number of Build actions.</summary>
    BuildStructureType = 10,

    /// <summary>Have completed at least a configured number of Repair actions.</summary>
    RepairStructureType = 11,

    /// <summary>Currently possess a relic.</summary>
    ControlRelic = 12,

    /// <summary>Have defeated a configured opponent in a finalized battle.</summary>
    DefeatOpponent = 13,

    /// <summary>Have gained, caused, or sequenced a configured force status.</summary>
    ForceStatus = 14,
}

/// <summary>
/// Who a DefeatOpponent automatic private objective is scored against.
/// </summary>
public enum PrivateObjectiveTargetKind
{
    /// <summary>No opponent filter.</summary>
    None = 0,

    /// <summary>A player.</summary>
    Player = 1,

    /// <summary>A faction.</summary>
    Faction = 2,

    /// <summary>An ally group.</summary>
    AllyGroup = 3,
}

/// <summary>
/// How a DefeatOpponent target is chosen.
/// </summary>
public enum PrivateObjectiveTargetSelection
{
    /// <summary>A specific catalog identifier.</summary>
    Specific = 0,

    /// <summary>Any opposing player, faction, or ally group of the configured kind.</summary>
    Any = 1,

    /// <summary>A single opponent chosen at assignment from that kind's pool.</summary>
    Random = 2,
}

/// <summary>
/// How a ForceStatus automatic private objective matches status facts.
/// </summary>
public enum PrivateObjectiveStatusMatchKind
{
    /// <summary>No status match.</summary>
    None = 0,

    /// <summary>The holder gained a configured status the required number of times.</summary>
    Gained = 1,

    /// <summary>The holder caused another force to gain a configured status.</summary>
    Caused = 2,

    /// <summary>The holder gained a configured status after gaining or losing another status.</summary>
    GainedAfter = 3,
}
