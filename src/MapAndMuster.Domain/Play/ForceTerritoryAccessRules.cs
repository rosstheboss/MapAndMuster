using MapAndMuster.Domain.Campaigns;

namespace MapAndMuster.Domain.Play;

/// <summary>
/// Walks a force's owned and allied territory chain, plus special-rule non-contiguous access, to
/// decide spawn and structure reachability for force-status conditions.
/// </summary>
public static class ForceTerritoryAccessRules
{
    /// <summary>
    /// Builds access facts from the force's current territory on <paramref name="map"/>.
    /// </summary>
    public static TerritoryAccessFacts Evaluate(
        PlayMap map,
        CampaignForce force,
        IReadOnlyDictionary<Guid, string?>? allyGroupByFaction = null,
        IReadOnlySet<Guid>? brokenAllyFactionIds = null,
        IReadOnlyList<AllyBetrayal>? allyBetrayals = null,
        SpecialRuleContext? specialRules = null)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(force);
        var groups = allyGroupByFaction ?? new Dictionary<Guid, string?>();
        var broken = brokenAllyFactionIds ?? new HashSet<Guid>();
        var betrayals = allyBetrayals ?? [];
        var rules = specialRules ?? SpecialRuleContext.None;
        var accessible = ConnectedTerritoryIds(map, force, groups, broken, betrayals);
        foreach (var extra in ExtraAccessibleTerritoryIds(map, force, rules, accessible))
        {
            accessible.Add(extra);
        }

        var typeIds = new HashSet<Guid>();
        var tagIds = new HashSet<Guid>();
        CollectStructures(map, force, rules, accessible, typeIds, tagIds);
        return new TerritoryAccessFacts(
            HasFriendlySpawnAccess(map, force, groups, broken, betrayals, accessible),
            [.. typeIds],
            [.. tagIds]);
    }

    private static HashSet<Guid> ConnectedTerritoryIds(
        PlayMap map,
        CampaignForce force,
        IReadOnlyDictionary<Guid, string?> allyGroupByFaction,
        IReadOnlySet<Guid> brokenAllyFactionIds,
        IReadOnlyList<AllyBetrayal> allyBetrayals)
    {
        var connected = new HashSet<Guid> { force.TerritoryId };
        var queue = new Queue<Guid>();
        queue.Enqueue(force.TerritoryId);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var neighborId in map.Neighbors(current))
            {
                if (!connected.Add(neighborId))
                {
                    continue;
                }

                var neighbor = map.Territory(neighborId);
                if (neighbor is null
                    || !InFriendlyNetwork(neighbor, force, allyGroupByFaction, brokenAllyFactionIds, allyBetrayals))
                {
                    connected.Remove(neighborId);
                    continue;
                }

                queue.Enqueue(neighborId);
            }
        }

        return connected;
    }

    private static IEnumerable<Guid> ExtraAccessibleTerritoryIds(
        PlayMap map,
        CampaignForce force,
        SpecialRuleContext rules,
        HashSet<Guid> connected)
    {
        foreach (var territory in map.Territories)
        {
            if (connected.Contains(territory.Id))
            {
                continue;
            }

            if (IsSpawningPoolWater(map, force, rules, territory)
                || IsDefendersNeutralTown(force, rules, territory))
            {
                yield return territory.Id;
            }
        }
    }

    private static void CollectStructures(
        PlayMap map,
        CampaignForce force,
        SpecialRuleContext rules,
        HashSet<Guid> accessible,
        HashSet<Guid> typeIds,
        HashSet<Guid> tagIds)
    {
        foreach (var territoryId in accessible)
        {
            var territory = map.Territory(territoryId);
            if (territory is null)
            {
                continue;
            }

            if (territory.StructureTypeId is { } structureType
                && territory.StructureCondition != StructureCondition.Destroyed)
            {
                typeIds.Add(structureType);
                foreach (var tag in territory.StructureTagIds)
                {
                    tagIds.Add(tag);
                }
            }

            AddVirtualStructures(map, force, rules, territory, typeIds, tagIds);
        }
    }

    private static void AddVirtualStructures(
        PlayMap map,
        CampaignForce force,
        SpecialRuleContext rules,
        PlayTerritory territory,
        HashSet<Guid> typeIds,
        HashSet<Guid> tagIds)
    {
        if (IsSpawningPoolWater(map, force, rules, territory))
        {
            AddNamedStructure(map, typeIds, tagIds, StructureKinds.IsSupplyDepot);
            AddNamedStructure(map, typeIds, tagIds, StructureKinds.IsFortification);
        }

        if (IsGreenTideEmptyLand(force, rules, territory)
            || IsDefendersNeutralTown(force, rules, territory))
        {
            AddNamedStructure(map, typeIds, tagIds, StructureKinds.IsSupplyDepot);
        }
    }

    private static void AddNamedStructure(
        PlayMap map,
        HashSet<Guid> typeIds,
        HashSet<Guid> tagIds,
        Func<string?, bool> matches)
    {
        foreach (var type in map.StructureTypes)
        {
            if (!matches(type.Name))
            {
                continue;
            }

            typeIds.Add(type.Id);
            foreach (var tag in type.TagIds)
            {
                tagIds.Add(tag);
            }
        }
    }

    private static bool HasFriendlySpawnAccess(
        PlayMap map,
        CampaignForce force,
        IReadOnlyDictionary<Guid, string?> allyGroupByFaction,
        IReadOnlySet<Guid> brokenAllyFactionIds,
        IReadOnlyList<AllyBetrayal> allyBetrayals,
        HashSet<Guid> accessible)
    {
        foreach (var spawnId in FriendlySpawnIds(map, force, allyGroupByFaction, brokenAllyFactionIds, allyBetrayals))
        {
            if (accessible.Contains(spawnId))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<Guid> FriendlySpawnIds(
        PlayMap map,
        CampaignForce force,
        IReadOnlyDictionary<Guid, string?> allyGroupByFaction,
        IReadOnlySet<Guid> brokenAllyFactionIds,
        IReadOnlyList<AllyBetrayal> allyBetrayals)
    {
        var own = map.SpawnFor(force.FactionId, force.Subfaction);
        if (own is not null)
        {
            yield return own.Id;
        }

        var selfGroup = allyGroupByFaction.GetValueOrDefault(force.FactionId);
        if (string.IsNullOrWhiteSpace(selfGroup) || brokenAllyFactionIds.Contains(force.FactionId))
        {
            yield break;
        }

        foreach (var (factionId, group) in allyGroupByFaction)
        {
            if (factionId == force.FactionId
                || !string.Equals(selfGroup, group, StringComparison.Ordinal)
                || brokenAllyFactionIds.Contains(factionId)
                || AllyBetrayalRules.PlayerBetrayedFaction(force.ControllerUserId, factionId, null, allyBetrayals))
            {
                continue;
            }

            var spawn = map.SpawnFor(factionId);
            if (spawn is not null)
            {
                yield return spawn.Id;
            }
        }
    }

    private static bool InFriendlyNetwork(
        PlayTerritory territory,
        CampaignForce force,
        IReadOnlyDictionary<Guid, string?> allyGroupByFaction,
        IReadOnlySet<Guid> brokenAllyFactionIds,
        IReadOnlyList<AllyBetrayal> allyBetrayals)
    {
        if (territory.OwnerFactionId is not { } owner)
        {
            return false;
        }

        if (owner == force.FactionId)
        {
            return true;
        }

        if (brokenAllyFactionIds.Contains(force.FactionId) || brokenAllyFactionIds.Contains(owner))
        {
            return false;
        }

        if (AllyBetrayalRules.PlayerBetrayedFaction(force.ControllerUserId, owner, null, allyBetrayals))
        {
            return false;
        }

        var selfGroup = allyGroupByFaction.GetValueOrDefault(force.FactionId);
        var ownerGroup = allyGroupByFaction.GetValueOrDefault(owner);
        return !string.IsNullOrWhiteSpace(selfGroup)
            && string.Equals(selfGroup, ownerGroup, StringComparison.Ordinal);
    }

    private static bool IsSpawningPoolWater(
        PlayMap map,
        CampaignForce force,
        SpecialRuleContext rules,
        PlayTerritory territory)
    {
        return rules.Has(force, SpecialRuleEffectKeys.SpawningPools)
            && territory.OwnerFactionId == force.FactionId
            && map.IsWaterFeature(territory)
            && !StructureKinds.IsSettlement(territory.StructureName)
            && (territory.StructureTypeId is null || territory.StructureCondition != StructureCondition.Operational);
    }

    private static bool IsDefendersNeutralTown(
        CampaignForce force,
        SpecialRuleContext rules,
        PlayTerritory territory)
    {
        return rules.Has(force, SpecialRuleEffectKeys.DefendersOfTheHomeland)
            && territory.OwnerFactionId is null
            && territory.StructureCondition == StructureCondition.Operational
            && StructureKinds.IsTownOrCity(territory.StructureName);
    }

    private static bool IsGreenTideEmptyLand(
        CampaignForce force,
        SpecialRuleContext rules,
        PlayTerritory territory)
    {
        return rules.Has(force, SpecialRuleEffectKeys.GreenTide)
            && territory.OwnerFactionId == force.FactionId
            && (territory.StructureTypeId is null || territory.StructureCondition == StructureCondition.Pillaged);
    }
}
