namespace Campaign.Domain.Play;

/// <summary>
/// A public objective completed for one player. Awards are append-only facts; revocation adds a later clearing award.
/// </summary>
public sealed class PublicObjectiveAward
{
    /// <summary>
    /// Initializes an award.
    /// </summary>
    /// <param name="id">The award identifier.</param>
    /// <param name="objectiveId">The public objective.</param>
    /// <param name="playerUserId">The player who received the points.</param>
    /// <param name="isActive">Whether the award currently counts.</param>
    /// <param name="actorUserId">The manager who awarded or revoked it.</param>
    /// <param name="awardedUtc">When the award or revocation was recorded, in UTC.</param>
    public PublicObjectiveAward(
        Guid id,
        Guid objectiveId,
        Guid playerUserId,
        bool isActive,
        Guid actorUserId,
        DateTimeOffset awardedUtc)
    {
        Id = id;
        ObjectiveId = objectiveId;
        PlayerUserId = playerUserId;
        IsActive = isActive;
        ActorUserId = actorUserId;
        AwardedUtc = awardedUtc;
    }

    /// <summary>Gets the award identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the public objective.</summary>
    public Guid ObjectiveId { get; }

    /// <summary>Gets the player who received the points.</summary>
    public Guid PlayerUserId { get; }

    /// <summary>Gets whether the award currently counts toward the player's total.</summary>
    public bool IsActive { get; }

    /// <summary>Gets the manager who recorded this fact.</summary>
    public Guid ActorUserId { get; }

    /// <summary>Gets when this fact was recorded, in UTC.</summary>
    public DateTimeOffset AwardedUtc { get; }
}
