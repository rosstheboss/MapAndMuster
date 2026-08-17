using System.Text.Json;
using Campaign.Application.Campaigns;
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
        IReadOnlyList<StoredItemObjectiveType>? itemObjectiveTypes = null)
    {
        ArgumentNullException.ThrowIfNull(terrainTypes);
        ArgumentNullException.ThrowIfNull(structureTypes);
        return JsonSerializer.Serialize(
            new CatalogDocument
            {
                TerrainTypes = [.. terrainTypes.Select(ToDocument)],
                StructureTypes = [.. structureTypes.Select(ToDocument)],
                ItemObjectiveTypes = [.. (itemObjectiveTypes ?? []).Select(ToDocument)],
            },
            Options);
    }

    public static (
        IReadOnlyList<StoredTerrainType> TerrainTypes,
        IReadOnlyList<StoredStructureType> StructureTypes,
        IReadOnlyList<StoredItemObjectiveType> ItemObjectiveTypes)
        Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return ([], [], []);
        }

        var document = JsonSerializer.Deserialize<CatalogDocument>(json, Options);
        if (document is null)
        {
            return ([], [], []);
        }

        return (
            [.. document.TerrainTypes.Select(FromDocument)],
            [.. document.StructureTypes.Select(FromDocument)],
            [.. (document.ItemObjectiveTypes ?? []).Select(FromDocument)]);
    }

    private static TerrainDocument ToDocument(StoredTerrainType type)
    {
        return new TerrainDocument
        {
            Id = type.Id,
            Name = type.Name,
            Color = type.Color,
            Missions = [.. type.Missions.Select(ToDocument)],
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
        };
    }

    private sealed class CatalogDocument
    {
        public List<TerrainDocument> TerrainTypes { get; set; } = [];

        public List<StructureDocument> StructureTypes { get; set; } = [];

        public List<ItemObjectiveDocument> ItemObjectiveTypes { get; set; } = [];
    }

    private sealed class TerrainDocument
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Color { get; set; } = string.Empty;

        public List<MissionDocument> Missions { get; set; } = [];
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
    }

    private sealed class ItemObjectiveDocument
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool IsHiddenUntilFound { get; set; } = true;

        public string Placement { get; set; } = "Random";

        public bool AllowOnSpawn { get; set; }
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
