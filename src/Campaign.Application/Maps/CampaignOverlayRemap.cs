using Campaign.Application.Campaigns;

namespace Campaign.Application.Maps;

/// <summary>
/// Rewrites overlay catalog identifiers so a preset graph is valid on another campaign.
/// Matching is by catalog name; unmatched terrain falls back to the target's first type.
/// </summary>
internal static class CampaignOverlayRemap
{
    public static StoredMapGraph? ForCampaign(StoredMapGraph? graph, StoredCampaign source, StoredCampaign target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        if (graph is null)
        {
            return null;
        }

        var terrain = MapIds(
            source.TerrainTypes.Select(static type => (type.Id, type.Name)),
            target.TerrainTypes.Select(static type => (type.Id, type.Name)));
        var structures = MapIds(
            source.StructureTypes.Select(static type => (type.Id, type.Name)),
            target.StructureTypes.Select(static type => (type.Id, type.Name)));
        var factions = MapIds(
            source.Factions.Select(static faction => (faction.Id, faction.Name)),
            target.Factions.Select(static faction => (faction.Id, faction.Name)));
        var items = MapIds(
            source.ItemObjectiveTypes.Select(static type => (type.Id, type.Name)),
            target.ItemObjectiveTypes.Select(static type => (type.Id, type.Name)));
        var fallbackTerrain = target.TerrainTypes.Count > 0 ? target.TerrainTypes[0].Id : (Guid?)null;

        return new StoredMapGraph
        {
            Territories =
            [
                .. graph.Territories.Select(territory => new TerritoryDetail
                {
                    Id = territory.Id,
                    DisplayNumber = territory.DisplayNumber,
                    Name = territory.Name,
                    Description = territory.Description,
                    Polygon = territory.Polygon,
                    TerrainTypeId = RemapRequired(territory.TerrainTypeId, terrain, fallbackTerrain),
                    StructureTypeId = RemapOptional(territory.StructureTypeId, structures),
                    StructureCondition = territory.StructureCondition,
                    OverlayColor = territory.OverlayColor,
                    OwnerFactionId = RemapOptional(territory.OwnerFactionId, factions),
                    OwnerSubfaction = territory.OwnerSubfaction,
                    SpawnFactionId = RemapOptional(territory.SpawnFactionId, factions),
                    SpawnSubfaction = territory.SpawnSubfaction,
                }),
            ],
            Adjacencies = graph.Adjacencies,
            ItemObjectivePlacements =
            [
                .. graph.ItemObjectivePlacements
                    .Select(placement => RemapOptional(placement.TypeId, items) is { } typeId
                        ? new ItemObjectivePlacementDetail
                        {
                            TypeId = typeId,
                            TerritoryId = placement.TerritoryId,
                        }
                        : null)
                    .OfType<ItemObjectivePlacementDetail>(),
            ],
        };
    }

    private static Dictionary<Guid, Guid> MapIds(
        IEnumerable<(Guid Id, string Name)> source,
        IEnumerable<(Guid Id, string Name)> target)
    {
        var targetList = target.ToArray();
        var targetIds = targetList.Select(static item => item.Id).ToHashSet();
        var byName = targetList
            .GroupBy(static item => item.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static group => group.Key, static group => group.First().Id, StringComparer.OrdinalIgnoreCase);
        var map = new Dictionary<Guid, Guid>();
        foreach (var item in source)
        {
            if (targetIds.Contains(item.Id))
            {
                map[item.Id] = item.Id;
                continue;
            }

            if (byName.TryGetValue(item.Name.Trim(), out var mapped))
            {
                map[item.Id] = mapped;
            }
        }

        return map;
    }

    private static Guid RemapRequired(Guid sourceId, Dictionary<Guid, Guid> map, Guid? fallback)
    {
        if (map.TryGetValue(sourceId, out var mapped))
        {
            return mapped;
        }

        return fallback ?? sourceId;
    }

    private static Guid? RemapOptional(Guid? sourceId, Dictionary<Guid, Guid> map)
    {
        if (sourceId is not { } id)
        {
            return null;
        }

        return map.TryGetValue(id, out var mapped) ? mapped : null;
    }
}
