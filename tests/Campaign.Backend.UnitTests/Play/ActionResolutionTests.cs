using Campaign.Domain.Campaigns;
using Campaign.Domain.Play;

namespace Campaign.Backend.UnitTests.Play;

public sealed class ActionResolutionTests
{
    private static readonly Guid North = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid South = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid NorthSpawn = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SouthSpawn = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Midland = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid PlayerOne = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid PlayerTwo = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid TownId = Guid.Parse("dddddd01-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid CityId = Guid.Parse("dddddd02-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid FortId = Guid.Parse("dddddd03-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid WindowId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void EligibleActionsFollowDocumentedOrder()
    {
        var force = new CampaignForce(Guid.NewGuid(), PlayerOne, North, Midland, false);
        var map = Map(midlandStructureId: TownId, midlandStructureName: "Town", midlandOwner: South);
        var state = State(force);
        var kinds = ActionResolution.EligibleActions(state, map, force, AlliedGroups());

        Assert.Equal(
            [ActionKind.Hold, ActionKind.Move, ActionKind.Pillage, ActionKind.Split, ActionKind.Backstab],
            kinds);
    }

    [Fact]
    public void EligibleActionsIncludeBuildAndRepairWhenThoseSlotsApply()
    {
        var emptyForce = new CampaignForce(Guid.NewGuid(), PlayerOne, North, Midland, false);
        var emptyKinds = ActionResolution.EligibleActions(
            State(emptyForce),
            Map(structureTypes: BuildableCatalog()),
            emptyForce,
            UnalignedGroups());
        Assert.Equal([ActionKind.Hold, ActionKind.Move, ActionKind.Build, ActionKind.Split], emptyKinds);

        var repairForce = new CampaignForce(Guid.NewGuid(), PlayerOne, North, Midland, false);
        var repairMap = Map(
            midlandStructureId: TownId,
            midlandStructureName: "Town",
            midlandOwner: North,
            midlandCondition: StructureCondition.Pillaged);
        var repairKinds = ActionResolution.EligibleActions(
            State(repairForce),
            repairMap,
            repairForce,
            UnalignedGroups());
        Assert.Equal([ActionKind.Hold, ActionKind.Move, ActionKind.Repair, ActionKind.Split], repairKinds);
    }

    [Fact]
    public void EligibleActionsAreSurrenderWhileTheForceIsInBattle()
    {
        var force = new CampaignForce(Guid.NewGuid(), PlayerOne, North, Midland, true);
        var kinds = ActionResolution.EligibleActions(State(force), Map(), force, UnalignedGroups());
        Assert.Equal([ActionKind.Surrender], kinds);
    }

    [Fact]
    public void PillageProgressesOperationalToPillagedAndRemovesDestructibleStructures()
    {
        var force = new CampaignForce(Guid.NewGuid(), PlayerOne, North, Midland, false);
        var first = Resolve(State(force, Pillage(force.Id)), Map(midlandStructureId: TownId, midlandStructureName: "Town"));
        Assert.Equal(StructureCondition.Pillaged, first.Map.Territory(Midland)!.StructureCondition);

        var second = Resolve(
            State(force, Pillage(force.Id)),
            Map(
                midlandStructureId: TownId,
                midlandStructureName: "Town",
                midlandCondition: StructureCondition.Pillaged,
                midlandDestructible: true));
        Assert.Null(second.Map.Territory(Midland)!.StructureTypeId);
        Assert.Equal(StructureCondition.Operational, second.Map.Territory(Midland)!.StructureCondition);
    }

    [Fact]
    public void PillagedNonDestructibleStructuresCannotBeDestroyed()
    {
        var force = new CampaignForce(Guid.NewGuid(), PlayerOne, North, Midland, false);
        var resolved = Resolve(
            State(force, Pillage(force.Id)),
            Map(
                midlandStructureId: CityId,
                midlandStructureName: "City",
                midlandCondition: StructureCondition.Pillaged,
                midlandPillageable: true,
                midlandDestructible: false));
        Assert.Equal(StructureCondition.Pillaged, resolved.Map.Territory(Midland)!.StructureCondition);
        Assert.Equal(CityId, resolved.Map.Territory(Midland)!.StructureTypeId);
    }

    [Fact]
    public void RepairRestoresAnOwnedPillagedStructure()
    {
        var force = new CampaignForce(Guid.NewGuid(), PlayerOne, North, Midland, false);
        var resolved = Resolve(
            State(force, Submit(force.Id, ActionKind.Repair)),
            Map(
                midlandStructureId: TownId,
                midlandStructureName: "Town",
                midlandOwner: North,
                midlandCondition: StructureCondition.Pillaged));
        Assert.Equal(StructureCondition.Operational, resolved.Map.Territory(Midland)!.StructureCondition);
        Assert.Equal(North, resolved.Map.Territory(Midland)!.OwnerFactionId);
    }

    [Fact]
    public void BuildPlacesAnOperationalStructure()
    {
        var force = new CampaignForce(Guid.NewGuid(), PlayerOne, North, Midland, false);
        var resolved = Resolve(
            State(force, Submit(force.Id, ActionKind.Build, structureTypeId: FortId)),
            Map(structureTypes: BuildableCatalog()));
        Assert.Equal(FortId, resolved.Map.Territory(Midland)!.StructureTypeId);
        Assert.Equal(StructureCondition.Operational, resolved.Map.Territory(Midland)!.StructureCondition);
        Assert.Equal(North, resolved.Map.Territory(Midland)!.OwnerFactionId);
    }

    [Fact]
    public void SplitCreatesASecondForceInTheDestination()
    {
        var force = new CampaignForce(Guid.NewGuid(), PlayerOne, North, NorthSpawn, false);
        var resolved = Resolve(State(force, Submit(force.Id, ActionKind.Split, Midland)), Map());
        Assert.Equal(2, resolved.State.Forces.Count);
        Assert.Contains(resolved.State.Forces, item => item.TerritoryId == NorthSpawn && item.Id == force.Id);
        Assert.Contains(resolved.State.Forces, item => item.TerritoryId == Midland && item.Id != force.Id);
        Assert.All(resolved.State.Forces, item => Assert.Equal(PlayerOne, item.ControllerUserId));
    }

    [Fact]
    public void SamePlayerForcesRejoinIntoOneActionAndAreLogged()
    {
        var staying = new CampaignForce(Guid.Parse("77777777-7777-7777-7777-777777777777"), PlayerOne, North, Midland, false);
        var moving = new CampaignForce(Guid.Parse("88888888-8888-8888-8888-888888888888"), PlayerOne, North, NorthSpawn, false);
        var resolved = Resolve(
            State(
                [staying, moving],
                [
                    Submit(staying.Id, ActionKind.Hold),
                    Submit(moving.Id, ActionKind.Move, Midland),
                ]),
            Map());

        Assert.Single(resolved.State.Forces);
        Assert.Equal(Midland, resolved.State.Forces[0].TerritoryId);
        Assert.Equal(staying.Id, resolved.State.Forces[0].Id);
        var rejoin = Assert.Single(resolved.State.Log, item => item.Kind == PlayLogKind.ForcesRejoined);
        Assert.Equal(Midland, rejoin.TerritoryId);
        Assert.Equal(PlayerOne, rejoin.ActorUserId);
        Assert.Contains(staying.Id, rejoin.RelatedForceIds);
        Assert.Contains(moving.Id, rejoin.RelatedForceIds);
    }

    [Fact]
    public void BackstabBreaksTheAlliance()
    {
        var force = new CampaignForce(Guid.NewGuid(), PlayerOne, North, NorthSpawn, false);
        var resolved = Resolve(State(force, Submit(force.Id, ActionKind.Backstab)), Map(), AlliedGroups());
        Assert.Contains(North, resolved.State.BrokenAllyFactionIds);
    }

    [Fact]
    public void CompetingStructureActionsBecomeHold()
    {
        var northForce = new CampaignForce(Guid.NewGuid(), PlayerOne, North, Midland, false);
        var southForce = new CampaignForce(Guid.NewGuid(), PlayerTwo, South, Midland, false);
        var resolved = Resolve(
            State(
                [northForce, southForce],
                [
                    Submit(northForce.Id, ActionKind.Build, structureTypeId: FortId),
                    Submit(southForce.Id, ActionKind.Build, structureTypeId: FortId),
                ]),
            Map(structureTypes: BuildableCatalog()),
            AlliedGroups());
        Assert.Contains(resolved.State.Log, item => item.Kind == PlayLogKind.ConflictingBuildHold);
        Assert.Empty(resolved.State.Battles);
    }

    [Fact]
    public void BattleSkipsInPlaceStructureEffects()
    {
        var northForce = new CampaignForce(Guid.NewGuid(), PlayerOne, North, Midland, false);
        var southForce = new CampaignForce(Guid.NewGuid(), PlayerTwo, South, SouthSpawn, false);
        var resolved = Resolve(
            State(
                [northForce, southForce],
                [
                    Pillage(northForce.Id),
                    Submit(southForce.Id, ActionKind.Move, Midland),
                ]),
            Map(midlandStructureId: TownId, midlandStructureName: "Town"));

        Assert.Single(resolved.State.Battles);
        Assert.Equal(StructureCondition.Operational, resolved.Map.Territory(Midland)!.StructureCondition);
        Assert.True(resolved.State.Forces.All(force => force.InBattle));
    }

    [Fact]
    public void NonPillageableStructuresCannotBePillaged()
    {
        var force = new CampaignForce(Guid.NewGuid(), PlayerOne, North, Midland, false);
        var map = Map(
            midlandStructureId: CityId,
            midlandStructureName: "Capital City",
            midlandPillageable: false,
            midlandDestructible: false);
        var kinds = ActionResolution.EligibleActions(State(force), map, force, UnalignedGroups());
        Assert.DoesNotContain(ActionKind.Pillage, kinds);

        var resolved = Resolve(State(force, Pillage(force.Id)), map);
        Assert.Equal(StructureCondition.Operational, resolved.Map.Territory(Midland)!.StructureCondition);
        Assert.Equal(CityId, resolved.Map.Territory(Midland)!.StructureTypeId);
    }

    [Fact]
    public void NonBuildableStructuresCannotBeBuilt()
    {
        var force = new CampaignForce(Guid.NewGuid(), PlayerOne, North, Midland, false);
        var catalog = new[]
        {
            new StructureTypePlayRules(TownId, "Town", false, true, true),
            new StructureTypePlayRules(FortId, "Fortification", true, true, true),
        };
        var resolved = Resolve(
            State(force, Submit(force.Id, ActionKind.Build, structureTypeId: TownId)),
            Map(structureTypes: catalog));
        Assert.Null(resolved.Map.Territory(Midland)!.StructureTypeId);
        Assert.Contains(resolved.State.Log, item => item.Kind == PlayLogKind.InvalidOrderHold);
    }

    private static (CampaignPlayState State, PlayMap Map) Resolve(
        CampaignPlayState state,
        PlayMap map,
        IReadOnlyDictionary<Guid, string?>? allyGroups = null)
    {
        return ActionResolution.Resolve(state, map, OpenAction(), allyGroups ?? UnalignedGroups(), Now);
    }

    private static CampaignPlayState State(CampaignForce force, params OrderSubmission[] submissions)
    {
        return State([force], submissions);
    }

    private static CampaignPlayState State(IReadOnlyList<CampaignForce> forces, IReadOnlyList<OrderSubmission> submissions)
    {
        return new CampaignPlayState(
            [OpenAction()],
            forces,
            [],
            submissions,
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            []);
    }

    private static PhaseWindow OpenAction()
    {
        return new PhaseWindow(
            WindowId,
            1,
            1,
            RoundPhaseKind.Action,
            6,
            DurationUnit.Minutes,
            Now,
            Now.AddMinutes(6),
            PhaseWindowStatus.Open);
    }

    private static OrderSubmission Pillage(Guid forceId)
    {
        return Submit(forceId, ActionKind.Pillage);
    }

    private static OrderSubmission Submit(
        Guid forceId,
        ActionKind kind,
        Guid? targetTerritoryId = null,
        Guid? structureTypeId = null,
        Guid actorUserId = default)
    {
        return new OrderSubmission(
            Guid.NewGuid(),
            WindowId,
            forceId,
            kind,
            targetTerritoryId,
            structureTypeId,
            OrderSource.Commit,
            Now,
            actorUserId == default ? PlayerOne : actorUserId);
    }

    private static PlayMap Map(
        Guid? midlandStructureId = null,
        string? midlandStructureName = null,
        Guid? midlandOwner = null,
        StructureCondition midlandCondition = StructureCondition.Operational,
        bool midlandPillageable = true,
        bool midlandDestructible = true,
        IReadOnlyList<StructureTypePlayRules>? structureTypes = null)
    {
        return new PlayMap(
            [
                new PlayTerritory(NorthSpawn, 1, North, North, null, null, StructureCondition.Operational),
                new PlayTerritory(
                    Midland,
                    2,
                    midlandOwner,
                    null,
                    midlandStructureId,
                    midlandStructureName,
                    midlandCondition,
                    midlandPillageable,
                    midlandDestructible),
                new PlayTerritory(SouthSpawn, 3, South, South, null, null, StructureCondition.Operational),
            ],
            [(NorthSpawn, Midland), (Midland, SouthSpawn)],
            structureTypes);
    }

    private static IReadOnlyList<StructureTypePlayRules> BuildableCatalog()
    {
        return [new StructureTypePlayRules(FortId, "Fortification", true, true, true)];
    }

    private static Dictionary<Guid, string?> UnalignedGroups()
    {
        return new Dictionary<Guid, string?>
        {
            [North] = null,
            [South] = null,
        };
    }

    private static Dictionary<Guid, string?> AlliedGroups()
    {
        return new Dictionary<Guid, string?>
        {
            [North] = "Coalition",
            [South] = "Coalition",
        };
    }
}
