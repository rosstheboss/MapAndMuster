using MapAndMuster.Application.Play;

namespace MapAndMuster.Api.Contracts;

/// <summary>
/// Campaign-page play payload. Other players' drafts are omitted.
/// </summary>
public sealed class CampaignPlayResponse
{
    /// <summary>Gets the campaign identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the campaign name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the revision.</summary>
    public required int Revision { get; init; }

    /// <summary>Gets whether the viewer can manage.</summary>
    public required bool CanManage { get; init; }

    /// <summary>Gets whether the viewer may enter campaign debug mode.</summary>
    public required bool CanDebug { get; init; }

    /// <summary>Gets whether a debug session is active.</summary>
    public required bool IsDebugActive { get; init; }

    /// <summary>Gets the user currently in debug mode, if any.</summary>
    public Guid? DebugActorUserId { get; init; }

    /// <summary>Gets whether the viewer is a player.</summary>
    public required bool IsParticipant { get; init; }

    /// <summary>Gets whether the viewer may chat in the log.</summary>
    public required bool CanChat { get; init; }

    /// <summary>Gets whether the viewer is an administrator currently in debug mode on this campaign.</summary>
    public bool CanInspectPrivateChat { get; init; }

    /// <summary>Gets current members who may be tagged in chat.</summary>
    public required IReadOnlyList<CampaignLogMemberResponse> MentionableMembers { get; init; }

    /// <summary>Gets compose targets: public, members, factions, and ally groups.</summary>
    public IReadOnlyList<ChatChannelResponse> ChatChannels { get; init; } = [];

    /// <summary>Gets the lifecycle status.</summary>
    public required string Status { get; init; }

    /// <summary>Gets the current round.</summary>
    public int? CurrentRound { get; init; }

    /// <summary>Gets the current phase number.</summary>
    public int? CurrentPhaseNumber { get; init; }

    /// <summary>Gets the current phase kind.</summary>
    public string? CurrentPhaseKind { get; init; }

    /// <summary>Gets the current phase label.</summary>
    public string? CurrentPhaseLabel { get; init; }

    /// <summary>Gets when the current phase opened.</summary>
    public DateTimeOffset? CurrentPhaseStartsUtc { get; init; }

    /// <summary>Gets when the current phase closes.</summary>
    public DateTimeOffset? CurrentPhaseEndsUtc { get; init; }

    /// <summary>Gets the current window identifier.</summary>
    public Guid? CurrentWindowId { get; init; }

    /// <summary>Gets whether a map image exists.</summary>
    public required bool HasMap { get; init; }

    /// <summary>Gets the viewer's faction.</summary>
    public Guid? FactionId { get; init; }

    /// <summary>Gets whether the viewer still needs to pick a faction.</summary>
    public required bool CanChooseFaction { get; init; }

    /// <summary>Gets whether the viewer is committed in the open action window.</summary>
    public required bool IsCommitted { get; init; }

    /// <summary>Gets the round count.</summary>
    public required int RoundCount { get; init; }

    /// <summary>Gets the minimum allowed round count after launch.</summary>
    public required int MinRoundCount { get; init; }

    /// <summary>Gets remaining windows that a manager may lengthen.</summary>
    public required IReadOnlyList<PlayWindowResponse> RemainingWindows { get; init; }

    /// <summary>Gets factions.</summary>
    public required IReadOnlyList<FactionResponse> Factions { get; init; }

    /// <summary>Gets structure types.</summary>
    public required IReadOnlyList<StructureTypeResponse> StructureTypes { get; init; }

    /// <summary>Gets visible item objectives. Hidden items are omitted unless the viewer holds them or is in debug mode.</summary>
    public IReadOnlyList<PlayItemObjectiveResponse> ItemObjectives { get; init; } = [];

    /// <summary>Gets factions that left their ally group through Backstab.</summary>
    public IReadOnlyList<Guid> BrokenAllyFactionIds { get; init; } = [];

    /// <summary>Gets current campaign-point standings for players.</summary>
    public IReadOnlyList<CampaignPointStandingResponse> Standings { get; init; } = [];

    /// <summary>Gets current top-five leaders for enabled ranking public objectives.</summary>
    public IReadOnlyList<PublicObjectiveLeaderboardResponse> PublicObjectiveLeaderboards { get; init; } = [];

    /// <summary>Gets assigned private objectives visible to the viewer.</summary>
    public IReadOnlyList<PrivateObjectiveAssignmentResponse> PrivateObjectives { get; init; } = [];

    /// <summary>Gets public unclaimed private-objective counts.</summary>
    public IReadOnlyList<PrivateObjectiveUnclaimedCountResponse> PrivateObjectiveUnclaimedCounts { get; init; } = [];

    /// <summary>Gets reusable special rules.</summary>
    public IReadOnlyList<SpecialRuleResponse> SpecialRules { get; init; } = [];

    /// <summary>Gets configured force statuses other than Normal.</summary>
    public IReadOnlyList<ForceStatusResponse> ForceStatuses { get; init; } = [];

    /// <summary>Gets campaign points awarded to the winner when differential scoring is off.</summary>
    public int PointsPerBattleWon { get; init; }

    /// <summary>Gets campaign points awarded to each participant of a draw.</summary>
    public int PointsPerBattleDraw { get; init; }

    /// <summary>Gets whether battle campaign points use score differential.</summary>
    public bool UseDifferentialBattleScoring { get; init; }

    /// <summary>Gets forces on the map.</summary>
    public required IReadOnlyList<PlayForceResponse> Forces { get; init; }

    /// <summary>Gets the viewer's drafts.</summary>
    public required IReadOnlyList<PlayDraftResponse> MyDrafts { get; init; }

    /// <summary>Gets revealed or own submitted orders.</summary>
    public required IReadOnlyList<PlayOrderResponse> Orders { get; init; }

    /// <summary>Gets every force's draft while the viewer is in debug mode.</summary>
    public required IReadOnlyList<PlayDraftResponse> DebugDrafts { get; init; }

    /// <summary>Gets commitment flags without secret orders.</summary>
    public required IReadOnlyList<PlayCommitmentResponse> Commitments { get; init; }

    /// <summary>Gets battles in the current battle window.</summary>
    public required IReadOnlyList<PlayBattleResponse> Battles { get; init; }

    /// <summary>Gets resolved-action and battle facts. Unrevealed secret orders are omitted.</summary>
    public required IReadOnlyList<PlayLogEntryResponse> Log { get; init; }

    /// <summary>Gets players who still need a faction.</summary>
    public required IReadOnlyList<string> PlayersMissingFaction { get; init; }
}

/// <summary>A remaining phase window.</summary>
public sealed class PlayWindowResponse
{
    /// <summary>Gets the window identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the round.</summary>
    public required int RoundNumber { get; init; }

    /// <summary>Gets the phase number.</summary>
    public required int PhaseNumber { get; init; }

    /// <summary>Gets the kind name.</summary>
    public required string Kind { get; init; }

    /// <summary>Gets the label.</summary>
    public required string Label { get; init; }

    /// <summary>Gets when the window ends.</summary>
    public required DateTimeOffset EndsUtc { get; init; }
}

/// <summary>A force on the play map.</summary>
public sealed class PlayForceResponse
{
    /// <summary>Gets the force identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the controller user identifier.</summary>
    public required Guid ControllerUserId { get; init; }

    /// <summary>Gets the controller username, when known.</summary>
    public string? ControllerUsername { get; init; }

    /// <summary>Gets the faction.</summary>
    public required Guid FactionId { get; init; }

    /// <summary>Gets the territory.</summary>
    public required Guid TerritoryId { get; init; }

    /// <summary>Gets whether the force is the viewer's.</summary>
    public required bool IsMine { get; init; }

    /// <summary>Gets whether the force is locked in battle.</summary>
    public required bool InBattle { get; init; }

    /// <summary>Gets the current status name, or null when Normal.</summary>
    public string? StatusName { get; init; }

    /// <summary>Gets tabletop effect text for the current status.</summary>
    public string? StatusEffects { get; init; }

    /// <summary>Gets adjacent eligible move destinations.</summary>
    public required IReadOnlyList<Guid> MoveTargets { get; init; }

    /// <summary>Gets two-territory Move hops when Crusaders applies.</summary>
    public IReadOnlyList<PlayMoveHopResponse> MoveHops { get; init; } = [];

    /// <summary>Gets player-submittable action kinds available for this force.</summary>
    public required IReadOnlyList<string> AvailableActions { get; init; }

    /// <summary>Gets the force subfaction, when chosen.</summary>
    public string? Subfaction { get; init; }

    /// <summary>Gets whether this force may Move through an intermediate territory.</summary>
    public bool CanMoveTwoTerritories { get; init; }

    /// <summary>Gets whether Pillage may destroy the structure in one action.</summary>
    public bool CanDestroyImmediately { get; init; }

    /// <summary>Gets whether this force may declare Extra Black Powder on a battle result.</summary>
    public bool CanUseExtraBlackPowder { get; init; }

    /// <summary>Gets whether this force may declare Magical Supply rerolls on a battle result.</summary>
    public bool CanUseMagicalSupply { get; init; }

    /// <summary>Gets whether a hidden relic is in an adjacent territory.</summary>
    public bool HiddenRelicNearby { get; init; }

    /// <summary>Gets tabletop or campaign reminders from assigned special rules.</summary>
    public IReadOnlyList<string> BattleReminders { get; init; } = [];
}

/// <summary>A two-territory Move hop.</summary>
public sealed class PlayMoveHopResponse
{
    /// <summary>Gets the first territory entered.</summary>
    public required Guid ViaTerritoryId { get; init; }

    /// <summary>Gets the intended destination.</summary>
    public required Guid TargetTerritoryId { get; init; }
}

/// <summary>A visible item objective.</summary>
public sealed class PlayItemObjectiveResponse
{
    /// <summary>Gets the instance identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the catalog type.</summary>
    public required Guid TypeId { get; init; }

    /// <summary>Gets the item name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the territory when the item is on the ground.</summary>
    public Guid? TerritoryId { get; init; }

    /// <summary>Gets the carrying force when possessed.</summary>
    public Guid? PossessorForceId { get; init; }

    /// <summary>Gets whether players can see this item.</summary>
    public required bool IsRevealed { get; init; }

    /// <summary>Gets the built-in logo key.</summary>
    public string BuiltinSymbol { get; init; } = "Crown";

    /// <summary>Gets the logo color as #RRGGBB.</summary>
    public string Color { get; init; } = "#C45C26";

    /// <summary>Gets whether a custom logo image is stored.</summary>
    public bool HasImage { get; init; }

    /// <summary>Gets flavor text when the viewer holds the item or is staff.</summary>
    public string? FlavorText { get; init; }

    /// <summary>Gets the optional state label after a choice.</summary>
    public string? StateKey { get; init; }

    /// <summary>Gets whether the item was destroyed.</summary>
    public bool IsDestroyed { get; init; }

    /// <summary>Gets the resolved choice, when one was already picked.</summary>
    public Guid? ResolvedChoiceId { get; init; }

    /// <summary>Gets holder choices when the viewer may resolve one.</summary>
    public IReadOnlyList<ItemObjectiveChoiceResponse> Choices { get; init; } = [];
}

/// <summary>The viewer's draft.</summary>
public sealed class PlayDraftResponse
{
    /// <summary>Gets the force.</summary>
    public required Guid ForceId { get; init; }

    /// <summary>Gets the action kind.</summary>
    public required string Kind { get; init; }

    /// <summary>Gets the destination.</summary>
    public Guid? TargetTerritoryId { get; init; }

    /// <summary>Gets the structure type.</summary>
    public Guid? StructureTypeId { get; init; }

    /// <summary>Gets the first hop for a two-territory Move.</summary>
    public Guid? ViaTerritoryId { get; init; }

    /// <summary>Gets whether a Pillage should destroy the structure immediately.</summary>
    public bool DestroyImmediately { get; init; }
}

/// <summary>A submitted or revealed order.</summary>
public sealed class PlayOrderResponse
{
    /// <summary>Gets the force.</summary>
    public required Guid ForceId { get; init; }

    /// <summary>Gets the action kind.</summary>
    public required string Kind { get; init; }

    /// <summary>Gets the destination.</summary>
    public Guid? TargetTerritoryId { get; init; }

    /// <summary>Gets whether this order is visible because the window resolved.</summary>
    public required bool IsRevealed { get; init; }
}

/// <summary>Commitment status for a required player.</summary>
public sealed class PlayCommitmentResponse
{
    /// <summary>Gets the user identifier.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets the username, when known.</summary>
    public string? Username { get; init; }

    /// <summary>Gets whether they are committed.</summary>
    public required bool IsCommitted { get; init; }
}

/// <summary>A battle on the campaign page.</summary>
public sealed class PlayBattleResponse
{
    /// <summary>Gets the battle identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the territory.</summary>
    public required Guid TerritoryId { get; init; }

    /// <summary>Gets the status name.</summary>
    public required string Status { get; init; }

    /// <summary>Gets participating force identifiers.</summary>
    public required IReadOnlyList<Guid> ParticipantForceIds { get; init; }

    /// <summary>Gets forces in the current tabletop pairing.</summary>
    public IReadOnlyList<Guid> ActiveForceIds { get; init; } = [];

    /// <summary>Gets forces waiting for a later pairing.</summary>
    public IReadOnlyList<Guid> WaitingForceIds { get; init; } = [];

    /// <summary>Gets forces that must currently report a tabletop result.</summary>
    public IReadOnlyList<Guid> ReportingForceIds { get; init; } = [];

    /// <summary>Gets whether every remaining force ran and nobody won.</summary>
    public bool IsNoContest { get; init; }

    /// <summary>Gets whether this is an ephemeral GM ringer battle.</summary>
    public bool IsRinger { get; init; }

    /// <summary>Gets the faction the ringer fights as.</summary>
    public Guid? RingerFactionId { get; init; }

    /// <summary>Gets whether the viewer participates.</summary>
    public required bool IsMine { get; init; }

    /// <summary>Gets the viewer's current submission, if any.</summary>
    public PlayBattleSubmissionResponse? MySubmission { get; init; }

    /// <summary>Gets the opponent submission when the viewer may accept it.</summary>
    public PlayBattleSubmissionResponse? OpponentSubmission { get; init; }

    /// <summary>Gets the winner when finalized.</summary>
    public Guid? WinnerForceId { get; init; }

    /// <summary>Gets whether the result is a draw.</summary>
    public required bool IsDraw { get; init; }

    /// <summary>Gets the recorded winner score when known.</summary>
    public int? WinnerScore { get; init; }

    /// <summary>Gets the recorded loser score when known.</summary>
    public int? LoserScore { get; init; }

    /// <summary>Gets whether the viewer must retreat.</summary>
    public required bool NeedsRetreat { get; init; }

    /// <summary>Gets eligible retreat destinations.</summary>
    public required IReadOnlyList<Guid> RetreatTargets { get; init; }

    /// <summary>Gets whether the viewer may surrender this engagement.</summary>
    public bool CanSurrender { get; init; }

    /// <summary>Gets questions to ask when reporting this battle's result.</summary>
    public IReadOnlyList<MissionResultQuestionResponse> ResultQuestions { get; init; } = [];

    /// <summary>Gets the viewer's current spendable supply, when participating.</summary>
    public int? ViewerSupplyPoints { get; init; }

    /// <summary>Gets current supply for each participating force.</summary>
    public IReadOnlyList<PlayBattleForceSupplyResponse> ForceSupplies { get; init; } = [];

    /// <summary>Gets whether a staff member may confirm the outstanding report.</summary>
    public bool CanStaffConfirm { get; init; }

    /// <summary>Gets the mission assigned to this battle, when one was chosen.</summary>
    public MissionResponse? Mission { get; init; }

    /// <summary>Gets the attacking force when the mission uses attacker/defender roles.</summary>
    public Guid? AttackerForceId { get; init; }

    /// <summary>Gets the defending force when the mission uses attacker/defender roles.</summary>
    public Guid? DefenderForceId { get; init; }
}

/// <summary>A battle result the viewer is allowed to see.</summary>
public sealed class PlayBattleSubmissionResponse
{
    /// <summary>Gets the submitter.</summary>
    public required Guid SubmitterUserId { get; init; }

    /// <summary>Gets the reported winner.</summary>
    public Guid? WinnerForceId { get; init; }

    /// <summary>Gets whether the report is a draw.</summary>
    public required bool IsDraw { get; init; }

    /// <summary>Gets the reported winner score.</summary>
    public int? WinnerScore { get; init; }

    /// <summary>Gets the reported loser score.</summary>
    public int? LoserScore { get; init; }

    /// <summary>Gets structured per-force reports, when submitted.</summary>
    public IReadOnlyList<BattleParticipantReportResponse> Reports { get; init; } = [];
}

/// <summary>One force's structured battle report.</summary>
public sealed class BattleParticipantReportResponse
{
    /// <summary>Gets the reported force.</summary>
    public required Guid ForceId { get; init; }

    /// <summary>Gets tabletop victory points.</summary>
    public int VictoryPoints { get; init; }

    /// <summary>Gets the army size in points used in the battle.</summary>
    public int ArmyPoints { get; init; }

    /// <summary>Gets battle points converted from victory points.</summary>
    public int DifferentialBattlePoints { get; init; }

    /// <summary>Gets bonus battle points from the mission.</summary>
    public int BonusBattlePoints { get; init; }

    /// <summary>Gets how many supply-costing units this force fielded.</summary>
    public int SupplyCostingUnitCount { get; init; }

    /// <summary>Gets whether Extra Black Powder was used this battle.</summary>
    public bool UsedExtraBlackPowder { get; init; }

    /// <summary>Gets leftover composition supply used as Magical Supply rerolls.</summary>
    public int MagicalSupplyRerolls { get; init; }

    /// <summary>Gets optional pasted army-list text.</summary>
    public string? ArmyListText { get; init; }

    /// <summary>Gets the game system selected for list verification.</summary>
    public string? ArmyListGameSystem { get; init; }

    /// <summary>Gets the army builder selected for automatic supply parsing.</summary>
    public string? ArmyListBuilder { get; init; }

    /// <summary>Gets optional per-category supply amounts.</summary>
    public IReadOnlyList<ArmyListSupplyCategoryResponse> SupplyCategories { get; init; } = [];

    /// <summary>Gets whether the reporter killed the opponent's general.</summary>
    public bool KilledEnemyGeneral { get; init; }

    /// <summary>Gets whether the reporter destroyed the enemy supply line.</summary>
    public bool DestroyedEnemySupplyLine { get; init; }

    /// <summary>Gets answers to extra mission questions.</summary>
    public IReadOnlyList<BattleQuestionAnswerResponse> Answers { get; init; } = [];
}

/// <summary>One answer on a stored battle report.</summary>
public sealed class BattleQuestionAnswerResponse
{
    /// <summary>Gets the catalog question.</summary>
    public required Guid QuestionId { get; init; }

    /// <summary>Gets the true/false answer, when applicable.</summary>
    public bool? BooleanValue { get; init; }

    /// <summary>Gets the reported battle-point amount, when applicable.</summary>
    public int? BattlePointsValue { get; init; }
}

/// <summary>One army-composition category on a stored battle report.</summary>
public sealed class ArmyListSupplyCategoryResponse
{
    /// <summary>Gets the category label.</summary>
    public required string Name { get; init; }

    /// <summary>Gets how many top-level units were counted.</summary>
    public int UnitCount { get; init; }

    /// <summary>Gets declared supply points for this category.</summary>
    public int SupplyPoints { get; init; }

    /// <summary>Gets whether this category spends supply by default.</summary>
    public bool CostsSupply { get; init; }
}

/// <summary>Supply shown next to a force in a battle to resolve.</summary>
public sealed class PlayBattleForceSupplyResponse
{
    /// <summary>Gets the force.</summary>
    public required Guid ForceId { get; init; }

    /// <summary>Gets the controlling player.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets map-plus-round supply after the split penalty, excluding temporary points.</summary>
    public required int ForceAllowancePoints { get; init; }

    /// <summary>Gets the maximum this force can spend if assigned the player's entire temporary pool.</summary>
    public required int CurrentSupplyPoints { get; init; }

    /// <summary>Gets remaining temporary supply.</summary>
    public required int TemporarySupplyPoints { get; init; }

    /// <summary>Gets map supply from connected owned territories and operational structures.</summary>
    public int MapSupplyPoints { get; init; }

    /// <summary>Gets free supply granted this round.</summary>
    public int RoundFreeSupplyPoints { get; init; }

    /// <summary>Gets supply subtracted because the player currently has split forces.</summary>
    public int SplitPenaltyPoints { get; init; }

    /// <summary>Gets the configured round army-point cap before allied extras.</summary>
    public int RoundMaxArmyPoints { get; init; }

    /// <summary>Gets this force's army-point cap for the tabletop game, including allied extras.</summary>
    public int AlliedArmyPoints { get; init; }

    /// <summary>Gets free characters whose base cost does not count against supply this round.</summary>
    public int FreeCharacterCount { get; init; }

    /// <summary>Gets whether the controlling player currently has split forces.</summary>
    public bool IsSplit { get; init; }
}

/// <summary>Request to save a draft order.</summary>
public sealed class SaveOrderDraftRequest
{
    /// <summary>Gets the last observed campaign revision.</summary>
    public required int Revision { get; init; }

    /// <summary>Gets the force.</summary>
    public required Guid ForceId { get; init; }

    /// <summary>Gets the action kind name.</summary>
    public required string Kind { get; init; }

    /// <summary>Gets the destination territory.</summary>
    public Guid? TargetTerritoryId { get; init; }

    /// <summary>Gets the structure type for Build.</summary>
    public Guid? StructureTypeId { get; init; }

    /// <summary>Gets the first hop for a two-territory Move.</summary>
    public Guid? ViaTerritoryId { get; init; }

    /// <summary>Gets whether a Pillage should destroy the structure immediately.</summary>
    public bool DestroyImmediately { get; init; }

    /// <summary>Gets whether to re-resolve the previous action instead of editing the current window.</summary>
    public bool ReResolvePrevious { get; init; }
}

/// <summary>Request that only carries a revision.</summary>
public sealed class PlayRevisionRequest
{
    /// <summary>Gets the last observed campaign revision.</summary>
    public required int Revision { get; init; }
}

/// <summary>Request for a manager to award or revoke a public campaign objective.</summary>
public sealed class SetPublicObjectiveAwardRequest
{
    /// <summary>Gets the last observed campaign revision.</summary>
    public required int Revision { get; init; }

    /// <summary>Gets the public objective.</summary>
    public required Guid ObjectiveId { get; init; }

    /// <summary>Gets the player receiving or losing the award.</summary>
    public required Guid PlayerUserId { get; init; }

    /// <summary>Gets whether to award (<see langword="true"/>) or revoke (<see langword="false"/>).</summary>
    public required bool Awarded { get; init; }
}

/// <summary>Request for a manager to grant a private objective.</summary>
public sealed class GrantPrivateObjectiveRequest
{
    /// <summary>Gets the last observed campaign revision.</summary>
    public required int Revision { get; init; }

    /// <summary>Gets Player, Faction, or AllyGroup.</summary>
    public required string HolderKind { get; init; }

    /// <summary>Gets the player, faction, or ally-group identifier.</summary>
    public required Guid HolderId { get; init; }

    /// <summary>Gets a specific catalog type, or omit to grant a random still-available entry.</summary>
    public Guid? TypeId { get; init; }
}

/// <summary>Request for a holder to claim a private objective.</summary>
public sealed class ClaimPrivateObjectiveRequest
{
    /// <summary>Gets the last observed campaign revision.</summary>
    public required int Revision { get; init; }

    /// <summary>Gets the assignment.</summary>
    public required Guid AssignmentId { get; init; }
}

/// <summary>Request for a manager to approve or deny a private-objective claim.</summary>
public sealed class ModeratePrivateObjectiveRequest
{
    /// <summary>Gets the last observed campaign revision.</summary>
    public required int Revision { get; init; }

    /// <summary>Gets the assignment.</summary>
    public required Guid AssignmentId { get; init; }

    /// <summary>Gets whether to approve and reveal the objective.</summary>
    public required bool Approved { get; init; }
}

/// <summary>Request for a holder to resolve an item-objective choice.</summary>
public sealed class ResolveItemObjectiveChoiceRequest
{
    /// <summary>Gets the last observed campaign revision.</summary>
    public required int Revision { get; init; }

    /// <summary>Gets the item instance.</summary>
    public required Guid ItemId { get; init; }

    /// <summary>Gets the configured choice.</summary>
    public required Guid ChoiceId { get; init; }
}

/// <summary>Request to submit or override a battle result.</summary>
public sealed class SubmitBattleResultRequest
{
    /// <summary>Gets the last observed campaign revision.</summary>
    public required int Revision { get; init; }

    /// <summary>Gets the battle.</summary>
    public required Guid BattleId { get; init; }

    /// <summary>Gets the winning force, when not a draw.</summary>
    public Guid? WinnerForceId { get; init; }

    /// <summary>Gets whether the result is a draw.</summary>
    public bool IsDraw { get; init; }

    /// <summary>Gets the winner's tabletop or converted battle score.</summary>
    public int? WinnerScore { get; init; }

    /// <summary>Gets the loser's tabletop or converted battle score.</summary>
    public int? LoserScore { get; init; }

    /// <summary>Gets structured reports for both participating forces, when used.</summary>
    public IReadOnlyList<BattleParticipantReportRequest>? Reports { get; init; }
}

/// <summary>One force's structured battle report in a submit request.</summary>
public sealed class BattleParticipantReportRequest
{
    /// <summary>Gets the reported force.</summary>
    public required Guid ForceId { get; init; }

    /// <summary>Gets tabletop victory points.</summary>
    public int VictoryPoints { get; init; }

    /// <summary>Gets the army size in points used in the battle.</summary>
    public int ArmyPoints { get; init; }

    /// <summary>Gets battle points converted from victory points.</summary>
    public int DifferentialBattlePoints { get; init; }

    /// <summary>Gets bonus battle points from the mission.</summary>
    public int BonusBattlePoints { get; init; }

    /// <summary>Gets how many supply-costing units this force fielded.</summary>
    public int SupplyCostingUnitCount { get; init; }

    /// <summary>Gets whether Extra Black Powder was used this battle.</summary>
    public bool UsedExtraBlackPowder { get; init; }

    /// <summary>Gets leftover composition supply used as Magical Supply rerolls.</summary>
    public int MagicalSupplyRerolls { get; init; }

    /// <summary>Gets optional pasted army-list text.</summary>
    public string? ArmyListText { get; init; }

    /// <summary>Gets the game system selected for list verification.</summary>
    public string? ArmyListGameSystem { get; init; }

    /// <summary>Gets the army builder selected for automatic supply parsing.</summary>
    public string? ArmyListBuilder { get; init; }

    /// <summary>Gets optional per-category supply amounts.</summary>
    public IReadOnlyList<ArmyListSupplyCategoryRequest>? SupplyCategories { get; init; }

    /// <summary>Gets whether the reporter killed the opponent's general.</summary>
    public bool KilledEnemyGeneral { get; init; }

    /// <summary>Gets whether the reporter destroyed the enemy supply line.</summary>
    public bool DestroyedEnemySupplyLine { get; init; }

    /// <summary>Gets answers to extra mission questions.</summary>
    public IReadOnlyList<BattleQuestionAnswerRequest>? Answers { get; init; }
}

/// <summary>One army-composition category on a battle report.</summary>
public sealed class ArmyListSupplyCategoryRequest
{
    /// <summary>Gets the category label.</summary>
    public required string Name { get; init; }

    /// <summary>Gets how many top-level units were counted.</summary>
    public int UnitCount { get; init; }

    /// <summary>Gets declared supply points for this category.</summary>
    public int SupplyPoints { get; init; }

    /// <summary>Gets whether this category spends supply by default.</summary>
    public bool CostsSupply { get; init; }
}

/// <summary>Request to parse pasted army-list text without submitting a battle result.</summary>
public sealed class ParseArmyListRequest
{
    /// <summary>Gets the game system to parse for.</summary>
    public string? GameSystem { get; init; }

    /// <summary>Gets the selected army builder.</summary>
    public string? Builder { get; init; }

    /// <summary>Gets the pasted army-list text.</summary>
    public string? Text { get; init; }
}

/// <summary>Parsed army points and supply amounts, or a failed parse.</summary>
public sealed class ParseArmyListResponse
{
    /// <summary>Gets whether the text was recognized and amounts were filled.</summary>
    public required bool Parsed { get; init; }

    /// <summary>Gets a player-facing message when parsing was attempted and failed.</summary>
    public string? Message { get; init; }

    /// <summary>Gets army points read from the list header.</summary>
    public int ArmyPoints { get; init; }

    /// <summary>Gets supply-costing units summed from special, rare, and similar categories.</summary>
    public int SupplyCostingUnitCount { get; init; }

    /// <summary>Gets per-category unit counts and default supply amounts.</summary>
    public IReadOnlyList<ArmyListSupplyCategoryResponse> Categories { get; init; } = [];
}

/// <summary>One answer to a mission result question.</summary>
public sealed class BattleQuestionAnswerRequest
{
    /// <summary>Gets the catalog question.</summary>
    public required Guid QuestionId { get; init; }

    /// <summary>Gets the true/false answer, when applicable.</summary>
    public bool? BooleanValue { get; init; }

    /// <summary>Gets the reported battle-point amount, when applicable.</summary>
    public int? BattlePointsValue { get; init; }
}

/// <summary>Request targeting one battle.</summary>
public sealed class BattleActionRequest
{
    /// <summary>Gets the last observed campaign revision.</summary>
    public required int Revision { get; init; }

    /// <summary>Gets the battle.</summary>
    public required Guid BattleId { get; init; }
}

/// <summary>Request to submit a retreat.</summary>
public sealed class SubmitRetreatRequest
{
    /// <summary>Gets the last observed campaign revision.</summary>
    public required int Revision { get; init; }

    /// <summary>Gets the battle.</summary>
    public required Guid BattleId { get; init; }

    /// <summary>Gets the destination.</summary>
    public required Guid TargetTerritoryId { get; init; }
}

/// <summary>Request to extend remaining phases and/or append rounds.</summary>
public sealed class ExtendCampaignScheduleRequest
{
    /// <summary>Gets the last observed campaign revision.</summary>
    public required int Revision { get; init; }

    /// <summary>Gets the desired round count.</summary>
    public required int RoundCount { get; init; }

    /// <summary>Gets extra durations for remaining windows.</summary>
    public IReadOnlyList<PhaseExtensionRequest>? Extensions { get; init; }
}

/// <summary>Extra time for one window.</summary>
public sealed class PhaseExtensionRequest
{
    /// <summary>Gets the window identifier.</summary>
    public required Guid WindowId { get; init; }

    /// <summary>Gets the additional amount.</summary>
    public required int DurationAmount { get; init; }

    /// <summary>Gets the additional unit name.</summary>
    public required string DurationUnit { get; init; }
}

/// <summary>Request to inject an ephemeral GM ringer battle.</summary>
public sealed class InjectRingerBattleRequest
{
    /// <summary>Gets the last observed campaign revision.</summary>
    public required int Revision { get; init; }

    /// <summary>Gets the player force to fight.</summary>
    public required Guid TargetForceId { get; init; }

    /// <summary>Gets the faction the ringer fights as.</summary>
    public required Guid RingerFactionId { get; init; }

    /// <summary>Gets an optional catalog mission.</summary>
    public Guid? MissionId { get; init; }

    /// <summary>Gets whether the player is marked as the defender.</summary>
    public bool PlayerIsDefender { get; init; }
}

/// <summary>Request to choose a faction.</summary>
public sealed class ChooseFactionRequest
{
    /// <summary>Gets the last observed campaign revision.</summary>
    public required int Revision { get; init; }

    /// <summary>Gets the faction.</summary>
    public required Guid FactionId { get; init; }

    /// <summary>Gets the subfaction, when required.</summary>
    public string? Subfaction { get; init; }
}

/// <summary>A public resolved-action or battle fact.</summary>
public sealed class PlayLogEntryResponse
{
    /// <summary>Gets the entry identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets when the fact was recorded, in UTC.</summary>
    public required DateTimeOffset OccurredUtc { get; init; }

    /// <summary>Gets the fact kind name.</summary>
    public required string Kind { get; init; }

    /// <summary>Gets "Campaign" for game events, or the member's display name for chat.</summary>
    public required string Originator { get; init; }

    /// <summary>Gets the chat author's username, when this is member chat.</summary>
    public string? OriginatorUsername { get; init; }

    /// <summary>Gets a player-visible summary or chat body.</summary>
    public required string Summary { get; init; }

    /// <summary>Gets the related territory, when any.</summary>
    public Guid? TerritoryId { get; init; }

    /// <summary>Gets the related force, when any.</summary>
    public Guid? ForceId { get; init; }

    /// <summary>Gets the related battle, when any.</summary>
    public Guid? BattleId { get; init; }

    /// <summary>Gets whether the application substituted or interrupted a player choice.</summary>
    public required bool IsSystemAdjustment { get; init; }

    /// <summary>Gets Public, Direct, Faction, or AllyGroup.</summary>
    public string ChannelKind { get; init; } = "Public";

    /// <summary>Gets the private-channel label, when this is private chat.</summary>
    public string? ChannelLabel { get; init; }

    /// <summary>Gets whether this is a private member chat.</summary>
    public bool IsPrivate { get; init; }
}

/// <summary>
/// Maps play application models onto HTTP contracts.
/// </summary>
public static class PlayResponses
{
    /// <summary>
    /// Maps a play detail. Other players' drafts are already omitted on the source model.
    /// </summary>
    public static CampaignPlayResponse FromDetail(CampaignPlayDetail detail)
    {
        ArgumentNullException.ThrowIfNull(detail);
        return new CampaignPlayResponse
        {
            Id = detail.Id,
            Name = detail.Name,
            Revision = detail.Revision,
            CanManage = detail.CanManage,
            CanDebug = detail.CanDebug,
            IsDebugActive = detail.IsDebugActive,
            DebugActorUserId = detail.DebugActorUserId,
            IsParticipant = detail.IsParticipant,
            CanChat = detail.CanChat,
            CanInspectPrivateChat = detail.CanInspectPrivateChat,
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
            Status = detail.Status,
            CurrentRound = detail.CurrentRound,
            CurrentPhaseNumber = detail.CurrentPhaseNumber,
            CurrentPhaseKind = detail.CurrentPhaseKind,
            CurrentPhaseLabel = detail.CurrentPhaseLabel,
            CurrentPhaseStartsUtc = detail.CurrentPhaseStartsUtc,
            CurrentPhaseEndsUtc = detail.CurrentPhaseEndsUtc,
            CurrentWindowId = detail.CurrentWindowId,
            HasMap = detail.HasMap,
            FactionId = detail.FactionId,
            CanChooseFaction = detail.CanChooseFaction,
            IsCommitted = detail.IsCommitted,
            RoundCount = detail.RoundCount,
            MinRoundCount = detail.MinRoundCount,
            RemainingWindows =
            [
                .. detail.RemainingWindows.Select(static window => new PlayWindowResponse
                {
                    Id = window.Id,
                    RoundNumber = window.RoundNumber,
                    PhaseNumber = window.PhaseNumber,
                    Kind = window.Kind,
                    Label = window.Label,
                    EndsUtc = window.EndsUtc,
                }),
            ],
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
                    TintFlagImage = faction.TintFlagImage,
                    SpecialRuleIds = faction.SpecialRuleIds,
                    SubfactionSpecialRules =
                    [
                        .. faction.SubfactionSpecialRules.Select(static item => new SubfactionSpecialRulesResponse
                        {
                            Name = item.Name,
                            SpecialRuleIds = item.SpecialRuleIds,
                        }),
                    ],
                    SubfactionAppearances =
                    [
                        .. faction.SubfactionAppearances.Select(static item => new SubfactionAppearanceResponse
                        {
                            Name = item.Name,
                            Color = item.Color,
                            FlagSource = item.FlagSource,
                            HasFlagImage = item.HasFlagImage,
                            TintFlagImage = item.TintFlagImage,
                        }),
                    ],
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
                    Missions =
                    [
                        .. type.Missions.Select(static mission => new MissionResponse
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
                        }),
                    ],
                }),
            ],
            ItemObjectives =
            [
                .. detail.ItemObjectives.Select(static item => new PlayItemObjectiveResponse
                {
                    Id = item.Id,
                    TypeId = item.TypeId,
                    Name = item.Name,
                    TerritoryId = item.TerritoryId,
                    PossessorForceId = item.PossessorForceId,
                    IsRevealed = item.IsRevealed,
                    BuiltinSymbol = item.BuiltinSymbol,
                    Color = item.Color,
                    HasImage = item.HasImage,
                    FlavorText = item.FlavorText,
                    StateKey = item.StateKey,
                    IsDestroyed = item.IsDestroyed,
                    ResolvedChoiceId = item.ResolvedChoiceId,
                    Choices =
                    [
                        .. item.Choices.Select(static choice => new ItemObjectiveChoiceResponse
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
            BrokenAllyFactionIds = detail.BrokenAllyFactionIds,
            Standings = [.. detail.Standings.Select(CampaignResponses.FromStanding)],
            PublicObjectiveLeaderboards = [.. detail.PublicObjectiveLeaderboards.Select(CampaignResponses.FromLeaderboard)],
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
            SpecialRules =
            [
                .. detail.SpecialRules.Select(static rule => new SpecialRuleResponse
                {
                    Id = rule.Id,
                    Name = rule.Name,
                    Text = rule.Text,
                }),
            ],
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
            PointsPerBattleWon = detail.PointsPerBattleWon,
            PointsPerBattleDraw = detail.PointsPerBattleDraw,
            UseDifferentialBattleScoring = detail.UseDifferentialBattleScoring,
            Forces =
            [
                .. detail.Forces.Select(static force => new PlayForceResponse
                {
                    Id = force.Id,
                    ControllerUserId = force.ControllerUserId,
                    ControllerUsername = force.ControllerUsername,
                    FactionId = force.FactionId,
                    TerritoryId = force.TerritoryId,
                    IsMine = force.IsMine,
                    InBattle = force.InBattle,
                    StatusName = force.StatusName,
                    StatusEffects = force.StatusEffects,
                    MoveTargets = force.MoveTargets,
                    MoveHops =
                    [
                        .. force.MoveHops.Select(static hop => new PlayMoveHopResponse
                        {
                            ViaTerritoryId = hop.ViaTerritoryId,
                            TargetTerritoryId = hop.TargetTerritoryId,
                        }),
                    ],
                    AvailableActions = force.AvailableActions,
                    Subfaction = force.Subfaction,
                    CanMoveTwoTerritories = force.CanMoveTwoTerritories,
                    CanDestroyImmediately = force.CanDestroyImmediately,
                    CanUseExtraBlackPowder = force.CanUseExtraBlackPowder,
                    CanUseMagicalSupply = force.CanUseMagicalSupply,
                    HiddenRelicNearby = force.HiddenRelicNearby,
                    BattleReminders = force.BattleReminders,
                }),
            ],
            MyDrafts =
            [
                .. detail.MyDrafts.Select(static draft => new PlayDraftResponse
                {
                    ForceId = draft.ForceId,
                    Kind = draft.Kind,
                    TargetTerritoryId = draft.TargetTerritoryId,
                    StructureTypeId = draft.StructureTypeId,
                    ViaTerritoryId = draft.ViaTerritoryId,
                    DestroyImmediately = draft.DestroyImmediately,
                }),
            ],
            Orders =
            [
                .. detail.Orders.Select(static order => new PlayOrderResponse
                {
                    ForceId = order.ForceId,
                    Kind = order.Kind,
                    TargetTerritoryId = order.TargetTerritoryId,
                    IsRevealed = order.IsRevealed,
                }),
            ],
            DebugDrafts =
            [
                .. detail.DebugDrafts.Select(static draft => new PlayDraftResponse
                {
                    ForceId = draft.ForceId,
                    Kind = draft.Kind,
                    TargetTerritoryId = draft.TargetTerritoryId,
                    StructureTypeId = draft.StructureTypeId,
                    ViaTerritoryId = draft.ViaTerritoryId,
                    DestroyImmediately = draft.DestroyImmediately,
                }),
            ],
            Commitments =
            [
                .. detail.Commitments.Select(static item => new PlayCommitmentResponse
                {
                    UserId = item.UserId,
                    Username = item.Username,
                    IsCommitted = item.IsCommitted,
                }),
            ],
            Battles =
            [
                .. detail.Battles.Select(static battle => new PlayBattleResponse
                {
                    Id = battle.Id,
                    TerritoryId = battle.TerritoryId,
                    Status = battle.Status,
                    ParticipantForceIds = battle.ParticipantForceIds,
                    ActiveForceIds = battle.ActiveForceIds,
                    WaitingForceIds = battle.WaitingForceIds,
                    ReportingForceIds = battle.ReportingForceIds,
                    IsNoContest = battle.IsNoContest,
                    IsRinger = battle.IsRinger,
                    RingerFactionId = battle.RingerFactionId,
                    IsMine = battle.IsMine,
                    MySubmission = ToSubmission(battle.MySubmission),
                    OpponentSubmission = ToSubmission(battle.OpponentSubmission),
                    WinnerForceId = battle.WinnerForceId,
                    IsDraw = battle.IsDraw,
                    WinnerScore = battle.WinnerScore,
                    LoserScore = battle.LoserScore,
                    NeedsRetreat = battle.NeedsRetreat,
                    CanSurrender = battle.CanSurrender,
                    RetreatTargets = battle.RetreatTargets,
                    ResultQuestions =
                    [
                        .. battle.ResultQuestions.Select(static question => new MissionResultQuestionResponse
                        {
                            Id = question.Id,
                            Prompt = question.Prompt,
                            Kind = question.Kind,
                            BattlePoints = question.BattlePoints,
                            CampaignPoints = question.CampaignPoints,
                        }),
                    ],
                    ViewerSupplyPoints = battle.ViewerSupplyPoints,
                    ForceSupplies =
                    [
                        .. battle.ForceSupplies.Select(static item => new PlayBattleForceSupplyResponse
                        {
                            ForceId = item.ForceId,
                            UserId = item.UserId,
                            ForceAllowancePoints = item.ForceAllowancePoints,
                            CurrentSupplyPoints = item.CurrentSupplyPoints,
                            TemporarySupplyPoints = item.TemporarySupplyPoints,
                            MapSupplyPoints = item.MapSupplyPoints,
                            RoundFreeSupplyPoints = item.RoundFreeSupplyPoints,
                            SplitPenaltyPoints = item.SplitPenaltyPoints,
                            RoundMaxArmyPoints = item.RoundMaxArmyPoints,
                            AlliedArmyPoints = item.AlliedArmyPoints,
                            FreeCharacterCount = item.FreeCharacterCount,
                            IsSplit = item.IsSplit,
                        }),
                    ],
                    CanStaffConfirm = battle.CanStaffConfirm,
                    Mission = battle.Mission is null ? null : CampaignResponses.FromMission(battle.Mission),
                    AttackerForceId = battle.AttackerForceId,
                    DefenderForceId = battle.DefenderForceId,
                }),
            ],
            Log =
            [
                .. detail.Log.Select(FromLogEntry),
            ],
            PlayersMissingFaction = detail.PlayersMissingFaction,
        };
    }

    /// <summary>
    /// Maps a play-log entry. Private chats are already omitted for unauthorized viewers.
    /// </summary>
    public static PlayLogEntryResponse FromLogEntry(PlayLogEntryDetail item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return new PlayLogEntryResponse
        {
            Id = item.Id,
            OccurredUtc = item.OccurredUtc,
            Kind = item.Kind,
            Originator = item.Originator,
            OriginatorUsername = item.OriginatorUsername,
            Summary = item.Summary,
            TerritoryId = item.TerritoryId,
            ForceId = item.ForceId,
            BattleId = item.BattleId,
            IsSystemAdjustment = item.IsSystemAdjustment,
            ChannelKind = item.ChannelKind,
            ChannelLabel = item.ChannelLabel,
            IsPrivate = item.IsPrivate,
        };
    }

    private static PlayBattleSubmissionResponse? ToSubmission(PlayBattleSubmissionDetail? submission)
    {
        return submission is null
            ? null
            : new PlayBattleSubmissionResponse
            {
                SubmitterUserId = submission.SubmitterUserId,
                WinnerForceId = submission.WinnerForceId,
                IsDraw = submission.IsDraw,
                WinnerScore = submission.WinnerScore,
                LoserScore = submission.LoserScore,
                Reports =
                [
                    .. submission.Reports.Select(static report => new BattleParticipantReportResponse
                    {
                        ForceId = report.ForceId,
                        VictoryPoints = report.VictoryPoints,
                        ArmyPoints = report.ArmyPoints,
                        DifferentialBattlePoints = report.DifferentialBattlePoints,
                        BonusBattlePoints = report.BonusBattlePoints,
                        SupplyCostingUnitCount = report.SupplyCostingUnitCount,
                        UsedExtraBlackPowder = report.UsedExtraBlackPowder,
                        MagicalSupplyRerolls = report.MagicalSupplyRerolls,
                        ArmyListText = report.ArmyListText,
                        ArmyListGameSystem = report.ArmyListGameSystem,
                        ArmyListBuilder = report.ArmyListBuilder,
                        SupplyCategories =
                        [
                            .. report.SupplyCategories.Select(static category => new ArmyListSupplyCategoryResponse
                            {
                                Name = category.Name,
                                UnitCount = category.UnitCount,
                                SupplyPoints = category.SupplyPoints,
                                CostsSupply = category.CostsSupply,
                            }),
                        ],
                        KilledEnemyGeneral = report.KilledEnemyGeneral,
                        DestroyedEnemySupplyLine = report.DestroyedEnemySupplyLine,
                        Answers =
                        [
                            .. report.Answers.Select(static answer => new BattleQuestionAnswerResponse
                            {
                                QuestionId = answer.QuestionId,
                                BooleanValue = answer.BooleanValue,
                                BattlePointsValue = answer.BattlePointsValue,
                            }),
                        ],
                    }),
                ],
            };
    }

    /// <summary>
    /// Maps HTTP battle-report requests onto application inputs.
    /// </summary>
    public static IReadOnlyList<BattleParticipantReportInput>? ToReportInputs(
        IReadOnlyList<BattleParticipantReportRequest>? reports)
    {
        return reports?
            .Select(static report => new BattleParticipantReportInput
            {
                ForceId = report.ForceId,
                VictoryPoints = report.VictoryPoints,
                ArmyPoints = report.ArmyPoints,
                DifferentialBattlePoints = report.DifferentialBattlePoints,
                BonusBattlePoints = report.BonusBattlePoints,
                SupplyCostingUnitCount = report.SupplyCostingUnitCount,
                UsedExtraBlackPowder = report.UsedExtraBlackPowder,
                MagicalSupplyRerolls = report.MagicalSupplyRerolls,
                ArmyListText = report.ArmyListText,
                ArmyListGameSystem = report.ArmyListGameSystem,
                ArmyListBuilder = report.ArmyListBuilder,
                SupplyCategories = report.SupplyCategories?
                    .Select(static category => new ArmyListSupplyCategoryInput
                    {
                        Name = category.Name,
                        UnitCount = category.UnitCount,
                        SupplyPoints = category.SupplyPoints,
                        CostsSupply = category.CostsSupply,
                    })
                    .ToArray(),
                KilledEnemyGeneral = report.KilledEnemyGeneral,
                DestroyedEnemySupplyLine = report.DestroyedEnemySupplyLine,
                Answers = report.Answers?
                    .Select(static answer => new BattleQuestionAnswerInput
                    {
                        QuestionId = answer.QuestionId,
                        BooleanValue = answer.BooleanValue,
                        BattlePointsValue = answer.BattlePointsValue,
                    })
                    .ToArray(),
            })
            .ToArray();
    }

    /// <summary>
    /// Maps a parsed army list onto the HTTP response.
    /// </summary>
    public static ParseArmyListResponse FromParse(ArmyListParseDetail detail)
    {
        ArgumentNullException.ThrowIfNull(detail);
        return new ParseArmyListResponse
        {
            Parsed = detail.Parsed,
            Message = detail.Message,
            ArmyPoints = detail.ArmyPoints,
            SupplyCostingUnitCount = detail.SupplyCostingUnitCount,
            Categories =
            [
                .. detail.Categories.Select(static category => new ArmyListSupplyCategoryResponse
                {
                    Name = category.Name,
                    UnitCount = category.UnitCount,
                    SupplyPoints = category.SupplyPoints,
                    CostsSupply = category.CostsSupply,
                }),
            ],
        };
    }
}
