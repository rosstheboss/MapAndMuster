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
/// One assigned private objective instance. Catalog types are unique across assignments.
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
        Guid? approvedByUserId = null)
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
            approvedByUserId ?? ApprovedByUserId);
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
