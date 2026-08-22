namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// Campaign-level lifecycle derived from the configured schedule and the server clock.
/// </summary>
public enum CampaignStatus
{
    /// <summary>The configured start instant has not been reached.</summary>
    Scheduled = 0,

    /// <summary>The current instant falls inside a configured round and phase.</summary>
    InProgress = 1,

    /// <summary>The configured end instant has been reached or passed.</summary>
    Completed = 2,
}
