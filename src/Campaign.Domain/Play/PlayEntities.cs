using Campaign.Domain.Campaigns;

namespace Campaign.Domain.Play;

/// <summary>
/// One stored action or battle window in a launched campaign.
/// </summary>
public sealed class PhaseWindow
{
    /// <summary>
    /// Initializes a phase window.
    /// </summary>
    public PhaseWindow(
        Guid id,
        int roundNumber,
        int phaseNumber,
        RoundPhaseKind kind,
        int plannedAmount,
        DurationUnit plannedUnit,
        DateTimeOffset startsUtc,
        DateTimeOffset endsUtc,
        PhaseWindowStatus status)
    {
        Id = id;
        RoundNumber = roundNumber;
        PhaseNumber = phaseNumber;
        Kind = kind;
        PlannedAmount = plannedAmount;
        PlannedUnit = plannedUnit;
        StartsUtc = startsUtc;
        EndsUtc = endsUtc;
        Status = status;
    }

    /// <summary>Gets the window identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the 1-based round.</summary>
    public int RoundNumber { get; }

    /// <summary>Gets the 1-based phase index in the round.</summary>
    public int PhaseNumber { get; }

    /// <summary>Gets the phase kind.</summary>
    public RoundPhaseKind Kind { get; }

    /// <summary>Gets the originally configured duration amount.</summary>
    public int PlannedAmount { get; }

    /// <summary>Gets the originally configured duration unit.</summary>
    public DurationUnit PlannedUnit { get; }

    /// <summary>Gets when the window opens, in UTC.</summary>
    public DateTimeOffset StartsUtc { get; }

    /// <summary>Gets when the window closes, in UTC.</summary>
    public DateTimeOffset EndsUtc { get; }

    /// <summary>Gets the window status.</summary>
    public PhaseWindowStatus Status { get; }

    /// <summary>
    /// Returns a copy with updated timing or status.
    /// </summary>
    public PhaseWindow With(
        DateTimeOffset? startsUtc = null,
        DateTimeOffset? endsUtc = null,
        PhaseWindowStatus? status = null)
    {
        return new PhaseWindow(
            Id,
            RoundNumber,
            PhaseNumber,
            Kind,
            PlannedAmount,
            PlannedUnit,
            startsUtc ?? StartsUtc,
            endsUtc ?? EndsUtc,
            status ?? Status);
    }
}

/// <summary>
/// A player-controlled force on the campaign map.
/// </summary>
public sealed class CampaignForce
{
    /// <summary>
    /// Initializes a force.
    /// </summary>
    public CampaignForce(
        Guid id,
        Guid controllerUserId,
        Guid factionId,
        Guid territoryId,
        bool inBattle,
        string? statusName = null)
    {
        Id = id;
        ControllerUserId = controllerUserId;
        FactionId = factionId;
        TerritoryId = territoryId;
        InBattle = inBattle;
        StatusName = string.IsNullOrWhiteSpace(statusName) ? null : statusName.Trim();
    }

    /// <summary>Gets the force identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the controlling player's user identifier.</summary>
    public Guid ControllerUserId { get; }

    /// <summary>Gets the force faction.</summary>
    public Guid FactionId { get; }

    /// <summary>Gets the current territory.</summary>
    public Guid TerritoryId { get; }

    /// <summary>Gets whether the force is locked in an unresolved battle.</summary>
    public bool InBattle { get; }

    /// <summary>Gets the current status name, or null when Normal.</summary>
    public string? StatusName { get; }

    /// <summary>
    /// Returns a copy with a new location or battle flag. Status is preserved.
    /// </summary>
    public CampaignForce With(Guid? territoryId = null, bool? inBattle = null)
    {
        return new CampaignForce(
            Id,
            ControllerUserId,
            FactionId,
            territoryId ?? TerritoryId,
            inBattle ?? InBattle,
            StatusName);
    }

    /// <summary>
    /// Returns a copy with a replacement status. Null is Normal.
    /// </summary>
    public CampaignForce WithStatus(string? statusName)
    {
        return new CampaignForce(Id, ControllerUserId, FactionId, TerritoryId, InBattle, statusName);
    }
}

/// <summary>
/// The latest saved player intent for one force in one action window.
/// </summary>
public sealed class OrderDraft
{
    /// <summary>
    /// Initializes a draft.
    /// </summary>
    public OrderDraft(
        Guid windowId,
        Guid forceId,
        ActionKind kind,
        Guid? targetTerritoryId,
        Guid? structureTypeId,
        DateTimeOffset updatedUtc)
    {
        WindowId = windowId;
        ForceId = forceId;
        Kind = kind;
        TargetTerritoryId = targetTerritoryId;
        StructureTypeId = structureTypeId;
        UpdatedUtc = updatedUtc;
    }

    /// <summary>Gets the action window.</summary>
    public Guid WindowId { get; }

    /// <summary>Gets the force.</summary>
    public Guid ForceId { get; }

    /// <summary>Gets the drafted action.</summary>
    public ActionKind Kind { get; }

    /// <summary>Gets the destination or build/pillage target territory.</summary>
    public Guid? TargetTerritoryId { get; }

    /// <summary>Gets the structure type for Build.</summary>
    public Guid? StructureTypeId { get; }

    /// <summary>Gets when the draft was last saved, in UTC.</summary>
    public DateTimeOffset UpdatedUtc { get; }
}

/// <summary>
/// An immutable submitted order. Later corrections append a new row.
/// </summary>
public sealed class OrderSubmission
{
    /// <summary>
    /// Initializes a submission.
    /// </summary>
    public OrderSubmission(
        Guid id,
        Guid windowId,
        Guid forceId,
        ActionKind kind,
        Guid? targetTerritoryId,
        Guid? structureTypeId,
        OrderSource source,
        DateTimeOffset submittedUtc,
        Guid actorUserId)
    {
        Id = id;
        WindowId = windowId;
        ForceId = forceId;
        Kind = kind;
        TargetTerritoryId = targetTerritoryId;
        StructureTypeId = structureTypeId;
        Source = source;
        SubmittedUtc = submittedUtc;
        ActorUserId = actorUserId;
    }

    /// <summary>Gets the submission identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the action window.</summary>
    public Guid WindowId { get; }

    /// <summary>Gets the force.</summary>
    public Guid ForceId { get; }

    /// <summary>Gets the submitted action.</summary>
    public ActionKind Kind { get; }

    /// <summary>Gets the destination or structure target.</summary>
    public Guid? TargetTerritoryId { get; }

    /// <summary>Gets the structure type for Build.</summary>
    public Guid? StructureTypeId { get; }

    /// <summary>Gets how the order entered history.</summary>
    public OrderSource Source { get; }

    /// <summary>Gets when the order was submitted, in UTC.</summary>
    public DateTimeOffset SubmittedUtc { get; }

    /// <summary>Gets the user who submitted or whose deadline produced the order.</summary>
    public Guid ActorUserId { get; }
}

/// <summary>
/// A player's declaration that required orders for a window are ready.
/// </summary>
public sealed class PlayerCommitment
{
    /// <summary>
    /// Initializes a commitment.
    /// </summary>
    public PlayerCommitment(Guid windowId, Guid userId, DateTimeOffset committedUtc)
    {
        WindowId = windowId;
        UserId = userId;
        CommittedUtc = committedUtc;
    }

    /// <summary>Gets the action window.</summary>
    public Guid WindowId { get; }

    /// <summary>Gets the player.</summary>
    public Guid UserId { get; }

    /// <summary>Gets when the player committed, in UTC.</summary>
    public DateTimeOffset CommittedUtc { get; }
}

/// <summary>
/// An engagement created by resolved campaign actions.
/// </summary>
public sealed class CampaignBattle
{
    /// <summary>
    /// Initializes a battle.
    /// </summary>
    public CampaignBattle(
        Guid id,
        Guid territoryId,
        Guid sourceWindowId,
        Guid? battleWindowId,
        BattleStatus status,
        IReadOnlyList<Guid> participantForceIds,
        Guid? winnerForceId,
        bool isDraw,
        DateTimeOffset createdUtc,
        int? winnerScore = null,
        int? loserScore = null)
    {
        ArgumentNullException.ThrowIfNull(participantForceIds);
        Id = id;
        TerritoryId = territoryId;
        SourceWindowId = sourceWindowId;
        BattleWindowId = battleWindowId;
        Status = status;
        ParticipantForceIds = participantForceIds;
        WinnerForceId = winnerForceId;
        IsDraw = isDraw;
        CreatedUtc = createdUtc;
        WinnerScore = winnerScore;
        LoserScore = loserScore;
    }

    /// <summary>Gets the battle identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the territory where the engagement occurs.</summary>
    public Guid TerritoryId { get; }

    /// <summary>Gets the action window that created the battle.</summary>
    public Guid SourceWindowId { get; }

    /// <summary>Gets the battle window that collects results, when assigned.</summary>
    public Guid? BattleWindowId { get; }

    /// <summary>Gets the battle status.</summary>
    public BattleStatus Status { get; }

    /// <summary>Gets participating force identifiers.</summary>
    public IReadOnlyList<Guid> ParticipantForceIds { get; }

    /// <summary>Gets the winning force when finalized.</summary>
    public Guid? WinnerForceId { get; }

    /// <summary>Gets whether the authoritative result is a draw.</summary>
    public bool IsDraw { get; }

    /// <summary>Gets when the battle was created, in UTC.</summary>
    public DateTimeOffset CreatedUtc { get; }

    /// <summary>Gets the winner's reported tabletop or converted battle score.</summary>
    public int? WinnerScore { get; }

    /// <summary>Gets the loser's reported tabletop or converted battle score.</summary>
    public int? LoserScore { get; }

    /// <summary>
    /// Returns a copy with an updated result or window assignment.
    /// </summary>
    public CampaignBattle With(
        Guid? battleWindowId = null,
        BattleStatus? status = null,
        Guid? winnerForceId = null,
        bool? isDraw = null,
        bool assignWindow = false,
        bool clearWinner = false,
        int? winnerScore = null,
        int? loserScore = null,
        bool assignScores = false)
    {
        return new CampaignBattle(
            Id,
            TerritoryId,
            SourceWindowId,
            assignWindow ? battleWindowId : battleWindowId ?? BattleWindowId,
            status ?? Status,
            ParticipantForceIds,
            clearWinner ? null : winnerForceId ?? WinnerForceId,
            isDraw ?? IsDraw,
            CreatedUtc,
            assignScores ? winnerScore : winnerScore ?? WinnerScore,
            assignScores ? loserScore : loserScore ?? LoserScore);
    }
}

/// <summary>
/// One participant's structured battle report. History is append-only.
/// </summary>
public sealed class BattleResultSubmission
{
    /// <summary>
    /// Initializes a result submission.
    /// </summary>
    public BattleResultSubmission(
        Guid id,
        Guid battleId,
        Guid submitterUserId,
        Guid? winnerForceId,
        bool isDraw,
        Guid? acceptedSubmissionId,
        DateTimeOffset submittedUtc,
        int? winnerScore = null,
        int? loserScore = null)
    {
        Id = id;
        BattleId = battleId;
        SubmitterUserId = submitterUserId;
        WinnerForceId = winnerForceId;
        IsDraw = isDraw;
        AcceptedSubmissionId = acceptedSubmissionId;
        SubmittedUtc = submittedUtc;
        WinnerScore = winnerScore;
        LoserScore = loserScore;
    }

    /// <summary>Gets the submission identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the battle.</summary>
    public Guid BattleId { get; }

    /// <summary>Gets the submitting participant.</summary>
    public Guid SubmitterUserId { get; }

    /// <summary>Gets the reported winner, when not a draw.</summary>
    public Guid? WinnerForceId { get; }

    /// <summary>Gets whether the reporter called a draw.</summary>
    public bool IsDraw { get; }

    /// <summary>Gets the opponent submission this report accepted, if any.</summary>
    public Guid? AcceptedSubmissionId { get; }

    /// <summary>Gets when the report was submitted, in UTC.</summary>
    public DateTimeOffset SubmittedUtc { get; }

    /// <summary>Gets the reported winner score used for differential campaign points.</summary>
    public int? WinnerScore { get; }

    /// <summary>Gets the reported loser score used for differential campaign points.</summary>
    public int? LoserScore { get; }
}

/// <summary>
/// Structure occupancy and condition for one territory during play.
/// </summary>
public sealed class TerritoryStructureState
{
    /// <summary>
    /// Initializes structure state.
    /// </summary>
    public TerritoryStructureState(Guid territoryId, Guid? structureTypeId, StructureCondition condition)
    {
        TerritoryId = territoryId;
        StructureTypeId = structureTypeId;
        Condition = condition;
    }

    /// <summary>Gets the territory.</summary>
    public Guid TerritoryId { get; }

    /// <summary>Gets the structure type, if any.</summary>
    public Guid? StructureTypeId { get; }

    /// <summary>Gets the condition.</summary>
    public StructureCondition Condition { get; }
}

/// <summary>
/// An append-only log fact recorded for chat, revealed orders, or battle resolution.
/// Unresolved secret orders are never written here. Private chat entries are stored
/// with audience metadata and must be filtered before they are returned to a client.
/// </summary>
public sealed class PlayLogEntry
{
    /// <summary>
    /// Initializes a play-log entry.
    /// </summary>
    public PlayLogEntry(
        Guid id,
        DateTimeOffset occurredUtc,
        PlayLogKind kind,
        Guid? windowId,
        Guid? forceId,
        Guid? actorUserId,
        Guid? territoryId,
        Guid? targetTerritoryId,
        Guid? battleId,
        ActionKind? actionKind,
        IReadOnlyList<Guid> relatedForceIds,
        string? message = null,
        string? actorDisplayName = null,
        ChatChannelKind chatChannelKind = ChatChannelKind.Public,
        Guid? chatTargetUserId = null,
        Guid? chatTargetFactionId = null,
        Guid? chatTargetAllyGroupId = null,
        string? chatTargetLabel = null)
    {
        ArgumentNullException.ThrowIfNull(relatedForceIds);
        Id = id;
        OccurredUtc = occurredUtc;
        Kind = kind;
        WindowId = windowId;
        ForceId = forceId;
        ActorUserId = actorUserId;
        TerritoryId = territoryId;
        TargetTerritoryId = targetTerritoryId;
        BattleId = battleId;
        ActionKind = actionKind;
        RelatedForceIds = relatedForceIds;
        Message = message;
        ActorDisplayName = actorDisplayName;
        ChatChannelKind = kind == PlayLogKind.PlayerChat ? chatChannelKind : ChatChannelKind.Public;
        ChatTargetUserId = ChatChannelKind == ChatChannelKind.Direct ? chatTargetUserId : null;
        ChatTargetFactionId = ChatChannelKind == ChatChannelKind.Faction ? chatTargetFactionId : null;
        ChatTargetAllyGroupId = ChatChannelKind == ChatChannelKind.AllyGroup ? chatTargetAllyGroupId : null;
        ChatTargetLabel = ChatChannelKind == ChatChannelKind.Public ? null : chatTargetLabel;
    }

    /// <summary>Gets the entry identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets when the fact was recorded, in UTC.</summary>
    public DateTimeOffset OccurredUtc { get; }

    /// <summary>Gets the fact kind.</summary>
    public PlayLogKind Kind { get; }

    /// <summary>Gets the related phase window, when any.</summary>
    public Guid? WindowId { get; }

    /// <summary>Gets the related force, when any.</summary>
    public Guid? ForceId { get; }

    /// <summary>Gets the acting player, when any.</summary>
    public Guid? ActorUserId { get; }

    /// <summary>Gets the related territory, when any.</summary>
    public Guid? TerritoryId { get; }

    /// <summary>Gets the destination territory, when any.</summary>
    public Guid? TargetTerritoryId { get; }

    /// <summary>Gets the related battle, when any.</summary>
    public Guid? BattleId { get; }

    /// <summary>Gets the resolved or attempted action, when any.</summary>
    public ActionKind? ActionKind { get; }

    /// <summary>Gets related forces, such as battle participants.</summary>
    public IReadOnlyList<Guid> RelatedForceIds { get; }

    /// <summary>Gets the chat text for a player message.</summary>
    public string? Message { get; }

    /// <summary>Gets the actor's display name snapshotted when a chat message was posted.</summary>
    public string? ActorDisplayName { get; }

    /// <summary>Gets the chat audience. Game-log facts are always public.</summary>
    public ChatChannelKind ChatChannelKind { get; }

    /// <summary>Gets the other member for a direct message.</summary>
    public Guid? ChatTargetUserId { get; }

    /// <summary>Gets the faction for a faction channel message.</summary>
    public Guid? ChatTargetFactionId { get; }

    /// <summary>Gets the ally group for an ally-group channel message.</summary>
    public Guid? ChatTargetAllyGroupId { get; }

    /// <summary>Gets a snapshot label for the private channel, such as a username or faction name.</summary>
    public string? ChatTargetLabel { get; }

    /// <summary>Gets whether this entry is a private member chat rather than a public log fact.</summary>
    public bool IsPrivateChat => Kind == PlayLogKind.PlayerChat && ChatChannelKind != ChatChannelKind.Public;

    /// <summary>Gets whether the application substituted or interrupted a player choice.</summary>
    public bool IsSystemAdjustment => Kind is PlayLogKind.DeadlineDraftSubmitted
        or PlayLogKind.MissingOrderHold
        or PlayLogKind.InvalidOrderHold
        or PlayLogKind.ConflictingBuildHold
        or PlayLogKind.DefaultRetreat
        or PlayLogKind.UnresolvedBattleHeldOpen
        or PlayLogKind.ForcesRejoined;
}

/// <summary>
/// A retreat after a lost battle.
/// </summary>
public sealed class RetreatOrder
{
    /// <summary>
    /// Initializes a retreat.
    /// </summary>
    public RetreatOrder(Guid id, Guid battleId, Guid forceId, Guid targetTerritoryId, bool isDefault, DateTimeOffset submittedUtc)
    {
        Id = id;
        BattleId = battleId;
        ForceId = forceId;
        TargetTerritoryId = targetTerritoryId;
        IsDefault = isDefault;
        SubmittedUtc = submittedUtc;
    }

    /// <summary>Gets the retreat identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the battle.</summary>
    public Guid BattleId { get; }

    /// <summary>Gets the retreating force.</summary>
    public Guid ForceId { get; }

    /// <summary>Gets the destination.</summary>
    public Guid TargetTerritoryId { get; }

    /// <summary>Gets whether this was the spawn-fallback default.</summary>
    public bool IsDefault { get; }

    /// <summary>Gets when the retreat was recorded, in UTC.</summary>
    public DateTimeOffset SubmittedUtc { get; }
}

/// <summary>
/// Ownership and structure facts for one territory at the start of an action window.
/// </summary>
public sealed class TerritorySnapshot
{
    /// <summary>
    /// Initializes a territory snapshot.
    /// </summary>
    public TerritorySnapshot(
        Guid territoryId,
        Guid? ownerFactionId,
        Guid? structureTypeId,
        string? structureName,
        StructureCondition condition)
    {
        TerritoryId = territoryId;
        OwnerFactionId = ownerFactionId;
        StructureTypeId = structureTypeId;
        StructureName = structureName;
        Condition = condition;
    }

    /// <summary>Gets the territory.</summary>
    public Guid TerritoryId { get; }

    /// <summary>Gets the controlling faction, or null when neutral.</summary>
    public Guid? OwnerFactionId { get; }

    /// <summary>Gets the structure type, if any.</summary>
    public Guid? StructureTypeId { get; }

    /// <summary>Gets the structure display name, if any.</summary>
    public string? StructureName { get; }

    /// <summary>Gets the structure condition.</summary>
    public StructureCondition Condition { get; }
}

/// <summary>
/// Map and force facts captured before an action window resolved, so debug can re-resolve.
/// </summary>
public sealed class ActionWindowSnapshot
{
    /// <summary>
    /// Initializes a snapshot.
    /// </summary>
    public ActionWindowSnapshot(
        Guid windowId,
        IReadOnlyList<CampaignForce> forces,
        IReadOnlyList<TerritoryStructureState> structures,
        IReadOnlyList<Guid> brokenAllyFactionIds,
        IReadOnlyList<TerritorySnapshot> territories,
        IReadOnlyList<CampaignItemObjective>? itemObjectives = null)
    {
        ArgumentNullException.ThrowIfNull(forces);
        ArgumentNullException.ThrowIfNull(structures);
        ArgumentNullException.ThrowIfNull(brokenAllyFactionIds);
        ArgumentNullException.ThrowIfNull(territories);
        WindowId = windowId;
        Forces = forces;
        Structures = structures;
        BrokenAllyFactionIds = brokenAllyFactionIds;
        Territories = territories;
        ItemObjectives = itemObjectives ?? [];
    }

    /// <summary>Gets the action window.</summary>
    public Guid WindowId { get; }

    /// <summary>Gets forces before resolution.</summary>
    public IReadOnlyList<CampaignForce> Forces { get; }

    /// <summary>Gets structure state before resolution.</summary>
    public IReadOnlyList<TerritoryStructureState> Structures { get; }

    /// <summary>Gets broken-alliance factions before resolution.</summary>
    public IReadOnlyList<Guid> BrokenAllyFactionIds { get; }

    /// <summary>Gets territory ownership and structures before resolution.</summary>
    public IReadOnlyList<TerritorySnapshot> Territories { get; }

    /// <summary>Gets item objectives before resolution.</summary>
    public IReadOnlyList<CampaignItemObjective> ItemObjectives { get; }
}
