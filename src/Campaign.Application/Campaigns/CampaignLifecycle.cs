using Campaign.Domain.Campaigns;
using Campaign.Domain.Play;

namespace Campaign.Application.Campaigns;

/// <summary>
/// Lifecycle helpers for launched campaigns.
/// </summary>
internal static class CampaignLifecycle
{
    /// <summary>
    /// Evaluates status from stored play windows when present; otherwise from the template schedule.
    /// </summary>
    public static CampaignProgress Progress(StoredCampaign campaign, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        if (campaign.PlayState is { Windows.Count: > 0 } play)
        {
            return play.Evaluate(campaign.StartsUtc, campaign.EndsUtc, utcNow);
        }

        return CampaignMapper.ToSchedule(campaign).Evaluate(utcNow);
    }

    /// <summary>
    /// Whether the campaign has left the setup window.
    /// </summary>
    public static bool HasLaunched(StoredCampaign campaign, DateTimeOffset utcNow)
    {
        return Progress(campaign, utcNow).Status != CampaignStatus.Scheduled;
    }

    /// <summary>
    /// Message used when a manager tries to change locked setup after launch.
    /// </summary>
    public const string LockedMessage =
        "This campaign has launched. The map, name, description, factions, catalogs, and phase order can no longer be changed.";

    /// <summary>
    /// Builds a play map from the stored overlay and structure catalog names.
    /// </summary>
    public static PlayMap ToPlayMap(StoredCampaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        var graph = campaign.MapGraph;
        if (graph is null)
        {
            return new PlayMap([], []);
        }

        var catalog = campaign.StructureTypes
            .Select(static type => new StructureTypePlayRules(
                type.Id,
                type.Name,
                type.IsBuildable,
                type.IsPillageable,
                type.IsDestructible))
            .ToArray();
        var names = catalog.ToDictionary(type => type.Id, type => type.Name);
        var rulesById = catalog.ToDictionary(static type => type.Id);
        var waterByTerrain = campaign.TerrainTypes.ToDictionary(static type => type.Id, static type => type.IsWaterFeature);
        var conditions = campaign.PlayState?.Structures.ToDictionary(item => item.TerritoryId) ?? [];
        var territories = graph.Territories.Select(territory =>
        {
            conditions.TryGetValue(territory.Id, out var structure);
            var structureTypeId = structure?.StructureTypeId ?? territory.StructureTypeId;
            names.TryGetValue(structureTypeId ?? Guid.Empty, out var structureName);
            rulesById.TryGetValue(structureTypeId ?? Guid.Empty, out var rules);
            var condition = structure?.Condition
                ?? ParseCondition(territory.StructureCondition)
                ?? StructureCondition.Operational;
            var intact = structureTypeId is not null && condition != StructureCondition.Destroyed;
            waterByTerrain.TryGetValue(territory.TerrainTypeId, out var isWater);
            return new PlayTerritory(
                territory.Id,
                territory.DisplayNumber,
                territory.OwnerFactionId,
                territory.SpawnFactionId,
                intact ? structureTypeId : null,
                intact ? structureName : null,
                intact ? condition : StructureCondition.Operational,
                intact && (rules?.IsPillageable ?? false),
                intact && (rules?.IsDestructible ?? false),
                isWater);
        }).ToArray();
        var edges = graph.Adjacencies
            .Select(edge => (edge.TerritoryAId, edge.TerritoryBId))
            .ToArray();
        return new PlayMap(territories, edges, catalog);
    }

    /// <summary>
    /// Copies ownership from a play map onto the stored overlay graph.
    /// </summary>
    public static Application.Maps.StoredMapGraph ApplyOwnership(
        Application.Maps.StoredMapGraph graph,
        PlayMap map)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(map);
        var territories = graph.Territories.Select(territory =>
        {
            var play = map.Territory(territory.Id);
            if (play is null)
            {
                return territory;
            }

            return new Application.Maps.TerritoryDetail
            {
                Id = territory.Id,
                DisplayNumber = territory.DisplayNumber,
                Name = territory.Name,
                Description = territory.Description,
                Polygon = territory.Polygon,
                TerrainTypeId = territory.TerrainTypeId,
                StructureTypeId = play.StructureTypeId,
                StructureCondition = play.StructureCondition.ToString(),
                OverlayColor = territory.OverlayColor,
                OwnerFactionId = play.OwnerFactionId,
                SpawnFactionId = territory.SpawnFactionId,
            };
        }).ToArray();
        return new Application.Maps.StoredMapGraph
        {
            Territories = territories,
            Adjacencies = graph.Adjacencies,
        };
    }

    private static StructureCondition? ParseCondition(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Enum.TryParse<StructureCondition>(value, ignoreCase: true, out var condition) && Enum.IsDefined(condition)
            ? condition
            : null;
    }
}
