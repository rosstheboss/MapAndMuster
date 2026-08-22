using MapAndMuster.Domain.Campaigns;
using MapAndMuster.Domain.Play;

namespace MapAndMuster.Backend.UnitTests.Play;

public sealed class BattleCampaignPointRulesTests
{
    [Fact]
    public void AwardsStraightWinAndDrawPointsWhenDifferentialIsOff()
    {
        var scoring = BattleScoringSetup.Straight(2, 1);

        Assert.Equal(2, BattleCampaignPointRules.WinnerPoints(scoring, isDraw: false, winnerScore: 10, loserScore: 1));
        Assert.Equal(0, BattleCampaignPointRules.LoserPoints(scoring, isDraw: false, winnerScore: 10, loserScore: 1));
        Assert.Equal(1, BattleCampaignPointRules.DrawPoints(scoring, isDraw: true));
    }

    [Fact]
    public void ClampsDifferentialToTheConfiguredRangeWithoutNegativeLoserPointsByDefault()
    {
        var scoring = BattleScoringSetup.Default;

        Assert.Equal(10, BattleCampaignPointRules.WinnerPoints(scoring, isDraw: false, winnerScore: 20, loserScore: 0));
        Assert.Equal(0, BattleCampaignPointRules.LoserPoints(scoring, isDraw: false, winnerScore: 20, loserScore: 0));
        Assert.Equal(3, BattleCampaignPointRules.WinnerPoints(scoring, isDraw: false, winnerScore: 8, loserScore: 5));
    }

    [Fact]
    public void MirrorsClampedWinnerPointsOntoTheLoserWhenNegativesAreAllowed()
    {
        var scoring = new BattleScoringSetup(2, 1, true, 1m, -5, 5, true);

        Assert.Equal(5, BattleCampaignPointRules.WinnerPoints(scoring, isDraw: false, winnerScore: 12, loserScore: 0));
        Assert.Equal(-5, BattleCampaignPointRules.LoserPoints(scoring, isDraw: false, winnerScore: 12, loserScore: 0));
    }

    [Fact]
    public void AppliesTheDifferentialMultiplierBeforeClamping()
    {
        var scoring = new BattleScoringSetup(2, 1, true, 0.5m, 0, 10, false);

        Assert.Equal(4, BattleCampaignPointRules.WinnerPoints(scoring, isDraw: false, winnerScore: 8, loserScore: 0));
    }
}

public sealed class TerritoryChainRulesTests
{
    [Fact]
    public void MeasuresTheLongestSimplePathAmongOwnedTerritories()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        var d = Guid.NewGuid();
        var length = TerritoryChainRules.LongestOwnedChain(
            [a, b, c],
            [
                new CampaignPointAdjacency(a, b),
                new CampaignPointAdjacency(b, c),
                new CampaignPointAdjacency(c, d),
            ]);

        Assert.Equal(3, length);
    }

    [Fact]
    public void ReturnsOneForAnIsolatedOwnedTerritory()
    {
        Assert.Equal(1, TerritoryChainRules.LongestOwnedChain([Guid.NewGuid()], []));
    }
}
