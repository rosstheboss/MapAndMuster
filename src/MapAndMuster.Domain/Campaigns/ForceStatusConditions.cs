namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// One enable trigger, consecutive-occurrence count, optional location filter, and optional
/// co-located force status.
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
        ConditionLocation? location = null,
        Guid? requiredStatusId = null,
        Guid? requiredQuestionId = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(occurrences, ForceStatusOccurrences.Min);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(occurrences, ForceStatusOccurrences.Max);
        Id = id is { } assigned && assigned != Guid.Empty ? assigned : Guid.NewGuid();
        Trigger = trigger;
        Occurrences = occurrences;
        Location = location ?? ConditionLocation.Any;
        RequiredStatusId = requiredStatusId is { } status && status != Guid.Empty ? status : null;
        RequiredQuestionId = requiredQuestionId is { } question && question != Guid.Empty ? question : null;
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
    /// Gets the catalog status another occupying force must have when the trigger is
    /// <see cref="ForceStatusEnableTrigger.OccupyingWithSpecifiedStatus"/>.
    /// </summary>
    public Guid? RequiredStatusId { get; }

    /// <summary>
    /// Gets the standard battle-result question that must be achieved when the trigger is
    /// <see cref="ForceStatusEnableTrigger.StandardBattleResultQuestion"/>.
    /// </summary>
    public Guid? RequiredQuestionId { get; }

    /// <summary>
    /// Returns a fingerprint used to detect duplicate conditions.
    /// </summary>
    public string Fingerprint()
    {
        return $"{Trigger}:{Occurrences}:{Location.Fingerprint()}:{RequiredStatusId?.ToString("D") ?? "-"}:{RequiredQuestionId?.ToString("D") ?? "-"}";
    }
}

/// <summary>
/// One clear trigger, consecutive-occurrence count, optional location filter, and optional
/// co-located force status.
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
        ConditionLocation? location = null,
        Guid? requiredStatusId = null,
        Guid? requiredQuestionId = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(occurrences, ForceStatusOccurrences.Min);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(occurrences, ForceStatusOccurrences.Max);
        Id = id is { } assigned && assigned != Guid.Empty ? assigned : Guid.NewGuid();
        Trigger = trigger;
        Occurrences = occurrences;
        Location = location ?? ConditionLocation.Any;
        RequiredStatusId = requiredStatusId is { } status && status != Guid.Empty ? status : null;
        RequiredQuestionId = requiredQuestionId is { } question && question != Guid.Empty ? question : null;
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
    /// Gets the catalog status another occupying force must have when the trigger is
    /// <see cref="ForceStatusClearTrigger.OccupyingWithSpecifiedStatus"/>.
    /// </summary>
    public Guid? RequiredStatusId { get; }

    /// <summary>
    /// Gets the standard battle-result question that must be achieved when the trigger is
    /// <see cref="ForceStatusClearTrigger.StandardBattleResultQuestion"/>.
    /// </summary>
    public Guid? RequiredQuestionId { get; }

    /// <summary>
    /// Returns a fingerprint used to detect duplicate conditions.
    /// </summary>
    public string Fingerprint()
    {
        return $"{Trigger}:{Occurrences}:{Location.Fingerprint()}:{RequiredStatusId?.ToString("D") ?? "-"}:{RequiredQuestionId?.ToString("D") ?? "-"}";
    }
}
