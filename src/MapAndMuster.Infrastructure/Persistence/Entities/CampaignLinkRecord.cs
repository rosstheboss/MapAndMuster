namespace MapAndMuster.Infrastructure.Persistence.Entities;

/// <summary>
/// Persisted labeled external link for a campaign.
/// </summary>
public sealed class CampaignLinkRecord
{
    /// <summary>Gets or sets the link identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the campaign identifier.</summary>
    public Guid CampaignId { get; set; }

    /// <summary>Gets or sets the display label.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the destination URL.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Gets or sets the display order.</summary>
    public int SortOrder { get; set; }

    /// <summary>Gets or sets the campaign.</summary>
    public CampaignRecord? Campaign { get; set; }
}
