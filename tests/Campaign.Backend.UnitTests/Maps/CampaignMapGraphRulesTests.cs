using Campaign.Domain.Maps;

namespace Campaign.Backend.UnitTests.Maps;

public sealed class CampaignMapGraphRulesTests
{
    private static readonly Guid NorthId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid SouthId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid PlainsId = Guid.Parse("cccccc01-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid SeaId = Guid.Parse("cccccc02-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid SwampId = Guid.Parse("cccccc03-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid TownId = Guid.Parse("dddddd01-dddd-dddd-dddd-dddddddddddd");

    private static HashSet<Guid> TerrainIds => [PlainsId, SeaId, SwampId];

    private static HashSet<Guid> StructureIds => [TownId];

    [Fact]
    public void CatalogsAreAlphabetical()
    {
        Assert.Equal(
            ["Beach", "Desert", "Highlands", "Lake", "Mountain", "Plains", "Riverlands", "Sea", "Swamp"],
            TerrainCatalog.All.Select(entry => entry.Label));
        Assert.Equal(
            ["Capital City", "Castle", "City", "Fortification", "Supply Depot", "Town"],
            StructureCatalog.All.Select(entry => entry.Label));
    }

    [Fact]
    public void AcceptsNonOverlappingTerritoriesWithOptionalMetadata()
    {
        var leftId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var rightId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var ok = CampaignMapGraphRules.TryCreate(
            [
                Territory(leftId, 1, "Northmarch", Square(0.1, 0.1, 0.3), PlainsId, TownId, "#7CB342", NorthId, NorthId),
                Territory(rightId, 2, null, Square(0.4, 0.1, 0.3), SeaId, null, null, null, null),
            ],
            [
                new AdjacencyInput
                {
                    TerritoryAId = leftId,
                    TerritoryBId = rightId,
                    Origin = "Manual",
                    MarkerX = 0.4,
                    MarkerY = 0.25,
                },
            ],
            new HashSet<Guid> { NorthId, SouthId },
            TerrainIds,
            StructureIds,
            out var graph,
            out var errors);

        Assert.True(ok);
        Assert.Empty(errors);
        Assert.NotNull(graph);
        Assert.Equal("Northmarch", graph.Territories[0].Name);
        Assert.Equal("2", graph.Territories[1].DisplayLabel);
        Assert.Equal(AdjacencyOrigin.Manual, graph.Adjacencies[0].Origin);
    }

    [Fact]
    public void RejectsOverlappingTerritoriesAndDuplicateNames()
    {
        var ok = CampaignMapGraphRules.TryCreate(
            [
                Territory(Guid.NewGuid(), 1, "Marsh", Square(0.1, 0.1, 0.4), SwampId, null, null, null, null),
                Territory(Guid.NewGuid(), 2, "marsh", Square(0.3, 0.1, 0.4), SwampId, null, null, null, null),
            ],
            [],
            new HashSet<Guid> { NorthId },
            TerrainIds,
            StructureIds,
            out var graph,
            out var errors);

        Assert.False(ok);
        Assert.Null(graph);
        Assert.Contains(errors, error => error.Code == "territories.overlap");
        Assert.Contains(errors, error => error.Code == "territories.name.duplicate");
    }

    [Fact]
    public void RejectsPointsPastTheMapEdge()
    {
        var ok = CampaignMapGraphRules.TryCreate(
            [
                Territory(
                    Guid.NewGuid(),
                    1,
                    null,
                    [new MapPointInput { X = 0.1, Y = 0.1 }, new MapPointInput { X = 1.2, Y = 0.1 }, new MapPointInput { X = 0.1, Y = 0.5 }],
                    PlainsId,
                    null,
                    null,
                    null,
                    null),
            ],
            [],
            new HashSet<Guid>(),
            TerrainIds,
            StructureIds,
            out _,
            out var errors);

        Assert.False(ok);
        Assert.Contains(errors, error => error.Code is "territories.polygon.bounds" or "territories.polygon.invalid");
    }

    [Fact]
    public void GenerateKeepsManualAdjacenciesAndSkipsThosePairs()
    {
        var left = MakeTerritory(Guid.Parse("11111111-1111-1111-1111-111111111111"), 1, SquarePoints(0.1, 0.1, 0.3));
        var middle = MakeTerritory(Guid.Parse("22222222-2222-2222-2222-222222222222"), 2, SquarePoints(0.4, 0.1, 0.3));
        var right = MakeTerritory(Guid.Parse("33333333-3333-3333-3333-333333333333"), 3, SquarePoints(0.7, 0.1, 0.3));
        var manual = new TerritoryAdjacency(
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            left.Id,
            middle.Id,
            AdjacencyOrigin.Manual,
            new MapPoint(0.4, 0.9));
        var staleGenerated = new TerritoryAdjacency(
            Guid.NewGuid(),
            middle.Id,
            right.Id,
            AdjacencyOrigin.Generated,
            new MapPoint(0.1, 0.1));

        var generated = AdjacencyGenerator.Generate([left, middle, right], [manual, staleGenerated]);

        Assert.Single(generated, edge => edge.Origin == AdjacencyOrigin.Manual);
        Assert.Equal(manual.Id, generated.First(edge => edge.Origin == AdjacencyOrigin.Manual).Id);
        Assert.Equal(0.9, generated.First(edge => edge.Origin == AdjacencyOrigin.Manual).Marker.Y);
        Assert.DoesNotContain(generated, edge => edge.Id == staleGenerated.Id);
        Assert.Contains(
            generated,
            edge => edge.Origin == AdjacencyOrigin.Generated && edge.Connects(middle.Id, right.Id));
        Assert.DoesNotContain(
            generated,
            edge => edge.Origin == AdjacencyOrigin.Generated && edge.Connects(left.Id, middle.Id));
    }

    [Fact]
    public void RejectsMissingTerrainAndDuplicateSpawns()
    {
        var missingTerrain = CampaignMapGraphRules.TryCreate(
            [
                Territory(Guid.NewGuid(), 1, null, Square(0.1, 0.1, 0.3), Guid.Empty, null, null, null, null),
            ],
            [],
            new HashSet<Guid> { NorthId },
            TerrainIds,
            StructureIds,
            out _,
            out var missingErrors);

        Assert.False(missingTerrain);
        Assert.Contains(missingErrors, error => error.Code == "territories.terrain.required");

        var duplicateSpawn = CampaignMapGraphRules.TryCreate(
            [
                Territory(Guid.NewGuid(), 1, null, Square(0.1, 0.1, 0.3), PlainsId, null, null, null, NorthId),
                Territory(Guid.NewGuid(), 2, null, Square(0.4, 0.1, 0.3), SeaId, null, null, null, NorthId),
            ],
            [],
            new HashSet<Guid> { NorthId },
            TerrainIds,
            StructureIds,
            out _,
            out var spawnErrors);

        Assert.False(duplicateSpawn);
        Assert.Contains(spawnErrors, error => error.Code == "territories.spawn.duplicate");
    }

    private static TerritoryInput Territory(
        Guid id,
        int number,
        string? name,
        IReadOnlyList<MapPointInput> polygon,
        Guid terrainTypeId,
        Guid? structureTypeId,
        string? overlayColor,
        Guid? ownerFactionId,
        Guid? spawnFactionId)
    {
        return new TerritoryInput
        {
            Id = id,
            DisplayNumber = number,
            Name = name,
            Polygon = polygon,
            TerrainTypeId = terrainTypeId == Guid.Empty ? null : terrainTypeId,
            StructureTypeId = structureTypeId,
            OverlayColor = overlayColor,
            OwnerFactionId = ownerFactionId,
            SpawnFactionId = spawnFactionId,
        };
    }

    private static IReadOnlyList<MapPointInput> Square(double x, double y, double size)
    {
        return
        [
            new MapPointInput { X = x, Y = y },
            new MapPointInput { X = x + size, Y = y },
            new MapPointInput { X = x + size, Y = y + size },
            new MapPointInput { X = x, Y = y + size },
        ];
    }

    private static IReadOnlyList<MapPoint> SquarePoints(double x, double y, double size)
    {
        return
        [
            new MapPoint(x, y),
            new MapPoint(x + size, y),
            new MapPoint(x + size, y + size),
            new MapPoint(x, y + size),
        ];
    }

    private static Territory MakeTerritory(Guid id, int number, IReadOnlyList<MapPoint> polygon)
    {
        return new Territory(id, number, null, null, polygon, PlainsId, null, null, null, null);
    }
}
