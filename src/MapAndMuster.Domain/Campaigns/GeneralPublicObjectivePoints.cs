namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// Campaign points for the built-in public objectives. A value of 0 ignores that objective.
/// Ranking objectives award their points to every player currently tied for first.
/// Running objectives add their points from live map and relic state.
/// </summary>
public sealed class GeneralPublicObjectivePoints
{
    /// <summary>
    /// Initializes ranking and running public-objective points.
    /// </summary>
    public GeneralPublicObjectivePoints(
        int mostTerritories,
        int longestTerritoryChain,
        int mostBattlesWon,
        int mostStructurePoints = 0,
        int pointsPerTerritory = 0,
        int alliedRelicControlPoints = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(mostTerritories);
        ArgumentOutOfRangeException.ThrowIfNegative(longestTerritoryChain);
        ArgumentOutOfRangeException.ThrowIfNegative(mostBattlesWon);
        ArgumentOutOfRangeException.ThrowIfNegative(mostStructurePoints);
        ArgumentOutOfRangeException.ThrowIfNegative(pointsPerTerritory);
        ArgumentOutOfRangeException.ThrowIfNegative(alliedRelicControlPoints);
        MostTerritories = mostTerritories;
        LongestTerritoryChain = longestTerritoryChain;
        MostBattlesWon = mostBattlesWon;
        MostStructurePoints = mostStructurePoints;
        PointsPerTerritory = pointsPerTerritory;
        AlliedRelicControlPoints = alliedRelicControlPoints;
    }

    /// <summary>Gets an all-zero configuration that ignores every built-in public objective.</summary>
    public static GeneralPublicObjectivePoints None { get; } = new(0, 0, 0);

    /// <summary>Gets points awarded to each player currently tied for most territories.</summary>
    public int MostTerritories { get; }

    /// <summary>Gets points awarded to each player currently tied for the longest owned territory chain.</summary>
    public int LongestTerritoryChain { get; }

    /// <summary>Gets points awarded to each player currently tied for most battle wins.</summary>
    public int MostBattlesWon { get; }

    /// <summary>Gets points awarded to each player currently tied for most structure campaign points.</summary>
    public int MostStructurePoints { get; }

    /// <summary>Gets campaign points awarded for each currently owned territory. Zero ignores the objective.</summary>
    public int PointsPerTerritory { get; }

    /// <summary>
    /// Gets campaign points awarded for each revealed relic held by another player of the same faction
    /// or a current ally. Relics the scoring player holds do not count. Zero ignores the objective.
    /// </summary>
    public int AlliedRelicControlPoints { get; }
}
