using Campaign.Application.Common;
using Campaign.Application.Ports;
using Campaign.Domain.Campaigns;

namespace Campaign.Application.Campaigns;

/// <summary>
/// Adds the caller as a player when the campaign is still upcoming.
/// </summary>
public sealed class JoinCampaignHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;
    private readonly ISecretHasher _secrets;

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    /// <param name="campaigns">The campaign store.</param>
    /// <param name="clock">The clock.</param>
    /// <param name="secrets">The secret hasher.</param>
    public JoinCampaignHandler(ICampaignStore campaigns, IClock clock, ISecretHasher secrets)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(secrets);
        _campaigns = campaigns;
        _clock = clock;
        _secrets = secrets;
    }

    /// <summary>
    /// Joins the campaign as a player when the caller is not already a member.
    /// </summary>
    /// <param name="command">The join command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated campaign list item.</returns>
    public async Task<OperationResult<CampaignListItem>> HandleAsync(
        JoinCampaignCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var campaign = await _campaigns.FindByIdAsync(command.CampaignId, cancellationToken).ConfigureAwait(false);
        var utcNow = _clock.UtcNow;
        if (campaign is null || !CampaignAccess.CanList(campaign, command.UserId, command.IsAdministrator, utcNow))
        {
            return OperationResults.Failure<CampaignListItem>(ErrorCodes.CampaignNotFound, "The campaign was not found.");
        }

        if (CampaignMapper.MembershipFor(campaign, command.UserId) is not null)
        {
            return OperationResults.Failure<CampaignListItem>(
                ErrorCodes.CampaignAlreadyMember,
                "You are already a member of this campaign.");
        }

        var progress = CampaignMapper.ToSchedule(campaign).Evaluate(utcNow);
        if (progress.Status != CampaignStatus.Scheduled)
        {
            return OperationResults.Failure<CampaignListItem>(
                ErrorCodes.CampaignJoinClosed,
                "You can only join a campaign before it starts.");
        }

        if (CampaignMapper.OccupiedPlayerSlots(campaign) >= campaign.PlayerSlotCount)
        {
            return OperationResults.Failure<CampaignListItem>(
                ErrorCodes.CampaignJoinFull,
                "This campaign has no remaining player slots.");
        }

        if (campaign.IsPrivate)
        {
            if (string.IsNullOrWhiteSpace(command.JoinPassword)
                || string.IsNullOrWhiteSpace(campaign.JoinPasswordHash)
                || !_secrets.Verify(campaign.JoinPasswordHash, command.JoinPassword))
            {
                return OperationResults.Failure<CampaignListItem>(
                    ErrorCodes.CampaignJoinPasswordInvalid,
                    "The join password is not correct.");
            }
        }

        var memberships = campaign.Memberships
            .Append(new StoredCampaignMembership
            {
                UserId = command.UserId,
                IsGameMaster = false,
                IsPlayer = true,
            })
            .ToArray();
        var updated = CampaignMapClone.CloneWithMemberships(campaign, memberships, utcNow);
        var outcome = await _campaigns.UpdateAsync(updated, campaign.Revision, cancellationToken).ConfigureAwait(false);
        if (!outcome.IsSuccess || outcome.Campaign is null)
        {
            return OperationResults.Failure<CampaignListItem>(
                outcome.ErrorCode ?? ErrorCodes.CampaignNotFound,
                outcome.Message ?? "The campaign could not be joined.");
        }

        return OperationResults.Success(
            CampaignMapper.ToListItem(outcome.Campaign, command.UserId, utcNow, command.IsAdministrator));
    }
}

/// <summary>
/// Removes a non-manager player from a campaign.
/// </summary>
public sealed class LeaveCampaignHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    /// <param name="campaigns">The campaign store.</param>
    /// <param name="clock">The clock.</param>
    public LeaveCampaignHandler(ICampaignStore campaigns, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        _campaigns = campaigns;
        _clock = clock;
    }

    /// <summary>
    /// Leaves the campaign when the caller is a player and not a manager.
    /// </summary>
    /// <param name="command">The leave command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A successful result when the caller left.</returns>
    public async Task<OperationResult> HandleAsync(LeaveCampaignCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var campaign = await _campaigns.FindByIdAsync(command.CampaignId, cancellationToken).ConfigureAwait(false);
        var membership = campaign is null ? null : CampaignMapper.MembershipFor(campaign, command.UserId);
        if (campaign is null || membership is null)
        {
            return OperationResult.Failure(ErrorCodes.CampaignNotFound, "The campaign was not found.");
        }

        if (membership.IsGameMaster)
        {
            return OperationResult.Failure(
                ErrorCodes.CampaignForbidden,
                "A campaign manager cannot leave this campaign.");
        }

        var memberships = campaign.Memberships.Where(member => member.UserId != command.UserId).ToArray();
        var updated = CampaignMapClone.CloneWithMemberships(campaign, memberships, _clock.UtcNow);
        var outcome = await _campaigns.UpdateAsync(updated, campaign.Revision, cancellationToken).ConfigureAwait(false);
        if (!outcome.IsSuccess)
        {
            return OperationResult.Failure(
                outcome.ErrorCode ?? ErrorCodes.CampaignNotFound,
                outcome.Message ?? "The campaign could not be left.");
        }

        return OperationResult.Success();
    }
}
