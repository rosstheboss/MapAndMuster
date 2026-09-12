namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// A validated mechanical or display effect on an item objective.
/// </summary>
public sealed class ItemObjectiveEffectSetup
{
    /// <summary>
    /// Initializes a validated item-objective effect.
    /// </summary>
    public ItemObjectiveEffectSetup(
        Guid id,
        ItemObjectiveEffectKind kind,
        int amount = 0,
        bool amountIsPercent = false,
        IReadOnlyList<Guid>? statusTypeIds = null,
        bool immuneToAllStatuses = false,
        bool suspendCurrentAllyGroup = false,
        string? forcedAllyGroupName = null,
        IReadOnlyList<ItemObjectiveAllianceTarget>? alliedFactions = null,
        string? customText = null)
    {
        Id = id;
        Kind = kind;
        Amount = amount;
        AmountIsPercent = amountIsPercent;
        StatusTypeIds = DistinctIds(statusTypeIds);
        ImmuneToAllStatuses = immuneToAllStatuses;
        SuspendCurrentAllyGroup = suspendCurrentAllyGroup;
        ForcedAllyGroupName = string.IsNullOrWhiteSpace(forcedAllyGroupName) ? null : forcedAllyGroupName.Trim();
        AlliedFactions = alliedFactions ?? [];
        CustomText = string.IsNullOrWhiteSpace(customText) ? null : customText.Trim();
    }

    /// <summary>Gets the effect identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the effect kind.</summary>
    public ItemObjectiveEffectKind Kind { get; }

    /// <summary>Gets the signed amount for speed, supply, or army-point changes.</summary>
    public int Amount { get; }

    /// <summary>Gets whether <see cref="Amount"/> is a percent of the round army-point cap.</summary>
    public bool AmountIsPercent { get; }

    /// <summary>Gets catalog status identifiers for inflict or immunity effects.</summary>
    public IReadOnlyList<Guid> StatusTypeIds { get; }

    /// <summary>Gets whether the holder is immune to every catalog status.</summary>
    public bool ImmuneToAllStatuses { get; }

    /// <summary>Gets whether the holder's campaign ally group is ignored while the item is held.</summary>
    public bool SuspendCurrentAllyGroup { get; }

    /// <summary>Gets an ally-group name the holder is treated as belonging to, when set.</summary>
    public string? ForcedAllyGroupName { get; }

    /// <summary>Gets extra factions or subfactions treated as allied while the item is held.</summary>
    public IReadOnlyList<ItemObjectiveAllianceTarget> AlliedFactions { get; }

    /// <summary>Gets display-only reminder text for a custom battle effect.</summary>
    public string? CustomText { get; }

    private static IReadOnlyList<Guid> DistinctIds(IReadOnlyList<Guid>? ids)
    {
        if (ids is null || ids.Count == 0)
        {
            return [];
        }

        return [.. ids.Where(static id => id != Guid.Empty).Distinct()];
    }
}
