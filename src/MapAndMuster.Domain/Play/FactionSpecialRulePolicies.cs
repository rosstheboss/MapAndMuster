using MapAndMuster.Domain.Campaigns;

namespace MapAndMuster.Domain.Play;

/// <summary>
/// Typed campaign policies for named special-rule effect keys.
/// </summary>
public static class FactionSpecialRulePolicies
{
    /// <summary>
    /// Returns whether a one- or two-territory Move is legal for this force.
    /// </summary>
    public static bool IsValidMove(
        PlayMap map,
        CampaignForce force,
        Guid? targetId,
        Guid? viaId,
        IReadOnlyList<CampaignItemObjective> items,
        SpecialRuleContext rules)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(force);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(rules);
        if (targetId is null || targetId == force.TerritoryId)
        {
            return false;
        }

        if (viaId is { } via && via != Guid.Empty && via != force.TerritoryId && via != targetId)
        {
            if (!rules.Has(force, SpecialRuleEffectKeys.Crusaders))
            {
                return false;
            }

            return IsLegalStep(map, force, force.TerritoryId, via) && IsLegalStep(map, force, via, targetId.Value);
        }

        if (map.AreAdjacent(force.TerritoryId, targetId.Value) && IsLegalStep(map, force, force.TerritoryId, targetId.Value))
        {
            return true;
        }

        return RelicAdjacentMoveTargets(map, force, items, rules).Contains(targetId.Value);
    }

    /// <summary>
    /// Extra Move destinations after a relic is revealed: any territory adjacent to a revealed relic.
    /// </summary>
    public static IReadOnlyList<Guid> RelicAdjacentMoveTargets(
        PlayMap map,
        CampaignForce force,
        IReadOnlyList<CampaignItemObjective> items,
        SpecialRuleContext rules)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(force);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(rules);
        if (!rules.Has(force, SpecialRuleEffectKeys.ConduitsOfPower))
        {
            return [];
        }

        var ids = new HashSet<Guid>();
        foreach (var item in items)
        {
            if (!item.IsRevealed || item.TerritoryId is not { } relicId)
            {
                continue;
            }

            foreach (var neighbor in map.Neighbors(relicId))
            {
                if (neighbor != force.TerritoryId && CanEnter(map, force, neighbor))
                {
                    ids.Add(neighbor);
                }
            }
        }

        return [.. ids];
    }

    /// <summary>
    /// Returns whether the territory is another faction's or required subfaction's spawn.
    /// </summary>
    /// <param name="territory">The destination territory.</param>
    /// <param name="force">The moving force.</param>
    /// <returns><see langword="true"/> when the force may not enter this spawn.</returns>
    public static bool IsEnemySpawn(PlayTerritory territory, CampaignForce force)
    {
        ArgumentNullException.ThrowIfNull(territory);
        ArgumentNullException.ThrowIfNull(force);
        if (territory.SpawnFactionId is not { } spawnFaction)
        {
            return false;
        }

        if (spawnFaction != force.FactionId)
        {
            return true;
        }

        return !string.IsNullOrEmpty(territory.SpawnSubfaction)
            && !string.Equals(territory.SpawnSubfaction, force.Subfaction, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Returns whether the force may enter the territory (not an enemy spawn).</summary>
    public static bool CanEnter(PlayMap map, CampaignForce force, Guid territoryId)
    {
        var territory = map.Territory(territoryId);
        return territory is not null && !IsEnemySpawn(territory, force);
    }

    /// <summary>
    /// Stops a Crusaders two-hop Move at the first territory when an enemy is encountered there.
    /// </summary>
    public static Guid ResolveMoveDestination(
        PlayMap map,
        CampaignForce force,
        Guid targetId,
        Guid? viaId,
        IReadOnlyList<CampaignForce> startingForces,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlyCollection<Guid> brokenFactions,
        IReadOnlyList<BrokenAllySubfaction> brokenSubfactions,
        SpecialRuleContext rules)
    {
        if (viaId is not { } via || via == Guid.Empty || via == targetId || via == force.TerritoryId)
        {
            return targetId;
        }

        if (!rules.Has(force, SpecialRuleEffectKeys.Crusaders))
        {
            return targetId;
        }

        if (HasEnemy(startingForces, via, force, factionAllyGroups, brokenFactions, brokenSubfactions, rules))
        {
            return via;
        }

        return targetId;
    }

    /// <summary>Returns whether a two-hop Move should skip claiming the via territory.</summary>
    public static bool SkipClaiming(
        CampaignForce force,
        Guid territoryId,
        Guid originId,
        Guid destinationId,
        Guid? viaId,
        SpecialRuleContext rules)
    {
        if (!rules.Has(force, SpecialRuleEffectKeys.Crusaders) || viaId is not { } via)
        {
            return false;
        }

        return territoryId == via && via != originId && via != destinationId;
    }

    /// <summary>Returns whether two forces are enemies, including daemon-god identity.</summary>
    public static bool AreEnemies(
        CampaignForce left,
        CampaignForce right,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlyCollection<Guid> brokenFactions,
        IReadOnlyList<BrokenAllySubfaction> brokenSubfactions,
        SpecialRuleContext rules)
    {
        if (left.Id == right.Id)
        {
            return false;
        }

        if (AreDividedGods(left, right, rules))
        {
            if (SameGod(left, right))
            {
                return false;
            }

            return IsGodBroken(left, brokenSubfactions) || IsGodBroken(right, brokenSubfactions);
        }

        return ActionResolution.AreEnemies(left.FactionId, right.FactionId, factionAllyGroups, brokenFactions);
    }

    /// <summary>Returns whether two forces are allied, including implicit daemon-god alliances.</summary>
    public static bool AreAllies(
        CampaignForce left,
        CampaignForce right,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlyCollection<Guid> brokenFactions,
        IReadOnlyList<BrokenAllySubfaction> brokenSubfactions,
        SpecialRuleContext rules)
    {
        if (AreDividedGods(left, right, rules))
        {
            return !SameGod(left, right)
                && !IsGodBroken(left, brokenSubfactions)
                && !IsGodBroken(right, brokenSubfactions);
        }

        return ActionResolution.AreAllies(left.FactionId, right.FactionId, factionAllyGroups, brokenFactions);
    }

    /// <summary>Returns whether occupying forces start a battle, skipping Skaven spawn fights.</summary>
    public static bool CreatesBattle(
        CampaignForce[] present,
        PlayTerritory territory,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlyCollection<Guid> brokenFactions,
        IReadOnlyList<BrokenAllySubfaction> brokenSubfactions,
        SpecialRuleContext rules)
    {
        if (territory.IsSpawn && present.Any(force => rules.Has(force, SpecialRuleEffectKeys.UndergroundNetwork)))
        {
            return false;
        }

        for (var i = 0; i < present.Length; i++)
        {
            for (var j = i + 1; j < present.Length; j++)
            {
                if (AreEnemies(present[i], present[j], factionAllyGroups, brokenFactions, brokenSubfactions, rules))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>Returns whether a faction/owner pair is allied for structure actions.</summary>
    public static bool AreAllies(
        Guid leftFactionId,
        string? leftSubfaction,
        Guid rightFactionId,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlyCollection<Guid> brokenFactions,
        SpecialRuleContext rules)
    {
        if (leftFactionId == rightFactionId
            && rules.Has(leftFactionId, leftSubfaction, SpecialRuleEffectKeys.DividedWeStand))
        {
            return false;
        }

        return ActionResolution.AreAllies(leftFactionId, rightFactionId, factionAllyGroups, brokenFactions);
    }

    /// <summary>Green Tide cannot build supply depots.</summary>
    public static bool CanBuild(
        PlayMap map,
        CampaignForce force,
        Guid structureTypeId,
        SpecialRuleContext rules)
    {
        if (!rules.Has(force, SpecialRuleEffectKeys.GreenTide))
        {
            return true;
        }

        var type = map.StructureRules(structureTypeId);
        return type is null || !StructureKinds.IsSupplyDepot(type.Name);
    }

    /// <summary>Returns whether Pillage may target an allied structure.</summary>
    public static bool CanPillageAllied(CampaignForce force, SpecialRuleContext rules)
    {
        return rules.Has(force, SpecialRuleEffectKeys.OnlyBloodSatisfies);
    }

    /// <summary>Returns whether Pillage may destroy in one action.</summary>
    public static bool CanDestroyImmediately(CampaignForce force, SpecialRuleContext rules)
    {
        return rules.Has(force, SpecialRuleEffectKeys.OnlyBloodSatisfies);
    }

    /// <summary>Returns whether Crusaders split forces should rejoin when co-located.</summary>
    public static bool ShouldRejoin(
        CampaignForce left,
        CampaignForce right,
        IReadOnlyDictionary<Guid, ActionKind> arrivalKinds,
        SpecialRuleContext rules)
    {
        if (!rules.Has(left, SpecialRuleEffectKeys.Crusaders) && !rules.Has(right, SpecialRuleEffectKeys.Crusaders))
        {
            return true;
        }

        var leftKind = arrivalKinds.GetValueOrDefault(left.Id, ActionKind.Hold);
        var rightKind = arrivalKinds.GetValueOrDefault(right.Id, ActionKind.Hold);
        return leftKind is ActionKind.Move or ActionKind.Split or ActionKind.Retreat
            && rightKind is ActionKind.Move or ActionKind.Split or ActionKind.Retreat;
    }

    /// <summary>Returns whether a named status may apply to this force.</summary>
    public static bool AllowsStatus(CampaignForce force, string statusName, SpecialRuleContext rules)
    {
        if (rules.Has(force, SpecialRuleEffectKeys.Undead)
            && MatchesAny(statusName, "Shaken", "Diseased", "Well Rested", "Confident"))
        {
            return false;
        }

        if (rules.Has(force, SpecialRuleEffectKeys.BringersOfThePlague)
            && MatchesAny(statusName, "Diseased", "Well Rested"))
        {
            return false;
        }

        if (rules.Has(force, SpecialRuleEffectKeys.ToughGuts) && MatchesAny(statusName, "Diseased"))
        {
            return false;
        }

        return true;
    }

    /// <summary>Returns Diseased when a Nurgle force beats a non-Diseased, non-Shaken army.</summary>
    public static string? StatusInflictedOnLoser(CampaignForce winner, CampaignForce loser, SpecialRuleContext rules)
    {
        if (!rules.Has(winner, SpecialRuleEffectKeys.BringersOfThePlague))
        {
            return null;
        }

        if (MatchesAny(loser.StatusName, "Diseased", "Shaken"))
        {
            return null;
        }

        return "Diseased";
    }

    /// <summary>Returns whether a hidden item is adjacent to the force.</summary>
    public static bool HiddenRelicAdjacent(
        PlayMap map,
        CampaignForce force,
        IReadOnlyList<CampaignItemObjective> items,
        SpecialRuleContext rules)
    {
        if (!rules.Has(force, SpecialRuleEffectKeys.ConduitsOfPower))
        {
            return false;
        }

        foreach (var item in items)
        {
            if (item.IsRevealed || item.PossessorForceId is not null || item.TerritoryId is not { } relicId)
            {
                continue;
            }

            if (map.AreAdjacent(force.TerritoryId, relicId))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Move destinations that reduce distance to a revealed relic.</summary>
    public static IReadOnlyList<Guid> RelicPursuitTargets(
        PlayMap map,
        CampaignForce force,
        IReadOnlyList<CampaignItemObjective> items,
        SpecialRuleContext rules)
    {
        if (!rules.Has(force, SpecialRuleEffectKeys.CalledByTheRelic))
        {
            return [];
        }

        var revealed = items
            .Where(static item => item.IsRevealed && item.TerritoryId is not null)
            .Select(static item => item.TerritoryId!.Value)
            .Distinct()
            .ToArray();
        if (revealed.Length == 0)
        {
            return [];
        }

        var current = revealed.Min(id => Distance(map, force.TerritoryId, id));
        if (current == 0)
        {
            return [];
        }

        var closer = new List<Guid>();
        foreach (var neighbor in map.Neighbors(force.TerritoryId))
        {
            if (!CanEnter(map, force, neighbor))
            {
                continue;
            }

            var next = revealed.Min(id => Distance(map, neighbor, id));
            if (next < current)
            {
                closer.Add(neighbor);
            }
        }

        return closer;
    }

    /// <summary>Breadth-first distance, or a large number when unreachable.</summary>
    public static int Distance(PlayMap map, Guid from, Guid to)
    {
        if (from == to)
        {
            return 0;
        }

        var seen = new HashSet<Guid> { from };
        var queue = new Queue<(Guid Id, int Cost)>();
        queue.Enqueue((from, 0));
        while (queue.Count > 0)
        {
            var (id, cost) = queue.Dequeue();
            foreach (var neighbor in map.Neighbors(id))
            {
                if (!seen.Add(neighbor))
                {
                    continue;
                }

                if (neighbor == to)
                {
                    return cost + 1;
                }

                queue.Enqueue((neighbor, cost + 1));
            }
        }

        return 1000;
    }

    private static bool IsLegalStep(PlayMap map, CampaignForce force, Guid from, Guid to)
    {
        return map.AreAdjacent(from, to) && CanEnter(map, force, to);
    }

    private static bool HasEnemy(
        IReadOnlyList<CampaignForce> forces,
        Guid territoryId,
        CampaignForce mover,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlyCollection<Guid> brokenFactions,
        IReadOnlyList<BrokenAllySubfaction> brokenSubfactions,
        SpecialRuleContext rules)
    {
        return forces.Any(force =>
            force.TerritoryId == territoryId
            && force.Id != mover.Id
            && AreEnemies(mover, force, factionAllyGroups, brokenFactions, brokenSubfactions, rules));
    }

    private static bool AreDividedGods(CampaignForce left, CampaignForce right, SpecialRuleContext rules)
    {
        return left.FactionId == right.FactionId
            && rules.Has(left, SpecialRuleEffectKeys.DividedWeStand)
            && rules.Has(right, SpecialRuleEffectKeys.DividedWeStand);
    }

    private static bool SameGod(CampaignForce left, CampaignForce right)
    {
        return string.Equals(left.Subfaction ?? "", right.Subfaction ?? "", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGodBroken(CampaignForce force, IReadOnlyList<BrokenAllySubfaction> broken)
    {
        if (string.IsNullOrWhiteSpace(force.Subfaction))
        {
            return false;
        }

        return broken.Any(item =>
            item.FactionId == force.FactionId
            && string.Equals(item.Subfaction, force.Subfaction, StringComparison.OrdinalIgnoreCase));
    }

    private static bool MatchesAny(string? statusName, params string[] names)
    {
        if (string.IsNullOrWhiteSpace(statusName))
        {
            return false;
        }

        foreach (var name in names)
        {
            if (string.Equals(statusName, name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Starting territory for a newly placed force, including Magritta and Skaven placement.
    /// </summary>
    public static (Guid TerritoryId, bool Capture)? StartingPlacement(
        PlayMap map,
        Guid factionId,
        string? subfaction,
        IReadOnlyList<CampaignForce> existingForces,
        SpecialRuleContext rules,
        Func<int, int> pickIndex)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(existingForces);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(pickIndex);
        if (rules.Has(factionId, subfaction, SpecialRuleEffectKeys.GreatCityOfMagritta))
        {
            var capital = map.Territories.FirstOrDefault(static territory => StructureKinds.IsCapitalCity(territory.StructureName));
            if (capital is not null)
            {
                return (capital.Id, true);
            }
        }

        if (rules.Has(factionId, subfaction, SpecialRuleEffectKeys.UndergroundNetwork))
        {
            var occupied = existingForces.Select(static force => force.TerritoryId).ToHashSet();
            var emptyTowns = map.Territories
                .Where(territory =>
                    StructureKinds.IsTownOrCity(territory.StructureName)
                    && !StructureKinds.IsCapitalCity(territory.StructureName)
                    && !territory.IsSpawn
                    && !occupied.Contains(territory.Id))
                .OrderBy(static territory => territory.DisplayNumber)
                .ToArray();
            if (emptyTowns.Length > 0)
            {
                return (emptyTowns[pickIndex(emptyTowns.Length)].Id, true);
            }

            var capital = map.Territories.FirstOrDefault(static territory => StructureKinds.IsCapitalCity(territory.StructureName));
            if (capital is not null)
            {
                return (capital.Id, false);
            }
        }

        var spawn = map.SpawnFor(factionId, subfaction);
        return spawn is null ? null : (spawn.Id, false);
    }

    /// <summary>Assigns territory ownership to a faction.</summary>
    public static PlayMap Capture(PlayMap map, Guid territoryId, Guid factionId)
    {
        ArgumentNullException.ThrowIfNull(map);
        return map.WithTerritories(
        [
            .. map.Territories.Select(territory =>
                territory.Id == territoryId ? territory.With(ownerFactionId: factionId, assignOwner: true) : territory),
        ]);
    }
}
