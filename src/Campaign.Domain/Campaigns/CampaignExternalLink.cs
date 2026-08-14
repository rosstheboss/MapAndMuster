namespace Campaign.Domain.Campaigns;

/// <summary>
/// A validated labeled HTTP(S) link attached to a campaign.
/// </summary>
public sealed class CampaignExternalLink
{
    /// <summary>
    /// Initializes a validated external link.
    /// </summary>
    /// <param name="label">The display label.</param>
    /// <param name="url">The absolute HTTP or HTTPS URL.</param>
    public CampaignExternalLink(string label, string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        Label = label;
        Url = url;
    }

    /// <summary>Gets the display label.</summary>
    public string Label { get; }

    /// <summary>Gets the absolute HTTP or HTTPS URL.</summary>
    public string Url { get; }
}
