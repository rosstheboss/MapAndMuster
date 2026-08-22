using MapAndMuster.Domain.Play;

namespace MapAndMuster.Backend.UnitTests.Play;

public sealed class CombatantStrengthRulesTests
{
    [Fact]
    public void CompareUsesCampaignPointsThenTerritoriesThenStructuresThenSupply()
    {
        var weaker = new CombatantStrengthRules.Strength(1, 5, 5, 5);
        var strongerPoints = new CombatantStrengthRules.Strength(2, 0, 0, 0);
        var strongerTerritories = new CombatantStrengthRules.Strength(1, 6, 0, 0);
        var strongerStructures = new CombatantStrengthRules.Strength(1, 5, 6, 0);
        var strongerSupply = new CombatantStrengthRules.Strength(1, 5, 5, 6);

        Assert.True(CombatantStrengthRules.Compare(strongerPoints, weaker) > 0);
        Assert.True(CombatantStrengthRules.Compare(strongerTerritories, weaker) > 0);
        Assert.True(CombatantStrengthRules.Compare(strongerStructures, weaker) > 0);
        Assert.True(CombatantStrengthRules.Compare(strongerSupply, weaker) > 0);
        Assert.Equal(0, CombatantStrengthRules.Compare(weaker, weaker));
    }

    [Fact]
    public void RankBreaksRemainingTiesWithPickIndex()
    {
        var first = "a";
        var second = "b";
        var strength = new CombatantStrengthRules.Strength(1, 1, 1, 1);
        var ranked = CombatantStrengthRules.Rank(
            [first, second],
            _ => strength,
            pickIndex: count => count - 1);

        Assert.Equal(["b", "a"], ranked);
    }
}
