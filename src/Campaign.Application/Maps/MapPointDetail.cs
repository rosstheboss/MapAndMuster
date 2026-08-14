namespace Campaign.Application.Maps;

/// <summary>
/// A normalized map coordinate in a graph response.
/// </summary>
public sealed class MapPointDetail
{
    /// <summary>Gets the horizontal coordinate.</summary>
    public required double X { get; init; }

    /// <summary>Gets the vertical coordinate.</summary>
    public required double Y { get; init; }
}
