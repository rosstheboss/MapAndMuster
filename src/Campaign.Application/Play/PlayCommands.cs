namespace Campaign.Application.Play;

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
/// Play-page payload. Secret drafts belonging to other players are omitted.
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

    /// <summary>Gets whether the viewer is a player.</summary>
    public required bool IsParticipant { get; init; }

    /// <summary>Gets whether the viewer may chat in the public log.</summary>
    public required bool CanChat { get; init; }

    /// <summary>Gets current members who may be tagged in chat.</summary>
    public required IReadOnlyList<Campaigns.CampaignLogMemberDetail> MentionableMembers { get; init; }

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

    /// <summary>Gets forces on the map.</summary>
    public required IReadOnlyList<PlayForceDetail> Forces { get; init; }

    /// <summary>Gets the viewer's drafts.</summary>
    public required IReadOnlyList<PlayDraftDetail> MyDrafts { get; init; }

    /// <summary>Gets revealed or own submitted orders.</summary>
    public required IReadOnlyList<PlayOrderDetail> Orders { get; init; }

    /// <summary>Gets other players' commitment flags without their orders.</summary>
    public required IReadOnlyList<PlayCommitmentDetail> Commitments { get; init; }

    /// <summary>Gets battles in the current battle window.</summary>
    public required IReadOnlyList<PlayBattleDetail> Battles { get; init; }

    /// <summary>Gets resolved-action and battle facts. Unrevealed secret orders are omitted.</summary>
    public required IReadOnlyList<PlayLogEntryDetail> Log { get; init; }

    /// <summary>Gets players who still need a faction.</summary>
    public required IReadOnlyList<string> PlayersMissingFaction { get; init; }
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

    /// <summary>Gets adjacent eligible move destinations.</summary>
    public required IReadOnlyList<Guid> MoveTargets { get; init; }

    /// <summary>Gets player-submittable action kinds available for this force.</summary>
    public required IReadOnlyList<string> AvailableActions { get; init; }
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

/// <summary>A battle on the play page.</summary>
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

    /// <summary>Gets whether the viewer must retreat.</summary>
    public required bool NeedsRetreat { get; init; }

    /// <summary>Gets eligible retreat destinations.</summary>
    public required IReadOnlyList<Guid> RetreatTargets { get; init; }
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
}
