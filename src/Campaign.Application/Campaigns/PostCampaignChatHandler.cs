using Campaign.Application.Common;
using Campaign.Application.Play;
using Campaign.Application.Ports;
using Campaign.Domain.Play;

namespace Campaign.Application.Campaigns;

/// <summary>
/// Posts a public chat message to a campaign log.
/// </summary>
public sealed class PostCampaignChatHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;
    private readonly IUserAccountStore _accounts;

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    /// <param name="campaigns">The campaign store.</param>
    /// <param name="clock">The clock.</param>
    /// <param name="accounts">The user account store.</param>
    public PostCampaignChatHandler(ICampaignStore campaigns, IClock clock, IUserAccountStore accounts)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(accounts);
        _campaigns = campaigns;
        _clock = clock;
        _accounts = accounts;
    }

    /// <summary>
    /// Appends a chat message when the caller is a current campaign member.
    /// </summary>
    /// <param name="command">The chat command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated campaign detail.</returns>
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

        var mentionable = await CampaignPlayMapper.ChatMembersAsync(campaign, _accounts, cancellationToken)
            .ConfigureAwait(false);
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
                out var error))
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

        var names = await CampaignPlayMapper.UsernamesAsync(outcome.Campaign, _accounts, cancellationToken)
            .ConfigureAwait(false);
        var refreshedMembers = await CampaignPlayMapper.ChatMembersAsync(outcome.Campaign, _accounts, cancellationToken)
            .ConfigureAwait(false);
        return OperationResults.Success(CampaignMapper.ToDetail(
            outcome.Campaign,
            command.UserId,
            _clock.UtcNow,
            CampaignPlayMapper.ToLogEntries(outcome.Campaign, names),
            refreshedMembers));
    }
}
