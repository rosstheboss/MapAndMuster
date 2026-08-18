using System.Diagnostics.CodeAnalysis;
using Campaign.Domain.Campaigns;
using Campaign.Domain.Common;

namespace Campaign.Domain.Play;

/// <summary>
/// Seeds, advances, and mutates launched-campaign play state.
/// </summary>
public static class CampaignPlayRules
{
    private const int MaxBattleScore = 9999;
    /// <summary>
    /// Materializes windows, spawn flags, and starting forces when the campaign is in progress.
    /// </summary>
    public static PlayOutcome Seed(
        CampaignPlayState state,
        PlayMap map,
        CampaignSchedule schedule,
        IReadOnlyList<PlayerFactionAssignment> players,
        DateTimeOffset utcNow,
        IReadOnlyList<ItemObjectiveTypePlayRules>? itemObjectiveTypes = null,
        IReadOnlyList<ItemObjectiveMapPlacement>? itemPlacements = null,
        Func<int, int>? pickIndex = null,
        IReadOnlyList<PrivateObjectiveTypePlayRules>? privateObjectiveTypes = null,
        IReadOnlyList<Guid>? factionIds = null,
        IReadOnlyList<Guid>? allyGroupIds = null)
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

        var items = ItemObjectiveRules.Seed(
            itemObjectiveTypes ?? [],
            seededMap,
            itemPlacements ?? [],
            pickIndex ?? (static count => 0));
        var privateObjectives = PrivateObjectiveRules.SeedInitial(
            privateObjectiveTypes ?? [],
            [.. players.Where(static item => item.FactionId.HasValue).Select(static item => item.UserId)],
            factionIds ?? [],
            allyGroupIds ?? [],
            utcNow,
            pickIndex ?? (static count => 0));

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
            items,
            state.Log,
            privateObjectives: privateObjectives)
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
    /// Removes a kicked player's forces, drafts, and unresolved battles. Carried items drop on the territory.
    /// </summary>
    public static CampaignPlayState RemoveController(
        CampaignPlayState state,
        Guid userId,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(state);
        var removed = state.Forces.Where(force => force.ControllerUserId == userId).ToArray();
        if (removed.Length == 0 && state.Commitments.All(commitment => commitment.UserId != userId))
        {
            return state;
        }

        var removedIds = removed.Select(static force => force.Id).ToHashSet();
        var origins = removed.ToDictionary(static force => force.Id, static force => force.TerritoryId);
        var log = new List<PlayLogEntry>();
        var items = ItemObjectiveRules.DropCarriedByMovers(state.ItemObjectives, origins, utcNow, log);
        var battles = state.Battles
            .Where(battle =>
                battle.Status is BattleStatus.Finalized or BattleStatus.GMResolved
                || !battle.ParticipantForceIds.Any(removedIds.Contains))
            .ToArray();
        var remainingBattleIds = battles.Select(static battle => battle.Id).ToHashSet();
        return state.With(
                forces: [.. state.Forces.Where(force => force.ControllerUserId != userId)],
                drafts: [.. state.Drafts.Where(draft => !removedIds.Contains(draft.ForceId))],
                commitments: [.. state.Commitments.Where(commitment => commitment.UserId != userId)],
                battles: battles,
                battleSubmissions: [.. state.BattleSubmissions.Where(item => remainingBattleIds.Contains(item.BattleId))],
                retreats: [.. state.Retreats.Where(item => remainingBattleIds.Contains(item.BattleId))],
                itemObjectives: items)
            .AppendLog([.. log]);
    }

    /// <summary>
    /// Reassigns a player's existing forces to another faction without moving them.
    /// </summary>
    public static CampaignPlayState ReassignControllerFaction(
        CampaignPlayState state,
        Guid userId,
        Guid factionId)
    {
        ArgumentNullException.ThrowIfNull(state);
        var forces = state.Forces
            .Select(force => force.ControllerUserId == userId && force.FactionId != factionId
                ? new CampaignForce(force.Id, force.ControllerUserId, factionId, force.TerritoryId, force.InBattle, force.StatusName)
                : force)
            .ToArray();
        return state.With(forces: forces);
    }

    /// <summary>
    /// Closes overdue windows, resolves actions, and opens the next window.
    /// </summary>
    public static PlayOutcome Advance(
        CampaignPlayState state,
        PlayMap map,
        CampaignSchedule schedule,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        DateTimeOffset utcNow,
        IReadOnlyList<ForceStatusSetup>? forceStatuses = null)
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
                (nextState, nextMap) = CloseActionWindow(
                    nextState,
                    nextMap,
                    current,
                    factionAllyGroups,
                    closeAt,
                    due,
                    forceStatuses);
            }
        }
        else if (current.Status == PhaseWindowStatus.Open && current.Kind == RoundPhaseKind.Battle)
        {
            if (BattlePhaseComplete(nextState, current) || utcNow >= current.EndsUtc)
            {
                var closeAt = utcNow >= current.EndsUtc ? current.EndsUtc : utcNow;
                var due = utcNow >= current.EndsUtc;
                (nextState, nextMap) = CloseBattleWindow(nextState, nextMap, current, closeAt, due, forceStatuses);
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
        [NotNullWhen(false)] out DomainError? error,
        bool requireUncommitted = true)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(factionAllyGroups);
        next = null;
        error = null;
        if (!TryOpenAction(state, userId, forceId, utcNow, requireUncommitted, out var window, out var force, out error))
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

            if (map.StructureTypes.Count > 0)
            {
                var rules = map.StructureRules(structureTypeId.Value);
                if (rules is null || !rules.IsBuildable)
                {
                    error = new DomainError("order.build.not_buildable", "That structure cannot be built.", "structureTypeId");
                    return false;
                }
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
        [NotNullWhen(false)] out DomainError? error,
        IReadOnlyList<ForceStatusSetup>? forceStatuses = null)
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

        if (requiredForces.Any(force => state.DraftFor(window.Id, force.Id) is null))
        {
            error = new DomainError("order.draft.required", "Save a draft for every force before committing.");
            return false;
        }

        var submissions = state.Submissions.ToList();
        foreach (var force in requiredForces)
        {
            var draft = state.DraftFor(window.Id, force.Id)!;
            submissions.Add(new OrderSubmission(
                Guid.NewGuid(),
                window.Id,
                force.Id,
                draft.Kind,
                draft.TargetTerritoryId,
                draft.StructureTypeId,
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
            var (closed, closedMap) = CloseActionWindow(
                next,
                map,
                window,
                factionAllyGroups,
                utcNow,
                due: false,
                forceStatuses);
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
        [NotNullWhen(false)] out DomainError? error,
        int? winnerScore = null,
        int? loserScore = null,
        IReadOnlyList<ForceStatusSetup>? forceStatuses = null)
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

        if (!TryNormalizeBattleScores(isDraw, winnerScore, loserScore, out var parsedWinnerScore, out var parsedLoserScore, out error))
        {
            return false;
        }

        var submission = new BattleResultSubmission(
            Guid.NewGuid(),
            battle.Id,
            userId,
            winnerForceId,
            isDraw,
            null,
            utcNow,
            parsedWinnerScore,
            parsedLoserScore);
        var next = AppendBattleSubmission(state, battle, submission, utcNow, notifyManagers: out var notify);
        outcome = BattleMutationOutcome(next, utcNow, notify, forceStatuses);
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
        [NotNullWhen(false)] out DomainError? error,
        IReadOnlyList<ForceStatusSetup>? forceStatuses = null)
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
            utcNow,
            theirs.WinnerScore,
            theirs.LoserScore);
        var next = AppendBattleSubmission(state, battle, submission, utcNow, out var notify);
        outcome = BattleMutationOutcome(next, utcNow, notify, forceStatuses);
        return true;
    }

    /// <summary>
    /// Records a manager's authoritative battle result without erasing prior submissions.
    /// Requires an active debug session owned by <paramref name="actorUserId"/>.
    /// </summary>
    public static bool TryResolveBattle(
        CampaignPlayState state,
        Guid actorUserId,
        Guid battleId,
        Guid? winnerForceId,
        bool isDraw,
        DateTimeOffset utcNow,
        [NotNullWhen(true)] out CampaignPlayState? next,
        [NotNullWhen(false)] out DomainError? error,
        int? winnerScore = null,
        int? loserScore = null,
        IReadOnlyList<ForceStatusSetup>? forceStatuses = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        next = null;
        if (!TryRequireDebugActor(state, actorUserId, out error))
        {
            return false;
        }

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
        if (!TryNormalizeBattleScores(isDraw, winnerScore, loserScore, out var parsedWinnerScore, out var parsedLoserScore, out error))
        {
            return false;
        }

        var updated = battle.With(
            status: BattleStatus.GMResolved,
            winnerForceId: winnerForceId,
            isDraw: isDraw,
            clearWinner: isDraw,
            winnerScore: parsedWinnerScore,
            loserScore: parsedLoserScore,
            assignScores: true);

        var logged = ApplyBattleSpoils(
            state.With(battles: ReplaceBattle(state.Battles, updated)),
            updated,
            utcNow)
            .AppendLog(BattleEntry(PlayLogKind.BattleGmResolved, updated, utcNow, actorUserId));
        (next, _) = CloseCompletedBattlePhase(logged, MapUnchanged, utcNow, forceStatuses);
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
        [NotNullWhen(false)] out DomainError? error,
        IReadOnlyList<ForceStatusSetup>? forceStatuses = null)
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
        var (closed, closedMap) = CloseCompletedBattlePhase(next, map, utcNow, forceStatuses);
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
        bool due,
        IReadOnlyList<ForceStatusSetup>? forceStatuses = null)
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

        var snapshot = CaptureWindowSnapshot(state, map, window.Id);
        var withOrders = state.With(submissions: submissions);
        var (resolved, resolvedMap) = ActionResolution.Resolve(withOrders, map, window, factionAllyGroups, closeAt);
        var destructions = StructureDestructionRules.Detect(map, resolvedMap, resolved.Forces, closeAt);
        if (destructions.Count > 0)
        {
            resolved = resolved.With(structureDestructions: [.. resolved.StructureDestructions, .. destructions]);
        }

        var snapshots = resolved.Snapshots.Where(item => item.WindowId != window.Id).Append(snapshot).ToArray();
        resolved = ApplyActionStatuses(resolved.With(snapshots: snapshots), resolvedMap, window, forceStatuses);
        return FinishWindow(resolved, resolvedMap, window, closeAt, due, forceStatuses);
    }

    private static (CampaignPlayState State, PlayMap Map) CloseBattleWindow(
        CampaignPlayState state,
        PlayMap map,
        PhaseWindow window,
        DateTimeOffset closeAt,
        bool due,
        IReadOnlyList<ForceStatusSetup>? forceStatuses = null)
    {
        var battles = state.Battles.ToList();
        var log = new List<PlayLogEntry>();
        var notify = false;
        var nextItems = state.ItemObjectives;
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
                var finalized = battle.With(
                    status: BattleStatus.Finalized,
                    winnerForceId: only.WinnerForceId,
                    isDraw: only.IsDraw,
                    clearWinner: only.IsDraw,
                    winnerScore: only.WinnerScore,
                    loserScore: only.LoserScore,
                    assignScores: true);
                ReplaceInPlace(battles, finalized);
                log.Add(BattleEntry(PlayLogKind.BattleFinalized, finalized, closeAt));
                nextItems = ItemObjectiveRules.AwardBattleSpoils(nextItems, finalized, state.Forces, closeAt, log);
            }
            else if (current.Count == 0 && due)
            {
                notify = true;
                log.Add(BattleEntry(PlayLogKind.UnresolvedBattleHeldOpen, battle, closeAt));
            }
        }

        var next = state.With(battles: battles, itemObjectives: nextItems).AppendLog([.. log]);
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

        next = ApplyRetreats(next, window, closeAt);
        next = ApplyBattleStatuses(next, map, window, forceStatuses);
        return FinishWindow(next, map, window, closeAt, due, forceStatuses);
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

    private static CampaignPlayState ApplyRetreats(CampaignPlayState state, PhaseWindow window, DateTimeOffset utcNow)
    {
        var forces = state.Forces.ToDictionary(static force => force.Id);
        var origins = new Dictionary<Guid, Guid>();
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
                    origins[forceId] = force.TerritoryId;
                    forces[forceId] = force.With(territoryId: retreat.TargetTerritoryId, inBattle: false);
                }
                else
                {
                    forces[forceId] = force.With(inBattle: false);
                }
            }
        }

        var log = new List<PlayLogEntry>();
        var nextForces = forces.Values.OrderBy(static force => force.Id).ToArray();
        var items = ItemObjectiveRules.DropCarriedByMovers(state.ItemObjectives, origins, utcNow, log);
        items = ItemObjectiveRules.PickUpUnpossessed(items, nextForces, utcNow, log);
        return state.With(forces: nextForces, itemObjectives: items).AppendLog([.. log]);
    }

    private static CampaignPlayState ApplyBattleSpoils(
        CampaignPlayState state,
        CampaignBattle battle,
        DateTimeOffset utcNow)
    {
        var log = new List<PlayLogEntry>();
        var items = ItemObjectiveRules.AwardBattleSpoils(state.ItemObjectives, battle, state.Forces, utcNow, log);
        return state.With(itemObjectives: items).AppendLog([.. log]);
    }

    private static (CampaignPlayState State, PlayMap Map) FinishWindow(
        CampaignPlayState state,
        PlayMap map,
        PhaseWindow window,
        DateTimeOffset closeAt,
        bool due,
        IReadOnlyList<ForceStatusSetup>? forceStatuses = null)
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

        return CloseCompletedBattlePhase(nextState, map, closeAt, forceStatuses);
    }

    private static (CampaignPlayState State, PlayMap Map) CloseCompletedBattlePhase(
        CampaignPlayState state,
        PlayMap map,
        DateTimeOffset closeAt,
        IReadOnlyList<ForceStatusSetup>? forceStatuses = null)
    {
        var current = state.CurrentWindow();
        if (current is not { Kind: RoundPhaseKind.Battle, Status: PhaseWindowStatus.Open }
            || !BattlePhaseComplete(state, current))
        {
            return (state, map);
        }

        return CloseBattleWindow(state, map, current, closeAt, due: false, forceStatuses);
    }

    private static PlayOutcome BattleMutationOutcome(
        CampaignPlayState state,
        DateTimeOffset utcNow,
        IReadOnlyList<Guid> notify,
        IReadOnlyList<ForceStatusSetup>? forceStatuses = null)
    {
        var (closed, closedMap) = CloseCompletedBattlePhase(state, MapUnchanged, utcNow, forceStatuses);
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

    private static bool TryNormalizeBattleScores(
        bool isDraw,
        int? winnerScore,
        int? loserScore,
        out int? parsedWinnerScore,
        out int? parsedLoserScore,
        [NotNullWhen(false)] out DomainError? error)
    {
        _ = isDraw;
        parsedWinnerScore = null;
        parsedLoserScore = null;
        error = null;
        if (winnerScore is null && loserScore is null)
        {
            return true;
        }

        if (winnerScore is null || loserScore is null)
        {
            error = new DomainError(
                "battle.score.invalid",
                "Report both the winner and loser scores, or omit both.",
                "winnerScore");
            return false;
        }

        if (winnerScore < 0 || winnerScore > MaxBattleScore || loserScore < 0 || loserScore > MaxBattleScore)
        {
            error = new DomainError(
                "battle.score.invalid",
                $"Battle scores must be between 0 and {MaxBattleScore}.",
                "winnerScore");
            return false;
        }

        parsedWinnerScore = winnerScore;
        parsedLoserScore = loserScore;
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
            var equivalent = current.All(item =>
                item.IsDraw == first.IsDraw
                && item.WinnerForceId == first.WinnerForceId
                && item.WinnerScore == first.WinnerScore
                && item.LoserScore == first.LoserScore);
            if (equivalent)
            {
                var resolved = battle.With(
                    status: BattleStatus.Finalized,
                    winnerForceId: first.WinnerForceId,
                    isDraw: first.IsDraw,
                    clearWinner: first.IsDraw,
                    winnerScore: first.WinnerScore,
                    loserScore: first.LoserScore,
                    assignScores: true);
                return ApplyBattleSpoils(nextState.With(battles: ReplaceBattle(nextState.Battles, resolved)), resolved, utcNow)
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

    /// <summary>
    /// Starts a debug session for a manager or administrator. Logged publicly.
    /// </summary>
    public static bool TryEnterDebug(
        CampaignPlayState state,
        Guid userId,
        DateTimeOffset utcNow,
        [NotNullWhen(true)] out CampaignPlayState? next,
        [NotNullWhen(false)] out DomainError? error)
    {
        ArgumentNullException.ThrowIfNull(state);
        next = null;
        if (state.Windows.Count == 0)
        {
            error = new DomainError("debug.not_started", "Debug is available after the campaign starts.");
            return false;
        }

        if (state.DebugActorUserId is { } existing && existing != userId)
        {
            error = new DomainError("debug.busy", "Another manager is already in debug mode.");
            return false;
        }

        error = null;
        if (state.DebugActorUserId == userId)
        {
            next = state;
            return true;
        }

        next = state
            .With(debugActorUserId: userId, debugStartedUtc: utcNow)
            .AppendLog(DebugEntry(PlayLogKind.DebugEntered, userId, utcNow));
        return true;
    }

    /// <summary>
    /// Ends the current debug session. Logged publicly so every player is notified.
    /// </summary>
    public static bool TryExitDebug(
        CampaignPlayState state,
        Guid userId,
        DateTimeOffset utcNow,
        [NotNullWhen(true)] out CampaignPlayState? next,
        [NotNullWhen(false)] out DomainError? error)
    {
        ArgumentNullException.ThrowIfNull(state);
        next = null;
        if (state.DebugActorUserId is null)
        {
            error = new DomainError("debug.inactive", "Debug mode is not active.");
            return false;
        }

        error = null;
        next = state
            .With(clearDebug: true)
            .AppendLog(DebugEntry(PlayLogKind.DebugExited, userId, utcNow));
        return true;
    }

    /// <summary>
    /// While in debug, corrects a force's order. Open windows save a staff draft without revealing
    /// the order. The last resolved action window can be re-resolved while the following phase is open.
    /// Original submissions are never overwritten.
    /// </summary>
    public static bool TryDebugCorrectOrder(
        CampaignPlayState state,
        Guid actorUserId,
        Guid forceId,
        ActionKind kind,
        Guid? targetTerritoryId,
        Guid? structureTypeId,
        PlayMap map,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlySet<Guid>? knownStructureTypeIds,
        DateTimeOffset utcNow,
        [NotNullWhen(true)] out PlayOutcome? outcome,
        [NotNullWhen(false)] out DomainError? error)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(factionAllyGroups);
        outcome = null;
        if (!TryRequireDebugActor(state, actorUserId, out error))
        {
            return false;
        }

        var force = state.Forces.FirstOrDefault(item => item.Id == forceId);
        if (force is null)
        {
            error = new DomainError("order.force.invalid", "That force was not found.");
            return false;
        }

        var current = state.CurrentWindow();
        if (current is { Kind: RoundPhaseKind.Action, Status: PhaseWindowStatus.Open })
        {
            if (!TrySaveDraft(
                state,
                force.ControllerUserId,
                forceId,
                kind,
                targetTerritoryId,
                structureTypeId,
                map,
                factionAllyGroups,
                knownStructureTypeIds,
                utcNow,
                out var drafted,
                out error,
                requireUncommitted: false))
            {
                return false;
            }

            var logged = drafted.AppendLog(DebugEntry(
                PlayLogKind.DebugOrderCorrected,
                actorUserId,
                utcNow,
                current.Id,
                force.Id,
                force.TerritoryId,
                targetTerritoryId,
                actionKind: null));
            outcome = new PlayOutcome(logged, map, default, 0, preserveSchedule: true) { PreserveMap = true };
            return true;
        }

        var lastAction = state.Windows.LastOrDefault(item =>
            item.Kind == RoundPhaseKind.Action && item.Status == PhaseWindowStatus.Resolved);
        if (lastAction is null)
        {
            error = new DomainError("debug.no_action", "There is no resolved action window to correct.");
            return false;
        }

        var windows = state.Windows.ToList();
        var lastIndex = windows.FindIndex(item => item.Id == lastAction.Id);
        if (current is null
            || lastIndex < 0
            || lastIndex + 1 >= windows.Count
            || current.Id != windows[lastIndex + 1].Id
            || current.Status != PhaseWindowStatus.Open)
        {
            error = new DomainError(
                "debug.window.locked",
                "The previous action can only be re-resolved while the following phase is still open.");
            return false;
        }

        var snapshot = state.Snapshots.LastOrDefault(item => item.WindowId == lastAction.Id);
        if (snapshot is null)
        {
            error = new DomainError("debug.snapshot.missing", "That action window cannot be re-resolved.");
            return false;
        }

        if (kind == ActionKind.Battle || kind == ActionKind.Retreat)
        {
            error = new DomainError("order.kind.invalid", "Choose a player-submittable action.", "kind");
            return false;
        }

        var restoredMap = RestoreMap(map, snapshot);
        var removedBattleIds = state.Battles
            .Where(item => item.SourceWindowId == lastAction.Id)
            .Select(item => item.Id)
            .ToHashSet();
        var restored = state.With(
            forces: snapshot.Forces,
            structures: snapshot.Structures,
            brokenAllyFactionIds: snapshot.BrokenAllyFactionIds,
            itemObjectives: snapshot.ItemObjectives,
            battles: [.. state.Battles.Where(item => item.SourceWindowId != lastAction.Id)],
            battleSubmissions: [.. state.BattleSubmissions.Where(item => !removedBattleIds.Contains(item.BattleId))],
            retreats: [.. state.Retreats.Where(item => !removedBattleIds.Contains(item.BattleId))]);
        var snapshotForce = restored.Forces.FirstOrDefault(item => item.Id == forceId);
        if (snapshotForce is null)
        {
            error = new DomainError("order.force.invalid", "That force was not found in the restored window.");
            return false;
        }

        var validationWindows = restored.Windows
            .Select(item => item.Id == lastAction.Id
                ? item.With(status: PhaseWindowStatus.Open)
                : item.Id == current.Id
                    ? item.With(status: PhaseWindowStatus.Pending)
                    : item)
            .ToArray();
        if (!TrySaveDraft(
            restored.With(windows: validationWindows),
            snapshotForce.ControllerUserId,
            forceId,
            kind,
            targetTerritoryId,
            structureTypeId,
            restoredMap,
            factionAllyGroups,
            knownStructureTypeIds,
            lastAction.EndsUtc.AddTicks(-1),
            out _,
            out error,
            requireUncommitted: false))
        {
            return false;
        }

        var submission = new OrderSubmission(
            Guid.NewGuid(),
            lastAction.Id,
            forceId,
            kind,
            targetTerritoryId,
            structureTypeId,
            OrderSource.StaffCorrection,
            utcNow,
            actorUserId);
        var withOrders = restored
            .With(submissions: [.. restored.Submissions, submission])
            .AppendLog(DebugEntry(
                PlayLogKind.DebugOrderCorrected,
                actorUserId,
                utcNow,
                lastAction.Id,
                forceId,
                snapshotForce.TerritoryId,
                targetTerritoryId,
                kind));
        var (resolved, resolvedMap) = ActionResolution.Resolve(
            withOrders,
            restoredMap,
            lastAction,
            factionAllyGroups,
            utcNow);
        var battles = resolved.Battles
            .Select(item =>
                item.SourceWindowId == lastAction.Id && item.Status == BattleStatus.Pending
                    ? item.With(battleWindowId: current.Id, status: BattleStatus.AwaitingResults, assignWindow: true)
                    : item)
            .ToArray();
        var next = resolved
            .With(battles: battles)
            .AppendLog(DebugEntry(PlayLogKind.DebugActionReresolved, actorUserId, utcNow, lastAction.Id));
        outcome = new PlayOutcome(next, resolvedMap, LastEnd(next, current.EndsUtc), RoundCountOf(next));
        return true;
    }

    private static PlayLogEntry BattleEntry(
        PlayLogKind kind,
        CampaignBattle battle,
        DateTimeOffset utcNow,
        Guid? actorUserId = null)
    {
        return new PlayLogEntry(
            Guid.NewGuid(),
            utcNow,
            kind,
            battle.BattleWindowId ?? battle.SourceWindowId,
            forceId: battle.WinnerForceId,
            actorUserId,
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

    private static bool TryRequireDebugActor(
        CampaignPlayState state,
        Guid userId,
        [NotNullWhen(false)] out DomainError? error)
    {
        if (state.DebugActorUserId is null)
        {
            error = new DomainError("debug.required", "Enter debug mode to correct orders or battle results.");
            return false;
        }

        if (state.DebugActorUserId != userId)
        {
            error = new DomainError("debug.other_actor", "Another manager is already in debug mode.");
            return false;
        }

        error = null;
        return true;
    }

    private static PlayLogEntry DebugEntry(
        PlayLogKind kind,
        Guid actorUserId,
        DateTimeOffset utcNow,
        Guid? windowId = null,
        Guid? forceId = null,
        Guid? territoryId = null,
        Guid? targetTerritoryId = null,
        ActionKind? actionKind = null)
    {
        return new PlayLogEntry(
            Guid.NewGuid(),
            utcNow,
            kind,
            windowId,
            forceId,
            actorUserId,
            territoryId,
            targetTerritoryId,
            battleId: null,
            actionKind,
            forceId is { } id ? [id] : []);
    }

    private static CampaignPlayState ApplyActionStatuses(
        CampaignPlayState state,
        PlayMap map,
        PhaseWindow window,
        IReadOnlyList<ForceStatusSetup>? statuses)
    {
        var catalog = statuses ?? [];
        if (catalog.Count == 0)
        {
            return state;
        }

        var facts = state.Forces.ToDictionary(
            force => force.Id,
            force => ForceStatusRules.FromAction(
                state.LatestSubmission(window.Id, force.Id)?.Kind,
                map.Territory(force.TerritoryId)?.IsWaterFeature == true));
        return state.With(forces: ForceStatusRules.Apply(state.Forces, catalog, facts));
    }

    private static CampaignPlayState ApplyBattleStatuses(
        CampaignPlayState state,
        PlayMap map,
        PhaseWindow window,
        IReadOnlyList<ForceStatusSetup>? statuses)
    {
        var catalog = statuses ?? [];
        if (catalog.Count == 0)
        {
            return state;
        }

        var battles = state.Battles
            .Where(item => item.BattleWindowId == window.Id
                && item.Status is BattleStatus.Finalized or BattleStatus.GMResolved)
            .ToArray();
        var retreated = state.Retreats
            .Where(item => battles.Any(battle => battle.Id == item.BattleId))
            .Select(item => item.ForceId)
            .ToHashSet();
        var facts = new Dictionary<Guid, ForceStatusRules.Facts>();
        foreach (var force in state.Forces)
        {
            var fought = battles.Where(item => item.ParticipantForceIds.Contains(force.Id)).ToArray();
            facts[force.Id] = ForceStatusRules.FromBattle(
                fought.Length > 0,
                fought.Any(item => item.WinnerForceId == force.Id),
                fought.Any(item => !item.IsDraw && item.WinnerForceId != force.Id),
                retreated.Contains(force.Id),
                map.Territory(force.TerritoryId)?.IsWaterFeature == true);
        }

        return state.With(forces: ForceStatusRules.Apply(state.Forces, catalog, facts));
    }

    private static ActionWindowSnapshot CaptureWindowSnapshot(CampaignPlayState state, PlayMap map, Guid windowId)
    {
        return new ActionWindowSnapshot(
            windowId,
            [.. state.Forces.Select(static force => new CampaignForce(
                force.Id,
                force.ControllerUserId,
                force.FactionId,
                force.TerritoryId,
                force.InBattle,
                force.StatusName))],
            state.Structures,
            state.BrokenAllyFactionIds,
            [.. map.Territories.Select(static territory => new TerritorySnapshot(
                territory.Id,
                territory.OwnerFactionId,
                territory.StructureTypeId,
                territory.StructureName,
                territory.StructureCondition))],
            [.. state.ItemObjectives.Select(static item => new CampaignItemObjective(
                item.Id,
                item.TypeId,
                item.Name,
                item.TerritoryId,
                item.PossessorForceId,
                item.IsRevealed,
                item.OriginalTerritoryId,
                item.WasHiddenUntilFound))]);
    }

    private static PlayMap RestoreMap(PlayMap map, ActionWindowSnapshot snapshot)
    {
        var byId = snapshot.Territories.ToDictionary(static item => item.TerritoryId);
        var next = map.Territories.Select(territory =>
        {
            if (!byId.TryGetValue(territory.Id, out var captured))
            {
                return territory;
            }

            return new PlayTerritory(
                territory.Id,
                territory.DisplayNumber,
                captured.OwnerFactionId,
                territory.SpawnFactionId,
                captured.StructureTypeId,
                captured.StructureName,
                captured.Condition);
        }).ToArray();
        return map.WithTerritories(next);
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
