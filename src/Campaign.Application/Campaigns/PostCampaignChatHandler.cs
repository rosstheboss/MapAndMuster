using Campaign.Application.Common;
using Campaign.Application.Notifications;
using Campaign.Application.Play;
using Campaign.Application.Ports;
using Campaign.Domain.Play;

namespace Campaign.Application.Campaigns;

/// <summary>
/// Posts a chat message to a campaign log. Private channels are stored on the entry and filtered on read.
/// </summary>
public sealed class PostCampaignChatHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;
    private readonly IUserAccountStore _accounts;
    private readonly CampaignNotificationPublisher? _notifications;

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    public PostCampaignChatHandler(
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
    /// Appends a chat message when the caller is a current campaign member.
    /// </summary>
    public async Task<OperationResult<CampaignDetail>> HandleAsync(
        PostCampaignChatCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var campaign = await _campaigns.FindByIdAsync(command.CampaignId, cancellationToken).ConfigureAwait(false);
        if (campaign is null || !CampaignAccess.CanView(campaign, command.UserId, command.IsAdministrator))
        {
            return OperationResults.Failure<CampaignDetail>(ErrorCodes.CampaignNotFound, "The campaign was not found.");
        }

        if (CampaignMapper.MembershipFor(campaign, command.UserId) is null)
        {
            return OperationResults.Failure<CampaignDetail>(
                ErrorCodes.CampaignForbidden,
                "Only campaign members can chat in this log.");
        }

        if (campaign.Revision != command.ExpectedRevision)
        {
            return OperationResults.Failure<CampaignDetail>(
                ErrorCodes.ConcurrencyConflict,
                "The campaign was changed by another request. Reload and try again.");
        }

        if (!CampaignChatContext.TryParseChannel(command.ChannelKind, command.TargetId, out var channel, out var channelError))
        {
            return OperationResults.Failure<CampaignDetail>("chat.channel.invalid", channelError ?? "Choose a chat channel.");
        }

        var participants = await CampaignPlayMapper.ParticipantsAsync(campaign, _accounts, cancellationToken)
            .ConfigureAwait(false);
        var mentionable = CampaignPlayMapper.ToChatMembers(participants);
        var members = mentionable
            .Select(static member => new CampaignChatMember(member.UserId, member.Username, member.DisplayName))
            .ToArray();
        if (!CampaignChatRules.TryPost(
                campaign.PlayState ?? CampaignPlayState.Empty,
                command.UserId,
                command.Message,
                members,
                _clock.UtcNow,
                out var next,
                out var error,
                channel,
                CampaignChatContext.Memberships(campaign),
                CampaignChatContext.Factions(campaign),
                CampaignChatContext.AllyGroups(campaign)))
        {
            return OperationResults.Failure<CampaignDetail>(
                error.Code,
                error.Message);
        }

        var outcome = await _campaigns.UpdatePlayStateAsync(
                campaign.Id,
                next,
                campaign.MapGraph,
                campaign.EndsUtc,
                campaign.RoundCount,
                command.ExpectedRevision,
                _clock.UtcNow,
                cancellationToken)
            .ConfigureAwait(false);
        if (!outcome.IsSuccess || outcome.Campaign is null)
        {
            return OperationResults.Failure<CampaignDetail>(
                outcome.ErrorCode ?? ErrorCodes.ConcurrencyConflict,
                outcome.Message ?? "The campaign could not be updated.");
        }

        var log = (outcome.Campaign.PlayState ?? CampaignPlayState.Empty).Log;
        var posted = log.Count == 0 ? null : log[^1];
        if (posted is { Kind: PlayLogKind.PlayerChat } && _notifications is not null)
        {
            await _notifications.PublishChatAsync(
                    outcome.Campaign,
                    posted,
                    command.UserId,
                    members,
                    CampaignChatContext.Memberships(outcome.Campaign),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return OperationResults.Success(
            ToViewerDetail(outcome.Campaign, command.UserId, command.IsAdministrator, mentionable, participants));
    }

    private CampaignDetail ToViewerDetail(
        StoredCampaign campaign,
        Guid userId,
        bool isAdministrator,
        IReadOnlyList<CampaignLogMemberDetail> members,
        IReadOnlyList<CampaignParticipantDetail> participants)
    {
        var inspect = CampaignChatContext.CanInspectPrivateChat(isAdministrator, userId, campaign.PlayState);
        var names = members.ToDictionary(static member => member.UserId, static member => member.Username);
        return CampaignMapper.ToDetail(
            campaign,
            userId,
            _clock.UtcNow,
            CampaignPlayMapper.ToLogEntries(campaign, names, userId, inspect),
            members,
            CampaignChatContext.Channels(campaign, userId, members),
            inspect,
            participants);
    }
}
