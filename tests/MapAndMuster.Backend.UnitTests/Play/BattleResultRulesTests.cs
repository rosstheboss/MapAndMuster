using MapAndMuster.Domain.Campaigns;
using MapAndMuster.Domain.Play;

namespace MapAndMuster.Backend.UnitTests.Play;

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

    [Fact]
    public void ExtraBlackPowderAndMagicalSupplyRequireTheMatchingRule()
    {
        var empire = new CampaignForce(NorthForce, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), false);
        var tzeentch = new CampaignForce(SouthForce, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), false, subfaction: "Tzeentch");
        var powder = Context(empire.FactionId, SpecialRuleEffectKeys.PreparedForBattle);
        var magic = Context(tzeentch.FactionId, SpecialRuleEffectKeys.MagicalSupply, tzeentch.Subfaction);

        Assert.False(BattleResultRules.TryValidateSpecialRuleUses(
            [Report(NorthForce, 5, 0, usedExtraBlackPowder: true)],
            [empire],
            SpecialRuleContext.None,
            null,
            out var forbiddenPowder));
        Assert.Equal("battle.result.extra_black_powder.forbidden", forbiddenPowder!.Code);

        Assert.True(BattleResultRules.TryValidateSpecialRuleUses(
            [Report(NorthForce, 5, 0, usedExtraBlackPowder: true)],
            [empire],
            powder,
            null,
            out var allowedPowder));
        Assert.Null(allowedPowder);

        Assert.False(BattleResultRules.TryValidateSpecialRuleUses(
            [Report(SouthForce, 5, 0, magicalSupplyRerolls: 2)],
            [tzeentch],
            SpecialRuleContext.None,
            null,
            out var forbiddenMagic));
        Assert.Equal("battle.result.magical_supply.forbidden", forbiddenMagic!.Code);

        Assert.False(BattleResultRules.TryValidateSpecialRuleUses(
            [Report(SouthForce, 5, 0, magicalSupplyRerolls: 3)],
            [tzeentch],
            magic,
            new Dictionary<Guid, int> { [SouthForce] = 2 },
            out var tooMany));
        Assert.Equal("battle.result.magical_supply.exceeds_leftover", tooMany!.Code);

        Assert.True(BattleResultRules.TryValidateSpecialRuleUses(
            [Report(SouthForce, 5, 0, magicalSupplyRerolls: 2)],
            [tzeentch],
            magic,
            new Dictionary<Guid, int> { [SouthForce] = 2 },
            out var allowedMagic));
        Assert.Null(allowedMagic);
        Assert.Equal(4, Report(NorthForce, 5, 0, supplyCostingUnitCount: 3, usedExtraBlackPowder: true).SupplySpend);
    }

    private static BattleParticipantReport Report(
        Guid forceId,
        int differential,
        int bonus,
        int supplyCostingUnitCount = 0,
        bool usedExtraBlackPowder = false,
        int magicalSupplyRerolls = 0)
    {
        return new BattleParticipantReport(
            forceId,
            10,
            1000,
            differential,
            bonus,
            false,
            false,
            [],
            supplyCostingUnitCount,
            usedExtraBlackPowder: usedExtraBlackPowder,
            magicalSupplyRerolls: magicalSupplyRerolls);
    }

    private static SpecialRuleContext Context(Guid factionId, string effectKey, string? subfaction = null)
    {
        var ruleId = Guid.NewGuid();
        var subfactionRules = new Dictionary<(Guid, string), IReadOnlyList<Guid>>();
        if (!string.IsNullOrWhiteSpace(subfaction))
        {
            subfactionRules[(factionId, subfaction)] = [ruleId];
        }

        return new SpecialRuleContext(
            [new SpecialRuleSetup(ruleId, effectKey, "Rule text.", effectKey)],
            new Dictionary<Guid, IReadOnlyList<Guid>> { [factionId] = [ruleId] },
            subfactionRules);
    }
}
