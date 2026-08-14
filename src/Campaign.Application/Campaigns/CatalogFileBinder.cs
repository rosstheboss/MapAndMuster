using Campaign.Domain.Campaigns;

namespace Campaign.Application.Campaigns;

/// <summary>
/// Copies stored file keys onto newly validated catalog items that keep the same identifiers.
/// </summary>
internal static class CatalogFileBinder
{
    public static IReadOnlyList<StoredTerrainType> BindTerrains(
        IReadOnlyList<TerrainTypeSetup> incoming,
        IReadOnlyList<StoredTerrainType>? previous)
    {
        var previousMissions = IndexMissions(previous?.SelectMany(static type => type.Missions));
        return
        [
            .. incoming.Select(type => new StoredTerrainType
            {
                Id = type.Id,
                Name = type.Name,
                Color = type.Color,
                Missions = BindMissions(type.Missions, previousMissions),
            }),
        ];
    }

    public static IReadOnlyList<StoredStructureType> BindStructures(
        IReadOnlyList<StructureTypeSetup> incoming,
        IReadOnlyList<StoredStructureType>? previous)
    {
        var previousById = previous?.ToDictionary(static type => type.Id) ?? [];
        var previousMissions = IndexMissions(previous?.SelectMany(static type => type.Missions));
        return
        [
            .. incoming.Select(type =>
            {
                previousById.TryGetValue(type.Id, out var existing);
                var imageKey = type.ClearImage ? null : existing?.ImageStorageKey;
                return new StoredStructureType
                {
                    Id = type.Id,
                    Name = type.Name,
                    BuiltinSymbol = imageKey is null ? type.BuiltinSymbol : existing?.BuiltinSymbol ?? type.BuiltinSymbol,
                    ImageStorageKey = imageKey,
                    Missions = BindMissions(type.Missions, previousMissions),
                };
            }),
        ];
    }

    public static IEnumerable<string> CollectStorageKeys(
        IReadOnlyList<StoredTerrainType>? terrains,
        IReadOnlyList<StoredStructureType>? structures)
    {
        if (terrains is not null)
        {
            foreach (var mission in terrains.SelectMany(static type => type.Missions))
            {
                if (!string.IsNullOrWhiteSpace(mission.FileStorageKey))
                {
                    yield return mission.FileStorageKey;
                }
            }
        }

        if (structures is not null)
        {
            foreach (var structure in structures)
            {
                if (!string.IsNullOrWhiteSpace(structure.ImageStorageKey))
                {
                    yield return structure.ImageStorageKey;
                }

                foreach (var mission in structure.Missions)
                {
                    if (!string.IsNullOrWhiteSpace(mission.FileStorageKey))
                    {
                        yield return mission.FileStorageKey;
                    }
                }
            }
        }
    }

    private static IReadOnlyList<StoredMission> BindMissions(
        IReadOnlyList<MissionSetup> incoming,
        Dictionary<Guid, StoredMission> previous)
    {
        return
        [
            .. incoming.Select(mission =>
            {
                previous.TryGetValue(mission.Id, out var existing);
                var keepFile = !mission.ClearFile && mission.Url is null;
                return new StoredMission
                {
                    Id = mission.Id,
                    Name = mission.Name,
                    Url = mission.Url,
                    FileStorageKey = keepFile ? existing?.FileStorageKey : null,
                    FileName = keepFile ? existing?.FileName : null,
                };
            }),
        ];
    }

    private static Dictionary<Guid, StoredMission> IndexMissions(IEnumerable<StoredMission>? missions)
    {
        var indexed = new Dictionary<Guid, StoredMission>();
        if (missions is null)
        {
            return indexed;
        }

        foreach (var mission in missions)
        {
            indexed[mission.Id] = mission;
        }

        return indexed;
    }
}
