namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// User-supplied mission status-change condition.
/// </summary>
public sealed class MissionStatusChangeInput
{
    /// <summary>Gets the client-assigned identifier, when present.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets Win or Lose.</summary>
    public string? Outcome { get; init; }

    /// <summary>Gets the required current status, or null/empty to match any remaining status.</summary>
    public string? WhenCurrentStatus { get; init; }

    /// <summary>Gets the status to apply, or null/Normal for no named status.</summary>
    public string? SetStatus { get; init; }

    /// <summary>Gets whether the force keeps its current status.</summary>
    public bool LeaveUnchanged { get; init; }
}
