using Campaign.Domain.Campaigns;
using Campaign.Domain.Play;

namespace Campaign.Backend.UnitTests.Play;

public sealed class SupplyRulesTests
{
    private static readonly Guid Player = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid Faction = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Spawn = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Adjacent = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Terrain = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid Keep = Guid.Parse("55555555-5555-5555-5555-555555555555");

    [Fact]
    public void MapSupplyAddsOwnedConnectedTerrainAndOperationalStructures()
    {
        var state = EmptyState(forceCount: 1);
        var map = MapWithKeep();
        var snapshot = SupplyRules.ForPlayer(state, map, Catalog(), Player, roundNumber: 1);

        Assert.Equal(3, snapshot.MapSupplyPoints);
        Assert.Equal(1, snapshot.RoundFreeSupplyPoints);
        Assert.Equal(0, snapshot.SplitPenaltyPoints);
        Assert.Equal(4, snapshot.CurrentSupplyPoints);
        Assert.Equal(500, snapshot.MaxArmyPoints);
        Assert.False(snapshot.IsSplit);
    }

    [Fact]
    public void MapSupplyCountsConnectedAlliedTerrainAndOperationalStructures()
    {
        var ally = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
        var state = EmptyState(forceCount: 1);
        var map = new PlayMap(
            [
                new PlayTerritory(Spawn, 1, Faction, Faction, null, null, StructureCondition.Operational, terrainTypeId: Terrain),
                new PlayTerritory(
                    Adjacent,
                    2,
                    ally,
                    null,
                    Keep,
                    "Keep",
                    StructureCondition.Operational,
                    isPillageable: true,
                    isDestructible: true,
                    terrainTypeId: Terrain),
            ],
            [(Spawn, Adjacent)],
            [new StructureTypePlayRules(Keep, "Keep", true, true, true, 1, 1, 1)]);
        var catalog = new SupplyCatalog(
            new Dictionary<Guid, int> { [Terrain] = 1 },
            new Dictionary<Guid, StructureSupplyRules> { [Keep] = new(1, 1, 1) },
            HuntInEstaliaDefaults.SplitForceSupplyPenaltyPercent,
            HuntInEstaliaDefaults.ArmyEscalations(8),
            new Dictionary<Guid, Guid> { [Player] = Faction },
            new Dictionary<Guid, string?> { [Faction] = "League", [ally] = "League" },
            new HashSet<Guid>());
        var snapshot = SupplyRules.ForPlayer(state, map, catalog, Player, roundNumber: 1);

        Assert.Equal(3, snapshot.MapSupplyPoints);
    }

    [Fact]
    public void SplitForcesApplyHuntInEstaliaPercentPenalty()
    {
        var state = EmptyState(forceCount: 2, territoryId: Spawn);
        var map = MapWithKeep();
        var snapshot = SupplyRules.ForPlayer(state, map, Catalog(), Player, roundNumber: 3);

        Assert.True(snapshot.IsSplit);
        Assert.Equal(3, snapshot.MapSupplyPoints);
        Assert.Equal(1, snapshot.RoundFreeSupplyPoints);
        Assert.Equal(0, snapshot.SplitPenaltyPoints);
        Assert.Equal(4, snapshot.ForceAllowancePoints);
        Assert.Equal(4, snapshot.CurrentSupplyPoints);
    }

    [Fact]
    public void TemporarySupplyFromPillageIsSpendableImmediately()
    {
        var forces = new[]
        {
            new CampaignForce(
                Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Player,
                Faction,
                Adjacent,
                inBattle: false),
        };
        var before = MapWithKeep();
        var after = new PlayMap(
            [
                before.Territory(Spawn)!,
                new PlayTerritory(
                    Adjacent,
                    2,
                    Faction,
                    null,
                    Keep,
                    "Keep",
                    StructureCondition.Pillaged,
                    isPillageable: true,
                    isDestructible: true,
                    terrainTypeId: Terrain),
            ],
            [(Spawn, Adjacent)],
            [new StructureTypePlayRules(Keep, "Keep", true, true, true, 1, 1, 1)]);
        var balances = SupplyRules.AwardTemporary([], before, after, forces, Catalog());
        var state = new CampaignPlayState([], forces, [], [], [], [], [], [], [], [], [], [], playerSupplies: balances);
        var snapshot = SupplyRules.ForPlayer(state, after, Catalog(), Player, roundNumber: 1);

        Assert.Equal(1, snapshot.TemporarySupplyPoints);
        Assert.Equal(snapshot.ForceAllowancePoints + snapshot.TemporarySupplyPoints, snapshot.CurrentSupplyPoints);
        Assert.Equal(snapshot.MapSupplyPoints + snapshot.RoundFreeSupplyPoints - snapshot.SplitPenaltyPoints, snapshot.ForceAllowancePoints);
    }

    [Fact]
    public void TemporarySpendOnSplitForcesRequiresAPointPerForce()
    {
        var balances = new[] { new PlayerSupplyBalance(Player, 1) };

        Assert.Equal(2, SupplyRules.TemporaryPointsRequired([1, 1]));
        var remaining = SupplyRules.SpendTemporary(balances, Player, [1, 1]);

        Assert.Empty(remaining);
    }

    [Fact]
    public void TemporarySpendCanBeAssignedToEitherForceFromThePlayerPool()
    {
        var balances = new[] { new PlayerSupplyBalance(Player, 1) };
        var remaining = SupplyRules.SpendTemporary(balances, Player, [1]);

        Assert.Empty(remaining);
        Assert.Equal(1, SupplyRules.TemporaryPointsRequired([1, 0]));
        Assert.Equal(1, SupplyRules.TemporaryPointsRequired([0, 1]));
    }

    [Fact]
    public void AllocateSpendTakesForceAllowanceBeforeTemporary()
    {
        var (recurring, temporary) = SupplyRules.AllocateSpend(supplyCostingUnitCount: 5, forceAllowancePoints: 3);

        Assert.Equal(3, recurring);
        Assert.Equal(2, temporary);
    }

    private static CampaignPlayState EmptyState(int forceCount, Guid? territoryId = null)
    {
        var location = territoryId ?? Spawn;
        var forces = Enumerable.Range(0, forceCount)
            .Select(index => new CampaignForce(
                Guid.Parse($"00000000-0000-0000-0000-00000000000{index + 1}"),
                Player,
                Faction,
                location,
                inBattle: false))
            .ToArray();
        return new CampaignPlayState([], forces, [], [], [], [], [], [], [], [], [], []);
    }

    private static PlayMap MapWithKeep()
    {
        var territories = new[]
        {
            new PlayTerritory(Spawn, 1, Faction, Faction, null, null, StructureCondition.Operational, terrainTypeId: Terrain),
            new PlayTerritory(
                Adjacent,
                2,
                Faction,
                null,
                Keep,
                "Keep",
                StructureCondition.Operational,
                isPillageable: true,
                isDestructible: true,
                terrainTypeId: Terrain),
        };
        return new PlayMap(
            territories,
            [(Spawn, Adjacent)],
            [new StructureTypePlayRules(Keep, "Keep", true, true, true, 1, 1, 1)]);
    }

    private static SupplyCatalog Catalog()
    {
        return new SupplyCatalog(
            new Dictionary<Guid, int> { [Terrain] = 1 },
            new Dictionary<Guid, StructureSupplyRules> { [Keep] = new(1, 1, 1) },
            HuntInEstaliaDefaults.SplitForceSupplyPenaltyPercent,
            HuntInEstaliaDefaults.ArmyEscalations(8),
            new Dictionary<Guid, Guid> { [Player] = Faction },
            new Dictionary<Guid, string?> { [Faction] = null },
            new HashSet<Guid>());
    }
}
