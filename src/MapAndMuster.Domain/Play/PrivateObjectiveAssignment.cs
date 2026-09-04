using MapAndMuster.Domain.Campaigns;

namespace MapAndMuster.Domain.Play;

/// <summary>
/// Lifecycle of one assigned private objective.
/// </summary>
public enum PrivateObjectiveAssignmentStatus
{
    /// <summary>Assigned and still secret.</summary>
    Assigned = 0,

    /// <summary>A holder submitted a manual claim awaiting manager approval.</summary>
    Claimed = 1,

    /// <summary>Revealed publicly and counting toward standings.</summary>
    Revealed = 2,
}

/// <summary>
/// One assigned private objective instance. Duplicate catalog types are independent assignments.
/// </summary>
public sealed class PrivateObjectiveAssignment
{
    /// <summary>
    /// Initializes an assignment.
    /// </summary>
    public PrivateObjectiveAssignment(
        Guid id,
        Guid typeId,
        PrivateObjectiveHolderKind holderKind,
        Guid holderId,
        PrivateObjectiveScoringKind scoringKind,
        PrivateObjectiveAssignmentStatus status,
        DateTimeOffset assignedUtc,
        DateTimeOffset? claimedUtc = null,
        DateTimeOffset? revealedUtc = null,
        Guid? claimedByUserId = null,
        Guid? approvedByUserId = null,
        Guid? resolvedTargetId = null)
    {
        Id = id;
        TypeId = typeId;
        HolderKind = holderKind;
        HolderId = holderId;
        ScoringKind = scoringKind;
        Status = status;
        AssignedUtc = assignedUtc;
        ClaimedUtc = claimedUtc;
        RevealedUtc = revealedUtc;
        ClaimedByUserId = claimedByUserId;
        ApprovedByUserId = approvedByUserId;
        ResolvedTargetId = resolvedTargetId;
    }

    /// <summary>Gets the assignment identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the catalog type.</summary>
    public Guid TypeId { get; }

    /// <summary>Gets whether the holder is a player, faction, or ally group.</summary>
    public PrivateObjectiveHolderKind HolderKind { get; }

    /// <summary>Gets the player, faction, or ally-group identifier.</summary>
    public Guid HolderId { get; }

    /// <summary>Gets whether scoring is manual or automatic.</summary>
    public PrivateObjectiveScoringKind ScoringKind { get; }

    /// <summary>Gets the current assignment status.</summary>
    public PrivateObjectiveAssignmentStatus Status { get; }

    /// <summary>Gets when the assignment was created, in UTC.</summary>
    public DateTimeOffset AssignedUtc { get; }

    /// <summary>Gets when a manual claim was submitted, in UTC.</summary>
    public DateTimeOffset? ClaimedUtc { get; }

    /// <summary>Gets when the assignment became public, in UTC.</summary>
    public DateTimeOffset? RevealedUtc { get; }

    /// <summary>Gets the player who submitted a manual claim.</summary>
    public Guid? ClaimedByUserId { get; }

    /// <summary>Gets the manager who approved a claim, or the system actor for automatic completion.</summary>
    public Guid? ApprovedByUserId { get; }

    /// <summary>Gets the opponent chosen at assignment for a Random DefeatOpponent criterion.</summary>
    public Guid? ResolvedTargetId { get; }

    /// <summary>Gets whether the assignment still counts as unclaimed for public counts.</summary>
    public bool IsUnclaimed => Status is PrivateObjectiveAssignmentStatus.Assigned or PrivateObjectiveAssignmentStatus.Claimed;

    /// <summary>Gets whether points currently count in standings for an in-progress campaign.</summary>
    public bool CountsDuringPlay => Status == PrivateObjectiveAssignmentStatus.Revealed;

    /// <summary>
    /// Returns a copy with updated claim or reveal state.
    /// </summary>
    public PrivateObjectiveAssignment With(
        PrivateObjectiveAssignmentStatus? status = null,
        DateTimeOffset? claimedUtc = null,
        DateTimeOffset? revealedUtc = null,
        Guid? claimedByUserId = null,
        Guid? approvedByUserId = null,
        bool clearClaim = false)
    {
        return new PrivateObjectiveAssignment(
            Id,
            TypeId,
            HolderKind,
            HolderId,
            ScoringKind,
            status ?? Status,
            AssignedUtc,
            clearClaim ? null : claimedUtc ?? ClaimedUtc,
            revealedUtc ?? RevealedUtc,
            clearClaim ? null : claimedByUserId ?? ClaimedByUserId,
            approvedByUserId ?? ApprovedByUserId,
            ResolvedTargetId);
    }
}

/// <summary>
/// An append-only record that a structure was destroyed during play.
/// </summary>
public sealed class StructureDestructionFact
{
    /// <summary>
    /// Initializes a destruction fact.
    /// </summary>
    public StructureDestructionFact(
        Guid id,
        Guid territoryId,
        Guid structureTypeId,
        Guid actorFactionId,
        Guid actorUserId,
        DateTimeOffset destroyedUtc)
    {
        Id = id;
        TerritoryId = territoryId;
        StructureTypeId = structureTypeId;
        ActorFactionId = actorFactionId;
        ActorUserId = actorUserId;
        DestroyedUtc = destroyedUtc;
    }

    /// <summary>Gets the fact identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the territory where the structure was destroyed.</summary>
    public Guid TerritoryId { get; }

    /// <summary>Gets the destroyed structure type.</summary>
    public Guid StructureTypeId { get; }

    /// <summary>Gets the faction of the destroying force.</summary>
    public Guid ActorFactionId { get; }

    /// <summary>Gets the player who controlled the destroying force.</summary>
    public Guid ActorUserId { get; }

    /// <summary>Gets when the structure was destroyed, in UTC.</summary>
    public DateTimeOffset DestroyedUtc { get; }
}

/// <summary>
/// An append-only record that a force gained or lost a named status.
/// </summary>
public sealed class ForceStatusChangeFact
{
    /// <summary>
    /// Initializes a status-change fact.
    /// </summary>
    public ForceStatusChangeFact(
        Guid id,
        Guid forceId,
        Guid factionId,
        Guid controllerUserId,
        Guid? statusTypeId,
        string? previousStatusName,
        string? nextStatusName,
        Guid? actorForceId,
        Guid? actorFactionId,
        Guid? actorUserId,
        DateTimeOffset occurredUtc,
        Guid? previousStatusTypeId = null)
    {
        Id = id;
        ForceId = forceId;
        FactionId = factionId;
        ControllerUserId = controllerUserId;
        StatusTypeId = statusTypeId;
        PreviousStatusName = previousStatusName;
        NextStatusName = nextStatusName;
        ActorForceId = actorForceId;
        ActorFactionId = actorFactionId;
        ActorUserId = actorUserId;
        OccurredUtc = occurredUtc;
        PreviousStatusTypeId = previousStatusTypeId;
    }

    /// <summary>Gets the fact identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the force whose status changed.</summary>
    public Guid ForceId { get; }

    /// <summary>Gets the force's faction.</summary>
    public Guid FactionId { get; }

    /// <summary>Gets the player who controls the force.</summary>
    public Guid ControllerUserId { get; }

    /// <summary>Gets the catalog status gained, or null when the force returned to Normal.</summary>
    public Guid? StatusTypeId { get; }

    /// <summary>Gets the previous status name, or null for Normal.</summary>
    public string? PreviousStatusName { get; }

    /// <summary>Gets the previous catalog status, or null for Normal.</summary>
    public Guid? PreviousStatusTypeId { get; }

    /// <summary>Gets the next status name, or null for Normal.</summary>
    public string? NextStatusName { get; }

    /// <summary>Gets the force attributed as causing the change, when known.</summary>
    public Guid? ActorForceId { get; }

    /// <summary>Gets the faction attributed as causing the change, when known.</summary>
    public Guid? ActorFactionId { get; }

    /// <summary>Gets the player attributed as causing the change, when known.</summary>
    public Guid? ActorUserId { get; }

    /// <summary>Gets when the change was recorded, in UTC.</summary>
    public DateTimeOffset OccurredUtc { get; }
}

/// <summary>
/// An append-only record that a structure was built or repaired.
/// </summary>
public sealed class StructureWorkFact
{
    /// <summary>
    /// Initializes a build or repair fact.
    /// </summary>
    public StructureWorkFact(
        Guid id,
        Guid territoryId,
        Guid structureTypeId,
        ActionKind kind,
        Guid actorFactionId,
        Guid actorUserId,
        DateTimeOffset occurredUtc)
    {
        Id = id;
        TerritoryId = territoryId;
        StructureTypeId = structureTypeId;
        Kind = kind;
        ActorFactionId = actorFactionId;
        ActorUserId = actorUserId;
        OccurredUtc = occurredUtc;
    }

    /// <summary>Gets the fact identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the territory.</summary>
    public Guid TerritoryId { get; }

    /// <summary>Gets the structure type.</summary>
    public Guid StructureTypeId { get; }

    /// <summary>Gets Build or Repair.</summary>
    public ActionKind Kind { get; }

    /// <summary>Gets the acting faction.</summary>
    public Guid ActorFactionId { get; }

    /// <summary>Gets the acting player.</summary>
    public Guid ActorUserId { get; }

    /// <summary>Gets when the work completed, in UTC.</summary>
    public DateTimeOffset OccurredUtc { get; }
}
