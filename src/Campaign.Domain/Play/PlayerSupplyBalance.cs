namespace Campaign.Domain.Play;

/// <summary>
/// Remaining temporary supply for one player. History of awards is append-only on the play log.
/// The player may spend this pool on any of their forces; each spent point applies to one force.
/// </summary>
public sealed class PlayerSupplyBalance
{
    /// <summary>
    /// Initializes a player's temporary supply balance.
    /// </summary>
    /// <param name="userId">The player.</param>
    /// <param name="temporarySupplyPoints">Unspent temporary supply.</param>
    public PlayerSupplyBalance(Guid userId, int temporarySupplyPoints)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(temporarySupplyPoints);
        UserId = userId;
        TemporarySupplyPoints = temporarySupplyPoints;
    }

    /// <summary>Gets the player.</summary>
    public Guid UserId { get; }

    /// <summary>Gets unspent temporary supply points.</summary>
    public int TemporarySupplyPoints { get; }
}
