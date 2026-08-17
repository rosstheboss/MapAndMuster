using System.Text.Json;
using Campaign.Application.Campaigns;
using Campaign.Domain.Campaigns;
using Campaign.Domain.Maps;

namespace Campaign.Infrastructure.Campaigns;

/// <summary>
/// Serializes campaign terrain and structure catalogs for JSONB storage.
/// </summary>
internal static class CatalogJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static string Serialize(
        IReadOnlyList<StoredTerrainType> terrainTypes,
        IReadOnlyList<StoredStructureType> structureTypes,
        IReadOnlyList<StoredItemObjectiveType>? itemObjectiveTypes = null,
        IReadOnlyList<StoredPublicObjectiveType>? publicObjectiveTypes = null,
        BattleScoringSetup? battleScoring = null,
        GeneralPublicObjectivePoints? rankingObjectivePoints = null)
    {
        ArgumentNullException.ThrowIfNull(terrainTypes);
        ArgumentNullException.ThrowIfNull(structureTypes);
        var scoring = battleScoring ?? BattleScoringSetup.Default;
        var ranking = rankingObjectivePoints ?? GeneralPublicObjectivePoints.None;
        return JsonSerializer.Serialize(
            new CatalogDocument
            {
                TerrainTypes = [.. terrainTypes.Select(ToDocument)],
                StructureTypes = [.. structureTypes.Select(ToDocument)],
                ItemObjectiveTypes = [.. (itemObjectiveTypes ?? []).Select(ToDocument)],
                PublicObjectiveTypes = [.. (publicObjectiveTypes ?? []).Select(ToDocument)],
                PointsPerBattleWon = scoring.PointsPerWin,
                BattleScoring = ToDocument(scoring),
                MostTerritoriesCampaignPoints = ranking.MostTerritories,
                LongestTerritoryChainCampaignPoints = ranking.LongestTerritoryChain,
                MostBattlesWonCampaignPoints = ranking.MostBattlesWon,
            },
            Options);
    }

    public static (
        IReadOnlyList<StoredTerrainType> TerrainTypes,
        IReadOnlyList<StoredStructureType> StructureTypes,
        IReadOnlyList<StoredItemObjectiveType> ItemObjectiveTypes,
        IReadOnlyList<StoredPublicObjectiveType> PublicObjectiveTypes,
        BattleScoringSetup BattleScoring,
        GeneralPublicObjectivePoints RankingObjectivePoints)
        Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return ([], [], [], [], BattleScoringSetup.Straight(0), GeneralPublicObjectivePoints.None);
        }

        var document = JsonSerializer.Deserialize<CatalogDocument>(json, Options);
        if (document is null)
        {
            return ([], [], [], [], BattleScoringSetup.Straight(0), GeneralPublicObjectivePoints.None);
        }

        return (
            [.. document.TerrainTypes.Select(FromDocument)],
            [.. document.StructureTypes.Select(FromDocument)],
            [.. (document.ItemObjectiveTypes ?? []).Select(FromDocument)],
            [.. (document.PublicObjectiveTypes ?? []).Select(FromDocument)],
            BattleScoringFrom(document),
            new GeneralPublicObjectivePoints(
                Math.Max(0, document.MostTerritoriesCampaignPoints),
                Math.Max(0, document.LongestTerritoryChainCampaignPoints),
                Math.Max(0, document.MostBattlesWonCampaignPoints)));
    }

    private static TerrainDocument ToDocument(StoredTerrainType type)
    {
        return new TerrainDocument
        {
            Id = type.Id,
            Name = type.Name,
            Color = type.Color,
            Missions = [.. type.Missions.Select(ToDocument)],
            CampaignPoints = type.CampaignPoints,
        };
    }

    private static StructureDocument ToDocument(StoredStructureType type)
    {
        return new StructureDocument
        {
            Id = type.Id,
            Name = type.Name,
            BuiltinSymbol = type.BuiltinSymbol,
            ImageStorageKey = type.ImageStorageKey,
            PillagedImageStorageKey = type.PillagedImageStorageKey,
            IsBuildable = type.IsBuildable,
            IsPillageable = type.IsPillageable,
            IsDestructible = type.IsDestructible,
            Missions = [.. type.Missions.Select(ToDocument)],
            CampaignPoints = type.CampaignPoints,
        };
    }

    private static MissionDocument ToDocument(StoredMission mission)
    {
        return new MissionDocument
        {
            Id = mission.Id,
            Name = mission.Name,
            Url = mission.Url,
            FileStorageKey = mission.FileStorageKey,
            FileName = mission.FileName,
        };
    }

    private static StoredTerrainType FromDocument(TerrainDocument type)
    {
        return new StoredTerrainType
        {
            Id = type.Id,
            Name = type.Name,
            Color = type.Color,
            Missions = [.. type.Missions.Select(FromDocument)],
            CampaignPoints = type.CampaignPoints,
        };
    }

    private static StoredStructureType FromDocument(StructureDocument type)
    {
        var flags = StructureCatalog.DefaultFlags(type.Name, type.BuiltinSymbol);
        return new StoredStructureType
        {
            Id = type.Id,
            Name = type.Name,
            BuiltinSymbol = type.BuiltinSymbol,
            ImageStorageKey = type.ImageStorageKey,
            PillagedImageStorageKey = type.PillagedImageStorageKey,
            IsBuildable = type.IsBuildable ?? flags.IsBuildable,
            IsPillageable = type.IsPillageable ?? flags.IsPillageable,
            IsDestructible = type.IsDestructible ?? flags.IsDestructible,
            Missions = [.. type.Missions.Select(FromDocument)],
            CampaignPoints = type.CampaignPoints,
        };
    }

    private static StoredMission FromDocument(MissionDocument mission)
    {
        return new StoredMission
        {
            Id = mission.Id,
            Name = mission.Name,
            Url = mission.Url,
            FileStorageKey = mission.FileStorageKey,
            FileName = mission.FileName,
        };
    }

    private static ItemObjectiveDocument ToDocument(StoredItemObjectiveType type)
    {
        return new ItemObjectiveDocument
        {
            Id = type.Id,
            Name = type.Name,
            IsHiddenUntilFound = type.IsHiddenUntilFound,
            Placement = type.Placement,
            AllowOnSpawn = type.AllowOnSpawn,
            BuiltinSymbol = type.BuiltinSymbol,
            Color = type.Color,
            ImageStorageKey = type.ImageStorageKey,
            CampaignPoints = type.CampaignPoints,
        };
    }

    private static PublicObjectiveDocument ToDocument(StoredPublicObjectiveType type)
    {
        return new PublicObjectiveDocument
        {
            Id = type.Id,
            Name = type.Name,
            Description = type.Description,
            CampaignPoints = type.CampaignPoints,
        };
    }

    private static StoredItemObjectiveType FromDocument(ItemObjectiveDocument type)
    {
        return new StoredItemObjectiveType
        {
            Id = type.Id,
            Name = type.Name,
            IsHiddenUntilFound = type.IsHiddenUntilFound,
            Placement = type.Placement,
            AllowOnSpawn = type.AllowOnSpawn,
            BuiltinSymbol = string.IsNullOrWhiteSpace(type.BuiltinSymbol) ? "Crown" : type.BuiltinSymbol,
            Color = string.IsNullOrWhiteSpace(type.Color) ? "#C45C26" : type.Color,
            ImageStorageKey = type.ImageStorageKey,
            CampaignPoints = type.CampaignPoints,
        };
    }

    private static StoredPublicObjectiveType FromDocument(PublicObjectiveDocument type)
    {
        return new StoredPublicObjectiveType
        {
            Id = type.Id,
            Name = type.Name,
            Description = type.Description,
            CampaignPoints = type.CampaignPoints,
        };
    }

    private static BattleScoringDocument ToDocument(BattleScoringSetup scoring)
    {
        return new BattleScoringDocument
        {
            PointsPerWin = scoring.PointsPerWin,
            PointsPerDraw = scoring.PointsPerDraw,
            UseDifferential = scoring.UseDifferential,
            DifferentialMultiplier = scoring.DifferentialMultiplier,
            DifferentialMinimum = scoring.DifferentialMinimum,
            DifferentialMaximum = scoring.DifferentialMaximum,
            AllowNegativeDifferential = scoring.AllowNegativeDifferential,
        };
    }

    private static BattleScoringSetup BattleScoringFrom(CatalogDocument document)
    {
        if (document.BattleScoring is null)
        {
            return BattleScoringSetup.Straight(document.PointsPerBattleWon);
        }

        var scoring = document.BattleScoring;
        var multiplier = scoring.DifferentialMultiplier < BattleScoringSetup.MinMultiplier
            ? BattleScoringSetup.DefaultMultiplier
            : Math.Min(scoring.DifferentialMultiplier, BattleScoringSetup.MaxMultiplier);
        var minimum = scoring.DifferentialMinimum;
        var maximum = scoring.DifferentialMaximum < minimum ? minimum : scoring.DifferentialMaximum;
        return new BattleScoringSetup(
            Math.Max(0, scoring.PointsPerWin),
            Math.Max(0, scoring.PointsPerDraw),
            scoring.UseDifferential,
            multiplier,
            minimum,
            maximum,
            scoring.AllowNegativeDifferential);
    }

    private sealed class CatalogDocument
    {
        public List<TerrainDocument> TerrainTypes { get; set; } = [];

        public List<StructureDocument> StructureTypes { get; set; } = [];

        public List<ItemObjectiveDocument> ItemObjectiveTypes { get; set; } = [];

        public List<PublicObjectiveDocument> PublicObjectiveTypes { get; set; } = [];

        public int PointsPerBattleWon { get; set; }

        public BattleScoringDocument? BattleScoring { get; set; }

        public int MostTerritoriesCampaignPoints { get; set; }

        public int LongestTerritoryChainCampaignPoints { get; set; }

        public int MostBattlesWonCampaignPoints { get; set; }
    }

    private sealed class BattleScoringDocument
    {
        public int PointsPerWin { get; set; }

        public int PointsPerDraw { get; set; }

        public bool UseDifferential { get; set; } = true;

        public decimal DifferentialMultiplier { get; set; } = 1m;

        public int DifferentialMinimum { get; set; }

        public int DifferentialMaximum { get; set; } = 10;

        public bool AllowNegativeDifferential { get; set; }
    }

    private sealed class TerrainDocument
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Color { get; set; } = string.Empty;

        public List<MissionDocument> Missions { get; set; } = [];

        public int CampaignPoints { get; set; }
    }

    private sealed class StructureDocument
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? BuiltinSymbol { get; set; }

        public string? ImageStorageKey { get; set; }

        public string? PillagedImageStorageKey { get; set; }

        public bool? IsBuildable { get; set; }

        public bool? IsPillageable { get; set; }

        public bool? IsDestructible { get; set; }

        public List<MissionDocument> Missions { get; set; } = [];

        public int CampaignPoints { get; set; }
    }

    private sealed class ItemObjectiveDocument
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool IsHiddenUntilFound { get; set; } = true;

        public string Placement { get; set; } = "Random";

        public bool AllowOnSpawn { get; set; }

        public string BuiltinSymbol { get; set; } = "Crown";

        public string Color { get; set; } = "#C45C26";

        public string? ImageStorageKey { get; set; }

        public int CampaignPoints { get; set; }
    }

    private sealed class PublicObjectiveDocument
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int CampaignPoints { get; set; }
    }

    private sealed class MissionDocument
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Url { get; set; }

        public string? FileStorageKey { get; set; }

        public string? FileName { get; set; }
    }
}
