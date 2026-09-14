using System.Globalization;
using MapAndMuster.Domain.Campaigns;
using MapAndMuster.Domain.Play;

namespace MapAndMuster.Backend.UnitTests.Play;

public sealed class RivalObjectiveRulesTests
{
    private static readonly Guid PlayerOne = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid PlayerTwo = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid PlayerThree = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid North = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid South = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid East = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-01T12:00:00Z", CultureInfo.InvariantCulture);

    [Fact]
    public void SeedInitialAssignsEachPlayerADistinctEnemy()
    {
        var assigned = RivalObjectiveRules.SeedInitial(
            [PlayerOne, PlayerTwo, PlayerThree],
            new Dictionary<Guid, Guid>
            {
                [PlayerOne] = North,
                [PlayerTwo] = South,
                [PlayerThree] = East,
            },
            new Dictionary<Guid, string?>
            {
                [North] = null,
                [South] = null,
                [East] = null,
            },
            [],
            Now,
            static _ => 0);

        Assert.Equal(3, assigned.Count);
        Assert.Equal(3, assigned.Select(item => item.RivalUserId).Distinct().Count());
        Assert.All(assigned, item => Assert.NotEqual(item.HolderUserId, item.RivalUserId));
        Assert.All(assigned, item => Assert.Equal(5, item.CampaignPoints));
    }

    [Fact]
    public void SeedInitialSkipsAlliedPlayers()
    {
        var assigned = RivalObjectiveRules.SeedInitial(
            [PlayerOne, PlayerTwo],
            new Dictionary<Guid, Guid>
            {
                [PlayerOne] = North,
                [PlayerTwo] = South,
            },
            new Dictionary<Guid, string?>
            {
                [North] = "Pact",
                [South] = "Pact",
            },
            [],
            Now,
            static _ => 0);

        Assert.Empty(assigned);
    }

    [Fact]
    public void ApplyVictoriesRevealsWhenTheHolderWinsTheBattle()
    {
        var holderForce = new CampaignForce(Guid.NewGuid(), PlayerOne, North, Guid.NewGuid(), true);
        var rivalForce = new CampaignForce(Guid.NewGuid(), PlayerTwo, South, holderForce.TerritoryId, true);
        var assignment = new RivalObjectiveAssignment(
            Guid.NewGuid(),
            PlayerOne,
            PlayerTwo,
            5,
            PrivateObjectiveAssignmentStatus.Assigned,
            Now);
        var battle = new CampaignBattle(
            Guid.NewGuid(),
            holderForce.TerritoryId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            BattleStatus.Finalized,
            [holderForce.Id, rivalForce.Id],
            holderForce.Id,
            false,
            Now);
        var state = PlayState([holderForce, rivalForce], [assignment], [battle]);

        var next = RivalObjectiveRules.ApplyVictories(state, battle, Now.AddHours(1));

        var revealed = Assert.Single(next.RivalObjectives);
        Assert.Equal(PrivateObjectiveAssignmentStatus.Revealed, revealed.Status);
        Assert.Contains(next.Log, item => item.Kind == PlayLogKind.RivalObjectiveRevealed);
        Assert.Equal(5, RivalObjectiveRules.PointsForPlayer(next, PlayerOne));
    }

    [Fact]
    public void ApplyVictoriesIgnoresNoContestDrawAndRingerBattles()
    {
        var holderForce = new CampaignForce(Guid.NewGuid(), PlayerOne, North, Guid.NewGuid(), true);
        var rivalForce = new CampaignForce(Guid.NewGuid(), PlayerTwo, South, holderForce.TerritoryId, true);
        var assignment = new RivalObjectiveAssignment(
            Guid.NewGuid(),
            PlayerOne,
            PlayerTwo,
            5,
            PrivateObjectiveAssignmentStatus.Assigned,
            Now);
        var battle = new CampaignBattle(
            Guid.NewGuid(),
            holderForce.TerritoryId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            BattleStatus.Finalized,
            [holderForce.Id, rivalForce.Id],
            holderForce.Id,
            false,
            Now,
            isNoContest: true);
        var state = PlayState([holderForce, rivalForce], [assignment], [battle]);

        var next = RivalObjectiveRules.ApplyVictories(state, battle, Now.AddHours(1));

        Assert.Equal(PrivateObjectiveAssignmentStatus.Assigned, Assert.Single(next.RivalObjectives).Status);
        Assert.DoesNotContain(next.Log, item => item.Kind == PlayLogKind.RivalObjectiveRevealed);
    }

    [Fact]
    public void ReplenishAssignsTheClosestRemainingEnemy()
    {
        var northLand = Guid.Parse("01010101-0101-0101-0101-010101010101");
        var midland = Guid.Parse("02020202-0202-0202-0202-020202020202");
        var southLand = Guid.Parse("03030303-0303-0303-0303-030303030303");
        var eastLand = Guid.Parse("04040404-0404-0404-0404-040404040404");
        var map = new PlayMap(
            [
                new PlayTerritory(northLand, 1, North, North, null, null, StructureCondition.Operational),
                new PlayTerritory(midland, 2, null, null, null, null, StructureCondition.Operational),
                new PlayTerritory(southLand, 3, South, South, null, null, StructureCondition.Operational),
                new PlayTerritory(eastLand, 4, East, East, null, null, StructureCondition.Operational),
            ],
            [(northLand, midland), (midland, southLand), (northLand, eastLand)]);
        var window = new PhaseWindow(
            Guid.NewGuid(),
            2,
            1,
            RoundPhaseKind.Action,
            1,
            DurationUnit.Days,
            Now,
            Now.AddDays(1),
            PhaseWindowStatus.Open);
        var later = new PhaseWindow(
            Guid.NewGuid(),
            3,
            1,
            RoundPhaseKind.Action,
            1,
            DurationUnit.Days,
            Now.AddDays(1),
            Now.AddDays(2),
            PhaseWindowStatus.Pending);
        var forces = new[]
        {
            new CampaignForce(Guid.NewGuid(), PlayerOne, North, northLand, false),
            new CampaignForce(Guid.NewGuid(), PlayerTwo, South, southLand, false),
            new CampaignForce(Guid.NewGuid(), PlayerThree, East, eastLand, false),
        };
        var prior = new RivalObjectiveAssignment(
            Guid.NewGuid(),
            PlayerOne,
            PlayerTwo,
            5,
            PrivateObjectiveAssignmentStatus.Revealed,
            Now.AddDays(-7),
            Now);
        var state = new CampaignPlayState(
            [window, later],
            forces,
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            rivalObjectives: [prior]);

        var next = RivalObjectiveRules.Replenish(
            state,
            map,
            new Dictionary<Guid, string?>
            {
                [North] = null,
                [South] = null,
                [East] = null,
            },
            Now,
            static _ => 0);

        var active = Assert.Single(next.RivalObjectives, item => item.IsActive);
        Assert.Equal(PlayerOne, active.HolderUserId);
        Assert.Equal(PlayerThree, active.RivalUserId);
    }

    private static CampaignPlayState PlayState(
        IReadOnlyList<CampaignForce> forces,
        IReadOnlyList<RivalObjectiveAssignment> rivals,
        IReadOnlyList<CampaignBattle> battles)
    {
        return new CampaignPlayState(
            [],
            forces,
            [],
            [],
            [],
            battles,
            [],
            [],
            [],
            [],
            [],
            [],
            rivalObjectives: rivals);
    }
}
