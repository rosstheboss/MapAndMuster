namespace Campaign.Domain.Campaigns;

/// <summary>
/// Per-round army size, free supply, and free-character allowances.
/// </summary>
public sealed class RoundArmyEscalationSetup
{
    /// <summary>
    /// Initializes one round's army escalation.
    /// </summary>
    /// <param name="roundNumber">The 1-based round.</param>
    /// <param name="maxArmyPoints">Maximum army points size for the round.</param>
    /// <param name="freeSupplyPoints">Free supply points granted this round.</param>
    /// <param name="freeCharacterCount">Characters whose base cost does not count against supply.</param>
    public RoundArmyEscalationSetup(int roundNumber, int maxArmyPoints, int freeSupplyPoints, int freeCharacterCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(roundNumber, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(maxArmyPoints);
        ArgumentOutOfRangeException.ThrowIfNegative(freeSupplyPoints);
        ArgumentOutOfRangeException.ThrowIfNegative(freeCharacterCount);
        RoundNumber = roundNumber;
        MaxArmyPoints = maxArmyPoints;
        FreeSupplyPoints = freeSupplyPoints;
        FreeCharacterCount = freeCharacterCount;
    }

    /// <summary>Gets the 1-based round.</summary>
    public int RoundNumber { get; }

    /// <summary>Gets the maximum army points size for the round.</summary>
    public int MaxArmyPoints { get; }

    /// <summary>Gets free supply points granted this round.</summary>
    public int FreeSupplyPoints { get; }

    /// <summary>Gets how many characters have a free base cost against supply.</summary>
    public int FreeCharacterCount { get; }
}
