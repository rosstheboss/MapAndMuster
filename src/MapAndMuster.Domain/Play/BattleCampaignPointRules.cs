using MapAndMuster.Domain.Campaigns;

namespace MapAndMuster.Domain.Play;

/// <summary>
/// Converts a resolved battle into campaign points for the winner, loser, and draw participants.
/// </summary>
public static class BattleCampaignPointRules
{
    /// <summary>
    /// Returns campaign points for the recorded winner. Draws yield 0 here; use <see cref="DrawPoints"/>.
    /// </summary>
    public static int WinnerPoints(BattleScoringSetup scoring, bool isDraw, int? winnerScore, int? loserScore)
    {
        ArgumentNullException.ThrowIfNull(scoring);
        if (isDraw)
        {
            return 0;
        }

        if (!scoring.UseDifferential)
        {
            return scoring.PointsPerWin;
        }

        return ClampDifferential(scoring, Difference(winnerScore, loserScore));
    }

    /// <summary>
    /// Returns campaign points for a non-winning participant of a decisive battle.
    /// </summary>
    public static int LoserPoints(BattleScoringSetup scoring, bool isDraw, int? winnerScore, int? loserScore)
    {
        ArgumentNullException.ThrowIfNull(scoring);
        if (isDraw || !scoring.UseDifferential)
        {
            return 0;
        }

        if (!scoring.AllowNegativeDifferential)
        {
            return 0;
        }

        var mirrored = -WinnerPoints(scoring, isDraw: false, winnerScore, loserScore);
        if (mirrored < scoring.DifferentialMinimum)
        {
            return scoring.DifferentialMinimum;
        }

        if (mirrored > scoring.DifferentialMaximum)
        {
            return scoring.DifferentialMaximum;
        }

        return mirrored;
    }

    /// <summary>
    /// Returns campaign points awarded to each participant of a draw when that value is greater than 0.
    /// </summary>
    public static int DrawPoints(BattleScoringSetup scoring, bool isDraw)
    {
        ArgumentNullException.ThrowIfNull(scoring);
        return isDraw ? scoring.PointsPerDraw : 0;
    }

    private static decimal Difference(int? winnerScore, int? loserScore)
    {
        return (winnerScore ?? 0) - (loserScore ?? 0);
    }

    private static int ClampDifferential(BattleScoringSetup scoring, decimal raw)
    {
        var scaled = decimal.Round(raw * scoring.DifferentialMultiplier, MidpointRounding.AwayFromZero);
        var floor = scoring.AllowNegativeDifferential
            ? scoring.DifferentialMinimum
            : Math.Max(0, scoring.DifferentialMinimum);
        var ceiling = scoring.DifferentialMaximum;
        if (scaled < floor)
        {
            return floor;
        }

        if (scaled > ceiling)
        {
            return ceiling;
        }

        return (int)scaled;
    }
}
