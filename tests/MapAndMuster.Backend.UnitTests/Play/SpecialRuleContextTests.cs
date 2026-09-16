using MapAndMuster.Domain.Campaigns;
using MapAndMuster.Domain.Play;

namespace MapAndMuster.Backend.UnitTests.Play;

public sealed class SpecialRuleContextTests
{
    [Fact]
    public void HeldItemHasIgnoresFactionAssignments()
    {
        var factionId = Guid.NewGuid();
        var forceId = Guid.NewGuid();
        var factionRuleId = Guid.NewGuid();
        var itemRuleId = Guid.NewGuid();
        var force = new CampaignForce(forceId, Guid.NewGuid(), factionId, Guid.NewGuid(), false);
        var rules = new SpecialRuleContext(
            [
                new SpecialRuleSetup(factionRuleId, "Steady Advance", "A fictional march.", "PreparedForBattle"),
                new SpecialRuleSetup(itemRuleId, "Crown Spark", "A fictional teleport.", null),
            ],
            new Dictionary<Guid, IReadOnlyList<Guid>> { [factionId] = [factionRuleId] },
            new Dictionary<(Guid, string), IReadOnlyList<Guid>>(),
            forceItemRuleIds: new Dictionary<Guid, IReadOnlyList<Guid>> { [forceId] = [itemRuleId] });

        Assert.True(rules.Has(force, SpecialRuleEffectKeys.PreparedForBattle));
        Assert.False(rules.HeldItemHas(force, factionRuleId));
        Assert.True(rules.HeldItemHas(force, itemRuleId));
    }
}
