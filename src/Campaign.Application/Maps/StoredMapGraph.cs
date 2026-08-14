namespace Campaign.Application.Maps;

/// <summary>
/// Persistence shape for the overlay graph. Stored as JSONB on the campaign.
/// </summary>
public sealed class StoredMapGraph
{
    /// <summary>Gets the territories.</summary>
    public required IReadOnlyList<TerritoryDetail> Territories { get; init; }

    /// <summary>Gets the adjacencies.</summary>
    public required IReadOnlyList<AdjacencyDetail> Adjacencies { get; init; }
}
