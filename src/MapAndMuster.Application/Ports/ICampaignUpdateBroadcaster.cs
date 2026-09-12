namespace MapAndMuster.Application.Ports;

/// <summary>
/// What kind of change a subscriber should react to.
/// </summary>
public enum CampaignUpdateKind
{
    /// <summary>Play state advanced, orders resolved, or the log and chat gained an entry.</summary>
    Play = 0,

    /// <summary>Campaign setup, membership, map graph, or catalog changed.</summary>
    Setup = 1,

    /// <summary>The public site-chat board changed. Not scoped to a campaign.</summary>
    SiteChat = 2,
}

/// <summary>
/// A single change notification. Carries no campaign payload.
/// </summary>
/// <param name="Kind">The kind of change.</param>
/// <param name="Revision">
/// The campaign revision after the change, or zero for updates that are not campaign-scoped.
/// Subscribers refetch only when this exceeds the revision they have already applied.
/// </param>
public readonly record struct CampaignUpdate(CampaignUpdateKind Kind, int Revision);

/// <summary>
/// An active subscription to one topic's updates.
/// </summary>
public interface ICampaignUpdateSubscription : IDisposable
{
    /// <summary>
    /// Waits for the next update, or for the timeout to elapse.
    /// </summary>
    /// <param name="timeout">How long to wait before reporting no update.</param>
    /// <param name="cancellationToken">Cancelled when the client disconnects.</param>
    /// <returns>The next update, or <see langword="null"/> when the timeout elapsed first.</returns>
    /// <remarks>
    /// Returning <see langword="null"/> lets the caller emit a heartbeat without tearing the
    /// subscription down, which matters for proxies that close idle connections.
    /// </remarks>
    ValueTask<CampaignUpdate?> ReadAsync(TimeSpan timeout, CancellationToken cancellationToken);
}

/// <summary>
/// Fans campaign change notifications out to connected clients.
/// </summary>
/// <remarks>
/// Events deliberately carry only a kind and a revision. Hidden orders, relics, and private
/// objectives stay inside the authorized read endpoints, so there is no second code path that
/// could leak them. See <c>docs/adr/0004-server-sent-events-for-campaign-updates.md</c>.
/// <para>
/// Publishing is fire-and-forget and must never fail a campaign mutation. Delivery is best
/// effort: a dropped notification only delays a client refresh until its safety-net poll.
/// </para>
/// </remarks>
public interface ICampaignUpdateBroadcaster
{
    /// <summary>
    /// Notifies subscribers of one campaign that it changed.
    /// </summary>
    /// <param name="campaignId">The campaign that changed.</param>
    /// <param name="kind">The kind of change.</param>
    /// <param name="revision">The campaign revision after the change.</param>
    void Publish(Guid campaignId, CampaignUpdateKind kind, int revision);

    /// <summary>
    /// Notifies subscribers of the public site-chat board that it changed.
    /// </summary>
    void PublishSiteChat();

    /// <summary>
    /// Subscribes to one campaign's updates.
    /// </summary>
    /// <param name="campaignId">The campaign to watch.</param>
    /// <returns>The subscription. Dispose it to stop receiving updates.</returns>
    ICampaignUpdateSubscription Subscribe(Guid campaignId);

    /// <summary>
    /// Subscribes to public site-chat updates.
    /// </summary>
    /// <returns>The subscription. Dispose it to stop receiving updates.</returns>
    ICampaignUpdateSubscription SubscribeSiteChat();
}
