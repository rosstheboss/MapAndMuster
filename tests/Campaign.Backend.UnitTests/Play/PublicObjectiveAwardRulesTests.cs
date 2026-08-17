using Campaign.Domain.Play;

namespace Campaign.Backend.UnitTests.Play;

public sealed class PublicObjectiveAwardRulesTests
{
    [Fact]
    public void AwardsAndRevokesWithoutOverwritingEarlierFacts()
    {
        var player = Guid.NewGuid();
        var manager = Guid.NewGuid();
        var objective = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var known = new HashSet<Guid> { objective };
        var players = new HashSet<Guid> { player };

        Assert.True(PublicObjectiveAwardRules.TryAward(
            CampaignPlayState.Empty,
            objective,
            player,
            manager,
            now,
            known,
            players,
            out var awarded,
            out var awardError));
        Assert.Null(awardError);
        Assert.NotNull(awarded);
        Assert.True(PublicObjectiveAwardRules.IsActive(awarded.PublicObjectiveAwards, objective, player));
        Assert.Contains(awarded.Log, entry => entry.Kind == PlayLogKind.PublicObjectiveAwarded);

        Assert.False(PublicObjectiveAwardRules.TryAward(
            awarded,
            objective,
            player,
            manager,
            now.AddMinutes(1),
            known,
            players,
            out _,
            out var duplicateError));
        Assert.Equal("publicObjective.awarded", duplicateError?.Code);

        Assert.True(PublicObjectiveAwardRules.TryRevoke(
            awarded,
            objective,
            player,
            manager,
            now.AddMinutes(2),
            out var revoked,
            out var revokeError));
        Assert.Null(revokeError);
        Assert.NotNull(revoked);
        Assert.Equal(2, revoked.PublicObjectiveAwards.Count);
        Assert.False(PublicObjectiveAwardRules.IsActive(revoked.PublicObjectiveAwards, objective, player));
        Assert.Contains(revoked.Log, entry => entry.Kind == PlayLogKind.PublicObjectiveRevoked);
    }
}
