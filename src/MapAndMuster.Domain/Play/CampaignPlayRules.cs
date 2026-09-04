using System.Diagnostics.CodeAnalysis;
using MapAndMuster.Domain.Campaigns;
using MapAndMuster.Domain.Common;

namespace MapAndMuster.Domain.Play;

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
        IReadOnlyList<Guid>? allyGroupIds = null,
        SpecialRuleContext? specialRules = null,
        IReadOnlyDictionary<Guid, Guid?>? allyGroupByFaction = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(players);

        var rules = specialRules ?? SpecialRuleContext.None;
        var choose = pickIndex ?? (static count => 0);
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
        var nextMap = seededMap;
        foreach (var player in players.Where(static item => item.FactionId.HasValue).OrderBy(static item => item.UserId))
        {
            var placement = FactionSpecialRulePolicies.StartingPlacement(
                nextMap,
                player.FactionId!.Value,
                player.Subfaction,
                forces,
                rules,
                choose);
            if (placement is null)
            {
                continue;
            }

            forces.Add(new CampaignForce(
                Guid.NewGuid(),
                player.UserId,
                player.FactionId.Value,
                placement.Value.TerritoryId,
                false,
                subfaction: player.Subfaction));
            if (placement.Value.Capture)
            {
                nextMap = FactionSpecialRulePolicies.Capture(nextMap, placement.Value.TerritoryId, player.FactionId.Value);
            }
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
            pickIndex ?? (static count => 0),
            players
                .Where(static item => item.FactionId.HasValue)
                .ToDictionary(static item => item.UserId, static item => item.FactionId!.Value),
            allyGroupByFaction);

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
            CaptureStructures(nextMap),
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
        return new PlayOutcome(started, nextMap, schedule.EndsUtc, schedule.RoundCount);
    }

    /// <summary>
    /// Adds a starting force when a player chooses a faction that has a spawn.
    /// </summary>
    public static PlayOutcome EnsureForce(
        CampaignPlayState state,
        PlayMap map,
        Guid userId,
        Guid factionId,
        string? subfaction = null,
        SpecialRuleContext? specialRules = null,
        Func<int, int>? pickIndex = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(map);
        if (state.Forces.Any(force => force.ControllerUserId == userId))
        {
            return new PlayOutcome(state, map, default, 0, preserveSchedule: true);
        }

        var rules = specialRules ?? SpecialRuleContext.None;
        var placement = FactionSpecialRulePolicies.StartingPlacement(
            map,
            factionId,
            subfaction,
            state.Forces,
            rules,
            pickIndex ?? (static count => 0));
        if (placement is null)
        {
            return new PlayOutcome(state, ApplySpawnFlags(map), default, 0, preserveSchedule: true);
        }

        var nextMap = placement.Value.Capture
            ? FactionSpecialRulePolicies.Capture(map, placement.Value.TerritoryId, factionId)
            : ApplySpawnFlags(map);
        var forces = state.Forces.Append(
            new CampaignForce(Guid.NewGuid(), userId, factionId, placement.Value.TerritoryId, false, subfaction: subfaction)).ToArray();
        return new PlayOutcome(state.With(forces: forces), nextMap, default, 0, preserveSchedule: true);
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
                ? force.WithFaction(factionId, force.Subfaction)
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
        IReadOnlyList<ForceStatusSetup>? forceStatuses = null,
        Func<int, int>? pickIndex = null,
        IReadOnlyList<TerrainTypeSetup>? terrainTypes = null,
        IReadOnlyList<StructureTypeSetup>? structureTypes = null,
        SpecialRuleContext? specialRules = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(factionAllyGroups);

        var previousLog = state.Log.Count;
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
            if ((allCommitted && current.EndPhaseEarlyIfAble) || due)
            {
                var closeAt = due ? current.EndsUtc : utcNow;
                (nextState, nextMap) = CloseActionWindow(
                    nextState,
                    nextMap,
                    current,
                    factionAllyGroups,
                    closeAt,
                    due,
                    forceStatuses,
                    pickIndex ?? (static count => 0),
                    terrainTypes,
                    structureTypes,
                    specialRules);
            }
        }
        else if (current.Status == PhaseWindowStatus.Open && current.Kind == RoundPhaseKind.Battle)
        {
            if (BattlePhaseComplete(nextState, current) || utcNow >= current.EndsUtc)
            {
                var closeAt = utcNow >= current.EndsUtc ? current.EndsUtc : utcNow;
                var due = utcNow >= current.EndsUtc;
                (nextState, nextMap) = CloseBattleWindow(
                    nextState,
                    nextMap,
                    current,
                    closeAt,
                    due,
                    forceStatuses,
                    pickIndex ?? (static count => 0),
                    factionAllyGroups,
                    specialRules);
            }
        }

        return new PlayOutcome(
            nextState,
            nextMap,
            LastEnd(nextState, schedule.EndsUtc),
            nextState.Windows.Count == 0
                ? schedule.RoundCount
                : nextState.Windows.Max(static window => window.RoundNumber),
            NotifyManagerUserIds: DelinquencyRules.ShouldNotifyManagers(nextState, previousLog) ? [Guid.Empty] : null);
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
        bool requireUncommitted = true,
        Guid? viaTerritoryId = null,
        bool destroyImmediately = false,
        SpecialRuleContext? specialRules = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(factionAllyGroups);
        var rules = specialRules ?? SpecialRuleContext.None;
        next = null;
        error = null;
        if (!TryOpenAction(state, userId, forceId, utcNow, requireUncommitted, out var window, out var force, out error, allowInBattle: kind == ActionKind.Surrender))
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

        if (kind == ActionKind.Surrender)
        {
            if (!force.InBattle)
            {
                error = new DomainError("order.surrender.not_engaged", "Surrender is only available while the force is in battle.", "kind");
                return false;
            }

            if (targetTerritoryId is null || !IsEligibleRetreat(map, force, targetTerritoryId.Value))
            {
                error = new DomainError("order.target.invalid", "Choose an eligible retreat destination.", "targetTerritoryId");
                return false;
            }
        }

        if (kind is ActionKind.Move or ActionKind.Split or ActionKind.Retreat && targetTerritoryId is null)
        {
            error = new DomainError("order.target.required", "Choose a destination territory.", "targetTerritoryId");
            return false;
        }

        if (kind is ActionKind.Move or ActionKind.Split or ActionKind.Retreat
            && targetTerritoryId is { } destinationId)
        {
            var destination = map.Territory(destinationId);
            if (destination is not null && FactionSpecialRulePolicies.IsEnemySpawn(destination, force))
            {
                error = new DomainError("order.spawn.forbidden", "A force cannot enter another faction's spawn.", "targetTerritoryId");
                return false;
            }
        }

        if (kind is ActionKind.Move or ActionKind.Split
            && !FactionSpecialRulePolicies.IsValidMove(map, force, targetTerritoryId, viaTerritoryId, state.ItemObjectives, rules))
        {
            error = new DomainError("order.target.invalid", "That territory is not a legal destination.", "targetTerritoryId");
            return false;
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
                var structureRules = map.StructureRules(structureTypeId.Value);
                if (structureRules is null || !structureRules.IsBuildable)
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

        if (kind == ActionKind.Pillage && !ActionResolution.IsValidPillage(map, force, factionAllyGroups, state.BrokenAllyFactionIds, rules, state.BrokenAllySubfactions))
        {
            error = new DomainError("order.pillage.invalid", "Pillage requires a pillageable structure that is not allied.", "kind");
            return false;
        }

        if (kind == ActionKind.Repair && !ActionResolution.IsValidRepair(map, force, factionAllyGroups, state.BrokenAllyFactionIds))
        {
            error = new DomainError("order.repair.invalid", "Repair requires a pillaged structure you or an ally own.", "kind");
            return false;
        }

        if (kind == ActionKind.Backstab && !ActionResolution.IsValidBackstab(force, factionAllyGroups, state.BrokenAllyFactionIds, rules, state.BrokenAllySubfactions))
        {
            error = new DomainError("order.backstab.invalid", "Backstab requires an active alliance.", "kind");
            return false;
        }

        if (kind == ActionKind.Build
            && structureTypeId is { } buildType
            && !FactionSpecialRulePolicies.CanBuild(map, force, buildType, rules))
        {
            error = new DomainError("order.build.not_buildable", "That structure cannot be built.", "structureTypeId");
            return false;
        }

        if (kind == ActionKind.Pillage
            && destroyImmediately
            && !FactionSpecialRulePolicies.CanDestroyImmediately(force, rules))
        {
            error = new DomainError("order.pillage.destroy_forbidden", "This force cannot destroy a structure in a single Pillage.", "destroyImmediately");
            return false;
        }

        var draft = new OrderDraft(
            window.Id,
            force.Id,
            kind,
            targetTerritoryId,
            structureTypeId,
            utcNow,
            viaTerritoryId,
            destroyImmediately);
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
        IReadOnlyList<ForceStatusSetup>? forceStatuses = null,
        SpecialRuleContext? specialRules = null)
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
                userId,
                draft.ViaTerritoryId,
                draft.DestroyImmediately));
        }

        var commitments = state.Commitments.Append(new PlayerCommitment(window.Id, userId, utcNow)).ToArray();
        var next = state.With(submissions: submissions, commitments: commitments);
        var requiredPlayers = next.RequiredOrderPlayers(window.Id);
        if (window.EndPhaseEarlyIfAble
            && requiredPlayers.Count > 0
            && requiredPlayers.All(playerId => next.Commitments.Any(item => item.WindowId == window.Id && item.UserId == playerId)))
        {
            var (closed, closedMap) = CloseActionWindow(
                next,
                map,
                window,
                factionAllyGroups,
                utcNow,
                due: false,
                forceStatuses,
                pickIndex: null,
                terrainTypes: null,
                structureTypes: null,
                specialRules);
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

        if (state.Drafts.Any(item =>
            item.WindowId == window.Id
            && item.Kind == ActionKind.Surrender
            && state.Forces.Any(force => force.Id == item.ForceId && force.ControllerUserId == userId)))
        {
            error = new DomainError("order.surrender.locked", "A committed surrender cannot be withdrawn.");
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
        IReadOnlyList<ForceStatusSetup>? forceStatuses = null,
        IReadOnlyList<BattleParticipantReport>? reports = null,
        IReadOnlyList<MissionResultQuestionSetup>? missionQuestions = null,
        bool isStaff = false,
        PlayMap? map = null,
        SupplyCatalog? catalog = null,
        IReadOnlyDictionary<Guid, string?>? factionAllyGroups = null,
        Func<int, int>? pickIndex = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        outcome = null;
        if (!TryOpenBattle(state, userId, battleId, utcNow, out var battle, out error, isStaff))
        {
            return false;
        }

        var scoredReports = reports is { Count: > 0 }
            ? BattleResultRules.WithScoredAnswers(reports, missionQuestions ?? [])
            : [];
        if (scoredReports.Count > 0 && !battle.IsRinger)
        {
            if (!BattleResultRules.TryDeriveOutcome(
                    battle.ReportingForceIds,
                    scoredReports,
                    out winnerForceId,
                    out isDraw,
                    out winnerScore,
                    out loserScore,
                    out error))
            {
                return false;
            }
        }
        else if (isDraw && winnerForceId is not null)
        {
            error = new DomainError("battle.result.invalid", "A draw cannot name a winner.", "winnerForceId");
            return false;
        }

        if (!isDraw
            && winnerForceId is not null
            && !battle.ParticipantForceIds.Contains(winnerForceId.Value))
        {
            error = new DomainError("battle.result.invalid", "Choose a participating force as the winner.", "winnerForceId");
            return false;
        }

        if (!isDraw && winnerForceId is null && !battle.IsRinger)
        {
            error = new DomainError("battle.result.invalid", "Choose a participating force as the winner.", "winnerForceId");
            return false;
        }

        if (!TryNormalizeBattleScores(isDraw, winnerScore, loserScore, out var parsedWinnerScore, out var parsedLoserScore, out error))
        {
            return false;
        }

        if (scoredReports.Count > 0
            && !TryValidateBattleSpecialRuleUses(state, scoredReports, map, catalog, out error))
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
            parsedLoserScore,
            scoredReports);
        var next = AppendBattleSubmission(
            state,
            battle,
            submission,
            utcNow,
            out var notify,
            map,
            catalog,
            factionAllyGroups,
            pickIndex);
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
        IReadOnlyList<ForceStatusSetup>? forceStatuses = null,
        bool isStaff = false,
        PlayMap? map = null,
        SupplyCatalog? catalog = null,
        IReadOnlyDictionary<Guid, string?>? factionAllyGroups = null,
        Func<int, int>? pickIndex = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        outcome = null;
        if (!TryOpenBattle(state, userId, battleId, utcNow, out var battle, out error, isStaff))
        {
            return false;
        }

        var theirs = LatestConfirmableSubmission(state, battle, userId);
        if (theirs is null)
        {
            error = new DomainError("battle.accept.missing", "There is no submitted result to accept yet.");
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
            theirs.LoserScore,
            theirs.Reports);
        var next = AppendBattleSubmission(
            state,
            battle,
            submission,
            utcNow,
            out var notify,
            map,
            catalog,
            factionAllyGroups,
            pickIndex);
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
        IReadOnlyList<ForceStatusSetup>? forceStatuses = null,
        IReadOnlyList<BattleParticipantReport>? reports = null,
        IReadOnlyList<MissionResultQuestionSetup>? missionQuestions = null,
        PlayMap? map = null,
        SupplyCatalog? catalog = null,
        IReadOnlyDictionary<Guid, string?>? factionAllyGroups = null,
        Func<int, int>? pickIndex = null)
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

        var scoredReports = reports is { Count: > 0 }
            ? BattleResultRules.WithScoredAnswers(reports, missionQuestions ?? [])
            : [];
        if (scoredReports.Count > 0
            && !BattleResultRules.TryDeriveOutcome(
                battle.ReportingForceIds,
                scoredReports,
                out winnerForceId,
                out isDraw,
                out winnerScore,
                out loserScore,
                out error))
        {
            return false;
        }

        error = null;
        if (!TryNormalizeBattleScores(isDraw, winnerScore, loserScore, out var parsedWinnerScore, out var parsedLoserScore, out error))
        {
            return false;
        }

        if (scoredReports.Count > 0
            && !TryValidateBattleSpecialRuleUses(state, scoredReports, map, catalog, out error))
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
        logged = AfterMatchResolved(
            logged,
            updated,
            utcNow,
            map,
            catalog,
            factionAllyGroups,
            pickIndex,
            parkForNextBattlePhase: false);
        if (map is not null)
        {
            logged = ApplyStaffCorrectionRetreats(logged, map, updated, utcNow);
        }

        (next, _) = CloseCompletedBattlePhase(logged, map ?? MapUnchanged, utcNow, forceStatuses);
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

        var force = state.Forces.FirstOrDefault(item =>
            item.ControllerUserId == userId && battle.ParticipantForceIds.Contains(item.Id));
        if (force is null || !ForcesRequiredToRetreat(battle).Contains(force.Id))
        {
            error = new DomainError("retreat.not_required", "Only a force that must leave the battlefield submits a retreat.");
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
    /// Commits a surrender and retreat while the force is engaged. It cannot be withdrawn.
    /// </summary>
    public static bool TrySubmitSurrender(
        CampaignPlayState state,
        PlayMap map,
        Guid userId,
        Guid battleId,
        Guid targetTerritoryId,
        DateTimeOffset utcNow,
        [NotNullWhen(true)] out PlayOutcome? outcome,
        [NotNullWhen(false)] out DomainError? error,
        IReadOnlyList<ForceStatusSetup>? forceStatuses = null,
        IReadOnlyDictionary<Guid, string?>? factionAllyGroups = null,
        BattleScoringSetup? battleScoring = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(map);
        outcome = null;
        var window = state.CurrentWindow();
        if (window is null
            || window.Status != PhaseWindowStatus.Open
            || window.Kind is not (RoundPhaseKind.Action or RoundPhaseKind.Battle)
            || utcNow >= window.EndsUtc)
        {
            error = new DomainError("order.window.closed", "The current window is not open.");
            return false;
        }

        return TryCommitSurrender(
            state,
            map,
            userId,
            battleId,
            targetTerritoryId,
            utcNow,
            window.Id,
            out outcome,
            out error,
            forceStatuses,
            factionAllyGroups,
            battleScoring);
    }

    private static bool TryCommitSurrender(
        CampaignPlayState state,
        PlayMap map,
        Guid userId,
        Guid battleId,
        Guid targetTerritoryId,
        DateTimeOffset utcNow,
        Guid? windowId,
        [NotNullWhen(true)] out PlayOutcome? outcome,
        [NotNullWhen(false)] out DomainError? error,
        IReadOnlyList<ForceStatusSetup>? forceStatuses = null,
        IReadOnlyDictionary<Guid, string?>? factionAllyGroups = null,
        BattleScoringSetup? battleScoring = null,
        bool resolveImmediately = true)
    {
        outcome = null;
        var battle = state.Battles.FirstOrDefault(item => item.Id == battleId);
        if (battle is null || battle.Status is BattleStatus.Finalized or BattleStatus.GMResolved)
        {
            error = new DomainError("surrender.not_available", "That battle cannot be surrendered.");
            return false;
        }

        var force = state.Forces.FirstOrDefault(item =>
            item.ControllerUserId == userId && battle.ParticipantForceIds.Contains(item.Id) && item.InBattle);
        if (force is null)
        {
            error = new DomainError("surrender.not_engaged", "Surrender is only available while the force is in battle.");
            return false;
        }

        if (battle.SurrenderedForceIds.Contains(force.Id)
            || state.Retreats.Any(item => item.BattleId == battle.Id && item.ForceId == force.Id && item.IsSurrender))
        {
            error = new DomainError("surrender.already_committed", "A committed surrender cannot be withdrawn.");
            return false;
        }

        if (!IsEligibleRetreat(map, force, targetTerritoryId))
        {
            error = new DomainError("retreat.target.invalid", "Choose an adjacent eligible territory or your spawn.", "targetTerritoryId");
            return false;
        }

        error = null;
        var allies = factionAllyGroups ?? new Dictionary<Guid, string?>();
        var scoring = battleScoring ?? BattleScoringSetup.Default;
        var retreat = new RetreatOrder(Guid.NewGuid(), battle.Id, force.Id, targetTerritoryId, false, utcNow, isSurrender: true);
        var surrendered = battle.SurrenderedForceIds.Append(force.Id).Distinct().ToArray();
        var updatedBattle = battle.With(surrenderedForceIds: surrendered);
        var next = state.With(
                retreats: [.. state.Retreats, retreat],
                battles: ReplaceBattle(state.Battles, updatedBattle))
            .AppendLog(new PlayLogEntry(
                Guid.NewGuid(),
                utcNow,
                PlayLogKind.PlayerSurrendered,
                windowId,
                force.Id,
                userId,
                battle.TerritoryId,
                targetTerritoryId,
                battle.Id,
                ActionKind.Surrender,
                [force.Id]));
        if (resolveImmediately)
        {
            next = ResolveSurrenderedBattle(next, updatedBattle, map, allies, scoring, utcNow);
        }

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
                        PhaseWindowStatus.Pending,
                        phase.EndPhaseEarlyIfAble));
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
    /// Starts an ephemeral GM ringer battle against an idle player force.
    /// </summary>
    public static bool TryInjectRingerBattle(
        CampaignPlayState state,
        PlayMap map,
        Guid gmUserId,
        Guid targetForceId,
        Guid ringerFactionId,
        Guid? missionId,
        bool playerIsDefender,
        IReadOnlyList<TerrainTypeSetup> terrainTypes,
        IReadOnlyList<StructureTypeSetup> structureTypes,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        DateTimeOffset utcNow,
        Func<int, int> pickIndex,
        [NotNullWhen(true)] out PlayOutcome? outcome,
        [NotNullWhen(false)] out DomainError? error)
    {
        outcome = null;
        if (!RingerBattleRules.TryInject(
                state,
                map,
                gmUserId,
                targetForceId,
                ringerFactionId,
                missionId,
                playerIsDefender,
                terrainTypes,
                structureTypes,
                factionAllyGroups,
                utcNow,
                pickIndex,
                out var next,
                out error))
        {
            return false;
        }

        outcome = new PlayOutcome(next, map, default, 0, preserveSchedule: true) { PreserveMap = true };
        return true;
    }

    /// <summary>
    /// Adjacent territories a force may Move or Split into (never another faction's spawn), plus
    /// special-rule extra destinations.
    /// </summary>
    public static IReadOnlyList<Guid> EligibleMoves(
        PlayMap map,
        CampaignForce force,
        IReadOnlyList<CampaignItemObjective>? items = null,
        SpecialRuleContext? specialRules = null)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(force);
        var rules = specialRules ?? SpecialRuleContext.None;
        var catalogItems = items ?? [];
        var ids = new List<Guid>();
        foreach (var neighborId in map.Neighbors(force.TerritoryId))
        {
            var neighbor = map.Territory(neighborId);
            if (neighbor is null)
            {
                continue;
            }

            if (FactionSpecialRulePolicies.IsEnemySpawn(neighbor, force))
            {
                continue;
            }

            ids.Add(neighborId);
        }

        foreach (var extra in FactionSpecialRulePolicies.RelicAdjacentMoveTargets(map, force, catalogItems, rules))
        {
            if (!ids.Contains(extra))
            {
                ids.Add(extra);
            }
        }

        var pursuit = FactionSpecialRulePolicies.RelicPursuitTargets(map, force, catalogItems, rules);
        if (pursuit.Count > 0)
        {
            return pursuit;
        }

        return ids;
    }

    /// <summary>
    /// Two-territory Move hops for Crusaders.
    /// </summary>
    public static IReadOnlyList<MoveHop> EligibleMoveHops(
        PlayMap map,
        CampaignForce force,
        SpecialRuleContext? specialRules = null)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(force);
        var rules = specialRules ?? SpecialRuleContext.None;
        if (!rules.Has(force, SpecialRuleEffectKeys.Crusaders))
        {
            return [];
        }

        var hops = new List<MoveHop>();
        foreach (var via in map.Neighbors(force.TerritoryId))
        {
            if (!FactionSpecialRulePolicies.CanEnter(map, force, via))
            {
                continue;
            }

            foreach (var destination in map.Neighbors(via))
            {
                if (destination == force.TerritoryId || !FactionSpecialRulePolicies.CanEnter(map, force, destination))
                {
                    continue;
                }

                hops.Add(new MoveHop(via, destination));
            }
        }

        return hops;
    }

    /// <summary>
    /// Eligible retreat destinations: adjacent non-enemy-spawn territories, plus own spawn.
    /// The Art of War allows any non-enemy-spawn territory.
    /// </summary>
    public static IReadOnlyList<Guid> EligibleRetreats(
        PlayMap map,
        CampaignForce force,
        SpecialRuleContext? specialRules = null)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(force);
        var rules = specialRules ?? SpecialRuleContext.None;
        var ids = new List<Guid>();
        var spawn = map.SpawnFor(force.FactionId);
        if (spawn is not null)
        {
            ids.Add(spawn.Id);
        }

        var candidates = rules.Has(force, SpecialRuleEffectKeys.ArtOfWar)
            ? map.Territories.Select(static territory => territory.Id)
            : map.Neighbors(force.TerritoryId);
        foreach (var neighborId in candidates)
        {
            var neighbor = map.Territory(neighborId);
            if (neighbor is null)
            {
                continue;
            }

            if (FactionSpecialRulePolicies.IsEnemySpawn(neighbor, force))
            {
                continue;
            }

            if (!ids.Contains(neighborId))
            {
                ids.Add(neighborId);
            }
        }

        if (rules.Has(force, SpecialRuleEffectKeys.GreatCityOfMagritta))
        {
            var capital = map.Territories.FirstOrDefault(static territory => StructureKinds.IsCapitalCity(territory.StructureName));
            if (capital is not null && !ids.Contains(capital.Id))
            {
                ids.Add(capital.Id);
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
                    PhaseWindowStatus.Pending,
                    phase.EndPhaseEarlyIfAble));
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
            {
                if (battle.Status != BattleStatus.Pending
                    || (battle.BattleWindowId != windowId && battle.BattleWindowId is not null)
                    || (battle.ParticipantForceIds.Count < 2 && !battle.IsRinger))
                {
                    return battle;
                }

                return battle.With(battleWindowId: windowId, status: BattleStatus.AwaitingResults, assignWindow: true);
            })
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
        IReadOnlyList<ForceStatusSetup>? forceStatuses = null,
        Func<int, int>? pickIndex = null,
        IReadOnlyList<TerrainTypeSetup>? terrainTypes = null,
        IReadOnlyList<StructureTypeSetup>? structureTypes = null,
        SpecialRuleContext? specialRules = null)
    {
        var choose = pickIndex ?? (static count => 0);
        state = ApplyDeadlineSurrenders(state, map, window, factionAllyGroups, closeAt, due);
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
                    force.ControllerUserId,
                    draft.ViaTerritoryId,
                    draft.DestroyImmediately));
            }
        }

        var snapshot = CaptureWindowSnapshot(state, map, window.Id);
        var withOrders = state.With(submissions: submissions);
        var (resolved, resolvedMap) = ActionResolution.Resolve(
            withOrders,
            map,
            window,
            factionAllyGroups,
            closeAt,
            terrainTypes,
            structureTypes,
            choose,
            specialRules);
        var structureSupply = map.StructureTypes.ToDictionary(
            static type => type.Id,
            static type => new StructureSupplyRules(type.SupplyPoints, type.PillageSupplyPoints, type.DestroySupplyPoints));
        resolved = resolved.With(
            playerSupplies: SupplyRules.AwardTemporary(
                resolved.PlayerSupplies,
                map,
                resolvedMap,
                resolved.Forces,
                structureSupply,
                specialRules));
        var destructions = StructureDestructionRules.Detect(map, resolvedMap, resolved.Forces, closeAt);
        if (destructions.Count > 0)
        {
            resolved = resolved.With(structureDestructions: [.. resolved.StructureDestructions, .. destructions]);
        }

        var works = StructureDestructionRules.DetectWork(map, resolvedMap, resolved.Forces, closeAt);
        if (works.Count > 0)
        {
            resolved = resolved.With(structureWorks: [.. resolved.StructureWorks, .. works]);
        }

        resolved = AssignOpeningMatches(resolved, resolvedMap, factionAllyGroups, choose);
        var snapshots = resolved.Snapshots.Where(item => item.WindowId != window.Id).Append(snapshot).ToArray();
        resolved = ApplyActionStatuses(resolved.With(snapshots: snapshots), resolvedMap, window, forceStatuses, specialRules, closeAt);
        var missing = submissions
            .Where(item => item.WindowId == window.Id && item.Source == OrderSource.DeadlineHold)
            .Select(item => item.ForceId);
        resolved = DelinquencyRules.Record(resolved, missing, window, closeAt);
        return FinishWindow(resolved, resolvedMap, window, closeAt, due, forceStatuses);
    }

    private static (CampaignPlayState State, PlayMap Map) CloseBattleWindow(
        CampaignPlayState state,
        PlayMap map,
        PhaseWindow window,
        DateTimeOffset closeAt,
        bool due,
        IReadOnlyList<ForceStatusSetup>? forceStatuses = null,
        Func<int, int>? pickIndex = null,
        IReadOnlyDictionary<Guid, string?>? factionAllyGroups = null,
        SpecialRuleContext? specialRules = null)
    {
        var choose = pickIndex ?? (static count => 0);
        var allies = factionAllyGroups ?? new Dictionary<Guid, string?>();
        state = ApplyDeadlineSurrenders(state, map, window, allies, closeAt, due);
        var battles = state.Battles.ToList();
        var log = new List<PlayLogEntry>();
        var notify = false;
        var nextItems = state.ItemObjectives;
        var finalizedNow = new List<CampaignBattle>();
        var voidedRingerIds = new HashSet<Guid>();
        var noResultForceIds = new List<Guid>();
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
                finalizedNow.Add(finalized);
            }
            else if (current.Count == 0 && due)
            {
                if (battle.IsRinger)
                {
                    voidedRingerIds.Add(battle.Id);
                    log.Add(BattleEntry(PlayLogKind.RingerBattleVoided, battle, closeAt));
                    continue;
                }

                var finalized = battle.With(
                    status: BattleStatus.Finalized,
                    isDraw: false,
                    clearWinner: true,
                    isNoContest: true,
                    winnerScore: 0,
                    loserScore: 0,
                    assignScores: true);
                ReplaceInPlace(battles, finalized);
                log.Add(BattleEntry(PlayLogKind.NoResultForcedRetreat, finalized, closeAt));
                noResultForceIds.AddRange(finalized.ReportingForceIds);
                finalizedNow.Add(finalized);
            }
        }

        var next = state.With(battles: battles, itemObjectives: nextItems).AppendLog([.. log]);
        if (voidedRingerIds.Count > 0)
        {
            var remainingBattles = next.Battles.Where(item => !voidedRingerIds.Contains(item.Id)).ToArray();
            next = next.With(
                battles: remainingBattles,
                forces:
                [
                    .. next.Forces.Select(force => force.With(
                        inBattle: remainingBattles.Any(item =>
                            item.ParticipantForceIds.Contains(force.Id)
                            && item.Status is not BattleStatus.Finalized and not BattleStatus.GMResolved))),
                ]);
        }
        foreach (var finalized in finalizedNow)
        {
            next = AfterMatchResolved(
                next,
                finalized,
                closeAt,
                map,
                catalog: null,
                allies,
                choose,
                parkForNextBattlePhase: true);
        }
        if (!due && !BattlePhaseComplete(next, window))
        {
            return (next, map);
        }

        if (due)
        {
            next = ApplyDefaultRetreats(next, map, window, closeAt, noContestOnly: false);
            next = ApplyDefaultRetreats(next, map, window, closeAt, noContestOnly: true);
            if (noResultForceIds.Count > 0)
            {
                next = DelinquencyRules.Record(next, noResultForceIds, window, closeAt);
            }

            var missedRetreats = next.Retreats
                .Where(item =>
                    item.IsDefault
                    && !item.IsStaffCorrection
                    && next.Battles.Any(battle =>
                        battle.Id == item.BattleId
                        && !battle.IsNoContest
                        && !battle.IsRinger
                        && battle.BattleWindowId == window.Id))
                .Select(item => item.ForceId);
            next = DelinquencyRules.Record(next, missedRetreats, window, closeAt);
        }

        if (!BattlePhaseComplete(next, window) && due)
        {
            _ = notify;
            return (next, map);
        }

        next = ApplyRetreats(next, map, window, closeAt, pickIndex ?? (static count => 0));
        next = ApplyBattleStatuses(next, map, window, forceStatuses, specialRules, closeAt);
        var claimedMap = ApplyOccupationClaims(next, map, allies, choose);
        return FinishWindow(next, claimedMap, window, closeAt, due, forceStatuses);
    }

    private static PlayMap ApplyOccupationClaims(
        CampaignPlayState state,
        PlayMap map,
        IReadOnlyDictionary<Guid, string?> allies,
        Func<int, int> pickIndex)
    {
        var next = map;
        foreach (var battle in state.Battles.Where(static item =>
                     item.IsRinger
                     && item.Status is BattleStatus.Finalized or BattleStatus.GMResolved
                     && !item.IsDraw
                     && !item.IsNoContest))
        {
            var playerForce = state.Forces.FirstOrDefault(force => battle.ParticipantForceIds.Contains(force.Id));
            if (playerForce is null || battle.WinnerForceId == playerForce.Id)
            {
                continue;
            }

            var territory = next.Territory(battle.TerritoryId);
            if (territory is null || territory.IsSpawn)
            {
                continue;
            }

            next = next.Replace(territory.With(ownerFactionId: null, assignOwner: true));
        }

        return ActionResolution.ApplyIdleOccupation(
            next,
            state.Forces,
            allies,
            state.BrokenAllyFactionIds,
            pickIndex);
    }

    private static CampaignPlayState ApplyDefaultRetreats(
        CampaignPlayState state,
        PlayMap map,
        PhaseWindow window,
        DateTimeOffset utcNow,
        bool noContestOnly)
    {
        var retreats = state.Retreats.ToList();
        var log = new List<PlayLogEntry>();
        foreach (var battle in state.Battles.Where(item => item.BattleWindowId == window.Id))
        {
            if (battle.Status is not BattleStatus.Finalized and not BattleStatus.GMResolved)
            {
                continue;
            }

            if (battle.IsNoContest != noContestOnly)
            {
                continue;
            }

            var occupied = new HashSet<Guid>(
                retreats.Select(item => item.TargetTerritoryId));
            foreach (var forceId in ForcesRequiredToRetreat(battle))
            {
                if (retreats.Any(item => item.BattleId == battle.Id && item.ForceId == forceId))
                {
                    continue;
                }

                var force = state.Forces.FirstOrDefault(item => item.Id == forceId);
                if (force is null)
                {
                    continue;
                }

                var target = PickSafestRetreat(map, force, occupied);
                occupied.Add(target);
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

    private static CampaignPlayState ApplyRetreats(
        CampaignPlayState state,
        PlayMap map,
        PhaseWindow window,
        DateTimeOffset utcNow,
        Func<int, int> pickIndex)
    {
        var forces = state.Forces.ToDictionary(static force => force.Id);
        var origins = new Dictionary<Guid, Guid>();
        var staying = new HashSet<Guid>();
        foreach (var battle in state.Battles)
        {
            if (battle.Status is not BattleStatus.Finalized and not BattleStatus.GMResolved)
            {
                foreach (var forceId in battle.ParticipantForceIds)
                {
                    staying.Add(forceId);
                    if (forces.TryGetValue(forceId, out var openForce))
                    {
                        forces[forceId] = openForce.With(inBattle: true);
                    }
                }

                continue;
            }

            if (battle.BattleWindowId != window.Id)
            {
                continue;
            }

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
                    forces[forceId] = force.With(territoryId: retreat.TargetTerritoryId, inBattle: staying.Contains(forceId));
                }
                else if (!staying.Contains(forceId))
                {
                    forces[forceId] = force.With(inBattle: false);
                }
            }
        }

        ResolveRetreatCollisions(forces, map, state, pickIndex, utcNow, out var collisionLog);
        var log = new List<PlayLogEntry>(collisionLog);
        var nextForces = forces.Values.OrderBy(static force => force.Id).ToArray();
        var items = ItemObjectiveRules.DropCarriedByMovers(state.ItemObjectives, origins, utcNow, log);
        items = ItemObjectiveRules.PickUpUnpossessed(items, nextForces, utcNow, log);
        _ = staying;
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
        if (!window.EndPhaseEarlyIfAble)
        {
            return false;
        }

        var battles = state.Battles.Where(item => item.BattleWindowId == window.Id).ToArray();
        if (battles.Any(item => item.Status is not BattleStatus.Finalized and not BattleStatus.GMResolved))
        {
            return false;
        }

        foreach (var battle in battles)
        {
            if (battle.Status is not BattleStatus.Finalized and not BattleStatus.GMResolved)
            {
                continue;
            }

            foreach (var forceId in ForcesRequiredToRetreat(battle))
            {
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
        out IReadOnlyList<Guid> notifyManagers,
        PlayMap? map = null,
        SupplyCatalog? catalog = null,
        IReadOnlyDictionary<Guid, string?>? factionAllyGroups = null,
        Func<int, int>? pickIndex = null)
    {
        _ = utcNow;
        notifyManagers = [];
        var submissions = state.BattleSubmissions.Append(submission).ToArray();
        var nextState = state.With(battleSubmissions: submissions);
        var participants = battle.ReportingForceIds
            .Select(id => state.Forces.FirstOrDefault(force => force.Id == id)?.ControllerUserId)
            .OfType<Guid>()
            .Distinct()
            .ToHashSet();
        if (submission.AcceptedSubmissionId is not null && !participants.Contains(submission.SubmitterUserId))
        {
            var resolved = battle.With(
                status: BattleStatus.Finalized,
                winnerForceId: submission.WinnerForceId,
                isDraw: submission.IsDraw,
                clearWinner: submission.IsDraw,
                winnerScore: submission.WinnerScore,
                loserScore: submission.LoserScore,
                assignScores: true);
            return AfterMatchResolved(
                ApplyBattleSpoils(nextState.With(battles: ReplaceBattle(nextState.Battles, resolved)), resolved, utcNow)
                    .AppendLog(BattleEntry(PlayLogKind.BattleFinalized, resolved, utcNow)),
                resolved,
                utcNow,
                map,
                catalog,
                factionAllyGroups,
                pickIndex,
                parkForNextBattlePhase: false);
        }

        var current = CurrentSubmissions(nextState, battle);
        if (battle.IsRinger && current.Count >= 1)
        {
            var first = current[0];
            var resolved = battle.With(
                status: BattleStatus.Finalized,
                winnerForceId: first.WinnerForceId,
                isDraw: first.IsDraw,
                clearWinner: first.IsDraw || first.WinnerForceId is null,
                winnerScore: first.WinnerScore,
                loserScore: first.LoserScore,
                assignScores: true);
            return AfterMatchResolved(
                ApplyBattleSpoils(nextState.With(battles: ReplaceBattle(nextState.Battles, resolved)), resolved, utcNow)
                    .AppendLog(BattleEntry(PlayLogKind.BattleFinalized, resolved, utcNow)),
                resolved,
                utcNow,
                map,
                catalog,
                factionAllyGroups,
                pickIndex,
                parkForNextBattlePhase: false);
        }

        if (current.Count >= 2)
        {
            var first = current[0];
            var equivalent = current.All(item => BattleResultRules.AreEquivalent(item, first));
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
                return AfterMatchResolved(
                    ApplyBattleSpoils(nextState.With(battles: ReplaceBattle(nextState.Battles, resolved)), resolved, utcNow)
                        .AppendLog(BattleEntry(PlayLogKind.BattleFinalized, resolved, utcNow)),
                    resolved,
                    utcNow,
                    map,
                    catalog,
                    factionAllyGroups,
                    pickIndex,
                    parkForNextBattlePhase: false);
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
        var participants = battle.ReportingForceIds
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
    /// the order. The last resolved action window can be re-resolved while the following phase is open
    /// or during post-campaign grace. Original submissions are never overwritten.
    /// </summary>
    /// <param name="state"></param>
    /// <param name="actorUserId"></param>
    /// <param name="forceId"></param>
    /// <param name="kind"></param>
    /// <param name="targetTerritoryId"></param>
    /// <param name="structureTypeId"></param>
    /// <param name="map"></param>
    /// <param name="factionAllyGroups"></param>
    /// <param name="knownStructureTypeIds"></param>
    /// <param name="utcNow"></param>
    /// <param name="outcome"></param>
    /// <param name="error"></param>
    /// <param name="terrainTypes"></param>
    /// <param name="structureTypes"></param>
    /// <param name="pickIndex"></param>
    /// <param name="schedule"></param>
    /// <param name="reResolvePrevious">
    /// When true, re-resolves the previous action even if the current window is an open action phase.
    /// </param>
    /// <param name="viaTerritoryId">The first hop for a two-territory Move.</param>
    /// <param name="destroyImmediately">Whether Pillage destroys the structure immediately.</param>
    /// <param name="specialRules">Mechanical special-rule assignments.</param>
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
        [NotNullWhen(false)] out DomainError? error,
        IReadOnlyList<TerrainTypeSetup>? terrainTypes = null,
        IReadOnlyList<StructureTypeSetup>? structureTypes = null,
        Func<int, int>? pickIndex = null,
        CampaignSchedule? schedule = null,
        bool reResolvePrevious = false,
        Guid? viaTerritoryId = null,
        bool destroyImmediately = false,
        SpecialRuleContext? specialRules = null)
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
        if (!reResolvePrevious && current is { Kind: RoundPhaseKind.Action, Status: PhaseWindowStatus.Open })
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
                requireUncommitted: false,
                viaTerritoryId,
                destroyImmediately,
                specialRules))
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
        var grace = schedule is not null && IsWithinPostCampaignGrace(state, schedule, utcNow);
        var followingOpen = current is not null
            && lastIndex >= 0
            && lastIndex + 1 < windows.Count
            && current.Id == windows[lastIndex + 1].Id
            && current.Status == PhaseWindowStatus.Open;
        if (!followingOpen && !grace)
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
        var previousBattles = state.Battles.Where(item => item.SourceWindowId == lastAction.Id).ToArray();
        var restored = state.With(
            forces: snapshot.Forces,
            structures: snapshot.Structures,
            brokenAllyFactionIds: snapshot.BrokenAllyFactionIds,
            itemObjectives: snapshot.ItemObjectives,
            battles: [.. state.Battles.Where(item => item.SourceWindowId != lastAction.Id)]);
        var snapshotForce = restored.Forces.FirstOrDefault(item => item.Id == forceId);
        if (snapshotForce is null)
        {
            error = new DomainError("order.force.invalid", "That force was not found in the restored window.");
            return false;
        }

        var validationWindows = restored.Windows
            .Select(item => item.Id == lastAction.Id
                ? item.With(status: PhaseWindowStatus.Open)
                : current is not null && item.Id == current.Id
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
            requireUncommitted: false,
            viaTerritoryId,
            destroyImmediately,
            specialRules))
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
            utcNow,
            terrainTypes,
            structureTypes,
            pickIndex);
        var battles = resolved.Battles
            .Select(item =>
                item.SourceWindowId == lastAction.Id && item.Status == BattleStatus.Pending
                    ? item.With(
                        battleWindowId: current?.Id ?? item.BattleWindowId,
                        status: BattleStatus.AwaitingResults,
                        assignWindow: current is not null)
                    : item)
            .ToArray();
        var next = ReattachMatchingBattles(
                resolved.With(battles: battles),
                previousBattles)
            .AppendLog(DebugEntry(PlayLogKind.DebugActionReresolved, actorUserId, utcNow, lastAction.Id));
        if (current is not null)
        {
            next = UncommitAffectedCurrentPhase(
                state,
                next,
                current,
                restoredMap,
                resolvedMap,
                factionAllyGroups,
                knownStructureTypeIds);
        }

        outcome = new PlayOutcome(next, resolvedMap, LastEnd(next, current?.EndsUtc ?? lastAction.EndsUtc), RoundCountOf(next));
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
        [NotNullWhen(false)] out DomainError? error,
        bool allowInBattle = false)
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

        if (force.InBattle && !allowInBattle)
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

    private static bool IsWithinPostCampaignGrace(
        CampaignPlayState state,
        CampaignSchedule schedule,
        DateTimeOffset utcNow)
    {
        if (state.Windows.Count == 0 || state.Windows.Any(static window => window.Status != PhaseWindowStatus.Resolved))
        {
            return false;
        }

        var last = state.Windows[^1];
        var phases = schedule.Phases;
        if (phases.Count == 0)
        {
            return false;
        }

        var nextPhase = phases[last.PhaseNumber % phases.Count];
        var graceEnds = CampaignCalendar.Add(last.EndsUtc, schedule.TimeZone, nextPhase.Duration);
        return utcNow < graceEnds;
    }

    private static CampaignPlayState ReattachMatchingBattles(
        CampaignPlayState state,
        CampaignBattle[] previousBattles)
    {
        if (previousBattles.Length == 0)
        {
            return state;
        }

        var replacements = new Dictionary<Guid, Guid>();
        var battles = state.Battles.Select(battle =>
        {
            if (battle.Status is BattleStatus.Finalized or BattleStatus.GMResolved)
            {
                return battle;
            }

            var match = previousBattles.FirstOrDefault(previous =>
                previous.TerritoryId == battle.TerritoryId
                && previous.ParticipantForceIds.ToHashSet().SetEquals(battle.ParticipantForceIds));
            if (match is null || match.Id == battle.Id)
            {
                return battle;
            }

            replacements[battle.Id] = match.Id;
            return new CampaignBattle(
                match.Id,
                battle.TerritoryId,
                battle.SourceWindowId,
                battle.BattleWindowId,
                battle.Status,
                battle.ParticipantForceIds,
                battle.WinnerForceId,
                battle.IsDraw,
                match.CreatedUtc,
                battle.WinnerScore,
                battle.LoserScore,
                battle.ActiveForceIds,
                battle.WaitingForceIds,
                battle.SurrenderedForceIds,
                battle.IsNoContest,
                battle.MissionId,
                battle.AttackerForceId,
                battle.DefenderForceId,
                battle.IsRinger,
                battle.RingerFactionId,
                battle.InitiatingGmUserId,
                battle.RingerIsAttacker);
        }).ToArray();

        if (replacements.Count == 0)
        {
            return state.With(battles: battles);
        }

        var submissions = state.BattleSubmissions
            .Select(item => replacements.TryGetValue(item.BattleId, out var nextId)
                ? new BattleResultSubmission(
                    item.Id,
                    nextId,
                    item.SubmitterUserId,
                    item.WinnerForceId,
                    item.IsDraw,
                    item.AcceptedSubmissionId,
                    item.SubmittedUtc,
                    item.WinnerScore,
                    item.LoserScore,
                    item.Reports)
                : item)
            .ToArray();
        return state.With(battles: battles, battleSubmissions: submissions);
    }

    private static CampaignPlayState UncommitAffectedCurrentPhase(
        CampaignPlayState before,
        CampaignPlayState after,
        PhaseWindow current,
        PlayMap beforeMap,
        PlayMap afterMap,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlySet<Guid>? knownStructureTypeIds)
    {
        if (current.Kind != RoundPhaseKind.Action)
        {
            return after;
        }

        var priorForces = before.Forces.ToDictionary(static force => force.Id);
        var affectedUsers = new HashSet<Guid>();
        var drafts = after.Drafts.ToList();
        foreach (var force in after.Forces)
        {
            priorForces.TryGetValue(force.Id, out var prior);
            var draft = drafts.FirstOrDefault(item => item.WindowId == current.Id && item.ForceId == force.Id);
            var locationChanged = prior is null
                || prior.TerritoryId != force.TerritoryId
                || prior.InBattle != force.InBattle;
            var targetChanged = draft?.TargetTerritoryId is { } target
                && OccupancyChanged(target, priorForces.Values, after.Forces, beforeMap, afterMap);
            var illegal = draft is not null
                && !force.InBattle
                && !IsCurrentDraftLegal(after, afterMap, force, draft, factionAllyGroups, knownStructureTypeIds);
            if (!locationChanged && !targetChanged && !illegal)
            {
                continue;
            }

            affectedUsers.Add(force.ControllerUserId);
            if (illegal)
            {
                drafts.RemoveAll(item => item.WindowId == current.Id && item.ForceId == force.Id);
            }
        }

        if (affectedUsers.Count == 0)
        {
            return after;
        }

        var commitments = after.Commitments
            .Where(item => item.WindowId != current.Id || !affectedUsers.Contains(item.UserId))
            .ToArray();
        return after.With(drafts: drafts, commitments: commitments);
    }

    private static bool OccupancyChanged(
        Guid territoryId,
        IEnumerable<CampaignForce> beforeForces,
        IReadOnlyList<CampaignForce> afterForces,
        PlayMap beforeMap,
        PlayMap afterMap)
    {
        var beforeOwner = beforeMap.Territory(territoryId)?.OwnerFactionId;
        var afterOwner = afterMap.Territory(territoryId)?.OwnerFactionId;
        if (beforeOwner != afterOwner)
        {
            return true;
        }

        var beforeOccupants = beforeForces
            .Where(force => force.TerritoryId == territoryId)
            .Select(static force => force.Id)
            .OrderBy(static id => id);
        var afterOccupants = afterForces
            .Where(force => force.TerritoryId == territoryId)
            .Select(static force => force.Id)
            .OrderBy(static id => id);
        return !beforeOccupants.SequenceEqual(afterOccupants);
    }

    private static bool IsCurrentDraftLegal(
        CampaignPlayState state,
        PlayMap map,
        CampaignForce force,
        OrderDraft draft,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlySet<Guid>? knownStructureTypeIds)
    {
        return TrySaveDraft(
            state,
            force.ControllerUserId,
            force.Id,
            draft.Kind,
            draft.TargetTerritoryId,
            draft.StructureTypeId,
            map,
            factionAllyGroups,
            knownStructureTypeIds,
            draft.UpdatedUtc,
            out _,
            out _,
            requireUncommitted: false);
    }

    private static CampaignPlayState ApplyStaffCorrectionRetreats(
        CampaignPlayState state,
        PlayMap map,
        CampaignBattle battle,
        DateTimeOffset utcNow)
    {
        var retreats = state.Retreats.ToList();
        var log = new List<PlayLogEntry>();
        var occupied = retreats.Select(static item => item.TargetTerritoryId).ToHashSet();
        foreach (var forceId in ForcesRequiredToRetreat(battle))
        {
            if (retreats.Any(item => item.BattleId == battle.Id && item.ForceId == forceId))
            {
                continue;
            }

            var force = state.Forces.FirstOrDefault(item => item.Id == forceId);
            if (force is null)
            {
                continue;
            }

            var target = PickSafestRetreat(map, force, occupied);
            occupied.Add(target);
            retreats.Add(new RetreatOrder(
                Guid.NewGuid(),
                battle.Id,
                force.Id,
                target,
                true,
                utcNow,
                isStaffCorrection: true));
            log.Add(new PlayLogEntry(
                Guid.NewGuid(),
                utcNow,
                PlayLogKind.DefaultRetreat,
                battle.BattleWindowId,
                force.Id,
                force.ControllerUserId,
                battle.TerritoryId,
                target,
                battle.Id,
                ActionKind.Retreat,
                [force.Id]));
        }

        return state.With(retreats: retreats).AppendLog([.. log]);
    }

    private static bool TryOpenBattle(
        CampaignPlayState state,
        Guid userId,
        Guid battleId,
        DateTimeOffset utcNow,
        [NotNullWhen(true)] out CampaignBattle? battle,
        [NotNullWhen(false)] out DomainError? error,
        bool isStaff = false)
    {
        battle = state.Battles.FirstOrDefault(item => item.Id == battleId);
        error = null;
        var window = state.CurrentWindow();
        if (window is null || window.Status != PhaseWindowStatus.Open)
        {
            error = new DomainError("battle.window.closed", "The battle phase is not open.");
            return false;
        }

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

        if (isStaff)
        {
            _ = utcNow;
            return true;
        }

        if (battle.IsRinger && battle.InitiatingGmUserId == userId)
        {
            return true;
        }

        var participantIds = battle.ReportingForceIds;
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
        IReadOnlyList<ForceStatusSetup>? statuses,
        SpecialRuleContext? specialRules,
        DateTimeOffset utcNow)
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
        var applied = ForceStatusRules.Apply(state.Forces, catalog, facts, specialRules);
        var changes = DetectStatusChanges(
            state.Forces,
            applied,
            catalog,
            utcNow,
            static (previous, next) => (next.Id, next.FactionId, (Guid?)next.ControllerUserId));
        return state.With(
            forces: applied,
            forceStatusChanges: changes.Count == 0 ? state.ForceStatusChanges : [.. state.ForceStatusChanges, .. changes]);
    }

    private static CampaignPlayState ApplyBattleStatuses(
        CampaignPlayState state,
        PlayMap map,
        PhaseWindow window,
        IReadOnlyList<ForceStatusSetup>? statuses,
        SpecialRuleContext? specialRules,
        DateTimeOffset utcNow)
    {
        var catalog = statuses ?? [];
        var rules = specialRules ?? SpecialRuleContext.None;
        if (catalog.Count == 0 && !rules.AnyoneHas(SpecialRuleEffectKeys.BringersOfThePlague))
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
                fought.Any(item => !item.IsNoContest),
                fought.Any(item => !item.IsNoContest && item.WinnerForceId == force.Id),
                fought.Any(item => !item.IsNoContest && !item.IsDraw && item.WinnerForceId != force.Id),
                retreated.Contains(force.Id) && fought.Any(item => !item.IsNoContest),
                map.Territory(force.TerritoryId)?.IsWaterFeature == true);
        }

        var applied = catalog.Count == 0
            ? state.Forces
            : ForceStatusRules.Apply(state.Forces, catalog, facts, rules);
        var catalogChanges = DetectStatusChanges(
            state.Forces,
            applied,
            catalog,
            utcNow,
            static (previous, next) => (next.Id, next.FactionId, (Guid?)next.ControllerUserId));
        var byId = applied.ToDictionary(static force => force.Id);
        var inflictedActors = new Dictionary<Guid, CampaignForce>();
        foreach (var battle in battles.Where(static item =>
                     !item.IsDraw && !item.IsNoContest && item.WinnerForceId is not null))
        {
            var winner = byId.GetValueOrDefault(battle.WinnerForceId!.Value);
            if (winner is null)
            {
                continue;
            }

            foreach (var loserId in battle.ParticipantForceIds.Where(id => id != winner.Id))
            {
                var loser = byId.GetValueOrDefault(loserId);
                if (loser is null)
                {
                    continue;
                }

                var inflicted = FactionSpecialRulePolicies.StatusInflictedOnLoser(winner, loser, rules);
                if (inflicted is null || !FactionSpecialRulePolicies.AllowsStatus(loser, inflicted, rules))
                {
                    continue;
                }

                byId[loser.Id] = loser.WithStatus(inflicted);
                inflictedActors[loser.Id] = winner;
            }
        }

        var final = applied.Select(force => byId[force.Id]).ToArray();
        var inflictedChanges = DetectStatusChanges(
            applied,
            final,
            catalog,
            utcNow,
            (previous, next) =>
            {
                if (inflictedActors.TryGetValue(next.Id, out var winner))
                {
                    return (winner.Id, winner.FactionId, (Guid?)winner.ControllerUserId);
                }

                return (next.Id, next.FactionId, (Guid?)next.ControllerUserId);
            });
        return state.With(
            forces: final,
            forceStatusChanges: catalogChanges.Count + inflictedChanges.Count == 0
                ? state.ForceStatusChanges
                : [.. state.ForceStatusChanges, .. catalogChanges, .. inflictedChanges]);
    }

    private static List<ForceStatusChangeFact> DetectStatusChanges(
        IReadOnlyList<CampaignForce> before,
        IReadOnlyList<CampaignForce> after,
        IReadOnlyList<ForceStatusSetup> catalog,
        DateTimeOffset utcNow,
        Func<CampaignForce, CampaignForce, (Guid? ActorForceId, Guid ActorFactionId, Guid? ActorUserId)> actorFor)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(actorFor);
        var byName = catalog.ToDictionary(static status => status.Name, static status => status.Id, StringComparer.OrdinalIgnoreCase);
        var previousById = before.ToDictionary(static force => force.Id);
        var facts = new List<ForceStatusChangeFact>();
        foreach (var next in after.OrderBy(static force => force.Id))
        {
            if (!previousById.TryGetValue(next.Id, out var previous)
                || string.Equals(previous.StatusName, next.StatusName, StringComparison.Ordinal))
            {
                continue;
            }

            var actor = actorFor(previous, next);
            facts.Add(new ForceStatusChangeFact(
                Guid.NewGuid(),
                next.Id,
                next.FactionId,
                next.ControllerUserId,
                next.StatusName is { } name && byName.TryGetValue(name, out var statusId) ? statusId : null,
                previous.StatusName,
                next.StatusName,
                actor.ActorForceId,
                actor.ActorFactionId,
                actor.ActorUserId,
                utcNow,
                previous.StatusName is { } previousName && byName.TryGetValue(previousName, out var previousId)
                    ? previousId
                    : null));
        }

        return facts;
    }

    private static IReadOnlyList<Guid> ForcesRequiredToRetreat(CampaignBattle battle)
    {
        if (battle.IsNoContest)
        {
            return battle.SurrenderedForceIds.Count > 0
                ? battle.SurrenderedForceIds
                : battle.ReportingForceIds;
        }

        var reporting = battle.ReportingForceIds;
        if (battle.IsDraw)
        {
            return reporting;
        }

        return [.. reporting.Where(forceId => forceId != battle.WinnerForceId)];
    }

    private static CampaignPlayState ResolveSurrenderedBattle(
        CampaignPlayState state,
        CampaignBattle battle,
        PlayMap map,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        BattleScoringSetup scoring,
        DateTimeOffset utcNow)
    {
        _ = map;
        var fighting = state.Forces
            .Where(force =>
                battle.ParticipantForceIds.Contains(force.Id)
                && !battle.SurrenderedForceIds.Contains(force.Id)
                && !state.Retreats.Any(item => item.BattleId == battle.Id && item.ForceId == force.Id))
            .ToArray();
        var sides = BattleMatchRules.Sides(fighting, factionAllyGroups, state.BrokenAllyFactionIds);
        if (sides.Count == 0)
        {
            var noContest = battle.With(
                status: BattleStatus.Finalized,
                isDraw: false,
                clearWinner: true,
                isNoContest: true,
                winnerScore: 0,
                loserScore: 0,
                assignScores: true);
            return AfterMatchResolved(
                ApplyBattleSpoils(state.With(battles: ReplaceBattle(state.Battles, noContest)), noContest, utcNow)
                    .AppendLog(BattleEntry(PlayLogKind.BattleFinalized, noContest, utcNow)),
                noContest,
                utcNow,
                map,
                catalog: null,
                factionAllyGroups,
                pickIndex: null,
                parkForNextBattlePhase: false);
        }

        if (sides.Count == 1)
        {
            var winner = sides[0][0];
            var maxPoints = scoring.DifferentialMaximum;
            var won = battle.With(
                status: BattleStatus.Finalized,
                winnerForceId: winner.Id,
                isDraw: false,
                winnerScore: maxPoints,
                loserScore: 0,
                assignScores: true,
                isNoContest: false);
            return AfterMatchResolved(
                ApplyBattleSpoils(state.With(battles: ReplaceBattle(state.Battles, won)), won, utcNow)
                    .AppendLog(BattleEntry(PlayLogKind.BattleFinalized, won, utcNow)),
                won,
                utcNow,
                map,
                catalog: null,
                factionAllyGroups,
                pickIndex: null,
                parkForNextBattlePhase: false);
        }

        var active = BattleMatchRules.NextActiveForceIds(
            fighting,
            factionAllyGroups,
            state.BrokenAllyFactionIds,
            force => StrengthOf(force, state, map),
            static _ => 0);
        var waiting = fighting.Select(static force => force.Id).Except(active).ToArray();
        var continued = battle.With(activeForceIds: active, waitingForceIds: waiting);
        return state.With(battles: ReplaceBattle(state.Battles, continued));
    }

    private static CampaignPlayState ApplyDeadlineSurrenders(
        CampaignPlayState state,
        PlayMap map,
        PhaseWindow window,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        DateTimeOffset closeAt,
        bool due)
    {
        var next = state;
        var affected = new HashSet<Guid>();
        foreach (var force in state.Forces.Where(static item => item.InBattle).OrderBy(static item => item.Id))
        {
            var committed = next.Commitments.Any(item => item.WindowId == window.Id && item.UserId == force.ControllerUserId);
            if (!due && !committed)
            {
                continue;
            }

            var draft = next.DraftFor(window.Id, force.Id);
            if (draft is null || draft.Kind != ActionKind.Surrender || draft.TargetTerritoryId is not { } target)
            {
                continue;
            }

            var battle = next.Battles.FirstOrDefault(item =>
                item.ParticipantForceIds.Contains(force.Id)
                && item.Status is not BattleStatus.Finalized and not BattleStatus.GMResolved);
            if (battle is null)
            {
                continue;
            }

            if (TryCommitSurrender(
                next,
                map,
                force.ControllerUserId,
                battle.Id,
                target,
                closeAt,
                window.Id,
                out var outcome,
                out _,
                factionAllyGroups: factionAllyGroups,
                resolveImmediately: false)
                && outcome is not null)
            {
                next = outcome.State;
                affected.Add(battle.Id);
            }
        }

        foreach (var battleId in affected)
        {
            var battle = next.Battles.FirstOrDefault(item => item.Id == battleId);
            if (battle is null || battle.Status is BattleStatus.Finalized or BattleStatus.GMResolved)
            {
                continue;
            }

            next = ResolveSurrenderedBattle(next, battle, map, factionAllyGroups, BattleScoringSetup.Default, closeAt);
        }

        return next;
    }

    private static CampaignPlayState AssignOpeningMatches(
        CampaignPlayState state,
        PlayMap map,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        Func<int, int> pickIndex)
    {
        var battles = new List<CampaignBattle>();
        var changed = false;
        foreach (var battle in state.Battles)
        {
            if (battle.ActiveForceIds.Count > 0 || battle.WaitingForceIds.Count > 0
                || battle.Status is BattleStatus.Finalized or BattleStatus.GMResolved)
            {
                battles.Add(battle);
                continue;
            }

            var fighting = state.Forces.Where(force => battle.ParticipantForceIds.Contains(force.Id)).ToArray();
            var active = BattleMatchRules.NextActiveForceIds(
                fighting,
                factionAllyGroups,
                state.BrokenAllyFactionIds,
                force => StrengthOf(force, state, map),
                pickIndex);
            var waiting = fighting.Select(static force => force.Id).Except(active).ToArray();
            if (waiting.Length == 0 && active.Count == fighting.Length)
            {
                battles.Add(battle);
                continue;
            }

            changed = true;
            battles.Add(battle.With(activeForceIds: active, waitingForceIds: waiting));
        }

        return changed ? state.With(battles: battles) : state;
    }

    private static CampaignPlayState AfterMatchResolved(
        CampaignPlayState state,
        CampaignBattle resolved,
        DateTimeOffset utcNow,
        PlayMap? map,
        SupplyCatalog? catalog,
        IReadOnlyDictionary<Guid, string?>? factionAllyGroups,
        Func<int, int>? pickIndex,
        bool parkForNextBattlePhase)
    {
        var spent = SpendReportedSupply(state, resolved, map, catalog);
        if (resolved.WaitingForceIds.Count == 0 || resolved.IsNoContest)
        {
            return spent;
        }

        var remainingIds = resolved.WaitingForceIds.ToList();
        if (!resolved.IsDraw && resolved.WinnerForceId is { } winnerId)
        {
            remainingIds.Add(winnerId);
        }

        remainingIds = [.. remainingIds.Distinct()];
        if (remainingIds.Count == 0)
        {
            return spent.With(battles: ReplaceBattle(spent.Battles, resolved.With(waitingForceIds: [])));
        }

        var remainingForces = spent.Forces.Where(force => remainingIds.Contains(force.Id)).ToArray();
        var allies = factionAllyGroups ?? new Dictionary<Guid, string?>();
        var sides = BattleMatchRules.Sides(remainingForces, allies, spent.BrokenAllyFactionIds);
        var choose = pickIndex ?? (static count => 0);
        IReadOnlyList<Guid> active = [];
        IReadOnlyList<Guid> waiting = remainingIds;
        var followUpStatus = BattleStatus.Pending;
        Guid? followUpWindow = null;
        if (map is not null && sides.Count >= 2 && remainingForces.Length >= 2)
        {
            active = BattleMatchRules.NextActiveForceIds(
                remainingForces,
                allies,
                spent.BrokenAllyFactionIds,
                force => StrengthOf(force, spent, map),
                choose);
            waiting = [.. remainingIds.Except(active)];
            if (!parkForNextBattlePhase)
            {
                followUpStatus = BattleStatus.AwaitingResults;
                followUpWindow = resolved.BattleWindowId;
            }
        }

        var followUp = new CampaignBattle(
            Guid.NewGuid(),
            resolved.TerritoryId,
            resolved.SourceWindowId,
            followUpWindow,
            followUpStatus,
            remainingIds,
            winnerForceId: null,
            isDraw: false,
            utcNow,
            activeForceIds: active,
            waitingForceIds: waiting);
        var cleared = resolved.With(waitingForceIds: []);
        var nextForces = spent.Forces
            .Select(force => remainingIds.Contains(force.Id) ? force.With(inBattle: true) : force)
            .ToArray();
        return spent
            .With(
                forces: nextForces,
                battles: [.. ReplaceBattle(spent.Battles, cleared), followUp])
            .AppendLog(new PlayLogEntry(
                Guid.NewGuid(),
                utcNow,
                PlayLogKind.BattleMatchAdvanced,
                followUpWindow ?? resolved.BattleWindowId,
                resolved.WinnerForceId,
                actorUserId: null,
                resolved.TerritoryId,
                targetTerritoryId: null,
                followUp.Id,
                ActionKind.Battle,
                remainingIds));
    }

    private static CampaignPlayState SpendReportedSupply(
        CampaignPlayState state,
        CampaignBattle resolved,
        PlayMap? map,
        SupplyCatalog? catalog)
    {
        if (map is null || catalog is null)
        {
            return state;
        }

        var current = CurrentSubmissions(state, resolved);
        var reports = current.Count > 0
            ? current[0].Reports
            : state.BattleSubmissions
                .Where(item => item.BattleId == resolved.Id)
                .OrderByDescending(static item => item.SubmittedUtc)
                .Select(static item => item.Reports)
                .FirstOrDefault()
                ?? [];
        if (reports.Count == 0)
        {
            return state;
        }

        var round = state.CurrentWindow()?.RoundNumber
            ?? (state.Windows.Count > 0 ? state.Windows[^1].RoundNumber : 1);
        var tempByPlayer = new Dictionary<Guid, List<int>>();
        foreach (var report in reports)
        {
            var force = state.Forces.FirstOrDefault(item => item.Id == report.ForceId);
            if (force is null)
            {
                continue;
            }

            var snapshot = SupplyRules.ForPlayer(state, map, catalog, force.ControllerUserId, round);
            var (_, temporary) = SupplyRules.AllocateSpend(report.SupplySpend, snapshot.ForceAllowancePoints);
            if (!tempByPlayer.TryGetValue(force.ControllerUserId, out var requested))
            {
                requested = [];
                tempByPlayer[force.ControllerUserId] = requested;
            }

            requested.Add(temporary);
        }

        var supplies = state.PlayerSupplies;
        foreach (var pair in tempByPlayer)
        {
            supplies = SupplyRules.SpendTemporary(supplies, pair.Key, pair.Value);
        }

        return state.With(playerSupplies: supplies);
    }

    private static bool TryValidateBattleSpecialRuleUses(
        CampaignPlayState state,
        IReadOnlyList<BattleParticipantReport> reports,
        PlayMap? map,
        SupplyCatalog? catalog,
        [NotNullWhen(false)] out DomainError? error)
    {
        Dictionary<Guid, int>? leftover = null;
        if (map is not null && catalog is not null)
        {
            leftover = [];
            var round = state.CurrentWindow()?.RoundNumber
                ?? (state.Windows.Count > 0 ? state.Windows[^1].RoundNumber : 1);
            foreach (var report in reports)
            {
                var force = state.Forces.FirstOrDefault(item => item.Id == report.ForceId);
                if (force is null)
                {
                    continue;
                }

                var snapshot = SupplyRules.ForPlayer(state, map, catalog, force.ControllerUserId, round);
                leftover[force.Id] = Math.Max(0, snapshot.CurrentSupplyPoints - report.SupplyCostingUnitCount);
            }
        }

        return BattleResultRules.TryValidateSpecialRuleUses(
            reports,
            state.Forces,
            catalog?.SpecialRules ?? SpecialRuleContext.None,
            leftover,
            out error);
    }

    private static void ResolveRetreatCollisions(
        Dictionary<Guid, CampaignForce> forces,
        PlayMap map,
        CampaignPlayState state,
        Func<int, int> pickIndex,
        DateTimeOffset utcNow,
        out List<PlayLogEntry> log)
    {
        log = [];
        var occupied = new Dictionary<Guid, List<CampaignForce>>();
        foreach (var force in forces.Values)
        {
            if (!occupied.TryGetValue(force.TerritoryId, out var list))
            {
                list = [];
                occupied[force.TerritoryId] = list;
            }

            list.Add(force);
        }

        var allyGroups = new Dictionary<Guid, string?>();
        foreach (var group in occupied.Where(static pair => pair.Value.Count > 1))
        {
            var sides = BattleMatchRules.Sides(group.Value, allyGroups, state.BrokenAllyFactionIds);
            if (sides.Count < 2)
            {
                continue;
            }

            var ranked = CombatantStrengthRules.Rank(
                group.Value,
                force => StrengthOf(force, state, map),
                pickIndex);
            var keeper = ranked[0];
            var blocked = occupied.Keys.ToHashSet();
            foreach (var displaced in ranked.Skip(1))
            {
                if (!ActionResolution.AreEnemies(keeper.FactionId, displaced.FactionId, allyGroups, state.BrokenAllyFactionIds))
                {
                    continue;
                }

                var target = PickSafestRetreat(map, displaced, blocked);
                blocked.Add(target);
                forces[displaced.Id] = displaced.With(territoryId: target, inBattle: false);
                log.Add(new PlayLogEntry(
                    Guid.NewGuid(),
                    utcNow,
                    PlayLogKind.RetreatCollisionResolved,
                    state.CurrentWindow()?.Id,
                    displaced.Id,
                    displaced.ControllerUserId,
                    group.Key,
                    target,
                    battleId: null,
                    ActionKind.Retreat,
                    [keeper.Id, displaced.Id]));
            }
        }
    }

    private static CombatantStrengthRules.Strength StrengthOf(
        CampaignForce force,
        CampaignPlayState state,
        PlayMap map)
    {
        var (Territories, Structures) = CombatantStrengthRules.Holdings(map, force.FactionId);
        var points = 0;
        foreach (var battle in state.Battles)
        {
            if (battle.Status is not BattleStatus.Finalized and not BattleStatus.GMResolved || battle.IsNoContest)
            {
                continue;
            }

            if (battle.WinnerForceId is { } winnerId
                && state.Forces.FirstOrDefault(item => item.Id == winnerId)?.ControllerUserId == force.ControllerUserId)
            {
                points += battle.WinnerScore ?? 1;
            }
        }

        var supply = state.PlayerSupplies.FirstOrDefault(item => item.UserId == force.ControllerUserId)?.TemporarySupplyPoints ?? 0;
        supply += Territories + Structures;
        return new CombatantStrengthRules.Strength(points, Territories, Structures, supply);
    }

    private static BattleResultSubmission? LatestConfirmableSubmission(
        CampaignPlayState state,
        CampaignBattle battle,
        Guid userId)
    {
        return state.BattleSubmissions
            .Where(item => item.BattleId == battle.Id && item.SubmitterUserId != userId)
            .OrderByDescending(static item => item.SubmittedUtc)
            .FirstOrDefault();
    }

    private static Guid PickSafestRetreat(PlayMap map, CampaignForce force, HashSet<Guid> occupied)
    {
        var spawn = map.SpawnFor(force.FactionId);
        PlayTerritory? best = null;
        var bestRank = int.MaxValue;
        foreach (var id in EligibleRetreats(map, force))
        {
            var territory = map.Territory(id);
            if (territory is null)
            {
                continue;
            }

            var rank = 3;
            if (territory.OwnerFactionId == force.FactionId && !occupied.Contains(id))
            {
                rank = 0;
            }
            else if (!occupied.Contains(id))
            {
                rank = 1;
            }
            else if (spawn is not null && territory.Id == spawn.Id)
            {
                rank = 2;
            }

            if (rank < bestRank || (rank == bestRank && (best is null || territory.DisplayNumber < best.DisplayNumber)))
            {
                best = territory;
                bestRank = rank;
            }
        }

        return best?.Id ?? spawn?.Id ?? force.TerritoryId;
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
                force.StatusName,
                force.Subfaction))],
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
                captured.Condition,
                territory.IsPillageable,
                territory.IsDestructible,
                territory.IsWaterFeature,
                territory.TerrainTypeId,
                territory.SpawnSubfaction);
        }).ToArray();
        return map.WithTerritories(next);
    }
}

/// <summary>
/// A player and the faction they have chosen.
/// </summary>
/// <param name="UserId">The player.</param>
/// <param name="FactionId">The chosen faction, if any.</param>
/// <param name="Subfaction">The chosen subfaction, if any.</param>
public sealed record PlayerFactionAssignment(Guid UserId, Guid? FactionId, string? Subfaction = null);

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
