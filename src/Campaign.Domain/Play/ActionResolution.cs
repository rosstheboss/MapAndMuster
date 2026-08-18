using Campaign.Domain.Campaigns;

namespace Campaign.Domain.Play;

/// <summary>
/// Resolves one action window simultaneously against the starting map.
/// Movement and splits apply first, then backstab alliance breaks, then battles from
/// enemy co-location. Build, Pillage, and Repair apply afterward for forces not in battle.
/// Competing structure actions on the same territory become Hold.
/// </summary>
public static class ActionResolution
{
    /// <summary>Maximum forces one player may control.</summary>
    public const int MaxForcesPerPlayer = 2;

    /// <summary>
    /// Applies submitted orders, updates forces and ownership, and creates battles.
    /// </summary>
    public static (CampaignPlayState State, PlayMap Map) Resolve(
        CampaignPlayState state,
        PlayMap map,
        PhaseWindow window,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(factionAllyGroups);

        var forces = state.Forces.ToDictionary(static force => force.Id);
        var acting = forces.Values
            .Where(force => !force.InBattle)
            .OrderBy(static force => force.Id)
            .ToArray();
        var resolved = new Dictionary<Guid, ResolvedOrder>();
        foreach (var force in acting)
        {
            resolved[force.Id] = Normalize(state, map, window, force, factionAllyGroups);
        }

        DisallowConflictingStructureActions(resolved);
        var log = new List<PlayLogEntry>(state.Log);
        foreach (var force in acting)
        {
            var order = resolved[force.Id];
            var submission = state.LatestSubmission(window.Id, force.Id);
            AppendResolvedActionLog(log, window, force, submission, order, utcNow);
        }

        var nextForces = new List<CampaignForce>();
        var occupied = new Dictionary<Guid, List<Guid>>();
        var moveOrigins = new Dictionary<Guid, Guid>();
        foreach (var force in state.Forces.OrderBy(static item => item.Id))
        {
            if (force.InBattle)
            {
                nextForces.Add(force);
                AddOccupied(occupied, force.TerritoryId, force.Id);
                continue;
            }

            if (!resolved.TryGetValue(force.Id, out var order))
            {
                nextForces.Add(force);
                AddOccupied(occupied, force.TerritoryId, force.Id);
                continue;
            }

            if (order.Kind == ActionKind.Split && order.TargetTerritoryId is { } splitTarget)
            {
                nextForces.Add(force);
                AddOccupied(occupied, force.TerritoryId, force.Id);
                var split = new CampaignForce(Guid.NewGuid(), force.ControllerUserId, force.FactionId, splitTarget, false);
                nextForces.Add(split);
                AddOccupied(occupied, splitTarget, split.Id);
                continue;
            }

            var destination = order.Kind is ActionKind.Move or ActionKind.Retreat
                ? order.TargetTerritoryId ?? force.TerritoryId
                : force.TerritoryId;
            if (order.Kind is ActionKind.Move or ActionKind.Retreat && destination != force.TerritoryId)
            {
                moveOrigins[force.Id] = force.TerritoryId;
            }

            var moved = force.With(territoryId: destination);
            nextForces.Add(moved);
            AddOccupied(occupied, destination, moved.Id);
        }

        nextForces = Rejoin(nextForces, window, utcNow, log);
        occupied = [];
        foreach (var force in nextForces)
        {
            AddOccupied(occupied, force.TerritoryId, force.Id);
        }

        var broken = state.BrokenAllyFactionIds.ToHashSet();
        foreach (var order in resolved.Values)
        {
            if (order.Kind == ActionKind.Backstab && forces.TryGetValue(order.ForceId, out var force))
            {
                broken.Add(force.FactionId);
            }
        }

        var battles = state.Battles.ToList();
        var inBattle = new HashSet<Guid>();
        foreach (var (territoryId, forceIds) in occupied)
        {
            var present = forceIds
                .Select(id => nextForces.First(force => force.Id == id))
                .ToArray();
            if (CreatesBattle(present, factionAllyGroups, broken))
            {
                var presentIds = present.Select(static force => force.Id).ToArray();
                var existing = battles.FirstOrDefault(item =>
                    item.TerritoryId == territoryId
                    && item.Status is not BattleStatus.Finalized and not BattleStatus.GMResolved);
                if (existing is not null)
                {
                    var mergedIds = existing.ParticipantForceIds.Concat(presentIds).Distinct().ToArray();
                    var newcomers = presentIds.Except(existing.ParticipantForceIds).ToArray();
                    var keepPairing = existing.ActiveForceIds.Count > 0;
                    var updated = existing.With(
                        participantForceIds: mergedIds,
                        waitingForceIds: keepPairing
                            ? existing.WaitingForceIds.Concat(newcomers).Distinct().ToArray()
                            : [],
                        activeForceIds: keepPairing ? existing.ActiveForceIds : []);
                    var index = battles.FindIndex(item => item.Id == existing.Id);
                    battles[index] = updated;
                    foreach (var force in present)
                    {
                        inBattle.Add(force.Id);
                    }

                    continue;
                }

                var battleWindow = NextBattleWindow(state, window);
                var battle = new CampaignBattle(
                    Guid.NewGuid(),
                    territoryId,
                    window.Id,
                    battleWindow?.Id,
                    BattleStatus.Pending,
                    presentIds,
                    winnerForceId: null,
                    isDraw: false,
                    utcNow);
                battles.Add(battle);
                log.Add(new PlayLogEntry(
                    Guid.NewGuid(),
                    utcNow,
                    PlayLogKind.BattleCreated,
                    window.Id,
                    forceId: null,
                    actorUserId: null,
                    territoryId,
                    targetTerritoryId: null,
                    battle.Id,
                    ActionKind.Battle,
                    battle.ParticipantForceIds));
                foreach (var force in present)
                {
                    inBattle.Add(force.Id);
                }
            }
        }

        nextForces =
        [
            .. nextForces.Select(force => force.With(inBattle: inBattle.Contains(force.Id) || force.InBattle)),
        ];

        var items = ItemObjectiveRules.DropCarriedByMovers(state.ItemObjectives, moveOrigins, utcNow, log);
        items = ItemObjectiveRules.PickUpUnpossessed(items, nextForces, utcNow, log);

        var nextMap = ApplyTerritoryEffects(map, nextForces, resolved, inBattle);
        return (
            state.With(
                forces: nextForces,
                battles: battles,
                brokenAllyFactionIds: [.. broken.OrderBy(static id => id)],
                structures: CaptureStructures(nextMap),
                itemObjectives: items,
                log: log),
            nextMap);
    }

    /// <summary>
    /// Player-submittable actions available for a force in an open action window, in documented order:
    /// Hold, Move, Build, Pillage, Repair, Split, then Backstab.
    /// </summary>
    public static IReadOnlyList<ActionKind> EligibleActions(
        CampaignPlayState state,
        PlayMap map,
        CampaignForce force,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(force);
        ArgumentNullException.ThrowIfNull(factionAllyGroups);
        if (force.InBattle)
        {
            return [ActionKind.Surrender];
        }

        var kinds = new List<ActionKind> { ActionKind.Hold };
        var moves = CampaignPlayRules.EligibleMoves(map, force);
        if (moves.Count > 0)
        {
            kinds.Add(ActionKind.Move);
        }

        if (map.HasBuildableStructure && CanBuildInTerritory(map, force))
        {
            kinds.Add(ActionKind.Build);
        }

        if (IsValidPillage(map, force))
        {
            kinds.Add(ActionKind.Pillage);
        }

        if (IsValidRepair(map, force))
        {
            kinds.Add(ActionKind.Repair);
        }

        if (moves.Any(target => IsValidSplit(state, map, force, target)))
        {
            kinds.Add(ActionKind.Split);
        }

        if (IsValidBackstab(force, factionAllyGroups, state.BrokenAllyFactionIds))
        {
            kinds.Add(ActionKind.Backstab);
        }

        return kinds;
    }

    internal static IReadOnlyList<TerritoryStructureState> CaptureStructures(PlayMap map)
    {
        return
        [
            .. map.Territories.Select(static territory =>
                new TerritoryStructureState(territory.Id, territory.StructureTypeId, territory.StructureCondition)),
        ];
    }

    private static ResolvedOrder Normalize(
        CampaignPlayState state,
        PlayMap map,
        PhaseWindow window,
        CampaignForce force,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups)
    {
        var submission = state.LatestSubmission(window.Id, force.Id);
        var kind = submission?.Kind ?? ActionKind.Hold;
        var target = submission?.TargetTerritoryId;
        var structureTypeId = submission?.StructureTypeId;
        if (kind == ActionKind.Move && !IsValidMove(map, force, target))
        {
            return Hold(force, OrderAdjustment.InvalidOrder);
        }

        if (kind == ActionKind.Split && !IsValidSplit(state, map, force, target))
        {
            return Hold(force, OrderAdjustment.InvalidOrder);
        }

        if (kind == ActionKind.Build && !IsValidBuild(map, force, structureTypeId))
        {
            return Hold(force, OrderAdjustment.InvalidOrder);
        }

        if (kind == ActionKind.Pillage && !IsValidPillage(map, force))
        {
            return Hold(force, OrderAdjustment.InvalidOrder);
        }

        if (kind == ActionKind.Repair && !IsValidRepair(map, force))
        {
            return Hold(force, OrderAdjustment.InvalidOrder);
        }

        if (kind == ActionKind.Backstab && !IsValidBackstab(force, factionAllyGroups, state.BrokenAllyFactionIds))
        {
            return Hold(force, OrderAdjustment.InvalidOrder);
        }

        if (kind == ActionKind.Retreat)
        {
            return Hold(force, OrderAdjustment.InvalidOrder);
        }

        if (kind == ActionKind.Build)
        {
            return new ResolvedOrder(force.Id, kind, force.TerritoryId, structureTypeId);
        }

        if (kind is ActionKind.Hold or ActionKind.Pillage or ActionKind.Repair or ActionKind.Backstab)
        {
            return new ResolvedOrder(force.Id, kind, force.TerritoryId, structureTypeId);
        }

        return new ResolvedOrder(force.Id, kind, target, structureTypeId);
    }

    private static void DisallowConflictingStructureActions(Dictionary<Guid, ResolvedOrder> resolved)
    {
        var structureActions = resolved.Values
            .Where(static order => order.Kind is ActionKind.Build or ActionKind.Pillage or ActionKind.Repair)
            .GroupBy(static order => order.TargetTerritoryId);
        foreach (var group in structureActions)
        {
            if (group.Count() <= 1)
            {
                continue;
            }

            foreach (var order in group)
            {
                resolved[order.ForceId] = new ResolvedOrder(
                    order.ForceId,
                    ActionKind.Hold,
                    order.TargetTerritoryId,
                    null,
                    OrderAdjustment.ConflictingBuild);
            }
        }
    }

    private static bool IsValidMove(PlayMap map, CampaignForce force, Guid? targetId)
    {
        if (targetId is null || targetId == force.TerritoryId || !map.AreAdjacent(force.TerritoryId, targetId.Value))
        {
            return false;
        }

        var target = map.Territory(targetId.Value);
        return target is not null && (target.SpawnFactionId is null || target.SpawnFactionId == force.FactionId);
    }

    private static bool IsValidSplit(CampaignPlayState state, PlayMap map, CampaignForce force, Guid? targetId)
    {
        if (state.Forces.Count(item => item.ControllerUserId == force.ControllerUserId) >= MaxForcesPerPlayer)
        {
            return false;
        }

        return IsValidMove(map, force, targetId);
    }

    internal static bool CanBuildInTerritory(PlayMap map, CampaignForce force)
    {
        var territory = map.Territory(force.TerritoryId);
        if (territory is null || territory.IsSpawn)
        {
            return false;
        }

        return territory.StructureTypeId is null || territory.StructureCondition == StructureCondition.Destroyed;
    }

    private static bool IsValidBuild(PlayMap map, CampaignForce force, Guid? structureTypeId)
    {
        if (structureTypeId is null)
        {
            return false;
        }

        if (map.StructureTypes.Count > 0)
        {
            var rules = map.StructureRules(structureTypeId.Value);
            if (rules is null || !rules.IsBuildable)
            {
                return false;
            }
        }

        return CanBuildInTerritory(map, force);
    }

    internal static bool IsValidPillage(PlayMap map, CampaignForce force)
    {
        var territory = map.Territory(force.TerritoryId);
        if (territory?.StructureTypeId is null || territory.StructureCondition == StructureCondition.Destroyed)
        {
            return false;
        }

        if (territory.OwnerFactionId == force.FactionId)
        {
            return false;
        }

        if (!territory.IsPillageable)
        {
            return false;
        }

        if (territory.StructureCondition == StructureCondition.Pillaged && !territory.IsDestructible)
        {
            return false;
        }

        return true;
    }

    internal static bool IsValidRepair(PlayMap map, CampaignForce force)
    {
        var territory = map.Territory(force.TerritoryId);
        return territory is not null
            && territory.StructureTypeId is not null
            && territory.StructureCondition == StructureCondition.Pillaged
            && territory.OwnerFactionId == force.FactionId;
    }

    internal static bool IsValidBackstab(
        CampaignForce force,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlyList<Guid> broken)
    {
        if (broken.Contains(force.FactionId))
        {
            return false;
        }

        return factionAllyGroups.TryGetValue(force.FactionId, out var group) && !string.IsNullOrWhiteSpace(group);
    }

    private static bool CreatesBattle(
        CampaignForce[] present,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        HashSet<Guid> broken)
    {
        for (var i = 0; i < present.Length; i++)
        {
            for (var j = i + 1; j < present.Length; j++)
            {
                if (AreEnemies(present[i].FactionId, present[j].FactionId, factionAllyGroups, broken))
                {
                    return true;
                }
            }
        }

        return false;
    }

    internal static bool AreEnemies(
        Guid leftFactionId,
        Guid rightFactionId,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlyCollection<Guid> broken)
    {
        if (leftFactionId == rightFactionId)
        {
            return false;
        }

        if (broken.Contains(leftFactionId) || broken.Contains(rightFactionId))
        {
            return true;
        }

        if (!factionAllyGroups.TryGetValue(leftFactionId, out var leftGroup)
            || !factionAllyGroups.TryGetValue(rightFactionId, out var rightGroup))
        {
            return true;
        }

        return string.IsNullOrWhiteSpace(leftGroup)
            || string.IsNullOrWhiteSpace(rightGroup)
            || !string.Equals(leftGroup, rightGroup, StringComparison.Ordinal);
    }

    private static List<CampaignForce> Rejoin(
        List<CampaignForce> forces,
        PhaseWindow window,
        DateTimeOffset utcNow,
        List<PlayLogEntry> log)
    {
        var result = new List<CampaignForce>();
        foreach (var group in forces.GroupBy(static force => (force.ControllerUserId, force.TerritoryId)))
        {
            var members = group.OrderBy(static force => force.Id).ToArray();
            result.Add(members[0]);
            if (members.Length > 1)
            {
                log.Add(new PlayLogEntry(
                    Guid.NewGuid(),
                    utcNow,
                    PlayLogKind.ForcesRejoined,
                    window.Id,
                    members[0].Id,
                    members[0].ControllerUserId,
                    members[0].TerritoryId,
                    targetTerritoryId: null,
                    battleId: null,
                    actionKind: null,
                    [.. members.Select(static force => force.Id)]));
            }
        }

        return [.. result.OrderBy(static force => force.Id)];
    }

    private static PlayMap ApplyTerritoryEffects(
        PlayMap map,
        IReadOnlyList<CampaignForce> forces,
        IReadOnlyDictionary<Guid, ResolvedOrder> resolved,
        HashSet<Guid> inBattle)
    {
        var next = map.Territories.ToDictionary(static territory => territory.Id);
        foreach (var order in resolved.Values)
        {
            if (!forces.Any(force => force.Id == order.ForceId))
            {
                continue;
            }

            var force = forces.First(item => item.Id == order.ForceId);
            if (inBattle.Contains(force.Id))
            {
                continue;
            }

            var territory = next[force.TerritoryId];
            if (order.Kind == ActionKind.Build && order.StructureTypeId is { } structureTypeId)
            {
                var rules = map.StructureRules(structureTypeId);
                next[territory.Id] = territory.With(
                    ownerFactionId: force.FactionId,
                    structureTypeId: structureTypeId,
                    structureName: rules?.Name,
                    structureCondition: StructureCondition.Operational,
                    isPillageable: rules?.IsPillageable ?? territory.IsPillageable,
                    isDestructible: rules?.IsDestructible ?? territory.IsDestructible);
            }
            else if (order.Kind == ActionKind.Pillage)
            {
                if (territory.StructureCondition == StructureCondition.Operational)
                {
                    next[territory.Id] = territory.With(structureCondition: StructureCondition.Pillaged);
                }
                else if (territory.IsDestructible)
                {
                    next[territory.Id] = territory.With(clearStructure: true);
                }
            }
            else if (order.Kind == ActionKind.Repair)
            {
                next[territory.Id] = territory.With(structureCondition: StructureCondition.Operational);
            }
        }

        foreach (var territory in next.Values.ToArray())
        {
            if (territory.IsSpawn)
            {
                next[territory.Id] = territory.With(ownerFactionId: territory.SpawnFactionId);
                continue;
            }

            var occupants = forces.Where(force => force.TerritoryId == territory.Id).ToArray();
            if (occupants.Length == 0 || occupants.Any(force => inBattle.Contains(force.Id)))
            {
                continue;
            }

            var factions = occupants.Select(static force => force.FactionId).Distinct().ToArray();
            if (factions.Length != 1)
            {
                continue;
            }

            next[territory.Id] = territory.With(ownerFactionId: factions[0]);
        }

        return map.WithTerritories([.. next.Values.OrderBy(static territory => territory.DisplayNumber)]);
    }

    private static PhaseWindow? NextBattleWindow(CampaignPlayState state, PhaseWindow actionWindow)
    {
        var index = state.Windows.ToList().FindIndex(item => item.Id == actionWindow.Id);
        return state.Windows.Skip(index + 1).FirstOrDefault(static window => window.Kind == RoundPhaseKind.Battle);
    }

    private static void AddOccupied(Dictionary<Guid, List<Guid>> occupied, Guid territoryId, Guid forceId)
    {
        if (!occupied.TryGetValue(territoryId, out var list))
        {
            list = [];
            occupied[territoryId] = list;
        }

        list.Add(forceId);
    }

    private static void AppendResolvedActionLog(
        List<PlayLogEntry> log,
        PhaseWindow window,
        CampaignForce force,
        OrderSubmission? submission,
        ResolvedOrder order,
        DateTimeOffset utcNow)
    {
        var submittedKind = submission?.Kind ?? ActionKind.Hold;
        if (submission?.Source == OrderSource.DeadlineHold)
        {
            log.Add(Entry(
                utcNow,
                PlayLogKind.MissingOrderHold,
                window.Id,
                force,
                order.Kind,
                force.TerritoryId,
                order.TargetTerritoryId));
            return;
        }

        if (submission?.Source == OrderSource.DeadlineDraft)
        {
            log.Add(Entry(
                utcNow,
                PlayLogKind.DeadlineDraftSubmitted,
                window.Id,
                force,
                submittedKind,
                force.TerritoryId,
                submission.TargetTerritoryId));
        }

        if (order.Adjustment == OrderAdjustment.InvalidOrder)
        {
            log.Add(Entry(
                utcNow,
                PlayLogKind.InvalidOrderHold,
                window.Id,
                force,
                submittedKind,
                force.TerritoryId,
                submission?.TargetTerritoryId));
            return;
        }

        if (order.Adjustment == OrderAdjustment.ConflictingBuild)
        {
            log.Add(Entry(
                utcNow,
                PlayLogKind.ConflictingBuildHold,
                window.Id,
                force,
                submittedKind,
                force.TerritoryId,
                force.TerritoryId));
            return;
        }

        log.Add(Entry(
            utcNow,
            PlayLogKind.ResolvedAction,
            window.Id,
            force,
            order.Kind,
            force.TerritoryId,
            order.TargetTerritoryId));
    }

    private static PlayLogEntry Entry(
        DateTimeOffset utcNow,
        PlayLogKind kind,
        Guid windowId,
        CampaignForce force,
        ActionKind actionKind,
        Guid territoryId,
        Guid? targetTerritoryId)
    {
        return new PlayLogEntry(
            Guid.NewGuid(),
            utcNow,
            kind,
            windowId,
            force.Id,
            force.ControllerUserId,
            territoryId,
            targetTerritoryId,
            battleId: null,
            actionKind,
            [force.Id]);
    }

    private static ResolvedOrder Hold(CampaignForce force, OrderAdjustment adjustment)
    {
        return new ResolvedOrder(force.Id, ActionKind.Hold, force.TerritoryId, null, adjustment);
    }

    private sealed record ResolvedOrder(
        Guid ForceId,
        ActionKind Kind,
        Guid? TargetTerritoryId,
        Guid? StructureTypeId,
        OrderAdjustment Adjustment = OrderAdjustment.None);

    private enum OrderAdjustment
    {
        None = 0,
        InvalidOrder = 1,
        ConflictingBuild = 2,
    }
}
