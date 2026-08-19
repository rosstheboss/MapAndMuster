namespace Campaign.Infrastructure.Persistence.Entities;

/// <summary>
/// Persisted action or battle step in a campaign round.
/// </summary>
public sealed class CampaignRoundPhaseRecord
{
    /// <summary>Gets or sets the phase identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the campaign identifier.</summary>
    public Guid CampaignId { get; set; }

    /// <summary>Gets or sets the phase kind name.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the duration amount.</summary>
    public int DurationAmount { get; set; }

    /// <summary>Gets or sets the duration unit name.</summary>
    public string DurationUnit { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the phase may close as soon as it can resolve.</summary>
    public bool EndPhaseEarlyIfAble { get; set; } = true;

    /// <summary>Gets or sets the display order.</summary>
    public int SortOrder { get; set; }

    /// <summary>Gets or sets the campaign.</summary>
    public CampaignRecord? Campaign { get; set; }
}
