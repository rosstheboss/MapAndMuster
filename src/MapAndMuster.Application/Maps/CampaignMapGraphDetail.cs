namespace MapAndMuster.Application.Maps;

/// <summary>
/// Member-visible overlay graph for a campaign map. Arrow markers are editor aids stored with the graph.
/// </summary>
public sealed class CampaignMapGraphDetail
{
    /// <summary>Gets the campaign identifier.</summary>
    public required Guid CampaignId { get; init; }

    /// <summary>Gets the optimistic concurrency revision.</summary>
    public required int Revision { get; init; }

    /// <summary>Gets whether the current user can edit the map graph.</summary>
    public required bool CanManage { get; init; }

    /// <summary>Gets the overlay territories.</summary>
    public required IReadOnlyList<TerritoryDetail> Territories { get; init; }

    /// <summary>Gets the explicit adjacencies.</summary>
    public required IReadOnlyList<AdjacencyDetail> Adjacencies { get; init; }

    /// <summary>Gets manager-assigned item objective placements.</summary>
    public IReadOnlyList<ItemObjectivePlacementDetail> ItemObjectivePlacements { get; init; } = [];
}
