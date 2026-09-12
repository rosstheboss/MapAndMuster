using MapAndMuster.Domain.Campaigns;
using MapAndMuster.Domain.Play;

namespace MapAndMuster.Backend.UnitTests.Play;

public sealed class ForceMovementRulesTests
{
    private static readonly Guid Bretonnia = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa01");
    private static readonly Guid ChaosDwarfs = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa02");
    private static readonly Guid Player = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid Origin = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Via = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Dest = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid EnemySpawn = Guid.Parse("44444444-4444-4444-4444-444444444444");

    [Fact]
    public void FactionSpeedTwoAllowsATwoTerritoryMoveWithoutCrusaders()
    {
        var rules = SpeedContext(Bretonnia, 2);
        var force = new CampaignForce(Guid.NewGuid(), Player, Bretonnia, Origin, false);
        var map = Map();

        Assert.Equal(2, ForceMovementRules.EffectiveSpeed(force, rules));
        Assert.True(ForceMovementRules.IsValidMove(map, force, Dest, Via, [], rules));
        Assert.True(ForceMovementRules.SkipClaiming(Via, Origin, Dest, Via));
        Assert.False(ForceMovementRules.SkipClaiming(Dest, Origin, Dest, Via));
        Assert.Contains(Dest, ForceMovementRules.EligibleDestinations(map, force, 2, [], rules));
        Assert.Contains(ForceMovementRules.EligibleHops(map, force, 2), hop => hop.ViaTerritoryId == Via && hop.TargetTerritoryId == Dest);
    }

    [Fact]
    public void CalledByTheRelicAddsSpeedUntilAFactionForceHoldsAnItem()
    {
        var rules = CalledByTheRelicContext(Bretonnia);
        var force = new CampaignForce(Guid.NewGuid(), Player, Bretonnia, Origin, false);
        var ally = new CampaignForce(Guid.NewGuid(), Guid.NewGuid(), Bretonnia, Via, false);
        var map = Map();
        var hidden = new CampaignItemObjective(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Relic",
            Dest,
            null,
            false,
            Dest,
            true);
        var revealed = hidden.With(isRevealed: true);

        Assert.Equal(1, ForceMovementRules.EffectiveSpeed(force, rules, map, [hidden], [force, ally]));
        Assert.Equal(2, ForceMovementRules.EffectiveSpeed(force, rules, map, [revealed], [force, ally]));
        Assert.True(ForceMovementRules.IsValidMove(map, force, Dest, Via, [revealed], rules, occupyingForces: [force, ally]));
        Assert.Contains(Dest, CampaignPlayRules.EligibleMoves(map, force, [revealed], rules, [force, ally]));

        var heldByAlly = revealed.With(possessorForceId: ally.Id, clearTerritory: true);
        Assert.Equal(1, ForceMovementRules.EffectiveSpeed(force, rules, map, [heldByAlly], [force, ally]));
        Assert.False(ForceMovementRules.IsValidMove(map, force, Dest, Via, [heldByAlly], rules, occupyingForces: [force, ally]));

        var enemy = new CampaignForce(Guid.NewGuid(), Guid.NewGuid(), ChaosDwarfs, Dest, false);
        var heldByEnemy = revealed.With(possessorForceId: enemy.Id, clearTerritory: true);
        Assert.Equal(2, ForceMovementRules.EffectiveSpeed(force, rules, map, [heldByEnemy], [force, ally, enemy]));
    }

    [Fact]
    public void HeldItemAddsMovementSpeed()
    {
        var typeId = Guid.NewGuid();
        var force = new CampaignForce(Guid.NewGuid(), Player, Bretonnia, Origin, false);
        var item = HeldItem(typeId, force.Id);
        var rules = new SpecialRuleContext(
            [],
            new Dictionary<Guid, IReadOnlyList<Guid>>(),
            new Dictionary<(Guid, string), IReadOnlyList<Guid>>(),
            itemEffectsByTypeId: new Dictionary<Guid, IReadOnlyList<ItemObjectiveEffectSetup>>
            {
                [typeId] = [new ItemObjectiveEffectSetup(Guid.NewGuid(), ItemObjectiveEffectKind.AddMovementSpeed, 1)],
            });

        Assert.Equal(2, ForceMovementRules.EffectiveSpeed(force, rules, Map(), [item]));
        Assert.True(ForceMovementRules.IsValidMove(Map(), force, Dest, Via, [item], rules));
    }

    [Fact]
    public void AdjacentDestinationDoesNotRequireAVia()
    {
        var rules = SpeedContext(Bretonnia, 2);
        var force = new CampaignForce(Guid.NewGuid(), Player, Bretonnia, Origin, false);

        Assert.True(ForceMovementRules.IsValidMove(Map(), force, Via, null, [], rules));
    }

    private static SpecialRuleContext SpeedContext(Guid factionId, int speed)
    {
        return new SpecialRuleContext(
            [],
            new Dictionary<Guid, IReadOnlyList<Guid>>(),
            new Dictionary<(Guid, string), IReadOnlyList<Guid>>(),
            factionMovementSpeeds: new Dictionary<Guid, int> { [factionId] = speed });
    }

    private static SpecialRuleContext CalledByTheRelicContext(Guid factionId)
    {
        var ruleId = Guid.NewGuid();
        return new SpecialRuleContext(
            [new SpecialRuleSetup(ruleId, SpecialRuleEffectKeys.CalledByTheRelic, "Rule text.", SpecialRuleEffectKeys.CalledByTheRelic)],
            new Dictionary<Guid, IReadOnlyList<Guid>> { [factionId] = [ruleId] },
            new Dictionary<(Guid, string), IReadOnlyList<Guid>>());
    }

    private static CampaignItemObjective HeldItem(Guid typeId, Guid forceId)
    {
        return new CampaignItemObjective(
            Guid.NewGuid(),
            typeId,
            "Banner",
            Origin,
            forceId,
            true,
            Origin,
            true);
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
