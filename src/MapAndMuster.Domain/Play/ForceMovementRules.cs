using MapAndMuster.Domain.Campaigns;

namespace MapAndMuster.Domain.Play;

/// <summary>
/// Movement-speed hops: adjacent steps up to a force's effective speed, claiming only the landing territory.
/// </summary>
public static class ForceMovementRules
{
    /// <summary>
    /// Effective Move speed: subfaction override, else faction speed (default 1), plus Called by the Relic
    /// and held item bonuses, clamped to <see cref="ForceMovementSpeeds.Max"/>.
    /// </summary>
    public static int EffectiveSpeed(
        CampaignForce force,
        SpecialRuleContext rules,
        PlayMap? map = null,
        IReadOnlyList<CampaignItemObjective>? items = null,
        IReadOnlyList<CampaignForce>? occupyingForces = null)
    {
        ArgumentNullException.ThrowIfNull(force);
        ArgumentNullException.ThrowIfNull(rules);
        var speed = rules.MovementSpeedFor(force);
        if (items is not null)
        {
            speed += FactionSpecialRulePolicies.CalledByTheRelicSpeedBonus(
                force,
                occupyingForces ?? [],
                items,
                rules);
        }

        if (map is not null && items is not null)
        {
            speed += ItemObjectiveEffectRules.MovementSpeedBonus(force, map, items, rules);
        }

        return Math.Clamp(speed, ForceMovementSpeeds.Min, ForceMovementSpeeds.Max);
    }

    /// <summary>
    /// Returns whether a Move or Split path of one or more adjacent hops is legal.
    /// </summary>
    public static bool IsValidMove(
        PlayMap map,
        CampaignForce force,
        Guid? targetId,
        Guid? viaId,
        IReadOnlyList<CampaignItemObjective> items,
        SpecialRuleContext rules,
        IReadOnlyList<Guid>? viaPath = null,
        IReadOnlyList<CampaignForce>? occupyingForces = null,
        IReadOnlyList<CampaignBattle>? battles = null)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(force);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(rules);
        if (targetId is null || targetId == force.TerritoryId)
        {
            return false;
        }

        var locked = LockedBattleTerritories(occupyingForces ?? [], battles);
        var intermediates = Intermediates(viaId, viaPath, targetId.Value, force.TerritoryId);
        if (intermediates.Count > 0)
        {
            var speed = EffectiveSpeed(force, rules, map, items, occupyingForces);
            if (intermediates.Count + 1 > speed)
            {
                return false;
            }

            return IsLegalPath(map, force, [force.TerritoryId, .. intermediates, targetId.Value], locked);
        }

        if (locked.Contains(targetId.Value))
        {
            return false;
        }

        if (map.AreAdjacent(force.TerritoryId, targetId.Value)
            && FactionSpecialRulePolicies.CanLandOnForMove(map, force, targetId.Value))
        {
            return true;
        }

        return FactionSpecialRulePolicies.RelicAdjacentMoveTargets(map, force, items, rules).Contains(targetId.Value)
            && !locked.Contains(targetId.Value);
    }

    /// <summary>
    /// Walks a multi-hop path and stops at the first territory that already has an enemy.
    /// </summary>
    public static Guid ResolveDestination(
        PlayMap map,
        CampaignForce force,
        Guid targetId,
        Guid? viaId,
        IReadOnlyList<CampaignForce> startingForces,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlyCollection<Guid> brokenFactions,
        IReadOnlyList<BrokenAllySubfaction> brokenSubfactions,
        SpecialRuleContext rules,
        IReadOnlyList<AllyBetrayal>? allyBetrayals = null,
        IReadOnlyList<Guid>? viaPath = null)
    {
        var hops = Intermediates(viaId, viaPath, targetId, force.TerritoryId);
        if (hops.Count == 0)
        {
            return targetId;
        }

        foreach (var hop in hops)
        {
            if (HasEnemy(
                startingForces,
                hop,
                force,
                factionAllyGroups,
                brokenFactions,
                brokenSubfactions,
                rules,
                allyBetrayals))
            {
                return hop;
            }
        }

        return targetId;
    }

    /// <summary>Returns whether an intermediate hop should not be claimed.</summary>
    public static bool SkipClaiming(
        Guid territoryId,
        Guid originId,
        Guid destinationId,
        Guid? viaId,
        IReadOnlyList<Guid>? viaPath = null)
    {
        if (territoryId == originId || territoryId == destinationId)
        {
            return false;
        }

        return Intermediates(viaId, viaPath, destinationId, originId).Contains(territoryId);
    }

    /// <summary>
    /// Returns whether <paramref name="targetId"/> is reachable from <paramref name="originId"/>
    /// in at most <paramref name="speed"/> adjacent hops that each satisfy
    /// <paramref name="canStepOnto"/>.
    /// </summary>
    public static bool CanReachWithinSpeed(
        PlayMap map,
        Guid originId,
        Guid targetId,
        int speed,
        Func<Guid, bool> canStepOnto)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(canStepOnto);
        if (originId == targetId || speed < ForceMovementSpeeds.Min)
        {
            return false;
        }

        var visited = new HashSet<Guid> { originId };
        var queue = new Queue<(Guid Id, int Dist)>();
        queue.Enqueue((originId, 0));
        while (queue.Count > 0)
        {
            var (current, dist) = queue.Dequeue();
            if (dist >= speed)
            {
                continue;
            }

            foreach (var next in map.Neighbors(current))
            {
                if (!visited.Add(next) || !canStepOnto(next))
                {
                    continue;
                }

                if (next == targetId)
                {
                    return true;
                }

                queue.Enqueue((next, dist + 1));
            }
        }

        return false;
    }

    /// <summary>
    /// Destinations reachable in 1..<paramref name="speed"/> adjacent hops, plus relic extras.
    /// </summary>
    public static IReadOnlyList<Guid> EligibleDestinations(
        PlayMap map,
        CampaignForce force,
        int speed,
        IReadOnlyList<CampaignItemObjective> items,
        SpecialRuleContext rules,
        IReadOnlySet<Guid>? blockedTerritoryIds = null)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(force);
        var blocked = blockedTerritoryIds ?? new HashSet<Guid>();
        var ids = new List<Guid>();
        foreach (var neighborId in map.Neighbors(force.TerritoryId))
        {
            if (blocked.Contains(neighborId)
                || !FactionSpecialRulePolicies.CanLandOnForMove(map, force, neighborId)
                || ids.Contains(neighborId))
            {
                continue;
            }

            ids.Add(neighborId);
        }

        foreach (var hop in EligibleHops(map, force, speed, blocked))
        {
            if (!ids.Contains(hop.TargetTerritoryId))
            {
                ids.Add(hop.TargetTerritoryId);
            }
        }

        foreach (var extra in FactionSpecialRulePolicies.RelicAdjacentMoveTargets(map, force, items, rules))
        {
            if (!blocked.Contains(extra) && !ids.Contains(extra))
            {
                ids.Add(extra);
            }
        }

        return ids;
    }

    /// <summary>
    /// Every legal path of 1..<paramref name="speed"/> hops. Adjacent destinations use an empty via.
    /// </summary>
    public static IReadOnlyList<MoveHop> EligibleHops(
        PlayMap map,
        CampaignForce force,
        int speed,
        IReadOnlySet<Guid>? blockedTerritoryIds = null)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(force);
        var hops = new List<MoveHop>();
        if (speed < 2)
        {
            return hops;
        }

        Walk(map, force, force.TerritoryId, speed, [], hops, blockedTerritoryIds ?? new HashSet<Guid>());
        return hops;
    }

    /// <summary>
    /// Territories where two or more players are locked in battle. Other forces cannot enter or
    /// pass through until that battle is no longer open.
    /// </summary>
    public static HashSet<Guid> LockedBattleTerritories(
        IReadOnlyList<CampaignForce> forces,
        IReadOnlyList<CampaignBattle>? battles = null)
    {
        ArgumentNullException.ThrowIfNull(forces);
        var locked = new HashSet<Guid>();
        foreach (var group in forces.Where(static force => force.InBattle).GroupBy(static force => force.TerritoryId))
        {
            if (group.Select(static force => force.ControllerUserId).Distinct().Count() >= 2)
            {
                locked.Add(group.Key);
            }
        }

        if (battles is null)
        {
            return locked;
        }

        foreach (var battle in battles)
        {
            if (battle.Status is not (BattleStatus.Pending or BattleStatus.AwaitingResults or BattleStatus.Disputed))
            {
                continue;
            }

            var players = forces
                .Where(force => battle.ParticipantForceIds.Contains(force.Id))
                .Select(static force => force.ControllerUserId)
                .Distinct()
                .Count();
            if (players >= 2 || battle.IsRinger)
            {
                locked.Add(battle.TerritoryId);
            }
        }

        return locked;
    }

    /// <summary>
    /// Spawn relocation offered when every normal move would enter a locked battle.
    /// The move spends the force's whole speed and is not a hop path.
    /// </summary>
    public static IReadOnlyList<Guid> EscapeSpawnTargets(
        PlayMap map,
        CampaignForce force,
        IReadOnlyList<CampaignForce> forces,
        IReadOnlyList<CampaignBattle>? battles,
        IReadOnlyList<CampaignItemObjective> items,
        SpecialRuleContext rules,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(force);
        ArgumentNullException.ThrowIfNull(forces);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(factionAllyGroups);
        var locked = LockedBattleTerritories(forces, battles);
        var speed = EffectiveSpeed(force, rules, map, items, forces);
        var open = EligibleDestinations(map, force, speed, items, rules, locked);
        if (open.Count > 0)
        {
            return [];
        }

        var ignoringLocks = EligibleDestinations(map, force, speed, items, rules);
        if (ignoringLocks.Count == 0)
        {
            return [];
        }

        var usingAllies = !rules.IsFreeForAll
            && factionAllyGroups.Values.Any(static group => !string.IsNullOrWhiteSpace(group));
        var targets = new List<Guid>();
        var factionSpawn = map.SpawnFor(force.FactionId, force.Subfaction);
        if (factionSpawn is not null && IsUsableEscapeSpawn(map, force, factionSpawn.Id, forces, locked, usingAllies, factionAllyGroups, rules))
        {
            targets.Add(factionSpawn.Id);
        }

        var neutral = ClosestGeneralSpawn(
            map,
            force.TerritoryId,
            candidate => candidate != factionSpawn?.Id
                && IsUsableEscapeSpawn(map, force, candidate, forces, locked, usingAllies, factionAllyGroups, rules));
        if (neutral is { } neutralId && !targets.Contains(neutralId))
        {
            targets.Add(neutralId);
        }

        return targets;
    }

    /// <summary>Returns whether a Move lands on an escape spawn instead of walking a path.</summary>
    public static bool IsEscapeSpawnMove(
        PlayMap map,
        CampaignForce force,
        Guid? targetId,
        Guid? viaId,
        IReadOnlyList<Guid>? viaPath,
        IReadOnlyList<CampaignForce> forces,
        IReadOnlyList<CampaignBattle>? battles,
        IReadOnlyList<CampaignItemObjective> items,
        SpecialRuleContext rules,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups)
    {
        if (targetId is null || viaId is { } via && via != Guid.Empty || viaPath is { Count: > 0 })
        {
            return false;
        }

        return EscapeSpawnTargets(map, force, forces, battles, items, rules, factionAllyGroups).Contains(targetId.Value);
    }

    private static void Walk(
        PlayMap map,
        CampaignForce force,
        Guid current,
        int remaining,
        List<Guid> path,
        List<MoveHop> hops,
        IReadOnlySet<Guid> blocked)
    {
        if (remaining <= 0)
        {
            return;
        }

        foreach (var next in map.Neighbors(current))
        {
            if (next == force.TerritoryId
                || path.Contains(next)
                || blocked.Contains(next)
                || !FactionSpecialRulePolicies.CanEnter(map, force, next))
            {
                continue;
            }

            var nextPath = new List<Guid>(path) { next };
            if (nextPath.Count >= 2 && FactionSpecialRulePolicies.CanLandOnForMove(map, force, next))
            {
                hops.Add(new MoveHop(nextPath[0], next, nextPath.Count == 2 ? [] : [.. nextPath.Skip(1).Take(nextPath.Count - 2)]));
            }

            Walk(map, force, next, remaining - 1, nextPath, hops, blocked);
        }
    }

    internal static bool IsLegalStep(PlayMap map, CampaignForce force, Guid from, Guid to)
    {
        return map.AreAdjacent(from, to) && FactionSpecialRulePolicies.CanEnter(map, force, to);
    }

    private static bool IsLegalPath(PlayMap map, CampaignForce force, IReadOnlyList<Guid> path, HashSet<Guid> locked)
    {
        for (var i = 1; i < path.Count; i++)
        {
            if (locked.Contains(path[i]))
            {
                return false;
            }

            var landing = i == path.Count - 1;
            if (!map.AreAdjacent(path[i - 1], path[i]))
            {
                return false;
            }

            if (landing
                ? !FactionSpecialRulePolicies.CanLandOnForMove(map, force, path[i])
                : !FactionSpecialRulePolicies.CanEnter(map, force, path[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static List<Guid> Intermediates(
        Guid? viaId,
        IReadOnlyList<Guid>? viaPath,
        Guid targetId,
        Guid originId)
    {
        var hops = new List<Guid>();
        if (viaId is { } via && via != Guid.Empty && via != originId && via != targetId)
        {
            hops.Add(via);
        }

        if (viaPath is not null)
        {
            foreach (var hop in viaPath)
            {
                if (hop == Guid.Empty || hop == originId || hop == targetId || hops.Contains(hop))
                {
                    continue;
                }

                hops.Add(hop);
            }
        }

        return hops;
    }

    private static bool IsUsableEscapeSpawn(
        PlayMap map,
        CampaignForce force,
        Guid territoryId,
        IReadOnlyList<CampaignForce> forces,
        HashSet<Guid> locked,
        bool usingAllies,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        SpecialRuleContext rules)
    {
        if (territoryId == force.TerritoryId || locked.Contains(territoryId))
        {
            return false;
        }

        foreach (var occupant in forces)
        {
            if (occupant.Id == force.Id || occupant.TerritoryId != territoryId || occupant.ControllerUserId == force.ControllerUserId)
            {
                continue;
            }

            var allied = usingAllies
                && FactionSpecialRulePolicies.AreAllies(
                    force,
                    occupant,
                    factionAllyGroups,
                    [],
                    [],
                    rules);
            if (!allied)
            {
                return false;
            }
        }

        return map.Neighbors(territoryId).Any(neighbor =>
            !locked.Contains(neighbor) && FactionSpecialRulePolicies.CanLandOnForMove(map, force, neighbor));
    }

    private static Guid? ClosestGeneralSpawn(PlayMap map, Guid fromTerritoryId, Func<Guid, bool> accept)
    {
        var candidates = RandomSpawnRules.GeneralSpawnIds(map).Where(accept).ToArray();
        return candidates.Length == 0 ? null : RandomSpawnRules.Nearest(map, fromTerritoryId, candidates);
    }

    private static bool HasEnemy(
        IReadOnlyList<CampaignForce> forces,
        Guid territoryId,
        CampaignForce mover,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlyCollection<Guid> brokenFactions,
        IReadOnlyList<BrokenAllySubfaction> brokenSubfactions,
        SpecialRuleContext rules,
        IReadOnlyList<AllyBetrayal>? allyBetrayals)
    {
        return forces.Any(force =>
            force.TerritoryId == territoryId
            && force.Id != mover.Id
            && FactionSpecialRulePolicies.AreEnemies(
                mover,
                force,
                factionAllyGroups,
                brokenFactions,
                brokenSubfactions,
                rules,
                allyBetrayals));
    }
}
