using MapAndMuster.Domain.Campaigns;

namespace MapAndMuster.Backend.UnitTests.Campaigns;

public sealed class TerritoryMissionRulesTests
{
    [Fact]
    public void UsesStructureMissionsWhenTheStructureHasAny()
    {
        var terrain = new TerrainTypeSetup(
            Guid.NewGuid(),
            "Plains",
            "#7CB342",
            [new MissionSetup(Guid.NewGuid(), "Plains control", null, false)]);
        var structure = new StructureTypeSetup(
            Guid.NewGuid(),
            "Town",
            "Town",
            false,
            false,
            false,
            true,
            true,
            [new MissionSetup(Guid.NewGuid(), "Town hold", "https://example.test/town", false)]);

        var missions = TerritoryMissionRules.Resolve(terrain, structure);

        Assert.Single(missions);
        Assert.Equal("Town hold", missions[0].Name);
    }

    [Fact]
    public void FallsBackToTerrainMissionsWhenTheStructureHasNone()
    {
        var terrain = new TerrainTypeSetup(
            Guid.NewGuid(),
            "Plains",
            "#7CB342",
            [new MissionSetup(Guid.NewGuid(), "Plains control", null, false)]);
        var structure = new StructureTypeSetup(Guid.NewGuid(), "Town", "Town", false, false, false, true, true, []);

        var missions = TerritoryMissionRules.Resolve(terrain, structure);

        Assert.Single(missions);
        Assert.Equal("Plains control", missions[0].Name);
    }
}
