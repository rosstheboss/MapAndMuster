using System.Text.Json;
using Campaign.Application.Maps;

namespace Campaign.Infrastructure.Campaigns;

/// <summary>
/// Serializes overlay map graphs for JSONB storage.
/// </summary>
internal static class MapGraphJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static string Serialize(StoredMapGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        var document = new MapGraphDocument
        {
            Territories = [.. graph.Territories.Select(ToDocument)],
            Adjacencies = [.. graph.Adjacencies.Select(ToDocument)],
            ItemObjectivePlacements = [.. graph.ItemObjectivePlacements.Select(static item => new ItemPlacementDocument
            {
                TypeId = item.TypeId,
                TerritoryId = item.TerritoryId,
            })],
        };
        return JsonSerializer.Serialize(document, Options);
    }

    public static StoredMapGraph? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        var document = JsonSerializer.Deserialize<MapGraphDocument>(json, Options);
        if (document is null)
        {
            return null;
        }

        return new StoredMapGraph
        {
            Territories = [.. document.Territories.Select(FromDocument)],
            Adjacencies = [.. document.Adjacencies.Select(FromDocument)],
            ItemObjectivePlacements =
            [
                .. (document.ItemObjectivePlacements ?? []).Select(static item => new ItemObjectivePlacementDetail
                {
                    TypeId = item.TypeId,
                    TerritoryId = item.TerritoryId,
                }),
            ],
        };
    }

    private static TerritoryDocument ToDocument(TerritoryDetail territory)
    {
        return new TerritoryDocument
        {
            Id = territory.Id,
            DisplayNumber = territory.DisplayNumber,
            Name = territory.Name,
            Description = territory.Description,
            Polygon = [.. territory.Polygon.Select(static point => new MapPointDocument { X = point.X, Y = point.Y })],
            TerrainTypeId = territory.TerrainTypeId,
            StructureTypeId = territory.StructureTypeId,
            OverlayColor = territory.OverlayColor,
            OwnerFactionId = territory.OwnerFactionId,
            OwnerSubfaction = territory.OwnerSubfaction,
            SpawnFactionId = territory.SpawnFactionId,
            SpawnSubfaction = territory.SpawnSubfaction,
            StructureCondition = territory.StructureCondition,
        };
    }

    private static AdjacencyDocument ToDocument(AdjacencyDetail edge)
    {
        return new AdjacencyDocument
        {
            Id = edge.Id,
            TerritoryAId = edge.TerritoryAId,
            TerritoryBId = edge.TerritoryBId,
            Origin = edge.Origin,
            MarkerX = edge.MarkerX,
            MarkerY = edge.MarkerY,
        };
    }

    private static TerritoryDetail FromDocument(TerritoryDocument territory)
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
            OverlayColor = territory.OverlayColor,
            OwnerFactionId = territory.OwnerFactionId,
            OwnerSubfaction = territory.OwnerSubfaction,
            SpawnFactionId = territory.SpawnFactionId,
            SpawnSubfaction = territory.SpawnSubfaction,
            StructureCondition = territory.StructureCondition,
        };
    }

    private static AdjacencyDetail FromDocument(AdjacencyDocument edge)
    {
        return new AdjacencyDetail
        {
            Id = edge.Id,
            TerritoryAId = edge.TerritoryAId,
            TerritoryBId = edge.TerritoryBId,
            Origin = edge.Origin ?? AdjacencyOriginFallback,
            MarkerX = edge.MarkerX,
            MarkerY = edge.MarkerY,
        };
    }

    private const string AdjacencyOriginFallback = "Manual";

    private sealed class MapGraphDocument
    {
        public List<TerritoryDocument> Territories { get; set; } = [];

        public List<AdjacencyDocument> Adjacencies { get; set; } = [];

        public List<ItemPlacementDocument>? ItemObjectivePlacements { get; set; }
    }

    private sealed class ItemPlacementDocument
    {
        public Guid TypeId { get; set; }

        public Guid TerritoryId { get; set; }
    }

    private sealed class TerritoryDocument
    {
        public Guid Id { get; set; }

        public int DisplayNumber { get; set; }

        public string? Name { get; set; }

        public string? Description { get; set; }

        public List<MapPointDocument> Polygon { get; set; } = [];

        public Guid TerrainTypeId { get; set; }

        public Guid? StructureTypeId { get; set; }

        public string? OverlayColor { get; set; }

        public Guid? OwnerFactionId { get; set; }

        public string? OwnerSubfaction { get; set; }

        public Guid? SpawnFactionId { get; set; }

        public string? SpawnSubfaction { get; set; }

        public string? StructureCondition { get; set; }
    }

    private sealed class MapPointDocument
    {
        public double X { get; set; }

        public double Y { get; set; }
    }

    private sealed class AdjacencyDocument
    {
        public Guid Id { get; set; }

        public Guid TerritoryAId { get; set; }

        public Guid TerritoryBId { get; set; }

        public string? Origin { get; set; }

        public double MarkerX { get; set; }

        public double MarkerY { get; set; }
    }
}
