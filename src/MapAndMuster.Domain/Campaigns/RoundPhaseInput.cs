namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// Unvalidated action or battle step in a round.
/// </summary>
public sealed record RoundPhaseInput
{
    /// <summary>Gets the phase kind name.</summary>
    public string? Kind { get; init; }

    /// <summary>Gets the duration amount.</summary>
    public int DurationAmount { get; init; }

    /// <summary>Gets the duration unit name.</summary>
    public string? DurationUnit { get; init; }

    /// <summary>Gets whether the phase may close as soon as it can resolve. Omitted means on.</summary>
    public bool? EndPhaseEarlyIfAble { get; init; }
}
