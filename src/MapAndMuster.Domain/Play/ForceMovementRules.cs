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
        IReadOnlyList<CampaignForce>? occupyingForces = null)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(force);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(rules);
        if (targetId is null || targetId == force.TerritoryId)
        {
            return false;
        }

        var intermediates = Intermediates(viaId, viaPath, targetId.Value, force.TerritoryId);
        if (intermediates.Count > 0)
        {
            var speed = EffectiveSpeed(force, rules, map, items, occupyingForces);
            if (intermediates.Count + 1 > speed)
            {
                return false;
            }

            return IsLegalPath(map, force, [force.TerritoryId, .. intermediates, targetId.Value]);
        }

        if (map.AreAdjacent(force.TerritoryId, targetId.Value)
            && FactionSpecialRulePolicies.CanLandOnForMove(map, force, targetId.Value))
        {
            return true;
        }

        return FactionSpecialRulePolicies.RelicAdjacentMoveTargets(map, force, items, rules).Contains(targetId.Value);
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
        SpecialRuleContext rules)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(force);
        var ids = new List<Guid>();
        foreach (var neighborId in map.Neighbors(force.TerritoryId))
        {
            if (!FactionSpecialRulePolicies.CanLandOnForMove(map, force, neighborId) || ids.Contains(neighborId))
            {
                continue;
            }

            ids.Add(neighborId);
        }

        foreach (var hop in EligibleHops(map, force, speed))
        {
            if (!ids.Contains(hop.TargetTerritoryId))
            {
                ids.Add(hop.TargetTerritoryId);
            }
        }

        foreach (var extra in FactionSpecialRulePolicies.RelicAdjacentMoveTargets(map, force, items, rules))
        {
            if (!ids.Contains(extra))
            {
                ids.Add(extra);
            }
        }

        return ids;
    }

    /// <summary>
    /// Every legal path of 1..<paramref name="speed"/> hops. Adjacent destinations use an empty via.
    /// </summary>
    public static IReadOnlyList<MoveHop> EligibleHops(PlayMap map, CampaignForce force, int speed)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(force);
        var hops = new List<MoveHop>();
        if (speed < 2)
        {
            return hops;
        }

        Walk(map, force, force.TerritoryId, speed, [], hops);
        return hops;
    }

    private static void Walk(
        PlayMap map,
        CampaignForce force,
        Guid current,
        int remaining,
        List<Guid> path,
        List<MoveHop> hops)
    {
        if (remaining <= 0)
        {
            return;
        }

        foreach (var next in map.Neighbors(current))
        {
            if (next == force.TerritoryId
                || path.Contains(next)
                || !FactionSpecialRulePolicies.CanEnter(map, force, next))
            {
                continue;
            }

            var nextPath = new List<Guid>(path) { next };
            if (nextPath.Count >= 2 && FactionSpecialRulePolicies.CanLandOnForMove(map, force, next))
            {
                hops.Add(new MoveHop(nextPath[0], next, nextPath.Count == 2 ? [] : [.. nextPath.Skip(1).Take(nextPath.Count - 2)]));
            }

            Walk(map, force, next, remaining - 1, nextPath, hops);
        }
    }

    internal static bool IsLegalStep(PlayMap map, CampaignForce force, Guid from, Guid to)
    {
        return map.AreAdjacent(from, to) && FactionSpecialRulePolicies.CanEnter(map, force, to);
    }

    private static bool IsLegalPath(PlayMap map, CampaignForce force, IReadOnlyList<Guid> path)
    {
        for (var i = 1; i < path.Count; i++)
        {
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
