using Campaign.Domain.Play;

namespace Campaign.Backend.UnitTests.Play;

public sealed class AlliedArmyPointRulesTests
{
    [Fact]
    public void OnePlayerUsesTheRoundMaximum()
    {
        Assert.Equal(1000, AlliedArmyPointRules.ForceArmyPoints(1000, sidePlayerCount: 1));
    }

    [Fact]
    public void ExtraAlliedPlayerAddsTwentyFivePercentThenRoundsEachShareUpToTen()
    {
        Assert.Equal(630, AlliedArmyPointRules.ForceArmyPoints(1000, sidePlayerCount: 2));
    }
}
