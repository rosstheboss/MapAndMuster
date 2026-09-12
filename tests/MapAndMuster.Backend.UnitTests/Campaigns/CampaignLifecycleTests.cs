using MapAndMuster.Application.Campaigns;
using MapAndMuster.Application.Maps;
using MapAndMuster.Domain.Play;

namespace MapAndMuster.Backend.UnitTests.Campaigns;

public sealed class CampaignLifecycleTests
{
    [Fact]
    public void ApplyOwnershipCopiesTheRequiredSubfactionFlag()
    {
        var territoryId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var factionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var graph = new StoredMapGraph
        {
            Territories =
            [
                new TerritoryDetail
                {
                    Id = territoryId,
                    DisplayNumber = 1,
                    Polygon = [],
                    TerrainTypeId = Guid.NewGuid(),
                    OwnerFactionId = null,
                    OwnerSubfaction = null,
                },
            ],
            Adjacencies = [],
        };
        var map = new PlayMap(
            [
                new PlayTerritory(
                    territoryId,
                    1,
                    factionId,
                    null,
                    null,
                    null,
                    StructureCondition.Operational,
                    ownerSubfaction: "Khorne"),
            ],
            []);

        var next = CampaignLifecycle.ApplyOwnership(graph, map);

        Assert.Equal(factionId, next.Territories[0].OwnerFactionId);
        Assert.Equal("Khorne", next.Territories[0].OwnerSubfaction);
    }
}
