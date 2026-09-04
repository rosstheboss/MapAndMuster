namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// A validated private-objective catalog entry.
/// </summary>
public sealed class PrivateObjectiveTypeSetup
{
    /// <summary>
    /// Initializes a validated private objective type.
    /// </summary>
    public PrivateObjectiveTypeSetup(
        Guid id,
        string name,
        string? description,
        int campaignPoints,
        IReadOnlyList<PrivateObjectiveHolderKind> allowedHolderKinds,
        PrivateObjectiveScoringKind scoringKind,
        PrivateObjectiveAutomaticKind automaticKind,
        int requiredCount,
        Guid? structureTypeId,
        IReadOnlyList<Guid> territoryIds,
        bool matchesAnyStructureType = false,
        Guid? itemObjectiveTypeId = null,
        bool matchesAnyItemObjective = false,
        PrivateObjectiveTargetKind targetKind = PrivateObjectiveTargetKind.None,
        PrivateObjectiveTargetSelection targetSelection = PrivateObjectiveTargetSelection.Specific,
        Guid? targetId = null,
        IReadOnlyList<Guid>? forceStatusTypeIds = null,
        PrivateObjectiveStatusMatchKind statusMatchKind = PrivateObjectiveStatusMatchKind.None,
        Guid? prerequisiteForceStatusTypeId = null,
        bool prerequisiteWasLost = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(allowedHolderKinds);
        ArgumentNullException.ThrowIfNull(territoryIds);
        ArgumentOutOfRangeException.ThrowIfNegative(campaignPoints);
        ArgumentOutOfRangeException.ThrowIfNegative(requiredCount);
        Id = id;
        Name = name;
        Description = description;
        CampaignPoints = campaignPoints;
        AllowedHolderKinds = allowedHolderKinds;
        ScoringKind = scoringKind;
        AutomaticKind = automaticKind;
        RequiredCount = requiredCount;
        StructureTypeId = structureTypeId;
        TerritoryIds = territoryIds;
        MatchesAnyStructureType = matchesAnyStructureType;
        ItemObjectiveTypeId = itemObjectiveTypeId;
        MatchesAnyItemObjective = matchesAnyItemObjective;
        TargetKind = targetKind;
        TargetSelection = targetSelection;
        TargetId = targetId;
        ForceStatusTypeIds = forceStatusTypeIds ?? [];
        StatusMatchKind = statusMatchKind;
        PrerequisiteForceStatusTypeId = prerequisiteForceStatusTypeId;
        PrerequisiteWasLost = prerequisiteWasLost;
    }

    /// <summary>Gets the catalog identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the objective name.</summary>
    public string Name { get; }

    /// <summary>Gets the optional secret description.</summary>
    public string? Description { get; }

    /// <summary>Gets campaign points awarded when revealed or completed.</summary>
    public int CampaignPoints { get; }

    /// <summary>Gets holder kinds this entry may be assigned to.</summary>
    public IReadOnlyList<PrivateObjectiveHolderKind> AllowedHolderKinds { get; }

    /// <summary>Gets whether scoring is manual or automatic.</summary>
    public PrivateObjectiveScoringKind ScoringKind { get; }

    /// <summary>Gets the automatic criterion kind.</summary>
    public PrivateObjectiveAutomaticKind AutomaticKind { get; }

    /// <summary>Gets how many matching facts complete an automatic objective.</summary>
    public int RequiredCount { get; }

    /// <summary>Gets the structure type for structure-based automatic criteria.</summary>
    public Guid? StructureTypeId { get; }

    /// <summary>Gets named territories for ControlNamedTerritories.</summary>
    public IReadOnlyList<Guid> TerritoryIds { get; }

    /// <summary>Gets whether Build or Repair matches any structure type.</summary>
    public bool MatchesAnyStructureType { get; }

    /// <summary>Gets the item-objective type for relic criteria.</summary>
    public Guid? ItemObjectiveTypeId { get; }

    /// <summary>Gets whether relic criteria match any item objective.</summary>
    public bool MatchesAnyItemObjective { get; }

    /// <summary>Gets the opponent kind for DefeatOpponent.</summary>
    public PrivateObjectiveTargetKind TargetKind { get; }

    /// <summary>Gets how DefeatOpponent chooses its opponent.</summary>
    public PrivateObjectiveTargetSelection TargetSelection { get; }

    /// <summary>Gets the specific opponent identifier when selection is Specific.</summary>
    public Guid? TargetId { get; }

    /// <summary>Gets force-status catalog identifiers for ForceStatus criteria.</summary>
    public IReadOnlyList<Guid> ForceStatusTypeIds { get; }

    /// <summary>Gets how ForceStatus facts are matched.</summary>
    public PrivateObjectiveStatusMatchKind StatusMatchKind { get; }

    /// <summary>Gets the prior status for GainedAfter.</summary>
    public Guid? PrerequisiteForceStatusTypeId { get; }

    /// <summary>Gets whether GainedAfter waits for the prerequisite to have been lost.</summary>
    public bool PrerequisiteWasLost { get; }

    /// <summary>Gets whether this catalog entry may be assigned to <paramref name="kind"/>.</summary>
    public bool Allows(PrivateObjectiveHolderKind kind)
    {
        return AllowedHolderKinds.Contains(kind);
    }
}
