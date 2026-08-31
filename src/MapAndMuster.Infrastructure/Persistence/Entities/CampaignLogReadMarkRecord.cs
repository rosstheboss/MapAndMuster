namespace MapAndMuster.Infrastructure.Persistence.Entities;

/// <summary>
/// Per-viewer last-read mark for a campaign log. Independent of campaign revision.
/// </summary>
public sealed class CampaignLogReadMarkRecord
{
    /// <summary>Gets or sets the campaign identifier.</summary>
    public Guid CampaignId { get; set; }

    /// <summary>Gets or sets the viewer's user identifier.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets when the viewer last marked the log read, in UTC.</summary>
    public DateTimeOffset LastReadUtc { get; set; }
}
