namespace MapAndMuster.Domain.Play;

/// <summary>Whether a request to replace a private objective is still open.</summary>
public enum PrivateObjectiveReissueStatus
{
    /// <summary>Waiting for a campaign manager.</summary>
    Pending = 0,

    /// <summary>The assignment was replaced and its catalog type left the pool.</summary>
    Approved = 1,

    /// <summary>The holder keeps the original assignment.</summary>
    Denied = 2,
}

/// <summary>
/// A player's request to replace an unachievable private objective, or a manager's immediate reissue.
/// The note is visible only to campaign managers, the affected player, and administrators.
/// </summary>
public sealed class PrivateObjectiveReissueRequest
{
    /// <summary>Maximum length of an approval or denial note.</summary>
    public const int NoteMaxLength = 500;

    /// <summary>
    /// Initializes a reissue record.
    /// </summary>
    public PrivateObjectiveReissueRequest(
        Guid id,
        Guid assignmentId,
        Guid typeId,
        Guid affectedUserId,
        Guid requestedByUserId,
        DateTimeOffset requestedUtc,
        PrivateObjectiveReissueStatus status,
        Guid? resolvedByUserId = null,
        DateTimeOffset? resolvedUtc = null,
        string? note = null,
        Guid? replacementAssignmentId = null)
    {
        Id = id;
        AssignmentId = assignmentId;
        TypeId = typeId;
        AffectedUserId = affectedUserId;
        RequestedByUserId = requestedByUserId;
        RequestedUtc = requestedUtc;
        Status = status;
        ResolvedByUserId = resolvedByUserId;
        ResolvedUtc = resolvedUtc;
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        ReplacementAssignmentId = replacementAssignmentId;
    }

    /// <summary>Gets the request identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the assignment being replaced.</summary>
    public Guid AssignmentId { get; }

    /// <summary>Gets the catalog type removed from the pool when approved.</summary>
    public Guid TypeId { get; }

    /// <summary>Gets the player who receives the result.</summary>
    public Guid AffectedUserId { get; }

    /// <summary>Gets the player or manager who opened the reissue.</summary>
    public Guid RequestedByUserId { get; }

    /// <summary>Gets when the reissue was requested, in UTC.</summary>
    public DateTimeOffset RequestedUtc { get; }

    /// <summary>Gets whether the request is pending, approved, or denied.</summary>
    public PrivateObjectiveReissueStatus Status { get; }

    /// <summary>Gets the manager who approved or denied it.</summary>
    public Guid? ResolvedByUserId { get; }

    /// <summary>Gets when it was approved or denied, in UTC.</summary>
    public DateTimeOffset? ResolvedUtc { get; }

    /// <summary>Gets the optional manager note, at most <see cref="NoteMaxLength"/> characters.</summary>
    public string? Note { get; }

    /// <summary>Gets the replacement assignment when the reissue was approved.</summary>
    public Guid? ReplacementAssignmentId { get; }

    /// <summary>Returns a resolved copy.</summary>
    public PrivateObjectiveReissueRequest Resolved(
        PrivateObjectiveReissueStatus status,
        Guid actorUserId,
        DateTimeOffset utcNow,
        string? note,
        Guid? replacementAssignmentId)
    {
        return new PrivateObjectiveReissueRequest(
            Id,
            AssignmentId,
            TypeId,
            AffectedUserId,
            RequestedByUserId,
            RequestedUtc,
            status,
            actorUserId,
            utcNow,
            note,
            replacementAssignmentId);
    }
}

/// <summary>
/// A manager's approval or denial of a manual private-objective claim.
/// The note is visible only to campaign managers, the affected player, and administrators.
/// </summary>
public sealed class PrivateObjectiveClaimDecision
{
    /// <summary>
    /// Initializes a claim decision.
    /// </summary>
    public PrivateObjectiveClaimDecision(
        Guid id,
        Guid assignmentId,
        Guid subjectUserId,
        Guid actorUserId,
        bool approved,
        string? note,
        DateTimeOffset occurredUtc)
    {
        Id = id;
        AssignmentId = assignmentId;
        SubjectUserId = subjectUserId;
        ActorUserId = actorUserId;
        Approved = approved;
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        OccurredUtc = occurredUtc;
    }

    /// <summary>Gets the decision identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the assignment.</summary>
    public Guid AssignmentId { get; }

    /// <summary>Gets the player who is told the result.</summary>
    public Guid SubjectUserId { get; }

    /// <summary>Gets the manager who decided.</summary>
    public Guid ActorUserId { get; }

    /// <summary>Gets whether the claim was approved.</summary>
    public bool Approved { get; }

    /// <summary>Gets the optional note.</summary>
    public string? Note { get; }

    /// <summary>Gets when the decision was recorded, in UTC.</summary>
    public DateTimeOffset OccurredUtc { get; }
}
