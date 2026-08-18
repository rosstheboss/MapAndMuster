using Campaign.Domain.Identity;

namespace Campaign.Domain.Campaigns;

/// <summary>
/// Validated round schedule: start, time zone, round length, round count, and ordered phases.
/// </summary>
public sealed class CampaignSchedule
{
    /// <summary>
    /// Initializes a validated schedule.
    /// </summary>
    /// <param name="timeZone">The campaign time zone used during setup.</param>
    /// <param name="startsUtc">The campaign start instant, in UTC.</param>
    /// <param name="endsUtc">The campaign end instant, in UTC.</param>
    /// <param name="roundCount">The number of rounds.</param>
    /// <param name="roundLength">The length of each round.</param>
    /// <param name="phases">The ordered action and battle steps in one round.</param>
    /// <param name="armyEscalations">Per-round army size, free supply, and free characters.</param>
    public CampaignSchedule(
        IanaTimeZone timeZone,
        DateTimeOffset startsUtc,
        DateTimeOffset endsUtc,
        int roundCount,
        ScheduleDuration roundLength,
        IReadOnlyList<RoundPhaseSetup> phases,
        IReadOnlyList<RoundArmyEscalationSetup>? armyEscalations = null)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        ArgumentNullException.ThrowIfNull(roundLength);
        ArgumentNullException.ThrowIfNull(phases);
        TimeZone = timeZone;
        StartsUtc = startsUtc;
        EndsUtc = endsUtc;
        RoundCount = roundCount;
        RoundLength = roundLength;
        Phases = phases;
        ArmyEscalations = armyEscalations ?? HuntInEstaliaDefaults.ArmyEscalations(roundCount);
    }

    /// <summary>Gets the campaign time zone used during setup.</summary>
    public IanaTimeZone TimeZone { get; }

    /// <summary>Gets the campaign start instant, in UTC.</summary>
    public DateTimeOffset StartsUtc { get; }

    /// <summary>Gets the campaign end instant, in UTC.</summary>
    public DateTimeOffset EndsUtc { get; }

    /// <summary>Gets the start as a local wall-clock value in the campaign time zone.</summary>
    public string StartsAtLocal => CampaignCalendar.FormatLocal(StartsUtc, TimeZone);

    /// <summary>Gets the number of rounds.</summary>
    public int RoundCount { get; }

    /// <summary>Gets the length of each round.</summary>
    public ScheduleDuration RoundLength { get; }

    /// <summary>Gets the ordered action and battle steps in one round.</summary>
    public IReadOnlyList<RoundPhaseSetup> Phases { get; }

    /// <summary>Gets per-round army size, free supply, and free-character allowances.</summary>
    public IReadOnlyList<RoundArmyEscalationSetup> ArmyEscalations { get; }

    /// <summary>
    /// Evaluates campaign status from the server clock. Phase boundaries belong to the following phase.
    /// </summary>
    /// <param name="utcNow">The current UTC instant.</param>
    /// <returns>The lifecycle snapshot.</returns>
    public CampaignProgress Evaluate(DateTimeOffset utcNow)
    {
        if (utcNow < StartsUtc)
        {
            return new CampaignProgress(CampaignStatus.Scheduled, null, null, null, null, null);
        }

        if (utcNow >= EndsUtc)
        {
            return new CampaignProgress(CampaignStatus.Completed, null, null, null, null, null);
        }

        var cursor = StartsUtc;
        for (var round = 1; round <= RoundCount; round++)
        {
            for (var index = 0; index < Phases.Count; index++)
            {
                var phase = Phases[index];
                var phaseEnd = CampaignCalendar.Add(cursor, TimeZone, phase.Duration);
                if (utcNow < phaseEnd)
                {
                    return new CampaignProgress(
                        CampaignStatus.InProgress,
                        round,
                        index + 1,
                        phase.Kind,
                        cursor,
                        phaseEnd);
                }

                cursor = phaseEnd;
            }
        }

        return new CampaignProgress(CampaignStatus.Completed, null, null, null, null, null);
    }
}
