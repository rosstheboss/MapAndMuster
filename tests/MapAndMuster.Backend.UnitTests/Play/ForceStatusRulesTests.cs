using MapAndMuster.Domain.Campaigns;
using MapAndMuster.Domain.Play;

namespace MapAndMuster.Backend.UnitTests.Play;

public sealed class ForceStatusRulesTests
{
    private static readonly Guid ForceId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OtherForceId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaab");
    private static readonly Guid UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid OtherUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1");
    private static readonly Guid FactionId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid OtherFactionId = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc1");
    private static readonly Guid TerritoryId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    [Fact]
    public void HoldEnablesWellRestedAndClearsShaken()
    {
        var catalog = Catalog();
        var shaken = Force(statusName: "Shaken");
        var facts = new Dictionary<Guid, ForceStatusRules.Facts>
        {
            [ForceId] = ForceStatusRules.FromAction(ActionKind.Hold, occupiesWater: false),
        };

        var next = Assert.Single(ForceStatusRules.Apply([shaken], catalog, facts));
        Assert.Equal("Well Rested", next.StatusName);
    }

    [Fact]
    public void BattleLossEnablesShakenInsteadOfExhausted()
    {
        var catalog = Catalog();
        var force = Force();
        var facts = new Dictionary<Guid, ForceStatusRules.Facts>
        {
            [ForceId] = ForceStatusRules.FromBattle(fought: true, won: false, lost: true, retreated: true, occupiesWater: false),
        };

        var next = Assert.Single(ForceStatusRules.Apply([force], catalog, facts));
        Assert.Equal("Shaken", next.StatusName);
    }

    [Fact]
    public void BattleWinEnablesConfidentAndClearsWellRested()
    {
        var catalog = Catalog();
        var rested = Force(statusName: "Well Rested");
        var facts = new Dictionary<Guid, ForceStatusRules.Facts>
        {
            [ForceId] = ForceStatusRules.FromBattle(fought: true, won: true, lost: false, retreated: false, occupiesWater: false),
        };

        var next = Assert.Single(ForceStatusRules.Apply([rested], catalog, facts));
        Assert.Equal("Confident", next.StatusName);
    }

    [Fact]
    public void DrawEnablesExhaustedWhenNotShakenOrConfident()
    {
        var catalog = Catalog();
        var force = Force();
        var facts = new Dictionary<Guid, ForceStatusRules.Facts>
        {
            [ForceId] = ForceStatusRules.FromBattle(fought: true, won: false, lost: false, retreated: false, occupiesWater: false),
        };

        var next = Assert.Single(ForceStatusRules.Apply([force], catalog, facts));
        Assert.Equal("Exhausted", next.StatusName);
    }

    [Fact]
    public void OneWaterActionDoesNotEnableDiseased()
    {
        var catalog = Catalog();
        var force = Force();
        var facts = new Dictionary<Guid, ForceStatusRules.Facts>
        {
            [ForceId] = ForceStatusRules.FromAction(ActionKind.Move, occupiesWater: true),
        };

        var next = Assert.Single(ForceStatusRules.Apply([force], catalog, facts));
        Assert.Null(next.StatusName);
        Assert.Equal(1, next.ConsecutiveWaterActions);
    }

    [Fact]
    public void ThreeConsecutiveWaterActionsEnableDiseased()
    {
        var catalog = Catalog();
        var force = Force(consecutiveWaterActions: 2);
        var facts = new Dictionary<Guid, ForceStatusRules.Facts>
        {
            [ForceId] = ForceStatusRules.FromAction(ActionKind.Hold, occupiesWater: true),
        };

        var application = ForceStatusRules.ApplyDetailed([force], catalog, facts);
        var next = Assert.Single(application.Forces);
        Assert.Equal("Diseased", next.StatusName);
        Assert.Equal(3, next.ConsecutiveWaterActions);
        Assert.Equal(ForceStatusChangeSource.ConsecutiveWater, application.Attributions[ForceId].Source);
    }

    [Fact]
    public void WaterBattleDefeatEnablesDiseased()
    {
        var catalog = Catalog();
        var force = Force(statusName: "Confident");
        var facts = new Dictionary<Guid, ForceStatusRules.Facts>
        {
            [ForceId] = ForceStatusRules.FromBattle(
                fought: true,
                won: false,
                lost: true,
                retreated: true,
                occupiesWater: false,
                lostOnWater: true),
        };

        var next = Assert.Single(ForceStatusRules.Apply([force], catalog, facts));
        Assert.Equal("Diseased", next.StatusName);
    }

    [Fact]
    public void SurrenderAfterTwoWaterActionsEnablesDiseased()
    {
        var catalog = Catalog();
        var force = Force(consecutiveWaterActions: 2);
        var facts = new Dictionary<Guid, ForceStatusRules.Facts>
        {
            [ForceId] = ForceStatusRules.FromBattle(
                fought: false,
                won: false,
                lost: true,
                retreated: true,
                occupiesWater: true,
                surrendered: true),
        };

        var next = Assert.Single(ForceStatusRules.Apply([force], catalog, facts));
        Assert.Equal("Diseased", next.StatusName);
        Assert.Equal(2, next.ConsecutiveWaterActions);
    }

    [Fact]
    public void SurrenderAfterOneWaterActionDoesNotEnableDiseased()
    {
        var catalog = Catalog();
        var force = Force(consecutiveWaterActions: 1);
        var facts = new Dictionary<Guid, ForceStatusRules.Facts>
        {
            [ForceId] = ForceStatusRules.FromBattle(
                fought: false,
                won: false,
                lost: true,
                retreated: true,
                occupiesWater: true,
                surrendered: true),
        };

        var next = Assert.Single(ForceStatusRules.Apply([force], catalog, facts));
        Assert.Equal("Shaken", next.StatusName);
        Assert.Equal(1, next.ConsecutiveWaterActions);
    }

    [Fact]
    public void BattleWindowDoesNotCountAsAWaterAction()
    {
        var catalog = Catalog();
        var force = Force(consecutiveWaterActions: 2);
        var facts = new Dictionary<Guid, ForceStatusRules.Facts>
        {
            [ForceId] = ForceStatusRules.FromBattle(
                fought: true,
                won: false,
                lost: false,
                retreated: false,
                occupiesWater: true),
        };

        var next = Assert.Single(ForceStatusRules.Apply([force], catalog, facts));
        Assert.Equal("Exhausted", next.StatusName);
        Assert.Equal(2, next.ConsecutiveWaterActions);
    }

    [Fact]
    public void OccupyingWaterStillEnablesACustomStatus()
    {
        var damp = new ForceStatusSetup(
            Guid.NewGuid(),
            "Damp",
            "Custom water status.",
            ForceStatusEnableTrigger.OccupyingWater,
            ForceStatusClearTrigger.HoldWhileNotWater);
        var force = Force();
        var facts = new Dictionary<Guid, ForceStatusRules.Facts>
        {
            [ForceId] = ForceStatusRules.FromAction(ActionKind.Move, occupiesWater: true),
        };

        var next = Assert.Single(ForceStatusRules.Apply([force], [damp], facts));
        Assert.Equal("Damp", next.StatusName);
    }

    [Fact]
    public void HoldAtSettlementClearsDiseasedToNormalWithoutWellRested()
    {
        var catalog = Catalog();
        var diseased = Force(statusName: "Diseased");
        var facts = new Dictionary<Guid, ForceStatusRules.Facts>
        {
            [ForceId] = ForceStatusRules.FromAction(
                ActionKind.Hold,
                occupiesWater: false,
                occupiesCureSettlement: true),
        };

        var next = Assert.Single(ForceStatusRules.Apply([diseased], catalog, facts));
        Assert.Null(next.StatusName);
    }

    [Fact]
    public void HoldOffWaterWithoutSettlementLeavesDiseased()
    {
        var catalog = Catalog();
        var diseased = Force(statusName: "Diseased");
        var facts = new Dictionary<Guid, ForceStatusRules.Facts>
        {
            [ForceId] = ForceStatusRules.FromAction(ActionKind.Hold, occupiesWater: false),
        };

        var next = Assert.Single(ForceStatusRules.Apply([diseased], catalog, facts));
        Assert.Equal("Diseased", next.StatusName);
    }

    [Fact]
    public void ContagionInfectsAnotherFactionInTheSameTerritory()
    {
        var catalog = Catalog();
        var carrier = Force(statusName: "Diseased");
        var neighbor = new CampaignForce(OtherForceId, OtherUserId, OtherFactionId, TerritoryId, false);
        var facts = new Dictionary<Guid, ForceStatusRules.Facts>
        {
            [ForceId] = ForceStatusRules.FromAction(ActionKind.Hold, occupiesWater: false),
            [OtherForceId] = ForceStatusRules.FromAction(ActionKind.Hold, occupiesWater: false),
        };

        var next = ForceStatusRules.Apply([carrier, neighbor], catalog, facts);
        Assert.All(next, force => Assert.Equal("Diseased", force.StatusName));
    }

    [Fact]
    public void MissionWinCanSetConfidentUnlessDiseasedIsLeftUnchanged()
    {
        var shaken = Force(statusName: "Shaken");
        var mission = new MissionSetup(
            Guid.NewGuid(),
            "Triumph",
            url: null,
            clearFile: false,
            statusChanges:
            [
                new MissionStatusChangeSetup(Guid.NewGuid(), MissionBattleOutcome.Win, "Diseased", null, leaveUnchanged: true),
                new MissionStatusChangeSetup(Guid.NewGuid(), MissionBattleOutcome.Win, "Shaken", "Normal", leaveUnchanged: false),
                new MissionStatusChangeSetup(Guid.NewGuid(), MissionBattleOutcome.Win, null, "Confident", leaveUnchanged: false),
            ]);

        var (fromShaken, shakenAttribution) = ForceStatusRules.ApplyMission(shaken, mission, won: true, SpecialRuleContext.None);
        Assert.Null(fromShaken.StatusName);
        Assert.NotNull(shakenAttribution);

        var diseased = Force(statusName: "Diseased");
        var (fromDiseased, diseasedAttribution) = ForceStatusRules.ApplyMission(diseased, mission, won: true, SpecialRuleContext.None);
        Assert.Equal("Diseased", fromDiseased.StatusName);
        Assert.Null(diseasedAttribution);
    }

    [Fact]
    public void StaffAssignIgnoresImmunity()
    {
        var catalog = Catalog();
        var undead = Force();
        var assigned = ForceStatusRules.Assign(undead, "Diseased", catalog);
        Assert.Equal("Diseased", assigned.StatusName);
    }

    [Fact]
    public void EmptyCatalogLeavesStatusesUnchanged()
    {
        var shaken = Force(statusName: "Shaken");
        var facts = new Dictionary<Guid, ForceStatusRules.Facts>
        {
            [ForceId] = ForceStatusRules.FromAction(ActionKind.Hold, occupiesWater: false),
        };

        var next = Assert.Single(ForceStatusRules.Apply([shaken], [], facts));
        Assert.Equal("Shaken", next.StatusName);
    }

    [Fact]
    public void DescribeChangeIncludesTheSource()
    {
        var text = ForceStatusRules.DescribeChange("Shaken", "Diseased", ForceStatusChangeSource.Staff, null);
        Assert.Contains("Shaken became Diseased", text);
        Assert.Contains("manager or administrator", text);
    }

    private static CampaignForce Force(string? statusName = null, int consecutiveWaterActions = 0)
    {
        return new CampaignForce(ForceId, UserId, FactionId, TerritoryId, false, statusName, consecutiveWaterActions: consecutiveWaterActions);
    }

    private static IReadOnlyList<ForceStatusSetup> Catalog()
    {
        return
        [
            .. ForceStatusCatalog.Standard.Select(status => new ForceStatusSetup(
                Guid.NewGuid(),
                status.Name,
                status.Effects,
                status.EnableTrigger,
                status.ClearTrigger)),
        ];
    }
}
