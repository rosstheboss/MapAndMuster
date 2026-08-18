using Campaign.Application.Common;
using Campaign.Application.Notifications;
using Campaign.Application.Ports;
using Campaign.Domain.Campaigns;
using Campaign.Domain.Play;

namespace Campaign.Application.Campaigns;

/// <summary>
/// Searches accounts a manager or administrator may add to a campaign.
/// </summary>
public sealed class SearchCampaignUsersHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IUserAccountStore _accounts;

    /// <summary>Initializes a new handler.</summary>
    public SearchCampaignUsersHandler(ICampaignStore campaigns, IUserAccountStore accounts)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(accounts);
        _campaigns = campaigns;
        _accounts = accounts;
    }

    /// <summary>Returns matching accounts that are not already members.</summary>
    public async Task<OperationResult<IReadOnlyList<MentionableAccount>>> HandleAsync(
        SearchCampaignUsersCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var campaign = await _campaigns.FindByIdAsync(command.CampaignId, cancellationToken).ConfigureAwait(false);
        if (campaign is null || !CampaignAccess.CanView(campaign, command.UserId, command.IsAdministrator))
        {
            return OperationResults.Failure<IReadOnlyList<MentionableAccount>>(
                ErrorCodes.CampaignNotFound,
                "The campaign was not found.");
        }

        if (!CampaignAccess.CanStaffMembers(campaign, command.UserId, command.IsAdministrator))
        {
            return OperationResults.Failure<IReadOnlyList<MentionableAccount>>(
                ErrorCodes.CampaignForbidden,
                "Only a campaign manager or administrator can search for players to add.");
        }

        var query = command.Query?.Trim() ?? string.Empty;
        if (query.Length < 2)
        {
            return OperationResults.Success<IReadOnlyList<MentionableAccount>>([]);
        }

        var memberIds = campaign.Memberships.Select(static member => member.UserId).ToHashSet();
        var hits = await _accounts.SearchAsync(query, 20, cancellationToken).ConfigureAwait(false);
        return OperationResults.Success<IReadOnlyList<MentionableAccount>>(
            [.. hits.Where(hit => !memberIds.Contains(hit.UserId))]);
    }
}

/// <summary>
/// Adds a player to a public or private campaign without requiring the join password.
/// </summary>
public sealed class AddCampaignMemberHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IUserAccountStore _accounts;
    private readonly IClock _clock;

    /// <summary>Initializes a new handler.</summary>
    public AddCampaignMemberHandler(ICampaignStore campaigns, IUserAccountStore accounts, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(accounts);
        ArgumentNullException.ThrowIfNull(clock);
        _campaigns = campaigns;
        _accounts = accounts;
        _clock = clock;
    }

    /// <summary>Adds the target as a player when a slot remains and the campaign is not completed.</summary>
    public async Task<OperationResult> HandleAsync(AddCampaignMemberCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var campaign = await _campaigns.FindByIdAsync(command.CampaignId, cancellationToken).ConfigureAwait(false);
        var utcNow = _clock.UtcNow;
        if (campaign is null || !CampaignAccess.CanView(campaign, command.UserId, command.IsAdministrator))
        {
            return OperationResult.Failure(ErrorCodes.CampaignNotFound, "The campaign was not found.");
        }

        if (!CampaignAccess.CanStaffMembers(campaign, command.UserId, command.IsAdministrator))
        {
            return OperationResult.Failure(
                ErrorCodes.CampaignForbidden,
                "Only a campaign manager or administrator can add players.");
        }

        if (campaign.Revision != command.ExpectedRevision)
        {
            return OperationResult.Failure(
                ErrorCodes.ConcurrencyConflict,
                "The campaign was updated by another request. Reload and try again.");
        }

        var target = await _accounts.FindByIdAsync(command.TargetUserId, cancellationToken).ConfigureAwait(false);
        if (target is null)
        {
            return OperationResult.Failure(ErrorCodes.ProfileNotFound, "The user was not found.");
        }

        if (CampaignMapper.MembershipFor(campaign, command.TargetUserId) is not null)
        {
            return OperationResult.Failure(ErrorCodes.CampaignAlreadyMember, "That user is already a member of this campaign.");
        }

        var progress = CampaignMapper.ToSchedule(campaign).Evaluate(utcNow);
        if (progress.Status == CampaignStatus.Completed)
        {
            return OperationResult.Failure(ErrorCodes.CampaignJoinClosed, "Players cannot be added after the campaign ends.");
        }

        if (CampaignMapper.OccupiedPlayerSlots(campaign) >= campaign.PlayerSlotCount)
        {
            return OperationResult.Failure(ErrorCodes.CampaignJoinFull, "This campaign has no remaining player slots.");
        }

        var memberships = campaign.Memberships
            .Append(new StoredCampaignMembership
            {
                UserId = command.TargetUserId,
                IsGameMaster = false,
                IsPlayer = true,
            })
            .ToArray();
        var updated = CampaignMapClone.CloneWithMemberships(campaign, memberships, utcNow);
        var outcome = await _campaigns.UpdateAsync(updated, campaign.Revision, cancellationToken).ConfigureAwait(false);
        if (!outcome.IsSuccess)
        {
            return OperationResult.Failure(
                outcome.ErrorCode ?? ErrorCodes.CampaignNotFound,
                outcome.Message ?? "The player could not be added.");
        }

        return OperationResult.Success();
    }
}

/// <summary>
/// Removes a non-manager player and notifies them.
/// </summary>
public sealed class KickCampaignMemberHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;
    private readonly CampaignNotificationPublisher _notifications;

    /// <summary>Initializes a new handler.</summary>
    public KickCampaignMemberHandler(
        ICampaignStore campaigns,
        IClock clock,
        CampaignNotificationPublisher notifications)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(notifications);
        _campaigns = campaigns;
        _clock = clock;
        _notifications = notifications;
    }

    /// <summary>Kicks a player who is not a campaign manager.</summary>
    public async Task<OperationResult> HandleAsync(KickCampaignMemberCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var campaign = await _campaigns.FindByIdAsync(command.CampaignId, cancellationToken).ConfigureAwait(false);
        if (campaign is null || !CampaignAccess.CanView(campaign, command.UserId, command.IsAdministrator))
        {
            return OperationResult.Failure(ErrorCodes.CampaignNotFound, "The campaign was not found.");
        }

        if (!CampaignAccess.CanStaffMembers(campaign, command.UserId, command.IsAdministrator))
        {
            return OperationResult.Failure(
                ErrorCodes.CampaignForbidden,
                "Only a campaign manager or administrator can remove players.");
        }

        if (campaign.Revision != command.ExpectedRevision)
        {
            return OperationResult.Failure(
                ErrorCodes.ConcurrencyConflict,
                "The campaign was updated by another request. Reload and try again.");
        }

        var target = CampaignMapper.MembershipFor(campaign, command.TargetUserId);
        if (target is null || !target.IsPlayer)
        {
            return OperationResult.Failure(ErrorCodes.CampaignMemberNotFound, "That player is not in this campaign.");
        }

        if (target.IsGameMaster)
        {
            return OperationResult.Failure(
                ErrorCodes.CampaignForbidden,
                "A campaign manager cannot be removed this way.");
        }

        var utcNow = _clock.UtcNow;
        var memberships = campaign.Memberships.Where(member => member.UserId != command.TargetUserId).ToArray();
        var play = campaign.PlayState is null
            ? null
            : CampaignPlayRules.RemoveController(campaign.PlayState, command.TargetUserId, utcNow);
        var updated = CampaignMapClone.CloneWithMemberships(campaign, memberships, utcNow, play);
        var outcome = await _campaigns.UpdateAsync(updated, campaign.Revision, cancellationToken).ConfigureAwait(false);
        if (!outcome.IsSuccess || outcome.Campaign is null)
        {
            return OperationResult.Failure(
                outcome.ErrorCode ?? ErrorCodes.CampaignNotFound,
                outcome.Message ?? "The player could not be removed.");
        }

        await _notifications.PublishKickedAsync(outcome.Campaign, command.TargetUserId, cancellationToken)
            .ConfigureAwait(false);
        return OperationResult.Success();
    }
}
