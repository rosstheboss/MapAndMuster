using MapAndMuster.Domain.Campaigns;
using MapAndMuster.Domain.Play;

namespace MapAndMuster.Backend.UnitTests.Play;

public sealed class MissionAdvantageRulesTests
{
    [Fact]
    public void ArmyPointNumberIncreaseIsAppliedThenClamped()
    {
        var mission = AdvantageMission(hasArmy: true, armyAmount: 250);
        Assert.Equal(1250, MissionAdvantageRules.ApplyArmyPoints(1000, mission, isAdvantagedSide: true));
        Assert.Equal(1000, MissionAdvantageRules.ApplyArmyPoints(1000, mission, isAdvantagedSide: false));
    }

    [Fact]
    public void ArmyPointPercentDecreaseNeverGoesBelow500()
    {
        var mission = AdvantageMission(hasArmy: true, armyAmount: -80, armyIsPercent: true);
        Assert.Equal(500, MissionAdvantageRules.ApplyArmyPoints(1000, mission, isAdvantagedSide: true));
    }

    [Fact]
    public void ArmyPointNumberDecreaseNeverGoesBelow500()
    {
        var mission = AdvantageMission(hasArmy: true, armyAmount: -800);
        Assert.Equal(500, MissionAdvantageRules.ApplyArmyPoints(1000, mission, isAdvantagedSide: true));
    }

    [Fact]
    public void SupplyPointChangeNeverGoesBelow1()
    {
        var mission = AdvantageMission(hasSupply: true, supplyAmount: -5);
        Assert.Equal(1, MissionAdvantageRules.ApplySupplyPoints(3, mission, isAdvantagedSide: true));
        Assert.Equal(8, MissionAdvantageRules.ApplySupplyPoints(3, AdvantageMission(hasSupply: true, supplyAmount: 5), true));
        Assert.Equal(3, MissionAdvantageRules.ApplySupplyPoints(3, mission, isAdvantagedSide: false));
    }

    private static MissionSetup AdvantageMission(
        bool hasArmy = false,
        int armyAmount = 0,
        bool armyIsPercent = false,
        bool hasSupply = false,
        int supplyAmount = 0)
    {
        return new MissionSetup(
            Guid.NewGuid(),
            "Assault",
            null,
            false,
            isAttackerDefender: true,
            hasArmyPointsAdvantage: hasArmy,
            armyPointsAdvantageSide: MissionAdvantageSide.Defender,
            armyPointsAdvantageIsPercent: armyIsPercent,
            armyPointsAdvantageAmount: armyAmount,
            hasSupplyPointsAdvantage: hasSupply,
            supplyPointsAdvantageSide: MissionAdvantageSide.Defender,
            supplyPointsAdvantageAmount: supplyAmount);
    }
}
