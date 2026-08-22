using MapAndMuster.Domain.Campaigns;

namespace MapAndMuster.Backend.UnitTests.Campaigns;

public sealed class CampaignPhaseLabelsTests
{
    [Fact]
    public void NumbersActionWindowsInRoundOrder()
    {
        var phases = new[]
        {
            new RoundPhaseSetup(RoundPhaseKind.Action, new ScheduleDuration(3, DurationUnit.Days)),
            new RoundPhaseSetup(RoundPhaseKind.Battle, new ScheduleDuration(1, DurationUnit.Days)),
            new RoundPhaseSetup(RoundPhaseKind.Action, new ScheduleDuration(3, DurationUnit.Days)),
        };

        Assert.Equal("Action 1", CampaignPhaseLabels.Format(phases, 1, RoundPhaseKind.Action));
        Assert.Equal("Battle", CampaignPhaseLabels.Format(phases, 2, RoundPhaseKind.Battle));
        Assert.Equal("Action 2", CampaignPhaseLabels.Format(phases, 3, RoundPhaseKind.Action));
    }

    [Fact]
    public void NumbersBattlesWhenARoundHasMoreThanOne()
    {
        var phases = new[]
        {
            new RoundPhaseSetup(RoundPhaseKind.Action, new ScheduleDuration(2, DurationUnit.Days)),
            new RoundPhaseSetup(RoundPhaseKind.Battle, new ScheduleDuration(1, DurationUnit.Days)),
            new RoundPhaseSetup(RoundPhaseKind.Battle, new ScheduleDuration(1, DurationUnit.Days)),
        };

        Assert.Equal("Battle 1", CampaignPhaseLabels.Format(phases, 2, RoundPhaseKind.Battle));
        Assert.Equal("Battle 2", CampaignPhaseLabels.Format(phases, 3, RoundPhaseKind.Battle));
    }
}
