namespace MapAndMuster.Application.Maps;

/// <summary>
/// An explicit adjacency in a graph response.
/// </summary>
public sealed class AdjacencyDetail
{
    /// <summary>Gets the adjacency identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets one territory identifier.</summary>
    public required Guid TerritoryAId { get; init; }

    /// <summary>Gets the other territory identifier.</summary>
    public required Guid TerritoryBId { get; init; }

    /// <summary>Gets Generated or Manual.</summary>
    public required string Origin { get; init; }

    /// <summary>Gets the editor arrow marker X coordinate.</summary>
    public required double MarkerX { get; init; }

    /// <summary>Gets the editor arrow marker Y coordinate.</summary>
    public required double MarkerY { get; init; }
}
