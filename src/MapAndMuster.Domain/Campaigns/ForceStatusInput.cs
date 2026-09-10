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

    /// <summary>Gets how many times in a row a single enable trigger must match, when supplied.</summary>
    public int? EnableOccurrences { get; init; }

    /// <summary>Gets how many times in a row a single clear trigger must match, when supplied.</summary>
    public int? ClearOccurrences { get; init; }
}

/// <summary>
/// One enable or clear trigger and its consecutive-occurrence count.
/// </summary>
public sealed class ForceStatusConditionInput
{
    /// <summary>Gets the trigger name.</summary>
    public string? Trigger { get; init; }

    /// <summary>Gets how many times in a row the trigger must match, when supplied.</summary>
    public int? Occurrences { get; init; }
}
