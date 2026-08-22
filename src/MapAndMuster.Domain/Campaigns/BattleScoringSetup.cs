namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// Configured conversion from resolved battles into campaign points.
/// </summary>
public sealed class BattleScoringSetup
{
    /// <summary>Default points awarded to the winner when differential scoring is off.</summary>
    public const int DefaultPointsPerWin = 2;

    /// <summary>Default points awarded to each participant of a draw.</summary>
    public const int DefaultPointsPerDraw = 1;

    /// <summary>Default differential multiplier.</summary>
    public const decimal DefaultMultiplier = 1m;

    /// <summary>Minimum allowed differential multiplier. Zero is not permitted.</summary>
    public const decimal MinMultiplier = 0.01m;

    /// <summary>Maximum allowed differential multiplier.</summary>
    public const decimal MaxMultiplier = 999m;

    /// <summary>
    /// Initializes battle scoring.
    /// </summary>
    public BattleScoringSetup(
        int pointsPerWin,
        int pointsPerDraw,
        bool useDifferential,
        decimal differentialMultiplier,
        int differentialMinimum,
        int differentialMaximum,
        bool allowNegativeDifferential)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(pointsPerWin);
        ArgumentOutOfRangeException.ThrowIfNegative(pointsPerDraw);
        ArgumentOutOfRangeException.ThrowIfLessThan(differentialMultiplier, MinMultiplier);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(differentialMultiplier, MaxMultiplier);
        if (differentialMaximum < differentialMinimum)
        {
            throw new ArgumentOutOfRangeException(nameof(differentialMaximum), "The differential maximum cannot be below the minimum.");
        }

        PointsPerWin = pointsPerWin;
        PointsPerDraw = pointsPerDraw;
        UseDifferential = useDifferential;
        DifferentialMultiplier = differentialMultiplier;
        DifferentialMinimum = differentialMinimum;
        DifferentialMaximum = differentialMaximum;
        AllowNegativeDifferential = allowNegativeDifferential;
    }

    /// <summary>Gets the default 0-to-10 differential configuration with 2/1 straight fallback points.</summary>
    public static BattleScoringSetup Default { get; } = new(
        DefaultPointsPerWin,
        DefaultPointsPerDraw,
        useDifferential: true,
        DefaultMultiplier,
        differentialMinimum: 0,
        differentialMaximum: 10,
        allowNegativeDifferential: false);

    /// <summary>
    /// Returns straight win/draw scoring with differential conversion turned off.
    /// </summary>
    public static BattleScoringSetup Straight(int pointsPerWin, int pointsPerDraw = 0)
    {
        return new(
            pointsPerWin,
            pointsPerDraw,
            useDifferential: false,
            DefaultMultiplier,
            differentialMinimum: 0,
            differentialMaximum: 10,
            allowNegativeDifferential: false);
    }

    /// <summary>Gets campaign points awarded to the winner when differential scoring is off.</summary>
    public int PointsPerWin { get; }

    /// <summary>Gets campaign points awarded to each draw participant.</summary>
    public int PointsPerDraw { get; }

    /// <summary>Gets whether battle campaign points use score differential instead of flat win points.</summary>
    public bool UseDifferential { get; }

    /// <summary>Gets the multiplier applied to the winner-minus-loser score difference.</summary>
    public decimal DifferentialMultiplier { get; }

    /// <summary>Gets the inclusive lower clamp for differential campaign points.</summary>
    public int DifferentialMinimum { get; }

    /// <summary>Gets the inclusive upper clamp for differential campaign points.</summary>
    public int DifferentialMaximum { get; }

    /// <summary>Gets whether the loser can receive negative campaign points when the clamped differential is negative.</summary>
    public bool AllowNegativeDifferential { get; }
}
