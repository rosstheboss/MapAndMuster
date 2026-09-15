using MapAndMuster.Domain.Campaigns;
using MapAndMuster.Domain.Play;

namespace MapAndMuster.Backend.UnitTests.Play;

public sealed class ForceTerritoryAccessRulesTests
{
    private static readonly Guid Player = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Faction = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Ally = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid Enemy = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid Spawn = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Mid = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid Isolated = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid Water = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid Depot = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid Fort = Guid.Parse("ffffffff-ffff-ffff-ffff-fffffffffff1");
    private static readonly Guid Watchtower = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3");
    private static readonly Guid WaterTag = Guid.Parse("99999999-9999-9999-9999-999999999999");

    [Fact]
    public void ContiguousOwnedChainReachesSpawnAndStructures()
    {
        var force = Force(Mid);
        var map = new PlayMap(
            [
                Territory(Spawn, 1, Faction, spawn: Faction),
                Territory(Mid, 2, Faction, structureTypeId: Depot, structureName: "Supply Depot"),
            ],
            [(Spawn, Mid)],
            [DepotType()],
            WaterTag);

        var access = ForceTerritoryAccessRules.Evaluate(map, force);

        Assert.True(access.HasFriendlySpawnAccess);
        Assert.Contains(Depot, access.StructureTypeIds);
    }

    [Fact]
    public void EnemyOwnedGapCutsOffSpawnAndStructures()
    {
        var force = Force(Isolated);
        var map = new PlayMap(
            [
                Territory(Spawn, 1, Faction, spawn: Faction),
                Territory(Mid, 2, Enemy),
                Territory(Isolated, 3, Faction, structureTypeId: Depot, structureName: "Supply Depot"),
            ],
            [(Spawn, Mid), (Mid, Isolated)],
            [DepotType()],
            WaterTag);

        var access = ForceTerritoryAccessRules.Evaluate(map, force);

        Assert.False(access.HasFriendlySpawnAccess);
        Assert.Contains(Depot, access.StructureTypeIds);
    }

    [Fact]
    public void AllySpawnIsReachableThroughAlliedLand()
    {
        var force = Force(Mid);
        var allySpawn = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
        var map = new PlayMap(
            [
                Territory(allySpawn, 1, Ally, spawn: Ally),
                Territory(Mid, 2, Faction),
            ],
            [(allySpawn, Mid)]);
        var groups = new Dictionary<Guid, string?>
        {
            [Faction] = "North",
            [Ally] = "North",
        };

        var access = ForceTerritoryAccessRules.Evaluate(map, force, groups);

        Assert.True(access.HasFriendlySpawnAccess);
    }

    [Fact]
    public void SpawningPoolsWaterCountsAsADepotWithoutAPath()
    {
        var force = Force(Isolated);
        var map = new PlayMap(
            [
                Territory(Spawn, 1, Faction, spawn: Faction),
                Territory(Isolated, 2, Faction),
                Territory(Water, 3, Faction, terrainTagIds: [WaterTag]),
            ],
            [],
            [DepotType(), FortType()],
            WaterTag);

        var access = ForceTerritoryAccessRules.Evaluate(
            map,
            force,
            specialRules: Context(SpecialRuleEffectKeys.SpawningPools));

        Assert.Contains(Depot, access.StructureTypeIds);
        Assert.Contains(Fort, access.StructureTypeIds);
        Assert.False(access.HasFriendlySpawnAccess);
    }

    [Fact]
    public void ExtraSpecialRuleTerritoriesDoNotBridgeOwnedClusters()
    {
        var force = Force(Isolated);
        var far = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2");
        var map = new PlayMap(
            [
                Territory(Spawn, 1, Faction, spawn: Faction),
                Territory(Isolated, 2, Faction),
                Territory(Water, 3, Faction, terrainTagIds: [WaterTag]),
                Territory(far, 4, Faction, structureTypeId: Watchtower, structureName: "Watchtower"),
            ],
            [(Water, far)],
            [DepotType(), FortType(), WatchtowerType()],
            WaterTag);

        var access = ForceTerritoryAccessRules.Evaluate(
            map,
            force,
            specialRules: Context(SpecialRuleEffectKeys.SpawningPools));

        Assert.False(access.HasFriendlySpawnAccess);
        Assert.DoesNotContain(Watchtower, access.StructureTypeIds);
    }

    [Fact]
    public void GreenTideEmptyOwnedLandOnTheChainCountsAsADepot()
    {
        var force = Force(Isolated);
        var map = new PlayMap(
            [
                Territory(Spawn, 1, Faction, spawn: Faction),
                Territory(Isolated, 2, Faction),
            ],
            [],
            [DepotType()],
            WaterTag);

        var access = ForceTerritoryAccessRules.Evaluate(
            map,
            force,
            specialRules: Context(SpecialRuleEffectKeys.GreenTide));

        Assert.Contains(Depot, access.StructureTypeIds);
        Assert.False(access.HasFriendlySpawnAccess);
    }

    [Fact]
    public void DefendersNeutralTownIsADepotWithoutAPath()
    {
        var town = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4");
        var force = Force(Isolated);
        var map = new PlayMap(
            [
                Territory(Spawn, 1, Faction, spawn: Faction),
                Territory(Isolated, 2, Faction),
                Territory(town, 3, null, structureTypeId: town, structureName: "Town"),
            ],
            [],
            [DepotType(), new StructureTypePlayRules(town, "Town", false, true, true)],
            WaterTag);

        var access = ForceTerritoryAccessRules.Evaluate(
            map,
            force,
            specialRules: Context(SpecialRuleEffectKeys.DefendersOfTheHomeland));

        Assert.Contains(Depot, access.StructureTypeIds);
        Assert.Contains(town, access.StructureTypeIds);
        Assert.False(access.HasFriendlySpawnAccess);
    }

    private static CampaignForce Force(Guid territoryId)
    {
        return new CampaignForce(Guid.NewGuid(), Player, Faction, territoryId, false);
    }

    private static PlayTerritory Territory(
        Guid id,
        int number,
        Guid? owner,
        Guid? spawn = null,
        Guid? structureTypeId = null,
        string? structureName = null,
        IReadOnlyList<Guid>? terrainTagIds = null)
    {
        return new PlayTerritory(
            id,
            number,
            owner,
            spawn,
            structureTypeId,
            structureName,
            StructureCondition.Operational,
            terrainTagIds: terrainTagIds,
            structureTagIds: structureTypeId is null ? [] : []);
    }

    private static StructureTypePlayRules DepotType()
    {
        return new StructureTypePlayRules(Depot, "Supply Depot", false, true, true);
    }

    private static StructureTypePlayRules FortType()
    {
        return new StructureTypePlayRules(Fort, "Fortification", false, true, true);
    }

    private static StructureTypePlayRules WatchtowerType()
    {
        return new StructureTypePlayRules(Watchtower, "Watchtower", false, true, true);
    }

    private static SpecialRuleContext Context(string effectKey)
    {
        var ruleId = Guid.NewGuid();
        return new SpecialRuleContext(
            [new SpecialRuleSetup(ruleId, effectKey, "Rule text.", effectKey)],
            new Dictionary<Guid, IReadOnlyList<Guid>> { [Faction] = [ruleId] },
            new Dictionary<(Guid, string), IReadOnlyList<Guid>>());
    }
}
