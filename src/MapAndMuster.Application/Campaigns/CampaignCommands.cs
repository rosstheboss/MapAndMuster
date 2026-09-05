using MapAndMuster.Domain.Campaigns;

namespace MapAndMuster.Application.Campaigns;

/// <summary>
/// Command to create a campaign and make the caller its manager.
/// </summary>
public sealed class CreateCampaignCommand
{
    /// <summary>Gets the authenticated user creating the campaign.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets the campaign name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the configured player-slot count.</summary>
    public required int PlayerCount { get; init; }

    /// <summary>Gets whether a join password is required.</summary>
    public required bool IsPrivate { get; init; }

    /// <summary>Gets whether non-members may view the campaign.</summary>
    public required bool IsPubliclyViewable { get; init; }

    /// <summary>Gets the join password for a private campaign.</summary>
    public string? JoinPassword { get; init; }

    /// <summary>Gets whether the creator also occupies a player slot.</summary>
    public required bool CreatorIsParticipant { get; init; }

    /// <summary>Gets the optional city.</summary>
    public string? City { get; init; }

    /// <summary>Gets the optional state, province, or region.</summary>
    public string? Region { get; init; }

    /// <summary>Gets the optional country.</summary>
    public string? Country { get; init; }

    /// <summary>Gets the factions.</summary>
    public required IReadOnlyList<FactionInput> Factions { get; init; }

    /// <summary>Gets the ally groups.</summary>
    public IReadOnlyList<AllyGroupInput>? AllyGroups { get; init; }

    /// <summary>Gets the external links.</summary>
    public IReadOnlyList<CampaignLinkInput>? Links { get; init; }

    /// <summary>Gets the round schedule.</summary>
    public required CampaignScheduleInput Schedule { get; init; }

    /// <summary>Gets the terrain types. Defaults are used when omitted.</summary>
    public IReadOnlyList<TerrainTypeInput>? TerrainTypes { get; init; }

    /// <summary>Gets the structure types. Defaults are used when omitted.</summary>
    public IReadOnlyList<StructureTypeInput>? StructureTypes { get; init; }

    /// <summary>Gets the item objective types. Omitted or empty means none.</summary>
    public IReadOnlyList<ItemObjectiveTypeInput>? ItemObjectiveTypes { get; init; }

    /// <summary>Gets the public campaign objectives. Omitted or empty means none.</summary>
    public IReadOnlyList<PublicObjectiveTypeInput>? PublicObjectiveTypes { get; init; }

    /// <summary>Gets campaign points awarded to the winner when differential scoring is off.</summary>
    public int? PointsPerBattleWon { get; init; }

    /// <summary>Gets campaign points awarded to each participant of a draw.</summary>
    public int? PointsPerBattleDraw { get; init; }

    /// <summary>Gets whether battle campaign points use score differential.</summary>
    public bool? UseDifferentialBattleScoring { get; init; }

    /// <summary>Gets the multiplier applied to the winner-minus-loser score difference.</summary>
    public decimal? DifferentialMultiplier { get; init; }

    /// <summary>Gets the inclusive lower clamp for differential campaign points.</summary>
    public int? DifferentialMinimum { get; init; }

    /// <summary>Gets the inclusive upper clamp for differential campaign points.</summary>
    public int? DifferentialMaximum { get; init; }

    /// <summary>Gets whether the loser can receive negative campaign points.</summary>
    public bool? AllowNegativeDifferential { get; init; }

    /// <summary>Gets campaign points for most territories currently controlled.</summary>
    public int? MostTerritoriesCampaignPoints { get; init; }

    /// <summary>Gets campaign points for the longest owned territory chain.</summary>
    public int? LongestTerritoryChainCampaignPoints { get; init; }

    /// <summary>Gets campaign points for most battle wins.</summary>
    public int? MostBattlesWonCampaignPoints { get; init; }

    /// <summary>Gets campaign points for most structure campaign points.</summary>
    public int? MostStructurePointsCampaignPoints { get; init; }

    /// <summary>Gets campaign points awarded for each currently owned territory.</summary>
    public int? PointsPerTerritoryCampaignPoints { get; init; }

    /// <summary>Gets campaign points for each revealed relic held by an ally or faction-mate other than the player.</summary>
    public int? AlliedRelicControlCampaignPoints { get; init; }

    /// <summary>Gets reusable special rules. Omitted or empty means none.</summary>
    public IReadOnlyList<SpecialRuleInput>? SpecialRules { get; init; }

    /// <summary>Gets configured force statuses other than Normal. Omitted or empty means none.</summary>
    public IReadOnlyList<ForceStatusInput>? ForceStatuses { get; init; }

    /// <summary>Gets private campaign objectives. Omitted or empty means none.</summary>
    public IReadOnlyList<PrivateObjectiveTypeInput>? PrivateObjectiveTypes { get; init; }

    /// <summary>Gets the amount subtracted from map supply when a player has split forces.</summary>
    public int? SplitForceSupplyPenaltyPercent { get; init; }

    /// <summary>Gets whether the split-force supply penalty is a percent of map supply.</summary>
    public bool? SplitForceSupplyPenaltyIsPercent { get; init; }

    /// <summary>Gets reusable battle-result questions. Omitted or empty means none.</summary>
    public IReadOnlyList<StandardBattleResultQuestionInput>? StandardBattleResultQuestions { get; init; }

    /// <summary>Gets reusable missions. Omitted means nested terrain and structure missions only.</summary>
    public IReadOnlyList<MissionInput>? Missions { get; init; }
}

/// <summary>
/// Command to replace campaign setup fields. The join password may be omitted to keep the current hash.
/// </summary>
public sealed class UpdateCampaignCommand
{
    /// <summary>Gets the authenticated user.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets the campaign identifier.</summary>
    public required Guid CampaignId { get; init; }

    /// <summary>Gets the last observed campaign revision.</summary>
    public required int ExpectedRevision { get; init; }

    /// <summary>Gets the campaign name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the configured player-slot count.</summary>
    public required int PlayerCount { get; init; }

    /// <summary>Gets whether a join password is required.</summary>
    public required bool IsPrivate { get; init; }

    /// <summary>Gets whether non-members may view the campaign.</summary>
    public required bool IsPubliclyViewable { get; init; }

    /// <summary>Gets the replacement join password, or null to keep the current hash.</summary>
    public string? JoinPassword { get; init; }

    /// <summary>Gets whether the creator also occupies a player slot.</summary>
    public required bool CreatorIsParticipant { get; init; }

    /// <summary>Gets the optional city.</summary>
    public string? City { get; init; }

    /// <summary>Gets the optional state, province, or region.</summary>
    public string? Region { get; init; }

    /// <summary>Gets the optional country.</summary>
    public string? Country { get; init; }

    /// <summary>Gets the factions.</summary>
    public required IReadOnlyList<FactionInput> Factions { get; init; }

    /// <summary>Gets the ally groups.</summary>
    public IReadOnlyList<AllyGroupInput>? AllyGroups { get; init; }

    /// <summary>Gets the external links.</summary>
    public IReadOnlyList<CampaignLinkInput>? Links { get; init; }

    /// <summary>Gets the round schedule.</summary>
    public required CampaignScheduleInput Schedule { get; init; }

    /// <summary>Gets the terrain types. Defaults are used when omitted.</summary>
    public IReadOnlyList<TerrainTypeInput>? TerrainTypes { get; init; }

    /// <summary>Gets the structure types. Defaults are used when omitted.</summary>
    public IReadOnlyList<StructureTypeInput>? StructureTypes { get; init; }

    /// <summary>Gets the item objective types. Omitted or empty means none.</summary>
    public IReadOnlyList<ItemObjectiveTypeInput>? ItemObjectiveTypes { get; init; }

    /// <summary>Gets the public campaign objectives. Omitted or empty means none.</summary>
    public IReadOnlyList<PublicObjectiveTypeInput>? PublicObjectiveTypes { get; init; }

    /// <summary>Gets campaign points awarded to the winner when differential scoring is off.</summary>
    public int? PointsPerBattleWon { get; init; }

    /// <summary>Gets campaign points awarded to each participant of a draw.</summary>
    public int? PointsPerBattleDraw { get; init; }

    /// <summary>Gets whether battle campaign points use score differential.</summary>
    public bool? UseDifferentialBattleScoring { get; init; }

    /// <summary>Gets the multiplier applied to the winner-minus-loser score difference.</summary>
    public decimal? DifferentialMultiplier { get; init; }

    /// <summary>Gets the inclusive lower clamp for differential campaign points.</summary>
    public int? DifferentialMinimum { get; init; }

    /// <summary>Gets the inclusive upper clamp for differential campaign points.</summary>
    public int? DifferentialMaximum { get; init; }

    /// <summary>Gets whether the loser can receive negative campaign points.</summary>
    public bool? AllowNegativeDifferential { get; init; }

    /// <summary>Gets campaign points for most territories currently controlled.</summary>
    public int? MostTerritoriesCampaignPoints { get; init; }

    /// <summary>Gets campaign points for the longest owned territory chain.</summary>
    public int? LongestTerritoryChainCampaignPoints { get; init; }

    /// <summary>Gets campaign points for most battle wins.</summary>
    public int? MostBattlesWonCampaignPoints { get; init; }

    /// <summary>Gets campaign points for most structure campaign points.</summary>
    public int? MostStructurePointsCampaignPoints { get; init; }

    /// <summary>Gets campaign points awarded for each currently owned territory.</summary>
    public int? PointsPerTerritoryCampaignPoints { get; init; }

    /// <summary>Gets campaign points for each revealed relic held by an ally or faction-mate other than the player.</summary>
    public int? AlliedRelicControlCampaignPoints { get; init; }

    /// <summary>Gets reusable special rules. Omitted or empty means none.</summary>
    public IReadOnlyList<SpecialRuleInput>? SpecialRules { get; init; }

    /// <summary>Gets configured force statuses other than Normal. Omitted or empty means none.</summary>
    public IReadOnlyList<ForceStatusInput>? ForceStatuses { get; init; }

    /// <summary>Gets private campaign objectives. Omitted or empty means none.</summary>
    public IReadOnlyList<PrivateObjectiveTypeInput>? PrivateObjectiveTypes { get; init; }

    /// <summary>Gets the amount subtracted from map supply when a player has split forces.</summary>
    public int? SplitForceSupplyPenaltyPercent { get; init; }

    /// <summary>Gets whether the split-force supply penalty is a percent of map supply.</summary>
    public bool? SplitForceSupplyPenaltyIsPercent { get; init; }

    /// <summary>Gets reusable battle-result questions. Omitted or empty means none.</summary>
    public IReadOnlyList<StandardBattleResultQuestionInput>? StandardBattleResultQuestions { get; init; }

    /// <summary>Gets reusable missions. Omitted means nested terrain and structure missions only.</summary>
    public IReadOnlyList<MissionInput>? Missions { get; init; }
}

/// <summary>
/// Command to replace a campaign map image.
/// </summary>
public sealed class UploadCampaignMapCommand
{
    /// <summary>Gets the authenticated user.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets the campaign identifier.</summary>
    public required Guid CampaignId { get; init; }

    /// <summary>Gets the last observed campaign revision.</summary>
    public required int ExpectedRevision { get; init; }

    /// <summary>Gets the uploaded map stream.</summary>
    public required Stream Content { get; init; }

    /// <summary>Gets the declared content type.</summary>
    public required string ContentType { get; init; }

    /// <summary>Gets the declared length, if known.</summary>
    public long? Length { get; init; }
}

/// <summary>
/// Command to join a campaign as a player.
/// </summary>
public sealed class JoinCampaignCommand
{
    /// <summary>Gets the authenticated user.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets whether the caller is a system administrator.</summary>
    public required bool IsAdministrator { get; init; }

    /// <summary>Gets the campaign identifier.</summary>
    public required Guid CampaignId { get; init; }

    /// <summary>Gets the join password for a private campaign.</summary>
    public string? JoinPassword { get; init; }
}

/// <summary>
/// Command to post a public chat message in a campaign log.
/// </summary>
public sealed class PostCampaignChatCommand
{
    /// <summary>Gets the authenticated user.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets whether the caller is a system administrator.</summary>
    public required bool IsAdministrator { get; init; }

    /// <summary>Gets the campaign identifier.</summary>
    public required Guid CampaignId { get; init; }

    /// <summary>Gets the last observed campaign revision.</summary>
    public required int ExpectedRevision { get; init; }

    /// <summary>Gets the chat message.</summary>
    public required string Message { get; init; }

    /// <summary>Gets Public, Direct, Faction, or AllyGroup.</summary>
    public string ChannelKind { get; init; } = "Public";

    /// <summary>Gets the member, faction, or ally-group identifier for a private channel.</summary>
    public Guid? TargetId { get; init; }
}

/// <summary>
/// Command to leave a campaign the caller plays in but does not manage.
/// </summary>
public sealed class LeaveCampaignCommand
{
    /// <summary>Gets the authenticated user.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets the campaign identifier.</summary>
    public required Guid CampaignId { get; init; }
}

/// <summary>
/// Command for a manager or administrator to search accounts to add to a campaign.
/// </summary>
public sealed class SearchCampaignUsersCommand
{
    /// <summary>Gets the authenticated staff user.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets whether the caller is a system administrator.</summary>
    public required bool IsAdministrator { get; init; }

    /// <summary>Gets the campaign identifier.</summary>
    public required Guid CampaignId { get; init; }

    /// <summary>Gets the search text.</summary>
    public required string Query { get; init; }
}

/// <summary>
/// Command for a manager or administrator to add a player without a join password.
/// </summary>
public sealed class AddCampaignMemberCommand
{
    /// <summary>Gets the authenticated staff user.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets whether the caller is a system administrator.</summary>
    public required bool IsAdministrator { get; init; }

    /// <summary>Gets the campaign identifier.</summary>
    public required Guid CampaignId { get; init; }

    /// <summary>Gets the account to add or promote.</summary>
    public required Guid TargetUserId { get; init; }

    /// <summary>Gets whether the target should be a campaign manager.</summary>
    public bool IsGameMaster { get; init; }

    /// <summary>Gets whether the target occupies a player slot. Defaults to a player-only add.</summary>
    public bool IsPlayer { get; init; } = true;

    /// <summary>Gets the last observed campaign revision.</summary>
    public required int ExpectedRevision { get; init; }
}

/// <summary>
/// Command for a manager or administrator to remove a player.
/// </summary>
public sealed class KickCampaignMemberCommand
{
    /// <summary>Gets the authenticated staff user.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets whether the caller is a system administrator.</summary>
    public required bool IsAdministrator { get; init; }

    /// <summary>Gets the campaign identifier.</summary>
    public required Guid CampaignId { get; init; }

    /// <summary>Gets the player to remove.</summary>
    public required Guid TargetUserId { get; init; }

    /// <summary>Gets the last observed campaign revision.</summary>
    public required int ExpectedRevision { get; init; }
}

/// <summary>
/// Command for a manager or administrator to assign another player's faction.
/// </summary>
public sealed class AssignPlayerFactionCommand
{
    /// <summary>Gets the authenticated staff user.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets whether the caller is a system administrator.</summary>
    public required bool IsAdministrator { get; init; }

    /// <summary>Gets the campaign identifier.</summary>
    public required Guid CampaignId { get; init; }

    /// <summary>Gets the player whose faction is assigned.</summary>
    public required Guid TargetUserId { get; init; }

    /// <summary>Gets the faction identifier.</summary>
    public required Guid FactionId { get; init; }

    /// <summary>Gets the optional subfaction name.</summary>
    public string? Subfaction { get; init; }

    /// <summary>Gets the last observed campaign revision.</summary>
    public required int ExpectedRevision { get; init; }
}

/// <summary>
/// Command to copy a campaign's setup, map overlay, and catalog while sharing stored art files.
/// </summary>
public sealed class DuplicateCampaignCommand
{
    /// <summary>Gets the authenticated user creating the copy.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets the source campaign identifier.</summary>
    public required Guid CampaignId { get; init; }
}

/// <summary>
/// Command for a manager or administrator to close a campaign while keeping its final state.
/// </summary>
public sealed class EndCampaignCommand
{
    /// <summary>Gets the authenticated staff user.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets whether the caller is a system administrator.</summary>
    public required bool IsAdministrator { get; init; }

    /// <summary>Gets the campaign identifier.</summary>
    public required Guid CampaignId { get; init; }

    /// <summary>Gets the last observed campaign revision, when the caller supplied one.</summary>
    public int? ExpectedRevision { get; init; }
}
