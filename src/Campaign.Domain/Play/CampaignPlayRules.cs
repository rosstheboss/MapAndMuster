using System.Diagnostics.CodeAnalysis;
using Campaign.Domain.Campaigns;
using Campaign.Domain.Common;

namespace Campaign.Domain.Play;

/// <summary>
/// Seeds, advances, and mutates launched-campaign play state.
/// </summary>
public static class CampaignPlayRules
{
    /// <summary>
    /// Materializes windows, spawn flags, and starting forces when the campaign is in progress.
    /// </summary>
    public static PlayOutcome Seed(
        CampaignPlayState state,
        PlayMap map,
        CampaignSchedule schedule,
        IReadOnlyList<PlayerFactionAssignment> players,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(players);

        var seededMap = ApplySpawnFlags(map);
        if (state.Windows.Count > 0)
        {
            return new PlayOutcome(state, seededMap, schedule.EndsUtc, schedule.RoundCount);
        }

        if (utcNow < schedule.StartsUtc)
        {
            return new PlayOutcome(state, seededMap, schedule.EndsUtc, schedule.RoundCount);
        }

        var windows = MaterializeWindows(schedule);
        if (windows.Count > 0 && utcNow >= windows[0].StartsUtc)
        {
            windows[0] = windows[0].With(status: PhaseWindowStatus.Open);
        }

        var forces = new List<CampaignForce>();
        foreach (var player in players.Where(static item => item.FactionId.HasValue).OrderBy(static item => item.UserId))
        {
            var spawn = seededMap.SpawnFor(player.FactionId!.Value);
            if (spawn is null)
            {
                continue;
            }

            forces.Add(new CampaignForce(Guid.NewGuid(), player.UserId, player.FactionId.Value, spawn.Id, false));
        }

        var started = new CampaignPlayState(
            windows,
            forces,
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            CaptureStructures(seededMap),
            state.Log)
            .AppendLog(new PlayLogEntry(
                Guid.NewGuid(),
                utcNow,
                PlayLogKind.CampaignStarted,
                windows.Count == 0 ? null : windows[0].Id,
                null,
                null,
                null,
                null,
                null,
                null,
                []));
        return new PlayOutcome(started, seededMap, schedule.EndsUtc, schedule.RoundCount);
    }

    /// <summary>
    /// Adds a starting force when a player chooses a faction that has a spawn.
    /// </summary>
    public static PlayOutcome EnsureForce(
        CampaignPlayState state,
        PlayMap map,
        Guid userId,
        Guid factionId)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(map);
        if (state.Forces.Any(force => force.ControllerUserId == userId))
        {
            return new PlayOutcome(state, map, default, 0, preserveSchedule: true);
        }

        var spawn = map.SpawnFor(factionId);
        if (spawn is null)
        {
            return new PlayOutcome(state, ApplySpawnFlags(map), default, 0, preserveSchedule: true);
        }

        var forces = state.Forces.Append(new CampaignForce(Guid.NewGuid(), userId, factionId, spawn.Id, false)).ToArray();
        return new PlayOutcome(state.With(forces: forces), ApplySpawnFlags(map), default, 0, preserveSchedule: true);
    }

    /// <summary>
    /// Closes overdue windows, resolves actions, and opens the next window.
    /// </summary>
    public static PlayOutcome Advance(
        CampaignPlayState state,
        PlayMap map,
        CampaignSchedule schedule,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(factionAllyGroups);

        var current = state.CurrentWindow();
        if (current is null)
        {
            return new PlayOutcome(state, map, schedule.EndsUtc, schedule.RoundCount);
        }

        var nextState = state;
        var nextMap = map;
        if (current.Status == PhaseWindowStatus.Pending && utcNow >= current.StartsUtc)
        {
            nextState = OpenWindow(nextState, current.Id, utcNow);
            current = nextState.Windows.First(window => window.Id == current.Id);
        }

        if (current.Status == PhaseWindowStatus.Open && current.Kind == RoundPhaseKind.Action)
        {
            var required = nextState.RequiredOrderPlayers(current.Id);
            var allCommitted = required.Count > 0
                && required.All(userId => nextState.Commitments.Any(item => item.WindowId == current.Id && item.UserId == userId));
            var due = utcNow >= current.EndsUtc;
            if (allCommitted || due)
            {
                var closeAt = due ? current.EndsUtc : utcNow;
                (nextState, nextMap) = CloseActionWindow(nextState, nextMap, current, factionAllyGroups, closeAt, due);
            }
        }
        else if (current.Status == PhaseWindowStatus.Open && current.Kind == RoundPhaseKind.Battle)
        {
            if (BattlePhaseComplete(nextState, current) || utcNow >= current.EndsUtc)
            {
                var closeAt = utcNow >= current.EndsUtc ? current.EndsUtc : utcNow;
                var due = utcNow >= current.EndsUtc;
                (nextState, nextMap) = CloseBattleWindow(nextState, nextMap, current, closeAt, due);
            }
        }

        return new PlayOutcome(nextState, nextMap, LastEnd(nextState, schedule.EndsUtc), nextState.Windows.Count == 0
            ? schedule.RoundCount
            : nextState.Windows.Max(static window => window.RoundNumber));
    }

    /// <summary>
    /// Saves a draft while the action window is open and the player is uncommitted.
    /// </summary>
    public static bool TrySaveDraft(
        CampaignPlayState state,
        Guid userId,
        Guid forceId,
        ActionKind kind,
        Guid? targetTerritoryId,
        Guid? structureTypeId,
        PlayMap map,
        DateTimeOffset utcNow,
        [NotNullWhen(true)] out CampaignPlayState? next,
        [NotNullWhen(false)] out DomainError? error)
    {
        return TrySaveDraft(
            state,
            userId,
            forceId,
            kind,
            targetTerritoryId,
            structureTypeId,
            map,
            new Dictionary<Guid, string?>(),
            knownStructureTypeIds: null,
            utcNow,
            out next,
            out error);
    }

    /// <summary>
    /// Saves a draft while the action window is open and the player is uncommitted.
    /// </summary>
    public static bool TrySaveDraft(
        CampaignPlayState state,
        Guid userId,
        Guid forceId,
        ActionKind kind,
        Guid? targetTerritoryId,
        Guid? structureTypeId,
        PlayMap map,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlySet<Guid>? knownStructureTypeIds,
        DateTimeOffset utcNow,
        [NotNullWhen(true)] out CampaignPlayState? next,
        [NotNullWhen(false)] out DomainError? error)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(factionAllyGroups);
        next = null;
        error = null;
        if (!TryOpenAction(state, userId, forceId, utcNow, requireUncommitted: true, out var window, out var force, out error))
        {
            return false;
        }

        if (kind == ActionKind.Battle)
        {
            error = new DomainError("order.kind.invalid", "Players cannot submit the Battle action directly.", "kind");
            return false;
        }

        if (kind == ActionKind.Retreat)
        {
            error = new DomainError("order.kind.invalid", "Retreat is submitted after a battle, not during an action window.", "kind");
            return false;
        }

        if (kind is ActionKind.Move or ActionKind.Split or ActionKind.Retreat && targetTerritoryId is null)
        {
            error = new DomainError("order.target.required", "Choose a destination territory.", "targetTerritoryId");
            return false;
        }

        if (kind is ActionKind.Move or ActionKind.Split
            && (targetTerritoryId is null || !map.AreAdjacent(force.TerritoryId, targetTerritoryId.Value)))
        {
            error = new DomainError("order.target.invalid", "That territory is not adjacent.", "targetTerritoryId");
            return false;
        }

        if (kind is ActionKind.Move or ActionKind.Split or ActionKind.Retreat
            && targetTerritoryId is { } destinationId)
        {
            var destination = map.Territory(destinationId);
            if (destination?.SpawnFactionId is { } spawnFaction && spawnFaction != force.FactionId)
            {
                error = new DomainError("order.spawn.forbidden", "A force cannot enter another faction's spawn.", "targetTerritoryId");
                return false;
            }
        }

        if (kind == ActionKind.Split && state.Forces.Count(item => item.ControllerUserId == force.ControllerUserId) >= ActionResolution.MaxForcesPerPlayer)
        {
            error = new DomainError("order.split.limit", "A player may have at most two forces.", "kind");
            return false;
        }

        if (kind == ActionKind.Build)
        {
            if (structureTypeId is null)
            {
                error = new DomainError("order.structure.required", "Choose a structure to build.", "structureTypeId");
                return false;
            }

            if (knownStructureTypeIds is { Count: > 0 } && !knownStructureTypeIds.Contains(structureTypeId.Value))
            {
                error = new DomainError("order.structure.invalid", "Choose a structure type from this campaign.", "structureTypeId");
                return false;
            }

            if (!ActionResolution.CanBuildInTerritory(map, force))
            {
                error = new DomainError("order.build.invalid", "A structure can only be built in a non-spawn territory without an intact structure.", "kind");
                return false;
            }
        }

        if (kind == ActionKind.Pillage && !ActionResolution.IsValidPillage(map, force))
        {
            error = new DomainError("order.pillage.invalid", "Pillage requires an enemy or unowned intact structure in this territory.", "kind");
            return false;
        }

        if (kind == ActionKind.Repair && !ActionResolution.IsValidRepair(map, force))
        {
            error = new DomainError("order.repair.invalid", "Repair requires a pillaged structure you control.", "kind");
            return false;
        }

        if (kind == ActionKind.Backstab && !ActionResolution.IsValidBackstab(force, factionAllyGroups, state.BrokenAllyFactionIds))
        {
            error = new DomainError("order.backstab.invalid", "Backstab requires an active alliance.", "kind");
            return false;
        }

        var draft = new OrderDraft(window.Id, force.Id, kind, targetTerritoryId, structureTypeId, utcNow);
        var drafts = state.Drafts.Where(item => !(item.WindowId == window.Id && item.ForceId == force.Id)).Append(draft).ToArray();
        next = state.With(drafts: drafts);
        return true;
    }

    /// <summary>
    /// Commits the player's current drafts. The last required commitment closes the window.
    /// </summary>
    public static bool TryCommit(
        CampaignPlayState state,
        PlayMap map,
        Guid userId,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        DateTimeOffset utcNow,
        [NotNullWhen(true)] out PlayOutcome? outcome,
        [NotNullWhen(false)] out DomainError? error)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(factionAllyGroups);
        outcome = null;
        error = null;
        var window = state.CurrentWindow();
        if (window is null || window.Status != PhaseWindowStatus.Open || window.Kind != RoundPhaseKind.Action || utcNow >= window.EndsUtc)
        {
            error = new DomainError("order.window.closed", "The action window is not open.");
            return false;
        }

        if (state.Commitments.Any(item => item.WindowId == window.Id && item.UserId == userId))
        {
            error = new DomainError("order.already_committed", "You have already committed these orders.");
            return false;
        }

        var requiredForces = state.Forces.Where(force => force.ControllerUserId == userId && !force.InBattle).ToArray();
        if (requiredForces.Length == 0)
        {
            error = new DomainError("order.not_required", "You have no orders to commit in this window.");
            return false;
        }

        var submissions = state.Submissions.ToList();
        foreach (var force in requiredForces)
        {
            var draft = state.DraftFor(window.Id, force.Id);
            var kind = draft?.Kind ?? ActionKind.Hold;
            submissions.Add(new OrderSubmission(
                Guid.NewGuid(),
                window.Id,
                force.Id,
                kind,
                draft?.TargetTerritoryId,
                draft?.StructureTypeId,
                OrderSource.Commit,
                utcNow,
                userId));
        }

        var commitments = state.Commitments.Append(new PlayerCommitment(window.Id, userId, utcNow)).ToArray();
        var next = state.With(submissions: submissions, commitments: commitments);
        var requiredPlayers = next.RequiredOrderPlayers(window.Id);
        if (requiredPlayers.Count > 0
            && requiredPlayers.All(playerId => next.Commitments.Any(item => item.WindowId == window.Id && item.UserId == playerId)))
        {
            var (closed, closedMap) = CloseActionWindow(next, map, window, factionAllyGroups, utcNow, due: false);
            outcome = new PlayOutcome(closed, closedMap, LastEnd(closed, window.EndsUtc), RoundCountOf(closed));
            return true;
        }

        outcome = new PlayOutcome(next, map, LastEnd(next, window.EndsUtc), RoundCountOf(next));
        return true;
    }

    /// <summary>
    /// Withdraws a commitment while the window remains open.
    /// </summary>
    public static bool TryUncommit(
        CampaignPlayState state,
        Guid userId,
        DateTimeOffset utcNow,
        [NotNullWhen(true)] out CampaignPlayState? next,
        [NotNullWhen(false)] out DomainError? error)
    {
        ArgumentNullException.ThrowIfNull(state);
        next = null;
        error = null;
        var window = state.CurrentWindow();
        if (window is null || window.Status != PhaseWindowStatus.Open || window.Kind != RoundPhaseKind.Action || utcNow >= window.EndsUtc)
        {
            error = new DomainError("order.window.closed", "You can uncommit only while the action window is open.");
            return false;
        }

        if (!state.Commitments.Any(item => item.WindowId == window.Id && item.UserId == userId))
        {
            error = new DomainError("order.not_committed", "You have not committed orders in this window.");
            return false;
        }

        var commitments = state.Commitments.Where(item => !(item.WindowId == window.Id && item.UserId == userId)).ToArray();
        next = state.With(commitments: commitments);
        return true;
    }

    /// <summary>
    /// Records a battle result from a participant.
    /// </summary>
    public static bool TrySubmitBattleResult(
        CampaignPlayState state,
        Guid userId,
        Guid battleId,
        Guid? winnerForceId,
        bool isDraw,
        DateTimeOffset utcNow,
        [NotNullWhen(true)] out PlayOutcome? outcome,
        [NotNullWhen(false)] out DomainError? error)
    {
        ArgumentNullException.ThrowIfNull(state);
        outcome = null;
        if (!TryOpenBattle(state, userId, battleId, utcNow, out var battle, out error))
        {
            return false;
        }

        if (isDraw && winnerForceId is not null)
        {
            error = new DomainError("battle.result.invalid", "A draw cannot name a winner.", "winnerForceId");
            return false;
        }

        if (!isDraw && (winnerForceId is null || !battle.ParticipantForceIds.Contains(winnerForceId.Value)))
        {
            error = new DomainError("battle.result.invalid", "Choose a participating force as the winner.", "winnerForceId");
            return false;
        }

        var submission = new BattleResultSubmission(Guid.NewGuid(), battle.Id, userId, winnerForceId, isDraw, null, utcNow);
        var next = AppendBattleSubmission(state, battle, submission, utcNow, notifyManagers: out var notify);
        outcome = BattleMutationOutcome(next, utcNow, notify);
        return true;
    }

    /// <summary>
    /// Accepts the opponent's current battle result.
    /// </summary>
    public static bool TryAcceptBattleResult(
        CampaignPlayState state,
        Guid userId,
        Guid battleId,
        DateTimeOffset utcNow,
        [NotNullWhen(true)] out PlayOutcome? outcome,
        [NotNullWhen(false)] out DomainError? error)
    {
        ArgumentNullException.ThrowIfNull(state);
        outcome = null;
        if (!TryOpenBattle(state, userId, battleId, utcNow, out var battle, out error))
        {
            return false;
        }

        var opponent = battle.ParticipantForceIds
            .Select(id => state.Forces.FirstOrDefault(force => force.Id == id))
            .OfType<CampaignForce>()
            .FirstOrDefault(force => force.ControllerUserId != userId);
        if (opponent is null)
        {
            error = new DomainError("battle.accept.invalid", "There is no opponent result to accept.");
            return false;
        }

        var theirs = state.LatestBattleSubmission(battle.Id, opponent.ControllerUserId);
        if (theirs is null)
        {
            error = new DomainError("battle.accept.missing", "Your opponent has not submitted a result yet.");
            return false;
        }

        var submission = new BattleResultSubmission(
            Guid.NewGuid(),
            battle.Id,
            userId,
            theirs.WinnerForceId,
            theirs.IsDraw,
            theirs.Id,
            utcNow);
        var next = AppendBattleSubmission(state, battle, submission, utcNow, out var notify);
        outcome = BattleMutationOutcome(next, utcNow, notify);
        return true;
    }

    /// <summary>
    /// Records a manager's authoritative battle result without erasing prior submissions.
    /// </summary>
    public static bool TryResolveBattle(
        CampaignPlayState state,
        Guid battleId,
        Guid? winnerForceId,
        bool isDraw,
        DateTimeOffset utcNow,
        [NotNullWhen(true)] out CampaignPlayState? next,
        [NotNullWhen(false)] out DomainError? error)
    {
        ArgumentNullException.ThrowIfNull(state);
        next = null;
        var battle = state.Battles.FirstOrDefault(item => item.Id == battleId);
        if (battle is null)
        {
            error = new DomainError("battle.not_found", "The battle was not found.");
            return false;
        }

        if (battle.Status is BattleStatus.Finalized or BattleStatus.GMResolved)
        {
            error = new DomainError("battle.already_resolved", "That battle is already resolved.");
            return false;
        }

        error = null;
        var updated = battle.With(status: BattleStatus.GMResolved, winnerForceId: winnerForceId, isDraw: isDraw);
        if (isDraw)
        {
            updated = new CampaignBattle(
                battle.Id,
                battle.TerritoryId,
                battle.SourceWindowId,
                battle.BattleWindowId,
                BattleStatus.GMResolved,
                battle.ParticipantForceIds,
                null,
                true,
                battle.CreatedUtc);
        }

        var logged = state.With(battles: ReplaceBattle(state.Battles, updated))
            .AppendLog(BattleEntry(PlayLogKind.BattleGmResolved, updated, utcNow));
        (next, _) = CloseCompletedBattlePhase(logged, MapUnchanged, utcNow);
        return true;
    }

    /// <summary>
    /// Records a retreat for a force that lost a finalized battle.
    /// </summary>
    public static bool TrySubmitRetreat(
        CampaignPlayState state,
        PlayMap map,
        Guid userId,
        Guid battleId,
        Guid targetTerritoryId,
        DateTimeOffset utcNow,
        [NotNullWhen(true)] out PlayOutcome? outcome,
        [NotNullWhen(false)] out DomainError? error)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(map);
        outcome = null;
        var battle = state.Battles.FirstOrDefault(item => item.Id == battleId);
        if (battle is null || battle.Status is not BattleStatus.Finalized and not BattleStatus.GMResolved)
        {
            error = new DomainError("retreat.not_required", "A retreat is only required after a resolved loss.");
            return false;
        }

        if (battle.IsDraw)
        {
            error = new DomainError("retreat.not_required", "A draw does not require a retreat.");
            return false;
        }

        var force = state.Forces.FirstOrDefault(item =>
            item.ControllerUserId == userId && battle.ParticipantForceIds.Contains(item.Id));
        if (force is null || force.Id == battle.WinnerForceId)
        {
            error = new DomainError("retreat.not_required", "Only a losing force submits a retreat.");
            return false;
        }

        if (state.Retreats.Any(item => item.BattleId == battle.Id && item.ForceId == force.Id))
        {
            error = new DomainError("retreat.already_submitted", "A retreat is already recorded for this force.");
            return false;
        }

        if (!IsEligibleRetreat(map, force, targetTerritoryId))
        {
            error = new DomainError("retreat.target.invalid", "Choose an adjacent eligible territory or your spawn.", "targetTerritoryId");
            return false;
        }

        error = null;
        var retreat = new RetreatOrder(Guid.NewGuid(), battle.Id, force.Id, targetTerritoryId, false, utcNow);
        var next = state.With(retreats: [.. state.Retreats, retreat]).AppendLog(new PlayLogEntry(
            Guid.NewGuid(),
            utcNow,
            PlayLogKind.PlayerRetreat,
            battle.BattleWindowId,
            force.Id,
            userId,
            battle.TerritoryId,
            targetTerritoryId,
            battle.Id,
            ActionKind.Retreat,
            [force.Id]));
        var (closed, closedMap) = CloseCompletedBattlePhase(next, map, utcNow);
        outcome = new PlayOutcome(closed, closedMap, LastEnd(closed, default), RoundCountOf(closed), preserveSchedule: true);
        return true;
    }

    /// <summary>
    /// Lengthens remaining windows and/or appends rounds. Durations cannot shrink.
    /// </summary>
    public static bool TryExtendSchedule(
        CampaignPlayState state,
        CampaignSchedule schedule,
        int roundCount,
        IReadOnlyList<PhaseExtension> extensions,
        DateTimeOffset utcNow,
        Guid actorUserId,
        [NotNullWhen(true)] out PlayOutcome? outcome,
        [NotNullWhen(false)] out DomainError? error)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(extensions);
        outcome = null;
        var currentRound = state.CurrentWindow()?.RoundNumber ?? 1;
        if (roundCount < currentRound || roundCount > CampaignSetupRules.MaxRoundCount)
        {
            error = new DomainError(
                "roundCount.invalid",
                $"Number of rounds must be between {currentRound} and {CampaignSetupRules.MaxRoundCount}.",
                "roundCount");
            return false;
        }

        if (roundCount < CampaignSetupRules.MinRoundCount && currentRound < CampaignSetupRules.MinRoundCount)
        {
            error = new DomainError(
                "roundCount.invalid",
                $"Number of rounds must be between {CampaignSetupRules.MinRoundCount} and {CampaignSetupRules.MaxRoundCount}.",
                "roundCount");
            return false;
        }

        var windows = state.Windows.ToList();
        foreach (var extension in extensions)
        {
            var index = windows.FindIndex(window => window.Id == extension.WindowId);
            if (index < 0)
            {
                error = new DomainError("schedule.window.invalid", "A phase window to extend was not found.", "windowId");
                return false;
            }

            var window = windows[index];
            if (window.Status == PhaseWindowStatus.Resolved)
            {
                error = new DomainError("schedule.window.closed", "Only the current or remaining phases can be extended.", "windowId");
                return false;
            }

            var newEnd = CampaignCalendar.Add(window.EndsUtc, schedule.TimeZone, extension.ExtraDuration);
            if (newEnd <= window.EndsUtc)
            {
                error = new DomainError("schedule.duration.invalid", "A phase can only be made longer.", "duration");
                return false;
            }

            var delta = newEnd - window.EndsUtc;
            windows[index] = window.With(endsUtc: newEnd);
            for (var later = index + 1; later < windows.Count; later++)
            {
                windows[later] = windows[later].With(
                    startsUtc: windows[later].StartsUtc + delta,
                    endsUtc: windows[later].EndsUtc + delta);
            }
        }

        error = null;
        var existingRounds = windows.Count == 0 ? 0 : windows.Max(static window => window.RoundNumber);
        if (roundCount > existingRounds)
        {
            var cursor = windows.Count == 0 ? schedule.StartsUtc : windows[^1].EndsUtc;
            for (var round = existingRounds + 1; round <= roundCount; round++)
            {
                for (var phaseIndex = 0; phaseIndex < schedule.Phases.Count; phaseIndex++)
                {
                    var phase = schedule.Phases[phaseIndex];
                    var end = CampaignCalendar.Add(cursor, schedule.TimeZone, phase.Duration);
                    windows.Add(new PhaseWindow(
                        Guid.NewGuid(),
                        round,
                        phaseIndex + 1,
                        phase.Kind,
                        phase.Duration.Amount,
                        phase.Duration.Unit,
                        cursor,
                        end,
                        PhaseWindowStatus.Pending));
                    cursor = end;
                }
            }
        }

        var next = state.With(windows: windows).AppendLog(new PlayLogEntry(
            Guid.NewGuid(),
            utcNow,
            PlayLogKind.ScheduleExtended,
            state.CurrentWindow()?.Id,
            null,
            actorUserId,
            null,
            null,
            null,
            null,
            []));
        outcome = new PlayOutcome(next, MapUnchanged, LastEnd(next, schedule.EndsUtc), roundCount)
        {
            PreserveMap = true,
        };
        return true;
    }

    /// <summary>
    /// Adjacent territories a force may Move or Split into (never another faction's spawn).
    /// </summary>
    public static IReadOnlyList<Guid> EligibleMoves(PlayMap map, CampaignForce force)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(force);
        var ids = new List<Guid>();
        foreach (var neighborId in map.Neighbors(force.TerritoryId))
        {
            var neighbor = map.Territory(neighborId);
            if (neighbor is null)
            {
                continue;
            }

            if (neighbor.SpawnFactionId is { } spawnFaction && spawnFaction != force.FactionId)
            {
                continue;
            }

            ids.Add(neighborId);
        }

        return ids;
    }

    /// <summary>
    /// Eligible retreat destinations: adjacent non-enemy-spawn territories, plus own spawn.
    /// </summary>
    public static IReadOnlyList<Guid> EligibleRetreats(PlayMap map, CampaignForce force)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(force);
        var ids = new List<Guid>();
        var spawn = map.SpawnFor(force.FactionId);
        if (spawn is not null)
        {
            ids.Add(spawn.Id);
        }

        foreach (var neighborId in map.Neighbors(force.TerritoryId))
        {
            var neighbor = map.Territory(neighborId);
            if (neighbor is null)
            {
                continue;
            }

            if (neighbor.SpawnFactionId is { } spawnFaction && spawnFaction != force.FactionId)
            {
                continue;
            }

            if (!ids.Contains(neighborId))
            {
                ids.Add(neighborId);
            }
        }

        return ids;
    }

    private static readonly PlayMap MapUnchanged = new([], []);

    private static List<PhaseWindow> MaterializeWindows(CampaignSchedule schedule)
    {
        var windows = new List<PhaseWindow>();
        var cursor = schedule.StartsUtc;
        for (var round = 1; round <= schedule.RoundCount; round++)
        {
            for (var index = 0; index < schedule.Phases.Count; index++)
            {
                var phase = schedule.Phases[index];
                var end = CampaignCalendar.Add(cursor, schedule.TimeZone, phase.Duration);
                windows.Add(new PhaseWindow(
                    Guid.NewGuid(),
                    round,
                    index + 1,
                    phase.Kind,
                    phase.Duration.Amount,
                    phase.Duration.Unit,
                    cursor,
                    end,
                    PhaseWindowStatus.Pending));
                cursor = end;
            }
        }

        return windows;
    }

    private static PlayMap ApplySpawnFlags(PlayMap map)
    {
        var changed = false;
        var next = map.Territories.Select(territory =>
        {
            if (!territory.IsSpawn || territory.OwnerFactionId == territory.SpawnFactionId)
            {
                return territory;
            }

            changed = true;
            return territory.With(ownerFactionId: territory.SpawnFactionId);
        }).ToArray();
        return changed ? map.WithTerritories(next) : map;
    }

    private static CampaignPlayState OpenWindow(CampaignPlayState state, Guid windowId, DateTimeOffset utcNow)
    {
        var windows = state.Windows
            .Select(window => window.Id == windowId ? window.With(status: PhaseWindowStatus.Open) : window)
            .ToArray();
        var opened = windows.First(window => window.Id == windowId);
        if (opened.Kind != RoundPhaseKind.Battle)
        {
            return state.With(windows: windows);
        }

        var battles = state.Battles
            .Select(battle =>
                battle.Status == BattleStatus.Pending && (battle.BattleWindowId == windowId || battle.BattleWindowId is null)
                    ? battle.With(battleWindowId: windowId, status: BattleStatus.AwaitingResults, assignWindow: true)
                    : battle)
            .ToArray();
        _ = utcNow;
        return state.With(windows: windows, battles: battles);
    }

    private static (CampaignPlayState State, PlayMap Map) CloseActionWindow(
        CampaignPlayState state,
        PlayMap map,
        PhaseWindow window,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        DateTimeOffset closeAt,
        bool due)
    {
        var submissions = state.Submissions.ToList();
        foreach (var force in state.Forces.Where(item => !item.InBattle).OrderBy(static item => item.Id))
        {
            if (state.LatestSubmission(window.Id, force.Id) is not null)
            {
                continue;
            }

            var draft = state.DraftFor(window.Id, force.Id);
            if (draft is null)
            {
                submissions.Add(new OrderSubmission(
                    Guid.NewGuid(),
                    window.Id,
                    force.Id,
                    ActionKind.Hold,
                    null,
                    null,
                    OrderSource.DeadlineHold,
                    closeAt,
                    force.ControllerUserId));
            }
            else
            {
                submissions.Add(new OrderSubmission(
                    Guid.NewGuid(),
                    window.Id,
                    force.Id,
                    draft.Kind,
                    draft.TargetTerritoryId,
                    draft.StructureTypeId,
                    OrderSource.DeadlineDraft,
                    closeAt,
                    force.ControllerUserId));
            }
        }

        var withOrders = state.With(submissions: submissions);
        var (resolved, resolvedMap) = ActionResolution.Resolve(withOrders, map, window, factionAllyGroups, closeAt);
        return FinishWindow(resolved, resolvedMap, window, closeAt, due);
    }

    private static (CampaignPlayState State, PlayMap Map) CloseBattleWindow(
        CampaignPlayState state,
        PlayMap map,
        PhaseWindow window,
        DateTimeOffset closeAt,
        bool due)
    {
        var battles = state.Battles.ToList();
        var log = new List<PlayLogEntry>();
        var notify = false;
        foreach (var battle in battles.Where(item => item.BattleWindowId == window.Id).ToArray())
        {
            if (battle.Status is BattleStatus.Finalized or BattleStatus.GMResolved)
            {
                continue;
            }

            var current = CurrentSubmissions(state, battle);
            if (current.Count == 1 && due)
            {
                var only = current[0];
                var finalized = battle.With(status: BattleStatus.Finalized, winnerForceId: only.WinnerForceId, isDraw: only.IsDraw);
                ReplaceInPlace(battles, finalized);
                log.Add(BattleEntry(PlayLogKind.BattleFinalized, finalized, closeAt));
            }
            else if (current.Count == 0 && due)
            {
                notify = true;
                log.Add(BattleEntry(PlayLogKind.UnresolvedBattleHeldOpen, battle, closeAt));
            }
        }

        var next = state.With(battles: battles).AppendLog([.. log]);
        if (!due && !BattlePhaseComplete(next, window))
        {
            return (next, map);
        }

        if (due)
        {
            next = ApplyDefaultRetreats(next, map, window, closeAt);
        }

        if (!BattlePhaseComplete(next, window) && due)
        {
            _ = notify;
            return (next, map);
        }

        next = ApplyRetreats(next, window);
        return FinishWindow(next, map, window, closeAt, due);
    }

    private static CampaignPlayState ApplyDefaultRetreats(
        CampaignPlayState state,
        PlayMap map,
        PhaseWindow window,
        DateTimeOffset utcNow)
    {
        var retreats = state.Retreats.ToList();
        var log = new List<PlayLogEntry>();
        foreach (var battle in state.Battles.Where(item => item.BattleWindowId == window.Id))
        {
            if (battle.Status is not BattleStatus.Finalized and not BattleStatus.GMResolved || battle.IsDraw)
            {
                continue;
            }

            foreach (var forceId in battle.ParticipantForceIds)
            {
                if (forceId == battle.WinnerForceId)
                {
                    continue;
                }

                if (retreats.Any(item => item.BattleId == battle.Id && item.ForceId == forceId))
                {
                    continue;
                }

                var force = state.Forces.FirstOrDefault(item => item.Id == forceId);
                if (force is null)
                {
                    continue;
                }

                var spawn = map.SpawnFor(force.FactionId);
                var target = spawn?.Id ?? force.TerritoryId;
                retreats.Add(new RetreatOrder(Guid.NewGuid(), battle.Id, force.Id, target, true, utcNow));
                log.Add(new PlayLogEntry(
                    Guid.NewGuid(),
                    utcNow,
                    PlayLogKind.DefaultRetreat,
                    window.Id,
                    force.Id,
                    force.ControllerUserId,
                    battle.TerritoryId,
                    target,
                    battle.Id,
                    ActionKind.Retreat,
                    [force.Id]));
            }
        }

        return state.With(retreats: retreats).AppendLog([.. log]);
    }

    private static CampaignPlayState ApplyRetreats(CampaignPlayState state, PhaseWindow window)
    {
        var forces = state.Forces.ToDictionary(static force => force.Id);
        foreach (var battle in state.Battles.Where(item => item.BattleWindowId == window.Id))
        {
            foreach (var forceId in battle.ParticipantForceIds)
            {
                if (!forces.TryGetValue(forceId, out var force))
                {
                    continue;
                }

                var retreat = state.Retreats.FirstOrDefault(item => item.BattleId == battle.Id && item.ForceId == forceId);
                if (retreat is not null)
                {
                    forces[forceId] = force.With(territoryId: retreat.TargetTerritoryId, inBattle: false);
                }
                else
                {
                    forces[forceId] = force.With(inBattle: false);
                }
            }
        }

        return state.With(forces: [.. forces.Values.OrderBy(static force => force.Id)]);
    }

    private static (CampaignPlayState State, PlayMap Map) FinishWindow(
        CampaignPlayState state,
        PlayMap map,
        PhaseWindow window,
        DateTimeOffset closeAt,
        bool due)
    {
        var windows = state.Windows.ToList();
        var index = windows.FindIndex(item => item.Id == window.Id);
        windows[index] = window.With(endsUtc: closeAt, status: PhaseWindowStatus.Resolved);
        if (index + 1 < windows.Count)
        {
            var next = windows[index + 1];
            windows[index + 1] = next.With(startsUtc: closeAt, status: PhaseWindowStatus.Open);
        }

        var nextState = state.With(windows: windows);
        if (index + 1 < windows.Count && windows[index + 1].Kind == RoundPhaseKind.Battle)
        {
            nextState = OpenWindow(nextState, windows[index + 1].Id, closeAt);
        }

        return CloseCompletedBattlePhase(nextState, map, closeAt);
    }

    private static (CampaignPlayState State, PlayMap Map) CloseCompletedBattlePhase(
        CampaignPlayState state,
        PlayMap map,
        DateTimeOffset closeAt)
    {
        var current = state.CurrentWindow();
        if (current is not { Kind: RoundPhaseKind.Battle, Status: PhaseWindowStatus.Open }
            || !BattlePhaseComplete(state, current))
        {
            return (state, map);
        }

        return CloseBattleWindow(state, map, current, closeAt, due: false);
    }

    private static PlayOutcome BattleMutationOutcome(
        CampaignPlayState state,
        DateTimeOffset utcNow,
        IReadOnlyList<Guid> notify)
    {
        var (closed, closedMap) = CloseCompletedBattlePhase(state, MapUnchanged, utcNow);
        return new PlayOutcome(closed, closedMap, LastEnd(closed, default), RoundCountOf(closed), NotifyManagerUserIds: notify)
        {
            PreserveMap = true,
        };
    }

    private static bool BattlePhaseComplete(CampaignPlayState state, PhaseWindow window)
    {
        var battles = state.Battles.Where(item => item.BattleWindowId == window.Id).ToArray();
        if (battles.Any(item => item.Status is not BattleStatus.Finalized and not BattleStatus.GMResolved))
        {
            return false;
        }

        foreach (var battle in battles)
        {
            if (battle.IsDraw)
            {
                continue;
            }

            foreach (var forceId in battle.ParticipantForceIds)
            {
                if (forceId == battle.WinnerForceId)
                {
                    continue;
                }

                if (!state.Retreats.Any(item => item.BattleId == battle.Id && item.ForceId == forceId))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static CampaignPlayState AppendBattleSubmission(
        CampaignPlayState state,
        CampaignBattle battle,
        BattleResultSubmission submission,
        DateTimeOffset utcNow,
        out IReadOnlyList<Guid> notifyManagers)
    {
        _ = utcNow;
        notifyManagers = [];
        var submissions = state.BattleSubmissions.Append(submission).ToArray();
        var nextState = state.With(battleSubmissions: submissions);
        var current = CurrentSubmissions(nextState, battle);
        if (current.Count >= 2)
        {
            var first = current[0];
            var equivalent = current.All(item => item.IsDraw == first.IsDraw && item.WinnerForceId == first.WinnerForceId);
            if (equivalent)
            {
                var resolved = battle.With(status: BattleStatus.Finalized, winnerForceId: first.WinnerForceId, isDraw: first.IsDraw);
                return nextState.With(battles: ReplaceBattle(nextState.Battles, resolved))
                    .AppendLog(BattleEntry(PlayLogKind.BattleFinalized, resolved, utcNow));
            }

            var disputed = battle.With(status: BattleStatus.Disputed);
            notifyManagers = [Guid.Empty];
            return nextState.With(battles: ReplaceBattle(nextState.Battles, disputed))
                .AppendLog(BattleEntry(PlayLogKind.BattleDisputed, disputed, utcNow));
        }

        return nextState;
    }

    private static IReadOnlyList<BattleResultSubmission> CurrentSubmissions(CampaignPlayState state, CampaignBattle battle)
    {
        var participants = battle.ParticipantForceIds
            .Select(id => state.Forces.FirstOrDefault(force => force.Id == id)?.ControllerUserId)
            .OfType<Guid>()
            .Distinct()
            .ToArray();
        return
        [
            .. participants
                .Select(userId => state.LatestBattleSubmission(battle.Id, userId))
                .OfType<BattleResultSubmission>(),
        ];
    }

    private static IReadOnlyList<CampaignBattle> ReplaceBattle(IReadOnlyList<CampaignBattle> battles, CampaignBattle updated)
    {
        return [.. battles.Select(item => item.Id == updated.Id ? updated : item)];
    }

    private static PlayLogEntry BattleEntry(PlayLogKind kind, CampaignBattle battle, DateTimeOffset utcNow)
    {
        return new PlayLogEntry(
            Guid.NewGuid(),
            utcNow,
            kind,
            battle.BattleWindowId ?? battle.SourceWindowId,
            forceId: battle.WinnerForceId,
            actorUserId: null,
            battle.TerritoryId,
            targetTerritoryId: null,
            battle.Id,
            ActionKind.Battle,
            battle.ParticipantForceIds);
    }

    private static void ReplaceInPlace(List<CampaignBattle> battles, CampaignBattle updated)
    {
        var index = battles.FindIndex(item => item.Id == updated.Id);
        if (index >= 0)
        {
            battles[index] = updated;
        }
    }

    private static bool TryOpenAction(
        CampaignPlayState state,
        Guid userId,
        Guid forceId,
        DateTimeOffset utcNow,
        bool requireUncommitted,
        [NotNullWhen(true)] out PhaseWindow? window,
        [NotNullWhen(true)] out CampaignForce? force,
        [NotNullWhen(false)] out DomainError? error)
    {
        force = state.Forces.FirstOrDefault(item => item.Id == forceId && item.ControllerUserId == userId);
        window = state.CurrentWindow();
        error = null;
        if (window is null || window.Status != PhaseWindowStatus.Open || window.Kind != RoundPhaseKind.Action || utcNow >= window.EndsUtc)
        {
            error = new DomainError("order.window.closed", "The action window is not open.");
            return false;
        }

        if (force is null)
        {
            error = new DomainError("order.force.invalid", "That force is not yours.");
            return false;
        }

        if (force.InBattle)
        {
            error = new DomainError("order.force.in_battle", "A force in battle cannot submit a different action.");
            return false;
        }

        var windowId = window.Id;
        if (requireUncommitted && state.Commitments.Any(item => item.WindowId == windowId && item.UserId == userId))
        {
            error = new DomainError("order.already_committed", "Uncommit before changing orders.");
            return false;
        }

        return true;
    }

    private static bool TryOpenBattle(
        CampaignPlayState state,
        Guid userId,
        Guid battleId,
        DateTimeOffset utcNow,
        [NotNullWhen(true)] out CampaignBattle? battle,
        [NotNullWhen(false)] out DomainError? error)
    {
        battle = state.Battles.FirstOrDefault(item => item.Id == battleId);
        error = null;
        var window = state.CurrentWindow();
        if (window is null || window.Kind != RoundPhaseKind.Battle || window.Status != PhaseWindowStatus.Open)
        {
            error = new DomainError("battle.window.closed", "The battle phase is not open.");
            return false;
        }

        if (battle is null || battle.BattleWindowId != window.Id)
        {
            error = new DomainError("battle.not_found", "The battle was not found.");
            return false;
        }

        if (battle.Status is BattleStatus.Finalized or BattleStatus.GMResolved)
        {
            error = new DomainError("battle.already_resolved", "That battle is already resolved.");
            return false;
        }

        var participantIds = battle.ParticipantForceIds;
        var force = state.Forces.FirstOrDefault(item =>
            item.ControllerUserId == userId && participantIds.Contains(item.Id));
        if (force is null)
        {
            error = new DomainError("battle.forbidden", "You are not a participant in this battle.");
            return false;
        }

        _ = utcNow;
        return true;
    }

    private static bool IsEligibleRetreat(PlayMap map, CampaignForce force, Guid targetTerritoryId)
    {
        return EligibleRetreats(map, force).Contains(targetTerritoryId);
    }

    private static DateTimeOffset LastEnd(CampaignPlayState state, DateTimeOffset fallback)
    {
        return state.Windows.Count == 0 ? fallback : state.Windows.Max(static window => window.EndsUtc);
    }

    private static int RoundCountOf(CampaignPlayState state)
    {
        return state.Windows.Count == 0 ? 0 : state.Windows.Max(static window => window.RoundNumber);
    }

    internal static IReadOnlyList<TerritoryStructureState> CaptureStructures(PlayMap map)
    {
        return ActionResolution.CaptureStructures(map);
    }
}

/// <summary>
/// A player and the faction they have chosen.
/// </summary>
/// <param name="UserId">The player.</param>
/// <param name="FactionId">The chosen faction, if any.</param>
public sealed record PlayerFactionAssignment(Guid UserId, Guid? FactionId);

/// <summary>
/// Extra time to add to one remaining phase window.
/// </summary>
/// <param name="WindowId">The window to lengthen.</param>
/// <param name="ExtraDuration">The additional length.</param>
public sealed record PhaseExtension(Guid WindowId, ScheduleDuration ExtraDuration);

/// <summary>
/// Result of a play-state mutation.
/// </summary>
public sealed class PlayOutcome
{
    /// <summary>
    /// Initializes an outcome.
    /// </summary>
    public PlayOutcome(
        CampaignPlayState state,
        PlayMap map,
        DateTimeOffset endsUtc,
        int roundCount,
        bool preserveSchedule = false,
        IReadOnlyList<Guid>? NotifyManagerUserIds = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(map);
        State = state;
        Map = map;
        EndsUtc = endsUtc;
        RoundCount = roundCount;
        PreserveSchedule = preserveSchedule;
        this.NotifyManagerUserIds = NotifyManagerUserIds ?? [];
    }

    /// <summary>Gets the next play state.</summary>
    public CampaignPlayState State { get; }

    /// <summary>Gets the next map snapshot.</summary>
    public PlayMap Map { get; }

    /// <summary>Gets the campaign end instant.</summary>
    public DateTimeOffset EndsUtc { get; }

    /// <summary>Gets the round count.</summary>
    public int RoundCount { get; }

    /// <summary>Gets whether EndsUtc and RoundCount should be left unchanged by the caller.</summary>
    public bool PreserveSchedule { get; }

    /// <summary>Gets whether the map was not modified.</summary>
    public bool PreserveMap { get; init; }

    /// <summary>Gets manager user identifiers to notify. Empty means all managers when a sentinel is used.</summary>
    public IReadOnlyList<Guid> NotifyManagerUserIds { get; }
}
