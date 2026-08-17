using Campaign.Domain.Maps;

namespace Campaign.Application.Maps;

/// <summary>
/// Maps a validated graph onto application detail and storage models.
/// </summary>
public static class MapGraphMapper
{
    /// <summary>
    /// Maps a validated graph onto a member-visible detail.
    /// </summary>
    /// <param name="campaignId">The campaign identifier.</param>
    /// <param name="revision">The campaign revision.</param>
    /// <param name="canManage">Whether the viewer can edit the graph.</param>
    /// <param name="graph">The validated graph.</param>
    /// <param name="itemObjectivePlacements">Optional manager-assigned placements for Placed items.</param>
    /// <returns>The detail.</returns>
    public static CampaignMapGraphDetail ToDetail(
        Guid campaignId,
        int revision,
        bool canManage,
        CampaignMapGraph graph,
        IReadOnlyList<ItemObjectivePlacementDetail>? itemObjectivePlacements = null)
    {
        ArgumentNullException.ThrowIfNull(graph);
        return new CampaignMapGraphDetail
        {
            CampaignId = campaignId,
            Revision = revision,
            CanManage = canManage,
            Territories = [.. graph.Territories.Select(ToTerritory)],
            Adjacencies = [.. graph.Adjacencies.Select(ToAdjacency)],
            ItemObjectivePlacements = itemObjectivePlacements ?? [],
        };
    }

    /// <summary>
    /// Maps a validated graph onto the persistence model.
    /// </summary>
    /// <param name="graph">The validated graph.</param>
    /// <param name="itemObjectivePlacements">Optional manager-assigned placements for Placed items.</param>
    /// <returns>The stored graph.</returns>
    public static StoredMapGraph ToStored(
        CampaignMapGraph graph,
        IReadOnlyList<ItemObjectivePlacementDetail>? itemObjectivePlacements = null)
    {
        ArgumentNullException.ThrowIfNull(graph);
        return new StoredMapGraph
        {
            Territories = [.. graph.Territories.Select(ToTerritory)],
            Adjacencies = [.. graph.Adjacencies.Select(ToAdjacency)],
            ItemObjectivePlacements = itemObjectivePlacements ?? [],
        };
    }

    /// <summary>
    /// Maps stored territories onto domain inputs for re-validation.
    /// </summary>
    /// <param name="graph">The stored graph.</param>
    /// <returns>Territory inputs.</returns>
    public static IReadOnlyList<TerritoryInput> ToTerritoryInputs(StoredMapGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        return
        [
            .. graph.Territories.Select(static territory => new TerritoryInput
            {
                Id = territory.Id,
                DisplayNumber = territory.DisplayNumber,
                Name = territory.Name,
                Description = territory.Description,
                Polygon =
                [
                    .. territory.Polygon.Select(static point => new MapPointInput { X = point.X, Y = point.Y }),
                ],
                TerrainTypeId = territory.TerrainTypeId,
                StructureTypeId = territory.StructureTypeId,
                StructureCondition = territory.StructureCondition,
                OverlayColor = territory.OverlayColor,
                OwnerFactionId = territory.OwnerFactionId,
                SpawnFactionId = territory.SpawnFactionId,
            }),
        ];
    }

    /// <summary>
    /// Maps stored adjacencies onto domain inputs.
    /// </summary>
    /// <param name="graph">The stored graph.</param>
    /// <returns>Adjacency inputs.</returns>
    public static IReadOnlyList<AdjacencyInput> ToAdjacencyInputs(StoredMapGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        return
        [
            .. graph.Adjacencies.Select(static edge => new AdjacencyInput
            {
                Id = edge.Id,
                TerritoryAId = edge.TerritoryAId,
                TerritoryBId = edge.TerritoryBId,
                Origin = edge.Origin,
                MarkerX = edge.MarkerX,
                MarkerY = edge.MarkerY,
            }),
        ];
    }

    /// <summary>
    /// Returns an empty stored graph.
    /// </summary>
    /// <returns>An empty graph.</returns>
    public static StoredMapGraph Empty()
    {
        return new StoredMapGraph { Territories = [], Adjacencies = [] };
    }

    private static TerritoryDetail ToTerritory(Territory territory)
    {
        return new TerritoryDetail
        {
            Id = territory.Id,
            DisplayNumber = territory.DisplayNumber,
            Name = territory.Name,
            Description = territory.Description,
            Polygon = [.. territory.Polygon.Select(static point => new MapPointDetail { X = point.X, Y = point.Y })],
            TerrainTypeId = territory.TerrainTypeId,
            StructureTypeId = territory.StructureTypeId,
            StructureCondition = territory.StructureCondition.ToString(),
            OverlayColor = territory.OverlayColor,
            OwnerFactionId = territory.OwnerFactionId,
            SpawnFactionId = territory.SpawnFactionId,
        };
    }

    private static AdjacencyDetail ToAdjacency(TerritoryAdjacency edge)
    {
        return new AdjacencyDetail
        {
            Id = edge.Id,
            TerritoryAId = edge.TerritoryAId,
            TerritoryBId = edge.TerritoryBId,
            Origin = edge.Origin.ToString(),
            MarkerX = edge.Marker.X,
            MarkerY = edge.Marker.Y,
        };
    }
}
