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

        var withoutArt = CampaignPlayRules.EligibleRetreats(map, cathay);
        Assert.Contains(Via, withoutArt);
        Assert.DoesNotContain(Dest, withoutArt);

        var retreats = CampaignPlayRules.EligibleRetreats(map, cathay, art);
        Assert.Contains(Dest, retreats);
        Assert.DoesNotContain(EnemySpawn, retreats);
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
        var khornePlayer = Guid.NewGuid();
        var nurglePlayer = Guid.NewGuid();
        var scopedKhorne = new CampaignForce(Guid.NewGuid(), khornePlayer, Bretonnia, Origin, false, subfaction: "Khorne");
        var scopedNurgle = new CampaignForce(Guid.NewGuid(), nurglePlayer, Bretonnia, Via, false, subfaction: "Nurgle");
        var tzeentch = new CampaignForce(Guid.NewGuid(), Guid.NewGuid(), Bretonnia, Dest, false, subfaction: "Tzeentch");
        var betrayals = new[] { new AllyBetrayal(khornePlayer, Bretonnia, "Nurgle", nurglePlayer) };
        Assert.True(FactionSpecialRulePolicies.AreEnemies(
            scopedKhorne,
            scopedNurgle,
            new Dictionary<Guid, string?>(),
            [],
            [],
            rules,
            betrayals));
        Assert.False(FactionSpecialRulePolicies.AreEnemies(
            scopedKhorne,
            tzeentch,
            new Dictionary<Guid, string?>(),
            [],
            [],
            rules,
            betrayals));
    }

    [Fact]
    public void UndergroundNetworkPrefersUnoccupiedNeutralTownThenOwnedThenOccupied()
    {
        var skaven = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa06");
        var rules = Context(skaven, SpecialRuleEffectKeys.UndergroundNetwork);
        var town = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var city = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var capital = Guid.Parse("77777777-7777-7777-7777-777777777777");
        var plains = Guid.Parse("88888888-8888-8888-8888-888888888888");
        PlayTerritory Territory(Guid id, int number, Guid? owner, string name, Guid? spawn = null) =>
            new(id, number, owner, spawn, Guid.NewGuid(), name, StructureCondition.Operational);

        var map = new PlayMap(
            [
                Territory(plains, 1, null, "Plains"),
                Territory(city, 2, Bretonnia, "City"),
                Territory(capital, 3, ChaosDwarfs, "Capital City"),
                Territory(town, 4, null, "Town"),
            ],
            []);
        var occupiedCity = new CampaignForce(Guid.NewGuid(), Player, Bretonnia, city, false);

        var neutral = FactionSpecialRulePolicies.StartingPlacement(map, skaven, null, [], rules, static _ => 0);
        Assert.Equal((town, true), neutral);

        var owned = FactionSpecialRulePolicies.StartingPlacement(
            map,
            skaven,
            null,
            [new CampaignForce(Guid.NewGuid(), Player, skaven, town, false)],
            rules,
            static _ => 0);
        Assert.Equal((city, true), owned);

        var last = FactionSpecialRulePolicies.StartingPlacement(
            map,
            skaven,
            null,
            [
                new CampaignForce(Guid.NewGuid(), Player, skaven, town, false),
                occupiedCity,
                new CampaignForce(Guid.NewGuid(), Guid.NewGuid(), ChaosDwarfs, capital, false),
            ],
            rules,
            static _ => 0);
        Assert.Equal((city, false), last);
    }

    [Fact]
    public void UndergroundNetworkDoesNotCaptureSpawnOrCapitalAndPlacesWhenEveryTownIsOccupied()
    {
        var skaven = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa06");
        var rules = Context(skaven, SpecialRuleEffectKeys.UndergroundNetwork);
        var spawnTown = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var capital = Guid.Parse("77777777-7777-7777-7777-777777777777");
        var map = new PlayMap(
            [
                new PlayTerritory(spawnTown, 1, Bretonnia, Bretonnia, Guid.NewGuid(), "Town", StructureCondition.Operational),
                new PlayTerritory(capital, 2, ChaosDwarfs, null, Guid.NewGuid(), "Capital City", StructureCondition.Operational),
            ],
            []);

        var spawn = FactionSpecialRulePolicies.StartingPlacement(map, skaven, null, [], rules, static _ => 0);
        Assert.Equal((spawnTown, false), spawn);

        var occupied = FactionSpecialRulePolicies.StartingPlacement(
            map,
            skaven,
            null,
            [
                new CampaignForce(Guid.NewGuid(), Player, Bretonnia, spawnTown, false),
                new CampaignForce(Guid.NewGuid(), Guid.NewGuid(), ChaosDwarfs, capital, false),
            ],
            rules,
            static _ => 0);
        Assert.Equal((spawnTown, false), occupied);

        var blocked = FactionSpecialRulePolicies.ForcedSpawnPlacement(
            map,
            skaven,
            null,
            [],
            rules,
            static _ => 0,
            new HashSet<Guid> { spawnTown });
        Assert.Equal((capital, false), blocked);

        var empty = FactionSpecialRulePolicies.StartingPlacement(
            new PlayMap(
                [new PlayTerritory(Origin, 1, null, null, null, null, StructureCondition.Operational)],
                []),
            skaven,
            null,
            [],
            rules,
            static _ => 0);
        Assert.Null(empty);
    }

    [Fact]
    public void OptionalSubfactionBetrayalTurnsTheWholeFactionAgainstTheTraitorOnly()
    {
        var traitorId = Guid.NewGuid();
        var victimId = Guid.NewGuid();
        var mateId = Guid.NewGuid();
        var otherBretonniaId = Guid.NewGuid();
        var empire = new CampaignForce(Guid.NewGuid(), traitorId, ChaosDwarfs, Origin, false);
        var mate = new CampaignForce(Guid.NewGuid(), mateId, ChaosDwarfs, Via, false);
        var crusade = new CampaignForce(
            Guid.NewGuid(),
            victimId,
            Bretonnia,
            Origin,
            false,
            subfaction: "Errantry Crusade");
        var royal = new CampaignForce(
            Guid.NewGuid(),
            otherBretonniaId,
            Bretonnia,
            Dest,
            false,
            subfaction: "Royal Army");
        var groups = new Dictionary<Guid, string?> { [ChaosDwarfs] = "Coalition", [Bretonnia] = "Coalition" };
        var betrayals = new[] { new AllyBetrayal(traitorId, Bretonnia, null, victimId) };
        Assert.True(FactionSpecialRulePolicies.AreEnemies(
            empire,
            crusade,
            groups,
            [],
            [],
            SpecialRuleContext.None,
            betrayals));
        Assert.True(FactionSpecialRulePolicies.AreEnemies(
            empire,
            royal,
            groups,
            [],
            [],
            SpecialRuleContext.None,
            betrayals));
        Assert.False(FactionSpecialRulePolicies.AreEnemies(
            mate,
            crusade,
            groups,
            [],
            [],
            SpecialRuleContext.None,
            betrayals));
        Assert.True(FactionSpecialRulePolicies.AreAllies(
            mate,
            royal,
            groups,
            [],
            [],
            SpecialRuleContext.None,
            betrayals));
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
                new PlayTerritory(Dest, 3, ChaosDwarfs, null, null, null, StructureCondition.Operational),
                new PlayTerritory(EnemySpawn, 4, ChaosDwarfs, ChaosDwarfs, null, null, StructureCondition.Operational),
            ],
            [(Origin, Via), (Via, Dest), (Origin, EnemySpawn)]);
    }
}
