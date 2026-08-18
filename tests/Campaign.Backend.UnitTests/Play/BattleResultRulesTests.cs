using Campaign.Domain.Campaigns;
using Campaign.Domain.Play;

namespace Campaign.Backend.UnitTests.Play;

public sealed class BattleResultRulesTests
{
    private static readonly Guid NorthForce = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid SouthForce = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid Question = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public void EqualBattlePointsAreATrueTie()
    {
        var reports = new[]
        {
            Report(NorthForce, differential: 5, bonus: 1),
            Report(SouthForce, differential: 4, bonus: 2),
        };

        Assert.True(BattleResultRules.TryDeriveOutcome(
            [NorthForce, SouthForce],
            reports,
            out var winner,
            out var isDraw,
            out _,
            out _,
            out var error));
        Assert.Null(error);
        Assert.True(isDraw);
        Assert.Null(winner);
    }

    [Fact]
    public void HigherTotalBattlePointsWins()
    {
        var reports = new[]
        {
            Report(NorthForce, differential: 6, bonus: 0),
            Report(SouthForce, differential: 4, bonus: 0),
        };

        Assert.True(BattleResultRules.TryDeriveOutcome(
            [NorthForce, SouthForce],
            reports,
            out var winner,
            out var isDraw,
            out var winnerScore,
            out var loserScore,
            out _));
        Assert.False(isDraw);
        Assert.Equal(NorthForce, winner);
        Assert.Equal(6, winnerScore);
        Assert.Equal(4, loserScore);
    }

    [Fact]
    public void ExtraCampaignPointsIncludeGeneralKillAndBooleanQuestions()
    {
        var question = new MissionResultQuestionSetup(Question, "Held the shrine?", MissionResultQuestionKind.Boolean, 2, 3);
        var report = new BattleParticipantReport(
            NorthForce,
            10,
            1500,
            4,
            1,
            killedEnemyGeneral: true,
            destroyedEnemySupplyLine: true,
            [new BattleQuestionAnswer(Question, true, null)]);

        Assert.Equal(
            HuntInEstaliaDefaults.GeneralKillCampaignPoints
            + HuntInEstaliaDefaults.SupplyLineDestroyedCampaignPoints
            + 3,
            BattleResultRules.ExtraCampaignPoints(report, BattleReportRulesSetup.Default, [question]));
    }

    private static BattleParticipantReport Report(Guid forceId, int differential, int bonus)
    {
        return new BattleParticipantReport(forceId, 10, 1000, differential, bonus, false, false, []);
    }
}
