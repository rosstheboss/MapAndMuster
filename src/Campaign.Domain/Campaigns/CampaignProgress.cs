namespace Campaign.Domain.Campaigns;

/// <summary>
/// The campaign lifecycle at one instant of the server clock.
/// </summary>
public sealed class CampaignProgress
{
    /// <summary>
    /// Initializes progress for a schedule evaluation.
    /// </summary>
    /// <param name="status">The campaign-level status.</param>
    /// <param name="currentRound">The 1-based round when in progress.</param>
    /// <param name="currentPhaseNumber">The 1-based phase index in the round when in progress.</param>
    /// <param name="currentPhaseKind">The current phase kind when in progress.</param>
    /// <param name="currentPhaseStartsUtc">When the current phase opened, in UTC.</param>
    /// <param name="currentPhaseEndsUtc">When the current phase closes, in UTC.</param>
    public CampaignProgress(
        CampaignStatus status,
        int? currentRound,
        int? currentPhaseNumber,
        RoundPhaseKind? currentPhaseKind,
        DateTimeOffset? currentPhaseStartsUtc,
        DateTimeOffset? currentPhaseEndsUtc)
    {
        Status = status;
        CurrentRound = currentRound;
        CurrentPhaseNumber = currentPhaseNumber;
        CurrentPhaseKind = currentPhaseKind;
        CurrentPhaseStartsUtc = currentPhaseStartsUtc;
        CurrentPhaseEndsUtc = currentPhaseEndsUtc;
    }

    /// <summary>Gets the campaign-level status.</summary>
    public CampaignStatus Status { get; }

    /// <summary>Gets the 1-based round when the campaign is in progress.</summary>
    public int? CurrentRound { get; }

    /// <summary>Gets the 1-based phase index in the current round.</summary>
    public int? CurrentPhaseNumber { get; }

    /// <summary>Gets the current phase kind when the campaign is in progress.</summary>
    public RoundPhaseKind? CurrentPhaseKind { get; }

    /// <summary>Gets when the current phase opened, in UTC.</summary>
    public DateTimeOffset? CurrentPhaseStartsUtc { get; }

    /// <summary>Gets when the current phase closes, in UTC.</summary>
    public DateTimeOffset? CurrentPhaseEndsUtc { get; }
}
