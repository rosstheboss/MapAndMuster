using MapAndMuster.Domain.Maps;
using MapAndMuster.Domain.Play;

namespace MapAndMuster.Backend.UnitTests.Play;

public sealed class PlayMapSpawnTests
{
    private static readonly Guid Daemons = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa10");
    private static readonly Guid KhorneLand = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid NurgleLand = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void SpawnForPrefersTheMatchingRequiredSubfaction()
    {
        var map = new PlayMap(
            [
                new PlayTerritory(KhorneLand, 1, Daemons, Daemons, null, null, StructureCondition.Operational, spawnSubfaction: "Khorne"),
                new PlayTerritory(NurgleLand, 2, Daemons, Daemons, null, null, StructureCondition.Operational, spawnSubfaction: "Nurgle"),
            ],
            []);

        Assert.Equal(KhorneLand, map.SpawnFor(Daemons, "Khorne")?.Id);
        Assert.Equal(NurgleLand, map.SpawnFor(Daemons, "Nurgle")?.Id);
        Assert.Equal(KhorneLand, map.SpawnFor(Daemons)?.Id);
    }

    [Fact]
    public void CanEnterTreatsRequiredSubfactionSpawnsAsSeparateFactions()
    {
        var khorne = new CampaignForce(Guid.NewGuid(), Guid.NewGuid(), Daemons, KhorneLand, false, subfaction: "Khorne");
        var map = new PlayMap(
            [
                new PlayTerritory(KhorneLand, 1, Daemons, Daemons, null, null, StructureCondition.Operational, spawnSubfaction: "Khorne"),
                new PlayTerritory(NurgleLand, 2, Daemons, Daemons, null, null, StructureCondition.Operational, spawnSubfaction: "Nurgle"),
            ],
            [(KhorneLand, NurgleLand)]);

        Assert.True(FactionSpecialRulePolicies.CanEnter(map, khorne, KhorneLand));
        Assert.False(FactionSpecialRulePolicies.CanEnter(map, khorne, NurgleLand));
        Assert.True(FactionSpecialRulePolicies.IsEnemySpawn(map.Territory(NurgleLand)!, khorne));
    }

    [Fact]
    public void StartingPlacementUsesAnUnoccupiedNeutralSpawnWhenTheFactionHasNone()
    {
        var faction = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa20");
        var first = Guid.Parse("33333333-3333-3333-3333-333333333331");
        var second = Guid.Parse("33333333-3333-3333-3333-333333333332");
        var map = new PlayMap(
            [
                new PlayTerritory(first, 1, null, SpawnMarkers.General, null, null, StructureCondition.Operational),
                new PlayTerritory(second, 2, null, SpawnMarkers.General, null, null, StructureCondition.Operational),
            ],
            []);

        var placed = FactionSpecialRulePolicies.StartingPlacement(
            map,
            faction,
            null,
            [],
            SpecialRuleContext.None,
            static _ => 0);
        Assert.Equal((first, false), placed);

        var next = FactionSpecialRulePolicies.StartingPlacement(
            map,
            faction,
            null,
            [new CampaignForce(Guid.NewGuid(), Guid.NewGuid(), faction, first, false)],
            SpecialRuleContext.None,
            static _ => 0);
        Assert.Equal((second, false), next);
    }
}
