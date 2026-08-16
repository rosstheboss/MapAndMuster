using System.Diagnostics.CodeAnalysis;
using Campaign.Domain.Campaigns;

namespace Campaign.Application.Campaigns;

/// <summary>
/// Copies stored file keys onto newly validated catalog items that keep the same identifiers.
/// </summary>
internal static class CatalogFileBinder
{
    public static IReadOnlyList<StoredFaction> BindFactions(
        IReadOnlyList<FactionSetup> incoming,
        IReadOnlyList<StoredFaction>? previous)
    {
        var previousById = previous?.ToDictionary(static faction => faction.Id) ?? [];
        return
        [
            .. incoming.Select(faction =>
            {
                previousById.TryGetValue(faction.Id, out var existing);
                return new StoredFaction
                {
                    Id = faction.Id,
                    Name = faction.Name,
                    Color = faction.Color,
                    Subfactions = faction.Subfactions,
                    AllyGroupName = faction.AllyGroupName,
                    RequiresSubfaction = faction.RequiresSubfaction,
                    FlagImageStorageKey = faction.ClearFlagImage ? null : existing?.FlagImageStorageKey,
                };
            }),
        ];
    }

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
                var pillagedKey = type.ClearPillagedImage ? null : existing?.PillagedImageStorageKey;
                return new StoredStructureType
                {
                    Id = type.Id,
                    Name = type.Name,
                    BuiltinSymbol = imageKey is null ? type.BuiltinSymbol : existing?.BuiltinSymbol ?? type.BuiltinSymbol,
                    ImageStorageKey = imageKey,
                    PillagedImageStorageKey = pillagedKey,
                    Missions = BindMissions(type.Missions, previousMissions),
                };
            }),
        ];
    }

    public static IEnumerable<string> CollectStorageKeys(
        IReadOnlyList<StoredTerrainType>? terrains,
        IReadOnlyList<StoredStructureType>? structures,
        IReadOnlyList<StoredFaction>? factions = null)
    {
        if (terrains is not null)
        {
            foreach (var mission in terrains.SelectMany(static type => type.Missions))
            {
                if (IsUserUploadedFileKey(mission.FileStorageKey))
                {
                    yield return mission.FileStorageKey;
                }
            }
        }

        if (structures is not null)
        {
            foreach (var structure in structures)
            {
                if (IsUserUploadedFileKey(structure.ImageStorageKey))
                {
                    yield return structure.ImageStorageKey;
                }

                if (IsUserUploadedFileKey(structure.PillagedImageStorageKey))
                {
                    yield return structure.PillagedImageStorageKey;
                }

                foreach (var mission in structure.Missions)
                {
                    if (IsUserUploadedFileKey(mission.FileStorageKey))
                    {
                        yield return mission.FileStorageKey;
                    }
                }
            }
        }

        if (factions is not null)
        {
            foreach (var faction in factions)
            {
                if (IsUserUploadedFileKey(faction.FlagImageStorageKey))
                {
                    yield return faction.FlagImageStorageKey;
                }
            }
        }
    }

    public static IEnumerable<string> CollectCampaignStorageKeys(StoredCampaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        if (IsUserUploadedFileKey(campaign.MapStorageKey))
        {
            yield return campaign.MapStorageKey;
        }

        foreach (var key in CollectStorageKeys(campaign.TerrainTypes, campaign.StructureTypes, campaign.Factions))
        {
            yield return key;
        }
    }

    public static IEnumerable<string> UnusedStorageKeys(StoredCampaign previous, StoredCampaign current)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(current);
        var kept = new HashSet<string>(CollectCampaignStorageKeys(current), StringComparer.Ordinal);
        foreach (var key in CollectCampaignStorageKeys(previous))
        {
            if (!kept.Contains(key))
            {
                yield return key;
            }
        }
    }

    internal static bool IsUserUploadedFileKey([NotNullWhen(true)] string? storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || storageKey.Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        if (CampaignCatalogDefaults.CanonicalBuiltinSymbol(storageKey) is not null)
        {
            return false;
        }

        var slash = storageKey.IndexOf('/', StringComparison.Ordinal);
        if (slash <= 0 || slash != storageKey.LastIndexOf('/'))
        {
            return false;
        }

        return storageKey[..slash] is "maps" or "structures" or "flags" or "missions";
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
