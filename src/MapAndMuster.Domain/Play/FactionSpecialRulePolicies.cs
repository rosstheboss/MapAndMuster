using MapAndMuster.Domain.Campaigns;

namespace MapAndMuster.Domain.Play;

/// <summary>
/// Typed campaign policies for named special-rule effect keys.
/// </summary>
public static class FactionSpecialRulePolicies
{
    /// <summary>
    /// Returns whether a one- or multi-territory Move is legal for this force.
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
        return ForceMovementRules.IsValidMove(map, force, targetId, viaId, items, rules, viaPath, occupyingForces);
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
                if (neighbor != force.TerritoryId && CanLandOnForMove(map, force, neighbor))
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

    /// <summary>Returns whether the force may pass through the territory (not another faction's spawn).</summary>
    public static bool CanEnter(PlayMap map, CampaignForce force, Guid territoryId)
    {
        var territory = map.Territory(territoryId);
        return territory is not null && !IsEnemySpawn(territory, force);
    }

    /// <summary>
    /// Returns whether a Move or Split may end on the territory. Spawn landings are retreat-only.
    /// </summary>
    public static bool CanLandOnForMove(PlayMap map, CampaignForce force, Guid territoryId)
    {
        var territory = map.Territory(territoryId);
        return territory is not null && !territory.IsSpawn && CanEnter(map, force, territoryId);
    }

    /// <summary>
    /// Stops a multi-hop Move at the first territory when an enemy is encountered there.
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
        SpecialRuleContext rules,
        IReadOnlyList<AllyBetrayal>? allyBetrayals = null,
        IReadOnlyList<Guid>? viaPath = null)
    {
        return ForceMovementRules.ResolveDestination(
            map,
            force,
            targetId,
            viaId,
            startingForces,
            factionAllyGroups,
            brokenFactions,
            brokenSubfactions,
            rules,
            allyBetrayals,
            viaPath);
    }

    /// <summary>Returns whether a multi-hop Move should skip claiming an intermediate territory.</summary>
    public static bool SkipClaiming(
        CampaignForce force,
        Guid territoryId,
        Guid originId,
        Guid destinationId,
        Guid? viaId,
        SpecialRuleContext rules,
        IReadOnlyList<Guid>? viaPath = null)
    {
        _ = force;
        _ = rules;
        return ForceMovementRules.SkipClaiming(territoryId, originId, destinationId, viaId, viaPath);
    }

    /// <summary>Returns whether two forces are enemies, including daemon-god identity.</summary>
    public static bool AreEnemies(
        CampaignForce left,
        CampaignForce right,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlyCollection<Guid> brokenFactions,
        IReadOnlyList<BrokenAllySubfaction> brokenSubfactions,
        SpecialRuleContext rules,
        IReadOnlyList<AllyBetrayal>? allyBetrayals = null,
        PlayMap? map = null,
        IReadOnlyList<CampaignItemObjective>? items = null)
    {
        if (left.Id == right.Id || left.ControllerUserId == right.ControllerUserId)
        {
            return false;
        }

        if (AllyBetrayalRules.AreHostile(left, right, allyBetrayals ?? []))
        {
            return true;
        }

        if (map is not null
            && items is not null
            && ItemObjectiveEffectRules.ForcesAlliedByItem(left, right, map, items, rules))
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

        return ActionResolution.AreEnemies(left.FactionId, right.FactionId, EffectiveAllyGroups(left, right, factionAllyGroups, map, items, rules), brokenFactions);
    }

    /// <summary>Returns whether two forces are allied, including implicit daemon-god alliances.</summary>
    public static bool AreAllies(
        CampaignForce left,
        CampaignForce right,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlyCollection<Guid> brokenFactions,
        IReadOnlyList<BrokenAllySubfaction> brokenSubfactions,
        SpecialRuleContext rules,
        IReadOnlyList<AllyBetrayal>? allyBetrayals = null,
        PlayMap? map = null,
        IReadOnlyList<CampaignItemObjective>? items = null)
    {
        if (left.ControllerUserId == right.ControllerUserId)
        {
            return false;
        }

        if (AllyBetrayalRules.AreHostile(left, right, allyBetrayals ?? []))
        {
            return false;
        }

        if (map is not null
            && items is not null
            && ItemObjectiveEffectRules.ForcesAlliedByItem(left, right, map, items, rules))
        {
            return true;
        }

        if (AreDividedGods(left, right, rules))
        {
            return !SameGod(left, right)
                && !IsGodBroken(left, brokenSubfactions)
                && !IsGodBroken(right, brokenSubfactions);
        }

        var groups = EffectiveAllyGroups(left, right, factionAllyGroups, map, items, rules);
        return ActionResolution.AreAllies(left.FactionId, right.FactionId, groups, brokenFactions);
    }

    /// <summary>Returns whether occupying forces start a battle, skipping Skaven spawn fights.</summary>
    public static bool CreatesBattle(
        CampaignForce[] present,
        PlayTerritory territory,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlyCollection<Guid> brokenFactions,
        IReadOnlyList<BrokenAllySubfaction> brokenSubfactions,
        SpecialRuleContext rules,
        IReadOnlyList<AllyBetrayal>? allyBetrayals = null)
    {
        if (territory.IsSpawn && present.Any(force => rules.Has(force, SpecialRuleEffectKeys.UndergroundNetwork)))
        {
            return false;
        }

        for (var i = 0; i < present.Length; i++)
        {
            for (var j = i + 1; j < present.Length; j++)
            {
                if (AreEnemies(
                    present[i],
                    present[j],
                    factionAllyGroups,
                    brokenFactions,
                    brokenSubfactions,
                    rules,
                    allyBetrayals))
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

    /// <summary>Returns whether co-located same-player split forces should rejoin.</summary>
    public static bool ShouldRejoin(
        CampaignForce left,
        CampaignForce right,
        IReadOnlyDictionary<Guid, ActionKind> arrivalKinds,
        SpecialRuleContext rules)
    {
        _ = left;
        _ = right;
        _ = arrivalKinds;
        _ = rules;
        return true;
    }

    /// <summary>Returns whether a named status may apply to this force.</summary>
    public static bool AllowsStatus(
        CampaignForce force,
        string statusName,
        SpecialRuleContext rules,
        PlayMap? map = null,
        IReadOnlyList<CampaignItemObjective>? items = null)
    {
        if (map is not null
            && items is not null
            && ItemObjectiveEffectRules.IsImmuneToStatus(force, statusName, map, items, rules))
        {
            return false;
        }

        if (HasUndeadStatusImmunity(force, rules) && !ForceStatusNames.IsNormal(statusName))
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

    /// <summary>
    /// Returns Diseased when a plague-bearing force wins a fought battle, regardless of location.
    /// Diseased overrides the loser's other statuses. Immune factions are refused by
    /// <see cref="AllowsStatus"/>.
    /// </summary>
    public static string? StatusInflictedOnLoser(CampaignForce winner, CampaignForce loser, SpecialRuleContext rules)
    {
        if (!rules.Has(winner, SpecialRuleEffectKeys.BringersOfThePlague))
        {
            return null;
        }

        if (ForceStatusNames.IsDiseased(loser.StatusName))
        {
            return null;
        }

        return ForceStatusNames.Diseased;
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

    /// <summary>
    /// +1 Move speed while a revealed, non-destroyed item exists and no force of this faction holds one.
    /// </summary>
    public static int CalledByTheRelicSpeedBonus(
        CampaignForce force,
        IReadOnlyList<CampaignForce> occupyingForces,
        IReadOnlyList<CampaignItemObjective> items,
        SpecialRuleContext rules)
    {
        ArgumentNullException.ThrowIfNull(force);
        ArgumentNullException.ThrowIfNull(occupyingForces);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(rules);
        if (!rules.Has(force, SpecialRuleEffectKeys.CalledByTheRelic))
        {
            return 0;
        }

        var live = items.Where(static item => !item.IsDestroyed).ToArray();
        if (!live.Any(static item => item.IsRevealed))
        {
            return 0;
        }

        var factionForceIds = occupyingForces
            .Where(other => other.FactionId == force.FactionId)
            .Select(static other => other.Id)
            .ToHashSet();
        factionForceIds.Add(force.Id);
        if (live.Any(item => item.PossessorForceId is Guid possessorId && factionForceIds.Contains(possessorId)))
        {
            return 0;
        }

        return 1;
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
        SpecialRuleContext rules,
        IReadOnlyList<AllyBetrayal>? allyBetrayals)
    {
        return forces.Any(force =>
            force.TerritoryId == territoryId
            && force.Id != mover.Id
            && AreEnemies(mover, force, factionAllyGroups, brokenFactions, brokenSubfactions, rules, allyBetrayals));
    }

    private static IReadOnlyDictionary<Guid, string?> EffectiveAllyGroups(
        CampaignForce left,
        CampaignForce right,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        PlayMap? map,
        IReadOnlyList<CampaignItemObjective>? items,
        SpecialRuleContext rules)
    {
        if (map is null || items is null)
        {
            return factionAllyGroups;
        }

        var next = new Dictionary<Guid, string?>(factionAllyGroups);
        Apply(left);
        Apply(right);
        return next;

        void Apply(CampaignForce force)
        {
            var forced = ItemObjectiveEffectRules.ForcedAllyGroupName(force, map, items, rules);
            if (!string.IsNullOrWhiteSpace(forced))
            {
                next[force.FactionId] = forced;
                return;
            }

            if (ItemObjectiveEffectRules.SuspendsAllyGroup(force, map, items, rules))
            {
                next[force.FactionId] = null;
            }
        }
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

    private static bool HasUndeadStatusImmunity(CampaignForce force, SpecialRuleContext rules)
    {
        return rules.Has(force, SpecialRuleEffectKeys.Undead)
            || rules.Has(force, SpecialRuleEffectKeys.CalledByTheRelic);
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
            return UndergroundNetworkPlacement(map, existingForces, pickIndex);
        }

        var spawn = map.SpawnFor(factionId, subfaction);
        return spawn is null ? null : (spawn.Id, false);
    }

    /// <summary>
    /// Territory used when a force would be sent to spawn. Underground Network uses the same
    /// Town or City pick as the initial placement.
    /// </summary>
    public static (Guid TerritoryId, bool Capture)? ForcedSpawnPlacement(
        PlayMap map,
        Guid factionId,
        string? subfaction,
        IReadOnlyList<CampaignForce> existingForces,
        SpecialRuleContext rules,
        Func<int, int> pickIndex,
        IReadOnlySet<Guid>? blockedTerritoryIds = null)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(existingForces);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(pickIndex);
        if (rules.Has(factionId, subfaction, SpecialRuleEffectKeys.UndergroundNetwork))
        {
            return UndergroundNetworkPlacement(map, existingForces, pickIndex, blockedTerritoryIds);
        }

        var spawn = map.SpawnFor(factionId, subfaction);
        return spawn is null ? null : (spawn.Id, false);
    }

    /// <summary>
    /// Random Town or City: unoccupied Neutral first, then unoccupied owned, then occupied.
    /// Capture empty land that is not a spawn or Capital City.
    /// </summary>
    private static (Guid TerritoryId, bool Capture)? UndergroundNetworkPlacement(
        PlayMap map,
        IReadOnlyList<CampaignForce> existingForces,
        Func<int, int> pickIndex,
        IReadOnlySet<Guid>? blockedTerritoryIds = null)
    {
        var occupiedIds = existingForces.Select(static force => force.TerritoryId).ToHashSet();
        var blocked = blockedTerritoryIds ?? new HashSet<Guid>();
        var settlements = map.Territories
            .Where(territory =>
                territory.StructureCondition != StructureCondition.Destroyed
                && !blocked.Contains(territory.Id)
                && (StructureKinds.IsTownOrCity(territory.StructureName)
                    || StructureKinds.IsCapitalCity(territory.StructureName)))
            .OrderBy(static territory => territory.DisplayNumber)
            .ToArray();
        var unoccupiedNeutral = settlements
            .Where(territory => !occupiedIds.Contains(territory.Id) && territory.OwnerFactionId is null)
            .ToArray();
        if (unoccupiedNeutral.Length > 0)
        {
            return CaptureChoice(unoccupiedNeutral[pickIndex(unoccupiedNeutral.Length)]);
        }

        var unoccupiedOwned = settlements
            .Where(territory => !occupiedIds.Contains(territory.Id) && territory.OwnerFactionId is not null)
            .ToArray();
        if (unoccupiedOwned.Length > 0)
        {
            return CaptureChoice(unoccupiedOwned[pickIndex(unoccupiedOwned.Length)]);
        }

        var occupied = settlements.Where(territory => occupiedIds.Contains(territory.Id)).ToArray();
        if (occupied.Length > 0)
        {
            return (occupied[pickIndex(occupied.Length)].Id, false);
        }

        return null;
    }

    private static (Guid TerritoryId, bool Capture) CaptureChoice(PlayTerritory territory)
    {
        var capture = !territory.IsSpawn && !StructureKinds.IsCapitalCity(territory.StructureName);
        return (territory.Id, capture);
    }

    /// <summary>Assigns territory ownership to a faction, and to a required subfaction when one applies.</summary>
    public static PlayMap Capture(
        PlayMap map,
        Guid territoryId,
        Guid factionId,
        string? ownerSubfaction = null,
        SpecialRuleContext? specialRules = null)
    {
        ArgumentNullException.ThrowIfNull(map);
        var rules = specialRules ?? SpecialRuleContext.None;
        var subfaction = rules.FactionRequiresSubfaction(factionId) ? ownerSubfaction : null;
        return map.WithTerritories(
        [
            .. map.Territories.Select(territory =>
                territory.Id == territoryId
                    ? territory.With(
                        ownerFactionId: factionId,
                        assignOwner: true,
                        ownerSubfaction: subfaction,
                        assignOwnerSubfaction: true)
                    : territory),
        ]);
    }
}
