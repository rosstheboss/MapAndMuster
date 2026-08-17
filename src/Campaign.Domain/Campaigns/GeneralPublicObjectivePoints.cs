namespace Campaign.Domain.Campaigns;

/// <summary>
/// Campaign points for the built-in ranking public objectives. A value of 0 ignores that objective.
/// </summary>
public sealed class GeneralPublicObjectivePoints
{
    /// <summary>
    /// Initializes ranking-objective points.
    /// </summary>
    public GeneralPublicObjectivePoints(int mostTerritories, int longestTerritoryChain, int mostBattlesWon)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(mostTerritories);
        ArgumentOutOfRangeException.ThrowIfNegative(longestTerritoryChain);
        ArgumentOutOfRangeException.ThrowIfNegative(mostBattlesWon);
        MostTerritories = mostTerritories;
        LongestTerritoryChain = longestTerritoryChain;
        MostBattlesWon = mostBattlesWon;
    }

    /// <summary>Gets an all-zero configuration that ignores every ranking objective.</summary>
    public static GeneralPublicObjectivePoints None { get; } = new(0, 0, 0);

    /// <summary>Gets points awarded to each player currently tied for most territories.</summary>
    public int MostTerritories { get; }

    /// <summary>Gets points awarded to each player currently tied for the longest owned territory chain.</summary>
    public int LongestTerritoryChain { get; }

    /// <summary>Gets points awarded to each player currently tied for most battle wins.</summary>
    public int MostBattlesWon { get; }
}
