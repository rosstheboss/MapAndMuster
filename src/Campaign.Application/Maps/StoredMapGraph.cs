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

    /// <summary>Gets manager-assigned item objective placements.</summary>
    public IReadOnlyList<ItemObjectivePlacementDetail> ItemObjectivePlacements { get; init; } = [];
}

/// <summary>
/// A manager-assigned launch location for a Placed item objective.
/// </summary>
public sealed class ItemObjectivePlacementDetail
{
    /// <summary>Gets the item objective type.</summary>
    public required Guid TypeId { get; init; }

    /// <summary>Gets the territory.</summary>
    public required Guid TerritoryId { get; init; }
}
