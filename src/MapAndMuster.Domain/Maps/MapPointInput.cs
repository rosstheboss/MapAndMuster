namespace MapAndMuster.Domain.Maps;

/// <summary>
/// Unvalidated normalized polygon vertex.
/// </summary>
public sealed class MapPointInput
{
    /// <summary>Gets the horizontal coordinate.</summary>
    public double X { get; init; }

    /// <summary>Gets the vertical coordinate.</summary>
    public double Y { get; init; }
}
