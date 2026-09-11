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
    private static readonly Guid WaterTagId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid CapitalCityId = Guid.Parse("ffffffff-ffff-ffff-ffff-fffffffffff1");
    private static readonly Guid CityId = Guid.Parse("ffffffff-ffff-ffff-ffff-fffffffffff2");
    private static readonly Guid SupplyDepotId = Guid.Parse("ffffffff-ffff-ffff-ffff-fffffffffff3");
    private static readonly Guid TownId = Guid.Parse("ffffffff-ffff-ffff-ffff-fffffffffff4");

    [Fact]
    public void StandardCatalogGivesDiseasedHighestPriorityAndExhaustedCancelsWellRested()
    {
        var catalog = Catalog();
        Assert.Collection(
            catalog,
            status => Assert.Equal(0, status.Priority),
            status => Assert.Equal(1, status.Priority),
            status => Assert.Equal(2, status.Priority),
            status => Assert.Equal(3, status.Priority),
            status => Assert.Equal(4, status.Priority));
        var exhausted = Assert.Single(catalog, static status => status.Name == "Exhausted");
        var rested = Assert.Single(catalog, static status => status.Name == "Well Rested");
        Assert.Equal(rested.Id, Assert.Single(exhausted.CancelsStatusIds));
        Assert.All(catalog.Where(static status => status.Name != "Diseased"), static status =>
        {
            Assert.Equal(1, status.EnableOccurrences);
            Assert.Equal(1, status.ClearOccurrences);
        });
        var diseased = Assert.Single(catalog, static status => status.Name == "Diseased");
        Assert.Contains(
            diseased.EnableConditions,
            static condition =>
                condition.Trigger == ForceStatusEnableTrigger.ConsecutiveActions && condition.Occurrences == 3);
        Assert.Contains(
            diseased.EnableConditions,
            static condition =>
                condition.Trigger == ForceStatusEnableTrigger.BattleLostOrRetreat && condition.Occurrences == 1);
        Assert.Contains(
            diseased.EnableConditions,
            static condition => condition.Trigger == ForceStatusEnableTrigger.Surrender && condition.Occurrences == 2);
        Assert.Equal(4, diseased.ClearConditions.Count);
        Assert.All(
            diseased.ClearConditions,
            static condition =>
            {
                Assert.Equal(ForceStatusClearTrigger.Hold, condition.Trigger);
                Assert.Equal(ConditionLocationKind.StructureType, condition.Location.Kind);
            });
    }

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
    public void ExhaustedCancelsWellRestedToNoStatus()
    {
        var catalog = Catalog();
        var rested = Force(statusName: "Well Rested");
        var facts = new Dictionary<Guid, ForceStatusRules.Facts>
        {
            [ForceId] = ForceStatusRules.FromBattle(fought: true, won: false, lost: false, retreated: false, occupiesWater: false),
        };

        var next = Assert.Single(ForceStatusRules.Apply([rested], catalog, facts));
        Assert.Null(next.StatusName);
    }

    [Fact]
    public void HigherPriorityKeepsCurrentStatusWhenALowerPriorityWouldApply()
    {
        var catalog = Catalog();
        var shaken = Force(statusName: "Shaken");
        var facts = new Dictionary<Guid, ForceStatusRules.Facts>
        {
            [ForceId] = ForceStatusRules.FromBattle(fought: true, won: false, lost: false, retreated: false, occupiesWater: false),
        };

        var next = Assert.Single(ForceStatusRules.Apply([shaken], catalog, facts));
        Assert.Equal("Shaken", next.StatusName);
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
        var diseased = Assert.Single(catalog, static status => status.Name == "Diseased");
        var force = Force(enableStreaks: new Dictionary<string, int>
        {
            [EnableKey(diseased, ForceStatusEnableTrigger.ConsecutiveActions)] = 2,
        });
        var facts = new Dictionary<Guid, ForceStatusRules.Facts>
        {
            [ForceId] = ForceStatusRules.FromAction(ActionKind.Hold, occupiesWater: true),
        };

        var application = ForceStatusRules.ApplyDetailed([force], catalog, facts);
        var next = Assert.Single(application.Forces);
        Assert.Equal("Diseased", next.StatusName);
        Assert.Equal(ForceStatusChangeSource.Catalog, application.Attributions[ForceId].Source);
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
        var diseased = Assert.Single(catalog, static status => status.Name == "Diseased");
        var afterOne = Assert.Single(
            ForceStatusRules.Apply(
                [Force()],
                catalog,
                new Dictionary<Guid, ForceStatusRules.Facts>
                {
                    [ForceId] = ForceStatusRules.FromAction(ActionKind.Move, occupiesWater: true),
                }));
        var afterTwo = Assert.Single(
            ForceStatusRules.Apply(
                [afterOne],
                catalog,
                new Dictionary<Guid, ForceStatusRules.Facts>
                {
                    [ForceId] = ForceStatusRules.FromAction(ActionKind.Move, occupiesWater: true),
                }));
        Assert.Null(afterTwo.StatusName);
        Assert.Equal(2, afterTwo.EnableStreaks.GetValueOrDefault(EnableKey(diseased, ForceStatusEnableTrigger.Surrender)));

        var next = Assert.Single(
            ForceStatusRules.Apply(
                [afterTwo],
                catalog,
                new Dictionary<Guid, ForceStatusRules.Facts>
                {
                    [ForceId] = ForceStatusRules.FromBattle(
                        fought: false,
                        won: false,
                        lost: false,
                        retreated: false,
                        occupiesWater: true,
                        surrendered: true),
                }));
        Assert.Equal("Diseased", next.StatusName);
    }

    [Fact]
    public void SurrenderAfterOneWaterActionDoesNotEnableDiseased()
    {
        var catalog = Catalog();
        var afterOne = Assert.Single(
            ForceStatusRules.Apply(
                [Force()],
                catalog,
                new Dictionary<Guid, ForceStatusRules.Facts>
                {
                    [ForceId] = ForceStatusRules.FromAction(ActionKind.Move, occupiesWater: true),
                }));
        var next = Assert.Single(
            ForceStatusRules.Apply(
                [afterOne],
                catalog,
                new Dictionary<Guid, ForceStatusRules.Facts>
                {
                    [ForceId] = ForceStatusRules.FromBattle(
                        fought: false,
                        won: false,
                        lost: false,
                        retreated: false,
                        occupiesWater: true,
                        surrendered: true),
                }));
        Assert.Null(next.StatusName);
    }

    [Fact]
    public void BattleWindowDoesNotCountAsAWaterAction()
    {
        var catalog = Catalog();
        var diseased = Assert.Single(catalog, static status => status.Name == "Diseased");
        var force = Force(
            enableStreaks: new Dictionary<string, int>
            {
                [EnableKey(diseased, ForceStatusEnableTrigger.ConsecutiveActions)] = 2,
            });
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
        Assert.Equal(2, next.EnableStreaks.GetValueOrDefault(EnableKey(diseased, ForceStatusEnableTrigger.ConsecutiveActions)));
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
    public void HoldAtSettlementClearsDiseasedAndHoldEnablesWellRested()
    {
        var catalog = Catalog();
        var diseased = Force(statusName: "Diseased");
        var facts = new Dictionary<Guid, ForceStatusRules.Facts>
        {
            [ForceId] = ForceStatusRules.FromAction(
                ActionKind.Hold,
                occupiesWater: false,
                occupiesCureSettlement: true,
                territory: Settlement(CapitalCityId, "Capital City")),
        };

        var next = Assert.Single(ForceStatusRules.Apply([diseased], catalog, facts));
        Assert.Equal("Well Rested", next.StatusName);
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
    public void TwoConsecutiveBattleLossesEnableWhenOccurrencesAreTwo()
    {
        var shaken = Status(
            "Shaken",
            ForceStatusEnableTrigger.BattleLostOrRetreat,
            ForceStatusClearTrigger.Hold,
            enableOccurrences: 2);
        var afterOne = Assert.Single(ForceStatusRules.Apply([Force()], [shaken], BattleLoss()));
        Assert.Null(afterOne.StatusName);
        Assert.Equal(1, afterOne.EnableStreaks[EnableKey(shaken)]);

        var afterTwo = Assert.Single(ForceStatusRules.Apply([afterOne], [shaken], BattleLoss()));
        Assert.Equal("Shaken", afterTwo.StatusName);
        Assert.False(afterTwo.EnableStreaks.ContainsKey(EnableKey(shaken)));
    }

    [Fact]
    public void ABattleWinResetsABattleLossEnableStreak()
    {
        var shaken = Status(
            "Shaken",
            ForceStatusEnableTrigger.BattleLostOrRetreat,
            ForceStatusClearTrigger.Hold,
            enableOccurrences: 2);
        var afterLoss = Assert.Single(ForceStatusRules.Apply([Force()], [shaken], BattleLoss()));
        var afterWin = Assert.Single(ForceStatusRules.Apply(
            [afterLoss],
            [shaken],
            new Dictionary<Guid, ForceStatusRules.Facts>
            {
                [ForceId] = ForceStatusRules.FromBattle(true, true, false, false, false),
            }));
        Assert.Null(afterWin.StatusName);
        Assert.Equal(0, afterWin.EnableStreaks.GetValueOrDefault(EnableKey(shaken)));

        var afterSecondLoss = Assert.Single(ForceStatusRules.Apply([afterWin], [shaken], BattleLoss()));
        Assert.Null(afterSecondLoss.StatusName);
        Assert.Equal(1, afterSecondLoss.EnableStreaks[EnableKey(shaken)]);
    }

    [Fact]
    public void HoldDoesNotResetABattleLossEnableStreak()
    {
        var shaken = Status(
            "Shaken",
            ForceStatusEnableTrigger.BattleLostOrRetreat,
            ForceStatusClearTrigger.Hold,
            enableOccurrences: 2);
        var afterLoss = Assert.Single(ForceStatusRules.Apply([Force()], [shaken], BattleLoss()));
        var afterHold = Assert.Single(ForceStatusRules.Apply(
            [afterLoss],
            [shaken],
            new Dictionary<Guid, ForceStatusRules.Facts>
            {
                [ForceId] = ForceStatusRules.FromAction(ActionKind.Hold, occupiesWater: false),
            }));
        Assert.Equal(1, afterHold.EnableStreaks[EnableKey(shaken)]);

        var afterSecondLoss = Assert.Single(ForceStatusRules.Apply([afterHold], [shaken], BattleLoss()));
        Assert.Equal("Shaken", afterSecondLoss.StatusName);
    }

    [Fact]
    public void OccupyingWaterEnablesAfterConfiguredActionPhases()
    {
        var damp = Status(
            "Damp",
            ForceStatusEnableTrigger.OccupyingWater,
            ForceStatusClearTrigger.HoldWhileNotWater,
            enableOccurrences: 3);
        var water = new Dictionary<Guid, ForceStatusRules.Facts>
        {
            [ForceId] = ForceStatusRules.FromAction(ActionKind.Hold, occupiesWater: true),
        };

        var first = Assert.Single(ForceStatusRules.Apply([Force()], [damp], water));
        var second = Assert.Single(ForceStatusRules.Apply([first], [damp], water));
        var afterBattle = Assert.Single(ForceStatusRules.Apply(
            [second],
            [damp],
            new Dictionary<Guid, ForceStatusRules.Facts>
            {
                [ForceId] = ForceStatusRules.FromBattle(true, true, false, false, true),
            }));
        Assert.Null(afterBattle.StatusName);
        Assert.Equal(2, afterBattle.EnableStreaks[EnableKey(damp)]);

        var third = Assert.Single(ForceStatusRules.Apply([afterBattle], [damp], water));
        Assert.Equal("Damp", third.StatusName);
    }

    [Fact]
    public void OccupyingWaterResetsWhenAnActionIsNotOnWater()
    {
        var damp = Status(
            "Damp",
            ForceStatusEnableTrigger.OccupyingWater,
            ForceStatusClearTrigger.HoldWhileNotWater,
            enableOccurrences: 3);
        var water = new Dictionary<Guid, ForceStatusRules.Facts>
        {
            [ForceId] = ForceStatusRules.FromAction(ActionKind.Hold, occupiesWater: true),
        };
        var land = new Dictionary<Guid, ForceStatusRules.Facts>
        {
            [ForceId] = ForceStatusRules.FromAction(ActionKind.Hold, occupiesWater: false),
        };

        var first = Assert.Single(ForceStatusRules.Apply([Force()], [damp], water));
        var second = Assert.Single(ForceStatusRules.Apply([first], [damp], water));
        var inland = Assert.Single(ForceStatusRules.Apply([second], [damp], land));
        Assert.Equal(0, inland.EnableStreaks.GetValueOrDefault(EnableKey(damp)));
        var again = Assert.Single(ForceStatusRules.Apply([inland], [damp], water));
        Assert.Null(again.StatusName);
        Assert.Equal(1, again.EnableStreaks[EnableKey(damp)]);
    }

    [Fact]
    public void DelayedExhaustedStillCancelsWellRested()
    {
        var restedId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var exhaustedId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var rested = new ForceStatusSetup(
            restedId,
            "Well Rested",
            "rest",
            ForceStatusEnableTrigger.Hold,
            ForceStatusClearTrigger.AfterMoveOrBattle,
            4,
            [],
            1,
            3);
        var exhausted = new ForceStatusSetup(
            exhaustedId,
            "Exhausted",
            "tired",
            ForceStatusEnableTrigger.AfterBattle,
            ForceStatusClearTrigger.Hold,
            3,
            [restedId],
            2,
            1);
        var catalog = new[] { exhausted, rested };
        var battle = new Dictionary<Guid, ForceStatusRules.Facts>
        {
            [ForceId] = ForceStatusRules.FromBattle(true, false, false, false, false),
        };

        var afterOne = Assert.Single(ForceStatusRules.Apply([Force(statusName: "Well Rested")], catalog, battle));
        Assert.Equal("Well Rested", afterOne.StatusName);
        var afterTwo = Assert.Single(ForceStatusRules.Apply([afterOne], catalog, battle));
        Assert.Null(afterTwo.StatusName);
        Assert.Equal(0, afterTwo.EnableStreaks.GetValueOrDefault(EnableKey(exhausted)));
    }

    [Fact]
    public void SettlementHoldClearsDiseasedAfterConfiguredOccurrences()
    {
        var catalog = new[]
        {
            new ForceStatusSetup(
                Guid.NewGuid(),
                "Diseased",
                "sick",
                ForceStatusEnableTrigger.Disease,
                ForceStatusClearTrigger.HoldAtSettlement,
                0,
                [],
                1,
                2),
        };
        var hold = new Dictionary<Guid, ForceStatusRules.Facts>
        {
            [ForceId] = ForceStatusRules.FromAction(ActionKind.Hold, occupiesWater: false, occupiesCureSettlement: true),
        };

        var afterOne = Assert.Single(ForceStatusRules.Apply([Force(statusName: "Diseased")], catalog, hold));
        Assert.Equal("Diseased", afterOne.StatusName);
        var afterTwo = Assert.Single(ForceStatusRules.Apply([afterOne], catalog, hold));
        Assert.Null(afterTwo.StatusName);
    }

    [Fact]
    public void EitherListedEnableConditionCanGainTheStatus()
    {
        var shaken = new ForceStatusSetup(
            Guid.NewGuid(),
            "Shaken",
            "effects",
            [
                new ForceStatusEnableCondition(ForceStatusEnableTrigger.BattleLostOrRetreat, 2),
                new ForceStatusEnableCondition(ForceStatusEnableTrigger.Hold, 1),
            ],
            [new ForceStatusClearCondition(ForceStatusClearTrigger.BattleWon)]);
        var afterHold = Assert.Single(ForceStatusRules.Apply(
            [Force()],
            [shaken],
            new Dictionary<Guid, ForceStatusRules.Facts>
            {
                [ForceId] = ForceStatusRules.FromAction(ActionKind.Hold, occupiesWater: false),
            }));
        Assert.Equal("Shaken", afterHold.StatusName);
    }

    [Fact]
    public void EitherListedClearConditionCanClearTheStatus()
    {
        var shaken = new ForceStatusSetup(
            Guid.NewGuid(),
            "Shaken",
            "effects",
            [new ForceStatusEnableCondition(ForceStatusEnableTrigger.BattleLostOrRetreat)],
            [
                new ForceStatusClearCondition(ForceStatusClearTrigger.Hold, 2),
                new ForceStatusClearCondition(ForceStatusClearTrigger.BattleWon, 1),
            ]);
        var afterWin = Assert.Single(ForceStatusRules.Apply(
            [Force(statusName: "Shaken")],
            [shaken],
            new Dictionary<Guid, ForceStatusRules.Facts>
            {
                [ForceId] = ForceStatusRules.FromBattle(true, true, false, false, false),
            }));
        Assert.Null(afterWin.StatusName);
    }

    [Fact]
    public void DescribeChangeIncludesTheSource()
    {
        var text = ForceStatusRules.DescribeChange("Shaken", "Diseased", ForceStatusChangeSource.Staff, null);
        Assert.Contains("Shaken became Diseased", text);
        Assert.Contains("manager or administrator", text);
    }

    private static CampaignForce Force(
        string? statusName = null,
        int consecutiveWaterActions = 0,
        IReadOnlyDictionary<string, int>? enableStreaks = null)
    {
        return new CampaignForce(
            ForceId,
            UserId,
            FactionId,
            TerritoryId,
            false,
            statusName,
            consecutiveWaterActions: consecutiveWaterActions,
            enableStreaks: enableStreaks);
    }

    private static PlayTerritory Settlement(Guid structureId, string name)
    {
        return new PlayTerritory(
            TerritoryId,
            1,
            FactionId,
            null,
            structureId,
            name,
            StructureCondition.Operational);
    }

    private static ForceStatusSetup Status(
        string name,
        ForceStatusEnableTrigger enable,
        ForceStatusClearTrigger clear,
        int enableOccurrences = 1,
        int clearOccurrences = 1,
        int priority = 1,
        IReadOnlyList<Guid>? cancels = null)
    {
        return new ForceStatusSetup(
            Guid.NewGuid(),
            name,
            "effects",
            enable,
            clear,
            priority,
            cancels,
            enableOccurrences,
            clearOccurrences);
    }

    private static Dictionary<Guid, ForceStatusRules.Facts> BattleLoss()
    {
        return new Dictionary<Guid, ForceStatusRules.Facts>
        {
            [ForceId] = ForceStatusRules.FromBattle(true, false, true, true, false),
        };
    }

    private static string EnableKey(ForceStatusSetup status, ForceStatusEnableTrigger? trigger = null)
    {
        var condition = trigger is { } requested
            ? status.EnableConditions.First(item => item.Trigger == requested)
            : status.EnableConditions[0];
        return ForceStatusStreakKeys.Enable(status.Id, condition.Id);
    }

    private static IReadOnlyList<ForceStatusSetup> Catalog()
    {
        return ForceStatusCatalog.CreateStandardSetups(
            WaterTagId,
            new Dictionary<string, Guid>
            {
                ["Capital City"] = CapitalCityId,
                ["City"] = CityId,
                ["Supply Depot"] = SupplyDepotId,
                ["Town"] = TownId,
            });
    }
}
