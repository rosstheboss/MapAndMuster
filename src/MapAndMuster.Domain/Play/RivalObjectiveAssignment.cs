namespace MapAndMuster.Domain.Play;

/// <summary>
/// One secret rival assignment. A player may hold at most one active rival at a time.
/// </summary>
public sealed class RivalObjectiveAssignment
{
    /// <summary>
    /// Initializes a rival assignment.
    /// </summary>
    public RivalObjectiveAssignment(
        Guid id,
        Guid holderUserId,
        Guid rivalUserId,
        int campaignPoints,
        PrivateObjectiveAssignmentStatus status,
        DateTimeOffset assignedUtc,
        DateTimeOffset? revealedUtc = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(campaignPoints);
        Id = id;
        HolderUserId = holderUserId;
        RivalUserId = rivalUserId;
        CampaignPoints = campaignPoints;
        Status = status;
        AssignedUtc = assignedUtc;
        RevealedUtc = revealedUtc;
    }

    /// <summary>Gets the assignment identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the player who must defeat the rival.</summary>
    public Guid HolderUserId { get; }

    /// <summary>Gets the opposing player who is this assignment's target.</summary>
    public Guid RivalUserId { get; }

    /// <summary>Gets campaign points awarded when this rival is defeated.</summary>
    public int CampaignPoints { get; }

    /// <summary>Gets whether the assignment is still secret or has been revealed.</summary>
    public PrivateObjectiveAssignmentStatus Status { get; }

    /// <summary>Gets when the assignment was created, in UTC.</summary>
    public DateTimeOffset AssignedUtc { get; }

    /// <summary>Gets when the rival was defeated and revealed, in UTC.</summary>
    public DateTimeOffset? RevealedUtc { get; }

    /// <summary>Gets whether the holder still has this rival to defeat.</summary>
    public bool IsActive => Status == PrivateObjectiveAssignmentStatus.Assigned;

    /// <summary>
    /// Returns a copy marked revealed after a qualifying victory.
    /// </summary>
    public RivalObjectiveAssignment Reveal(DateTimeOffset revealedUtc)
    {
        return new RivalObjectiveAssignment(
            Id,
            HolderUserId,
            RivalUserId,
            CampaignPoints,
            PrivateObjectiveAssignmentStatus.Revealed,
            AssignedUtc,
            revealedUtc);
    }
}
