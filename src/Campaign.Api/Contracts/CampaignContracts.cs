using Campaign.Application.Campaigns;
using Campaign.Application.Maps;
using Campaign.Domain.Campaigns;
using Campaign.Domain.Maps;

namespace Campaign.Api.Contracts;

/// <summary>
/// Request to create or update campaign setup. Join passwords are never returned.
/// </summary>
public sealed class SaveCampaignRequest
{
    /// <summary>Gets the campaign name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the configured player-slot count.</summary>
    public required int PlayerCount { get; init; }

    /// <summary>Gets whether a join password is required.</summary>
    public required bool IsPrivate { get; init; }

    /// <summary>Gets whether non-members may view the campaign. Defaults to true.</summary>
    public bool IsPubliclyViewable { get; init; } = true;

    /// <summary>Gets the join password. Omit on update to keep the current password.</summary>
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
    public required IReadOnlyList<FactionRequest> Factions { get; init; }

    /// <summary>Gets the ally groups.</summary>
    public IReadOnlyList<AllyGroupRequest>? AllyGroups { get; init; }

    /// <summary>Gets the external links.</summary>
    public IReadOnlyList<LinkRequest>? Links { get; init; }

    /// <summary>Gets the last observed campaign revision. Required for updates.</summary>
    public int? Revision { get; init; }

    /// <summary>Gets the IANA time zone used to interpret the start wall-clock time. Defaults to UTC.</summary>
    public string? TimeZoneId { get; init; }

    /// <summary>Gets the start date and time in the campaign time zone, without an offset.</summary>
    public string? StartsAtLocal { get; init; }

    /// <summary>Gets the number of rounds.</summary>
    public int RoundCount { get; init; }

    /// <summary>Gets the round-length amount.</summary>
    public int RoundLengthAmount { get; init; }

    /// <summary>Gets the round-length unit name.</summary>
    public string? RoundLengthUnit { get; init; }

    /// <summary>Gets the ordered action and battle steps that make up one round.</summary>
    public IReadOnlyList<RoundPhaseRequest>? Phases { get; init; }

    /// <summary>Gets the terrain types. Defaults are used when omitted.</summary>
    public IReadOnlyList<TerrainTypeRequest>? TerrainTypes { get; init; }

    /// <summary>Gets the structure types. Defaults are used when omitted.</summary>
    public IReadOnlyList<StructureTypeRequest>? StructureTypes { get; init; }

    /// <summary>Gets the item objective types. Omitted or empty means none.</summary>
    public IReadOnlyList<ItemObjectiveTypeRequest>? ItemObjectiveTypes { get; init; }

    /// <summary>Gets the public campaign objectives. Omitted or empty means none.</summary>
    public IReadOnlyList<PublicObjectiveTypeRequest>? PublicObjectiveTypes { get; init; }

    /// <summary>Gets reusable special rules. Omitted or empty means none.</summary>
    public IReadOnlyList<SpecialRuleRequest>? SpecialRules { get; init; }

    /// <summary>Gets reusable missions. Omitted means nested terrain and structure missions only.</summary>
    public IReadOnlyList<MissionRequest>? Missions { get; init; }

    /// <summary>Gets configured force statuses other than Normal. Omitted or empty means none.</summary>
    public IReadOnlyList<ForceStatusRequest>? ForceStatuses { get; init; }

    /// <summary>Gets private campaign objectives. Omitted or empty means none.</summary>
    public IReadOnlyList<PrivateObjectiveTypeRequest>? PrivateObjectiveTypes { get; init; }

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

    /// <summary>Gets the percent subtracted from map-plus-round supply when a player has split forces.</summary>
    public int? SplitForceSupplyPenaltyPercent { get; init; }

    /// <summary>Gets whether every battle report asks if the enemy general was slain.</summary>
    public bool? AlwaysAskGeneralKill { get; init; }

    /// <summary>Gets whether every battle report asks if the enemy supply line was destroyed.</summary>
    public bool? AlwaysAskSupplyLineDestroyed { get; init; }

    /// <summary>Gets campaign points awarded for a slain enemy general.</summary>
    public int? GeneralKillCampaignPoints { get; init; }

    /// <summary>Gets campaign points awarded for destroying the enemy supply line.</summary>
    public int? SupplyLineDestroyedCampaignPoints { get; init; }

    /// <summary>Gets per-round army size, free supply, and free characters.</summary>
    public IReadOnlyList<RoundArmyEscalationRequest>? RoundEscalations { get; init; }
}

/// <summary>
/// An action or battle step in a save request.
/// </summary>
public sealed class RoundPhaseRequest
{
    /// <summary>Gets the phase kind name.</summary>
    public required string Kind { get; init; }

    /// <summary>Gets the duration amount.</summary>
    public required int DurationAmount { get; init; }

    /// <summary>Gets the duration unit name.</summary>
    public required string DurationUnit { get; init; }
}

/// <summary>
/// Per-round army size and free allowances in a save request.
/// </summary>
public sealed class RoundArmyEscalationRequest
{
    /// <summary>Gets the 1-based round.</summary>
    public int RoundNumber { get; init; }

    /// <summary>Gets the maximum army points size for the round.</summary>
    public int? MaxArmyPoints { get; init; }

    /// <summary>Gets free supply points granted this round.</summary>
    public int? FreeSupplyPoints { get; init; }

    /// <summary>Gets how many characters have a free base cost against supply.</summary>
    public int? FreeCharacterCount { get; init; }
}

/// <summary>
/// Faction configuration in a save request.
/// </summary>
public sealed class FactionRequest
{
    /// <summary>Gets the faction name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets optional subfaction names.</summary>
    public IReadOnlyList<string>? Subfactions { get; init; }

    /// <summary>Gets the optional ally-group name this faction joins.</summary>
    public string? AllyGroupName { get; init; }

    /// <summary>Gets the client-assigned identifier, when present.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets the unique faction color as #RRGGBB.</summary>
    public string? Color { get; init; }

    /// <summary>Gets whether a player who chooses this faction must pick a subfaction.</summary>
    public bool RequiresSubfaction { get; init; }

    /// <summary>Gets whether an existing uploaded flag image should be removed.</summary>
    public bool ClearFlagImage { get; init; }

    /// <summary>Gets special-rule identifiers assigned to this faction.</summary>
    public IReadOnlyList<Guid>? SpecialRuleIds { get; init; }
}

/// <summary>
/// Ally-group configuration in a save request.
/// </summary>
public sealed class AllyGroupRequest
{
    /// <summary>Gets the ally-group name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the unique overlay color as #RRGGBB.</summary>
    public string? Color { get; init; }
}

/// <summary>
/// Labeled external link in a save request.
/// </summary>
public sealed class LinkRequest
{
    /// <summary>Gets the display label.</summary>
    public required string Label { get; init; }

    /// <summary>Gets the destination URL.</summary>
    public required string Url { get; init; }
}

/// <summary>
/// Terrain type configuration in a save request.
/// </summary>
public sealed class TerrainTypeRequest
{
    /// <summary>Gets the client-assigned identifier, when present.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets the terrain type name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the unique overlay color as #RRGGBB.</summary>
    public required string Color { get; init; }

    /// <summary>Gets nested missions. At least one is required.</summary>
    public IReadOnlyList<MissionRequest>? Missions { get; init; }

    /// <summary>Gets campaign points awarded for currently owning a territory of this terrain.</summary>
    public int? CampaignPoints { get; init; }

    /// <summary>Gets whether this terrain is a water feature.</summary>
    public bool? IsWaterFeature { get; init; }

    /// <summary>Gets supply points granted by a controlled territory of this terrain.</summary>
    public int? SupplyPoints { get; init; }
}

/// <summary>
/// Structure type configuration in a save request.
/// </summary>
public sealed class StructureTypeRequest
{
    /// <summary>Gets the client-assigned identifier, when present.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets the structure name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the built-in logo key used until a custom image is uploaded.</summary>
    public string? BuiltinSymbol { get; init; }

    /// <summary>Gets whether an existing uploaded logo should be removed.</summary>
    public bool ClearImage { get; init; }

    /// <summary>Gets whether an existing uploaded pillaged logo should be removed.</summary>
    public bool ClearPillagedImage { get; init; }

    /// <summary>Gets whether players may Build this structure.</summary>
    public bool? IsBuildable { get; init; }

    /// <summary>Gets whether players may Pillage this structure.</summary>
    public bool? IsPillageable { get; init; }

    /// <summary>Gets whether a second Pillage may destroy and remove this structure.</summary>
    public bool? IsDestructible { get; init; }

    /// <summary>Gets nested missions.</summary>
    public IReadOnlyList<MissionRequest>? Missions { get; init; }

    /// <summary>Gets campaign points awarded for currently controlling this structure when it is not destroyed.</summary>
    public int? CampaignPoints { get; init; }

    /// <summary>Gets ongoing map supply while this structure is operational.</summary>
    public int? SupplyPoints { get; init; }

    /// <summary>Gets temporary supply awarded when this structure is pillaged.</summary>
    public int? PillageSupplyPoints { get; init; }

    /// <summary>Gets temporary supply awarded when this structure is destroyed.</summary>
    public int? DestroySupplyPoints { get; init; }
}

/// <summary>
/// Item objective configuration in a save request.
/// </summary>
public sealed class ItemObjectiveTypeRequest
{
    /// <summary>Gets the client-assigned identifier, when present.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets the item name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets whether the item stays hidden until found or staff-revealed. Defaults to true.</summary>
    public bool? IsHiddenUntilFound { get; init; }

    /// <summary>Gets Random or Placed. Defaults to Random.</summary>
    public string? Placement { get; init; }

    /// <summary>Gets whether the item may occupy a spawn territory. Defaults to false.</summary>
    public bool? AllowOnSpawn { get; init; }

    /// <summary>Gets the built-in logo key. Defaults to Crown.</summary>
    public string? BuiltinSymbol { get; init; }

    /// <summary>Gets the logo color as #RRGGBB.</summary>
    public string? Color { get; init; }

    /// <summary>Gets whether an existing uploaded logo should be removed.</summary>
    public bool ClearImage { get; init; }

    /// <summary>Gets campaign points awarded while a force currently holds this item.</summary>
    public int? CampaignPoints { get; init; }

    /// <summary>Gets optional flavor or lore text shown to the holder.</summary>
    public string? FlavorText { get; init; }

    /// <summary>Gets holder choices configured for this item.</summary>
    public IReadOnlyList<ItemObjectiveChoiceRequest>? Choices { get; init; }

    /// <summary>Gets special-rule identifiers assigned to this item.</summary>
    public IReadOnlyList<Guid>? SpecialRuleIds { get; init; }
}

/// <summary>
/// Public campaign objective configuration in a save request.
/// </summary>
public sealed class PublicObjectiveTypeRequest
{
    /// <summary>Gets the client-assigned identifier, when present.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets the objective name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets campaign points awarded when this objective is completed.</summary>
    public int? CampaignPoints { get; init; }
}

/// <summary>
/// Holder choice configuration on an item objective.
/// </summary>
public sealed class ItemObjectiveChoiceRequest
{
    /// <summary>Gets the client-assigned identifier, when present.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets the choice name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets configured results. Several results pick one at random.</summary>
    public IReadOnlyList<ItemObjectiveChoiceResultRequest>? Results { get; init; }
}

/// <summary>
/// One possible outcome of an item-objective choice.
/// </summary>
public sealed class ItemObjectiveChoiceResultRequest
{
    /// <summary>Gets the client-assigned identifier, when present.</summary>
    public Guid? Id { get; init; }

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
/// Reusable special-rule configuration in a save request.
/// </summary>
public sealed class SpecialRuleRequest
{
    /// <summary>Gets the client-assigned identifier, when present.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets the unique rule name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the player-facing rule text.</summary>
    public string? Text { get; init; }
}

/// <summary>
/// Force-status configuration in a save request. Normal is omitted.
/// </summary>
public sealed class ForceStatusRequest
{
    /// <summary>Gets the client-assigned identifier, when present.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets the unique status name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets tabletop effect text.</summary>
    public string? Effects { get; init; }

    /// <summary>Gets the enable-trigger name.</summary>
    public string? EnableTrigger { get; init; }

    /// <summary>Gets the clear-trigger name.</summary>
    public string? ClearTrigger { get; init; }
}

/// <summary>
/// Private campaign objective configuration in a save request.
/// </summary>
public sealed class PrivateObjectiveTypeRequest
{
    /// <summary>Gets the client-assigned identifier, when present.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets the objective name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the optional secret description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets campaign points awarded when revealed or completed.</summary>
    public int? CampaignPoints { get; init; }

    /// <summary>Gets holder kinds this entry may be assigned to.</summary>
    public IReadOnlyList<string>? AllowedHolderKinds { get; init; }

    /// <summary>Gets Manual or Automatic.</summary>
    public string? ScoringKind { get; init; }

    /// <summary>Gets the automatic criterion kind.</summary>
    public string? AutomaticKind { get; init; }

    /// <summary>Gets how many matching facts complete an automatic objective.</summary>
    public int? RequiredCount { get; init; }

    /// <summary>Gets the structure type for structure-based automatic criteria.</summary>
    public Guid? StructureTypeId { get; init; }

    /// <summary>Gets named territories for ControlNamedTerritories.</summary>
    public IReadOnlyList<Guid>? TerritoryIds { get; init; }
}

/// <summary>
/// Mission configuration nested under a terrain type or structure.
/// </summary>
public sealed class MissionRequest
{
    /// <summary>Gets the client-assigned identifier, when present.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets the mission name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets an optional http or https link.</summary>
    public string? Url { get; init; }

    /// <summary>Gets whether an existing uploaded file should be removed.</summary>
    public bool ClearFile { get; init; }

    /// <summary>Gets questions asked when reporting this mission's battle result.</summary>
    public IReadOnlyList<MissionResultQuestionRequest>? ResultQuestions { get; init; }

    /// <summary>Gets whether this mission is used for attacker/defender engagements.</summary>
    public bool IsAttackerDefender { get; init; }

    /// <summary>Gets whether attacker or defender army points are adjusted.</summary>
    public bool HasArmyPointsAdvantage { get; init; }

    /// <summary>Gets Attacker or Defender for the army-point adjustment.</summary>
    public string? ArmyPointsAdvantageSide { get; init; }

    /// <summary>Gets whether the army-point amount is a percent of the cap.</summary>
    public bool ArmyPointsAdvantageIsPercent { get; init; }

    /// <summary>Gets the signed army-point number or percent change.</summary>
    public int ArmyPointsAdvantageAmount { get; init; }

    /// <summary>Gets whether attacker or defender supply points are adjusted.</summary>
    public bool HasSupplyPointsAdvantage { get; init; }

    /// <summary>Gets Attacker or Defender for the supply-point adjustment.</summary>
    public string? SupplyPointsAdvantageSide { get; init; }

    /// <summary>Gets the signed raw supply-point change.</summary>
    public int SupplyPointsAdvantageAmount { get; init; }
}

/// <summary>
/// A campaign-manager-written question asked on a mission battle report.
/// </summary>
public sealed class MissionResultQuestionRequest
{
    /// <summary>Gets the client-assigned identifier, when present.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets the question text.</summary>
    public required string Prompt { get; init; }

    /// <summary>Gets Boolean or BattlePoints.</summary>
    public string? Kind { get; init; }

    /// <summary>Gets battle points awarded when a boolean answer is true.</summary>
    public int? BattlePoints { get; init; }

    /// <summary>Gets campaign points awarded when the question is scored.</summary>
    public int? CampaignPoints { get; init; }
}

/// <summary>
/// A campaign in the caller's list.
/// </summary>
public sealed class CampaignListItemResponse
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
/// Member-visible campaign metadata. Join passwords are omitted.
/// </summary>
public sealed class CampaignDetailResponse
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
    public required IReadOnlyList<FactionResponse> Factions { get; init; }

    /// <summary>Gets the terrain types.</summary>
    public required IReadOnlyList<TerrainTypeResponse> TerrainTypes { get; init; }

    /// <summary>Gets the structure types.</summary>
    public required IReadOnlyList<StructureTypeResponse> StructureTypes { get; init; }

    /// <summary>Gets the item objective types. Empty means none.</summary>
    public IReadOnlyList<ItemObjectiveTypeResponse> ItemObjectiveTypes { get; init; } = [];

    /// <summary>Gets the public campaign objectives. Empty means none.</summary>
    public IReadOnlyList<PublicObjectiveTypeResponse> PublicObjectiveTypes { get; init; } = [];

    /// <summary>Gets reusable special rules. Empty means none.</summary>
    public IReadOnlyList<SpecialRuleResponse> SpecialRules { get; init; } = [];

    /// <summary>Gets reusable missions. Empty means only nested terrain and structure missions.</summary>
    public IReadOnlyList<MissionResponse> Missions { get; init; } = [];

    /// <summary>Gets configured force statuses other than Normal.</summary>
    public IReadOnlyList<ForceStatusResponse> ForceStatuses { get; init; } = [];

    /// <summary>Gets private campaign objectives the viewer may see.</summary>
    public IReadOnlyList<PrivateObjectiveTypeResponse> PrivateObjectiveTypes { get; init; } = [];

    /// <summary>Gets assigned private objectives visible to the viewer.</summary>
    public IReadOnlyList<PrivateObjectiveAssignmentResponse> PrivateObjectives { get; init; } = [];

    /// <summary>Gets public unclaimed private-objective counts.</summary>
    public IReadOnlyList<PrivateObjectiveUnclaimedCountResponse> PrivateObjectiveUnclaimedCounts { get; init; } = [];

    /// <summary>Gets campaign points awarded to the winner when differential scoring is off.</summary>
    public int PointsPerBattleWon { get; init; }

    /// <summary>Gets campaign points awarded to each participant of a draw.</summary>
    public int PointsPerBattleDraw { get; init; }

    /// <summary>Gets whether battle campaign points use score differential.</summary>
    public bool UseDifferentialBattleScoring { get; init; }

    /// <summary>Gets the multiplier applied to the winner-minus-loser score difference.</summary>
    public decimal DifferentialMultiplier { get; init; }

    /// <summary>Gets the inclusive lower clamp for differential campaign points.</summary>
    public int DifferentialMinimum { get; init; }

    /// <summary>Gets the inclusive upper clamp for differential campaign points.</summary>
    public int DifferentialMaximum { get; init; }

    /// <summary>Gets whether the loser can receive negative campaign points.</summary>
    public bool AllowNegativeDifferential { get; init; }

    /// <summary>Gets campaign points for most territories currently controlled.</summary>
    public int MostTerritoriesCampaignPoints { get; init; }

    /// <summary>Gets campaign points for the longest owned territory chain.</summary>
    public int LongestTerritoryChainCampaignPoints { get; init; }

    /// <summary>Gets campaign points for most battle wins.</summary>
    public int MostBattlesWonCampaignPoints { get; init; }

    /// <summary>Gets the percent subtracted from map-plus-round supply when a player has split forces.</summary>
    public int SplitForceSupplyPenaltyPercent { get; init; }

    /// <summary>Gets whether every battle report asks if the enemy general was slain.</summary>
    public bool AlwaysAskGeneralKill { get; init; }

    /// <summary>Gets whether every battle report asks if the enemy supply line was destroyed.</summary>
    public bool AlwaysAskSupplyLineDestroyed { get; init; }

    /// <summary>Gets campaign points awarded for a slain enemy general.</summary>
    public int GeneralKillCampaignPoints { get; init; }

    /// <summary>Gets campaign points awarded for destroying the enemy supply line.</summary>
    public int SupplyLineDestroyedCampaignPoints { get; init; }

    /// <summary>Gets per-round army size, free supply, and free characters.</summary>
    public IReadOnlyList<RoundArmyEscalationResponse> RoundEscalations { get; init; } = [];

    /// <summary>Gets current campaign-point standings for players.</summary>
    public IReadOnlyList<CampaignPointStandingResponse> Standings { get; init; } = [];

    /// <summary>Gets current top-five leaders for enabled ranking public objectives.</summary>
    public IReadOnlyList<PublicObjectiveLeaderboardResponse> PublicObjectiveLeaderboards { get; init; } = [];

    /// <summary>Gets factions that left their ally group through Backstab.</summary>
    public IReadOnlyList<Guid> BrokenAllyFactionIds { get; init; } = [];

    /// <summary>Gets the ally groups.</summary>
    public required IReadOnlyList<AllyGroupResponse> AllyGroups { get; init; }

    /// <summary>Gets the external links.</summary>
    public required IReadOnlyList<LinkResponse> Links { get; init; }

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
    public required IReadOnlyList<RoundPhaseResponse> Phases { get; init; }

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
    public IReadOnlyList<CampaignParticipantResponse> Participants { get; init; } = [];

    /// <summary>Gets current members who may be tagged in chat.</summary>
    public required IReadOnlyList<CampaignLogMemberResponse> MentionableMembers { get; init; }

    /// <summary>Gets compose targets: public, members, factions, and ally groups.</summary>
    public IReadOnlyList<ChatChannelResponse> ChatChannels { get; init; } = [];

    /// <summary>Gets the campaign log, including chat the viewer is allowed to see.</summary>
    public required IReadOnlyList<PlayLogEntryResponse> Log { get; init; }
}

/// <summary>
/// A member attached to a campaign, shown on the Participants panel.
/// </summary>
public sealed class CampaignParticipantResponse
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

    /// <summary>Gets the maximum one of this player's forces can spend if assigned the remaining temporary pool.</summary>
    public int? CurrentSupplyPoints { get; init; }

    /// <summary>Gets remaining player-pool temporary supply, when play has started.</summary>
    public int? TemporarySupplyPoints { get; init; }

    /// <summary>Gets map supply from connected territories and operational structures.</summary>
    public int? MapSupplyPoints { get; init; }

    /// <summary>Gets free supply granted this round.</summary>
    public int? RoundFreeSupplyPoints { get; init; }

    /// <summary>Gets this round's maximum army points size.</summary>
    public int? MaxArmyPoints { get; init; }

    /// <summary>Gets free characters whose base cost does not count against supply this round.</summary>
    public int? FreeCharacterCount { get; init; }
}

/// <summary>
/// A current campaign member who may be tagged in chat.
/// </summary>
public sealed class CampaignLogMemberResponse
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
public sealed class ChatChannelResponse
{
    /// <summary>Gets Public, Direct, Faction, or AllyGroup.</summary>
    public required string Kind { get; init; }

    /// <summary>Gets the member, faction, or ally-group identifier for a private channel.</summary>
    public Guid? TargetId { get; init; }

    /// <summary>Gets the label shown in the channel list.</summary>
    public required string Label { get; init; }
}

/// <summary>
/// Request to post a chat message.
/// </summary>
public sealed class PostCampaignChatRequest
{
    /// <summary>Gets the last observed campaign revision.</summary>
    public required int Revision { get; init; }

    /// <summary>Gets the chat message.</summary>
    public required string Message { get; init; }

    /// <summary>Gets Public, Direct, Faction, or AllyGroup.</summary>
    public string ChannelKind { get; init; } = "Public";

    /// <summary>Gets the member, faction, or ally-group identifier for a private channel.</summary>
    public Guid? TargetId { get; init; }
}

/// <summary>
/// An action or battle step in a campaign response.
/// </summary>
public sealed class RoundPhaseResponse
{
    /// <summary>Gets the phase kind name.</summary>
    public required string Kind { get; init; }

    /// <summary>Gets the duration amount.</summary>
    public required int DurationAmount { get; init; }

    /// <summary>Gets the duration unit name.</summary>
    public required string DurationUnit { get; init; }
}

/// <summary>
/// A faction in a campaign response.
/// </summary>
public sealed class FactionResponse
{
    /// <summary>Gets the faction identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the faction name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the subfaction names.</summary>
    public required IReadOnlyList<string> Subfactions { get; init; }

    /// <summary>Gets the ally-group name this faction joins, if any.</summary>
    public string? AllyGroupName { get; init; }

    /// <summary>Gets the unique faction color as #RRGGBB.</summary>
    public required string Color { get; init; }

    /// <summary>Gets whether a player who chooses this faction must pick a subfaction.</summary>
    public required bool RequiresSubfaction { get; init; }

    /// <summary>Gets whether the faction has an uploaded flag image.</summary>
    public required bool HasFlagImage { get; init; }

    /// <summary>Gets special-rule identifiers assigned to this faction.</summary>
    public IReadOnlyList<Guid> SpecialRuleIds { get; init; } = [];
}

/// <summary>
/// An ally group in a campaign response.
/// </summary>
public sealed class AllyGroupResponse
{
    /// <summary>Gets the ally-group identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the ally-group name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the unique overlay color as #RRGGBB.</summary>
    public string Color { get; init; } = "#4B5563";
}

/// <summary>
/// A labeled external link in a campaign response.
/// </summary>
public sealed class LinkResponse
{
    /// <summary>Gets the link identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the display label.</summary>
    public required string Label { get; init; }

    /// <summary>Gets the destination URL.</summary>
    public required string Url { get; init; }
}

/// <summary>
/// A terrain type in a campaign response.
/// </summary>
public sealed class TerrainTypeResponse
{
    /// <summary>Gets the terrain type identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the terrain type name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the unique overlay color as #RRGGBB.</summary>
    public required string Color { get; init; }

    /// <summary>Gets the missions.</summary>
    public required IReadOnlyList<MissionResponse> Missions { get; init; }

    /// <summary>Gets campaign points awarded for currently owning a territory of this terrain.</summary>
    public int CampaignPoints { get; init; }

    /// <summary>Gets supply points granted by a controlled territory of this terrain.</summary>
    public int SupplyPoints { get; init; } = 1;

    /// <summary>Gets whether this terrain is a water feature.</summary>
    public bool IsWaterFeature { get; init; }
}

/// <summary>
/// A structure type in a campaign response.
/// </summary>
public sealed class StructureTypeResponse
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
    public required IReadOnlyList<MissionResponse> Missions { get; init; }

    /// <summary>Gets campaign points awarded for currently controlling this structure when it is not destroyed.</summary>
    public int CampaignPoints { get; init; }

    /// <summary>Gets ongoing map supply while this structure is operational.</summary>
    public int SupplyPoints { get; init; } = 1;

    /// <summary>Gets temporary supply awarded when this structure is pillaged.</summary>
    public int PillageSupplyPoints { get; init; } = 1;

    /// <summary>Gets temporary supply awarded when this structure is destroyed.</summary>
    public int DestroySupplyPoints { get; init; } = 1;
}

/// <summary>
/// An item objective type in a campaign response.
/// </summary>
public sealed class ItemObjectiveTypeResponse
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

    /// <summary>Gets whether a custom logo image is stored.</summary>
    public bool HasImage { get; init; }

    /// <summary>Gets campaign points awarded while a force currently holds this item.</summary>
    public int CampaignPoints { get; init; }

    /// <summary>Gets optional flavor or lore text shown to the holder or a manager.</summary>
    public string? FlavorText { get; init; }

    /// <summary>Gets holder choices configured for this item.</summary>
    public IReadOnlyList<ItemObjectiveChoiceResponse> Choices { get; init; } = [];

    /// <summary>Gets special-rule identifiers assigned to this item.</summary>
    public IReadOnlyList<Guid> SpecialRuleIds { get; init; } = [];
}

/// <summary>
/// A public campaign objective in a campaign response.
/// </summary>
public sealed class PublicObjectiveTypeResponse
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
/// A holder choice on an item objective.
/// </summary>
public sealed class ItemObjectiveChoiceResponse
{
    /// <summary>Gets the choice identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the choice name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets configured results. Result effects are omitted from unauthorized views.</summary>
    public IReadOnlyList<ItemObjectiveChoiceResultResponse> Results { get; init; } = [];
}

/// <summary>
/// One possible outcome of an item-objective choice.
/// </summary>
public sealed class ItemObjectiveChoiceResultResponse
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
public sealed class SpecialRuleResponse
{
    /// <summary>Gets the rule identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the unique rule name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the player-facing rule text.</summary>
    public required string Text { get; init; }
}

/// <summary>
/// A configured force status other than Normal.
/// </summary>
public sealed class ForceStatusResponse
{
    /// <summary>Gets the status identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the unique status name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets tabletop effect text.</summary>
    public required string Effects { get; init; }

    /// <summary>Gets the enable-trigger name.</summary>
    public required string EnableTrigger { get; init; }

    /// <summary>Gets the clear-trigger name.</summary>
    public required string ClearTrigger { get; init; }
}

/// <summary>
/// A private-objective catalog entry. Secret fields are omitted unless the viewer may see them.
/// </summary>
public sealed class PrivateObjectiveTypeResponse
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

    /// <summary>Gets the automatic criterion kind when the viewer may see it.</summary>
    public string? AutomaticKind { get; init; }

    /// <summary>Gets how many matching facts complete an automatic objective.</summary>
    public int RequiredCount { get; init; } = 1;

    /// <summary>Gets the structure type when the viewer may see it.</summary>
    public Guid? StructureTypeId { get; init; }

    /// <summary>Gets named territories when the viewer may see them.</summary>
    public IReadOnlyList<Guid> TerritoryIds { get; init; } = [];
}

/// <summary>
/// One assigned private objective visible to the current viewer.
/// </summary>
public sealed class PrivateObjectiveAssignmentResponse
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
public sealed class PrivateObjectiveUnclaimedCountResponse
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
/// Current leaders for one ranking public objective.
/// </summary>
public sealed class PublicObjectiveLeaderboardResponse
{
    /// <summary>Gets the ranking objective kind.</summary>
    public required string Kind { get; init; }

    /// <summary>Gets campaign points awarded to each current first-place player.</summary>
    public required int AwardPoints { get; init; }

    /// <summary>Gets players currently in the top five.</summary>
    public required IReadOnlyList<PublicObjectiveLeaderResponse> Leaders { get; init; }
}

/// <summary>
/// One player on a ranking public-objective leaderboard.
/// </summary>
public sealed class PublicObjectiveLeaderResponse
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
/// One player's current campaign-point standing.
/// </summary>
public sealed class CampaignPointStandingResponse
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

    /// <summary>Gets points from currently owned territories and non-destroyed structures.</summary>
    public required int TerritoryAndStructurePoints { get; init; }

    /// <summary>Gets points from finalized battle wins.</summary>
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
    public IReadOnlyList<HeldItemObjectiveResponse> HeldItems { get; init; } = [];
}

/// <summary>
/// A visible item objective currently held by a player.
/// </summary>
public sealed class HeldItemObjectiveResponse
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
/// A mission nested under a terrain type or structure.
/// </summary>
public sealed class MissionResponse
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

    /// <summary>Gets questions asked when reporting this mission's battle result.</summary>
    public IReadOnlyList<MissionResultQuestionResponse> ResultQuestions { get; init; } = [];

    /// <summary>Gets whether this mission is used for attacker/defender engagements.</summary>
    public bool IsAttackerDefender { get; init; }

    /// <summary>Gets whether attacker or defender army points are adjusted.</summary>
    public bool HasArmyPointsAdvantage { get; init; }

    /// <summary>Gets Attacker or Defender for the army-point adjustment.</summary>
    public string ArmyPointsAdvantageSide { get; init; } = "Defender";

    /// <summary>Gets whether the army-point amount is a percent of the cap.</summary>
    public bool ArmyPointsAdvantageIsPercent { get; init; }

    /// <summary>Gets the signed army-point number or percent change.</summary>
    public int ArmyPointsAdvantageAmount { get; init; }

    /// <summary>Gets whether attacker or defender supply points are adjusted.</summary>
    public bool HasSupplyPointsAdvantage { get; init; }

    /// <summary>Gets Attacker or Defender for the supply-point adjustment.</summary>
    public string SupplyPointsAdvantageSide { get; init; } = "Defender";

    /// <summary>Gets the signed raw supply-point change.</summary>
    public int SupplyPointsAdvantageAmount { get; init; }
}

/// <summary>
/// A campaign-manager-written question asked on a mission battle report.
/// </summary>
public sealed class MissionResultQuestionResponse
{
    /// <summary>Gets the question identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the question text.</summary>
    public required string Prompt { get; init; }

    /// <summary>Gets Boolean or BattlePoints.</summary>
    public required string Kind { get; init; }

    /// <summary>Gets battle points awarded when a boolean answer is true.</summary>
    public int BattlePoints { get; init; }

    /// <summary>Gets campaign points awarded when the question is scored.</summary>
    public int CampaignPoints { get; init; }
}

/// <summary>
/// Per-round army size and free allowances in a campaign response.
/// </summary>
public sealed class RoundArmyEscalationResponse
{
    /// <summary>Gets the 1-based round.</summary>
    public required int RoundNumber { get; init; }

    /// <summary>Gets the maximum army points size for the round.</summary>
    public required int MaxArmyPoints { get; init; }

    /// <summary>Gets free supply points granted this round.</summary>
    public required int FreeSupplyPoints { get; init; }

    /// <summary>Gets how many characters have a free base cost against supply.</summary>
    public required int FreeCharacterCount { get; init; }
}

/// <summary>
/// Request to join a campaign as a player.
/// </summary>
public sealed class JoinCampaignRequest
{
    /// <summary>Gets the join password for a private campaign.</summary>
    public string? JoinPassword { get; init; }
}

/// <summary>
/// Request for a manager or administrator to add a player without a join password.
/// </summary>
public sealed class AddCampaignMemberRequest
{
    /// <summary>Gets the last observed campaign revision.</summary>
    public required int Revision { get; init; }

    /// <summary>Gets the account to add.</summary>
    public required Guid UserId { get; init; }
}

/// <summary>
/// Request for a manager or administrator to remove a player.
/// </summary>
public sealed class KickCampaignMemberRequest
{
    /// <summary>Gets the last observed campaign revision.</summary>
    public required int Revision { get; init; }

    /// <summary>Gets the player to remove.</summary>
    public required Guid UserId { get; init; }
}

/// <summary>
/// Request for a manager or administrator to assign another player's faction.
/// </summary>
public sealed class AssignPlayerFactionRequest
{
    /// <summary>Gets the last observed campaign revision.</summary>
    public required int Revision { get; init; }

    /// <summary>Gets the player whose faction is assigned.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets the faction.</summary>
    public required Guid FactionId { get; init; }

    /// <summary>Gets the subfaction, when required.</summary>
    public string? Subfaction { get; init; }
}

/// <summary>
/// A public identity returned by campaign member search. Email is omitted.
/// </summary>
public sealed class UserSearchHitResponse
{
    /// <summary>Gets the account identifier.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets the unique username.</summary>
    public required string Username { get; init; }

    /// <summary>Gets the name shown to other users.</summary>
    public required string DisplayName { get; init; }
}

/// <summary>
/// A named campaign preset an administrator saved.
/// </summary>
public sealed class CampaignPresetListItemResponse
{
    /// <summary>Gets the preset identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the preset name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets whether the preset includes a map image or overlay graph.</summary>
    public required bool HasMap { get; init; }
}

/// <summary>
/// Body for saving the current campaign as a named preset.
/// </summary>
public sealed class SaveCampaignPresetRequest
{
    /// <summary>Gets the preset name. Matching an existing name overwrites that preset.</summary>
    public string Name { get; init; } = string.Empty;
}

/// <summary>
/// Body for copying a saved preset's map onto a campaign.
/// </summary>
public sealed class ApplyCampaignPresetRequest
{
    /// <summary>Gets the preset identifier.</summary>
    public Guid PresetId { get; init; }

    /// <summary>Gets the last observed campaign revision.</summary>
    public int Revision { get; init; }
}

/// <summary>
/// Maps campaign application models onto HTTP contracts.
/// </summary>
public static class CampaignResponses
{
    /// <summary>
    /// Maps a list item.
    /// </summary>
    /// <param name="item">The list item.</param>
    /// <returns>The HTTP response.</returns>
    public static CampaignListItemResponse FromListItem(CampaignListItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return new CampaignListItemResponse
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            PlayerSlotCount = item.PlayerSlotCount,
            OccupiedPlayerSlots = item.OccupiedPlayerSlots,
            IsPrivate = item.IsPrivate,
            IsPubliclyViewable = item.IsPubliclyViewable,
            CanManage = item.CanManage,
            IsParticipant = item.IsParticipant,
            CanView = item.CanView,
            CanJoin = item.CanJoin,
            CanLeave = item.CanLeave,
            City = item.City,
            Region = item.Region,
            Country = item.Country,
            Status = item.Status,
            StartsUtc = item.StartsUtc,
            EndsUtc = item.EndsUtc,
            CurrentRound = item.CurrentRound,
            CurrentPhaseLabel = item.CurrentPhaseLabel,
            CurrentPhaseEndsUtc = item.CurrentPhaseEndsUtc,
            CanPlay = item.CanPlay,
        };
    }

    /// <summary>
    /// Maps a saved campaign preset list item.
    /// </summary>
    /// <param name="item">The preset list item.</param>
    /// <returns>The HTTP response.</returns>
    public static CampaignPresetListItemResponse FromPresetListItem(CampaignPresetListItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return new CampaignPresetListItemResponse
        {
            Id = item.Id,
            Name = item.Name,
            HasMap = item.HasMap,
        };
    }

    /// <summary>
    /// Maps a campaign detail. Join password hashes are not present on the source model.
    /// </summary>
    /// <param name="detail">The detail.</param>
    /// <returns>The HTTP response.</returns>
    public static CampaignDetailResponse FromDetail(CampaignDetail detail)
    {
        ArgumentNullException.ThrowIfNull(detail);
        return new CampaignDetailResponse
        {
            Id = detail.Id,
            Name = detail.Name,
            Description = detail.Description,
            PlayerSlotCount = detail.PlayerSlotCount,
            OccupiedPlayerSlots = detail.OccupiedPlayerSlots,
            IsPrivate = detail.IsPrivate,
            IsPubliclyViewable = detail.IsPubliclyViewable,
            CreatorIsParticipant = detail.CreatorIsParticipant,
            City = detail.City,
            Region = detail.Region,
            Country = detail.Country,
            HasMap = detail.HasMap,
            CanManage = detail.CanManage,
            IsParticipant = detail.IsParticipant,
            Revision = detail.Revision,
            CreatedUtc = detail.CreatedUtc,
            UpdatedUtc = detail.UpdatedUtc,
            Factions =
            [
                .. detail.Factions.Select(static faction => new FactionResponse
                {
                    Id = faction.Id,
                    Name = faction.Name,
                    Subfactions = faction.Subfactions,
                    AllyGroupName = faction.AllyGroupName,
                    Color = faction.Color,
                    RequiresSubfaction = faction.RequiresSubfaction,
                    HasFlagImage = faction.HasFlagImage,
                    SpecialRuleIds = faction.SpecialRuleIds,
                }),
            ],
            TerrainTypes =
            [
                .. detail.TerrainTypes.Select(static type => new TerrainTypeResponse
                {
                    Id = type.Id,
                    Name = type.Name,
                    Color = type.Color,
                    CampaignPoints = type.CampaignPoints,
                    SupplyPoints = type.SupplyPoints,
                    IsWaterFeature = type.IsWaterFeature,
                    Missions = [.. type.Missions.Select(FromMission)],
                }),
            ],
            StructureTypes =
            [
                .. detail.StructureTypes.Select(static type => new StructureTypeResponse
                {
                    Id = type.Id,
                    Name = type.Name,
                    BuiltinSymbol = type.BuiltinSymbol,
                    HasImage = type.HasImage,
                    HasPillagedImage = type.HasPillagedImage,
                    IsBuildable = type.IsBuildable,
                    IsPillageable = type.IsPillageable,
                    IsDestructible = type.IsDestructible,
                    CampaignPoints = type.CampaignPoints,
                    SupplyPoints = type.SupplyPoints,
                    PillageSupplyPoints = type.PillageSupplyPoints,
                    DestroySupplyPoints = type.DestroySupplyPoints,
                    Missions = [.. type.Missions.Select(FromMission)],
                }),
            ],
            ItemObjectiveTypes =
            [
                .. detail.ItemObjectiveTypes.Select(static type => new ItemObjectiveTypeResponse
                {
                    Id = type.Id,
                    Name = type.Name,
                    IsHiddenUntilFound = type.IsHiddenUntilFound,
                    Placement = type.Placement,
                    AllowOnSpawn = type.AllowOnSpawn,
                    BuiltinSymbol = type.BuiltinSymbol,
                    Color = type.Color,
                    HasImage = type.HasImage,
                    CampaignPoints = type.CampaignPoints,
                    FlavorText = type.FlavorText,
                    SpecialRuleIds = type.SpecialRuleIds,
                    Choices =
                    [
                        .. type.Choices.Select(static choice => new ItemObjectiveChoiceResponse
                        {
                            Id = choice.Id,
                            Name = choice.Name,
                            Results =
                            [
                                .. choice.Results.Select(static result => new ItemObjectiveChoiceResultResponse
                                {
                                    Id = result.Id,
                                    FlavorText = result.FlavorText,
                                    NewStateKey = result.NewStateKey,
                                    DestroyItem = result.DestroyItem,
                                    ReplacementItemTypeId = result.ReplacementItemTypeId,
                                    GrantedPrivateObjectiveTypeId = result.GrantedPrivateObjectiveTypeId,
                                }),
                            ],
                        }),
                    ],
                }),
            ],
            PublicObjectiveTypes =
            [
                .. detail.PublicObjectiveTypes.Select(static type => new PublicObjectiveTypeResponse
                {
                    Id = type.Id,
                    Name = type.Name,
                    Description = type.Description,
                    CampaignPoints = type.CampaignPoints,
                }),
            ],
            SpecialRules =
            [
                .. detail.SpecialRules.Select(static rule => new SpecialRuleResponse
                {
                    Id = rule.Id,
                    Name = rule.Name,
                    Text = rule.Text,
                }),
            ],
            Missions = [.. detail.Missions.Select(FromMission)],
            ForceStatuses =
            [
                .. detail.ForceStatuses.Select(static status => new ForceStatusResponse
                {
                    Id = status.Id,
                    Name = status.Name,
                    Effects = status.Effects,
                    EnableTrigger = status.EnableTrigger,
                    ClearTrigger = status.ClearTrigger,
                }),
            ],
            PrivateObjectiveTypes =
            [
                .. detail.PrivateObjectiveTypes.Select(static type => new PrivateObjectiveTypeResponse
                {
                    Id = type.Id,
                    Name = type.Name,
                    Description = type.Description,
                    CampaignPoints = type.CampaignPoints,
                    AllowedHolderKinds = type.AllowedHolderKinds,
                    ScoringKind = type.ScoringKind,
                    AutomaticKind = type.AutomaticKind,
                    RequiredCount = type.RequiredCount,
                    StructureTypeId = type.StructureTypeId,
                    TerritoryIds = type.TerritoryIds,
                }),
            ],
            PrivateObjectives =
            [
                .. detail.PrivateObjectives.Select(static item => new PrivateObjectiveAssignmentResponse
                {
                    Id = item.Id,
                    TypeId = item.TypeId,
                    HolderKind = item.HolderKind,
                    HolderId = item.HolderId,
                    Status = item.Status,
                    ScoringKind = item.ScoringKind,
                    Name = item.Name,
                    Description = item.Description,
                    CampaignPoints = item.CampaignPoints,
                    CanClaim = item.CanClaim,
                    CanModerate = item.CanModerate,
                }),
            ],
            PrivateObjectiveUnclaimedCounts =
            [
                .. detail.PrivateObjectiveUnclaimedCounts.Select(static item => new PrivateObjectiveUnclaimedCountResponse
                {
                    HolderKind = item.HolderKind,
                    HolderId = item.HolderId,
                    HolderName = item.HolderName,
                    Count = item.Count,
                }),
            ],
            PointsPerBattleWon = detail.PointsPerBattleWon,
            PointsPerBattleDraw = detail.PointsPerBattleDraw,
            UseDifferentialBattleScoring = detail.UseDifferentialBattleScoring,
            DifferentialMultiplier = detail.DifferentialMultiplier,
            DifferentialMinimum = detail.DifferentialMinimum,
            DifferentialMaximum = detail.DifferentialMaximum,
            AllowNegativeDifferential = detail.AllowNegativeDifferential,
            MostTerritoriesCampaignPoints = detail.MostTerritoriesCampaignPoints,
            LongestTerritoryChainCampaignPoints = detail.LongestTerritoryChainCampaignPoints,
            MostBattlesWonCampaignPoints = detail.MostBattlesWonCampaignPoints,
            SplitForceSupplyPenaltyPercent = detail.SplitForceSupplyPenaltyPercent,
            AlwaysAskGeneralKill = detail.AlwaysAskGeneralKill,
            AlwaysAskSupplyLineDestroyed = detail.AlwaysAskSupplyLineDestroyed,
            GeneralKillCampaignPoints = detail.GeneralKillCampaignPoints,
            SupplyLineDestroyedCampaignPoints = detail.SupplyLineDestroyedCampaignPoints,
            RoundEscalations =
            [
                .. detail.RoundEscalations.Select(static row => new RoundArmyEscalationResponse
                {
                    RoundNumber = row.RoundNumber,
                    MaxArmyPoints = row.MaxArmyPoints,
                    FreeSupplyPoints = row.FreeSupplyPoints,
                    FreeCharacterCount = row.FreeCharacterCount,
                }),
            ],
            Standings = [.. detail.Standings.Select(FromStanding)],
            PublicObjectiveLeaderboards = [.. detail.PublicObjectiveLeaderboards.Select(FromLeaderboard)],
            BrokenAllyFactionIds = detail.BrokenAllyFactionIds,
            AllyGroups =
            [
                .. detail.AllyGroups.Select(static group => new AllyGroupResponse
                {
                    Id = group.Id,
                    Name = group.Name,
                    Color = group.Color,
                }),
            ],
            Links =
            [
                .. detail.Links.Select(static link => new LinkResponse
                {
                    Id = link.Id,
                    Label = link.Label,
                    Url = link.Url,
                }),
            ],
            TimeZoneId = detail.TimeZoneId,
            StartsAtLocal = detail.StartsAtLocal,
            StartsUtc = detail.StartsUtc,
            EndsUtc = detail.EndsUtc,
            RoundCount = detail.RoundCount,
            RoundLengthAmount = detail.RoundLengthAmount,
            RoundLengthUnit = detail.RoundLengthUnit,
            Phases =
            [
                .. detail.Phases.Select(static phase => new RoundPhaseResponse
                {
                    Kind = phase.Kind,
                    DurationAmount = phase.DurationAmount,
                    DurationUnit = phase.DurationUnit,
                }),
            ],
            Status = detail.Status,
            CurrentRound = detail.CurrentRound,
            CurrentPhaseNumber = detail.CurrentPhaseNumber,
            CurrentPhaseKind = detail.CurrentPhaseKind,
            CurrentPhaseStartsUtc = detail.CurrentPhaseStartsUtc,
            CurrentPhaseEndsUtc = detail.CurrentPhaseEndsUtc,
            FactionId = detail.FactionId,
            Subfaction = detail.Subfaction,
            CanPlay = detail.CanPlay,
            CanChooseFaction = detail.CanChooseFaction,
            CanChat = detail.CanChat,
            CanInspectPrivateChat = detail.CanInspectPrivateChat,
            Participants =
            [
                .. detail.Participants.Select(static participant => new CampaignParticipantResponse
                {
                    UserId = participant.UserId,
                    Username = participant.Username,
                    DisplayName = participant.DisplayName,
                    IsPlayer = participant.IsPlayer,
                    IsGameMaster = participant.IsGameMaster,
                    IsAdministrator = participant.IsAdministrator,
                    FactionName = participant.FactionName,
                    Subfaction = participant.Subfaction,
                    FactionId = participant.FactionId,
                    FactionColor = participant.FactionColor,
                    HasFlagImage = participant.HasFlagImage,
                    AllyGroupName = participant.AllyGroupName,
                    CurrentSupplyPoints = participant.CurrentSupplyPoints,
                    TemporarySupplyPoints = participant.TemporarySupplyPoints,
                    MapSupplyPoints = participant.MapSupplyPoints,
                    RoundFreeSupplyPoints = participant.RoundFreeSupplyPoints,
                    MaxArmyPoints = participant.MaxArmyPoints,
                    FreeCharacterCount = participant.FreeCharacterCount,
                }),
            ],
            MentionableMembers =
            [
                .. detail.MentionableMembers.Select(static member => new CampaignLogMemberResponse
                {
                    UserId = member.UserId,
                    Username = member.Username,
                    DisplayName = member.DisplayName,
                }),
            ],
            ChatChannels =
            [
                .. detail.ChatChannels.Select(static channel => new ChatChannelResponse
                {
                    Kind = channel.Kind,
                    TargetId = channel.TargetId,
                    Label = channel.Label,
                }),
            ],
            Log =
            [
                .. detail.Log.Select(PlayResponses.FromLogEntry),
            ],
        };
    }

    /// <summary>
    /// Maps a map-graph detail onto an HTTP response.
    /// </summary>
    /// <param name="detail">The detail.</param>
    /// <returns>The HTTP response.</returns>
    public static MapGraphResponse FromMapGraph(CampaignMapGraphDetail detail)
    {
        ArgumentNullException.ThrowIfNull(detail);
        return new MapGraphResponse
        {
            CampaignId = detail.CampaignId,
            Revision = detail.Revision,
            CanManage = detail.CanManage,
            Territories =
            [
                .. detail.Territories.Select(static territory => new TerritoryResponse
                {
                    Id = territory.Id,
                    DisplayNumber = territory.DisplayNumber,
                    Name = territory.Name,
                    Description = territory.Description,
                    Polygon =
                    [
                        .. territory.Polygon.Select(static point => new MapPointResponse { X = point.X, Y = point.Y }),
                    ],
                    TerrainTypeId = territory.TerrainTypeId,
                    StructureTypeId = territory.StructureTypeId,
                    OverlayColor = territory.OverlayColor,
                    OwnerFactionId = territory.OwnerFactionId,
                    SpawnFactionId = territory.SpawnFactionId,
                    StructureCondition = territory.StructureCondition,
                }),
            ],
            Adjacencies =
            [
                .. detail.Adjacencies.Select(static edge => new AdjacencyResponse
                {
                    Id = edge.Id,
                    TerritoryAId = edge.TerritoryAId,
                    TerritoryBId = edge.TerritoryBId,
                    Origin = edge.Origin,
                    MarkerX = edge.MarkerX,
                    MarkerY = edge.MarkerY,
                }),
            ],
            ItemObjectivePlacements =
            [
                .. detail.ItemObjectivePlacements.Select(static item => new ItemObjectivePlacementResponse
                {
                    TypeId = item.TypeId,
                    TerritoryId = item.TerritoryId,
                }),
            ],
        };
    }

    /// <summary>
    /// Maps HTTP territory requests onto domain inputs.
    /// </summary>
    /// <param name="territories">The request territories.</param>
    /// <returns>The domain inputs.</returns>
    public static IReadOnlyList<TerritoryInput> ToTerritoryInputs(IReadOnlyList<TerritoryRequest>? territories)
    {
        if (territories is null)
        {
            return [];
        }

        return
        [
            .. territories.Select(static territory => new TerritoryInput
            {
                Id = territory.Id,
                DisplayNumber = territory.DisplayNumber,
                Name = territory.Name,
                Description = territory.Description,
                Polygon =
                [
                    .. territory.Polygon.Select(static point => new MapPointInput { X = point.X, Y = point.Y }),
                ],
                TerrainTypeId = territory.TerrainTypeId,
                StructureTypeId = territory.StructureTypeId,
                OverlayColor = territory.OverlayColor,
                OwnerFactionId = territory.OwnerFactionId,
                SpawnFactionId = territory.SpawnFactionId,
                StructureCondition = territory.StructureCondition,
            }),
        ];
    }

    /// <summary>
    /// Maps HTTP adjacency requests onto domain inputs.
    /// </summary>
    /// <param name="adjacencies">The request adjacencies.</param>
    /// <returns>The domain inputs.</returns>
    public static IReadOnlyList<AdjacencyInput> ToAdjacencyInputs(IReadOnlyList<AdjacencyRequest>? adjacencies)
    {
        if (adjacencies is null)
        {
            return [];
        }

        return
        [
            .. adjacencies.Select(static edge => new AdjacencyInput
            {
                Id = edge.Id,
                TerritoryAId = edge.TerritoryAId,
                TerritoryBId = edge.TerritoryBId,
                Origin = edge.Origin,
                MarkerX = edge.MarkerX,
                MarkerY = edge.MarkerY,
            }),
        ];
    }

    /// <summary>
    /// Maps HTTP faction requests onto domain inputs.
    /// </summary>
    /// <param name="factions">The request factions.</param>
    /// <returns>The domain inputs.</returns>
    public static IReadOnlyList<FactionInput> ToFactionInputs(IReadOnlyList<FactionRequest>? factions)
    {
        if (factions is null)
        {
            return [];
        }

        return
        [
            .. factions.Select(static faction => new FactionInput
            {
                Id = faction.Id,
                Name = faction.Name,
                Color = faction.Color,
                Subfactions = faction.Subfactions,
                AllyGroupName = faction.AllyGroupName,
                RequiresSubfaction = faction.RequiresSubfaction,
                ClearFlagImage = faction.ClearFlagImage,
                SpecialRuleIds = faction.SpecialRuleIds,
            }),
        ];
    }

    /// <summary>
    /// Maps HTTP ally-group requests onto domain inputs.
    /// </summary>
    /// <param name="groups">The request groups.</param>
    /// <returns>The domain inputs.</returns>
    public static IReadOnlyList<AllyGroupInput>? ToAllyGroupInputs(IReadOnlyList<AllyGroupRequest>? groups)
    {
        return groups?
            .Select(static group => new AllyGroupInput { Name = group.Name, Color = group.Color })
            .ToArray();
    }

    /// <summary>
    /// Maps HTTP terrain-type requests onto domain inputs.
    /// </summary>
    /// <param name="types">The request terrain types.</param>
    /// <returns>The domain inputs.</returns>
    public static IReadOnlyList<TerrainTypeInput>? ToTerrainTypeInputs(IReadOnlyList<TerrainTypeRequest>? types)
    {
        return types?
            .Select(static type => new TerrainTypeInput
            {
                Id = type.Id,
                Name = type.Name,
                Color = type.Color,
                Missions = ToMissionInputs(type.Missions),
                IsWaterFeature = type.IsWaterFeature,
                SupplyPoints = type.SupplyPoints,
            })
            .ToArray();
    }

    /// <summary>
    /// Maps HTTP structure-type requests onto domain inputs.
    /// </summary>
    /// <param name="types">The request structure types.</param>
    /// <returns>The domain inputs.</returns>
    public static IReadOnlyList<StructureTypeInput>? ToStructureTypeInputs(IReadOnlyList<StructureTypeRequest>? types)
    {
        return types?
            .Select(static type => new StructureTypeInput
            {
                Id = type.Id,
                Name = type.Name,
                BuiltinSymbol = type.BuiltinSymbol,
                ClearImage = type.ClearImage,
                ClearPillagedImage = type.ClearPillagedImage,
                IsBuildable = type.IsBuildable,
                IsPillageable = type.IsPillageable,
                IsDestructible = type.IsDestructible,
                Missions = ToMissionInputs(type.Missions),
                CampaignPoints = type.CampaignPoints,
                SupplyPoints = type.SupplyPoints,
                PillageSupplyPoints = type.PillageSupplyPoints,
                DestroySupplyPoints = type.DestroySupplyPoints,
            })
            .ToArray();
    }

    /// <summary>
    /// Maps HTTP item-objective requests onto domain inputs.
    /// </summary>
    public static IReadOnlyList<ItemObjectiveTypeInput>? ToItemObjectiveTypeInputs(
        IReadOnlyList<ItemObjectiveTypeRequest>? types)
    {
        return types?
            .Select(static type => new ItemObjectiveTypeInput
            {
                Id = type.Id,
                Name = type.Name,
                IsHiddenUntilFound = type.IsHiddenUntilFound,
                Placement = type.Placement,
                AllowOnSpawn = type.AllowOnSpawn,
                BuiltinSymbol = type.BuiltinSymbol,
                Color = type.Color,
                ClearImage = type.ClearImage,
                CampaignPoints = type.CampaignPoints,
                FlavorText = type.FlavorText,
                SpecialRuleIds = type.SpecialRuleIds,
                Choices = type.Choices?
                    .Select(static choice => new ItemObjectiveChoiceInput
                    {
                        Id = choice.Id,
                        Name = choice.Name,
                        Results = choice.Results?
                            .Select(static result => new ItemObjectiveChoiceResultInput
                            {
                                Id = result.Id,
                                FlavorText = result.FlavorText,
                                NewStateKey = result.NewStateKey,
                                DestroyItem = result.DestroyItem,
                                ReplacementItemTypeId = result.ReplacementItemTypeId,
                                GrantedPrivateObjectiveTypeId = result.GrantedPrivateObjectiveTypeId,
                            })
                            .ToArray(),
                    })
                    .ToArray(),
            })
            .ToArray();
    }

    /// <summary>
    /// Maps HTTP special-rule requests onto domain inputs.
    /// </summary>
    public static IReadOnlyList<SpecialRuleInput>? ToSpecialRuleInputs(IReadOnlyList<SpecialRuleRequest>? rules)
    {
        return rules?
            .Select(static rule => new SpecialRuleInput
            {
                Id = rule.Id,
                Name = rule.Name,
                Text = rule.Text,
            })
            .ToArray();
    }

    /// <summary>
    /// Maps HTTP force-status requests onto domain inputs.
    /// </summary>
    public static IReadOnlyList<ForceStatusInput>? ToForceStatusInputs(IReadOnlyList<ForceStatusRequest>? statuses)
    {
        return statuses?
            .Select(static status => new ForceStatusInput
            {
                Id = status.Id,
                Name = status.Name,
                Effects = status.Effects,
                EnableTrigger = status.EnableTrigger,
                ClearTrigger = status.ClearTrigger,
            })
            .ToArray();
    }

    /// <summary>
    /// Maps HTTP private-objective requests onto domain inputs.
    /// </summary>
    public static IReadOnlyList<PrivateObjectiveTypeInput>? ToPrivateObjectiveTypeInputs(
        IReadOnlyList<PrivateObjectiveTypeRequest>? types)
    {
        return types?
            .Select(static type => new PrivateObjectiveTypeInput
            {
                Id = type.Id,
                Name = type.Name,
                Description = type.Description,
                CampaignPoints = type.CampaignPoints,
                AllowedHolderKinds = type.AllowedHolderKinds,
                ScoringKind = type.ScoringKind,
                AutomaticKind = type.AutomaticKind,
                RequiredCount = type.RequiredCount,
                StructureTypeId = type.StructureTypeId,
                TerritoryIds = type.TerritoryIds,
            })
            .ToArray();
    }

    /// <summary>
    /// Maps HTTP public-objective requests onto domain inputs.
    /// </summary>
    public static IReadOnlyList<PublicObjectiveTypeInput>? ToPublicObjectiveTypeInputs(
        IReadOnlyList<PublicObjectiveTypeRequest>? types)
    {
        return types?
            .Select(static type => new PublicObjectiveTypeInput
            {
                Id = type.Id,
                Name = type.Name,
                Description = type.Description,
                CampaignPoints = type.CampaignPoints,
            })
            .ToArray();
    }

    /// <summary>
    /// Maps a campaign-point standing onto an HTTP response.
    /// </summary>
    public static CampaignPointStandingResponse FromStanding(CampaignPointStandingDetail standing)
    {
        return new CampaignPointStandingResponse
        {
            UserId = standing.UserId,
            Username = standing.Username,
            DisplayName = standing.DisplayName,
            FactionId = standing.FactionId,
            FactionName = standing.FactionName,
            FactionColor = standing.FactionColor,
            HasFlagImage = standing.HasFlagImage,
            AllyGroupName = standing.AllyGroupName,
            TerritoryAndStructurePoints = standing.TerritoryAndStructurePoints,
            BattlesWonPoints = standing.BattlesWonPoints,
            PublicObjectivePoints = standing.PublicObjectivePoints,
            PrivateObjectivePoints = standing.PrivateObjectivePoints,
            OtherPoints = standing.OtherPoints,
            Total = standing.Total,
            HeldItems =
            [
                .. standing.HeldItems.Select(static item => new HeldItemObjectiveResponse
                {
                    TypeId = item.TypeId,
                    Name = item.Name,
                    BuiltinSymbol = item.BuiltinSymbol,
                    Color = item.Color,
                    HasImage = item.HasImage,
                }),
            ],
        };
    }

    /// <summary>
    /// Maps a ranking public-objective leaderboard onto an HTTP response.
    /// </summary>
    public static PublicObjectiveLeaderboardResponse FromLeaderboard(PublicObjectiveLeaderboardDetail board)
    {
        return new PublicObjectiveLeaderboardResponse
        {
            Kind = board.Kind,
            AwardPoints = board.AwardPoints,
            Leaders =
            [
                .. board.Leaders.Select(static leader => new PublicObjectiveLeaderResponse
                {
                    UserId = leader.UserId,
                    Username = leader.Username,
                    DisplayName = leader.DisplayName,
                    Rank = leader.Rank,
                    Metric = leader.Metric,
                    TieBreakMetric = leader.TieBreakMetric,
                    AwardsPoints = leader.AwardsPoints,
                }),
            ],
        };
    }

    internal static MissionInput[]? ToMissionInputs(IReadOnlyList<MissionRequest>? missions)
    {
        return missions?
            .Select(static mission => new MissionInput
            {
                Id = mission.Id,
                Name = mission.Name,
                Url = mission.Url,
                ClearFile = mission.ClearFile,
                ResultQuestions = mission.ResultQuestions?
                    .Select(static question => new MissionResultQuestionInput
                    {
                        Id = question.Id,
                        Prompt = question.Prompt,
                        Kind = question.Kind,
                        BattlePoints = question.BattlePoints,
                        CampaignPoints = question.CampaignPoints,
                    })
                    .ToArray(),
                IsAttackerDefender = mission.IsAttackerDefender,
                HasArmyPointsAdvantage = mission.HasArmyPointsAdvantage,
                ArmyPointsAdvantageSide = mission.ArmyPointsAdvantageSide,
                ArmyPointsAdvantageIsPercent = mission.ArmyPointsAdvantageIsPercent,
                ArmyPointsAdvantageAmount = mission.ArmyPointsAdvantageAmount,
                HasSupplyPointsAdvantage = mission.HasSupplyPointsAdvantage,
                SupplyPointsAdvantageSide = mission.SupplyPointsAdvantageSide,
                SupplyPointsAdvantageAmount = mission.SupplyPointsAdvantageAmount,
            })
            .ToArray();
    }

    internal static MissionResponse FromMission(MissionDetail mission)
    {
        ArgumentNullException.ThrowIfNull(mission);
        return new MissionResponse
        {
            Id = mission.Id,
            Name = mission.Name,
            Url = mission.Url,
            HasFile = mission.HasFile,
            FileName = mission.FileName,
            ResultQuestions =
            [
                .. mission.ResultQuestions.Select(static question => new MissionResultQuestionResponse
                {
                    Id = question.Id,
                    Prompt = question.Prompt,
                    Kind = question.Kind,
                    BattlePoints = question.BattlePoints,
                    CampaignPoints = question.CampaignPoints,
                }),
            ],
            IsAttackerDefender = mission.IsAttackerDefender,
            HasArmyPointsAdvantage = mission.HasArmyPointsAdvantage,
            ArmyPointsAdvantageSide = mission.ArmyPointsAdvantageSide,
            ArmyPointsAdvantageIsPercent = mission.ArmyPointsAdvantageIsPercent,
            ArmyPointsAdvantageAmount = mission.ArmyPointsAdvantageAmount,
            HasSupplyPointsAdvantage = mission.HasSupplyPointsAdvantage,
            SupplyPointsAdvantageSide = mission.SupplyPointsAdvantageSide,
            SupplyPointsAdvantageAmount = mission.SupplyPointsAdvantageAmount,
        };
    }

    /// <summary>
    /// Maps HTTP link requests onto domain inputs.
    /// </summary>
    /// <param name="links">The request links.</param>
    /// <returns>The domain inputs.</returns>
    public static IReadOnlyList<CampaignLinkInput>? ToLinkInputs(IReadOnlyList<LinkRequest>? links)
    {
        return links?
            .Select(static link => new CampaignLinkInput { Label = link.Label, Url = link.Url })
            .ToArray();
    }

    /// <summary>
    /// Maps HTTP schedule fields onto a domain schedule input.
    /// </summary>
    /// <param name="request">The save request.</param>
    /// <returns>The domain schedule input.</returns>
    public static CampaignScheduleInput ToScheduleInput(SaveCampaignRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new CampaignScheduleInput
        {
            TimeZoneId = request.TimeZoneId,
            StartsAtLocal = request.StartsAtLocal,
            RoundCount = request.RoundCount,
            RoundLengthAmount = request.RoundLengthAmount,
            RoundLengthUnit = request.RoundLengthUnit,
            Phases = request.Phases?
                .Select(static phase => new RoundPhaseInput
                {
                    Kind = phase.Kind,
                    DurationAmount = phase.DurationAmount,
                    DurationUnit = phase.DurationUnit,
                })
                .ToArray(),
            RoundEscalations = request.RoundEscalations?
                .Select(static row => new RoundArmyEscalationInput
                {
                    RoundNumber = row.RoundNumber,
                    MaxArmyPoints = row.MaxArmyPoints,
                    FreeSupplyPoints = row.FreeSupplyPoints,
                    FreeCharacterCount = row.FreeCharacterCount,
                })
                .ToArray(),
        };
    }
}

/// <summary>
/// Request to replace overlay territories and adjacencies.
/// </summary>
public sealed class SaveMapGraphRequest
{
    /// <summary>Gets the last observed campaign revision.</summary>
    public required int Revision { get; init; }

    /// <summary>Gets the territories.</summary>
    public required IReadOnlyList<TerritoryRequest> Territories { get; init; }

    /// <summary>Gets the adjacencies.</summary>
    public IReadOnlyList<AdjacencyRequest>? Adjacencies { get; init; }

    /// <summary>Gets manager-assigned item objective placements.</summary>
    public IReadOnlyList<ItemObjectivePlacementRequest>? ItemObjectivePlacements { get; init; }
}

/// <summary>
/// Territory fields in a map-graph save request.
/// </summary>
public sealed class TerritoryRequest
{
    /// <summary>Gets the territory identifier, when the client already assigned one.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets the unique display number used when no name is set.</summary>
    public int DisplayNumber { get; init; }

    /// <summary>Gets the optional unique name.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the polygon vertices.</summary>
    public required IReadOnlyList<MapPointRequest> Polygon { get; init; }

    /// <summary>Gets the campaign terrain type identifier.</summary>
    public Guid? TerrainTypeId { get; init; }

    /// <summary>Gets the optional campaign structure type identifier.</summary>
    public Guid? StructureTypeId { get; init; }

    /// <summary>Gets the structure condition when a structure is present.</summary>
    public string? StructureCondition { get; init; }

    /// <summary>Gets the optional overlay color as #RRGGBB.</summary>
    public string? OverlayColor { get; init; }

    /// <summary>Gets the owning faction, or null when the territory is neutral.</summary>
    public Guid? OwnerFactionId { get; init; }

    /// <summary>Gets the spawn-location faction, if any.</summary>
    public Guid? SpawnFactionId { get; init; }
}

/// <summary>
/// A normalized map coordinate in a request.
/// </summary>
public sealed class MapPointRequest
{
    /// <summary>Gets the horizontal coordinate.</summary>
    public double X { get; init; }

    /// <summary>Gets the vertical coordinate.</summary>
    public double Y { get; init; }
}

/// <summary>
/// Adjacency fields in a map-graph save request.
/// </summary>
public sealed class AdjacencyRequest
{
    /// <summary>Gets the adjacency identifier, when the client already assigned one.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets one territory identifier.</summary>
    public required Guid TerritoryAId { get; init; }

    /// <summary>Gets the other territory identifier.</summary>
    public required Guid TerritoryBId { get; init; }

    /// <summary>Gets Generated or Manual.</summary>
    public string? Origin { get; init; }

    /// <summary>Gets the editor arrow marker X coordinate.</summary>
    public double MarkerX { get; init; }

    /// <summary>Gets the editor arrow marker Y coordinate.</summary>
    public double MarkerY { get; init; }
}

/// <summary>
/// Member-visible overlay graph for a campaign map.
/// </summary>
public sealed class MapGraphResponse
{
    /// <summary>Gets the campaign identifier.</summary>
    public required Guid CampaignId { get; init; }

    /// <summary>Gets the optimistic concurrency revision.</summary>
    public required int Revision { get; init; }

    /// <summary>Gets whether the current user can edit the map graph.</summary>
    public required bool CanManage { get; init; }

    /// <summary>Gets the overlay territories.</summary>
    public required IReadOnlyList<TerritoryResponse> Territories { get; init; }

    /// <summary>Gets the explicit adjacencies.</summary>
    public required IReadOnlyList<AdjacencyResponse> Adjacencies { get; init; }

    /// <summary>Gets manager-assigned item objective placements.</summary>
    public IReadOnlyList<ItemObjectivePlacementResponse> ItemObjectivePlacements { get; init; } = [];
}

/// <summary>
/// A manager-assigned launch location for a Placed item objective.
/// </summary>
public sealed class ItemObjectivePlacementResponse
{
    /// <summary>Gets the item objective type.</summary>
    public required Guid TypeId { get; init; }

    /// <summary>Gets the territory.</summary>
    public required Guid TerritoryId { get; init; }
}

/// <summary>
/// A manager-assigned launch location in a save request.
/// </summary>
public sealed class ItemObjectivePlacementRequest
{
    /// <summary>Gets the item objective type.</summary>
    public required Guid TypeId { get; init; }

    /// <summary>Gets the territory.</summary>
    public required Guid TerritoryId { get; init; }
}

/// <summary>
/// A territory in a map-graph response.
/// </summary>
public sealed class TerritoryResponse
{
    /// <summary>Gets the territory identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the unique display number used when no name is set.</summary>
    public required int DisplayNumber { get; init; }

    /// <summary>Gets the optional unique name.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the polygon vertices.</summary>
    public required IReadOnlyList<MapPointResponse> Polygon { get; init; }

    /// <summary>Gets the campaign terrain type identifier.</summary>
    public required Guid TerrainTypeId { get; init; }

    /// <summary>Gets the optional campaign structure type identifier.</summary>
    public Guid? StructureTypeId { get; init; }

    /// <summary>Gets the structure condition when a structure is present.</summary>
    public string? StructureCondition { get; init; }

    /// <summary>Gets the optional overlay color as #RRGGBB.</summary>
    public string? OverlayColor { get; init; }

    /// <summary>Gets the owning faction, or null when the territory is neutral.</summary>
    public Guid? OwnerFactionId { get; init; }

    /// <summary>Gets the spawn-location faction, if any.</summary>
    public Guid? SpawnFactionId { get; init; }
}

/// <summary>
/// A normalized map coordinate in a response.
/// </summary>
public sealed class MapPointResponse
{
    /// <summary>Gets the horizontal coordinate.</summary>
    public required double X { get; init; }

    /// <summary>Gets the vertical coordinate.</summary>
    public required double Y { get; init; }
}

/// <summary>
/// An explicit adjacency in a map-graph response.
/// </summary>
public sealed class AdjacencyResponse
{
    /// <summary>Gets the adjacency identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets one territory identifier.</summary>
    public required Guid TerritoryAId { get; init; }

    /// <summary>Gets the other territory identifier.</summary>
    public required Guid TerritoryBId { get; init; }

    /// <summary>Gets Generated or Manual.</summary>
    public required string Origin { get; init; }

    /// <summary>Gets the editor arrow marker X coordinate.</summary>
    public required double MarkerX { get; init; }

    /// <summary>Gets the editor arrow marker Y coordinate.</summary>
    public required double MarkerY { get; init; }
}
