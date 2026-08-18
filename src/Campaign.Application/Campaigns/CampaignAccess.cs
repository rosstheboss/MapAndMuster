using Campaign.Domain.Campaigns;

namespace Campaign.Application.Campaigns;

/// <summary>
/// Campaign listing and viewing permissions for a caller.
/// </summary>
public static class CampaignAccess
{
    /// <summary>
    /// Whether the caller may open the campaign page, map, and catalog files.
    /// </summary>
    /// <param name="campaign">The stored campaign.</param>
    /// <param name="userId">The viewing user's identifier.</param>
    /// <param name="isAdministrator">Whether the caller is a system administrator.</param>
    /// <returns><see langword="true"/> when the campaign may be viewed.</returns>
    public static bool CanView(StoredCampaign campaign, Guid userId, bool isAdministrator)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        if (isAdministrator)
        {
            return true;
        }

        if (CampaignMapper.MembershipFor(campaign, userId) is not null)
        {
            return true;
        }

        return campaign.IsPubliclyViewable;
    }

    /// <summary>
    /// Whether the campaign belongs on the All Campaigns list for this caller.
    /// Upcoming campaigns are listed so players can join. Active and completed campaigns
    /// are listed only when publicly viewable, or when the caller is a member or administrator.
    /// </summary>
    /// <param name="campaign">The stored campaign.</param>
    /// <param name="userId">The viewing user's identifier.</param>
    /// <param name="isAdministrator">Whether the caller is a system administrator.</param>
    /// <param name="utcNow">The current UTC instant.</param>
    /// <returns><see langword="true"/> when the campaign may be listed.</returns>
    public static bool CanList(StoredCampaign campaign, Guid userId, bool isAdministrator, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        if (CanView(campaign, userId, isAdministrator))
        {
            return true;
        }

        var status = CampaignMapper.ToSchedule(campaign).Evaluate(utcNow).Status;
        return status == CampaignStatus.Scheduled;
    }

    /// <summary>
    /// Whether the caller may join as a player.
    /// </summary>
    /// <param name="campaign">The stored campaign.</param>
    /// <param name="userId">The viewing user's identifier.</param>
    /// <param name="utcNow">The current UTC instant.</param>
    /// <returns><see langword="true"/> when join is allowed.</returns>
    public static bool CanJoin(StoredCampaign campaign, Guid userId, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        if (CampaignMapper.MembershipFor(campaign, userId) is not null)
        {
            return false;
        }

        if (CampaignMapper.OccupiedPlayerSlots(campaign) >= campaign.PlayerSlotCount)
        {
            return false;
        }

        return CampaignMapper.ToSchedule(campaign).Evaluate(utcNow).Status == CampaignStatus.Scheduled;
    }

    /// <summary>
    /// Whether the caller may leave. Managers cannot leave.
    /// </summary>
    /// <param name="campaign">The stored campaign.</param>
    /// <param name="userId">The viewing user's identifier.</param>
    /// <returns><see langword="true"/> when leave is allowed.</returns>
    public static bool CanLeave(StoredCampaign campaign, Guid userId)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        var membership = CampaignMapper.MembershipFor(campaign, userId);
        return membership is not null && !membership.IsGameMaster;
    }

    /// <summary>
    /// Whether the caller may add, kick, or assign factions for other players.
    /// </summary>
    public static bool CanStaffMembers(StoredCampaign campaign, Guid userId, bool isAdministrator)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        return isAdministrator || CampaignMapper.MembershipFor(campaign, userId)?.IsGameMaster == true;
    }
}
