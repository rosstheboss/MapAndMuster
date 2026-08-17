using Campaign.Application.Ports;
using Campaign.Domain.Chat;
using Campaign.Domain.Notifications;
using Campaign.Domain.Play;

namespace Campaign.Application.Chat;

/// <summary>
/// Creates in-app notices and queues email copies for public site chat.
/// Email never includes the chat body.
/// </summary>
public sealed class SiteChatNotificationPublisher
{
    private readonly IUserNotificationStore _notifications;
    private readonly IUserAccountStore _accounts;
    private readonly IEmailOutbox _outbox;
    private readonly IClock _clock;

    /// <summary>
    /// Initializes a publisher.
    /// </summary>
    public SiteChatNotificationPublisher(
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
    /// Notifies mentioned users, or administrator-announcement recipients, who can see the message.
    /// </summary>
    public async Task PublishAsync(
        SiteChatMessage message,
        IReadOnlyList<CampaignChatMember> members,
        IReadOnlyList<SiteChatBlock> blocks,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(members);
        ArgumentNullException.ThrowIfNull(blocks);

        if (message.Kind == SiteChatKind.Admin)
        {
            if (message.TargetUserId is { } targetId)
            {
                await NotifyAsync(
                        targetId,
                        message.AuthorUserId,
                        NotificationKind.SiteAdminMessage,
                        "Administrator message",
                        "An administrator sent you a site chat message. Sign in to view it.",
                        $"site-admin:{message.Id:N}:{targetId:N}",
                        cancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            var everyone = await _accounts.ListAllAsync(cancellationToken).ConfigureAwait(false);
            foreach (var account in everyone)
            {
                await NotifyAsync(
                        account.Id,
                        message.AuthorUserId,
                        NotificationKind.SiteAdminMessage,
                        "Administrator message",
                        "An administrator sent a site chat message. Sign in to view it.",
                        $"site-admin:{message.Id:N}:{account.Id:N}",
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            return;
        }

        var hiddenByAuthor = SiteChatRules.HiddenAuthorIds(message.AuthorUserId, blocks);
        var mentioned = CampaignChatRules.ResolveMentions(message.Body, members);
        foreach (var member in mentioned)
        {
            if (member.UserId == message.AuthorUserId || hiddenByAuthor.Contains(member.UserId))
            {
                continue;
            }

            await NotifyAsync(
                    member.UserId,
                    message.AuthorUserId,
                    NotificationKind.SiteChatMention,
                    "You were mentioned",
                    "You were mentioned in site chat. Sign in to view it.",
                    $"site-mention:{message.Id:N}:{member.UserId:N}",
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task NotifyAsync(
        Guid userId,
        Guid senderUserId,
        NotificationKind kind,
        string title,
        string body,
        string dedupeKey,
        CancellationToken cancellationToken)
    {
        if (userId == senderUserId)
        {
            return;
        }

        var account = await _accounts.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (account is null)
        {
            return;
        }

        var added = await _notifications.TryAddAsync(
                new Notifications.NewUserNotification
                {
                    UserId = userId,
                    Kind = kind,
                    CampaignId = null,
                    CampaignName = null,
                    Title = title,
                    Body = body,
                    Path = SiteChatRules.BoardPath,
                    DedupeKey = dedupeKey,
                },
                _clock.UtcNow,
                cancellationToken)
            .ConfigureAwait(false);
        if (!added)
        {
            return;
        }

        if (account.EmailNotificationsEnabled)
        {
            await _outbox.QueueUserNoticeAsync(
                    account.Email,
                    userId,
                    title,
                    $"{body} Sign in to open site chat.",
                    SiteChatRules.BoardPath,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
