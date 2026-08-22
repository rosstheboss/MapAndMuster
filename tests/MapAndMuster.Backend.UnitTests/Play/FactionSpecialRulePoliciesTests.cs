using MapAndMuster.Domain.Campaigns;
using MapAndMuster.Domain.Play;

namespace MapAndMuster.Backend.UnitTests.Play;

public sealed class FactionSpecialRulePoliciesTests
{
    private static readonly Guid Bretonnia = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa01");
    private static readonly Guid ChaosDwarfs = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa02");
    private static readonly Guid Orcs = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa03");
    private static readonly Guid Cathay = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa04");
    private static readonly Guid TombKings = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa05");
    private static readonly Guid Player = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid Origin = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Via = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Dest = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid EnemySpawn = Guid.Parse("44444444-4444-4444-4444-444444444444");

    [Fact]
    public void CrusadersAllowsATwoTerritoryMoveAndSkipsClaimingTheVia()
    {
        var rules = Context(Bretonnia, SpecialRuleEffectKeys.Crusaders);
        var force = new CampaignForce(Guid.NewGuid(), Player, Bretonnia, Origin, false);
        var map = Map();

        Assert.True(FactionSpecialRulePolicies.IsValidMove(map, force, Dest, Via, [], rules));
        Assert.True(FactionSpecialRulePolicies.SkipClaiming(force, Via, Origin, Dest, Via, rules));
        Assert.False(FactionSpecialRulePolicies.SkipClaiming(force, Dest, Origin, Dest, Via, rules));
        Assert.Equal(Via, FactionSpecialRulePolicies.ResolveMoveDestination(
            map,
            force,
            Dest,
            Via,
            [force, new CampaignForce(Guid.NewGuid(), Guid.NewGuid(), ChaosDwarfs, Via, false)],
            new Dictionary<Guid, string?>(),
            [],
            [],
            rules));
    }

    [Fact]
    public void SlaversAndGreenTideChangeSupplyAndBuildRules()
    {
        var slaver = Context(ChaosDwarfs, SpecialRuleEffectKeys.Slavers);
        var tide = Context(Orcs, SpecialRuleEffectKeys.GreenTide);
        var orcForce = new CampaignForce(Guid.NewGuid(), Player, Orcs, Origin, false);
        var depot = new StructureTypePlayRules(Guid.NewGuid(), "Supply Depot", true, true, true, 1, 1, 1);
        var map = new PlayMap(
            [new PlayTerritory(Origin, 1, Orcs, null, depot.Id, depot.Name, StructureCondition.Operational)],
            [],
            [depot]);

        Assert.True(slaver.Has(ChaosDwarfs, null, SpecialRuleEffectKeys.Slavers));
        Assert.False(FactionSpecialRulePolicies.CanBuild(map, orcForce, depot.Id, tide));
    }

    [Fact]
    public void UndeadAndNurgleRejectForbiddenStatuses()
    {
        var undead = Context(TombKings, SpecialRuleEffectKeys.Undead);
        var force = new CampaignForce(Guid.NewGuid(), Player, TombKings, Origin, false);
        Assert.False(FactionSpecialRulePolicies.AllowsStatus(force, "Shaken", undead));
        Assert.False(FactionSpecialRulePolicies.AllowsStatus(force, "Well Rested", undead));
        Assert.True(FactionSpecialRulePolicies.AllowsStatus(force, "Exhausted", undead));
    }

    [Fact]
    public void ArtOfWarAndCalledByTheRelicChangeEligibleDestinations()
    {
        var art = Context(Cathay, SpecialRuleEffectKeys.ArtOfWar);
        var relic = Context(TombKings, SpecialRuleEffectKeys.CalledByTheRelic);
        var cathay = new CampaignForce(Guid.NewGuid(), Player, Cathay, Origin, false);
        var tomb = new CampaignForce(Guid.NewGuid(), Player, TombKings, Origin, false);
        var map = Map();
        var item = new CampaignItemObjective(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Relic",
            Dest,
            null,
            true,
            Dest,
            true,
            null,
            null,
            false,
            null);

        var retreats = CampaignPlayRules.EligibleRetreats(map, cathay, art);
        Assert.Contains(Dest, retreats);
        Assert.Equal([Via], CampaignPlayRules.EligibleMoves(map, tomb, [item], relic));
        Assert.Equal([Via], FactionSpecialRulePolicies.RelicPursuitTargets(map, tomb, [item], relic));
    }

    [Fact]
    public void DividedGodsAreAlliesUntilTheyBackstab()
    {
        var rules = Context(Bretonnia, SpecialRuleEffectKeys.DividedWeStand);
        var khorne = new CampaignForce(Guid.NewGuid(), Player, Bretonnia, Origin, false, subfaction: "Khorne");
        var nurgle = new CampaignForce(Guid.NewGuid(), Guid.NewGuid(), Bretonnia, Via, false, subfaction: "Nurgle");
        Assert.False(FactionSpecialRulePolicies.AreEnemies(
            khorne,
            nurgle,
            new Dictionary<Guid, string?>(),
            [],
            [],
            rules));
        Assert.True(FactionSpecialRulePolicies.AreEnemies(
            khorne,
            nurgle,
            new Dictionary<Guid, string?>(),
            [],
            [new BrokenAllySubfaction(Bretonnia, "Khorne")],
            rules));
    }

    private static SpecialRuleContext Context(Guid factionId, string effectKey)
    {
        var ruleId = Guid.NewGuid();
        return new SpecialRuleContext(
            [new SpecialRuleSetup(ruleId, effectKey, "Rule text.", effectKey)],
            new Dictionary<Guid, IReadOnlyList<Guid>> { [factionId] = [ruleId] },
            new Dictionary<(Guid, string), IReadOnlyList<Guid>>());
    }

    private static PlayMap Map()
    {
        return new PlayMap(
            [
                new PlayTerritory(Origin, 1, Bretonnia, Bretonnia, null, null, StructureCondition.Operational),
                new PlayTerritory(Via, 2, null, null, null, null, StructureCondition.Operational),
                new PlayTerritory(Dest, 3, null, null, null, null, StructureCondition.Operational),
                new PlayTerritory(EnemySpawn, 4, ChaosDwarfs, ChaosDwarfs, null, null, StructureCondition.Operational),
            ],
            [(Origin, Via), (Via, Dest), (Origin, EnemySpawn)]);
    }
}
