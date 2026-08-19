using Campaign.Application.Campaigns;
using Campaign.Application.Ports;
using Campaign.Domain.Campaigns;
using Campaign.Domain.Notifications;
using Campaign.Domain.Play;

namespace Campaign.Application.Notifications;

/// <summary>
/// Creates in-app notices and queues email copies according to each user's preferences.
/// Email never includes private chat text or hidden orders.
/// </summary>
public sealed class CampaignNotificationPublisher
{
    private readonly IUserNotificationStore _notifications;
    private readonly IUserAccountStore _accounts;
    private readonly IEmailOutbox _outbox;
    private readonly IClock _clock;

    /// <summary>
    /// Initializes a publisher.
    /// </summary>
    public CampaignNotificationPublisher(
        IUserNotificationStore notifications,
        IUserAccountStore accounts,
        IEmailOutbox outbox,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(notifications);
        ArgumentNullException.ThrowIfNull(accounts);
        ArgumentNullException.ThrowIfNull(outbox);
        ArgumentNullException.ThrowIfNull(clock);
        _notifications = notifications;
        _accounts = accounts;
        _outbox = outbox;
        _clock = clock;
    }

    /// <summary>
    /// Notifies recipients of a new chat message.
    /// </summary>
    public async Task PublishChatAsync(
        StoredCampaign campaign,
        PlayLogEntry entry,
        Guid senderUserId,
        IReadOnlyList<CampaignChatMember> members,
        IReadOnlyList<CampaignChatMembership> memberships,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(members);
        ArgumentNullException.ThrowIfNull(memberships);

        var audience = CampaignChatRules.AudienceUserIds(entry, memberships);
        var mentioned = CampaignChatRules.ResolveMentions(entry.Message ?? string.Empty, members)
            .Select(static member => member.UserId)
            .ToHashSet();
        var path = $"/campaigns/{campaign.Id}";
        foreach (var userId in audience)
        {
            if (userId == senderUserId)
            {
                continue;
            }

            if (mentioned.Contains(userId))
            {
                await NotifyAsync(
                        userId,
                        NotificationKind.Mention,
                        campaign,
                        "You were mentioned",
                        $"You were mentioned in {campaign.Name}. Sign in to view the campaign log.",
                        path,
                        $"mention:{entry.Id:N}:{userId:N}",
                        cancellationToken)
                    .ConfigureAwait(false);
                continue;
            }

            if (entry.IsPrivateChat)
            {
                await NotifyAsync(
                        userId,
                        NotificationKind.PrivateChat,
                        campaign,
                        "New private message",
                        $"You have a new private message in {campaign.Name}. Sign in to view it.",
                        path,
                        $"private:{entry.Id:N}:{userId:N}",
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Notifies members when a campaign launches, a phase changes, or the campaign ends.
    /// </summary>
    public async Task PublishPlayAdvanceAsync(
        StoredCampaign previous,
        StoredCampaign next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(next);
        var utcNow = _clock.UtcNow;
        var previousProgress = CampaignLifecycle.Progress(previous, utcNow);
        var nextProgress = CampaignLifecycle.Progress(next, utcNow);
        var previousIds = new HashSet<Guid>((previous.PlayState ?? CampaignPlayState.Empty).Log.Select(static item => item.Id));
        var newEntries = (next.PlayState ?? CampaignPlayState.Empty).Log
            .Where(item => !previousIds.Contains(item.Id))
            .ToArray();
        var path = $"/campaigns/{next.Id}";

        if (newEntries.Any(static item => item.Kind == PlayLogKind.CampaignStarted))
        {
            await NotifyMembersAsync(
                    next,
                    NotificationKind.CampaignStarted,
                    "Campaign started",
                    $"{next.Name} has started. Open the campaign to see the board and log.",
                    path,
                    $"start:{next.Id:N}",
                    cancellationToken)
                .ConfigureAwait(false);
        }

        var previousWindow = (previous.PlayState ?? CampaignPlayState.Empty).CurrentWindow()?.Id;
        var nextWindow = (next.PlayState ?? CampaignPlayState.Empty).CurrentWindow()?.Id;
        if (nextWindow is { } windowId
            && nextWindow != previousWindow
            && nextProgress.Status == CampaignStatus.InProgress)
        {
            var label = nextProgress.CurrentPhaseKind is null || nextProgress.CurrentPhaseNumber is null
                ? "a new phase"
                : CampaignPhaseLabels.Format(
                    CampaignMapper.ToSchedule(next).Phases,
                    nextProgress.CurrentPhaseNumber.Value,
                    nextProgress.CurrentPhaseKind.Value);
            await NotifyMembersAsync(
                    next,
                    NotificationKind.PhaseChanged,
                    "New phase",
                    $"{next.Name} is now in {label}. Open the campaign to see the last phase resolutions and the current board.",
                    path,
                    $"phase:{next.Id:N}:{windowId:N}",
                    cancellationToken)
                .ConfigureAwait(false);
        }

        if (previousProgress.Status != CampaignStatus.Completed
            && nextProgress.Status == CampaignStatus.Completed)
        {
            await NotifyMembersAsync(
                    next,
                    NotificationKind.CampaignEnded,
                    "Campaign ended",
                    $"{next.Name} has ended. Open the campaign to review the final board and log.",
                    path,
                    $"end:{next.Id:N}",
                    cancellationToken)
                .ConfigureAwait(false);
        }

        foreach (var entry in newEntries.Where(static item => item.Kind == PlayLogKind.DelinquencyThreshold))
        {
            await NotifyManagersAsync(
                    next,
                    NotificationKind.DelinquencyKickRecommendation,
                    "Possible kick",
                    $"A force in {next.Name} has reached three missed-order offences and may need to be removed.",
                    path,
                    $"delinquency:{next.Id:N}:{entry.ForceId:N}:{entry.Id:N}",
                    cancellationToken)
                .ConfigureAwait(false);
        }

        if (newEntries.Any(static item => item.Kind == PlayLogKind.BattleDisputed))
        {
            await NotifyManagersAsync(
                    next,
                    NotificationKind.ActionRequired,
                    "Disputed battle",
                    $"A battle in {next.Name} is disputed. Open the campaign to confirm the result.",
                    path,
                    $"dispute:{next.Id:N}",
                    cancellationToken)
                .ConfigureAwait(false);
        }

        if (newEntries.Any(static item => item.Kind == PlayLogKind.DebugActionReresolved))
        {
            await NotifyMembersAsync(
                    next,
                    NotificationKind.ActionRequired,
                    "Orders need review",
                    $"A manager re-resolved the previous action in {next.Name}. Check whether your current orders are still valid.",
                    path,
                    $"reresolve:{next.Id:N}:{newEntries.First(static item => item.Kind == PlayLogKind.DebugActionReresolved).Id:N}",
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Notifies a player that a manager removed them from the campaign.
    /// </summary>
    public async Task PublishKickedAsync(
        StoredCampaign campaign,
        Guid userId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        var path = campaign.IsPubliclyViewable ? $"/campaigns/{campaign.Id}" : "/campaigns/all";
        await NotifyAsync(
                userId,
                NotificationKind.CampaignKicked,
                campaign,
                "Removed from campaign",
                $"You were removed from {campaign.Name}.",
                path,
                $"kicked:{campaign.Id:N}:{userId:N}",
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task NotifyManagersAsync(
        StoredCampaign campaign,
        NotificationKind kind,
        string title,
        string body,
        string path,
        string dedupeKey,
        CancellationToken cancellationToken)
    {
        foreach (var membership in campaign.Memberships.Where(static member => member.IsGameMaster))
        {
            await NotifyAsync(
                    membership.UserId,
                    kind,
                    campaign,
                    title,
                    body,
                    path,
                    $"{dedupeKey}:{membership.UserId:N}",
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task NotifyMembersAsync(
        StoredCampaign campaign,
        NotificationKind kind,
        string title,
        string body,
        string path,
        string dedupePrefix,
        CancellationToken cancellationToken)
    {
        foreach (var membership in campaign.Memberships)
        {
            await NotifyAsync(
                    membership.UserId,
                    kind,
                    campaign,
                    title,
                    body,
                    path,
                    $"{dedupePrefix}:{membership.UserId:N}",
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task NotifyAsync(
        Guid userId,
        NotificationKind kind,
        StoredCampaign campaign,
        string title,
        string body,
        string path,
        string dedupeKey,
        CancellationToken cancellationToken)
    {
        var account = await _accounts.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (account is null)
        {
            return;
        }

        var added = await _notifications.TryAddAsync(
                new NewUserNotification
                {
                    UserId = userId,
                    Kind = kind,
                    CampaignId = campaign.Id,
                    CampaignName = campaign.Name,
                    Title = title,
                    Body = body,
                    Path = path,
                    DedupeKey = dedupeKey,
                },
                _clock.UtcNow,
                cancellationToken)
            .ConfigureAwait(false);
        if (!added)
        {
            return;
        }

        if (account.EmailNotificationsEnabled && !account.IsTestAccount)
        {
            await _outbox.QueueUserNoticeAsync(
                    account.Email,
                    userId,
                    title,
                    $"{body} Sign in to open the campaign.",
                    path,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
