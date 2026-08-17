using Campaign.Domain.Campaigns;
using Campaign.Domain.Play;

namespace Campaign.Backend.UnitTests.Play;

public sealed class ItemObjectiveRulesTests
{
    private static readonly Guid North = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid South = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid NorthSpawn = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SouthSpawn = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Midland = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid PlayerOne = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid TypeId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly DateTimeOffset Now = new(2026, 8, 16, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void SeedPlacesRandomItemsOffSpawnAndUsesPlacedTerritories()
    {
        var map = CreateMap();
        var randomType = HiddenType(ItemObjectivePlacementKind.Random, allowOnSpawn: false);
        var placedType = new ItemObjectiveTypePlayRules(
            Guid.Parse("77777777-7777-7777-7777-777777777777"),
            "Banner",
            isHiddenUntilFound: false,
            ItemObjectivePlacementKind.Placed,
            allowOnSpawn: true);

        var seeded = ItemObjectiveRules.Seed(
            [randomType, placedType],
            map,
            [new ItemObjectiveMapPlacement(placedType.Id, NorthSpawn)],
            static _ => 0);

        Assert.Equal(2, seeded.Count);
        var crown = Assert.Single(seeded, item => item.Name == "Crown");
        Assert.Equal(Midland, crown.TerritoryId);
        Assert.False(crown.IsRevealed);
        var banner = Assert.Single(seeded, item => item.Name == "Banner");
        Assert.Equal(NorthSpawn, banner.TerritoryId);
        Assert.True(banner.IsRevealed);
    }

    [Fact]
    public void SeedSkipsSpawnUnlessAllowedAndSkipsPlacedItemsWithoutAValidTerritory()
    {
        var map = CreateMap();
        var spawnOnly = HiddenType(ItemObjectivePlacementKind.Random, allowOnSpawn: false);
        var spawnMap = new PlayMap(
            [
                new PlayTerritory(NorthSpawn, 1, North, North, null, null, StructureCondition.Operational),
                new PlayTerritory(SouthSpawn, 2, South, South, null, null, StructureCondition.Operational),
            ],
            [(NorthSpawn, SouthSpawn)]);

        Assert.Empty(ItemObjectiveRules.Seed([spawnOnly], spawnMap, [], static _ => 0));

        var placed = new ItemObjectiveTypePlayRules(
            TypeId,
            "Crown",
            true,
            ItemObjectivePlacementKind.Placed,
            false);
        Assert.Empty(ItemObjectiveRules.Seed([placed], map, [], static _ => 0));
        Assert.Empty(ItemObjectiveRules.Seed(
            [placed],
            map,
            [new ItemObjectiveMapPlacement(TypeId, NorthSpawn)],
            static _ => 0));
    }

    [Fact]
    public void MovingForceDropsCarriedItemOnTheTerritoryItLeft()
    {
        var forceId = Guid.Parse("88888888-8888-8888-8888-888888888888");
        var item = new CampaignItemObjective(
            Guid.NewGuid(),
            TypeId,
            "Crown",
            territoryId: null,
            possessorForceId: forceId,
            isRevealed: true,
            Midland,
            wasHiddenUntilFound: true);
        var log = new List<PlayLogEntry>();

        var next = ItemObjectiveRules.DropCarriedByMovers(
            [item],
            new Dictionary<Guid, Guid> { [forceId] = Midland },
            Now,
            log);

        var dropped = Assert.Single(next);
        Assert.Equal(Midland, dropped.TerritoryId);
        Assert.Null(dropped.PossessorForceId);
        Assert.Contains(log, entry => entry.Kind == PlayLogKind.ItemObjectiveDropped);
    }

    [Fact]
    public void LoneForcePicksUpUnpossessedItemAndRevealsIt()
    {
        var force = new CampaignForce(Guid.NewGuid(), PlayerOne, North, Midland, false);
        var item = new CampaignItemObjective(
            Guid.NewGuid(),
            TypeId,
            "Crown",
            Midland,
            possessorForceId: null,
            isRevealed: false,
            Midland,
            wasHiddenUntilFound: true);
        var log = new List<PlayLogEntry>();

        var next = ItemObjectiveRules.PickUpUnpossessed([item], [force], Now, log);

        var taken = Assert.Single(next);
        Assert.Equal(force.Id, taken.PossessorForceId);
        Assert.Null(taken.TerritoryId);
        Assert.True(taken.IsRevealed);
        Assert.Contains(log, entry => entry.Kind == PlayLogKind.ItemObjectiveFound);
    }

    [Fact]
    public void BattleWinnerTakesItemsOnTheFieldAndFromParticipants()
    {
        var winner = Guid.Parse("88888888-8888-8888-8888-888888888888");
        var loser = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var held = new CampaignItemObjective(
            Guid.NewGuid(),
            TypeId,
            "Crown",
            territoryId: null,
            possessorForceId: loser,
            isRevealed: true,
            Midland,
            false);
        var ground = new CampaignItemObjective(
            Guid.NewGuid(),
            Guid.Parse("77777777-7777-7777-7777-777777777777"),
            "Banner",
            Midland,
            possessorForceId: null,
            isRevealed: false,
            Midland,
            true);
        var battle = new CampaignBattle(
            Guid.NewGuid(),
            Midland,
            Guid.NewGuid(),
            null,
            BattleStatus.Finalized,
            [winner, loser],
            winner,
            false,
            Now);
        var log = new List<PlayLogEntry>();

        var next = ItemObjectiveRules.AwardBattleSpoils(
            [held, ground],
            battle,
            [],
            Now,
            log);

        Assert.All(next, item =>
        {
            Assert.Equal(winner, item.PossessorForceId);
            Assert.True(item.IsRevealed);
        });
        Assert.Contains(log, entry => entry.Kind == PlayLogKind.ItemObjectivePickedUp);
        Assert.Contains(log, entry => entry.Kind == PlayLogKind.ItemObjectiveFound);
    }

    [Fact]
    public void StaffRevealRequiresDebugAndLeavesLocationsUnchanged()
    {
        var actor = PlayerOne;
        var item = new CampaignItemObjective(
            Guid.NewGuid(),
            TypeId,
            "Crown",
            Midland,
            possessorForceId: null,
            isRevealed: false,
            Midland,
            true);
        var state = CampaignPlayState.Empty.With(itemObjectives: [item]);

        Assert.False(ItemObjectiveRules.TryRevealHidden(state, actor, Now, out _, out var error));
        Assert.Equal("debug.required", error?.Code);

        var debug = state.With(debugActorUserId: actor, debugStartedUtc: Now);
        Assert.True(ItemObjectiveRules.TryRevealHidden(debug, actor, Now, out var next, out _));
        var revealed = Assert.Single(next!.ItemObjectives);
        Assert.True(revealed.IsRevealed);
        Assert.Equal(Midland, revealed.TerritoryId);
        Assert.Contains(next.Log, entry => entry.Kind == PlayLogKind.ItemObjectivesStaffRevealed);
    }

    private static ItemObjectiveTypePlayRules HiddenType(ItemObjectivePlacementKind placement, bool allowOnSpawn)
    {
        return new ItemObjectiveTypePlayRules(TypeId, "Crown", true, placement, allowOnSpawn);
    }

    private static PlayMap CreateMap()
    {
        var territories = new[]
        {
            new PlayTerritory(NorthSpawn, 1, North, North, null, null, StructureCondition.Operational),
            new PlayTerritory(Midland, 2, null, null, null, null, StructureCondition.Operational),
            new PlayTerritory(SouthSpawn, 3, South, South, null, null, StructureCondition.Operational),
        };
        return new PlayMap(
            territories,
            [(NorthSpawn, Midland), (Midland, SouthSpawn), (NorthSpawn, SouthSpawn)]);
    }
}
