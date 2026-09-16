using MapAndMuster.Domain.Play;

namespace MapAndMuster.Backend.UnitTests.Play;

public sealed class PlayLogFactsTests
{
    [Fact]
    public void RoundTripsADestroyedStructureName()
    {
        var message = PlayLogFacts.DestroyedStructure("City");
        Assert.True(PlayLogFacts.TryReadDestroyedStructure(message, out var name));
        Assert.Equal("City", name);
        Assert.False(PlayLogFacts.TryReadDestroyedStructure("Town", out _));
    }

    [Fact]
    public void RoundTripsAnActionCancelledPayload()
    {
        var interrupter = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var place = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var message = PlayLogFacts.ActionCancelled(PlayLogFacts.InterruptEnemy, interrupter, place);
        Assert.True(PlayLogFacts.TryReadActionCancelled(message, out var reason, out var readInterrupter, out var readPlace));
        Assert.Equal(PlayLogFacts.InterruptEnemy, reason);
        Assert.Equal(interrupter, readInterrupter);
        Assert.Equal(place, readPlace);
        Assert.False(PlayLogFacts.TryReadActionCancelled("enemy", out _, out _, out _));
        Assert.False(PlayLogFacts.TryReadActionCancelled(PlayLogFacts.Betrayal(PlayLogFacts.BetrayalAttack, interrupter, place), out _, out _, out _));
    }

    [Fact]
    public void RoundTripsBetrayalPayloads()
    {
        var victim = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var faction = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var attack = PlayLogFacts.Betrayal(PlayLogFacts.BetrayalAttack, victim, faction);
        Assert.True(PlayLogFacts.TryReadBetrayal(attack, out var kind, out var readVictim, out var readFaction, out var structure));
        Assert.Equal(PlayLogFacts.BetrayalAttack, kind);
        Assert.Equal(victim, readVictim);
        Assert.Equal(faction, readFaction);
        Assert.Null(structure);

        var pillage = PlayLogFacts.Betrayal(PlayLogFacts.BetrayalPillage, null, faction, "Castle");
        Assert.True(PlayLogFacts.TryReadBetrayal(pillage, out kind, out readVictim, out readFaction, out structure));
        Assert.Equal(PlayLogFacts.BetrayalPillage, kind);
        Assert.Null(readVictim);
        Assert.Equal(faction, readFaction);
        Assert.Equal("Castle", structure);
    }
}
