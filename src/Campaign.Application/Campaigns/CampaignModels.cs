using Campaign.Application.Maps;
using Campaign.Application.Play;

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

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the configured player-slot count.</summary>
    public required int PlayerSlotCount { get; init; }

    /// <summary>Gets the number of occupied player slots.</summary>
    public required int OccupiedPlayerSlots { get; init; }

    /// <summary>Gets whether the campaign is private.</summary>
    public required bool IsPrivate { get; init; }

    /// <summary>Gets whether non-members may view the campaign.</summary>
    public required bool IsPubliclyViewable { get; init; }

    /// <summary>Gets whether the current user can manage the campaign.</summary>
    public required bool CanManage { get; init; }

    /// <summary>Gets whether the current user occupies a player slot.</summary>
    public required bool IsParticipant { get; init; }

    /// <summary>Gets whether the current user may view the campaign page.</summary>
    public required bool CanView { get; init; }

    /// <summary>Gets whether the current user may join as a player.</summary>
    public required bool CanJoin { get; init; }

    /// <summary>Gets whether the current user may leave the campaign.</summary>
    public required bool CanLeave { get; init; }

    /// <summary>Gets the optional city.</summary>
    public string? City { get; init; }

    /// <summary>Gets the optional state, province, or region.</summary>
    public string? Region { get; init; }

    /// <summary>Gets the optional country.</summary>
    public string? Country { get; init; }

    /// <summary>Gets the campaign lifecycle status.</summary>
    public required string Status { get; init; }

    /// <summary>Gets the campaign start instant, in UTC.</summary>
    public required DateTimeOffset StartsUtc { get; init; }

    /// <summary>Gets the campaign end instant, in UTC.</summary>
    public required DateTimeOffset EndsUtc { get; init; }

    /// <summary>Gets the 1-based current round when the campaign is in progress.</summary>
    public int? CurrentRound { get; init; }

    /// <summary>Gets the display label for the current phase when the campaign is in progress.</summary>
    public string? CurrentPhaseLabel { get; init; }

    /// <summary>Gets when the current phase closes, in UTC.</summary>
    public DateTimeOffset? CurrentPhaseEndsUtc { get; init; }

    /// <summary>Gets whether the viewer may act on the live campaign board.</summary>
    public required bool CanPlay { get; init; }
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

    /// <summary>Gets whether non-members may view the campaign.</summary>
    public required bool IsPubliclyViewable { get; init; }

    /// <summary>Gets whether the creating manager also occupies a player slot.</summary>
    public required bool CreatorIsParticipant { get; init; }

    /// <summary>Gets the optional city.</summary>
    public string? City { get; init; }

    /// <summary>Gets the optional state, province, or region.</summary>
    public string? Region { get; init; }

    /// <summary>Gets the optional country.</summary>
    public string? Country { get; init; }

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

    /// <summary>Gets the terrain types.</summary>
    public required IReadOnlyList<TerrainTypeDetail> TerrainTypes { get; init; }

    /// <summary>Gets the structure types.</summary>
    public required IReadOnlyList<StructureTypeDetail> StructureTypes { get; init; }

    /// <summary>Gets the ally groups.</summary>
    public required IReadOnlyList<AllyGroupDetail> AllyGroups { get; init; }

    /// <summary>Gets the external links.</summary>
    public required IReadOnlyList<CampaignLinkDetail> Links { get; init; }

    /// <summary>Gets the IANA time zone used when the schedule was configured.</summary>
    public required string TimeZoneId { get; init; }

    /// <summary>Gets the start as a local wall-clock value in the campaign time zone.</summary>
    public required string StartsAtLocal { get; init; }

    /// <summary>Gets the campaign start instant, in UTC.</summary>
    public required DateTimeOffset StartsUtc { get; init; }

    /// <summary>Gets the campaign end instant, in UTC.</summary>
    public required DateTimeOffset EndsUtc { get; init; }

    /// <summary>Gets the number of rounds.</summary>
    public required int RoundCount { get; init; }

    /// <summary>Gets the round-length amount.</summary>
    public required int RoundLengthAmount { get; init; }

    /// <summary>Gets the round-length unit name.</summary>
    public required string RoundLengthUnit { get; init; }

    /// <summary>Gets the ordered action and battle steps in a round.</summary>
    public required IReadOnlyList<RoundPhaseDetail> Phases { get; init; }

    /// <summary>Gets the campaign lifecycle status.</summary>
    public required string Status { get; init; }

    /// <summary>Gets the 1-based current round when the campaign is in progress.</summary>
    public int? CurrentRound { get; init; }

    /// <summary>Gets the 1-based current phase in the round when the campaign is in progress.</summary>
    public int? CurrentPhaseNumber { get; init; }

    /// <summary>Gets the current phase kind when the campaign is in progress.</summary>
    public string? CurrentPhaseKind { get; init; }

    /// <summary>Gets when the current phase opened, in UTC.</summary>
    public DateTimeOffset? CurrentPhaseStartsUtc { get; init; }

    /// <summary>Gets when the current phase closes, in UTC.</summary>
    public DateTimeOffset? CurrentPhaseEndsUtc { get; init; }

    /// <summary>Gets the viewer's chosen faction, when they are a player who has picked one.</summary>
    public Guid? FactionId { get; init; }

    /// <summary>Gets the viewer's chosen subfaction, when one is selected.</summary>
    public string? Subfaction { get; init; }

    /// <summary>Gets whether the viewer may act on the live campaign board.</summary>
    public required bool CanPlay { get; init; }

    /// <summary>Gets whether the viewer still needs to pick a faction.</summary>
    public required bool CanChooseFaction { get; init; }

    /// <summary>Gets whether the viewer may post in the public campaign log.</summary>
    public required bool CanChat { get; init; }

    /// <summary>Gets current members who may be tagged in chat.</summary>
    public required IReadOnlyList<CampaignLogMemberDetail> MentionableMembers { get; init; }

    /// <summary>Gets the public campaign log, including chat. Unrevealed orders are omitted.</summary>
    public required IReadOnlyList<PlayLogEntryDetail> Log { get; init; }
}

/// <summary>
/// A current campaign member who may be tagged in the public log.
/// </summary>
public sealed class CampaignLogMemberDetail
{
    /// <summary>Gets the user identifier.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets the unique username.</summary>
    public required string Username { get; init; }

    /// <summary>Gets the name shown to other users.</summary>
    public required string DisplayName { get; init; }
}

/// <summary>
/// An action or battle step in a campaign round.
/// </summary>
public sealed class RoundPhaseDetail
{
    /// <summary>Gets the phase kind name.</summary>
    public required string Kind { get; init; }

    /// <summary>Gets the duration amount.</summary>
    public required int DurationAmount { get; init; }

    /// <summary>Gets the duration unit name.</summary>
    public required string DurationUnit { get; init; }
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

    /// <summary>Gets the unique faction color as #RRGGBB.</summary>
    public required string Color { get; init; }

    /// <summary>Gets the subfaction names.</summary>
    public required IReadOnlyList<string> Subfactions { get; init; }

    /// <summary>Gets the ally-group name this faction joins, if any.</summary>
    public string? AllyGroupName { get; init; }

    /// <summary>Gets whether a player who chooses this faction must pick a subfaction.</summary>
    public required bool RequiresSubfaction { get; init; }

    /// <summary>Gets whether the faction has an uploaded flag image.</summary>
    public required bool HasFlagImage { get; init; }
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

    /// <summary>Gets whether non-members may view the campaign.</summary>
    public required bool IsPubliclyViewable { get; init; }

    /// <summary>Gets the hashed join password for private campaigns.</summary>
    public string? JoinPasswordHash { get; init; }

    /// <summary>Gets whether the creating manager also occupies a player slot.</summary>
    public required bool CreatorIsParticipant { get; init; }

    /// <summary>Gets the optional city.</summary>
    public string? City { get; init; }

    /// <summary>Gets the optional state, province, or region.</summary>
    public string? Region { get; init; }

    /// <summary>Gets the optional country.</summary>
    public string? Country { get; init; }

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

    /// <summary>Gets the IANA time zone used when the schedule was configured.</summary>
    public required string TimeZoneId { get; init; }

    /// <summary>Gets the campaign start instant, in UTC.</summary>
    public required DateTimeOffset StartsUtc { get; init; }

    /// <summary>Gets the campaign end instant, in UTC.</summary>
    public required DateTimeOffset EndsUtc { get; init; }

    /// <summary>Gets the number of rounds.</summary>
    public required int RoundCount { get; init; }

    /// <summary>Gets the round-length amount.</summary>
    public required int RoundLengthAmount { get; init; }

    /// <summary>Gets the round-length unit name.</summary>
    public required string RoundLengthUnit { get; init; }

    /// <summary>Gets the ordered action and battle steps in a round.</summary>
    public required IReadOnlyList<StoredRoundPhase> Phases { get; init; }

    /// <summary>Gets the overlay territory graph, when one has been saved.</summary>
    public StoredMapGraph? MapGraph { get; init; }

    /// <summary>Gets launched play state, when the campaign has been seeded.</summary>
    public Campaign.Domain.Play.CampaignPlayState? PlayState { get; init; }

    /// <summary>Gets the terrain types.</summary>
    public required IReadOnlyList<StoredTerrainType> TerrainTypes { get; init; }

    /// <summary>Gets the structure types.</summary>
    public required IReadOnlyList<StoredStructureType> StructureTypes { get; init; }
}

/// <summary>
/// A persisted action or battle step.
/// </summary>
public sealed class StoredRoundPhase
{
    /// <summary>Gets the phase kind name.</summary>
    public required string Kind { get; init; }

    /// <summary>Gets the duration amount.</summary>
    public required int DurationAmount { get; init; }

    /// <summary>Gets the duration unit name.</summary>
    public required string DurationUnit { get; init; }
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

    /// <summary>Gets the chosen faction, when the member is a player who has picked one.</summary>
    public Guid? FactionId { get; init; }

    /// <summary>Gets the chosen subfaction name, when required.</summary>
    public string? Subfaction { get; init; }
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

    /// <summary>Gets the unique faction color as #RRGGBB.</summary>
    public required string Color { get; init; }

    /// <summary>Gets the subfaction names.</summary>
    public required IReadOnlyList<string> Subfactions { get; init; }

    /// <summary>Gets the ally-group name this faction joins, if any.</summary>
    public string? AllyGroupName { get; init; }

    /// <summary>Gets whether a player who chooses this faction must pick a subfaction.</summary>
    public required bool RequiresSubfaction { get; init; }

    /// <summary>Gets the stored flag image key, when a custom flag was uploaded.</summary>
    public string? FlagImageStorageKey { get; init; }
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

/// <summary>
/// A terrain type in a campaign detail response.
/// </summary>
public sealed class TerrainTypeDetail
{
    /// <summary>Gets the terrain type identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the terrain type name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the unique overlay color as #RRGGBB.</summary>
    public required string Color { get; init; }

    /// <summary>Gets the missions.</summary>
    public required IReadOnlyList<MissionDetail> Missions { get; init; }
}

/// <summary>
/// A structure type in a campaign detail response.
/// </summary>
public sealed class StructureTypeDetail
{
    /// <summary>Gets the structure type identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the structure name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the built-in logo key, when no custom image is stored.</summary>
    public string? BuiltinSymbol { get; init; }

    /// <summary>Gets whether a custom logo image is stored.</summary>
    public required bool HasImage { get; init; }

    /// <summary>Gets whether a custom pillaged logo image is stored.</summary>
    public required bool HasPillagedImage { get; init; }

    /// <summary>Gets the missions.</summary>
    public required IReadOnlyList<MissionDetail> Missions { get; init; }
}

/// <summary>
/// A mission nested under a terrain type or structure.
/// </summary>
public sealed class MissionDetail
{
    /// <summary>Gets the mission identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the mission name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the optional http or https link.</summary>
    public string? Url { get; init; }

    /// <summary>Gets whether a document file is stored.</summary>
    public required bool HasFile { get; init; }

    /// <summary>Gets the original uploaded file name, when a file is stored.</summary>
    public string? FileName { get; init; }
}

/// <summary>
/// A persisted terrain type.
/// </summary>
public sealed class StoredTerrainType
{
    /// <summary>Gets the terrain type identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the terrain type name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the unique overlay color.</summary>
    public required string Color { get; init; }

    /// <summary>Gets the missions.</summary>
    public required IReadOnlyList<StoredMission> Missions { get; init; }
}

/// <summary>
/// A persisted structure type.
/// </summary>
public sealed class StoredStructureType
{
    /// <summary>Gets the structure type identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the structure name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the built-in logo key, when used.</summary>
    public string? BuiltinSymbol { get; init; }

    /// <summary>Gets the stored logo key, when a custom image was uploaded.</summary>
    public string? ImageStorageKey { get; init; }

    /// <summary>Gets the stored pillaged logo key, when a custom pillaged image was uploaded.</summary>
    public string? PillagedImageStorageKey { get; init; }

    /// <summary>Gets the missions.</summary>
    public required IReadOnlyList<StoredMission> Missions { get; init; }
}

/// <summary>
/// A persisted mission.
/// </summary>
public sealed class StoredMission
{
    /// <summary>Gets the mission identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the mission name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the optional http or https link.</summary>
    public string? Url { get; init; }

    /// <summary>Gets the stored document key, when a file was uploaded.</summary>
    public string? FileStorageKey { get; init; }

    /// <summary>Gets the original uploaded file name.</summary>
    public string? FileName { get; init; }
}
