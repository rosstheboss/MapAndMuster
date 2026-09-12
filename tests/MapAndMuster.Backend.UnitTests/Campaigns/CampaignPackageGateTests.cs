using MapAndMuster.Application.Campaigns;

namespace MapAndMuster.Backend.UnitTests.Campaigns;

public sealed class CampaignPackageGateTests
{
    [Fact]
    public async Task ASecondCallerRunsAlongsideTheFirst()
    {
        using var gate = new CampaignPackageGate();
        using var first = await gate.EnterAsync(CancellationToken.None);
        using var second = await gate.EnterAsync(CancellationToken.None);
        Assert.NotSame(first, second);
    }

    [Fact]
    public async Task AThirdCallerWaitsUntilASlotIsReleased()
    {
        using var gate = new CampaignPackageGate();
        var first = await gate.EnterAsync(CancellationToken.None);
        using var second = await gate.EnterAsync(CancellationToken.None);

        var third = gate.EnterAsync(CancellationToken.None);
        Assert.False(third.IsCompleted);

        first.Dispose();
        using var admitted = await third;
        Assert.NotNull(admitted);
    }

    [Fact]
    public async Task ReleasingTwiceDoesNotHandBackAnExtraSlot()
    {
        using var gate = new CampaignPackageGate();
        var slot = await gate.EnterAsync(CancellationToken.None);
        slot.Dispose();
        slot.Dispose();

        using var second = await gate.EnterAsync(CancellationToken.None);
        using var third = await gate.EnterAsync(CancellationToken.None);

        // A double release would have raised the count above the bound, letting this one straight in.
        using var waiting = new CancellationTokenSource();
        var fourth = gate.EnterAsync(waiting.Token);
        Assert.False(fourth.IsCompleted);

        await waiting.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fourth);
    }
}
