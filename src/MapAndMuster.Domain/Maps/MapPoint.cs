namespace MapAndMuster.Domain.Maps;

/// <summary>
/// A point in the campaign map rectangle, normalized so (0,0) is the top-left and (1,1) is the bottom-right.
/// </summary>
/// <param name="X">The horizontal coordinate in the inclusive range 0 to 1.</param>
/// <param name="Y">The vertical coordinate in the inclusive range 0 to 1.</param>
public readonly record struct MapPoint(double X, double Y)
{
    /// <summary>
    /// Returns the point clamped onto the map rectangle.
    /// </summary>
    /// <returns>The clamped point.</returns>
    public MapPoint ClampToMap()
    {
        return new MapPoint(Clamp01(X), Clamp01(Y));
    }

    /// <summary>
    /// Gets whether the point lies on the map rectangle.
    /// </summary>
    public bool IsOnMap => X is >= 0 and <= 1 && Y is >= 0 and <= 1;

    /// <summary>
    /// Squared Euclidean distance to another point.
    /// </summary>
    /// <param name="other">The other point.</param>
    /// <returns>The squared distance.</returns>
    public double DistanceSquaredTo(MapPoint other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        return (dx * dx) + (dy * dy);
    }

    private static double Clamp01(double value)
    {
        if (value < 0)
        {
            return 0;
        }

        return value > 1 ? 1 : value;
    }
}
