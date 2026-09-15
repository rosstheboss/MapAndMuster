namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// A validated force status other than Normal. Effects text is shown with the force; enable and
/// clear conditions are applied during resolution. Priority and cancel-out decide which status remains
/// when more than one would apply.
/// </summary>
public sealed class ForceStatusSetup
{
    /// <summary>
    /// Initializes a validated force status with one enable condition and one clear condition.
    /// </summary>
    public ForceStatusSetup(
        Guid id,
        string name,
        string effects,
        ForceStatusEnableTrigger enableTrigger,
        ForceStatusClearTrigger clearTrigger,
        int priority = ForceStatusPriority.Min,
        IReadOnlyList<Guid>? cancelsStatusIds = null,
        int enableOccurrences = ForceStatusOccurrences.Default,
        int clearOccurrences = ForceStatusOccurrences.Default)
        : this(
            id,
            name,
            effects,
            [new ForceStatusEnableCondition(enableTrigger, enableOccurrences)],
            [new ForceStatusClearCondition(clearTrigger, clearOccurrences)],
            priority,
            cancelsStatusIds)
    {
    }

    /// <summary>
    /// Initializes a validated force status.
    /// </summary>
    /// <param name="id">The status identifier.</param>
    /// <param name="name">The unique status name.</param>
    /// <param name="effects">Tabletop effect text shown to players.</param>
    /// <param name="enableConditions">When this status is applied. At least one is required.</param>
    /// <param name="clearConditions">When this status returns to Normal. At least one is required.</param>
    /// <param name="priority">Unique ranking from 0 (highest) to 999 (lowest).</param>
    /// <param name="cancelsStatusIds">Other catalog statuses this one cancels to Normal.</param>
    /// <param name="immuneFactionIds">Factions that refuse this status.</param>
    /// <param name="immuneSubfactions">Named subfactions that refuse this status.</param>
    /// <param name="clearTokenImage">Whether an existing chit or token image should be removed.</param>
    public ForceStatusSetup(
        Guid id,
        string name,
        string effects,
        IReadOnlyList<ForceStatusEnableCondition> enableConditions,
        IReadOnlyList<ForceStatusClearCondition> clearConditions,
        int priority = ForceStatusPriority.Min,
        IReadOnlyList<Guid>? cancelsStatusIds = null,
        IReadOnlyList<Guid>? immuneFactionIds = null,
        IReadOnlyList<ForceStatusImmuneSubfaction>? immuneSubfactions = null,
        bool clearTokenImage = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(effects);
        ArgumentNullException.ThrowIfNull(enableConditions);
        ArgumentNullException.ThrowIfNull(clearConditions);
        ArgumentOutOfRangeException.ThrowIfLessThan(priority, ForceStatusPriority.Min);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(priority, ForceStatusPriority.Max);
        if (enableConditions.Count == 0)
        {
            throw new ArgumentException("At least one enable condition is required.", nameof(enableConditions));
        }

        if (clearConditions.Count == 0)
        {
            throw new ArgumentException("At least one clear condition is required.", nameof(clearConditions));
        }

        if (enableConditions.Select(static condition => condition.Fingerprint()).Distinct(StringComparer.Ordinal).Count()
            != enableConditions.Count)
        {
            throw new ArgumentException("Enable conditions must be unique.", nameof(enableConditions));
        }

        if (clearConditions.Select(static condition => condition.Fingerprint()).Distinct(StringComparer.Ordinal).Count()
            != clearConditions.Count)
        {
            throw new ArgumentException("Clear conditions must be unique.", nameof(clearConditions));
        }

        Id = id;
        Name = name;
        Effects = effects;
        EnableConditions = [.. enableConditions];
        ClearConditions = [.. clearConditions];
        Priority = priority;
        CancelsStatusIds = DistinctExcluding(cancelsStatusIds, id);
        ImmuneFactionIds = DistinctIds(immuneFactionIds);
        ImmuneSubfactions = DistinctSubfactions(immuneSubfactions);
        ClearTokenImage = clearTokenImage;
    }

    /// <summary>Gets the status identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the unique status name.</summary>
    public string Name { get; }

    /// <summary>Gets tabletop effect text shown to players.</summary>
    public string Effects { get; }

    /// <summary>Gets enable conditions. Any matching condition can gain the status.</summary>
    public IReadOnlyList<ForceStatusEnableCondition> EnableConditions { get; }

    /// <summary>Gets clear conditions. Any matching condition can return the force to Normal.</summary>
    public IReadOnlyList<ForceStatusClearCondition> ClearConditions { get; }

    /// <summary>Gets the first enable trigger. Used when a single-trigger view is enough.</summary>
    public ForceStatusEnableTrigger EnableTrigger => EnableConditions[0].Trigger;

    /// <summary>Gets the first clear trigger. Used when a single-trigger view is enough.</summary>
    public ForceStatusClearTrigger ClearTrigger => ClearConditions[0].Trigger;

    /// <summary>Gets consecutive matches required by the first enable condition.</summary>
    public int EnableOccurrences => EnableConditions[0].Occurrences;

    /// <summary>Gets consecutive matches required by the first clear condition.</summary>
    public int ClearOccurrences => ClearConditions[0].Occurrences;

    /// <summary>Gets the unique ranking. Lower numbers outrank higher numbers.</summary>
    public int Priority { get; }

    /// <summary>
    /// Gets catalog identifiers this status cancels. If this status would be gained while the force
    /// has one of these, both are removed and the force has no status.
    /// </summary>
    public IReadOnlyList<Guid> CancelsStatusIds { get; }

    /// <summary>Gets factions that refuse this named status.</summary>
    public IReadOnlyList<Guid> ImmuneFactionIds { get; }

    /// <summary>Gets named subfactions that refuse this named status.</summary>
    public IReadOnlyList<ForceStatusImmuneSubfaction> ImmuneSubfactions { get; }

    /// <summary>Gets whether an existing chit or token image should be removed.</summary>
    public bool ClearTokenImage { get; }

    /// <summary>Gets whether this status lists any configured immunities.</summary>
    public bool HasConfiguredImmunities => ImmuneFactionIds.Count > 0 || ImmuneSubfactions.Count > 0;

    /// <summary>Returns whether this status lists the faction or named subfaction as immune.</summary>
    public bool Refuses(Guid factionId, string? subfaction)
    {
        if (ImmuneFactionIds.Contains(factionId))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(subfaction))
        {
            return false;
        }

        return ImmuneSubfactions.Any(item =>
            item.FactionId == factionId
            && string.Equals(item.Subfaction, subfaction, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<Guid> DistinctIds(IReadOnlyList<Guid>? ids)
    {
        if (ids is null || ids.Count == 0)
        {
            return [];
        }

        return
        [
            .. ids
                .Where(static id => id != Guid.Empty)
                .Distinct(),
        ];
    }

    private static List<ForceStatusImmuneSubfaction> DistinctSubfactions(
        IReadOnlyList<ForceStatusImmuneSubfaction>? listed)
    {
        if (listed is null || listed.Count == 0)
        {
            return [];
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<ForceStatusImmuneSubfaction>();
        foreach (var item in listed)
        {
            var key = $"{item.FactionId:N}:{item.Subfaction}";
            if (seen.Add(key))
            {
                result.Add(item);
            }
        }

        return result;
    }

    private static IReadOnlyList<Guid> DistinctExcluding(IReadOnlyList<Guid>? ids, Guid self)
    {
        if (ids is null || ids.Count == 0)
        {
            return [];
        }

        return
        [
            .. ids
                .Where(id => id != self && id != Guid.Empty)
                .Distinct(),
        ];
    }
}
