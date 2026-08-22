using MapAndMuster.Domain.Campaigns;

namespace MapAndMuster.Backend.UnitTests.Campaigns;

public sealed class HuntInEstaliaDefaultsTests
{
    [Fact]
    public void ArmyEscalationsMatchTheEightRoundTable()
    {
        var rows = HuntInEstaliaDefaults.ArmyEscalations(8);

        Assert.Equal([500, 750, 1000, 1250, 1500, 2000, 2500, 3000], rows.Select(row => row.MaxArmyPoints));
        Assert.Equal([1, 1, 1, 2, 2, 2, 3, 3], rows.Select(row => row.FreeSupplyPoints));
        Assert.Equal([1, 1, 1, 1, 1, 2, 2, 2], rows.Select(row => row.FreeCharacterCount));
        Assert.Equal(Enumerable.Range(1, 8), rows.Select(row => row.RoundNumber));
    }

    [Fact]
    public void LongerCampaignsCopyTheLastHuntRow()
    {
        var rows = HuntInEstaliaDefaults.ArmyEscalations(9);

        Assert.Equal(3000, rows[8].MaxArmyPoints);
        Assert.Equal(3, rows[8].FreeSupplyPoints);
        Assert.Equal(2, rows[8].FreeCharacterCount);
        Assert.Equal(9, rows[8].RoundNumber);
    }

    [Fact]
    public void SplitForceSupplyPenaltyDefaultsToRawValueOfOne()
    {
        Assert.Equal(1, HuntInEstaliaDefaults.SplitForceSupplyPenaltyValue);
        Assert.False(HuntInEstaliaDefaults.SplitForceSupplyPenaltyIsPercent);
        Assert.Equal(25, HuntInEstaliaDefaults.LegacySplitForceSupplyPenaltyPercent);
    }
}
