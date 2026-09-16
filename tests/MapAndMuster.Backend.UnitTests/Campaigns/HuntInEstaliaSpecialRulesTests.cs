using MapAndMuster.Domain.Campaigns;

namespace MapAndMuster.Backend.UnitTests.Campaigns;

public sealed class HuntInEstaliaSpecialRulesTests
{
    [Fact]
    public void MapsDocumentedFactionAndSubfactionRuleNames()
    {
        Assert.Equal(["Prepared for Battle"], HuntInEstaliaSpecialRules.RuleNamesForFaction("empire of man"));
        Assert.Equal(
            ["Called by the Relic", "Undead"],
            HuntInEstaliaSpecialRules.RuleNamesForFaction("Tomb Kings of Khemri"));
        Assert.Equal(
            ["Only Blood Satisfies!"],
            HuntInEstaliaSpecialRules.RuleNamesForSubfaction("Daemons of Chaos", "Khorne"));
        Assert.Empty(HuntInEstaliaSpecialRules.RuleNamesForFaction("North"));
        Assert.Empty(HuntInEstaliaSpecialRules.RuleNamesForSubfaction("Daemons of Chaos", "Riders"));
    }
}
