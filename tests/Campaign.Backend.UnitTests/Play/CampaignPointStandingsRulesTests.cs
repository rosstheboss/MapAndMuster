using Campaign.Domain.Campaigns;
using Campaign.Domain.Play;

namespace Campaign.Backend.UnitTests.Play;

public sealed class CampaignPointStandingsRulesTests
{
    [Fact]
    public void SumsCurrentStructureHoldingsWithoutTerrainPoints()
    {
        var player = Guid.NewGuid();
        var faction = Guid.NewGuid();
        var town = Guid.NewGuid();
        var standings = CampaignPointStandingsRules.Calculate(new CampaignPointScoringState
        {
            Players = [new CampaignPointPlayer(player, faction)],
            Territories =
            [
                new CampaignPointTerritory(Guid.NewGuid(), faction, town, StructureCondition.Operational),
                new CampaignPointTerritory(Guid.NewGuid(), faction, null, StructureCondition.Operational),
                new CampaignPointTerritory(Guid.NewGuid(), null, town, StructureCondition.Operational),
                new CampaignPointTerritory(Guid.NewGuid(), faction, town, StructureCondition.Destroyed),
            ],
            StructurePoints = new Dictionary<Guid, int> { [town] = 3 },
            ItemPoints = new Dictionary<Guid, int>(),
            PublicObjectivePoints = new Dictionary<Guid, int>(),
            BattleScoring = BattleScoringSetup.Straight(0),
            RankingObjectivePoints = GeneralPublicObjectivePoints.None,
            Battles = [],
            Forces = [],
            VisibleItems = [],
            Awards = [],
        }).Standings;

        var row = Assert.Single(standings);
        Assert.Equal(3, row.TerritoryAndStructurePoints);
        Assert.Equal(row.TerritoryAndStructurePoints, row.Total);
    }

    [Fact]
    public void AwardsConfiguredPointsForFinalizedBattleWins()
    {
        var player = Guid.NewGuid();
        var rival = Guid.NewGuid();
        var winner = Guid.NewGuid();
        var loser = Guid.NewGuid();
        var open = Guid.NewGuid();
        var result = CampaignPointStandingsRules.Calculate(State(
            players: [new CampaignPointPlayer(player, Guid.NewGuid()), new CampaignPointPlayer(rival, Guid.NewGuid())],
            forces:
            [
                new CampaignForce(winner, player, Guid.NewGuid(), Guid.NewGuid(), false),
                new CampaignForce(loser, rival, Guid.NewGuid(), Guid.NewGuid(), false),
                new CampaignForce(open, player, Guid.NewGuid(), Guid.NewGuid(), true),
            ],
            battles:
            [
                Battle(BattleStatus.Finalized, winner, isDraw: false),
                Battle(BattleStatus.GMResolved, winner, isDraw: false),
                Battle(BattleStatus.Finalized, winner, isDraw: true, participants: [winner, loser]),
                Battle(BattleStatus.AwaitingResults, winner, isDraw: false),
            ],
            battleScoring: BattleScoringSetup.Straight(4, pointsPerDraw: 1)));

        Assert.Equal(9, result.Standings.Single(row => row.UserId == player).BattlesWonPoints);
        Assert.Equal(1, result.Standings.Single(row => row.UserId == rival).BattlesWonPoints);
    }

    [Fact]
    public void IgnoresNamedPublicObjectivesConfiguredAtZero()
    {
        var player = Guid.NewGuid();
        var objective = Guid.NewGuid();
        var result = CampaignPointStandingsRules.Calculate(State(
            players: [new CampaignPointPlayer(player, Guid.NewGuid())],
            publicPoints: new Dictionary<Guid, int> { [objective] = 0 },
            awards:
            [
                new PublicObjectiveAward(Guid.NewGuid(), objective, player, true, Guid.NewGuid(), DateTimeOffset.UtcNow),
            ]));

        Assert.Equal(0, Assert.Single(result.Standings).PublicObjectivePoints);
    }

    [Fact]
    public void AwardsRankingPointsToEveryTiedLeader()
    {
        var north = Guid.NewGuid();
        var south = Guid.NewGuid();
        var northFaction = Guid.NewGuid();
        var southFaction = Guid.NewGuid();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var third = Guid.NewGuid();
        var result = CampaignPointStandingsRules.Calculate(State(
            players:
            [
                new CampaignPointPlayer(north, northFaction),
                new CampaignPointPlayer(south, southFaction),
            ],
            territories:
            [
                new CampaignPointTerritory(first, northFaction, null, StructureCondition.Operational),
                new CampaignPointTerritory(second, southFaction, null, StructureCondition.Operational),
            ],
            adjacencies: [new CampaignPointAdjacency(first, third)],
            ranking: new GeneralPublicObjectivePoints(5, 0, 0)));

        Assert.Equal(5, result.Standings.Single(row => row.UserId == north).PublicObjectivePoints);
        Assert.Equal(5, result.Standings.Single(row => row.UserId == south).PublicObjectivePoints);
        var board = Assert.Single(result.Leaderboards);
        Assert.Equal(GeneralPublicObjectiveKinds.MostTerritories, board.Kind);
        Assert.Equal(2, board.Leaders.Count);
        Assert.All(board.Leaders, leader => Assert.True(leader.AwardsPoints));
    }

    [Fact]
    public void UsesDrawsToBreakMostBattlesWonTiesThenAwardsFriendlyRemainders()
    {
        var alpha = Guid.NewGuid();
        var beta = Guid.NewGuid();
        var gamma = Guid.NewGuid();
        var alphaWin = Guid.NewGuid();
        var betaWin = Guid.NewGuid();
        var gammaWin = Guid.NewGuid();
        var result = CampaignPointStandingsRules.Calculate(State(
            players:
            [
                new CampaignPointPlayer(alpha, Guid.NewGuid()),
                new CampaignPointPlayer(beta, Guid.NewGuid()),
                new CampaignPointPlayer(gamma, Guid.NewGuid()),
            ],
            forces:
            [
                new CampaignForce(alphaWin, alpha, Guid.NewGuid(), Guid.NewGuid(), false),
                new CampaignForce(betaWin, beta, Guid.NewGuid(), Guid.NewGuid(), false),
                new CampaignForce(gammaWin, gamma, Guid.NewGuid(), Guid.NewGuid(), false),
            ],
            battles:
            [
                Battle(BattleStatus.Finalized, alphaWin, isDraw: false),
                Battle(BattleStatus.Finalized, betaWin, isDraw: false),
                Battle(BattleStatus.Finalized, gammaWin, isDraw: false),
                Battle(BattleStatus.Finalized, alphaWin, isDraw: true),
                Battle(BattleStatus.Finalized, betaWin, isDraw: true),
            ],
            battleScoring: BattleScoringSetup.Straight(0),
            ranking: new GeneralPublicObjectivePoints(0, 0, 7)));

        Assert.Equal(7, result.Standings.Single(row => row.UserId == alpha).PublicObjectivePoints);
        Assert.Equal(7, result.Standings.Single(row => row.UserId == beta).PublicObjectivePoints);
        Assert.Equal(0, result.Standings.Single(row => row.UserId == gamma).PublicObjectivePoints);
        var board = Assert.Single(result.Leaderboards);
        Assert.Equal(1, board.Leaders.Single(leader => leader.UserId == alpha).Rank);
        Assert.Equal(1, board.Leaders.Single(leader => leader.UserId == beta).TieBreakMetric);
        Assert.Equal(3, board.Leaders.Single(leader => leader.UserId == gamma).Rank);
    }

    [Fact]
    public void CountsActivePublicObjectiveAwardsAndHeldVisibleItems()
    {
        var player = Guid.NewGuid();
        var force = Guid.NewGuid();
        var relic = Guid.NewGuid();
        var banner = Guid.NewGuid();
        var objective = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var standings = CampaignPointStandingsRules.Calculate(State(
            players: [new CampaignPointPlayer(player, Guid.NewGuid())],
            forces: [new CampaignForce(force, player, Guid.NewGuid(), Guid.NewGuid(), false)],
            items:
            [
                new CampaignItemObjective(Guid.NewGuid(), relic, "Crown", null, force, true, Guid.NewGuid(), false),
            ],
            itemPoints: new Dictionary<Guid, int> { [relic] = 5, [banner] = 9 },
            publicPoints: new Dictionary<Guid, int> { [objective] = 7 },
            awards:
            [
                new PublicObjectiveAward(Guid.NewGuid(), objective, player, true, Guid.NewGuid(), now.AddMinutes(-2)),
                new PublicObjectiveAward(Guid.NewGuid(), objective, player, false, Guid.NewGuid(), now.AddMinutes(-1)),
                new PublicObjectiveAward(Guid.NewGuid(), objective, player, true, Guid.NewGuid(), now),
            ])).Standings;

        var row = Assert.Single(standings);
        Assert.Equal(7, row.PublicObjectivePoints);
        Assert.Equal(5, row.OtherPoints);
        Assert.Equal([relic], row.HeldItemTypeIds);
        Assert.Equal(12, row.Total);
    }

    [Fact]
    public void OmitsHiddenItemsThatWereNotSuppliedAsVisible()
    {
        var player = Guid.NewGuid();
        var force = Guid.NewGuid();
        var relic = Guid.NewGuid();
        var standings = CampaignPointStandingsRules.Calculate(State(
            players: [new CampaignPointPlayer(player, Guid.NewGuid())],
            forces: [new CampaignForce(force, player, Guid.NewGuid(), Guid.NewGuid(), false)],
            items: [],
            itemPoints: new Dictionary<Guid, int> { [relic] = 11 })).Standings;

        var row = Assert.Single(standings);
        Assert.Equal(0, row.OtherPoints);
        Assert.Empty(row.HeldItemTypeIds);
    }

    private static CampaignPointScoringState State(
        IReadOnlyList<CampaignPointPlayer>? players = null,
        IReadOnlyList<CampaignPointTerritory>? territories = null,
        IReadOnlyList<CampaignPointAdjacency>? adjacencies = null,
        IReadOnlyList<CampaignForce>? forces = null,
        IReadOnlyList<CampaignBattle>? battles = null,
        IReadOnlyList<CampaignItemObjective>? items = null,
        IReadOnlyDictionary<Guid, int>? itemPoints = null,
        IReadOnlyDictionary<Guid, int>? publicPoints = null,
        IReadOnlyList<PublicObjectiveAward>? awards = null,
        BattleScoringSetup? battleScoring = null,
        GeneralPublicObjectivePoints? ranking = null)
    {
        return new CampaignPointScoringState
        {
            Players = players ?? [],
            Territories = territories ?? [],
            Adjacencies = adjacencies ?? [],
            StructurePoints = new Dictionary<Guid, int>(),
            ItemPoints = itemPoints ?? new Dictionary<Guid, int>(),
            PublicObjectivePoints = publicPoints ?? new Dictionary<Guid, int>(),
            BattleScoring = battleScoring ?? BattleScoringSetup.Straight(0),
            RankingObjectivePoints = ranking ?? GeneralPublicObjectivePoints.None,
            Battles = battles ?? [],
            Forces = forces ?? [],
            VisibleItems = items ?? [],
            Awards = awards ?? [],
        };
    }

    private static CampaignBattle Battle(
        BattleStatus status,
        Guid winnerForceId,
        bool isDraw,
        IReadOnlyList<Guid>? participants = null)
    {
        return new CampaignBattle(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            status,
            participants ?? [winnerForceId],
            winnerForceId,
            isDraw,
            DateTimeOffset.UtcNow);
    }
}
