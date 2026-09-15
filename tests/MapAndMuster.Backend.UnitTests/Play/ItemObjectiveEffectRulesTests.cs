using MapAndMuster.Domain.Campaigns;
using MapAndMuster.Domain.Play;

namespace MapAndMuster.Backend.UnitTests.Play;

public sealed class ItemObjectiveEffectRulesTests
{
    private static readonly Guid Bretonnia = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa01");
    private static readonly Guid ChaosDwarfs = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa02");
    private static readonly Guid Player = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid Origin = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Via = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Dest = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid EnemySpawn = Guid.Parse("44444444-4444-4444-4444-444444444444");

    [Fact]
    public void ModifySupplyNeverDropsBelowOne()
    {
        var typeId = Guid.NewGuid();
        var force = new CampaignForce(Guid.NewGuid(), Player, Bretonnia, Origin, false);
        var rules = Effects(typeId, new ItemObjectiveEffectSetup(Guid.NewGuid(), ItemObjectiveEffectKind.ModifySupply, -5));

        Assert.Equal(1, ItemObjectiveEffectRules.AdjustSupply(2, force, Map(), [HeldItem(typeId, force.Id)], rules));
    }

    [Fact]
    public void TeleportDestinationsExcludeSpawnsAndOccupiedLand()
    {
        var force = new CampaignForce(Guid.NewGuid(), Player, Bretonnia, Origin, false);
        var dests = ItemObjectiveEffectRules.TeleportDestinations(Map(), [force]);

        Assert.Equal([Via, Dest], dests);
    }

    [Fact]
    public void ChosenTeleportDestinationsExcludeSpawnsAndIncludeOccupiedLand()
    {
        var force = new CampaignForce(Guid.NewGuid(), Player, Bretonnia, Origin, false);
        var dests = ItemObjectiveEffectRules.ChosenTeleportDestinations(Map(), force);

        Assert.Contains(Via, dests);
        Assert.Contains(Dest, dests);
        Assert.DoesNotContain(Origin, dests);
        Assert.DoesNotContain(EnemySpawn, dests);
    }

    [Fact]
    public void ChosenTeleportIsUnavailableWhileRecharging()
    {
        var typeId = Guid.NewGuid();
        var force = new CampaignForce(Guid.NewGuid(), Player, Bretonnia, Origin, false);
        var recharging = force.With(chosenTeleportCooldownRemaining: 3);
        var item = HeldItem(typeId, force.Id);
        var rules = Effects(
            typeId,
            new ItemObjectiveEffectSetup(Guid.NewGuid(), ItemObjectiveEffectKind.TeleportToChosenNonSpawnOncePerRound));
        var map = Map();

        Assert.True(ItemObjectiveEffectRules.CanChosenTeleport(force, map, [item], rules, 2));
        Assert.False(ItemObjectiveEffectRules.CanChosenTeleport(recharging, map, [item], rules, 2));
        Assert.True(ItemObjectiveEffectRules.HasAvailableTeleport(force, map, [item], rules, [force], 2));
        Assert.False(ItemObjectiveEffectRules.HasAvailableTeleport(recharging, map, [item], rules, [recharging], 2));
        Assert.True(ItemObjectiveEffectRules.IsValidChosenTeleportTarget(force, map, [item], rules, 2, Via));
        Assert.False(ItemObjectiveEffectRules.IsValidChosenTeleportTarget(force, map, [item], rules, 2, Origin));
        Assert.False(ItemObjectiveEffectRules.IsValidChosenTeleportTarget(force, map, [item], rules, 2, EnemySpawn));
    }

    [Fact]
    public void PushDefeatedOpponentToSpawnIsActiveOnTheHolder()
    {
        var typeId = Guid.NewGuid();
        var force = new CampaignForce(Guid.NewGuid(), Player, Bretonnia, Origin, false);
        var rules = Effects(
            typeId,
            new ItemObjectiveEffectSetup(Guid.NewGuid(), ItemObjectiveEffectKind.PushDefeatedOpponentToSpawn));

        Assert.True(ItemObjectiveEffectRules.PushesDefeatedToSpawn(force, Map(), [HeldItem(typeId, force.Id)], rules));
    }

    [Fact]
    public void AdjacentNullifierSuppressesHeldItemEffects()
    {
        var bannerType = Guid.NewGuid();
        var nullifierType = Guid.NewGuid();
        var holder = new CampaignForce(Guid.NewGuid(), Player, Bretonnia, Origin, false);
        var banner = HeldItem(bannerType, holder.Id);
        var nullifier = new CampaignItemObjective(
            Guid.NewGuid(),
            nullifierType,
            "Ward",
            Via,
            null,
            true,
            Via,
            true);
        var rules = new SpecialRuleContext(
            [],
            new Dictionary<Guid, IReadOnlyList<Guid>>(),
            new Dictionary<(Guid, string), IReadOnlyList<Guid>>(),
            itemEffectsByTypeId: new Dictionary<Guid, IReadOnlyList<ItemObjectiveEffectSetup>>
            {
                [bannerType] = [new ItemObjectiveEffectSetup(Guid.NewGuid(), ItemObjectiveEffectKind.AddMovementSpeed, 3)],
                [nullifierType] =
                [
                    new ItemObjectiveEffectSetup(Guid.NewGuid(), ItemObjectiveEffectKind.NullifyAdjacentItemObjectives),
                ],
            });

        Assert.Equal(0, ItemObjectiveEffectRules.MovementSpeedBonus(holder, Map(), [banner, nullifier], rules));
        Assert.True(ItemObjectiveEffectRules.IsNullified(banner, Map(), [banner, nullifier], rules));
    }

    [Fact]
    public void ArmyPointPercentIsAppliedAgainstTheRoundCap()
    {
        var typeId = Guid.NewGuid();
        var force = new CampaignForce(Guid.NewGuid(), Player, Bretonnia, Origin, false);
        var rules = Effects(
            typeId,
            new ItemObjectiveEffectSetup(Guid.NewGuid(), ItemObjectiveEffectKind.ModifyArmyPoints, 10, amountIsPercent: true));

        Assert.Equal(1100, ItemObjectiveEffectRules.AdjustArmyPoints(1000, force, Map(), [HeldItem(typeId, force.Id)], rules));
    }

    private static SpecialRuleContext Effects(Guid typeId, ItemObjectiveEffectSetup effect)
    {
        return new SpecialRuleContext(
            [],
            new Dictionary<Guid, IReadOnlyList<Guid>>(),
            new Dictionary<(Guid, string), IReadOnlyList<Guid>>(),
            itemEffectsByTypeId: new Dictionary<Guid, IReadOnlyList<ItemObjectiveEffectSetup>>
            {
                [typeId] = [effect],
            });
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
