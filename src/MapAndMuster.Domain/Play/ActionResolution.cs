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
            AppendResolvedActionLog(log, window, force, submission, order, utcNow, map);
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

            var destination = order.Kind == ActionKind.Teleport
                ? ItemObjectiveEffectRules.PickTeleportDestination(
                    map,
                    state.Forces,
                    pickIndex ?? (static count => 0)) ?? force.TerritoryId
                : order.Kind is ActionKind.Move or ActionKind.Retreat
                ? FactionSpecialRulePolicies.ResolveMoveDestination(
                    map,
                    force,
                    order.TargetTerritoryId ?? force.TerritoryId,
                    order.ViaTerritoryId,
                    state.Forces,
                    factionAllyGroups,
                    state.BrokenAllyFactionIds,
                    state.BrokenAllySubfactions,
                    rules,
                    state.AllyBetrayals,
                    order.ViaPath)
                : force.TerritoryId;
            if (order.Kind is ActionKind.Move or ActionKind.Retreat or ActionKind.Teleport && destination != force.TerritoryId)
            {
                moveOrigins[force.Id] = force.TerritoryId;
            }

            var moved = force.With(territoryId: destination);
            nextForces.Add(moved);
            AddOccupied(occupied, destination, moved.Id);
            arrivalKinds[force.Id] = order.Kind;
            if (order.Kind == ActionKind.Move)
            {
                foreach (var hop in IntermediateHops(force.TerritoryId, destination, order.ViaTerritoryId, order.ViaPath))
                {
                    if (FactionSpecialRulePolicies.SkipClaiming(
                        force,
                        hop,
                        force.TerritoryId,
                        destination,
                        order.ViaTerritoryId,
                        rules,
                        order.ViaPath))
                    {
                        skipClaimTerritories.Add(hop);
                    }
                }
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
        var betrayals = state.AllyBetrayals.ToList();
        foreach (var order in resolved.Values)
        {
            if (order.Kind != ActionKind.Backstab)
            {
                continue;
            }

            var actor = nextForces.FirstOrDefault(item => item.Id == order.ForceId);
            if (actor is null)
            {
                continue;
            }

            var present = nextForces.Where(item => item.TerritoryId == actor.TerritoryId).ToArray();
            var recorded = RecordBackstabBetrayals(
                actor,
                present,
                map,
                factionAllyGroups,
                broken,
                brokenSubfactions,
                betrayals,
                rules);
            foreach (var betrayal in recorded)
            {
                betrayals.Add(betrayal);
                log.Add(new PlayLogEntry(
                    Guid.NewGuid(),
                    utcNow,
                    PlayLogKind.AllianceBetrayed,
                    window.Id,
                    actor.Id,
                    actor.ControllerUserId,
                    actor.TerritoryId,
                    targetTerritoryId: null,
                    battleId: null,
                    ActionKind.Backstab,
                    present
                        .Where(item => AllyBetrayalRules.MatchesVictim(betrayal, item))
                        .Select(static item => item.Id)
                        .ToArray(),
                    BetrayalLogMessage(map, actor, betrayal)));
            }
        }

        var battles = state.Battles.ToList();
        var inBattle = new HashSet<Guid>();
        foreach (var (territoryId, forceIds) in occupied)
        {
            var present = forceIds
                .Select(id => nextForces.First(force => force.Id == id))
                .ToArray();
            if (CreatesBattle(
                present,
                map.Territory(territoryId)!,
                factionAllyGroups,
                broken,
                brokenSubfactions,
                rules,
                betrayals))
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
                        pickIndex ?? (static count => 0),
                        brokenSubfactions,
                        rules,
                        betrayals);
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
        nextForces = [.. ItemObjectiveEffectRules.ApplyStatuses(nextForces, map, items, rules)];

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
            brokenSubfactions,
            betrayals);
        return (
            state.With(
                forces: nextForces,
                battles: battles,
                brokenAllyFactionIds: [.. broken.OrderBy(static id => id)],
                brokenAllySubfactions: [.. brokenSubfactions.OrderBy(static item => item.FactionId).ThenBy(static item => item.Subfaction)],
                allyBetrayals: [.. betrayals],
                structures: CaptureStructures(nextMap),
                itemObjectives: items,
                log: log),
            nextMap);
    }

    /// <summary>
    /// Player-submittable actions available for a force in an open action window, in documented order:
    /// Hold, Move, Teleport when granted, Build, Pillage, Repair, Split, then Backstab.
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
        var moves = CampaignPlayRules.EligibleMoves(map, force, state.ItemObjectives, rules, state.Forces);
        if (moves.Count > 0)
        {
            kinds.Add(ActionKind.Move);
        }

        if (ItemObjectiveEffectRules.CanTeleport(force, map, state.ItemObjectives, rules)
            && ItemObjectiveEffectRules.TeleportDestinations(map, state.Forces).Count > 0)
        {
            kinds.Add(ActionKind.Teleport);
        }

        if (map.HasBuildableStructure && CanBuildInTerritory(map, force))
        {
            kinds.Add(ActionKind.Build);
        }

        if (IsValidPillage(
            map,
            force,
            factionAllyGroups,
            state.BrokenAllyFactionIds,
            rules,
            state.BrokenAllySubfactions,
            state.AllyBetrayals))
        {
            kinds.Add(ActionKind.Pillage);
        }

        if (IsValidRepair(map, force, factionAllyGroups, state.BrokenAllyFactionIds, state.AllyBetrayals))
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
        Func<int, int> pickIndex,
        IReadOnlyList<AllyBetrayal>? allyBetrayals = null,
        SpecialRuleContext? specialRules = null)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(forces);
        ArgumentNullException.ThrowIfNull(factionAllyGroups);
        ArgumentNullException.ThrowIfNull(broken);
        ArgumentNullException.ThrowIfNull(pickIndex);
        var rules = specialRules ?? SpecialRuleContext.None;
        var next = map.Territories.ToDictionary(static territory => territory.Id);
        var inBattle = forces.Where(static force => force.InBattle).Select(static force => force.Id).ToHashSet();
        foreach (var territory in next.Values.ToArray())
        {
            if (territory.IsSpawn)
            {
                next[territory.Id] = territory.With(
                    ownerFactionId: territory.SpawnFactionId,
                    assignOwner: true,
                    ownerSubfaction: territory.SpawnSubfaction,
                    assignOwnerSubfaction: true);
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
                pickIndex,
                allyBetrayals);
            next[territory.Id] = WithClaim(territory, claimed, occupants, rules);
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
        var viaPath = submission?.ViaPath;
        var destroyImmediately = submission?.DestroyImmediately == true;
        if (kind == ActionKind.Move
            && !FactionSpecialRulePolicies.IsValidMove(map, force, target, via, state.ItemObjectives, rules, viaPath, state.Forces))
        {
            return Hold(force, OrderAdjustment.InvalidOrder);
        }

        if (kind == ActionKind.Split && !IsValidSplit(state, map, force, target, rules, via, viaPath))
        {
            return Hold(force, OrderAdjustment.InvalidOrder);
        }

        if (kind == ActionKind.Teleport
            && (!ItemObjectiveEffectRules.CanTeleport(force, map, state.ItemObjectives, rules)
                || ItemObjectiveEffectRules.TeleportDestinations(map, state.Forces).Count == 0))
        {
            return Hold(force, OrderAdjustment.InvalidOrder);
        }

        if (kind == ActionKind.Build
            && (!IsValidBuild(map, force, structureTypeId) || !FactionSpecialRulePolicies.CanBuild(map, force, structureTypeId ?? Guid.Empty, rules)))
        {
            return Hold(force, OrderAdjustment.InvalidOrder);
        }

        if (kind == ActionKind.Pillage
            && !IsValidPillage(
                map,
                force,
                factionAllyGroups,
                state.BrokenAllyFactionIds,
                rules,
                state.BrokenAllySubfactions,
                state.AllyBetrayals))
        {
            return Hold(force, OrderAdjustment.InvalidOrder);
        }

        if (kind == ActionKind.Repair
            && !IsValidRepair(map, force, factionAllyGroups, state.BrokenAllyFactionIds, state.AllyBetrayals))
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

        return new ResolvedOrder(force.Id, kind, target, structureTypeId, OrderAdjustment.None, via, false, viaPath);
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
        SpecialRuleContext rules,
        Guid? viaId = null,
        IReadOnlyList<Guid>? viaPath = null)
    {
        if (state.Forces.Count(item => item.ControllerUserId == force.ControllerUserId) >= MaxForcesPerPlayer)
        {
            return false;
        }

        return FactionSpecialRulePolicies.IsValidMove(map, force, targetId, viaId, state.ItemObjectives, rules, viaPath, state.Forces);
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
        IReadOnlyList<BrokenAllySubfaction>? brokenSubfactions = null,
        IReadOnlyList<AllyBetrayal>? allyBetrayals = null)
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
            && !AllyBetrayalRules.PlayerBetrayedFaction(force.ControllerUserId, owner, null, allyBetrayals ?? [])
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
        IReadOnlyCollection<Guid> broken,
        IReadOnlyList<AllyBetrayal>? allyBetrayals = null)
    {
        var territory = map.Territory(force.TerritoryId);
        if (territory is null
            || territory.StructureTypeId is null
            || territory.StructureCondition != StructureCondition.Pillaged
            || territory.OwnerFactionId is not { } owner)
        {
            return false;
        }

        if (owner == force.FactionId)
        {
            return true;
        }

        return AreAllies(force.FactionId, owner, factionAllyGroups, broken)
            && !AllyBetrayalRules.PlayerBetrayedFaction(force.ControllerUserId, owner, null, allyBetrayals ?? []);
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
        var betrayals = state.AllyBetrayals;
        var othersHere = state.Forces
            .Where(other => other.Id != force.Id && other.TerritoryId == force.TerritoryId)
            .ToArray();
        if (othersHere.Any(other =>
            FactionSpecialRulePolicies.AreAllies(
                force,
                other,
                factionAllyGroups,
                broken,
                brokenSubfactions,
                rules,
                betrayals)))
        {
            return true;
        }

        var territory = map.Territory(force.TerritoryId);
        return territory?.OwnerFactionId is { } owner
            && AreAllies(force.FactionId, owner, factionAllyGroups, broken)
            && !AllyBetrayalRules.PlayerBetrayedFaction(force.ControllerUserId, owner, null, betrayals);
    }

    private static List<AllyBetrayal> RecordBackstabBetrayals(
        CampaignForce actor,
        IReadOnlyList<CampaignForce> present,
        PlayMap map,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlyCollection<Guid> broken,
        IReadOnlyList<BrokenAllySubfaction> brokenSubfactions,
        IReadOnlyList<AllyBetrayal> existing,
        SpecialRuleContext rules)
    {
        var added = new List<AllyBetrayal>();
        var alliesHere = present
            .Where(other =>
                other.Id != actor.Id
                && FactionSpecialRulePolicies.AreAllies(
                    actor,
                    other,
                    factionAllyGroups,
                    broken,
                    brokenSubfactions,
                    rules,
                    existing))
            .ToArray();
        if (alliesHere.Length > 0)
        {
            foreach (var victim in alliesHere)
            {
                var scope = AllyBetrayalRules.ScopeKey(victim.FactionId, victim.Subfaction, rules);
                var row = new AllyBetrayal(actor.ControllerUserId, victim.FactionId, scope, victim.ControllerUserId);
                if (!existing.Concat(added).Any(item => AllyBetrayalRules.SameRow(item, row)))
                {
                    added.Add(row);
                }
            }

            return added;
        }

        var territory = map.Territory(actor.TerritoryId);
        if (territory?.OwnerFactionId is not { } owner
            || owner == actor.FactionId
            || !AreAllies(actor.FactionId, owner, factionAllyGroups, broken)
            || AllyBetrayalRules.PlayerBetrayedFaction(actor.ControllerUserId, owner, null, existing))
        {
            return added;
        }

        var emptyLand = new AllyBetrayal(actor.ControllerUserId, owner, BetrayedSubfaction: null, BetrayedUserId: null);
        if (!existing.Any(item => AllyBetrayalRules.SameRow(item, emptyLand)))
        {
            added.Add(emptyLand);
        }

        return added;
    }

    private static bool CreatesBattle(
        CampaignForce[] present,
        PlayTerritory territory,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        HashSet<Guid> broken,
        IReadOnlyList<BrokenAllySubfaction> brokenSubfactions,
        SpecialRuleContext rules,
        IReadOnlyList<AllyBetrayal> allyBetrayals)
    {
        return FactionSpecialRulePolicies.CreatesBattle(
            present,
            territory,
            factionAllyGroups,
            broken,
            brokenSubfactions,
            rules,
            allyBetrayals);
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

            var surviving = members[0];
            if (members.Any(static member => ForceStatusNames.IsDiseased(member.StatusName))
                && FactionSpecialRulePolicies.AllowsStatus(surviving, ForceStatusNames.Diseased, rules))
            {
                surviving = surviving.WithStatus(ForceStatusNames.Diseased);
            }

            result.Add(surviving);
            if (members.Length > 1)
            {
                log.Add(new PlayLogEntry(
                    Guid.NewGuid(),
                    utcNow,
                    PlayLogKind.ForcesRejoined,
                    window.Id,
                    surviving.Id,
                    surviving.ControllerUserId,
                    surviving.TerritoryId,
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
        IReadOnlyList<BrokenAllySubfaction>? brokenSubfactions = null,
        IReadOnlyList<AllyBetrayal>? allyBetrayals = null)
    {
        var rules = specialRules ?? SpecialRuleContext.None;
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
                var structureRules = map.StructureRules(structureTypeId);
                next[territory.Id] = territory.With(
                    ownerFactionId: force.FactionId,
                    assignOwner: true,
                    ownerSubfaction: ClaimOwnerSubfaction(territory, force.FactionId, [force], rules),
                    assignOwnerSubfaction: true,
                    structureTypeId: structureTypeId,
                    structureName: structureRules?.Name,
                    structureCondition: StructureCondition.Operational,
                    isPillageable: structureRules?.IsPillageable ?? territory.IsPillageable,
                    isDestructible: structureRules?.IsDestructible ?? territory.IsDestructible);
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
                pickIndex,
                allyBetrayals);
            next[territory.Id] = WithClaim(territory, claimed, occupants, rules);
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
        Func<int, int> pickIndex,
        IReadOnlyList<AllyBetrayal>? allyBetrayals = null)
    {
        var betrayals = allyBetrayals ?? [];
        var factions = occupants.Select(static force => force.FactionId).Distinct().ToArray();
        if (factions.Length == 1)
        {
            var factionId = factions[0];
            if (territory.OwnerFactionId is { } owner
                && owner != factionId
                && AreAllies(factionId, owner, factionAllyGroups, broken)
                && !occupants.Any(item =>
                    AllyBetrayalRules.PlayerBetrayedFaction(item.ControllerUserId, owner, null, betrayals)))
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

    private static PlayTerritory WithClaim(
        PlayTerritory territory,
        Guid? claimed,
        CampaignForce[] occupants,
        SpecialRuleContext rules)
    {
        var subfaction = ClaimOwnerSubfaction(territory, claimed, occupants, rules);
        if (claimed == territory.OwnerFactionId
            && string.Equals(subfaction, territory.OwnerSubfaction, StringComparison.OrdinalIgnoreCase))
        {
            return territory;
        }

        return territory.With(
            ownerFactionId: claimed,
            assignOwner: true,
            ownerSubfaction: subfaction,
            assignOwnerSubfaction: true);
    }

    private static string? ClaimOwnerSubfaction(
        PlayTerritory territory,
        Guid? claimedFactionId,
        IReadOnlyList<CampaignForce> occupants,
        SpecialRuleContext rules)
    {
        if (claimedFactionId is null || !rules.FactionRequiresSubfaction(claimedFactionId.Value))
        {
            return null;
        }

        if (claimedFactionId == territory.OwnerFactionId && !string.IsNullOrWhiteSpace(territory.OwnerSubfaction))
        {
            return territory.OwnerSubfaction;
        }

        var names = occupants
            .Where(force => force.FactionId == claimedFactionId)
            .Select(static force => force.Subfaction)
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (names.Length == 0)
        {
            return claimedFactionId == territory.OwnerFactionId ? territory.OwnerSubfaction : null;
        }

        return names[0];
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
        DateTimeOffset utcNow,
        PlayMap map)
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

        if (order.Kind == ActionKind.Backstab)
        {
            return;
        }

        log.Add(Entry(
            utcNow,
            PlayLogKind.ResolvedAction,
            window.Id,
            force,
            order.Kind,
            force.TerritoryId,
            order.TargetTerritoryId,
            StructureMessage(map, force, order)));
    }

    private static string? StructureMessage(PlayMap map, CampaignForce force, ResolvedOrder order)
    {
        if (order.Kind == ActionKind.Pillage && DestroysStructure(map, force, order))
        {
            return PlayLogFacts.DestroyedStructure(TerritoryStructureName(map, force.TerritoryId));
        }

        if (order.Kind is ActionKind.Pillage or ActionKind.Repair)
        {
            return TerritoryStructureName(map, force.TerritoryId);
        }

        if (order.Kind == ActionKind.Build && order.StructureTypeId is { } structureTypeId)
        {
            return map.StructureRules(structureTypeId)?.Name ?? "structure";
        }

        return null;
    }

    private static bool DestroysStructure(PlayMap map, CampaignForce force, ResolvedOrder order)
    {
        var territory = map.Territory(force.TerritoryId);
        if (territory?.StructureTypeId is null)
        {
            return false;
        }

        if (order.DestroyImmediately && territory.IsDestructible)
        {
            return true;
        }

        return territory.StructureCondition != StructureCondition.Operational && territory.IsDestructible;
    }

    private static string TerritoryStructureName(PlayMap map, Guid territoryId)
    {
        var territory = map.Territory(territoryId);
        if (!string.IsNullOrWhiteSpace(territory?.StructureName))
        {
            return territory.StructureName;
        }

        if (territory?.StructureTypeId is { } structureTypeId
            && map.StructureRules(structureTypeId) is { Name: { Length: > 0 } name })
        {
            return name;
        }

        return "structure";
    }

    private static string BetrayalLogMessage(PlayMap map, CampaignForce actor, AllyBetrayal betrayal)
    {
        if (betrayal.BetrayedUserId is { } victimId)
        {
            return PlayLogFacts.Betrayal(PlayLogFacts.BetrayalAttack, victimId, betrayal.BetrayedFactionId);
        }

        var territory = map.Territory(actor.TerritoryId);
        var willPillage = territory is not null
            && territory.OwnerFactionId == betrayal.BetrayedFactionId
            && territory.IsPillageable
            && territory.StructureTypeId is not null
            && territory.StructureCondition == StructureCondition.Operational;
        if (willPillage)
        {
            return PlayLogFacts.Betrayal(
                PlayLogFacts.BetrayalPillage,
                null,
                betrayal.BetrayedFactionId,
                TerritoryStructureName(map, actor.TerritoryId));
        }

        return PlayLogFacts.Betrayal(PlayLogFacts.BetrayalClaim, null, betrayal.BetrayedFactionId);
    }

    private static PlayLogEntry Entry(
        DateTimeOffset utcNow,
        PlayLogKind kind,
        Guid windowId,
        CampaignForce force,
        ActionKind actionKind,
        Guid territoryId,
        Guid? targetTerritoryId,
        string? message = null)
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
            [force.Id],
            message);
    }

    private static List<Guid> IntermediateHops(
        Guid originId,
        Guid destinationId,
        Guid? viaId,
        IReadOnlyList<Guid>? viaPath)
    {
        var hops = new List<Guid>();
        if (viaId is { } via && via != Guid.Empty && via != originId && via != destinationId)
        {
            hops.Add(via);
        }

        if (viaPath is not null)
        {
            foreach (var hop in viaPath)
            {
                if (hop == Guid.Empty || hop == originId || hop == destinationId || hops.Contains(hop))
                {
                    continue;
                }

                hops.Add(hop);
            }
        }

        return hops;
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
        bool DestroyImmediately = false,
        IReadOnlyList<Guid>? ViaPath = null);

    private enum OrderAdjustment
    {
        None = 0,
        InvalidOrder = 1,
        ConflictingBuild = 2,
    }
}
