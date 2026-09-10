namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// One enable trigger and how many times in a row it must match before the status is gained.
/// </summary>
public sealed class ForceStatusEnableCondition
{
    /// <summary>
    /// Initializes an enable condition.
    /// </summary>
    public ForceStatusEnableCondition(
        ForceStatusEnableTrigger trigger,
        int occurrences = ForceStatusOccurrences.Default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(occurrences, ForceStatusOccurrences.Min);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(occurrences, ForceStatusOccurrences.Max);
        Trigger = trigger;
        Occurrences = occurrences;
    }

    /// <summary>Gets the enable trigger.</summary>
    public ForceStatusEnableTrigger Trigger { get; }

    /// <summary>Gets how many consecutive matching triggers are required.</summary>
    public int Occurrences { get; }
}

/// <summary>
/// One clear trigger and how many times in a row it must match before the status returns to Normal.
/// </summary>
public sealed class ForceStatusClearCondition
{
    /// <summary>
    /// Initializes a clear condition.
    /// </summary>
    public ForceStatusClearCondition(
        ForceStatusClearTrigger trigger,
        int occurrences = ForceStatusOccurrences.Default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(occurrences, ForceStatusOccurrences.Min);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(occurrences, ForceStatusOccurrences.Max);
        Trigger = trigger;
        Occurrences = occurrences;
    }

    /// <summary>Gets the clear trigger.</summary>
    public ForceStatusClearTrigger Trigger { get; }

    /// <summary>Gets how many consecutive matching triggers are required.</summary>
    public int Occurrences { get; }
}
