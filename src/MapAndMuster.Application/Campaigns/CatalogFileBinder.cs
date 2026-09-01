using System.Diagnostics.CodeAnalysis;
using MapAndMuster.Domain.Campaigns;

namespace MapAndMuster.Application.Campaigns;

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
                var previousAppearances = existing?.SubfactionAppearances
                    .ToDictionary(static item => item.Name, StringComparer.OrdinalIgnoreCase)
                    ?? new Dictionary<string, StoredSubfactionAppearance>(StringComparer.OrdinalIgnoreCase);
                return new StoredFaction
                {
                    Id = faction.Id,
                    Name = faction.Name,
                    Color = faction.Color,
                    Subfactions = faction.Subfactions,
                    SubfactionAppearances =
                    [
                        .. faction.SubfactionAppearances.Select(appearance =>
                        {
                            previousAppearances.TryGetValue(appearance.Name, out var previous);
                            return new StoredSubfactionAppearance
                            {
                                Name = appearance.Name,
                                Color = appearance.Color,
                                FlagSource = appearance.FlagSource,
                                FlagImageStorageKey = appearance.ClearFlagImage ? null : previous?.FlagImageStorageKey,
                                TintFlagImage = appearance.TintFlagImage,
                            };
                        }),
                    ],
                    AllyGroupName = faction.AllyGroupName,
                    RequiresSubfaction = faction.RequiresSubfaction,
                    FlagImageStorageKey = faction.ClearFlagImage ? null : existing?.FlagImageStorageKey,
                    TintFlagImage = faction.TintFlagImage,
                    SpecialRuleIds = faction.SpecialRuleIds,
                    SubfactionSpecialRules = faction.SubfactionSpecialRules
                        .Select(static item => new SubfactionSpecialRulesDetail
                        {
                            Name = item.Name,
                            SpecialRuleIds = item.SpecialRuleIds,
                        })
                        .ToArray(),
                };
            }),
        ];
    }

    public static IReadOnlyList<StoredTerrainType> BindTerrains(
        IReadOnlyList<TerrainTypeSetup> incoming,
        IReadOnlyList<StoredTerrainType>? previous,
        IReadOnlyList<StoredMission>? extraMissions = null)
    {
        var previousMissions = IndexMissions(
            (previous?.SelectMany(static type => type.Missions) ?? []).Concat(extraMissions ?? []));
        return
        [
            .. incoming.Select(type => new StoredTerrainType
            {
                Id = type.Id,
                Name = type.Name,
                Color = type.Color,
                Missions = BindMissions(type.Missions, previousMissions),
                CampaignPoints = 0,
                IsWaterFeature = type.IsWaterFeature,
                SupplyPoints = type.SupplyPoints,
            }),
        ];
    }

    public static IReadOnlyList<StoredStructureType> BindStructures(
        IReadOnlyList<StructureTypeSetup> incoming,
        IReadOnlyList<StoredStructureType>? previous,
        IReadOnlyList<StoredMission>? extraMissions = null)
    {
        var previousById = previous?.ToDictionary(static type => type.Id) ?? [];
        var previousMissions = IndexMissions(
            (previous?.SelectMany(static type => type.Missions) ?? []).Concat(extraMissions ?? []));
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
                    IsBuildable = type.IsBuildable,
                    IsPillageable = type.IsPillageable,
                    IsDestructible = type.IsDestructible,
                    Missions = BindMissions(type.Missions, previousMissions),
                    CampaignPoints = type.CampaignPoints,
                    SupplyPoints = type.SupplyPoints,
                    PillageSupplyPoints = type.PillageSupplyPoints,
                    DestroySupplyPoints = type.DestroySupplyPoints,
                };
            }),
        ];
    }

    public static IReadOnlyList<StoredItemObjectiveType> BindItemObjectives(
        IReadOnlyList<ItemObjectiveTypeSetup> incoming,
        IReadOnlyList<StoredItemObjectiveType>? previous = null)
    {
        var previousById = previous?.ToDictionary(static type => type.Id) ?? [];
        return
        [
            .. incoming.Select(type =>
            {
                previousById.TryGetValue(type.Id, out var existing);
                var imageKey = type.ClearImage ? null : existing?.ImageStorageKey;
                return new StoredItemObjectiveType
                {
                    Id = type.Id,
                    Name = type.Name,
                    IsHiddenUntilFound = type.IsHiddenUntilFound,
                    Placement = type.Placement.ToString(),
                    AllowOnSpawn = type.AllowOnSpawn,
                    BuiltinSymbol = imageKey is null ? type.BuiltinSymbol : existing?.BuiltinSymbol ?? type.BuiltinSymbol,
                    Color = type.Color,
                    ImageStorageKey = imageKey,
                    CampaignPoints = type.CampaignPoints,
                    FlavorText = type.FlavorText,
                    Choices =
                    [
                        .. type.Choices.Select(static choice => new StoredItemObjectiveChoice
                        {
                            Id = choice.Id,
                            Name = choice.Name,
                            Results =
                            [
                                .. choice.Results.Select(static result => new StoredItemObjectiveChoiceResult
                                {
                                    Id = result.Id,
                                    FlavorText = result.FlavorText,
                                    NewStateKey = result.NewStateKey,
                                    DestroyItem = result.DestroyItem,
                                    ReplacementItemTypeId = result.ReplacementItemTypeId,
                                    GrantedPrivateObjectiveTypeId = result.GrantedPrivateObjectiveTypeId,
                                }),
                            ],
                        }),
                    ],
                    SpecialRuleIds = type.SpecialRuleIds,
                };
            }),
        ];
    }

    public static IReadOnlyList<StoredPublicObjectiveType> BindPublicObjectives(
        IReadOnlyList<PublicObjectiveTypeSetup> incoming)
    {
        return
        [
            .. incoming.Select(static type => new StoredPublicObjectiveType
            {
                Id = type.Id,
                Name = type.Name,
                Description = type.Description,
                CampaignPoints = type.CampaignPoints,
            }),
        ];
    }

    public static IReadOnlyList<StoredSpecialRule> BindSpecialRules(IReadOnlyList<SpecialRuleSetup> incoming)
    {
        return
        [
            .. incoming.Select(static rule => new StoredSpecialRule
            {
                Id = rule.Id,
                Name = rule.Name,
                Text = rule.Text,
                EffectKey = rule.EffectKey,
            }),
        ];
    }

    public static IReadOnlyList<StoredForceStatus> BindForceStatuses(IReadOnlyList<ForceStatusSetup> incoming)
    {
        return
        [
            .. incoming.Select(static status => new StoredForceStatus
            {
                Id = status.Id,
                Name = status.Name,
                Effects = status.Effects,
                EnableTrigger = status.EnableTrigger.ToString(),
                ClearTrigger = status.ClearTrigger.ToString(),
            }),
        ];
    }

    public static IReadOnlyList<StoredPrivateObjectiveType> BindPrivateObjectives(
        IReadOnlyList<PrivateObjectiveTypeSetup> incoming)
    {
        return
        [
            .. incoming.Select(static type => new StoredPrivateObjectiveType
            {
                Id = type.Id,
                Name = type.Name,
                Description = type.Description,
                CampaignPoints = type.CampaignPoints,
                AllowedHolderKinds = [.. type.AllowedHolderKinds.Select(static kind => kind.ToString())],
                ScoringKind = type.ScoringKind.ToString(),
                AutomaticKind = type.AutomaticKind.ToString(),
                RequiredCount = type.RequiredCount,
                StructureTypeId = type.StructureTypeId,
                TerritoryIds = type.TerritoryIds,
            }),
        ];
    }

    public static IEnumerable<string> CollectStorageKeys(
        IReadOnlyList<StoredTerrainType>? terrains,
        IReadOnlyList<StoredStructureType>? structures,
        IReadOnlyList<StoredFaction>? factions = null,
        IReadOnlyList<StoredItemObjectiveType>? itemObjectives = null)
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

                foreach (var appearance in faction.SubfactionAppearances)
                {
                    if (IsUserUploadedFileKey(appearance.FlagImageStorageKey))
                    {
                        yield return appearance.FlagImageStorageKey;
                    }
                }
            }
        }

        if (itemObjectives is not null)
        {
            foreach (var item in itemObjectives)
            {
                if (IsUserUploadedFileKey(item.ImageStorageKey))
                {
                    yield return item.ImageStorageKey;
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

        foreach (var key in CollectStorageKeys(
            campaign.TerrainTypes,
            campaign.StructureTypes,
            campaign.Factions,
            campaign.ItemObjectiveTypes))
        {
            yield return key;
        }

        foreach (var mission in campaign.Missions)
        {
            if (IsUserUploadedFileKey(mission.FileStorageKey))
            {
                yield return mission.FileStorageKey;
            }
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

    /// <summary>
    /// Whether the key is a generated user upload rather than a built-in symbol.
    /// </summary>
    public static bool IsUserUploadedFileKey([NotNullWhen(true)] string? storageKey)
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

        return storageKey[..slash] is "maps" or "structures" or "flags" or "missions" or "items";
    }

    public static IReadOnlyList<StoredMission> BindMissions(
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
                    ResultQuestions =
                    [
                        .. mission.ResultQuestions.Select(static question => new StoredMissionResultQuestion
                        {
                            Id = question.Id,
                            Prompt = question.Prompt,
                            Kind = question.Kind.ToString(),
                            BattlePoints = question.BattlePoints,
                            CampaignPoints = question.CampaignPoints,
                        }),
                    ],
                    IsAttackerDefender = mission.IsAttackerDefender,
                    HasArmyPointsAdvantage = mission.HasArmyPointsAdvantage,
                    ArmyPointsAdvantageSide = mission.ArmyPointsAdvantageSide.ToString(),
                    ArmyPointsAdvantageIsPercent = mission.ArmyPointsAdvantageIsPercent,
                    ArmyPointsAdvantageAmount = mission.ArmyPointsAdvantageAmount,
                    HasSupplyPointsAdvantage = mission.HasSupplyPointsAdvantage,
                    SupplyPointsAdvantageSide = mission.SupplyPointsAdvantageSide.ToString(),
                    SupplyPointsAdvantageAmount = mission.SupplyPointsAdvantageAmount,
                };
            }),
        ];
    }

    public static Dictionary<Guid, StoredMission> IndexMissions(IEnumerable<StoredMission>? missions)
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

    internal static Dictionary<string, T> IndexByName<T>(IEnumerable<T>? items, Func<T, string> nameSelector)
    {
        var indexed = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
        if (items is null)
        {
            return indexed;
        }

        foreach (var item in items)
        {
            var name = nameSelector(item).Trim();
            if (name.Length > 0)
            {
                indexed.TryAdd(name, item);
            }
        }

        return indexed;
    }
}
