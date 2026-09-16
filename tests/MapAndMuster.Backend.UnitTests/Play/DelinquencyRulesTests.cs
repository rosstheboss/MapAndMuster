using MapAndMuster.Domain.Campaigns;
using MapAndMuster.Domain.Play;

namespace MapAndMuster.Backend.UnitTests.Play;

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
        var first = Assert.Single(state.Delinquencies.Single().Offences);
        Assert.Equal(WindowId, first.WindowId);
        Assert.Equal(1, first.RoundNumber);
        Assert.Equal(1, first.KindOrdinal);
        Assert.Equal(RoundPhaseKind.Action, first.Kind);
        Assert.Equal(force.TerritoryId, first.TerritoryId);

        state = DelinquencyRules.Record(state, [ForceId], window, Now);
        Assert.Equal(2, state.Delinquencies.Single().OffenceCount);
        Assert.Empty(state.Log);

        var previous = state.Log.Count;
        state = DelinquencyRules.Record(state, [ForceId], window, Now);
        Assert.Equal(3, state.Delinquencies.Single().OffenceCount);
        Assert.Equal(3, state.Delinquencies.Single().Offences.Count);
        Assert.Contains(state.Log, item => item.Kind == PlayLogKind.DelinquencyThreshold);
        Assert.True(DelinquencyRules.ShouldNotifyManagers(state, previous));
    }
}
