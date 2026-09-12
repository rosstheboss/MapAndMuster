using MapAndMuster.Application.Campaigns;
using MapAndMuster.Application.Notifications;
using MapAndMuster.Application.Ports;
using MapAndMuster.Domain.Play;

namespace MapAndMuster.Application.Play;

/// <summary>
/// Runs time-based campaign transitions that have fallen due, and reports when the next one is.
/// </summary>
/// <remarks>
/// Clients no longer poll, so nothing else would open a pending phase window or close an expired
/// one for a campaign whose players are all offline. This handler is the deadline-driven
/// equivalent of what <c>GET /play</c> used to do incidentally, and it runs the same idempotent
/// advance: it reloads current database state, applies the domain rules, and persists only when
/// something actually changed. Running it twice is harmless.
/// </remarks>
public sealed class AdvanceDueCampaignsHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;
    private readonly IUserAccountStore _accounts;
    private readonly CampaignNotificationPublisher? _notifications;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    /// <param name="campaigns">The campaign store.</param>
    /// <param name="clock">The authoritative clock.</param>
    /// <param name="accounts">The account store, used by completion logging.</param>
    /// <param name="notifications">Notification publisher, when configured.</param>
    public AdvanceDueCampaignsHandler(
        ICampaignStore campaigns,
        IClock clock,
        IUserAccountStore accounts,
        CampaignNotificationPublisher? notifications = null)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(accounts);
        _campaigns = campaigns;
        _clock = clock;
        _accounts = accounts;
        _notifications = notifications;
    }

    /// <summary>
    /// Advances every campaign whose next transition instant has passed.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The earliest future transition instant across all open campaigns, or
    /// <see langword="null"/> when no campaign has a pending deadline.
    /// </returns>
    public async Task<DateTimeOffset?> RunAsync(CancellationToken cancellationToken)
    {
        var candidates = await _campaigns.ListTransitionCandidatesAsync(cancellationToken).ConfigureAwait(false);
        var utcNow = _clock.UtcNow;
        var due = new List<Guid>();
        foreach (var candidate in candidates)
        {
            if (NextTransitionUtc(candidate) is { } dueUtc && dueUtc <= utcNow)
            {
                due.Add(candidate.Id);
            }
        }

        foreach (var campaignId in due)
        {
            await AdvanceAsync(campaignId, cancellationToken).ConfigureAwait(false);
        }

        if (due.Count > 0)
        {
            // State changed, so recompute deadlines from the persisted result rather than from
            // the snapshot taken before the advance.
            candidates = await _campaigns.ListTransitionCandidatesAsync(cancellationToken).ConfigureAwait(false);
            utcNow = _clock.UtcNow;
        }

        DateTimeOffset? next = null;
        foreach (var candidate in candidates)
        {
            if (NextTransitionUtc(candidate) is not { } dueUtc)
            {
                continue;
            }

            // A deadline that is still in the past after an advance attempt means the campaign
            // could not progress (for example it is waiting on battle results). Do not let it
            // pin the delay to zero and spin.
            if (dueUtc <= utcNow)
            {
                continue;
            }

            if (next is null || dueUtc < next)
            {
                next = dueUtc;
            }
        }

        return next;
    }

    /// <summary>
    /// The next instant at which a campaign could need a time-based transition.
    /// </summary>
    /// <param name="candidate">The open campaign.</param>
    /// <returns>The instant, or <see langword="null"/> when no transition remains.</returns>
    internal static DateTimeOffset? NextTransitionUtc(CampaignTransitionCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        var play = candidate.PlayState;
        if (play is null || play.Windows.Count == 0)
        {
            // Not launched yet. The start instant is when play should begin.
            return candidate.StartsUtc;
        }

        DateTimeOffset? next = null;
        foreach (var window in play.Windows)
        {
            DateTimeOffset? instant = window.Status switch
            {
                PhaseWindowStatus.Pending => window.StartsUtc,
                PhaseWindowStatus.Open => window.EndsUtc,
                _ => null,
            };

            if (instant is { } value && (next is null || value < next))
            {
                next = value;
            }
        }

        return next;
    }

    private async Task AdvanceAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        // The system actor is not a member, so authorize as an administrator to reach the same
        // advance path a viewer would trigger. No state is returned to a caller here.
        var loaded = await CampaignPlayPipeline
            .LoadAsync(_campaigns, _clock, campaignId, Guid.Empty, isAdministrator: true, cancellationToken, _accounts)
            .ConfigureAwait(false);
        if (!loaded.IsSuccess || !loaded.Changed || loaded.Campaign is null)
        {
            return;
        }

        var persisted = await CampaignPlayPipeline
            .PersistIfChangedAsync(_campaigns, loaded, cancellationToken)
            .ConfigureAwait(false);
        if (!persisted.IsSuccess || persisted.Campaign is null)
        {
            return;
        }

        if (_notifications is not null && loaded.Previous is not null)
        {
            await _notifications
                .PublishPlayAdvanceAsync(loaded.Previous, persisted.Campaign, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
