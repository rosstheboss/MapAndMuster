using Campaign.Domain.Campaigns;

namespace Campaign.Domain.Play;

/// <summary>
/// Raises a side's round army-point cap when more than one player fights together, then splits the total.
/// </summary>
public static class AlliedArmyPointRules
{
    /// <summary>
    /// Returns each allied force's army-point cap for a shared tabletop game.
    /// One player uses the round maximum. Extra players add 25 percent of that maximum per extra
    /// player to the side total, then each share rounds up to the next 10.
    /// </summary>
    public static int ForceArmyPoints(int roundMaxArmyPoints, int sidePlayerCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(roundMaxArmyPoints);
        ArgumentOutOfRangeException.ThrowIfLessThan(sidePlayerCount, 1);
        if (sidePlayerCount == 1)
        {
            return roundMaxArmyPoints;
        }

        var extraPlayers = sidePlayerCount - 1;
        var sideTotal = roundMaxArmyPoints
            + (int)decimal.Floor(roundMaxArmyPoints * HuntInEstaliaDefaults.AlliedExtraPlayerArmyPercent / 100m * extraPlayers);
        var rawShare = sideTotal / (decimal)sidePlayerCount;
        var increment = HuntInEstaliaDefaults.AlliedArmyPointsRoundTo;
        return (int)(decimal.Ceiling(rawShare / increment) * increment);
    }
}
