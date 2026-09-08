namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// One ordered win/lose status-change condition on a mission. The first matching condition applies.
/// </summary>
public sealed class MissionStatusChangeSetup
{
    /// <summary>
    /// Initializes a validated mission status-change condition.
    /// </summary>
    /// <param name="id">The condition identifier.</param>
    /// <param name="outcome">Whether this applies after a win or a loss.</param>
    /// <param name="whenCurrentStatus">Required current status, or null to match any remaining status.</param>
    /// <param name="setStatus">Status to apply, or null for Normal. Ignored when <paramref name="leaveUnchanged"/> is true.</param>
    /// <param name="leaveUnchanged">When true, the force keeps its current status.</param>
    public MissionStatusChangeSetup(
        Guid id,
        MissionBattleOutcome outcome,
        string? whenCurrentStatus,
        string? setStatus,
        bool leaveUnchanged)
    {
        Id = id;
        Outcome = outcome;
        WhenCurrentStatus = string.IsNullOrWhiteSpace(whenCurrentStatus) ? null : whenCurrentStatus.Trim();
        SetStatus = string.IsNullOrWhiteSpace(setStatus) || ForceStatusNames.IsNormal(setStatus)
            ? null
            : setStatus.Trim();
        LeaveUnchanged = leaveUnchanged;
    }

    /// <summary>Gets the condition identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets whether this applies after a win or a loss.</summary>
    public MissionBattleOutcome Outcome { get; }

    /// <summary>Gets the required current status, or null to match any remaining status.</summary>
    public string? WhenCurrentStatus { get; }

    /// <summary>Gets the status to apply, or null for Normal.</summary>
    public string? SetStatus { get; }

    /// <summary>Gets whether the force keeps its current status.</summary>
    public bool LeaveUnchanged { get; }
}
