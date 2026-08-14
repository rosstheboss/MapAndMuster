namespace Campaign.Domain.Maps;

/// <summary>
/// An explicit undirected adjacency between two territories, with an editor-only arrow marker.
/// </summary>
public sealed class TerritoryAdjacency
{
    /// <summary>
    /// Initializes a validated adjacency. Territory identifiers are stored in a stable order.
    /// </summary>
    /// <param name="id">The adjacency identifier.</param>
    /// <param name="territoryAId">One territory.</param>
    /// <param name="territoryBId">The other territory.</param>
    /// <param name="origin">Whether the edge was generated or added by a user.</param>
    /// <param name="marker">The editor arrow position in normalized map coordinates.</param>
    public TerritoryAdjacency(Guid id, Guid territoryAId, Guid territoryBId, AdjacencyOrigin origin, MapPoint marker)
    {
        if (territoryAId == territoryBId)
        {
            throw new ArgumentException("An adjacency requires two distinct territories.", nameof(territoryBId));
        }

        Id = id;
        if (territoryAId.CompareTo(territoryBId) <= 0)
        {
            TerritoryAId = territoryAId;
            TerritoryBId = territoryBId;
        }
        else
        {
            TerritoryAId = territoryBId;
            TerritoryBId = territoryAId;
        }

        Origin = origin;
        Marker = marker.ClampToMap();
    }

    /// <summary>Gets the adjacency identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the lexicographically first territory identifier.</summary>
    public Guid TerritoryAId { get; }

    /// <summary>Gets the lexicographically second territory identifier.</summary>
    public Guid TerritoryBId { get; }

    /// <summary>Gets whether the edge was generated or added by a user.</summary>
    public AdjacencyOrigin Origin { get; }

    /// <summary>Gets the editor arrow position.</summary>
    public MapPoint Marker { get; }

    /// <summary>
    /// Gets whether this edge connects the two identifiers, regardless of order.
    /// </summary>
    /// <param name="left">A territory identifier.</param>
    /// <param name="right">The other territory identifier.</param>
    /// <returns><see langword="true"/> when this edge is that pair.</returns>
    public bool Connects(Guid left, Guid right)
    {
        return (TerritoryAId == left && TerritoryBId == right) || (TerritoryAId == right && TerritoryBId == left);
    }
}
