using MapAndMuster.Domain.Campaigns;
using MapAndMuster.Domain.Play;

namespace MapAndMuster.Backend.UnitTests.Play;

public sealed class AllyBetrayalRulesTests
{
    private static readonly Guid Empire = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa01");
    private static readonly Guid Bretonnia = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa02");
    private static readonly Guid Daemons = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa03");

    [Fact]
    public void ScopeKeyClearsOptionalSubfactionsAndKeepsRequiredOnes()
    {
        var required = new SpecialRuleContext(
            [],
            new Dictionary<Guid, IReadOnlyList<Guid>>(),
            new Dictionary<(Guid, string), IReadOnlyList<Guid>>(),
            new HashSet<Guid> { Daemons });
        Assert.Null(AllyBetrayalRules.ScopeKey(Bretonnia, "Errantry Crusade", SpecialRuleContext.None));
        Assert.Equal("Khorne", AllyBetrayalRules.ScopeKey(Daemons, "Khorne", required));
        Assert.Null(AllyBetrayalRules.ScopeKey(Daemons, null, required));
    }

    [Fact]
    public void ScopeKeyTreatsDividedWeStandAsScoped()
    {
        var ruleId = Guid.NewGuid();
        var rules = new SpecialRuleContext(
            [new SpecialRuleSetup(ruleId, SpecialRuleEffectKeys.DividedWeStand, "Rule text.", SpecialRuleEffectKeys.DividedWeStand)],
            new Dictionary<Guid, IReadOnlyList<Guid>> { [Daemons] = [ruleId] },
            new Dictionary<(Guid, string), IReadOnlyList<Guid>>());
        Assert.Equal("Nurgle", AllyBetrayalRules.ScopeKey(Daemons, "Nurgle", rules));
    }

    [Fact]
    public void DistinctRelationshipsIgnoreExtraVictimsOfTheSameFactionScope()
    {
        var traitor = Guid.NewGuid();
        var betrayals = new[]
        {
            new AllyBetrayal(traitor, Bretonnia, null, Guid.NewGuid()),
            new AllyBetrayal(traitor, Bretonnia, null, Guid.NewGuid()),
            new AllyBetrayal(traitor, Daemons, "Khorne", Guid.NewGuid()),
        };
        Assert.Equal(2, AllyBetrayalRules.DistinctRelationshipCount(betrayals, traitor));
        Assert.True(AllyBetrayalRules.PlayerBetrayedFaction(traitor, Bretonnia, "Royal Army", betrayals));
        Assert.True(AllyBetrayalRules.PlayerBetrayedFaction(traitor, Daemons, "Khorne", betrayals));
        Assert.False(AllyBetrayalRules.PlayerBetrayedFaction(traitor, Daemons, "Tzeentch", betrayals));
        Assert.False(AllyBetrayalRules.PlayerBetrayedFaction(Guid.NewGuid(), Bretonnia, null, betrayals));
    }

    [Fact]
    public void EmptyLandMatchesOnlyAWholeFactionBetrayal()
    {
        var traitor = Guid.NewGuid();
        var scoped = new[] { new AllyBetrayal(traitor, Daemons, "Khorne", Guid.NewGuid()) };
        var whole = new[] { new AllyBetrayal(traitor, Empire, null, null) };
        Assert.False(AllyBetrayalRules.PlayerBetrayedFaction(traitor, Daemons, null, scoped));
        Assert.True(AllyBetrayalRules.PlayerBetrayedFaction(traitor, Empire, null, whole));
    }
}
