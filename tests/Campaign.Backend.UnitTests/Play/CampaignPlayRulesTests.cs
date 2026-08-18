using Campaign.Domain.Campaigns;
using Campaign.Domain.Play;

namespace Campaign.Backend.UnitTests.Play;

public sealed class CampaignPlayRulesTests
{
    private static readonly Guid North = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid South = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid NorthSpawn = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SouthSpawn = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Midland = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid PlayerOne = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid PlayerTwo = Guid.Parse("55555555-5555-5555-5555-555555555555");

    [Fact]
    public void SeedPlacesForcesAndSpawnFlags()
    {
        var schedule = CreateSchedule();
        var map = CreateMap(ownerMidland: null);
        var seeded = CampaignPlayRules.Seed(
            CampaignPlayState.Empty,
            map,
            schedule,
            [new PlayerFactionAssignment(PlayerOne, North), new PlayerFactionAssignment(PlayerTwo, South)],
            schedule.StartsUtc);

        Assert.Equal(2, seeded.State.Forces.Count);
        Assert.Equal(NorthSpawn, seeded.State.Forces.Single(force => force.FactionId == North).TerritoryId);
        Assert.Equal(North, seeded.Map.Territory(NorthSpawn)?.OwnerFactionId);
        Assert.Equal(South, seeded.Map.Territory(SouthSpawn)?.OwnerFactionId);
        Assert.Equal(PhaseWindowStatus.Open, seeded.State.Windows[0].Status);
        Assert.Equal(RoundPhaseKind.Action, seeded.State.Windows[0].Kind);
        Assert.Contains(seeded.State.Log, item => item.Kind == PlayLogKind.CampaignStarted);
        var northForce = seeded.State.Forces.Single(force => force.FactionId == North);
        Assert.Equal([Midland], CampaignPlayRules.EligibleMoves(seeded.Map, northForce));
        Assert.Contains(NorthSpawn, CampaignPlayRules.EligibleRetreats(seeded.Map, northForce));
    }

    [Fact]
    public void LastCommitClosesEmptyBattlePhaseEarly()
    {
        var (state, map, schedule) = Seeded();
        var window = state.Windows[0];
        var battle = state.Windows[1];
        var northForce = state.Forces.Single(force => force.FactionId == North);
        Assert.True(CampaignPlayRules.TrySaveDraft(
            state,
            PlayerOne,
            northForce.Id,
            ActionKind.Hold,
            null,
            null,
            map,
            schedule.StartsUtc.AddMinutes(1),
            out state,
            out _));
        Assert.True(CampaignPlayRules.TryCommit(
            state,
            map,
            PlayerOne,
            AllyGroups(),
            schedule.StartsUtc.AddMinutes(1),
            out var afterOne,
            out _));
        state = afterOne!.State;
        Assert.Contains(state.Log, item => item.Kind == PlayLogKind.CampaignStarted);
        Assert.DoesNotContain(state.Log, item => item.Kind == PlayLogKind.ResolvedAction);

        var southForce = state.Forces.Single(force => force.FactionId == South);
        Assert.True(CampaignPlayRules.TrySaveDraft(
            state,
            PlayerTwo,
            southForce.Id,
            ActionKind.Hold,
            null,
            null,
            map,
            schedule.StartsUtc.AddMinutes(1),
            out state,
            out _));
        Assert.True(CampaignPlayRules.TryCommit(
            state,
            map,
            PlayerTwo,
            AllyGroups(),
            schedule.StartsUtc.AddMinutes(1),
            out var closed,
            out _));

        Assert.Equal(PhaseWindowStatus.Resolved, closed!.State.Windows[0].Status);
        Assert.Equal(PhaseWindowStatus.Resolved, closed.State.Windows[1].Status);
        Assert.Equal(PhaseWindowStatus.Open, closed.State.Windows[2].Status);
        Assert.Equal(schedule.StartsUtc.AddMinutes(1), closed.State.Windows[2].StartsUtc);
        Assert.Equal(2, closed.State.Log.Count(item => item.Kind == PlayLogKind.ResolvedAction && item.ActionKind == ActionKind.Hold));
        _ = window;
        _ = battle;
    }

    [Fact]
    public void SeedOmitsPlayersWhoHaveNotChosenAFaction()
    {
        var schedule = CreateSchedule();
        var map = CreateMap(ownerMidland: null);
        var seeded = CampaignPlayRules.Seed(
            CampaignPlayState.Empty,
            map,
            schedule,
            [new PlayerFactionAssignment(PlayerOne, North), new PlayerFactionAssignment(PlayerTwo, null)],
            schedule.StartsUtc);

        Assert.Single(seeded.State.Forces);
        Assert.Equal(PlayerOne, seeded.State.Forces[0].ControllerUserId);
        Assert.Equal(NorthSpawn, seeded.State.Forces[0].TerritoryId);
        Assert.False(CampaignPlayRules.TryCommit(
            seeded.State,
            seeded.Map,
            PlayerTwo,
            AllyGroups(),
            schedule.StartsUtc,
            out _,
            out var error));
        Assert.Equal("order.not_required", error!.Code);
    }

    [Fact]
    public void EnsureForcePlacesTheStartingForceAtTheFactionSpawn()
    {
        var (state, map, schedule) = Seeded();
        _ = schedule;
        var withoutSouth = state.With(forces: [.. state.Forces.Where(force => force.FactionId != South)]);
        var ensured = CampaignPlayRules.EnsureForce(withoutSouth, map, PlayerTwo, South);
        var south = ensured.State.Forces.Single(force => force.ControllerUserId == PlayerTwo);
        Assert.Equal(SouthSpawn, south.TerritoryId);
        Assert.Equal(South, south.FactionId);
    }

    [Fact]
    public void CommitRequiresADraftForEveryForce()
    {
        var (state, map, schedule) = Seeded();
        Assert.False(CampaignPlayRules.TryCommit(
            state,
            map,
            PlayerOne,
            AllyGroups(),
            schedule.StartsUtc,
            out _,
            out var error));
        Assert.Equal("order.draft.required", error!.Code);

        var force = state.Forces.Single(item => item.FactionId == North);
        Assert.True(CampaignPlayRules.TrySaveDraft(
            state, PlayerOne, force.Id, ActionKind.Hold, null, null, map, schedule.StartsUtc, out state, out _));
        Assert.True(CampaignPlayRules.TryCommit(state, map, PlayerOne, AllyGroups(), schedule.StartsUtc, out var committed, out _));
        Assert.False(committed!.State.Windows[0].Status == PhaseWindowStatus.Resolved);
        _ = schedule;
    }

    [Fact]
    public void UncommitIsAllowedUntilTheWindowCloses()
    {
        var (state, map, schedule) = Seeded();
        var force = state.Forces.Single(item => item.FactionId == North);
        Assert.True(CampaignPlayRules.TrySaveDraft(
            state, PlayerOne, force.Id, ActionKind.Hold, null, null, map, schedule.StartsUtc, out state, out _));
        Assert.True(CampaignPlayRules.TryCommit(state, map, PlayerOne, AllyGroups(), schedule.StartsUtc, out var committed, out _));
        var window = committed!.State.CurrentWindow()!;
        Assert.False(CampaignPlayRules.TryUncommit(committed.State, PlayerOne, window.EndsUtc, out _, out var closedError));
        Assert.Equal("order.window.closed", closedError!.Code);
        Assert.True(CampaignPlayRules.TryUncommit(committed.State, PlayerOne, schedule.StartsUtc, out var open, out _));
        Assert.DoesNotContain(open!.Commitments, item => item.UserId == PlayerOne);
    }

    [Fact]
    public void DeadlineSubmitsDraftAndCreatesHold()
    {
        var (state, map, schedule) = Seeded();
        var northForce = state.Forces.Single(force => force.FactionId == North);
        Assert.True(CampaignPlayRules.TrySaveDraft(
            state,
            PlayerOne,
            northForce.Id,
            ActionKind.Move,
            Midland,
            null,
            map,
            schedule.StartsUtc,
            out state,
            out _));

        var deadline = state.Windows[0].EndsUtc;
        var advanced = CampaignPlayRules.Advance(state, map, schedule, AllyGroups(), deadline);
        var submissions = advanced.State.Submissions;
        Assert.Contains(submissions, item => item.ForceId == northForce.Id && item.Kind == ActionKind.Move && item.Source == OrderSource.DeadlineDraft);
        Assert.Contains(submissions, item => item.Kind == ActionKind.Hold && item.Source == OrderSource.DeadlineHold);
        Assert.Contains(advanced.State.Log, item => item.Kind == PlayLogKind.DeadlineDraftSubmitted && item.ForceId == northForce.Id);
        Assert.Contains(advanced.State.Log, item => item.Kind == PlayLogKind.MissingOrderHold);
        Assert.DoesNotContain(advanced.State.Log, item => item.Kind == PlayLogKind.ResolvedAction && item.ForceId != northForce.Id && item.ActionKind != ActionKind.Hold);
        Assert.Equal(Midland, advanced.State.Forces.Single(force => force.FactionId == North).TerritoryId);
        Assert.Equal(North, advanced.Map.Territory(Midland)?.OwnerFactionId);
    }

    [Fact]
    public void InvalidMoveBecomesHoldAndSpawnIsForbidden()
    {
        var (state, map, schedule) = Seeded();
        var northForce = state.Forces.Single(force => force.FactionId == North);
        Assert.False(CampaignPlayRules.TrySaveDraft(
            state,
            PlayerOne,
            northForce.Id,
            ActionKind.Move,
            SouthSpawn,
            null,
            map,
            schedule.StartsUtc,
            out _,
            out var error));
        Assert.Equal("order.spawn.forbidden", error!.Code);

        Assert.True(CampaignPlayRules.TrySaveDraft(
            state, PlayerOne, northForce.Id, ActionKind.Move, Midland, null, map, schedule.StartsUtc, out state, out _));
        Assert.True(CampaignPlayRules.TryCommit(state, map, PlayerOne, AllyGroups(), schedule.StartsUtc, out var afterNorth, out _));
        state = afterNorth!.State;
        var southForce = state.Forces.Single(force => force.FactionId == South);
        Assert.True(CampaignPlayRules.TrySaveDraft(
            state, PlayerTwo, southForce.Id, ActionKind.Hold, null, null, map, schedule.StartsUtc, out state, out _));
        Assert.True(CampaignPlayRules.TryCommit(state, map, PlayerTwo, AllyGroups(), schedule.StartsUtc, out var closed, out _));
        Assert.Equal(Midland, closed!.State.Forces.Single(force => force.FactionId == North).TerritoryId);
    }

    [Fact]
    public void EnemyArrivalCreatesBattleAndMatchingResultsFinalize()
    {
        var (state, map, schedule) = Seeded();
        var northForce = state.Forces.Single(force => force.FactionId == North);
        var southForce = state.Forces.Single(force => force.FactionId == South);
        Assert.True(CampaignPlayRules.TrySaveDraft(
            state, PlayerOne, northForce.Id, ActionKind.Move, Midland, null, map, schedule.StartsUtc, out state, out _));
        Assert.True(CampaignPlayRules.TryCommit(state, map, PlayerOne, AllyGroups(), schedule.StartsUtc, out var afterNorth, out _));
        state = afterNorth!.State;
        Assert.True(CampaignPlayRules.TrySaveDraft(
            state, PlayerTwo, southForce.Id, ActionKind.Move, Midland, null, map, schedule.StartsUtc, out state, out _));
        Assert.True(CampaignPlayRules.TryCommit(state, map, PlayerTwo, AllyGroups(), schedule.StartsUtc, out var closed, out _));

        Assert.Single(closed!.State.Battles);
        var battle = closed.State.Battles[0];
        Assert.Equal(BattleStatus.AwaitingResults, battle.Status);
        Assert.Contains(closed.State.Log, item => item.Kind == PlayLogKind.BattleCreated && item.BattleId == battle.Id);
        Assert.True(closed.State.Forces.All(force => force.InBattle));

        Assert.True(CampaignPlayRules.TrySubmitBattleResult(
            closed.State, PlayerOne, battle.Id, northForce.Id, false, schedule.StartsUtc.AddMinutes(7), out var oneResult, out _));
        Assert.True(CampaignPlayRules.TryAcceptBattleResult(
            oneResult!.State, PlayerTwo, battle.Id, schedule.StartsUtc.AddMinutes(7), out var accepted, out _));
        Assert.Equal(BattleStatus.Finalized, accepted!.State.Battles[0].Status);
        Assert.Equal(northForce.Id, accepted.State.Battles[0].WinnerForceId);
        Assert.Equal(PhaseWindowStatus.Open, accepted.State.Windows[1].Status);
    }

    [Fact]
    public void AgreedDrawClosesTheBattlePhaseEarly()
    {
        var (state, map, schedule) = Seeded();
        state = ForceBattle(state, map, schedule);
        var battle = state.Battles[0];
        Assert.True(CampaignPlayRules.TrySubmitBattleResult(
            state, PlayerOne, battle.Id, null, true, schedule.StartsUtc.AddMinutes(7), out var oneResult, out _));
        Assert.True(CampaignPlayRules.TryAcceptBattleResult(
            oneResult!.State, PlayerTwo, battle.Id, schedule.StartsUtc.AddMinutes(7), out var accepted, out _));
        Assert.Equal(BattleStatus.Finalized, accepted!.State.Battles[0].Status);
        Assert.True(accepted.State.Battles[0].IsDraw);
        Assert.Equal(PhaseWindowStatus.Open, accepted.State.Windows[1].Status);
        Assert.True(accepted.State.Forces.All(force => force.InBattle));
        Assert.Contains(accepted.State.Log, item => item.Kind == PlayLogKind.BattleFinalized);

        Assert.True(CampaignPlayRules.TrySubmitRetreat(
            accepted.State,
            accepted.PreserveMap ? map : accepted.Map,
            PlayerOne,
            battle.Id,
            NorthSpawn,
            schedule.StartsUtc.AddMinutes(7),
            out var afterNorth,
            out _));
        Assert.True(CampaignPlayRules.TrySubmitRetreat(
            afterNorth!.State,
            afterNorth.PreserveMap ? map : afterNorth.Map,
            PlayerTwo,
            battle.Id,
            SouthSpawn,
            schedule.StartsUtc.AddMinutes(7),
            out var retreated,
            out _));
        Assert.Equal(PhaseWindowStatus.Resolved, retreated!.State.Windows[1].Status);
        Assert.DoesNotContain(retreated.State.Forces, force => force.InBattle);
    }

    [Fact]
    public void ConflictingResultsBecomeDisputed()
    {
        var (state, map, schedule) = Seeded();
        state = ForceBattle(state, map, schedule);
        var battle = state.Battles[0];
        var northForce = state.Forces.Single(force => force.FactionId == North);
        var southForce = state.Forces.Single(force => force.FactionId == South);
        Assert.True(CampaignPlayRules.TrySubmitBattleResult(
            state, PlayerOne, battle.Id, northForce.Id, false, schedule.StartsUtc.AddMinutes(7), out var one, out _));
        Assert.True(CampaignPlayRules.TrySubmitBattleResult(
            one!.State, PlayerTwo, battle.Id, southForce.Id, false, schedule.StartsUtc.AddMinutes(7), out var two, out _));
        Assert.Equal(BattleStatus.Disputed, two!.State.Battles[0].Status);
        Assert.Contains(two.State.Log, item => item.Kind == PlayLogKind.BattleDisputed);
        Assert.Contains(Guid.Empty, two.NotifyManagerUserIds);
    }

    [Fact]
    public void DefaultRetreatUsesSpawnAndBattleCanEndEarly()
    {
        var (state, map, schedule) = Seeded();
        state = ForceBattle(state, map, schedule);
        var battle = state.Battles[0];
        var northForce = state.Forces.Single(force => force.FactionId == North);
        Assert.True(CampaignPlayRules.TrySubmitBattleResult(
            state, PlayerOne, battle.Id, northForce.Id, false, schedule.StartsUtc.AddMinutes(7), out var one, out _));
        Assert.True(CampaignPlayRules.TryAcceptBattleResult(
            one!.State, PlayerTwo, battle.Id, schedule.StartsUtc.AddMinutes(7), out var accepted, out _));
        Assert.True(CampaignPlayRules.TrySubmitRetreat(
            accepted!.State,
            accepted.PreserveMap ? map : accepted.Map,
            PlayerTwo,
            battle.Id,
            SouthSpawn,
            schedule.StartsUtc.AddMinutes(7),
            out var retreated,
            out _));

        Assert.Equal(PhaseWindowStatus.Resolved, retreated!.State.Windows[1].Status);
        Assert.Equal(SouthSpawn, retreated.State.Forces.Single(force => force.FactionId == South).TerritoryId);
        Assert.DoesNotContain(retreated.State.Forces, force => force.InBattle);
        Assert.Contains(retreated.State.Log, item => item.Kind == PlayLogKind.PlayerRetreat);
        Assert.Contains(retreated.State.Log, item => item.Kind == PlayLogKind.BattleFinalized);
    }

    [Fact]
    public void DebugSessionIsLoggedAndExclusive()
    {
        var (state, _, schedule) = Seeded();
        var now = schedule.StartsUtc.AddMinutes(1);
        Assert.True(CampaignPlayRules.TryEnterDebug(state, PlayerOne, now, out var entered, out _));
        Assert.Equal(PlayerOne, entered!.DebugActorUserId);
        Assert.Contains(entered.Log, item => item.Kind == PlayLogKind.DebugEntered && item.ActorUserId == PlayerOne);
        Assert.True(CampaignPlayRules.TryEnterDebug(entered, PlayerOne, now, out var again, out _));
        Assert.Equal(entered.Log.Count, again!.Log.Count);
        Assert.False(CampaignPlayRules.TryEnterDebug(entered, PlayerTwo, now, out _, out var busy));
        Assert.Equal("debug.busy", busy!.Code);
        Assert.True(CampaignPlayRules.TryExitDebug(entered, PlayerTwo, now, out var exited, out _));
        Assert.Null(exited!.DebugActorUserId);
        Assert.Contains(exited.Log, item => item.Kind == PlayLogKind.DebugExited && item.ActorUserId == PlayerTwo);
    }

    [Fact]
    public void DebugCorrectsOpenWindowDraftWithoutRevealingTheAction()
    {
        var (state, map, schedule) = Seeded();
        var northForce = state.Forces.Single(force => force.FactionId == North);
        var now = schedule.StartsUtc.AddMinutes(1);
        Assert.True(CampaignPlayRules.TryEnterDebug(state, PlayerOne, now, out state, out _));
        Assert.True(CampaignPlayRules.TryDebugCorrectOrder(
            state!,
            PlayerOne,
            northForce.Id,
            ActionKind.Move,
            Midland,
            null,
            map,
            AllyGroups(),
            null,
            now,
            out var outcome,
            out _));

        Assert.NotNull(outcome);
        Assert.Contains(
            outcome.State.Drafts,
            item => item.ForceId == northForce.Id && item.Kind == ActionKind.Move && item.TargetTerritoryId == Midland);
        var correction = outcome.State.Log.Single(item => item.Kind == PlayLogKind.DebugOrderCorrected);
        Assert.Null(correction.ActionKind);
        Assert.DoesNotContain(outcome.State.Log, item => item.Kind == PlayLogKind.DebugActionReresolved);
    }

    [Fact]
    public void DebugReresolvesTheLastActionWhileTheFollowingPhaseIsOpen()
    {
        var (state, map, schedule) = Seeded();
        state = ForceBattle(state, map, schedule);
        var southForce = state.Forces.Single(force => force.FactionId == South);
        var now = schedule.StartsUtc.AddMinutes(7);
        Assert.True(CampaignPlayRules.TryEnterDebug(state, PlayerOne, now, out state, out _));
        Assert.True(CampaignPlayRules.TryDebugCorrectOrder(
            state!,
            PlayerOne,
            southForce.Id,
            ActionKind.Hold,
            null,
            null,
            map,
            AllyGroups(),
            null,
            now,
            out var outcome,
            out _));

        Assert.NotNull(outcome);
        Assert.Contains(outcome.State.Log, item => item.Kind == PlayLogKind.DebugOrderCorrected && item.ActionKind == ActionKind.Hold);
        Assert.Contains(outcome.State.Log, item => item.Kind == PlayLogKind.DebugActionReresolved);
        Assert.Contains(
            outcome.State.Submissions,
            item => item.ForceId == southForce.Id && item.Kind == ActionKind.Hold && item.Source == OrderSource.StaffCorrection);
        Assert.Empty(outcome.State.Battles);
        Assert.Equal(SouthSpawn, outcome.State.Forces.Single(force => force.FactionId == South).TerritoryId);
        Assert.Equal(Midland, outcome.State.Forces.Single(force => force.FactionId == North).TerritoryId);
    }

    [Fact]
    public void BattleOverrideRequiresTheActiveDebugActor()
    {
        var (state, map, schedule) = Seeded();
        state = ForceBattle(state, map, schedule);
        var battle = state.Battles[0];
        var northForce = state.Forces.Single(force => force.FactionId == North);
        var now = schedule.StartsUtc.AddMinutes(7);
        Assert.False(CampaignPlayRules.TryResolveBattle(
            state, PlayerOne, battle.Id, northForce.Id, false, now, out _, out var required));
        Assert.Equal("debug.required", required!.Code);
        Assert.True(CampaignPlayRules.TryEnterDebug(state, PlayerOne, now, out state, out _));
        Assert.False(CampaignPlayRules.TryResolveBattle(
            state!, PlayerTwo, battle.Id, northForce.Id, false, now, out _, out var other));
        Assert.Equal("debug.other_actor", other!.Code);
        Assert.True(CampaignPlayRules.TryResolveBattle(
            state!, PlayerOne, battle.Id, northForce.Id, false, now, out var resolved, out _));
        Assert.Equal(BattleStatus.GMResolved, resolved!.Battles[0].Status);
        Assert.Equal(northForce.Id, resolved.Battles[0].WinnerForceId);
        _ = map;
    }

    [Fact]
    public void SurrenderInOneVersusOneAwardsMaxVictoryBattlePointsWithNoBonus()
    {
        var (state, map, schedule) = Seeded();
        state = ForceBattle(state, map, schedule);
        var battle = state.Battles[0];
        var northForce = state.Forces.Single(force => force.FactionId == North);
        var southForce = state.Forces.Single(force => force.FactionId == South);
        var now = schedule.StartsUtc.AddMinutes(7);

        Assert.True(CampaignPlayRules.TrySubmitSurrender(
            state,
            map,
            PlayerOne,
            battle.Id,
            NorthSpawn,
            now,
            out var outcome,
            out _));

        var resolved = outcome!.State.Battles.Single(item => item.Id == battle.Id);
        Assert.Equal(BattleStatus.Finalized, resolved.Status);
        Assert.Equal(southForce.Id, resolved.WinnerForceId);
        Assert.Equal(10, resolved.WinnerScore);
        Assert.Equal(0, resolved.LoserScore);
        Assert.False(resolved.IsNoContest);
        Assert.Contains(outcome.State.Log, item => item.Kind == PlayLogKind.PlayerSurrendered);
        Assert.Contains(northForce.Id, resolved.SurrenderedForceIds);
        _ = northForce;
    }

    [Fact]
    public void CommittedSurrenderCannotBeUncommitted()
    {
        var (state, map, schedule) = Seeded();
        var northForce = state.Forces.Single(force => force.FactionId == North);
        var southForce = state.Forces.Single(force => force.FactionId == South);
        var now = schedule.StartsUtc.AddMinutes(1);
        var extra = new CampaignForce(Guid.NewGuid(), PlayerOne, North, NorthSpawn, false);
        var southExtra = new CampaignForce(Guid.NewGuid(), PlayerTwo, South, SouthSpawn, false);
        var battle = new CampaignBattle(
            Guid.NewGuid(),
            Midland,
            state.Windows[0].Id,
            state.Windows[1].Id,
            BattleStatus.AwaitingResults,
            [northForce.Id, southForce.Id],
            winnerForceId: null,
            isDraw: false,
            now);
        state = state.With(
            forces:
            [
                northForce.With(territoryId: Midland, inBattle: true),
                extra,
                southForce.With(territoryId: Midland, inBattle: true),
                southExtra,
            ],
            battles: [battle]);

        Assert.True(CampaignPlayRules.TrySaveDraft(
            state,
            PlayerOne,
            extra.Id,
            ActionKind.Hold,
            null,
            null,
            map,
            now,
            out state,
            out _));
        Assert.True(CampaignPlayRules.TrySaveDraft(
            state!,
            PlayerOne,
            northForce.Id,
            ActionKind.Surrender,
            NorthSpawn,
            null,
            map,
            now,
            out state,
            out _));
        Assert.True(CampaignPlayRules.TryCommit(state!, map, PlayerOne, AllyGroups(), now, out var committed, out _));
        Assert.False(CampaignPlayRules.TryUncommit(committed!.State, PlayerOne, now, out _, out var error));
        Assert.Equal("order.surrender.locked", error!.Code);
    }

    [Fact]
    public void ThreeOpposingSidesPairTheTwoStrongestFirst()
    {
        var east = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var eastSpawn = Guid.Parse("44444444-4444-4444-4444-444444444440");
        var playerThree = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var (state, map, schedule) = Seeded();
        _ = schedule;
        var northForce = state.Forces.Single(force => force.FactionId == North);
        var southForce = state.Forces.Single(force => force.FactionId == South);
        var eastForce = new CampaignForce(Guid.NewGuid(), playerThree, east, Midland, true);
        var now = schedule.StartsUtc.AddMinutes(7);
        var battle = new CampaignBattle(
            Guid.NewGuid(),
            Midland,
            state.Windows[0].Id,
            state.Windows[1].Id,
            BattleStatus.AwaitingResults,
            [northForce.Id, southForce.Id, eastForce.Id],
            winnerForceId: null,
            isDraw: false,
            now);
        var ownedMap = new PlayMap(
            [
                new PlayTerritory(NorthSpawn, 1, North, North, null, null, StructureCondition.Operational),
                new PlayTerritory(Midland, 2, North, null, null, null, StructureCondition.Operational),
                new PlayTerritory(SouthSpawn, 3, South, South, null, null, StructureCondition.Operational),
                new PlayTerritory(eastSpawn, 4, east, east, null, null, StructureCondition.Operational),
            ],
            [(NorthSpawn, Midland), (Midland, SouthSpawn), (NorthSpawn, SouthSpawn), (Midland, eastSpawn)]);
        _ = map;
        state = state.With(
            forces:
            [
                northForce.With(territoryId: Midland, inBattle: true),
                southForce.With(territoryId: Midland, inBattle: true),
                eastForce,
            ],
            battles: [battle],
            windows: state.Windows.Select(window =>
                window.Kind == RoundPhaseKind.Battle
                    ? window.With(status: PhaseWindowStatus.Open)
                    : window.With(status: PhaseWindowStatus.Resolved)).ToArray());

        var ranked = CombatantStrengthRules.Rank(
            state.Forces,
            force => new CombatantStrengthRules.Strength(
                force.FactionId == North ? 3 : 1,
                force.FactionId == North ? 2 : 1,
                0,
                0),
            static _ => 0);
        Assert.Equal(North, ranked[0].FactionId);

        var active = BattleMatchRules.NextActiveForceIds(
            state.Forces,
            new Dictionary<Guid, string?> { [North] = null, [South] = null, [east] = null },
            [],
            force => new CombatantStrengthRules.Strength(force.FactionId == North ? 5 : force.FactionId == South ? 3 : 1, 1, 0, 0),
            static _ => 0);
        Assert.Equal(2, active.Count);
        Assert.Contains(northForce.Id, active);
        Assert.Contains(southForce.Id, active);
        Assert.DoesNotContain(eastForce.Id, active);
        _ = ownedMap;
        _ = now;
    }

    [Fact]
    public void AgreedResultWithWaitingForcesStartsTheNextPairing()
    {
        var east = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var playerThree = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var (state, map, schedule) = Seeded();
        state = ForceBattle(state, map, schedule);
        var northForce = state.Forces.Single(force => force.FactionId == North);
        var southForce = state.Forces.Single(force => force.FactionId == South);
        var eastForce = new CampaignForce(Guid.NewGuid(), playerThree, east, Midland, true);
        var battle = state.Battles[0].With(
            participantForceIds: [northForce.Id, southForce.Id, eastForce.Id],
            activeForceIds: [northForce.Id, southForce.Id],
            waitingForceIds: [eastForce.Id]);
        state = state.With(
            forces:
            [
                northForce.With(territoryId: Midland, inBattle: true),
                southForce.With(territoryId: Midland, inBattle: true),
                eastForce,
            ],
            battles: [battle]);
        var now = schedule.StartsUtc.AddMinutes(7);
        var groups = new Dictionary<Guid, string?>
        {
            [North] = null,
            [South] = null,
            [east] = null,
        };

        Assert.True(CampaignPlayRules.TrySubmitBattleResult(
            state,
            PlayerOne,
            battle.Id,
            northForce.Id,
            false,
            now,
            out var oneResult,
            out _,
            map: map,
            factionAllyGroups: groups,
            pickIndex: static _ => 0));
        Assert.True(CampaignPlayRules.TryAcceptBattleResult(
            oneResult!.State,
            PlayerTwo,
            battle.Id,
            now,
            out var accepted,
            out _,
            map: map,
            factionAllyGroups: groups,
            pickIndex: static _ => 0));

        Assert.Equal(BattleStatus.Finalized, accepted!.State.Battles.Single(item => item.Id == battle.Id).Status);
        Assert.Contains(accepted.State.Log, item => item.Kind == PlayLogKind.BattleMatchAdvanced);
        var followUp = accepted.State.Battles.Single(item => item.Id != battle.Id);
        Assert.Contains(northForce.Id, followUp.ParticipantForceIds);
        Assert.Contains(eastForce.Id, followUp.ParticipantForceIds);
        Assert.DoesNotContain(southForce.Id, followUp.ParticipantForceIds);
        Assert.Equal(BattleStatus.AwaitingResults, followUp.Status);
        Assert.True(accepted.State.Forces.Single(force => force.Id == northForce.Id).InBattle);
        Assert.True(accepted.State.Forces.Single(force => force.Id == eastForce.Id).InBattle);
    }

    [Fact]
    public void CannotReduceRoundsBelowCurrentRound()
    {
        var (state, map, schedule) = Seeded();
        _ = map;
        Assert.False(CampaignPlayRules.TryExtendSchedule(
            state,
            schedule,
            roundCount: 1,
            [],
            schedule.StartsUtc,
            PlayerOne,
            out _,
            out var error));
        Assert.Equal("roundCount.invalid", error!.Code);
    }

    [Fact]
    public void ExtendingRoundsIsRecordedInTheLog()
    {
        var (state, map, schedule) = Seeded();
        _ = map;
        Assert.True(CampaignPlayRules.TryExtendSchedule(
            state,
            schedule,
            roundCount: 4,
            [],
            schedule.StartsUtc,
            PlayerOne,
            out var outcome,
            out _));
        Assert.Contains(outcome!.State.Log, item => item.Kind == PlayLogKind.ScheduleExtended && item.ActorUserId == PlayerOne);
        Assert.Equal(4, outcome.RoundCount);
    }

    [Fact]
    public void DraftRejectsPillageRepairBackstabAndRetreatWhenTheyAreNotLegal()
    {
        var (state, map, schedule) = Seeded();
        var northForce = state.Forces.Single(force => force.FactionId == North);
        var now = schedule.StartsUtc;

        Assert.False(CampaignPlayRules.TrySaveDraft(
            state, PlayerOne, northForce.Id, ActionKind.Pillage, null, null, map, now, out _, out var pillageError));
        Assert.Equal("order.pillage.invalid", pillageError!.Code);

        Assert.False(CampaignPlayRules.TrySaveDraft(
            state, PlayerOne, northForce.Id, ActionKind.Repair, null, null, map, now, out _, out var repairError));
        Assert.Equal("order.repair.invalid", repairError!.Code);

        Assert.False(CampaignPlayRules.TrySaveDraft(
            state,
            PlayerOne,
            northForce.Id,
            ActionKind.Backstab,
            null,
            null,
            map,
            AllyGroups(),
            null,
            now,
            out _,
            out var backstabError));
        Assert.Equal("order.backstab.invalid", backstabError!.Code);

        Assert.False(CampaignPlayRules.TrySaveDraft(
            state, PlayerOne, northForce.Id, ActionKind.Retreat, NorthSpawn, null, map, now, out _, out var retreatError));
        Assert.Equal("order.kind.invalid", retreatError!.Code);

        Assert.False(CampaignPlayRules.TrySaveDraft(
            state, PlayerOne, northForce.Id, ActionKind.Build, null, null, map, now, out _, out var buildError));
        Assert.Equal("order.structure.required", buildError!.Code);
    }

    [Fact]
    public void DraftAcceptsPillageOfAnUnownedStructure()
    {
        var (state, map, schedule) = Seeded();
        var northForce = state.Forces.Single(force => force.FactionId == North);
        var relocated = northForce.With(territoryId: Midland);
        state = state.With(forces: [relocated, .. state.Forces.Where(force => force.Id != northForce.Id)]);
        map = map.Replace(map.Territory(Midland)!.With(structureTypeId: Guid.NewGuid(), structureName: "Town"));
        Assert.True(CampaignPlayRules.TrySaveDraft(
            state,
            PlayerOne,
            relocated.Id,
            ActionKind.Pillage,
            null,
            null,
            map,
            schedule.StartsUtc,
            out var drafted,
            out _));
        Assert.Equal(ActionKind.Pillage, drafted!.Drafts.Single(item => item.ForceId == relocated.Id).Kind);
    }

    private static CampaignPlayState ForceBattle(CampaignPlayState state, PlayMap map, CampaignSchedule schedule)
    {
        var northForce = state.Forces.Single(force => force.FactionId == North);
        var southForce = state.Forces.Single(force => force.FactionId == South);
        Assert.True(CampaignPlayRules.TrySaveDraft(
            state, PlayerOne, northForce.Id, ActionKind.Move, Midland, null, map, schedule.StartsUtc, out var northDraft, out _));
        Assert.True(CampaignPlayRules.TryCommit(northDraft!, map, PlayerOne, AllyGroups(), schedule.StartsUtc, out var afterNorth, out _));
        state = afterNorth!.State;
        Assert.True(CampaignPlayRules.TrySaveDraft(
            state, PlayerTwo, southForce.Id, ActionKind.Move, Midland, null, map, schedule.StartsUtc, out var southDraft, out _));
        Assert.True(CampaignPlayRules.TryCommit(southDraft!, map, PlayerTwo, AllyGroups(), schedule.StartsUtc, out var closed, out _));
        return closed!.State;
    }

    [Fact]
    public void RemoveControllerDropsForcesDraftsAndOpenBattles()
    {
        var (state, map, schedule) = Seeded();
        var northForce = state.Forces.Single(force => force.FactionId == North);
        Assert.True(CampaignPlayRules.TrySaveDraft(
            state,
            PlayerOne,
            northForce.Id,
            ActionKind.Hold,
            null,
            null,
            map,
            schedule.StartsUtc,
            out state,
            out _));
        state = ForceBattle(state, map, schedule);
        northForce = state.Forces.Single(force => force.FactionId == North);
        Assert.Contains(
            state.Battles,
            battle => battle.Status == BattleStatus.AwaitingResults && battle.ParticipantForceIds.Contains(northForce.Id));

        var next = CampaignPlayRules.RemoveController(state, PlayerOne, schedule.StartsUtc);

        Assert.DoesNotContain(next.Forces, force => force.ControllerUserId == PlayerOne);
        Assert.Contains(next.Forces, force => force.ControllerUserId == PlayerTwo);
        Assert.DoesNotContain(next.Drafts, draft => draft.ForceId == northForce.Id);
        Assert.DoesNotContain(next.Commitments, commitment => commitment.UserId == PlayerOne);
        Assert.DoesNotContain(
            next.Battles,
            battle => battle.ParticipantForceIds.Contains(northForce.Id) && battle.Status == BattleStatus.AwaitingResults);
    }

    [Fact]
    public void ReassignControllerFactionKeepsTerritory()
    {
        var (state, _, _) = Seeded();
        var origin = state.Forces.Single(force => force.ControllerUserId == PlayerOne);

        var next = CampaignPlayRules.ReassignControllerFaction(state, PlayerOne, South);
        var force = next.Forces.Single(item => item.ControllerUserId == PlayerOne);

        Assert.Equal(South, force.FactionId);
        Assert.Equal(origin.Id, force.Id);
        Assert.Equal(origin.TerritoryId, force.TerritoryId);
        Assert.Equal(South, next.Forces.Single(item => item.ControllerUserId == PlayerTwo).FactionId);
    }

    [Fact]
    public void HoldAppliesWellRestedFromConfiguredStatuses()
    {
        var (state, map, schedule) = Seeded();
        var catalog = ForceStatusCatalog.Standard
            .Select(status => new ForceStatusSetup(
                Guid.NewGuid(),
                status.Name,
                status.Effects,
                status.EnableTrigger,
                status.ClearTrigger))
            .ToArray();
        var advanced = CampaignPlayRules.Advance(state, map, schedule, AllyGroups(), state.Windows[0].EndsUtc, catalog);
        Assert.All(advanced.State.Forces, force => Assert.Equal("Well Rested", force.StatusName));
    }

    private static (CampaignPlayState State, PlayMap Map, CampaignSchedule Schedule) Seeded()
    {
        var schedule = CreateSchedule();
        var map = CreateMap(ownerMidland: null);
        var seeded = CampaignPlayRules.Seed(
            CampaignPlayState.Empty,
            map,
            schedule,
            [new PlayerFactionAssignment(PlayerOne, North), new PlayerFactionAssignment(PlayerTwo, South)],
            schedule.StartsUtc);
        return (seeded.State, seeded.Map, schedule);
    }

    private static Dictionary<Guid, string?> AllyGroups()
    {
        return new Dictionary<Guid, string?>
        {
            [North] = null,
            [South] = null,
        };
    }

    private static PlayMap CreateMap(Guid? ownerMidland)
    {
        var territories = new[]
        {
            new PlayTerritory(NorthSpawn, 1, North, North, null, null, StructureCondition.Operational),
            new PlayTerritory(Midland, 2, ownerMidland, null, null, null, StructureCondition.Operational),
            new PlayTerritory(SouthSpawn, 3, South, South, null, null, StructureCondition.Operational),
        };
        return new PlayMap(
            territories,
            [(NorthSpawn, Midland), (Midland, SouthSpawn), (NorthSpawn, SouthSpawn)]);
    }

    private static CampaignSchedule CreateSchedule()
    {
        var succeeded = CampaignSetupRules.TryCreate(
            "Border War",
            null,
            8,
            false,
            null,
            false,
            true,
            0,
            [new FactionInput { Name = "North" }, new FactionInput { Name = "South" }],
            null,
            null,
            new CampaignScheduleInput
            {
                TimeZoneId = "UTC",
                StartsAtLocal = "2026-09-01T12:00",
                RoundCount = 3,
                RoundLengthAmount = 10,
                RoundLengthUnit = "Minutes",
                Phases =
                [
                    new RoundPhaseInput { Kind = "Action", DurationAmount = 6, DurationUnit = "Minutes" },
                    new RoundPhaseInput { Kind = "Battle", DurationAmount = 4, DurationUnit = "Minutes" },
                ],
            },
            out var setup,
            out _,
            out var errors);
        Assert.True(succeeded, string.Join('\n', errors.Select(error => error.Message)));
        Assert.NotNull(setup);
        return setup.Schedule;
    }
}
