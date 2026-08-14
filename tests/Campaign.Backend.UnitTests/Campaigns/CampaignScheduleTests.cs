using Campaign.Domain.Campaigns;

namespace Campaign.Backend.UnitTests.Campaigns;

public sealed class CampaignScheduleTests
{
    [Fact]
    public void ReportsScheduledBeforeStart()
    {
        var schedule = CreateWeekSchedule();
        var progress = schedule.Evaluate(new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero));

        Assert.Equal(CampaignStatus.Scheduled, progress.Status);
        Assert.Null(progress.CurrentRound);
        Assert.Null(progress.CurrentPhaseKind);
    }

    [Fact]
    public void ReportsFirstActionAtStart()
    {
        var schedule = CreateWeekSchedule();
        var progress = schedule.Evaluate(schedule.StartsUtc);

        Assert.Equal(CampaignStatus.InProgress, progress.Status);
        Assert.Equal(1, progress.CurrentRound);
        Assert.Equal(1, progress.CurrentPhaseNumber);
        Assert.Equal(RoundPhaseKind.Action, progress.CurrentPhaseKind);
    }

    [Fact]
    public void ReportsBattleAfterActionWindows()
    {
        var schedule = CreateWeekSchedule();
        var battleStart = CampaignCalendar.Add(
            CampaignCalendar.Add(schedule.StartsUtc, schedule.TimeZone, new ScheduleDuration(3, DurationUnit.Days)),
            schedule.TimeZone,
            new ScheduleDuration(3, DurationUnit.Days));
        var progress = schedule.Evaluate(battleStart);

        Assert.Equal(CampaignStatus.InProgress, progress.Status);
        Assert.Equal(1, progress.CurrentRound);
        Assert.Equal(3, progress.CurrentPhaseNumber);
        Assert.Equal(RoundPhaseKind.Battle, progress.CurrentPhaseKind);
    }

    [Fact]
    public void AdvancesToSecondRoundAfterFirstRoundEnds()
    {
        var schedule = CreateWeekSchedule();
        var secondRound = CampaignCalendar.Add(
            schedule.StartsUtc,
            schedule.TimeZone,
            new ScheduleDuration(1, DurationUnit.Weeks));
        var progress = schedule.Evaluate(secondRound);

        Assert.Equal(CampaignStatus.InProgress, progress.Status);
        Assert.Equal(2, progress.CurrentRound);
        Assert.Equal(1, progress.CurrentPhaseNumber);
        Assert.Equal(RoundPhaseKind.Action, progress.CurrentPhaseKind);
    }

    [Fact]
    public void ReportsCompletedAtEnd()
    {
        var schedule = CreateWeekSchedule();
        var progress = schedule.Evaluate(schedule.EndsUtc);

        Assert.Equal(CampaignStatus.Completed, progress.Status);
        Assert.Null(progress.CurrentRound);
    }

    private static CampaignSchedule CreateWeekSchedule()
    {
        var succeeded = CampaignSetupRules.TryCreate(
            "Border War",
            null,
            8,
            false,
            null,
            false,
            true,
            0,
            [
                new FactionInput { Name = "North" },
                new FactionInput { Name = "South" },
            ],
            null,
            null,
            CampaignSetupRulesTests.WeekSchedule(),
            out var setup,
            out _,
            out var errors);

        Assert.True(succeeded, string.Join('\n', errors.Select(error => error.Message)));
        Assert.NotNull(setup);
        return setup.Schedule;
    }
}
