namespace Campaign.Application.Campaigns;

/// <summary>
/// A campaign the current user manages or participates in.
/// </summary>
public sealed class CampaignListItem
{
    /// <summary>Gets the campaign identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the campaign name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the configured player-slot count.</summary>
    public required int PlayerSlotCount { get; init; }

    /// <summary>Gets the number of occupied player slots.</summary>
    public required int OccupiedPlayerSlots { get; init; }

    /// <summary>Gets whether the campaign is private.</summary>
    public required bool IsPrivate { get; init; }

    /// <summary>Gets whether the current user can manage the campaign.</summary>
    public required bool CanManage { get; init; }

    /// <summary>Gets whether the current user occupies a player slot.</summary>
    public required bool IsParticipant { get; init; }
}

/// <summary>
/// Campaign metadata visible to a member. Join passwords are never included.
/// </summary>
public sealed class CampaignDetail
{
    /// <summary>Gets the campaign identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the campaign name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the configured player-slot count.</summary>
    public required int PlayerSlotCount { get; init; }

    /// <summary>Gets the number of occupied player slots.</summary>
    public required int OccupiedPlayerSlots { get; init; }

    /// <summary>Gets whether the campaign is private.</summary>
    public required bool IsPrivate { get; init; }

    /// <summary>Gets whether the creating manager also occupies a player slot.</summary>
    public required bool CreatorIsParticipant { get; init; }

    /// <summary>Gets whether a map image is stored.</summary>
    public required bool HasMap { get; init; }

    /// <summary>Gets whether the current user can manage the campaign.</summary>
    public required bool CanManage { get; init; }

    /// <summary>Gets whether the current user occupies a player slot.</summary>
    public required bool IsParticipant { get; init; }

    /// <summary>Gets the optimistic concurrency revision.</summary>
    public required int Revision { get; init; }

    /// <summary>Gets when the campaign was created, in UTC.</summary>
    public required DateTimeOffset CreatedUtc { get; init; }

    /// <summary>Gets when the campaign was last edited, in UTC.</summary>
    public required DateTimeOffset UpdatedUtc { get; init; }

    /// <summary>Gets the factions.</summary>
    public required IReadOnlyList<FactionDetail> Factions { get; init; }

    /// <summary>Gets the ally groups.</summary>
    public required IReadOnlyList<AllyGroupDetail> AllyGroups { get; init; }

    /// <summary>Gets the external links.</summary>
    public required IReadOnlyList<CampaignLinkDetail> Links { get; init; }
}

/// <summary>
/// A faction in a campaign detail response.
/// </summary>
public sealed class FactionDetail
{
    /// <summary>Gets the faction identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the faction name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the subfaction names.</summary>
    public required IReadOnlyList<string> Subfactions { get; init; }

    /// <summary>Gets the ally-group name this faction joins, if any.</summary>
    public string? AllyGroupName { get; init; }
}

/// <summary>
/// An ally group in a campaign detail response.
/// </summary>
public sealed class AllyGroupDetail
{
    /// <summary>Gets the ally-group identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the ally-group name.</summary>
    public required string Name { get; init; }
}

/// <summary>
/// A labeled external link in a campaign detail response.
/// </summary>
public sealed class CampaignLinkDetail
{
    /// <summary>Gets the link identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the display label.</summary>
    public required string Label { get; init; }

    /// <summary>Gets the destination URL.</summary>
    public required string Url { get; init; }
}

/// <summary>
/// Persistence model for a campaign, including fields that must never be returned to clients.
/// </summary>
public sealed class StoredCampaign
{
    /// <summary>Gets the campaign identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the campaign name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the configured player-slot count.</summary>
    public required int PlayerSlotCount { get; init; }

    /// <summary>Gets whether the campaign is private.</summary>
    public required bool IsPrivate { get; init; }

    /// <summary>Gets the hashed join password for private campaigns.</summary>
    public string? JoinPasswordHash { get; init; }

    /// <summary>Gets whether the creating manager also occupies a player slot.</summary>
    public required bool CreatorIsParticipant { get; init; }

    /// <summary>Gets the map storage key, if a map has been uploaded.</summary>
    public string? MapStorageKey { get; init; }

    /// <summary>Gets the optimistic concurrency revision.</summary>
    public required int Revision { get; init; }

    /// <summary>Gets when the campaign was created, in UTC.</summary>
    public required DateTimeOffset CreatedUtc { get; init; }

    /// <summary>Gets when the campaign was last edited, in UTC.</summary>
    public required DateTimeOffset UpdatedUtc { get; init; }

    /// <summary>Gets the creating user's identifier.</summary>
    public required Guid CreatedByUserId { get; init; }

    /// <summary>Gets campaign memberships.</summary>
    public required IReadOnlyList<StoredCampaignMembership> Memberships { get; init; }

    /// <summary>Gets the factions.</summary>
    public required IReadOnlyList<StoredFaction> Factions { get; init; }

    /// <summary>Gets the ally groups.</summary>
    public required IReadOnlyList<StoredAllyGroup> AllyGroups { get; init; }

    /// <summary>Gets the external links.</summary>
    public required IReadOnlyList<StoredCampaignLink> Links { get; init; }
}

/// <summary>
/// A persisted campaign membership.
/// </summary>
public sealed class StoredCampaignMembership
{
    /// <summary>Gets the member's user identifier.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets whether the member is a campaign manager.</summary>
    public required bool IsGameMaster { get; init; }

    /// <summary>Gets whether the member occupies a player slot.</summary>
    public required bool IsPlayer { get; init; }
}

/// <summary>
/// A persisted faction.
/// </summary>
public sealed class StoredFaction
{
    /// <summary>Gets the faction identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the faction name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the subfaction names.</summary>
    public required IReadOnlyList<string> Subfactions { get; init; }

    /// <summary>Gets the ally-group name this faction joins, if any.</summary>
    public string? AllyGroupName { get; init; }
}

/// <summary>
/// A persisted ally group.
/// </summary>
public sealed class StoredAllyGroup
{
    /// <summary>Gets the ally-group identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the ally-group name.</summary>
    public required string Name { get; init; }
}

/// <summary>
/// A persisted campaign link.
/// </summary>
public sealed class StoredCampaignLink
{
    /// <summary>Gets the link identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the display label.</summary>
    public required string Label { get; init; }

    /// <summary>Gets the destination URL.</summary>
    public required string Url { get; init; }
}

/// <summary>
/// Outcome of a campaign persistence update.
/// </summary>
public sealed class UpdateStoredCampaignOutcome
{
    /// <summary>Gets a value indicating whether the update succeeded.</summary>
    public required bool IsSuccess { get; init; }

    /// <summary>Gets the stored campaign when successful.</summary>
    public StoredCampaign? Campaign { get; init; }

    /// <summary>Gets the error code when the update failed.</summary>
    public string? ErrorCode { get; init; }

    /// <summary>Gets the error message when the update failed.</summary>
    public string? Message { get; init; }
}
