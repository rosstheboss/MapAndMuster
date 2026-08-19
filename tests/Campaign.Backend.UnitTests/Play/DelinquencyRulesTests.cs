using Campaign.Domain.Campaigns;
using Campaign.Domain.Play;

namespace Campaign.Backend.UnitTests.Play;

public sealed class DelinquencyRulesTests
{
    private static readonly Guid ForceId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid WindowId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RecordsPerForceAndLogsFromTheThirdOffence()
    {
        var window = new PhaseWindow(
            WindowId,
            1,
            1,
            RoundPhaseKind.Action,
            6,
            DurationUnit.Minutes,
            Now,
            Now.AddMinutes(6),
            PhaseWindowStatus.Resolved);
        var force = new CampaignForce(ForceId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), false);
        var state = new CampaignPlayState([window], [force], [], [], [], [], [], [], [], [], [], []);

        state = DelinquencyRules.Record(state, [ForceId], window, Now);
        Assert.Equal(1, state.Delinquencies.Single().OffenceCount);
        Assert.Empty(state.Log);

        state = DelinquencyRules.Record(state, [ForceId], window, Now);
        Assert.Equal(2, state.Delinquencies.Single().OffenceCount);
        Assert.Empty(state.Log);

        var previous = state.Log.Count;
        state = DelinquencyRules.Record(state, [ForceId], window, Now);
        Assert.Equal(3, state.Delinquencies.Single().OffenceCount);
        Assert.Contains(state.Log, item => item.Kind == PlayLogKind.DelinquencyThreshold);
        Assert.True(DelinquencyRules.ShouldNotifyManagers(state, previous));
    }
}
