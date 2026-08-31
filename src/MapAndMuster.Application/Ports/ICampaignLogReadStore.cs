namespace MapAndMuster.Application.Ports;

/// <summary>
/// Persistence for per-viewer campaign-log last-read marks.
/// Last-read is not stored on campaign memberships and does not bump campaign revision.
/// </summary>
public interface ICampaignLogReadStore
{
    /// <summary>
    /// Returns when the viewer last marked the campaign log read, if ever.
    /// </summary>
    Task<DateTimeOffset?> GetLastReadUtcAsync(Guid campaignId, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Upserts the viewer's last-read instant using the server clock.
    /// </summary>
    Task MarkReadAsync(Guid campaignId, Guid userId, DateTimeOffset utcNow, CancellationToken cancellationToken);
}
