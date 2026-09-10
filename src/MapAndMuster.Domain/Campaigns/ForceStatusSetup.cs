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
    public ForceStatusSetup(
        Guid id,
        string name,
        string effects,
        IReadOnlyList<ForceStatusEnableCondition> enableConditions,
        IReadOnlyList<ForceStatusClearCondition> clearConditions,
        int priority = ForceStatusPriority.Min,
        IReadOnlyList<Guid>? cancelsStatusIds = null)
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

        if (enableConditions.Select(static condition => condition.Trigger).Distinct().Count() != enableConditions.Count)
        {
            throw new ArgumentException("Enable triggers must be unique.", nameof(enableConditions));
        }

        if (clearConditions.Select(static condition => condition.Trigger).Distinct().Count() != clearConditions.Count)
        {
            throw new ArgumentException("Clear triggers must be unique.", nameof(clearConditions));
        }

        Id = id;
        Name = name;
        Effects = effects;
        EnableConditions = [.. enableConditions];
        ClearConditions = [.. clearConditions];
        Priority = priority;
        CancelsStatusIds = DistinctExcluding(cancelsStatusIds, id);
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
