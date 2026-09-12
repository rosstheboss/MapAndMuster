using System.Collections.Concurrent;
using System.Threading.Channels;
using MapAndMuster.Application.Ports;

namespace MapAndMuster.Infrastructure.Notifications;

/// <summary>
/// In-memory fan-out of campaign change notifications to connected Server-Sent Events clients.
/// </summary>
/// <remarks>
/// Valid only while the API runs as a single instance (ADR 0003). With more than one instance a
/// subscriber on one instance would not observe a mutation served by another; that requires
/// PostgreSQL <c>LISTEN</c>/<c>NOTIFY</c> or a broker. See
/// <c>docs/adr/0004-server-sent-events-for-campaign-updates.md</c>.
/// </remarks>
public sealed class InProcessCampaignUpdateBroadcaster : ICampaignUpdateBroadcaster
{
    /// <summary>
    /// Topic key for the campaign-independent site-chat board. No campaign uses the empty id.
    /// </summary>
    private static readonly Guid SiteChatTopic = Guid.Empty;

    /// <summary>
    /// Per-subscriber buffer depth. Small on purpose: an update carries only a revision, so a
    /// slow client that misses intermediate values still converges once it reads the newest one.
    /// </summary>
    private const int SubscriberCapacity = 8;

    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Subscription, byte>> _topics = new();

    /// <inheritdoc />
    public void Publish(Guid campaignId, CampaignUpdateKind kind, int revision)
    {
        Dispatch(campaignId, new CampaignUpdate(kind, revision));
    }

    /// <inheritdoc />
    public void PublishSiteChat()
    {
        Dispatch(SiteChatTopic, new CampaignUpdate(CampaignUpdateKind.SiteChat, 0));
    }

    /// <inheritdoc />
    public ICampaignUpdateSubscription Subscribe(Guid campaignId)
    {
        return SubscribeTo(campaignId);
    }

    /// <inheritdoc />
    public ICampaignUpdateSubscription SubscribeSiteChat()
    {
        return SubscribeTo(SiteChatTopic);
    }

    /// <summary>
    /// Gets the number of live subscriptions. Used by tests to prove disposal unregisters.
    /// </summary>
    internal int SubscriberCount => _topics.Values.Sum(static subscribers => subscribers.Count);

    private Subscription SubscribeTo(Guid topic)
    {
        var subscribers = _topics.GetOrAdd(topic, static _ => new ConcurrentDictionary<Subscription, byte>());
        var subscription = new Subscription(this, topic);
        subscribers.TryAdd(subscription, 0);
        return subscription;
    }

    private void Dispatch(Guid topic, CampaignUpdate update)
    {
        if (!_topics.TryGetValue(topic, out var subscribers))
        {
            return;
        }

        foreach (var subscriber in subscribers.Keys)
        {
            // DropOldest, so a stalled reader cannot grow memory or block the publisher.
            subscriber.Writer.TryWrite(update);
        }
    }

    private void Unsubscribe(Guid topic, Subscription subscription)
    {
        if (!_topics.TryGetValue(topic, out var subscribers))
        {
            return;
        }

        subscribers.TryRemove(subscription, out _);

        // Drop the topic once its last subscriber leaves so idle campaigns cost nothing.
        if (subscribers.IsEmpty)
        {
            _topics.TryRemove(new KeyValuePair<Guid, ConcurrentDictionary<Subscription, byte>>(topic, subscribers));
        }
    }

    private sealed class Subscription : ICampaignUpdateSubscription
    {
        private readonly Channel<CampaignUpdate> _channel = Channel.CreateBounded<CampaignUpdate>(
            new BoundedChannelOptions(SubscriberCapacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false,
            });

        private readonly InProcessCampaignUpdateBroadcaster _owner;
        private readonly Guid _topic;
        private bool _disposed;

        public Subscription(InProcessCampaignUpdateBroadcaster owner, Guid topic)
        {
            _owner = owner;
            _topic = topic;
        }

        public ChannelWriter<CampaignUpdate> Writer => _channel.Writer;

        public async ValueTask<CampaignUpdate?> ReadAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (_channel.Reader.TryRead(out var buffered))
            {
                return buffered;
            }

            using var timer = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timer.CancelAfter(timeout);
            try
            {
                return await _channel.Reader.ReadAsync(timer.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // The timeout elapsed. No update was consumed; the caller sends a heartbeat.
                return null;
            }
            catch (ChannelClosedException)
            {
                return null;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _owner.Unsubscribe(_topic, this);
            _channel.Writer.TryComplete();
        }
    }
}
