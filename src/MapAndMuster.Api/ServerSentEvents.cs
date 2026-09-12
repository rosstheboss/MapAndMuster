using System.Globalization;
using MapAndMuster.Application.Ports;
using Microsoft.AspNetCore.Http.Features;

namespace MapAndMuster.Api;

/// <summary>
/// Writes campaign change notifications to a client as a Server-Sent Events stream.
/// </summary>
/// <remarks>
/// Frames contain only a change kind and revision. The client refetches an authorized read
/// endpoint when the revision is newer than what it has applied, so hidden campaign state never
/// travels on this channel. See <c>docs/adr/0004-server-sent-events-for-campaign-updates.md</c>.
/// </remarks>
public static class ServerSentEvents
{
    /// <summary>
    /// Named event the client watches to detect a connection that looks open but is not delivering
    /// bytes. Must stay below the shortest idle timeout of any proxy in front of the API.
    /// </summary>
    public const string HeartbeatEvent = "heartbeat";

    /// <summary>
    /// Idle interval between heartbeat events. Must stay below the shortest idle timeout of
    /// any proxy in front of the API.
    /// </summary>
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Streams updates from a subscription until the client disconnects or the host shuts down.
    /// </summary>
    /// <param name="context">The HTTP context for the streaming request.</param>
    /// <param name="subscription">The update subscription. Disposed when streaming ends.</param>
    /// <param name="cancellationToken">Cancelled on disconnect or shutdown.</param>
    public static async Task WriteStreamAsync(
        HttpContext context,
        ICampaignUpdateSubscription subscription,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(subscription);

        using (subscription)
        {
            PrepareResponse(context);
            await context.Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);

            // A named event, not a comment: EventSource exposes this to JavaScript so a buffered
            // or half-dead proxy can be detected. Sending one immediately proves the body is
            // flowing before the first idle interval.
            await WriteHeartbeatAsync(context, cancellationToken).ConfigureAwait(false);

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var update = await subscription.ReadAsync(HeartbeatInterval, cancellationToken).ConfigureAwait(false);
                    if (update is { } change)
                    {
                        await WriteEventAsync(context, change, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    await WriteHeartbeatAsync(context, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // The client navigated away or the host is stopping. Not an error.
            }
        }
    }

    private static void PrepareResponse(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache, no-store";
        context.Response.Headers.Pragma = "no-cache";

        // Tells nginx-family proxies, including the reverse proxy in front of Render, to forward
        // each frame instead of buffering the response.
        context.Response.Headers["X-Accel-Buffering"] = "no";

        // Kestrel buffers responses by default; events must reach the client as they are written.
        context.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
    }

    private static async Task WriteHeartbeatAsync(HttpContext context, CancellationToken cancellationToken)
    {
        await context.Response.WriteAsync($"event: {HeartbeatEvent}\ndata: {{}}\n\n", cancellationToken)
            .ConfigureAwait(false);
        await context.Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteEventAsync(
        HttpContext context,
        CampaignUpdate update,
        CancellationToken cancellationToken)
    {
        var revision = update.Revision.ToString(CultureInfo.InvariantCulture);
        var frame = $"event: {ToEventName(update.Kind)}\ndata: {{\"revision\":{revision}}}\n\n";
        await context.Response.WriteAsync(frame, cancellationToken).ConfigureAwait(false);
        await context.Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string ToEventName(CampaignUpdateKind kind)
    {
        return kind switch
        {
            CampaignUpdateKind.Play => "play",
            CampaignUpdateKind.Setup => "setup",
            CampaignUpdateKind.SiteChat => "site-chat",
            _ => "play",
        };
    }
}
