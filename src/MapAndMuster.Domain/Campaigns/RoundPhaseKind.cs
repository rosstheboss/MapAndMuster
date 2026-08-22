namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// A timed step inside a campaign round.
/// </summary>
public enum RoundPhaseKind
{
    /// <summary>A simultaneous-order action window.</summary>
    Action = 0,

    /// <summary>A battle-resolution window.</summary>
    Battle = 1,
}
