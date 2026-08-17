namespace Campaign.Domain.Campaigns;

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
}
