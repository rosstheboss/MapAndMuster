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
    public RoundPhaseSetup(RoundPhaseKind kind, ScheduleDuration duration)
    {
        ArgumentNullException.ThrowIfNull(duration);
        Kind = kind;
        Duration = duration;
    }

    /// <summary>Gets the phase kind.</summary>
    public RoundPhaseKind Kind { get; }

    /// <summary>Gets the phase length.</summary>
    public ScheduleDuration Duration { get; }
}
