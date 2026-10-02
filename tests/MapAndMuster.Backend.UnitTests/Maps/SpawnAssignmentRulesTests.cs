using MapAndMuster.Domain.Maps;

namespace MapAndMuster.Backend.UnitTests.Maps;

public sealed class SpawnAssignmentRulesTests
{
    private static readonly Guid North = Guid.Parse("aaaa1111-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid South = Guid.Parse("bbbb2222-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void NeutralSpawnSatisfiesFactionsThatHaveNoSpawnOfTheirOwn()
    {
        var error = SpawnAssignmentRules.RequireSpawnOrNeutral(
            randomSpawnLocations: false,
            [Faction(North, "North"), Faction(South, "South")],
            [new TerritorySpawn(SpawnMarkers.General, null)]);

        Assert.Null(error);
    }

    [Fact]
    public void MissingSpawnNamesEveryFactionWhenNoNeutralSpawnExists()
    {
        var error = SpawnAssignmentRules.RequireSpawnOrNeutral(
            randomSpawnLocations: false,
            [Faction(South, "South"), Faction(North, "North")],
            []);

        Assert.NotNull(error);
        Assert.Equal("territories.spawn.missing", error.Code);
        Assert.Equal(
            "No neutral spawn locations exist, and North, South have no specific spawn location.",
            error.Message);
    }

    [Fact]
    public void RandomPlacementDoesNotRequireAFactionSpawn()
    {
        var error = SpawnAssignmentRules.RequireSpawnOrNeutral(
            randomSpawnLocations: true,
            [Faction(North, "North")],
            []);

        Assert.Null(error);
    }

    private static FactionSpawnCheck Faction(Guid id, string name)
    {
        return new FactionSpawnCheck(id, name, false, [], false, new HashSet<string>());
    }
}
