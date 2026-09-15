namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// User-supplied force status for campaign setup. Normal is omitted.
/// </summary>
public sealed class ForceStatusInput
{
    /// <summary>Gets the client-assigned identifier, when present.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets the status name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets tabletop effect text shown to players.</summary>
    public string? Effects { get; init; }

    /// <summary>Gets the enable-trigger name when a single condition is supplied.</summary>
    public string? EnableTrigger { get; init; }

    /// <summary>Gets the clear-trigger name when a single condition is supplied.</summary>
    public string? ClearTrigger { get; init; }

    /// <summary>Gets enable conditions. Any matching condition can gain the status.</summary>
    public IReadOnlyList<ForceStatusConditionInput>? EnableConditions { get; init; }

    /// <summary>Gets clear conditions. Any matching condition can return the force to Normal.</summary>
    public IReadOnlyList<ForceStatusConditionInput>? ClearConditions { get; init; }

    /// <summary>Gets the unique ranking from 0 (highest) to 999 (lowest), when supplied.</summary>
    public int? Priority { get; init; }

    /// <summary>Gets catalog identifiers this status cancels to Normal, when supplied.</summary>
    public IReadOnlyList<Guid>? CancelsStatusIds { get; init; }

    /// <summary>Gets factions that refuse this status, when supplied.</summary>
    public IReadOnlyList<Guid>? ImmuneFactionIds { get; init; }

    /// <summary>Gets named subfactions that refuse this status, when supplied.</summary>
    public IReadOnlyList<ForceStatusImmuneSubfactionInput>? ImmuneSubfactions { get; init; }

    /// <summary>Gets whether an existing chit or token image should be removed.</summary>
    public bool ClearTokenImage { get; init; }

    /// <summary>Gets how many times in a row a single enable trigger must match, when supplied.</summary>
    public int? EnableOccurrences { get; init; }

    /// <summary>Gets how many times in a row a single clear trigger must match, when supplied.</summary>
    public int? ClearOccurrences { get; init; }
}

/// <summary>
/// One enable or clear trigger, consecutive-occurrence count, and optional location filter.
/// </summary>
public sealed class ForceStatusConditionInput
{
    /// <summary>Gets the client-assigned identifier, when present.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets the trigger name.</summary>
    public string? Trigger { get; init; }

    /// <summary>Gets how many times in a row the trigger must match, when supplied.</summary>
    public int? Occurrences { get; init; }

    /// <summary>Gets Any, TerrainType, TerrainTag, StructureType, or StructureTag.</summary>
    public string? LocationKind { get; init; }

    /// <summary>Gets the terrain or structure type when the location is a type filter.</summary>
    public Guid? LocationTypeId { get; init; }

    /// <summary>Gets the terrain or structure tag when the location is a tag filter.</summary>
    public Guid? LocationTagId { get; init; }

    /// <summary>
    /// Gets the catalog status another occupying force must have when the trigger is
    /// OccupyingWithSpecifiedStatus.
    /// </summary>
    public Guid? RequiredStatusId { get; init; }

    /// <summary>
    /// Gets the standard battle-result question that must be achieved when the trigger is
    /// StandardBattleResultQuestion.
    /// </summary>
    public Guid? RequiredQuestionId { get; init; }
}

/// <summary>
/// A named subfaction that refuses a catalog force status.
/// </summary>
public sealed class ForceStatusImmuneSubfactionInput
{
    /// <summary>Gets the parent faction.</summary>
    public Guid FactionId { get; init; }

    /// <summary>Gets the subfaction name.</summary>
    public string? Subfaction { get; init; }
}
