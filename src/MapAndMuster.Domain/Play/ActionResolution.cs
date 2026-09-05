using MapAndMuster.Domain.Campaigns;

namespace MapAndMuster.Domain.Play;

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
        DateTimeOffset utcNow,
        IReadOnlyList<TerrainTypeSetup>? terrainTypes = null,
        IReadOnlyList<StructureTypeSetup>? structureTypes = null,
        Func<int, int>? pickIndex = null,
        SpecialRuleContext? specialRules = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(factionAllyGroups);
        var rules = specialRules ?? SpecialRuleContext.None;

        var forces = state.Forces.ToDictionary(static force => force.Id);
        var acting = forces.Values
            .Where(force => !force.InBattle)
            .OrderBy(static force => force.Id)
            .ToArray();
        var resolved = new Dictionary<Guid, ResolvedOrder>();
        foreach (var force in acting)
        {
            resolved[force.Id] = Normalize(state, map, window, force, factionAllyGroups, rules);
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
        var arrivalKinds = new Dictionary<Guid, ActionKind>();
        var skipClaimTerritories = new HashSet<Guid>();
        foreach (var force in state.Forces.OrderBy(static item => item.Id))
        {
            if (force.InBattle)
            {
                nextForces.Add(force);
                AddOccupied(occupied, force.TerritoryId, force.Id);
                arrivalKinds[force.Id] = ActionKind.Hold;
                continue;
            }

            if (!resolved.TryGetValue(force.Id, out var order))
            {
                nextForces.Add(force);
                AddOccupied(occupied, force.TerritoryId, force.Id);
                arrivalKinds[force.Id] = ActionKind.Hold;
                continue;
            }

            if (order.Kind == ActionKind.Split && order.TargetTerritoryId is { } splitTarget)
            {
                nextForces.Add(force);
                AddOccupied(occupied, force.TerritoryId, force.Id);
                arrivalKinds[force.Id] = ActionKind.Hold;
                var split = new CampaignForce(
                    Guid.NewGuid(),
                    force.ControllerUserId,
                    force.FactionId,
                    splitTarget,
                    false,
                    statusName: null,
                    force.Subfaction);
                nextForces.Add(split);
                AddOccupied(occupied, splitTarget, split.Id);
                arrivalKinds[split.Id] = ActionKind.Split;
                continue;
            }

            var destination = order.Kind is ActionKind.Move or ActionKind.Retreat
                ? FactionSpecialRulePolicies.ResolveMoveDestination(
                    map,
                    force,
                    order.TargetTerritoryId ?? force.TerritoryId,
                    order.ViaTerritoryId,
                    state.Forces,
                    factionAllyGroups,
                    state.BrokenAllyFactionIds,
                    state.BrokenAllySubfactions,
                    rules)
                : force.TerritoryId;
            if (order.Kind is ActionKind.Move or ActionKind.Retreat && destination != force.TerritoryId)
            {
                moveOrigins[force.Id] = force.TerritoryId;
            }

            var moved = force.With(territoryId: destination);
            nextForces.Add(moved);
            AddOccupied(occupied, destination, moved.Id);
            arrivalKinds[force.Id] = order.Kind;
            if (order.Kind == ActionKind.Move
                && order.ViaTerritoryId is { } via
                && via != destination
                && FactionSpecialRulePolicies.SkipClaiming(
                    force,
                    via,
                    force.TerritoryId,
                    destination,
                    via,
                    rules))
            {
                skipClaimTerritories.Add(via);
            }
        }

        nextForces = Rejoin(nextForces, window, utcNow, log, arrivalKinds, rules);
        occupied = [];
        foreach (var force in nextForces)
        {
            AddOccupied(occupied, force.TerritoryId, force.Id);
        }

        var broken = state.BrokenAllyFactionIds.ToHashSet();
        var brokenSubfactions = state.BrokenAllySubfactions.ToList();
        foreach (var order in resolved.Values)
        {
            if (order.Kind == ActionKind.Backstab && forces.TryGetValue(order.ForceId, out var force))
            {
                if (rules.Has(force, SpecialRuleEffectKeys.DividedWeStand)
                    && !string.IsNullOrWhiteSpace(force.Subfaction))
                {
                    if (!brokenSubfactions.Any(item =>
                        item.FactionId == force.FactionId
                        && string.Equals(item.Subfaction, force.Subfaction, StringComparison.OrdinalIgnoreCase)))
                    {
                        brokenSubfactions.Add(new BrokenAllySubfaction(force.FactionId, force.Subfaction));
                    }
                }
                else
                {
                    broken.Add(force.FactionId);
                }
            }
        }

        var battles = state.Battles.ToList();
        var inBattle = new HashSet<Guid>();
        foreach (var (territoryId, forceIds) in occupied)
        {
            var present = forceIds
                .Select(id => nextForces.First(force => force.Id == id))
                .ToArray();
            if (CreatesBattle(present, map.Territory(territoryId)!, factionAllyGroups, broken, brokenSubfactions, rules))
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
                var assignment = terrainTypes is null
                    ? null
                    : BattleMissionRules.Choose(
                        map.Territory(territoryId),
                        present,
                        arrivalKinds,
                        factionAllyGroups,
                        broken,
                        terrainTypes,
                        structureTypes ?? [],
                        pickIndex ?? (static count => 0));
                var battle = new CampaignBattle(
                    Guid.NewGuid(),
                    territoryId,
                    window.Id,
                    battleWindow?.Id,
                    BattleStatus.Pending,
                    presentIds,
                    winnerForceId: null,
                    isDraw: false,
                    utcNow,
                    missionId: assignment?.MissionId,
                    attackerForceId: assignment?.AttackerForceId,
                    defenderForceId: assignment?.DefenderForceId);
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

        var nextMap = ApplyTerritoryEffects(
            map,
            nextForces,
            resolved,
            inBattle,
            factionAllyGroups,
            broken,
            pickIndex ?? (static count => 0),
            skipClaimTerritories,
            rules,
            brokenSubfactions);
        return (
            state.With(
                forces: nextForces,
                battles: battles,
                brokenAllyFactionIds: [.. broken.OrderBy(static id => id)],
                brokenAllySubfactions: [.. brokenSubfactions.OrderBy(static item => item.FactionId).ThenBy(static item => item.Subfaction)],
                structures: CaptureStructures(nextMap),
                itemObjectives: items,
                log: log),
            nextMap);
    }

    /// <summary>
    /// Player-submittable actions available for a force in an open action window, in documented order:
    /// Hold, Move, Build, Pillage, Repair, Split, then Backstab.
    /// Kinds that are not legal for the force's current territory are omitted.
    /// </summary>
    public static IReadOnlyList<ActionKind> EligibleActions(
        CampaignPlayState state,
        PlayMap map,
        CampaignForce force,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        SpecialRuleContext? specialRules = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(force);
        ArgumentNullException.ThrowIfNull(factionAllyGroups);
        var rules = specialRules ?? SpecialRuleContext.None;
        if (force.InBattle)
        {
            // Surrender is committed from the battle panel, not as a required action-list item.
            return [ActionKind.Surrender];
        }

        var kinds = new List<ActionKind> { ActionKind.Hold };
        var moves = CampaignPlayRules.EligibleMoves(map, force, state.ItemObjectives, rules);
        if (moves.Count > 0)
        {
            kinds.Add(ActionKind.Move);
        }

        if (map.HasBuildableStructure && CanBuildInTerritory(map, force))
        {
            kinds.Add(ActionKind.Build);
        }

        if (IsValidPillage(map, force, factionAllyGroups, state.BrokenAllyFactionIds, rules, state.BrokenAllySubfactions))
        {
            kinds.Add(ActionKind.Pillage);
        }

        if (IsValidRepair(map, force, factionAllyGroups, state.BrokenAllyFactionIds))
        {
            kinds.Add(ActionKind.Repair);
        }

        if (moves.Any(target => IsValidSplit(state, map, force, target, rules)))
        {
            kinds.Add(ActionKind.Split);
        }

        if (IsValidBackstab(state, map, force, factionAllyGroups, rules))
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

    /// <summary>
    /// Applies uncontested occupation claims for forces that are not in battle.
    /// </summary>
    internal static PlayMap ApplyIdleOccupation(
        PlayMap map,
        IReadOnlyList<CampaignForce> forces,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlyCollection<Guid> broken,
        Func<int, int> pickIndex)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(forces);
        ArgumentNullException.ThrowIfNull(factionAllyGroups);
        ArgumentNullException.ThrowIfNull(broken);
        ArgumentNullException.ThrowIfNull(pickIndex);
        var next = map.Territories.ToDictionary(static territory => territory.Id);
        var inBattle = forces.Where(static force => force.InBattle).Select(static force => force.Id).ToHashSet();
        foreach (var territory in next.Values.ToArray())
        {
            if (territory.IsSpawn)
            {
                next[territory.Id] = territory.With(ownerFactionId: territory.SpawnFactionId);
                continue;
            }

            var occupants = forces
                .Where(force => force.TerritoryId == territory.Id && !inBattle.Contains(force.Id))
                .ToArray();
            if (occupants.Length == 0 || forces.Any(force => force.TerritoryId == territory.Id && inBattle.Contains(force.Id)))
            {
                continue;
            }

            var claimed = ClaimOwner(
                territory,
                occupants,
                map,
                factionAllyGroups,
                broken.ToHashSet(),
                pickIndex);
            if (claimed != territory.OwnerFactionId)
            {
                next[territory.Id] = next[territory.Id].With(ownerFactionId: claimed, assignOwner: true);
            }
        }

        return map.WithTerritories([.. next.Values.OrderBy(static territory => territory.DisplayNumber)]);
    }

    private static ResolvedOrder Normalize(
        CampaignPlayState state,
        PlayMap map,
        PhaseWindow window,
        CampaignForce force,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        SpecialRuleContext rules)
    {
        var submission = state.LatestSubmission(window.Id, force.Id);
        var kind = submission?.Kind ?? ActionKind.Hold;
        var target = submission?.TargetTerritoryId;
        var structureTypeId = submission?.StructureTypeId;
        var via = submission?.ViaTerritoryId;
        var destroyImmediately = submission?.DestroyImmediately == true;
        if (kind == ActionKind.Move
            && !FactionSpecialRulePolicies.IsValidMove(map, force, target, via, state.ItemObjectives, rules))
        {
            return Hold(force, OrderAdjustment.InvalidOrder);
        }

        if (kind == ActionKind.Split && !IsValidSplit(state, map, force, target, rules))
        {
            return Hold(force, OrderAdjustment.InvalidOrder);
        }

        if (kind == ActionKind.Build
            && (!IsValidBuild(map, force, structureTypeId) || !FactionSpecialRulePolicies.CanBuild(map, force, structureTypeId ?? Guid.Empty, rules)))
        {
            return Hold(force, OrderAdjustment.InvalidOrder);
        }

        if (kind == ActionKind.Pillage
            && !IsValidPillage(map, force, factionAllyGroups, state.BrokenAllyFactionIds, rules, state.BrokenAllySubfactions))
        {
            return Hold(force, OrderAdjustment.InvalidOrder);
        }

        if (kind == ActionKind.Repair && !IsValidRepair(map, force, factionAllyGroups, state.BrokenAllyFactionIds))
        {
            return Hold(force, OrderAdjustment.InvalidOrder);
        }

        if (kind == ActionKind.Backstab
            && !IsValidBackstab(state, map, force, factionAllyGroups, rules))
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
            return new ResolvedOrder(
                force.Id,
                kind,
                force.TerritoryId,
                structureTypeId,
                OrderAdjustment.None,
                via,
                destroyImmediately && FactionSpecialRulePolicies.CanDestroyImmediately(force, rules));
        }

        return new ResolvedOrder(force.Id, kind, target, structureTypeId, OrderAdjustment.None, via);
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

    private static bool IsValidSplit(
        CampaignPlayState state,
        PlayMap map,
        CampaignForce force,
        Guid? targetId,
        SpecialRuleContext rules)
    {
        if (state.Forces.Count(item => item.ControllerUserId == force.ControllerUserId) >= MaxForcesPerPlayer)
        {
            return false;
        }

        return FactionSpecialRulePolicies.IsValidMove(map, force, targetId, viaId: null, state.ItemObjectives, rules);
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

    internal static bool IsValidPillage(
        PlayMap map,
        CampaignForce force,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlyCollection<Guid> broken,
        SpecialRuleContext? specialRules = null,
        IReadOnlyList<BrokenAllySubfaction>? brokenSubfactions = null)
    {
        var rules = specialRules ?? SpecialRuleContext.None;
        var territory = map.Territory(force.TerritoryId);
        if (territory?.StructureTypeId is null || territory.StructureCondition == StructureCondition.Destroyed)
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

        if (territory.OwnerFactionId is { } owner
            && AreAllies(force.FactionId, owner, factionAllyGroups, broken)
            && !FactionSpecialRulePolicies.CanPillageAllied(force, rules))
        {
            return false;
        }

        _ = brokenSubfactions;
        return true;
    }

    internal static bool IsValidRepair(
        PlayMap map,
        CampaignForce force,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlyCollection<Guid> broken)
    {
        var territory = map.Territory(force.TerritoryId);
        if (territory is null
            || territory.StructureTypeId is null
            || territory.StructureCondition != StructureCondition.Pillaged
            || territory.OwnerFactionId is not { } owner)
        {
            return false;
        }

        return owner == force.FactionId
            || AreAllies(force.FactionId, owner, factionAllyGroups, broken);
    }

    internal static bool IsValidBackstab(
        CampaignPlayState state,
        PlayMap map,
        CampaignForce force,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        SpecialRuleContext? specialRules = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(force);
        ArgumentNullException.ThrowIfNull(factionAllyGroups);
        var rules = specialRules ?? SpecialRuleContext.None;
        var broken = state.BrokenAllyFactionIds;
        var brokenSubfactions = state.BrokenAllySubfactions;
        if (!HasAllianceToBreak(force, factionAllyGroups, broken, rules, brokenSubfactions))
        {
            return false;
        }

        var othersHere = state.Forces
            .Where(other => other.Id != force.Id && other.TerritoryId == force.TerritoryId)
            .ToArray();
        if (othersHere.Any(other =>
            FactionSpecialRulePolicies.AreAllies(force, other, factionAllyGroups, broken, brokenSubfactions, rules)))
        {
            return true;
        }

        var territory = map.Territory(force.TerritoryId);
        return territory?.OwnerFactionId is { } owner
            && AreAllies(force.FactionId, owner, factionAllyGroups, broken);
    }

    private static bool HasAllianceToBreak(
        CampaignForce force,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlyList<Guid> broken,
        SpecialRuleContext rules,
        IReadOnlyList<BrokenAllySubfaction> brokenSubfactions)
    {
        if (rules.Has(force, SpecialRuleEffectKeys.DividedWeStand)
            && !string.IsNullOrWhiteSpace(force.Subfaction))
        {
            return !brokenSubfactions.Any(item =>
                item.FactionId == force.FactionId
                && string.Equals(item.Subfaction, force.Subfaction, StringComparison.OrdinalIgnoreCase));
        }

        if (broken.Contains(force.FactionId))
        {
            return false;
        }

        return factionAllyGroups.TryGetValue(force.FactionId, out var group) && !string.IsNullOrWhiteSpace(group);
    }

    private static bool CreatesBattle(
        CampaignForce[] present,
        PlayTerritory territory,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        HashSet<Guid> broken,
        IReadOnlyList<BrokenAllySubfaction> brokenSubfactions,
        SpecialRuleContext rules)
    {
        return FactionSpecialRulePolicies.CreatesBattle(
            present,
            territory,
            factionAllyGroups,
            broken,
            brokenSubfactions,
            rules);
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

    internal static bool AreAllies(
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
            return false;
        }

        if (!factionAllyGroups.TryGetValue(leftFactionId, out var leftGroup)
            || !factionAllyGroups.TryGetValue(rightFactionId, out var rightGroup))
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(leftGroup)
            && string.Equals(leftGroup, rightGroup, StringComparison.Ordinal);
    }

    private static List<CampaignForce> Rejoin(
        List<CampaignForce> forces,
        PhaseWindow window,
        DateTimeOffset utcNow,
        List<PlayLogEntry> log,
        IReadOnlyDictionary<Guid, ActionKind> arrivalKinds,
        SpecialRuleContext rules)
    {
        var result = new List<CampaignForce>();
        foreach (var group in forces.GroupBy(static force => (force.ControllerUserId, force.TerritoryId)))
        {
            var members = group.OrderBy(static force => force.Id).ToArray();
            if (members.Length > 1
                && !FactionSpecialRulePolicies.ShouldRejoin(members[0], members[1], arrivalKinds, rules))
            {
                result.AddRange(members);
                continue;
            }

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
        HashSet<Guid> inBattle,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        HashSet<Guid> broken,
        Func<int, int> pickIndex,
        HashSet<Guid>? skipClaimTerritories = null,
        SpecialRuleContext? specialRules = null,
        IReadOnlyList<BrokenAllySubfaction>? brokenSubfactions = null)
    {
        _ = specialRules;
        _ = brokenSubfactions;
        var next = map.Territories.ToDictionary(static territory => territory.Id);
        var originalOwners = map.Territories.ToDictionary(static territory => territory.Id, static territory => territory.OwnerFactionId);
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
                if (order.DestroyImmediately && territory.IsDestructible)
                {
                    next[territory.Id] = territory.With(clearStructure: true);
                }
                else if (territory.StructureCondition == StructureCondition.Operational)
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

            if (skipClaimTerritories is not null && skipClaimTerritories.Contains(territory.Id))
            {
                continue;
            }

            var occupants = forces
                .Where(force => force.TerritoryId == territory.Id && !inBattle.Contains(force.Id))
                .ToArray();
            if (occupants.Length == 0 || forces.Any(force => force.TerritoryId == territory.Id && inBattle.Contains(force.Id)))
            {
                continue;
            }

            var claimed = ClaimOwner(
                territory,
                occupants,
                map,
                factionAllyGroups,
                broken,
                pickIndex);
            if (claimed != territory.OwnerFactionId)
            {
                next[territory.Id] = next[territory.Id].With(ownerFactionId: claimed, assignOwner: true);
            }
        }

        foreach (var order in resolved.Values)
        {
            if (order.Kind != ActionKind.Backstab || !forces.Any(force => force.Id == order.ForceId))
            {
                continue;
            }

            var force = forces.First(item => item.Id == order.ForceId);
            if (inBattle.Contains(force.Id))
            {
                continue;
            }

            var territory = next[force.TerritoryId];
            if (!originalOwners.TryGetValue(territory.Id, out var previousOwner)
                || previousOwner is not { } former
                || former == force.FactionId
                || territory.OwnerFactionId != force.FactionId)
            {
                continue;
            }

            if (!SameAllyGroup(force.FactionId, former, factionAllyGroups)
                || !territory.IsPillageable
                || territory.StructureTypeId is null
                || territory.StructureCondition != StructureCondition.Operational)
            {
                continue;
            }

            next[territory.Id] = territory.With(structureCondition: StructureCondition.Pillaged);
        }

        return map.WithTerritories([.. next.Values.OrderBy(static territory => territory.DisplayNumber)]);
    }

    private static Guid? ClaimOwner(
        PlayTerritory territory,
        CampaignForce[] occupants,
        PlayMap map,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        HashSet<Guid> broken,
        Func<int, int> pickIndex)
    {
        var factions = occupants.Select(static force => force.FactionId).Distinct().ToArray();
        if (factions.Length == 1)
        {
            var factionId = factions[0];
            if (territory.OwnerFactionId is { } owner
                && owner != factionId
                && AreAllies(factionId, owner, factionAllyGroups, broken))
            {
                return owner;
            }

            return factionId;
        }

        if (factions.Any(left => factions.Any(right => AreEnemies(left, right, factionAllyGroups, broken))))
        {
            return territory.OwnerFactionId;
        }

        if (territory.OwnerFactionId is { } current
            && factions.Any(faction => faction == current || AreAllies(faction, current, factionAllyGroups, broken)))
        {
            return current;
        }

        if (territory.OwnerFactionId is not null)
        {
            return territory.OwnerFactionId;
        }

        var ranked = CombatantStrengthRules.Rank(
            factions,
            factionId =>
            {
                var holdings = CombatantStrengthRules.Holdings(map, factionId);
                return new CombatantStrengthRules.Strength(0, holdings.Territories, holdings.Structures, 0);
            },
            pickIndex);
        return ranked[0];
    }

    private static bool SameAllyGroup(
        Guid leftFactionId,
        Guid rightFactionId,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups)
    {
        if (!factionAllyGroups.TryGetValue(leftFactionId, out var leftGroup)
            || !factionAllyGroups.TryGetValue(rightFactionId, out var rightGroup))
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(leftGroup)
            && string.Equals(leftGroup, rightGroup, StringComparison.Ordinal);
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
        OrderAdjustment Adjustment = OrderAdjustment.None,
        Guid? ViaTerritoryId = null,
        bool DestroyImmediately = false);

    private enum OrderAdjustment
    {
        None = 0,
        InvalidOrder = 1,
        ConflictingBuild = 2,
    }
}
