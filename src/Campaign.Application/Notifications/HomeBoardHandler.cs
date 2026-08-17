using Campaign.Application.Campaigns;
using Campaign.Application.Common;
using Campaign.Application.Ports;
using Campaign.Domain.Campaigns;
using Campaign.Domain.Notifications;
using Campaign.Domain.Play;

namespace Campaign.Application.Notifications;

/// <summary>
/// Builds the home-page attention board from unread notices and live play obligations.
/// </summary>
public sealed class GetHomeBoardHandler
{
    private readonly IUserNotificationStore _notifications;
    private readonly ICampaignStore _campaigns;
    private readonly IUserAccountStore _accounts;
    private readonly IClock _clock;

    /// <summary>
    /// Initializes a handler.
    /// </summary>
    public GetHomeBoardHandler(
        IUserNotificationStore notifications,
        ICampaignStore campaigns,
        IUserAccountStore accounts,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(notifications);
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(accounts);
        ArgumentNullException.ThrowIfNull(clock);
        _notifications = notifications;
        _campaigns = campaigns;
        _accounts = accounts;
        _clock = clock;
    }

    /// <summary>
    /// Returns items that need the viewer's attention.
    /// </summary>
    public async Task<OperationResult<IReadOnlyList<HomeAttentionItem>>> HandleAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var account = await _accounts.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (account is null)
        {
            return OperationResults.Failure<IReadOnlyList<HomeAttentionItem>>(
                ErrorCodes.ProfileNotFound,
                "The profile was not found.");
        }

        var items = new List<HomeAttentionItem>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        if (account.InAppNotificationsEnabled)
        {
            var unread = await _notifications.ListUnreadAsync(userId, cancellationToken).ConfigureAwait(false);
            foreach (var notice in unread)
            {
                if (!seen.Add(notice.DedupeKey))
                {
                    continue;
                }

                items.Add(new HomeAttentionItem
                {
                    Id = notice.Id.ToString("N"),
                    Kind = notice.Kind,
                    CampaignId = notice.CampaignId,
                    CampaignName = notice.CampaignName,
                    Title = notice.Title,
                    Body = notice.Body,
                    Path = notice.Path,
                    CreatedUtc = notice.CreatedUtc,
                });
            }
        }

        var campaigns = await _campaigns.ListForUserAsync(userId, cancellationToken).ConfigureAwait(false);
        foreach (var campaign in campaigns)
        {
            foreach (var item in LiveAttention(campaign, userId, _clock.UtcNow))
            {
                if (seen.Add(item.Id))
                {
                    items.Add(item);
                }
            }
        }

        return OperationResults.Success<IReadOnlyList<HomeAttentionItem>>(
            [.. items.OrderByDescending(static item => item.CreatedUtc)]);
    }

    internal static IEnumerable<HomeAttentionItem> LiveAttention(
        StoredCampaign campaign,
        Guid userId,
        DateTimeOffset utcNow)
    {
        var membership = CampaignMapper.MembershipFor(campaign, userId);
        if (membership is null)
        {
            yield break;
        }

        var progress = CampaignLifecycle.Progress(campaign, utcNow);
        var path = $"/campaigns/{campaign.Id}";
        if (membership.IsPlayer
            && membership.FactionId is null
            && progress.Status != CampaignStatus.Completed)
        {
            yield return Item(
                $"faction:{campaign.Id:N}:{userId:N}",
                NotificationKind.ActionRequired,
                campaign,
                "Choose your faction",
                $"Choose a faction in {campaign.Name} before you can play.",
                path,
                utcNow);
        }

        var play = campaign.PlayState;
        if (play is null || progress.Status != CampaignStatus.InProgress)
        {
            yield break;
        }

        var window = play.CurrentWindow();
        if (window is { Kind: RoundPhaseKind.Action, Status: PhaseWindowStatus.Open }
            && membership.IsPlayer
            && play.RequiredOrderPlayers(window.Id).Contains(userId)
            && !play.Commitments.Any(item => item.WindowId == window.Id && item.UserId == userId))
        {
            var label = progress.CurrentPhaseKind is null || progress.CurrentPhaseNumber is null
                ? "the current action window"
                : CampaignPhaseLabels.Format(
                    CampaignMapper.ToSchedule(campaign).Phases,
                    progress.CurrentPhaseNumber.Value,
                    progress.CurrentPhaseKind.Value);
            yield return Item(
                $"orders:{campaign.Id:N}:{window.Id:N}:{userId:N}",
                NotificationKind.ActionRequired,
                campaign,
                "Orders needed",
                $"Submit and commit orders for {label} in {campaign.Name}.",
                path,
                window.StartsUtc);
        }

        if (window is not { Kind: RoundPhaseKind.Battle, Status: PhaseWindowStatus.Open })
        {
            yield break;
        }

        foreach (var battle in play.Battles.Where(item => item.BattleWindowId == window.Id))
        {
            var myForce = play.Forces.FirstOrDefault(force =>
                force.ControllerUserId == userId && battle.ParticipantForceIds.Contains(force.Id));
            if (myForce is null)
            {
                continue;
            }

            if (battle.Status is BattleStatus.AwaitingResults or BattleStatus.Disputed
                && play.LatestBattleSubmission(battle.Id, userId) is null)
            {
                yield return Item(
                    $"battle:{campaign.Id:N}:{battle.Id:N}:{userId:N}",
                    NotificationKind.ActionRequired,
                    campaign,
                    "Battle result needed",
                    $"Submit a battle result in {campaign.Name}.",
                    path,
                    battle.CreatedUtc);
            }

            var needsRetreat = battle.Status is BattleStatus.Finalized or BattleStatus.GMResolved
                && !battle.IsDraw
                && battle.WinnerForceId != myForce.Id
                && !play.Retreats.Any(item => item.BattleId == battle.Id && item.ForceId == myForce.Id);
            if (needsRetreat)
            {
                yield return Item(
                    $"retreat:{campaign.Id:N}:{battle.Id:N}:{userId:N}",
                    NotificationKind.ActionRequired,
                    campaign,
                    "Retreat needed",
                    $"Record a retreat in {campaign.Name}.",
                    path,
                    battle.CreatedUtc);
            }
        }
    }

    private static HomeAttentionItem Item(
        string id,
        NotificationKind kind,
        StoredCampaign campaign,
        string title,
        string body,
        string path,
        DateTimeOffset createdUtc)
    {
        return new HomeAttentionItem
        {
            Id = id,
            Kind = kind.ToString(),
            CampaignId = campaign.Id,
            CampaignName = campaign.Name,
            Title = title,
            Body = body,
            Path = path,
            CreatedUtc = createdUtc,
        };
    }
}

/// <summary>
/// Marks a stored notice as read.
/// </summary>
public sealed class MarkNotificationReadHandler
{
    private readonly IUserNotificationStore _notifications;
    private readonly IClock _clock;

    /// <summary>
    /// Initializes a handler.
    /// </summary>
    public MarkNotificationReadHandler(IUserNotificationStore notifications, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(notifications);
        ArgumentNullException.ThrowIfNull(clock);
        _notifications = notifications;
        _clock = clock;
    }

    /// <summary>
    /// Marks the notice read when it belongs to the caller.
    /// </summary>
    public async Task<OperationResult> HandleAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken)
    {
        var marked = await _notifications.MarkReadAsync(notificationId, userId, _clock.UtcNow, cancellationToken)
            .ConfigureAwait(false);
        return marked
            ? OperationResult.Success()
            : OperationResult.Failure("notification.not_found", "The notification was not found.");
    }
}
