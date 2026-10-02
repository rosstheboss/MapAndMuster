using MapAndMuster.Domain.Campaigns;
using MapAndMuster.Domain.Play;

namespace MapAndMuster.Backend.UnitTests.Play;

public sealed class FreeForAllAndMovementTests
{
    [Fact]
    public void CrossingForcesStopAtTheFirstSharedStep()
    {
        var bob = Guid.NewGuid();
        var alice = Guid.NewGuid();
        var a = Guid.Parse("00000000-0000-0000-0000-00000000000a");
        var b = Guid.Parse("00000000-0000-0000-0000-00000000000b");
        var c = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var d = Guid.Parse("00000000-0000-0000-0000-00000000000d");
        var stops = MovementCrossingRules.StopTerritories(
            [
                new MovementCrossingRules.Mover(bob, a, [b, c]),
                new MovementCrossingRules.Mover(alice, d, [c, b]),
            ],
            static (_, _) => true);

        Assert.Equal(stops[bob], stops[alice]);
        Assert.Equal(c, stops[bob]);
    }

    [Fact]
    public void CrossingIgnoresAnOriginTheOtherForceAlreadyLeft()
    {
        var bob = Guid.NewGuid();
        var alice = Guid.NewGuid();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        var d = Guid.NewGuid();
        var stops = MovementCrossingRules.StopTerritories(
            [
                new MovementCrossingRules.Mover(bob, a, [b, c, d]),
                new MovementCrossingRules.Mover(alice, d, [a, b, c]),
            ],
            static (_, _) => true);

        Assert.Equal(b, stops[bob]);
        Assert.Equal(b, stops[alice]);
    }

    [Fact]
    public void FreeForAllNeedsAQuarterOfTheSlotsAsSpawns()
    {
        Assert.Equal(2, CampaignConfigurationRules.MinimumFreeForAllSpawnCount(5));
        Assert.Null(CampaignConfigurationRules.StartBlockReason(isFreeForAll: false, 8, 0));
        Assert.NotNull(CampaignConfigurationRules.StartBlockReason(isFreeForAll: true, 8, 1));
        Assert.Null(CampaignConfigurationRules.StartBlockReason(isFreeForAll: true, 8, 2));
    }

    [Fact]
    public void RandomSpawnPrefersTheFactionWithMoreMatches()
    {
        var forest = Guid.NewGuid();
        var hills = Guid.NewGuid();
        var town = Guid.NewGuid();
        var swamp = Guid.NewGuid();
        var forestSpawn = Guid.NewGuid();
        var swampSpawn = Guid.NewGuid();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var assigned = RandomSpawnRules.Assign(
            [
                new RandomSpawnRules.Player(second, Guid.NewGuid(), new FactionPreference([swamp], [], [], [])),
                new RandomSpawnRules.Player(first, Guid.NewGuid(), new FactionPreference([forest], [hills], [town], [])),
            ],
            [
                new RandomSpawnRules.Spawn(swampSpawn, swamp, new HashSet<Guid>(), null, new HashSet<Guid>()),
                new RandomSpawnRules.Spawn(forestSpawn, forest, new HashSet<Guid> { hills }, town, new HashSet<Guid>()),
            ],
            static _ => 0);

        Assert.Equal(forestSpawn, assigned[first]);
        Assert.Equal(swampSpawn, assigned[second]);
    }

    [Fact]
    public void LoneFactionPlayerKeepsTheFactionColor()
    {
        var lone = Guid.NewGuid();
        var first = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var second = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var faction = Guid.NewGuid();
        var colors = FreeForAllColorRules.Assign(
        [
            new FreeForAllColorRules.Player(lone, Guid.NewGuid(), null, "#111111", null),
            new FreeForAllColorRules.Player(second, faction, null, "#222222", null),
            new FreeForAllColorRules.Player(first, faction, null, "#222222", null),
        ]);

        Assert.Equal("#111111", colors[lone]);
        Assert.Equal("#222222", colors[first]);
        Assert.NotEqual("#222222", colors[second]);
    }
}
