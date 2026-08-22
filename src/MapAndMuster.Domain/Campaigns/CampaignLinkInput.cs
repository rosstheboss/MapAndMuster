namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// User-supplied labeled external link for a campaign.
/// </summary>
public sealed class CampaignLinkInput
{
    /// <summary>Gets the link label shown to players.</summary>
    public required string Label { get; init; }

    /// <summary>Gets the destination URL.</summary>
    public required string Url { get; init; }
}
