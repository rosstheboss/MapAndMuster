namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// One enable trigger, consecutive-occurrence count, and optional location filter.
/// </summary>
public sealed class ForceStatusEnableCondition
{
    /// <summary>
    /// Initializes an enable condition.
    /// </summary>
    public ForceStatusEnableCondition(
        ForceStatusEnableTrigger trigger,
        int occurrences = ForceStatusOccurrences.Default,
        Guid? id = null,
        ConditionLocation? location = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(occurrences, ForceStatusOccurrences.Min);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(occurrences, ForceStatusOccurrences.Max);
        Id = id is { } assigned && assigned != Guid.Empty ? assigned : Guid.NewGuid();
        Trigger = trigger;
        Occurrences = occurrences;
        Location = location ?? ConditionLocation.Any;
    }

    /// <summary>Gets the stable condition identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the enable trigger.</summary>
    public ForceStatusEnableTrigger Trigger { get; }

    /// <summary>Gets how many consecutive matching triggers are required.</summary>
    public int Occurrences { get; }

    /// <summary>Gets where this condition must match.</summary>
    public ConditionLocation Location { get; }

    /// <summary>
    /// Returns a fingerprint used to detect duplicate conditions.
    /// </summary>
    public string Fingerprint()
    {
        return $"{Trigger}:{Occurrences}:{Location.Fingerprint()}";
    }
}

/// <summary>
/// One clear trigger, consecutive-occurrence count, and optional location filter.
/// </summary>
public sealed class ForceStatusClearCondition
{
    /// <summary>
    /// Initializes a clear condition.
    /// </summary>
    public ForceStatusClearCondition(
        ForceStatusClearTrigger trigger,
        int occurrences = ForceStatusOccurrences.Default,
        Guid? id = null,
        ConditionLocation? location = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(occurrences, ForceStatusOccurrences.Min);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(occurrences, ForceStatusOccurrences.Max);
        Id = id is { } assigned && assigned != Guid.Empty ? assigned : Guid.NewGuid();
        Trigger = trigger;
        Occurrences = occurrences;
        Location = location ?? ConditionLocation.Any;
    }

    /// <summary>Gets the stable condition identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the clear trigger.</summary>
    public ForceStatusClearTrigger Trigger { get; }

    /// <summary>Gets how many consecutive matching triggers are required.</summary>
    public int Occurrences { get; }

    /// <summary>Gets where this condition must match.</summary>
    public ConditionLocation Location { get; }

    /// <summary>
    /// Returns a fingerprint used to detect duplicate conditions.
    /// </summary>
    public string Fingerprint()
    {
        return $"{Trigger}:{Occurrences}:{Location.Fingerprint()}";
    }
}
