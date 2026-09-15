namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// User-supplied mechanical or display effect on an item objective.
/// </summary>
public sealed class ItemObjectiveEffectInput
{
    /// <summary>Gets the client-assigned identifier, when present.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets the effect kind name.</summary>
    public required string Kind { get; init; }

    /// <summary>Gets the signed amount for speed, supply, or army-point changes.</summary>
    public int? Amount { get; init; }

    /// <summary>Gets whether the amount is a percent of the round army-point cap.</summary>
    public bool AmountIsPercent { get; init; }

    /// <summary>Gets catalog status identifiers for inflict or immunity effects.</summary>
    public IReadOnlyList<Guid>? StatusTypeIds { get; init; }

    /// <summary>Gets whether the holder is immune to every catalog status.</summary>
    public bool ImmuneToAllStatuses { get; init; }

    /// <summary>Gets whether the holder's campaign ally group is ignored while the item is held.</summary>
    public bool SuspendCurrentAllyGroup { get; init; }

    /// <summary>Gets an ally-group name the holder is treated as belonging to.</summary>
    public string? ForcedAllyGroupName { get; init; }

    /// <summary>Gets extra factions treated as allied while the item is held.</summary>
    public IReadOnlyList<ItemObjectiveAllianceTargetInput>? AlliedFactions { get; init; }

    /// <summary>Gets display-only reminder text for a custom battle effect.</summary>
    public string? CustomText { get; init; }

    /// <summary>Gets the catalog status applied after this special action succeeds.</summary>
    public Guid? SuccessStatusTypeId { get; init; }

    /// <summary>Gets the catalog status applied after this special action fails.</summary>
    public Guid? FailureStatusTypeId { get; init; }
}

/// <summary>
/// User-supplied alliance target on an item-objective effect.
/// </summary>
public sealed class ItemObjectiveAllianceTargetInput
{
    /// <summary>Gets the faction treated as allied.</summary>
    public Guid FactionId { get; init; }

    /// <summary>Gets the subfaction scope, when set.</summary>
    public string? Subfaction { get; init; }
}
