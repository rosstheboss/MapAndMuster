using MapAndMuster.Application.Campaigns;

namespace MapAndMuster.Application.Play;

/// <summary>
/// Shared revisioned play command.
/// </summary>
public sealed class PlayCommand
{
    /// <summary>Gets the caller.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets whether the caller is an administrator.</summary>
    public required bool IsAdministrator { get; init; }

    /// <summary>Gets the campaign identifier.</summary>
    public required Guid CampaignId { get; init; }

    /// <summary>Gets the last observed revision.</summary>
    public required int ExpectedRevision { get; init; }
}

/// <summary>
/// Command to save a draft order.
/// </summary>
public sealed class SaveOrderDraftCommand
{
    /// <summary>Gets the caller.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets whether the caller is an administrator.</summary>
    public required bool IsAdministrator { get; init; }

    /// <summary>Gets the campaign identifier.</summary>
    public required Guid CampaignId { get; init; }

    /// <summary>Gets the last observed revision.</summary>
    public required int ExpectedRevision { get; init; }

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

/// <summary>
/// Command to submit or override a battle result.
/// </summary>
public sealed class SubmitBattleResultCommand
{
    /// <summary>Gets the caller.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets whether the caller is an administrator.</summary>
    public required bool IsAdministrator { get; init; }

    /// <summary>Gets the campaign identifier.</summary>
    public required Guid CampaignId { get; init; }

    /// <summary>Gets the last observed revision.</summary>
    public required int ExpectedRevision { get; init; }

    /// <summary>Gets the battle.</summary>
    public required Guid BattleId { get; init; }

    /// <summary>Gets the winning force, when not a draw.</summary>
    public Guid? WinnerForceId { get; init; }

    /// <summary>Gets whether the result is a draw.</summary>
    public required bool IsDraw { get; init; }

    /// <summary>Gets the winner's tabletop or converted battle score.</summary>
    public int? WinnerScore { get; init; }

    /// <summary>Gets the loser's tabletop or converted battle score.</summary>
    public int? LoserScore { get; init; }

    /// <summary>Gets structured reports for both participating forces, when used.</summary>
    public IReadOnlyList<BattleParticipantReportInput>? Reports { get; init; }
}

/// <summary>
/// One force's structured battle report in a submit command.
/// </summary>
public sealed class BattleParticipantReportInput
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
    public IReadOnlyList<ArmyListSupplyCategoryInput>? SupplyCategories { get; init; }

    /// <summary>Gets whether the reporter killed the opponent's general.</summary>
    public bool KilledEnemyGeneral { get; init; }

    /// <summary>Gets whether the reporter destroyed the enemy supply line.</summary>
    public bool DestroyedEnemySupplyLine { get; init; }

    /// <summary>Gets answers to extra mission questions.</summary>
    public IReadOnlyList<BattleQuestionAnswerInput>? Answers { get; init; }
}

/// <summary>
/// One army-composition category on a submitted battle report.
/// </summary>
public sealed class ArmyListSupplyCategoryInput
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

/// <summary>
/// One answer to a mission result question.
/// </summary>
public sealed class BattleQuestionAnswerInput
{
    /// <summary>Gets the catalog question.</summary>
    public required Guid QuestionId { get; init; }

    /// <summary>Gets the true/false answer, when applicable.</summary>
    public bool? BooleanValue { get; init; }

    /// <summary>Gets the reported battle-point amount, when applicable.</summary>
    public int? BattlePointsValue { get; init; }
}

/// <summary>
/// Command targeting one battle.
/// </summary>
public sealed class BattleActionCommand
{
    /// <summary>Gets the caller.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets whether the caller is an administrator.</summary>
    public required bool IsAdministrator { get; init; }

    /// <summary>Gets the campaign identifier.</summary>
    public required Guid CampaignId { get; init; }

    /// <summary>Gets the last observed revision.</summary>
    public required int ExpectedRevision { get; init; }

    /// <summary>Gets the battle.</summary>
    public required Guid BattleId { get; init; }
}

/// <summary>
/// Command to submit a retreat.
/// </summary>
public sealed class SubmitRetreatCommand
{
    /// <summary>Gets the caller.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets whether the caller is an administrator.</summary>
    public required bool IsAdministrator { get; init; }

    /// <summary>Gets the campaign identifier.</summary>
    public required Guid CampaignId { get; init; }

    /// <summary>Gets the last observed revision.</summary>
    public required int ExpectedRevision { get; init; }

    /// <summary>Gets the battle.</summary>
    public required Guid BattleId { get; init; }

    /// <summary>Gets the destination.</summary>
    public required Guid TargetTerritoryId { get; init; }
}

/// <summary>
/// Command to extend remaining phases and/or append rounds.
/// </summary>
public sealed class ExtendCampaignScheduleCommand
{
    /// <summary>Gets the caller.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets whether the caller is an administrator.</summary>
    public required bool IsAdministrator { get; init; }

    /// <summary>Gets the campaign identifier.</summary>
    public required Guid CampaignId { get; init; }

    /// <summary>Gets the last observed revision.</summary>
    public required int ExpectedRevision { get; init; }

    /// <summary>Gets the desired round count.</summary>
    public required int RoundCount { get; init; }

    /// <summary>Gets extra durations for remaining windows.</summary>
    public required IReadOnlyList<PhaseExtensionInput> Extensions { get; init; }
}

/// <summary>
/// Extra time for one window.
/// </summary>
public sealed class PhaseExtensionInput
{
    /// <summary>Gets the window identifier.</summary>
    public required Guid WindowId { get; init; }

    /// <summary>Gets the additional amount.</summary>
    public required int DurationAmount { get; init; }

    /// <summary>Gets the additional unit name.</summary>
    public required string DurationUnit { get; init; }
}

/// <summary>
/// Command to inject an ephemeral GM ringer battle.
/// </summary>
public sealed class InjectRingerBattleCommand
{
    /// <summary>Gets the caller.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets whether the caller is an administrator.</summary>
    public required bool IsAdministrator { get; init; }

    /// <summary>Gets the campaign identifier.</summary>
    public required Guid CampaignId { get; init; }

    /// <summary>Gets the last observed revision.</summary>
    public required int ExpectedRevision { get; init; }

    /// <summary>Gets the player force to fight.</summary>
    public required Guid TargetForceId { get; init; }

    /// <summary>Gets the faction the ringer fights as.</summary>
    public required Guid RingerFactionId { get; init; }

    /// <summary>Gets an optional catalog mission.</summary>
    public Guid? MissionId { get; init; }

    /// <summary>Gets whether the player is marked as the defender.</summary>
    public bool PlayerIsDefender { get; init; }
}

/// <summary>
/// Command to choose a faction.
/// </summary>
public sealed class ChooseFactionCommand
{
    /// <summary>Gets the caller.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets the campaign identifier.</summary>
    public required Guid CampaignId { get; init; }

    /// <summary>Gets the last observed revision.</summary>
    public required int ExpectedRevision { get; init; }

    /// <summary>Gets the faction.</summary>
    public required Guid FactionId { get; init; }

    /// <summary>Gets the subfaction, when required.</summary>
    public string? Subfaction { get; init; }
}

/// <summary>
/// Campaign-page play payload. Secret drafts belonging to other players are omitted.
/// </summary>
public sealed class CampaignPlayDetail
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
    public required IReadOnlyList<Campaigns.CampaignLogMemberDetail> MentionableMembers { get; init; }

    /// <summary>Gets compose targets: public, members, factions, and ally groups.</summary>
    public IReadOnlyList<Campaigns.ChatChannelDetail> ChatChannels { get; init; } = [];

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
    public required IReadOnlyList<PlayWindowDetail> RemainingWindows { get; init; }

    /// <summary>Gets factions.</summary>
    public required IReadOnlyList<Campaigns.FactionDetail> Factions { get; init; }

    /// <summary>Gets structure types.</summary>
    public required IReadOnlyList<Campaigns.StructureTypeDetail> StructureTypes { get; init; }

    /// <summary>Gets visible item objectives. Hidden items are omitted unless the viewer holds them or is in debug mode.</summary>
    public IReadOnlyList<PlayItemObjectiveDetail> ItemObjectives { get; init; } = [];

    /// <summary>Gets factions that left their ally group through Backstab.</summary>
    public IReadOnlyList<Guid> BrokenAllyFactionIds { get; init; } = [];

    /// <summary>Gets current campaign-point standings for players.</summary>
    public IReadOnlyList<Campaigns.CampaignPointStandingDetail> Standings { get; init; } = [];

    /// <summary>Gets current top-five leaders for enabled ranking public objectives.</summary>
    public IReadOnlyList<Campaigns.PublicObjectiveLeaderboardDetail> PublicObjectiveLeaderboards { get; init; } = [];

    /// <summary>Gets assigned private objectives visible to the viewer.</summary>
    public IReadOnlyList<Campaigns.PrivateObjectiveAssignmentDetail> PrivateObjectives { get; init; } = [];

    /// <summary>Gets public unclaimed private-objective counts.</summary>
    public IReadOnlyList<Campaigns.PrivateObjectiveUnclaimedCountDetail> PrivateObjectiveUnclaimedCounts { get; init; } = [];

    /// <summary>Gets reusable special rules.</summary>
    public IReadOnlyList<Campaigns.SpecialRuleDetail> SpecialRules { get; init; } = [];

    /// <summary>Gets configured force statuses other than Normal.</summary>
    public IReadOnlyList<Campaigns.ForceStatusDetail> ForceStatuses { get; init; } = [];

    /// <summary>Gets campaign points awarded to the winner when differential scoring is off.</summary>
    public int PointsPerBattleWon { get; init; }

    /// <summary>Gets campaign points awarded to each participant of a draw.</summary>
    public int PointsPerBattleDraw { get; init; }

    /// <summary>Gets whether battle campaign points use score differential.</summary>
    public bool UseDifferentialBattleScoring { get; init; }

    /// <summary>Gets forces on the map.</summary>
    public required IReadOnlyList<PlayForceDetail> Forces { get; init; }

    /// <summary>Gets the viewer's drafts.</summary>
    public required IReadOnlyList<PlayDraftDetail> MyDrafts { get; init; }

    /// <summary>Gets revealed or own submitted orders.</summary>
    public required IReadOnlyList<PlayOrderDetail> Orders { get; init; }

    /// <summary>Gets every force's draft while the viewer is in debug mode. Empty otherwise.</summary>
    public required IReadOnlyList<PlayDraftDetail> DebugDrafts { get; init; }

    /// <summary>Gets other players' commitment flags without their orders.</summary>
    public required IReadOnlyList<PlayCommitmentDetail> Commitments { get; init; }

    /// <summary>Gets battles in the current battle window.</summary>
    public required IReadOnlyList<PlayBattleDetail> Battles { get; init; }

    /// <summary>Gets resolved-action and battle facts. Unrevealed secret orders are omitted.</summary>
    public required IReadOnlyList<PlayLogEntryDetail> Log { get; init; }

    /// <summary>Gets players who still need a faction.</summary>
    public required IReadOnlyList<string> PlayersMissingFaction { get; init; }
}

/// <summary>A visible item objective on the map or carried by a force.</summary>
public sealed class PlayItemObjectiveDetail
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
    public IReadOnlyList<Campaigns.ItemObjectiveChoiceDetail> Choices { get; init; } = [];
}

/// <summary>A remaining phase window.</summary>
public sealed class PlayWindowDetail
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
public sealed class PlayForceDetail
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
    public IReadOnlyList<PlayMoveHopDetail> MoveHops { get; init; } = [];

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
public sealed class PlayMoveHopDetail
{
    /// <summary>Gets the first territory entered.</summary>
    public required Guid ViaTerritoryId { get; init; }

    /// <summary>Gets the intended destination.</summary>
    public required Guid TargetTerritoryId { get; init; }
}

/// <summary>The viewer's draft.</summary>
public sealed class PlayDraftDetail
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
public sealed class PlayOrderDetail
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
public sealed class PlayCommitmentDetail
{
    /// <summary>Gets the user identifier.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets the username, when known.</summary>
    public string? Username { get; init; }

    /// <summary>Gets whether they are committed.</summary>
    public required bool IsCommitted { get; init; }
}

/// <summary>A battle on the campaign page.</summary>
public sealed class PlayBattleDetail
{
    /// <summary>Gets the battle identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the territory.</summary>
    public required Guid TerritoryId { get; init; }

    /// <summary>Gets the status name.</summary>
    public required string Status { get; init; }

    /// <summary>Gets participating force identifiers.</summary>
    public required IReadOnlyList<Guid> ParticipantForceIds { get; init; }

    /// <summary>Gets forces in the current tabletop pairing. Empty means every participant.</summary>
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
    public PlayBattleSubmissionDetail? MySubmission { get; init; }

    /// <summary>Gets the opponent submission when the viewer may accept it.</summary>
    public PlayBattleSubmissionDetail? OpponentSubmission { get; init; }

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
    public IReadOnlyList<MissionResultQuestionDetail> ResultQuestions { get; init; } = [];

    /// <summary>Gets the participant's current spendable supply, when the viewer can see it.</summary>
    public int? ViewerSupplyPoints { get; init; }

    /// <summary>Gets current supply for each participating force.</summary>
    public IReadOnlyList<PlayBattleForceSupplyDetail> ForceSupplies { get; init; } = [];

    /// <summary>Gets whether a staff member may confirm the outstanding report.</summary>
    public bool CanStaffConfirm { get; init; }

    /// <summary>Gets the mission assigned to this battle, when one was chosen.</summary>
    public MissionDetail? Mission { get; init; }

    /// <summary>Gets the attacking force when the mission uses attacker/defender roles.</summary>
    public Guid? AttackerForceId { get; init; }

    /// <summary>Gets the defending force when the mission uses attacker/defender roles.</summary>
    public Guid? DefenderForceId { get; init; }
}

/// <summary>A battle result the viewer is allowed to see.</summary>
public sealed class PlayBattleSubmissionDetail
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
    public IReadOnlyList<BattleParticipantReportDetail> Reports { get; init; } = [];
}

/// <summary>One force's structured battle report in a play response.</summary>
public sealed class BattleParticipantReportDetail
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
    public IReadOnlyList<ArmyListSupplyCategoryDetail> SupplyCategories { get; init; } = [];

    /// <summary>Gets whether the reporter killed the opponent's general.</summary>
    public bool KilledEnemyGeneral { get; init; }

    /// <summary>Gets whether the reporter destroyed the enemy supply line.</summary>
    public bool DestroyedEnemySupplyLine { get; init; }

    /// <summary>Gets answers to extra mission questions.</summary>
    public IReadOnlyList<BattleQuestionAnswerDetail> Answers { get; init; } = [];
}

/// <summary>One answer on a stored battle report.</summary>
public sealed class BattleQuestionAnswerDetail
{
    /// <summary>Gets the catalog question.</summary>
    public required Guid QuestionId { get; init; }

    /// <summary>Gets the true/false answer, when applicable.</summary>
    public bool? BooleanValue { get; init; }

    /// <summary>Gets the reported battle-point amount, when applicable.</summary>
    public int? BattlePointsValue { get; init; }
}

/// <summary>One army-composition category on a stored battle report.</summary>
public sealed class ArmyListSupplyCategoryDetail
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
public sealed class PlayBattleForceSupplyDetail
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

/// <summary>A public resolved-action or battle fact. Unrevealed orders are never included.</summary>
public sealed class PlayLogEntryDetail
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

    /// <summary>Gets Public, Direct, Faction, or AllyGroup for chat; Public for game-log facts.</summary>
    public string ChannelKind { get; init; } = "Public";

    /// <summary>Gets the private-channel label, when this is private chat.</summary>
    public string? ChannelLabel { get; init; }

    /// <summary>Gets whether this is a private member chat.</summary>
    public bool IsPrivate { get; init; }
}

/// <summary>
/// Command for a manager to award or revoke a public campaign objective.
/// </summary>
public sealed class SetPublicObjectiveAwardCommand
{
    /// <summary>Gets the caller.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets whether the caller is an administrator.</summary>
    public required bool IsAdministrator { get; init; }

    /// <summary>Gets the campaign identifier.</summary>
    public required Guid CampaignId { get; init; }

    /// <summary>Gets the last observed revision.</summary>
    public required int ExpectedRevision { get; init; }

    /// <summary>Gets the public objective.</summary>
    public required Guid ObjectiveId { get; init; }

    /// <summary>Gets the player receiving or losing the award.</summary>
    public required Guid PlayerUserId { get; init; }

    /// <summary>Gets whether to award (<see langword="true"/>) or revoke (<see langword="false"/>).</summary>
    public required bool Awarded { get; init; }
}

/// <summary>
/// Command for a manager to grant a still-available private objective.
/// </summary>
public sealed class GrantPrivateObjectiveCommand
{
    /// <summary>Gets the caller.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets whether the caller is an administrator.</summary>
    public required bool IsAdministrator { get; init; }

    /// <summary>Gets the campaign identifier.</summary>
    public required Guid CampaignId { get; init; }

    /// <summary>Gets the last observed revision.</summary>
    public required int ExpectedRevision { get; init; }

    /// <summary>Gets Player, Faction, or AllyGroup.</summary>
    public required string HolderKind { get; init; }

    /// <summary>Gets the player, faction, or ally-group identifier.</summary>
    public required Guid HolderId { get; init; }

    /// <summary>Gets a specific catalog type, or null to grant a random still-available entry.</summary>
    public Guid? TypeId { get; init; }
}

/// <summary>
/// Command for a holder to claim a manual private objective.
/// </summary>
public sealed class ClaimPrivateObjectiveCommand
{
    /// <summary>Gets the caller.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets whether the caller is an administrator.</summary>
    public required bool IsAdministrator { get; init; }

    /// <summary>Gets the campaign identifier.</summary>
    public required Guid CampaignId { get; init; }

    /// <summary>Gets the last observed revision.</summary>
    public required int ExpectedRevision { get; init; }

    /// <summary>Gets the assignment.</summary>
    public required Guid AssignmentId { get; init; }
}

/// <summary>
/// Command for a manager to approve or deny a private-objective claim.
/// </summary>
public sealed class ModeratePrivateObjectiveCommand
{
    /// <summary>Gets the caller.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets whether the caller is an administrator.</summary>
    public required bool IsAdministrator { get; init; }

    /// <summary>Gets the campaign identifier.</summary>
    public required Guid CampaignId { get; init; }

    /// <summary>Gets the last observed revision.</summary>
    public required int ExpectedRevision { get; init; }

    /// <summary>Gets the assignment.</summary>
    public required Guid AssignmentId { get; init; }

    /// <summary>Gets whether to approve and reveal the objective.</summary>
    public required bool Approved { get; init; }
}

/// <summary>
/// Command for a holder to resolve a configured item-objective choice.
/// </summary>
public sealed class ResolveItemObjectiveChoiceCommand
{
    /// <summary>Gets the caller.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets whether the caller is an administrator.</summary>
    public required bool IsAdministrator { get; init; }

    /// <summary>Gets the campaign identifier.</summary>
    public required Guid CampaignId { get; init; }

    /// <summary>Gets the last observed revision.</summary>
    public required int ExpectedRevision { get; init; }

    /// <summary>Gets the item instance.</summary>
    public required Guid ItemId { get; init; }

    /// <summary>Gets the configured choice.</summary>
    public required Guid ChoiceId { get; init; }
}

/// <summary>
/// Command to parse pasted army-list text for supply amounts. Does not mutate campaign state.
/// </summary>
public sealed class ParseArmyListCommand
{
    /// <summary>Gets the caller.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets whether the caller is an administrator.</summary>
    public required bool IsAdministrator { get; init; }

    /// <summary>Gets the campaign identifier.</summary>
    public required Guid CampaignId { get; init; }

    /// <summary>Gets the game system to parse for.</summary>
    public string? GameSystem { get; init; }

    /// <summary>Gets the selected army builder.</summary>
    public string? Builder { get; init; }

    /// <summary>Gets the pasted army-list text.</summary>
    public string? Text { get; init; }
}

/// <summary>
/// Parsed army points and supply amounts, or a failed parse the player must complete by hand.
/// </summary>
public sealed class ArmyListParseDetail
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
    public IReadOnlyList<ArmyListSupplyCategoryDetail> Categories { get; init; } = [];
}
