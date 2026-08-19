using Campaign.Domain.Campaigns;
using Campaign.Domain.Play;

namespace Campaign.Backend.UnitTests.Play;

public sealed class BattleMissionRulesTests
{
    private static readonly Guid Plains = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Town = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid Territory = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid North = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid South = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid HolderId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid MoverId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid MeetingId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid AssaultId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly Guid TownHoldId = Guid.Parse("77777777-7777-7777-7777-777777777777");

    [Fact]
    public void PrefersStructureMissionsOverTerrain()
    {
        var assignment = BattleMissionRules.Choose(
            TerritoryWith(structureTypeId: Town),
            Present(),
            MeetingArrivals(),
            Unaligned(),
            [],
            [PlainsTerrain(MeetingId)],
            [TownStructure(TownHoldId)],
            static _ => 0);

        Assert.NotNull(assignment);
        Assert.Equal(TownHoldId, assignment.MissionId);
        Assert.Null(assignment.AttackerForceId);
    }

    [Fact]
    public void UsesTerrainMissionsWhenTheStructureHasNone()
    {
        var assignment = BattleMissionRules.Choose(
            TerritoryWith(structureTypeId: Town),
            Present(),
            MeetingArrivals(),
            Unaligned(),
            [],
            [PlainsTerrain(MeetingId)],
            [TownStructure()],
            static _ => 0);

        Assert.NotNull(assignment);
        Assert.Equal(MeetingId, assignment.MissionId);
    }

    [Fact]
    public void PicksAmongStructureMissionsWhenSeveralExist()
    {
        var second = Guid.Parse("88888888-8888-8888-8888-888888888888");
        var assignment = BattleMissionRules.Choose(
            TerritoryWith(structureTypeId: Town),
            Present(),
            MeetingArrivals(),
            Unaligned(),
            [],
            [PlainsTerrain(MeetingId)],
            [TownStructure(TownHoldId, second)],
            static count => count == 2 ? 1 : 0);

        Assert.Equal(second, assignment!.MissionId);
    }

    [Fact]
    public void HoldVersusMoveUsesAttackerDefenderMission()
    {
        var assignment = BattleMissionRules.Choose(
            TerritoryWith(),
            Present(),
            new Dictionary<Guid, ActionKind>
            {
                [HolderId] = ActionKind.Hold,
                [MoverId] = ActionKind.Move,
            },
            Unaligned(),
            [],
            [PlainsTerrain(MeetingId, AssaultId, attackerDefender: true)],
            [],
            static _ => 0);

        Assert.Equal(AssaultId, assignment!.MissionId);
        Assert.Equal(MoverId, assignment.AttackerForceId);
        Assert.Equal(HolderId, assignment.DefenderForceId);
    }

    [Fact]
    public void MeetingEngagementDoesNotPickAttackerDefenderWhenANormalMissionExists()
    {
        var assignment = BattleMissionRules.Choose(
            TerritoryWith(),
            Present(),
            MeetingArrivals(),
            Unaligned(),
            [],
            [PlainsTerrain(MeetingId, AssaultId, attackerDefender: true)],
            [],
            static _ => 0);

        Assert.Equal(MeetingId, assignment!.MissionId);
        Assert.Null(assignment.AttackerForceId);
        Assert.Null(assignment.DefenderForceId);
    }

    [Fact]
    public void MeetingEngagementAssignsAttackerDefenderWhenNoNormalMissionExists()
    {
        var assignment = BattleMissionRules.Choose(
            TerritoryWith(),
            Present(),
            MeetingArrivals(),
            Unaligned(),
            [],
            [PlainsTerrain(AssaultId, attackerDefender: true)],
            [],
            static _ => 0);

        Assert.Equal(AssaultId, assignment!.MissionId);
        Assert.NotNull(assignment.AttackerForceId);
        Assert.NotNull(assignment.DefenderForceId);
        Assert.NotEqual(assignment.AttackerForceId, assignment.DefenderForceId);
    }

    [Fact]
    public void StructureOwnerIsTheDefender()
    {
        var assignment = BattleMissionRules.Choose(
            TerritoryWith(structureTypeId: Town, ownerFactionId: North),
            Present(),
            MeetingArrivals(),
            Unaligned(),
            [],
            [PlainsTerrain(MeetingId, AssaultId, attackerDefender: true)],
            [TownStructure(TownHoldId, attackerDefender: true)],
            static _ => 0);

        Assert.Equal(TownHoldId, assignment!.MissionId);
        Assert.Equal(MoverId, assignment.AttackerForceId);
        Assert.Equal(HolderId, assignment.DefenderForceId);
    }

    [Fact]
    public void BackstabberIsTheAttacker()
    {
        var assignment = BattleMissionRules.Choose(
            TerritoryWith(),
            Present(),
            new Dictionary<Guid, ActionKind>
            {
                [HolderId] = ActionKind.Backstab,
                [MoverId] = ActionKind.Hold,
            },
            Unaligned(),
            [],
            [PlainsTerrain(MeetingId, AssaultId, attackerDefender: true)],
            [],
            static _ => 0);

        Assert.Equal(AssaultId, assignment!.MissionId);
        Assert.Equal(HolderId, assignment.AttackerForceId);
        Assert.Equal(MoverId, assignment.DefenderForceId);
    }

    [Fact]
    public void SplitArrivalIsAnAttackerAgainstAHolder()
    {
        var assignment = BattleMissionRules.Choose(
            TerritoryWith(),
            Present(),
            new Dictionary<Guid, ActionKind>
            {
                [HolderId] = ActionKind.Hold,
                [MoverId] = ActionKind.Split,
            },
            Unaligned(),
            [],
            [PlainsTerrain(MeetingId, AssaultId, attackerDefender: true)],
            [],
            static _ => 0);

        Assert.Equal(AssaultId, assignment!.MissionId);
        Assert.Equal(MoverId, assignment.AttackerForceId);
        Assert.Equal(HolderId, assignment.DefenderForceId);
    }

    [Fact]
    public void RetreatArrivalIsNotAnAttacker()
    {
        var assignment = BattleMissionRules.Choose(
            TerritoryWith(),
            Present(),
            new Dictionary<Guid, ActionKind>
            {
                [HolderId] = ActionKind.Hold,
                [MoverId] = ActionKind.Retreat,
            },
            Unaligned(),
            [],
            [PlainsTerrain(MeetingId, AssaultId, attackerDefender: true)],
            [],
            static _ => 0);

        Assert.Equal(MeetingId, assignment!.MissionId);
        Assert.Null(assignment.AttackerForceId);
    }

    private static PlayTerritory TerritoryWith(Guid? structureTypeId = null, Guid? ownerFactionId = null)
    {
        return new PlayTerritory(
            Territory,
            1,
            ownerFactionId,
            spawnFactionId: null,
            structureTypeId,
            structureTypeId is null ? null : "Town",
            StructureCondition.Operational,
            terrainTypeId: Plains);
    }

    private static CampaignForce[] Present()
    {
        return
        [
            new CampaignForce(HolderId, Guid.NewGuid(), North, Territory, false),
            new CampaignForce(MoverId, Guid.NewGuid(), South, Territory, false),
        ];
    }

    private static Dictionary<Guid, ActionKind> MeetingArrivals()
    {
        return new Dictionary<Guid, ActionKind>
        {
            [HolderId] = ActionKind.Move,
            [MoverId] = ActionKind.Move,
        };
    }

    private static Dictionary<Guid, string?> Unaligned()
    {
        return new Dictionary<Guid, string?>
        {
            [North] = null,
            [South] = null,
        };
    }

    private static TerrainTypeSetup PlainsTerrain(Guid firstId, Guid? secondId = null, bool attackerDefender = false)
    {
        MissionSetup[] missions =
        [
            new MissionSetup(firstId, "Meeting", null, false, isAttackerDefender: false),
        ];
        if (secondId is { } extra)
        {
            missions =
            [
                missions[0],
                new MissionSetup(extra, "Assault", null, false, isAttackerDefender: attackerDefender),
            ];
        }
        else if (attackerDefender)
        {
            missions = [new MissionSetup(firstId, "Assault", null, false, isAttackerDefender: true)];
        }

        return new TerrainTypeSetup(Plains, "Plains", "#7CB342", missions);
    }

    private static StructureTypeSetup TownStructure(Guid? firstId = null, Guid? secondId = null, bool attackerDefender = false)
    {
        var missions = new List<MissionSetup>();
        if (firstId is { } first)
        {
            missions.Add(new MissionSetup(first, "Town hold", null, false, isAttackerDefender: attackerDefender));
        }

        if (secondId is { } second)
        {
            missions.Add(new MissionSetup(second, "Town wall", null, false));
        }

        return new StructureTypeSetup(Town, "Town", "Town", false, false, false, true, true, missions);
    }
}
