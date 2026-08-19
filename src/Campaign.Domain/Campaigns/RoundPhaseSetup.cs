namespace Campaign.Domain.Campaigns;

/// <summary>
/// A validated action or battle step inside a round.
/// </summary>
public sealed class RoundPhaseSetup
{
    /// <summary>
    /// Initializes a round phase.
    /// </summary>
    /// <param name="kind">The phase kind.</param>
    /// <param name="duration">The phase length.</param>
    /// <param name="endPhaseEarlyIfAble">Whether the phase may close as soon as it can resolve.</param>
    public RoundPhaseSetup(RoundPhaseKind kind, ScheduleDuration duration, bool endPhaseEarlyIfAble = true)
    {
        ArgumentNullException.ThrowIfNull(duration);
        Kind = kind;
        Duration = duration;
        EndPhaseEarlyIfAble = endPhaseEarlyIfAble;
    }

    /// <summary>Gets the phase kind.</summary>
    public RoundPhaseKind Kind { get; }

    /// <summary>Gets the phase length.</summary>
    public ScheduleDuration Duration { get; }

    /// <summary>Gets whether the phase may close as soon as it can resolve. Default is on.</summary>
    public bool EndPhaseEarlyIfAble { get; }
}
