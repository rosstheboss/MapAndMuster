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

    /// <summary>Gets the item objective types. Empty means none.</summary>
    public IReadOnlyList<ItemObjectiveTypeDetail> ItemObjectiveTypes { get; init; } = [];

    /// <summary>Gets the public campaign objectives. Empty means none.</summary>
    public IReadOnlyList<PublicObjectiveTypeDetail> PublicObjectiveTypes { get; init; } = [];

    /// <summary>Gets reusable special rules. Empty means none.</summary>
    public IReadOnlyList<SpecialRuleDetail> SpecialRules { get; init; } = [];

    /// <summary>Gets private campaign objectives the viewer may see. Secret fields are omitted unless authorized.</summary>
    public IReadOnlyList<PrivateObjectiveTypeDetail> PrivateObjectiveTypes { get; init; } = [];

    /// <summary>Gets assigned private objectives visible to the viewer.</summary>
    public IReadOnlyList<PrivateObjectiveAssignmentDetail> PrivateObjectives { get; init; } = [];

    /// <summary>Gets public unclaimed private-objective counts.</summary>
    public IReadOnlyList<PrivateObjectiveUnclaimedCountDetail> PrivateObjectiveUnclaimedCounts { get; init; } = [];

    /// <summary>Gets campaign points awarded to the winner when differential scoring is off.</summary>
    public int PointsPerBattleWon { get; init; }

    /// <summary>Gets campaign points awarded to each participant of a draw.</summary>
    public int PointsPerBattleDraw { get; init; }

    /// <summary>Gets whether battle campaign points use score differential.</summary>
    public bool UseDifferentialBattleScoring { get; init; } = true;

    /// <summary>Gets the multiplier applied to the winner-minus-loser score difference.</summary>
    public decimal DifferentialMultiplier { get; init; } = 1m;

    /// <summary>Gets the inclusive lower clamp for differential campaign points.</summary>
    public int DifferentialMinimum { get; init; }

    /// <summary>Gets the inclusive upper clamp for differential campaign points.</summary>
    public int DifferentialMaximum { get; init; } = 10;

    /// <summary>Gets whether the loser can receive negative campaign points.</summary>
    public bool AllowNegativeDifferential { get; init; }

    /// <summary>Gets campaign points for most territories currently controlled. Zero ignores the objective.</summary>
    public int MostTerritoriesCampaignPoints { get; init; }

    /// <summary>Gets campaign points for the longest owned territory chain. Zero ignores the objective.</summary>
    public int LongestTerritoryChainCampaignPoints { get; init; }

    /// <summary>Gets campaign points for most battle wins. Zero ignores the objective.</summary>
    public int MostBattlesWonCampaignPoints { get; init; }

    /// <summary>Gets current campaign-point standings for players.</summary>
    public IReadOnlyList<CampaignPointStandingDetail> Standings { get; init; } = [];

    /// <summary>Gets current top-five leaders for enabled ranking public objectives.</summary>
    public IReadOnlyList<PublicObjectiveLeaderboardDetail> PublicObjectiveLeaderboards { get; init; } = [];

    /// <summary>Gets factions that left their ally group through Backstab.</summary>
    public IReadOnlyList<Guid> BrokenAllyFactionIds { get; init; } = [];

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

    /// <summary>Gets whether the viewer may post in the campaign log.</summary>
    public required bool CanChat { get; init; }

    /// <summary>Gets whether the viewer is an administrator currently in debug mode on this campaign.</summary>
    public bool CanInspectPrivateChat { get; init; }

    /// <summary>Gets members attached to the campaign, including roles and chosen faction.</summary>
    public IReadOnlyList<CampaignParticipantDetail> Participants { get; init; } = [];

    /// <summary>Gets current members who may be tagged in chat.</summary>
    public required IReadOnlyList<CampaignLogMemberDetail> MentionableMembers { get; init; }

    /// <summary>Gets compose targets: public, members, factions, and ally groups.</summary>
    public IReadOnlyList<ChatChannelDetail> ChatChannels { get; init; } = [];

    /// <summary>Gets the campaign log, including chat the viewer is allowed to see. Unrevealed orders are omitted.</summary>
    public required IReadOnlyList<PlayLogEntryDetail> Log { get; init; }
}

/// <summary>
/// A member attached to a campaign, shown on the Participants panel.
/// </summary>
public sealed class CampaignParticipantDetail
{
    /// <summary>Gets the user identifier.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets the unique username.</summary>
    public required string Username { get; init; }

    /// <summary>Gets the name shown to other users.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Gets whether the member occupies a player slot.</summary>
    public required bool IsPlayer { get; init; }

    /// <summary>Gets whether the member is a campaign manager.</summary>
    public required bool IsGameMaster { get; init; }

    /// <summary>Gets whether the member is a system administrator.</summary>
    public required bool IsAdministrator { get; init; }

    /// <summary>Gets the chosen faction name, when the member has selected one.</summary>
    public string? FactionName { get; init; }

    /// <summary>Gets the chosen subfaction name, when one is selected.</summary>
    public string? Subfaction { get; init; }

    /// <summary>Gets the chosen faction identifier, when selected.</summary>
    public Guid? FactionId { get; init; }

    /// <summary>Gets the chosen faction color, when selected.</summary>
    public string? FactionColor { get; init; }

    /// <summary>Gets whether the chosen faction has an uploaded flag image.</summary>
    public bool HasFlagImage { get; init; }

    /// <summary>Gets the ally-group name for the chosen faction, when one applies.</summary>
    public string? AllyGroupName { get; init; }
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
/// A compose target for campaign chat.
/// </summary>
public sealed class ChatChannelDetail
{
    /// <summary>Gets Public, Direct, Faction, or AllyGroup.</summary>
    public required string Kind { get; init; }

    /// <summary>Gets the member, faction, or ally-group identifier for a private channel.</summary>
    public Guid? TargetId { get; init; }

    /// <summary>Gets the label shown in the channel list.</summary>
    public required string Label { get; init; }
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

    /// <summary>Gets special-rule identifiers assigned to this faction.</summary>
    public IReadOnlyList<Guid> SpecialRuleIds { get; init; } = [];
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

    /// <summary>Gets the unique overlay color as #RRGGBB.</summary>
    public string Color { get; init; } = "#4B5563";
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

    /// <summary>Gets the item objective types. Empty means the campaign has none.</summary>
    public IReadOnlyList<StoredItemObjectiveType> ItemObjectiveTypes { get; init; } = [];

    /// <summary>Gets the public campaign objectives. Empty means none.</summary>
    public IReadOnlyList<StoredPublicObjectiveType> PublicObjectiveTypes { get; init; } = [];

    /// <summary>Gets reusable special rules. Empty means none.</summary>
    public IReadOnlyList<StoredSpecialRule> SpecialRules { get; init; } = [];

    /// <summary>Gets the private campaign objectives. Empty means none.</summary>
    public IReadOnlyList<StoredPrivateObjectiveType> PrivateObjectiveTypes { get; init; } = [];

    /// <summary>Gets conversion from resolved battles into campaign points.</summary>
    public Campaign.Domain.Campaigns.BattleScoringSetup BattleScoring { get; init; } =
        Campaign.Domain.Campaigns.BattleScoringSetup.Default;

    /// <summary>Gets campaign points for the built-in ranking public objectives.</summary>
    public Campaign.Domain.Campaigns.GeneralPublicObjectivePoints RankingObjectivePoints { get; init; } =
        Campaign.Domain.Campaigns.GeneralPublicObjectivePoints.None;

    /// <summary>Gets campaign points awarded to the winner when differential scoring is off.</summary>
    public int PointsPerBattleWon => BattleScoring.PointsPerWin;
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

    /// <summary>Gets special-rule identifiers assigned to this faction.</summary>
    public IReadOnlyList<Guid> SpecialRuleIds { get; init; } = [];
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

    /// <summary>Gets the unique overlay color as #RRGGBB.</summary>
    public string Color { get; init; } = "#4B5563";
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

    /// <summary>Gets campaign points awarded for currently owning a territory of this terrain.</summary>
    public int CampaignPoints { get; init; }

    /// <summary>Gets whether this terrain is a water feature.</summary>
    public bool IsWaterFeature { get; init; }
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

    /// <summary>Gets whether players may Build this structure.</summary>
    public required bool IsBuildable { get; init; }

    /// <summary>Gets whether players may Pillage this structure.</summary>
    public required bool IsPillageable { get; init; }

    /// <summary>Gets whether a second Pillage may destroy and remove this structure.</summary>
    public required bool IsDestructible { get; init; }

    /// <summary>Gets the missions.</summary>
    public required IReadOnlyList<MissionDetail> Missions { get; init; }

    /// <summary>Gets campaign points awarded for currently controlling this structure when it is not destroyed.</summary>
    public int CampaignPoints { get; init; }
}

/// <summary>
/// An item objective type in a campaign detail response.
/// </summary>
public sealed class ItemObjectiveTypeDetail
{
    /// <summary>Gets the type identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the item name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets whether the item stays hidden until found or staff-revealed.</summary>
    public required bool IsHiddenUntilFound { get; init; }

    /// <summary>Gets Random or Placed.</summary>
    public required string Placement { get; init; }

    /// <summary>Gets whether the item may occupy a spawn territory.</summary>
    public required bool AllowOnSpawn { get; init; }

    /// <summary>Gets the built-in logo key when no custom image is stored.</summary>
    public string BuiltinSymbol { get; init; } = "Crown";

    /// <summary>Gets the logo color as #RRGGBB.</summary>
    public string Color { get; init; } = "#C45C26";

    /// <summary>Gets whether a custom logo image is stored.</summary>
    public bool HasImage { get; init; }

    /// <summary>Gets campaign points awarded while a force currently holds this item.</summary>
    public int CampaignPoints { get; init; }

    /// <summary>Gets optional flavor or lore text shown to the holder.</summary>
    public string? FlavorText { get; init; }

    /// <summary>Gets holder choices configured for this item.</summary>
    public IReadOnlyList<ItemObjectiveChoiceDetail> Choices { get; init; } = [];

    /// <summary>Gets special-rule identifiers assigned to this item.</summary>
    public IReadOnlyList<Guid> SpecialRuleIds { get; init; } = [];
}

/// <summary>
/// A holder choice on an item objective.
/// </summary>
public sealed class ItemObjectiveChoiceDetail
{
    /// <summary>Gets the choice identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the choice name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets configured results. Result effects are omitted from unauthorized views.</summary>
    public IReadOnlyList<ItemObjectiveChoiceResultDetail> Results { get; init; } = [];
}

/// <summary>
/// One possible outcome of an item-objective choice.
/// </summary>
public sealed class ItemObjectiveChoiceResultDetail
{
    /// <summary>Gets the result identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets replacement flavor text after the choice.</summary>
    public string? FlavorText { get; init; }

    /// <summary>Gets an optional state label after the choice.</summary>
    public string? NewStateKey { get; init; }

    /// <summary>Gets whether the item is destroyed.</summary>
    public bool DestroyItem { get; init; }

    /// <summary>Gets a replacement item-objective catalog type.</summary>
    public Guid? ReplacementItemTypeId { get; init; }

    /// <summary>Gets a private-objective catalog type granted to the possessing player.</summary>
    public Guid? GrantedPrivateObjectiveTypeId { get; init; }
}

/// <summary>
/// A reusable special rule.
/// </summary>
public sealed class SpecialRuleDetail
{
    /// <summary>Gets the rule identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the unique rule name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the player-facing rule text.</summary>
    public required string Text { get; init; }
}

/// <summary>
/// A private-objective catalog entry. Secret description is omitted unless the viewer may see it.
/// </summary>
public sealed class PrivateObjectiveTypeDetail
{
    /// <summary>Gets the catalog identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the objective name when the viewer may see it.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the secret description when the viewer may see it.</summary>
    public string? Description { get; init; }

    /// <summary>Gets campaign points when the viewer may see them.</summary>
    public int? CampaignPoints { get; init; }

    /// <summary>Gets holder kinds this entry may be assigned to.</summary>
    public IReadOnlyList<string> AllowedHolderKinds { get; init; } = [];

    /// <summary>Gets Manual or Automatic.</summary>
    public required string ScoringKind { get; init; }

    /// <summary>Gets the automatic criterion kind.</summary>
    public string? AutomaticKind { get; init; }

    /// <summary>Gets how many matching facts complete an automatic objective.</summary>
    public int RequiredCount { get; init; } = 1;

    /// <summary>Gets the structure type for structure-based automatic criteria.</summary>
    public Guid? StructureTypeId { get; init; }

    /// <summary>Gets named territories when the viewer may see them.</summary>
    public IReadOnlyList<Guid> TerritoryIds { get; init; } = [];
}

/// <summary>
/// One assigned private objective visible to the current viewer.
/// </summary>
public sealed class PrivateObjectiveAssignmentDetail
{
    /// <summary>Gets the assignment identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the catalog type.</summary>
    public required Guid TypeId { get; init; }

    /// <summary>Gets Player, Faction, or AllyGroup.</summary>
    public required string HolderKind { get; init; }

    /// <summary>Gets the player, faction, or ally-group identifier.</summary>
    public required Guid HolderId { get; init; }

    /// <summary>Gets Assigned, Claimed, or Revealed.</summary>
    public required string Status { get; init; }

    /// <summary>Gets Manual or Automatic.</summary>
    public required string ScoringKind { get; init; }

    /// <summary>Gets the objective name when the viewer may see it.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the secret description when the viewer may see it.</summary>
    public string? Description { get; init; }

    /// <summary>Gets campaign points when the viewer may see them.</summary>
    public int? CampaignPoints { get; init; }

    /// <summary>Gets whether the viewer may claim this assignment.</summary>
    public bool CanClaim { get; init; }

    /// <summary>Gets whether the viewer may approve or deny a claim.</summary>
    public bool CanModerate { get; init; }
}

/// <summary>
/// Public count of still-unclaimed private objectives for one holder.
/// </summary>
public sealed class PrivateObjectiveUnclaimedCountDetail
{
    /// <summary>Gets Player, Faction, or AllyGroup.</summary>
    public required string HolderKind { get; init; }

    /// <summary>Gets the player, faction, or ally-group identifier.</summary>
    public required Guid HolderId { get; init; }

    /// <summary>Gets a public display name for the holder.</summary>
    public required string HolderName { get; init; }

    /// <summary>Gets how many assigned private objectives are still unclaimed.</summary>
    public required int Count { get; init; }
}

/// <summary>
/// A public campaign objective in a campaign detail response.
/// </summary>
public sealed class PublicObjectiveTypeDetail
{
    /// <summary>Gets the objective identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the objective name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets campaign points awarded when this objective is completed.</summary>
    public required int CampaignPoints { get; init; }
}

/// <summary>
/// One player's current campaign-point standing.
/// </summary>
public sealed class CampaignPointStandingDetail
{
    /// <summary>Gets the player.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets the unique username.</summary>
    public required string Username { get; init; }

    /// <summary>Gets the name shown to other users.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Gets the chosen faction identifier, when selected.</summary>
    public Guid? FactionId { get; init; }

    /// <summary>Gets the chosen faction name, when selected.</summary>
    public string? FactionName { get; init; }

    /// <summary>Gets the chosen faction color, when selected.</summary>
    public string? FactionColor { get; init; }

    /// <summary>Gets whether the faction has an uploaded flag image.</summary>
    public bool HasFlagImage { get; init; }

    /// <summary>Gets the ally-group name, when the faction is aligned.</summary>
    public string? AllyGroupName { get; init; }

    /// <summary>Gets points from currently owned non-destroyed structures.</summary>
    public required int TerritoryAndStructurePoints { get; init; }

    /// <summary>Gets points from resolved battles, including draws and differentials.</summary>
    public required int BattlesWonPoints { get; init; }

    /// <summary>Gets points from ranking objectives and currently active named awards.</summary>
    public required int PublicObjectivePoints { get; init; }

    /// <summary>Gets points from revealed or completed private objectives that apply to this player.</summary>
    public int PrivateObjectivePoints { get; init; }

    /// <summary>Gets points from currently held visible item objectives.</summary>
    public required int OtherPoints { get; init; }

    /// <summary>Gets the sum of the five component columns.</summary>
    public required int Total { get; init; }

    /// <summary>Gets visible item objectives the player currently holds.</summary>
    public IReadOnlyList<HeldItemObjectiveDetail> HeldItems { get; init; } = [];
}

/// <summary>
/// A visible item objective currently held by a player.
/// </summary>
public sealed class HeldItemObjectiveDetail
{
    /// <summary>Gets the catalog type.</summary>
    public required Guid TypeId { get; init; }

    /// <summary>Gets the item name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the built-in logo key when no custom image is stored.</summary>
    public string? BuiltinSymbol { get; init; }

    /// <summary>Gets the logo color.</summary>
    public string Color { get; init; } = "#C45C26";

    /// <summary>Gets whether a custom logo image is stored.</summary>
    public bool HasImage { get; init; }
}

/// <summary>
/// Current leaders for one ranking public objective.
/// </summary>
public sealed class PublicObjectiveLeaderboardDetail
{
    /// <summary>Gets the ranking objective kind.</summary>
    public required string Kind { get; init; }

    /// <summary>Gets campaign points awarded to each current first-place player.</summary>
    public required int AwardPoints { get; init; }

    /// <summary>Gets players currently in the top five.</summary>
    public required IReadOnlyList<PublicObjectiveLeaderDetail> Leaders { get; init; }
}

/// <summary>
/// One player on a ranking public-objective leaderboard.
/// </summary>
public sealed class PublicObjectiveLeaderDetail
{
    /// <summary>Gets the player.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets the unique username.</summary>
    public required string Username { get; init; }

    /// <summary>Gets the name shown to other users.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Gets the 1-based rank after friendly ties.</summary>
    public required int Rank { get; init; }

    /// <summary>Gets the primary metric (territories, chain length, or wins).</summary>
    public required int Metric { get; init; }

    /// <summary>Gets the secondary metric used only for most battles won (draws).</summary>
    public required int TieBreakMetric { get; init; }

    /// <summary>Gets whether this player currently receives the objective's campaign points.</summary>
    public required bool AwardsPoints { get; init; }
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

    /// <summary>Gets campaign points awarded for currently owning a territory of this terrain.</summary>
    public int CampaignPoints { get; init; }

    /// <summary>Gets whether this terrain is a water feature.</summary>
    public bool IsWaterFeature { get; init; }
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

    /// <summary>Gets whether players may Build this structure.</summary>
    public required bool IsBuildable { get; init; }

    /// <summary>Gets whether players may Pillage this structure.</summary>
    public required bool IsPillageable { get; init; }

    /// <summary>Gets whether a second Pillage may destroy and remove this structure.</summary>
    public required bool IsDestructible { get; init; }

    /// <summary>Gets the missions.</summary>
    public required IReadOnlyList<StoredMission> Missions { get; init; }

    /// <summary>Gets campaign points awarded for currently controlling this structure when it is not destroyed.</summary>
    public int CampaignPoints { get; init; }
}

/// <summary>
/// A persisted item objective type.
/// </summary>
public sealed class StoredItemObjectiveType
{
    /// <summary>Gets the type identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the item name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets whether the item stays hidden until found or staff-revealed.</summary>
    public required bool IsHiddenUntilFound { get; init; }

    /// <summary>Gets Random or Placed.</summary>
    public required string Placement { get; init; }

    /// <summary>Gets whether the item may occupy a spawn territory.</summary>
    public required bool AllowOnSpawn { get; init; }

    /// <summary>Gets the built-in logo key.</summary>
    public string BuiltinSymbol { get; init; } = "Crown";

    /// <summary>Gets the logo color as #RRGGBB.</summary>
    public string Color { get; init; } = "#C45C26";

    /// <summary>Gets the stored logo key, when a custom image was uploaded.</summary>
    public string? ImageStorageKey { get; init; }

    /// <summary>Gets campaign points awarded while a force currently holds this item.</summary>
    public int CampaignPoints { get; init; }

    /// <summary>Gets optional flavor or lore text shown to the holder.</summary>
    public string? FlavorText { get; init; }

    /// <summary>Gets holder choices configured for this item.</summary>
    public IReadOnlyList<StoredItemObjectiveChoice> Choices { get; init; } = [];

    /// <summary>Gets special-rule identifiers assigned to this item.</summary>
    public IReadOnlyList<Guid> SpecialRuleIds { get; init; } = [];
}

/// <summary>
/// A persisted holder choice on an item objective.
/// </summary>
public sealed class StoredItemObjectiveChoice
{
    /// <summary>Gets the choice identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the choice name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the configured results.</summary>
    public required IReadOnlyList<StoredItemObjectiveChoiceResult> Results { get; init; }
}

/// <summary>
/// A persisted outcome of an item-objective choice.
/// </summary>
public sealed class StoredItemObjectiveChoiceResult
{
    /// <summary>Gets the result identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets replacement flavor text after the choice.</summary>
    public string? FlavorText { get; init; }

    /// <summary>Gets an optional state label after the choice.</summary>
    public string? NewStateKey { get; init; }

    /// <summary>Gets whether the item is destroyed and removed from the map.</summary>
    public bool DestroyItem { get; init; }

    /// <summary>Gets a replacement item-objective catalog type.</summary>
    public Guid? ReplacementItemTypeId { get; init; }

    /// <summary>Gets a private-objective catalog type granted to the possessing player.</summary>
    public Guid? GrantedPrivateObjectiveTypeId { get; init; }
}

/// <summary>
/// A persisted reusable special rule.
/// </summary>
public sealed class StoredSpecialRule
{
    /// <summary>Gets the rule identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the unique rule name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the player-facing rule text.</summary>
    public required string Text { get; init; }
}

/// <summary>
/// A persisted private campaign objective.
/// </summary>
public sealed class StoredPrivateObjectiveType
{
    /// <summary>Gets the catalog identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the objective name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the optional secret description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets campaign points awarded when revealed or completed.</summary>
    public required int CampaignPoints { get; init; }

    /// <summary>Gets holder kinds this entry may be assigned to.</summary>
    public required IReadOnlyList<string> AllowedHolderKinds { get; init; }

    /// <summary>Gets Manual or Automatic.</summary>
    public required string ScoringKind { get; init; }

    /// <summary>Gets the automatic criterion kind.</summary>
    public required string AutomaticKind { get; init; }

    /// <summary>Gets how many matching facts complete an automatic objective.</summary>
    public int RequiredCount { get; init; } = 1;

    /// <summary>Gets the structure type for structure-based automatic criteria.</summary>
    public Guid? StructureTypeId { get; init; }

    /// <summary>Gets named territories for ControlNamedTerritories.</summary>
    public IReadOnlyList<Guid> TerritoryIds { get; init; } = [];
}

/// <summary>
/// A persisted public campaign objective.
/// </summary>
public sealed class StoredPublicObjectiveType
{
    /// <summary>Gets the objective identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the objective name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets campaign points awarded when this objective is completed.</summary>
    public required int CampaignPoints { get; init; }
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
