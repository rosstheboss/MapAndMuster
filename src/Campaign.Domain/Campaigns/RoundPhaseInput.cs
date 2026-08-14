namespace Campaign.Domain.Campaigns;

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
}
