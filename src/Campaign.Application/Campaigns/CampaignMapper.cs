namespace Campaign.Application.Campaigns;

/// <summary>
/// Maps stored campaigns onto member-visible models. Join password hashes are omitted.
/// </summary>
public static class CampaignMapper
{
    /// <summary>
    /// Maps a stored campaign onto a list item for the specified viewer.
    /// </summary>
    /// <param name="campaign">The stored campaign.</param>
    /// <param name="viewerUserId">The viewing user's identifier.</param>
    /// <returns>The list item.</returns>
    public static CampaignListItem ToListItem(StoredCampaign campaign, Guid viewerUserId)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        var membership = MembershipFor(campaign, viewerUserId);
        return new CampaignListItem
        {
            Id = campaign.Id,
            Name = campaign.Name,
            PlayerSlotCount = campaign.PlayerSlotCount,
            OccupiedPlayerSlots = OccupiedPlayerSlots(campaign),
            IsPrivate = campaign.IsPrivate,
            CanManage = membership?.IsGameMaster == true,
            IsParticipant = membership?.IsPlayer == true,
        };
    }

    /// <summary>
    /// Maps a stored campaign onto a member detail for the specified viewer.
    /// </summary>
    /// <param name="campaign">The stored campaign.</param>
    /// <param name="viewerUserId">The viewing user's identifier.</param>
    /// <returns>The detail.</returns>
    public static CampaignDetail ToDetail(StoredCampaign campaign, Guid viewerUserId)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        var membership = MembershipFor(campaign, viewerUserId);
        return new CampaignDetail
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Description = campaign.Description,
            PlayerSlotCount = campaign.PlayerSlotCount,
            OccupiedPlayerSlots = OccupiedPlayerSlots(campaign),
            IsPrivate = campaign.IsPrivate,
            CreatorIsParticipant = campaign.CreatorIsParticipant,
            HasMap = !string.IsNullOrWhiteSpace(campaign.MapStorageKey),
            CanManage = membership?.IsGameMaster == true,
            IsParticipant = membership?.IsPlayer == true,
            Revision = campaign.Revision,
            CreatedUtc = campaign.CreatedUtc,
            UpdatedUtc = campaign.UpdatedUtc,
            Factions = [.. campaign.Factions.Select(static faction => new FactionDetail
            {
                Id = faction.Id,
                Name = faction.Name,
                Subfactions = faction.Subfactions,
                AllyGroupName = faction.AllyGroupName,
            })],
            AllyGroups = [.. campaign.AllyGroups.Select(static group => new AllyGroupDetail
            {
                Id = group.Id,
                Name = group.Name,
            })],
            Links = [.. campaign.Links.Select(static link => new CampaignLinkDetail
            {
                Id = link.Id,
                Label = link.Label,
                Url = link.Url,
            })],
        };
    }

    /// <summary>
    /// Counts memberships that occupy a player slot.
    /// </summary>
    /// <param name="campaign">The stored campaign.</param>
    /// <returns>The occupied slot count.</returns>
    public static int OccupiedPlayerSlots(StoredCampaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        return campaign.Memberships.Count(static membership => membership.IsPlayer);
    }

    /// <summary>
    /// Returns the viewer's membership, if any.
    /// </summary>
    /// <param name="campaign">The stored campaign.</param>
    /// <param name="viewerUserId">The viewing user's identifier.</param>
    /// <returns>The membership, or <see langword="null"/>.</returns>
    public static StoredCampaignMembership? MembershipFor(StoredCampaign campaign, Guid viewerUserId)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        return campaign.Memberships.FirstOrDefault(membership => membership.UserId == viewerUserId);
    }
}
