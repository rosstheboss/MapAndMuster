using MapAndMuster.Domain.Play;

namespace MapAndMuster.Backend.UnitTests.Play;

public sealed class TeleportActionRulesTests
{
    private static readonly Guid Bretonnia = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa01");
    private static readonly Guid ChaosDwarfs = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa02");
    private static readonly Guid Tilea = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa03");
    private static readonly Guid Player = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid EnemyPlayer = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2");
    private static readonly Guid Origin = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Via = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Dest = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid EnemySpawn = Guid.Parse("44444444-4444-4444-4444-444444444444");

    [Fact]
    public void RandomDestinationsAreNeutralOrAlliedNonSpawnWithoutEnemiesOrBattles()
    {
        var force = new CampaignForce(Guid.NewGuid(), Player, Bretonnia, Origin, false);
        var dests = TeleportActionRules.RandomDestinations(
            Map(),
            force,
            [force],
            [],
            Groups(),
            [],
            []);

        Assert.Equal([Via], dests);
    }

    [Fact]
    public void ChosenDestinationsAllowEnemyOwnedEmptyLandButNotSpawnsOrEnemyOccupants()
    {
        var force = new CampaignForce(Guid.NewGuid(), Player, Bretonnia, Origin, false);
        var empty = TeleportActionRules.ChosenDestinations(Map(), force, [force], Groups(), [], []);
        Assert.Contains(Via, empty);
        Assert.Contains(Dest, empty);
        Assert.DoesNotContain(Origin, empty);
        Assert.DoesNotContain(EnemySpawn, empty);

        var enemy = new CampaignForce(Guid.NewGuid(), EnemyPlayer, ChaosDwarfs, Dest, false);
        var occupied = TeleportActionRules.ChosenDestinations(
            Map(),
            force,
            [force, enemy],
            Groups(),
            [],
            []);
        Assert.Contains(Via, occupied);
        Assert.DoesNotContain(Dest, occupied);
    }

    [Fact]
    public void InterruptWhenEnemyOccupiesSourceOrDestinationOrAllyBackstabsAtSource()
    {
        var force = new CampaignForce(Guid.NewGuid(), Player, Bretonnia, Origin, false);
        var enemyAtDest = new CampaignForce(Guid.NewGuid(), EnemyPlayer, ChaosDwarfs, Dest, false);
        var ally = new CampaignForce(Guid.NewGuid(), Guid.NewGuid(), Tilea, Origin, false);
        var alliedGroups = new Dictionary<Guid, string?>
        {
            [Bretonnia] = "Coalition",
            [Tilea] = "Coalition",
            [ChaosDwarfs] = null,
        };

        Assert.True(TeleportActionRules.IsInterrupted(
            Origin,
            Dest,
            force,
            [force, enemyAtDest],
            new Dictionary<Guid, ActionKind>(),
            Groups(),
            [],
            []));
        Assert.True(TeleportActionRules.IsInterrupted(
            Origin,
            Via,
            force,
            [force, ally],
            new Dictionary<Guid, ActionKind> { [ally.Id] = ActionKind.Backstab },
            alliedGroups,
            [],
            []));
        Assert.False(TeleportActionRules.IsInterrupted(
            Origin,
            Via,
            force,
            [force, ally],
            new Dictionary<Guid, ActionKind> { [ally.Id] = ActionKind.Hold },
            alliedGroups,
            [],
            []));
    }

    [Fact]
    public void HoldReducesSpecifiedTeleportRechargeByAnExtraPhase()
    {
        Assert.Equal(3, TeleportActionRules.ChosenTeleportRechargePhases);
        Assert.Equal(2, TeleportActionRules.NextChosenTeleportCooldown(3, ActionKind.Move));
        Assert.Equal(1, TeleportActionRules.NextChosenTeleportCooldown(3, ActionKind.Hold));
        Assert.Equal(0, TeleportActionRules.NextChosenTeleportCooldown(1, ActionKind.Hold));
        Assert.Equal(0, TeleportActionRules.NextChosenTeleportCooldown(0, ActionKind.Hold));
    }

    [Fact]
    public void OpenedItemsCannotBeDroppedOnMove()
    {
        var forceId = Guid.NewGuid();
        var unopened = Held("Crown", forceId);
        var opened = new CampaignItemObjective(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Opened",
            Origin,
            forceId,
            true,
            Origin,
            true,
            resolvedChoiceId: Guid.NewGuid());

        Assert.True(TeleportActionRules.CanDropOnMove(unopened));
        Assert.False(TeleportActionRules.CanDropOnMove(opened));
    }

    private static CampaignItemObjective Held(string name, Guid forceId)
    {
        return new CampaignItemObjective(
            Guid.NewGuid(),
            Guid.NewGuid(),
            name,
            Origin,
            forceId,
            true,
            Origin,
            true);
    }

    private static Dictionary<Guid, string?> Groups()
    {
        return new Dictionary<Guid, string?>
        {
            [Bretonnia] = "Coalition",
            [ChaosDwarfs] = null,
        };
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
