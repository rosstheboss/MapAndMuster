using System.Text.Json;
using MapAndMuster.Application.Campaigns;
using MapAndMuster.Domain.Campaigns;
using MapAndMuster.Domain.Maps;
using MapAndMuster.Domain.Play;

namespace MapAndMuster.Infrastructure.Campaigns;

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
        GeneralPublicObjectivePoints? rankingObjectivePoints = null,
        IReadOnlyList<StoredSpecialRule>? specialRules = null,
        IReadOnlyList<StoredPrivateObjectiveType>? privateObjectiveTypes = null,
        IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>? factionSpecialRuleIds = null,
        IReadOnlyList<StoredForceStatus>? forceStatuses = null,
        int splitForceSupplyPenaltyPercent = HuntInEstaliaDefaults.SplitForceSupplyPenaltyValue,
        bool splitForceSupplyPenaltyIsPercent = HuntInEstaliaDefaults.SplitForceSupplyPenaltyIsPercent,
        IReadOnlyList<StoredStandardBattleResultQuestion>? standardBattleResultQuestions = null,
        IReadOnlyList<RoundArmyEscalationSetup>? armyEscalations = null,
        IReadOnlyList<StoredMission>? missions = null,
        IReadOnlyDictionary<Guid, IReadOnlyList<SubfactionSpecialRulesDetail>>? subfactionSpecialRules = null,
        IReadOnlyList<StoredCatalogTag>? terrainTags = null,
        IReadOnlyList<StoredCatalogTag>? structureTags = null,
        IReadOnlyList<StoredCatalogTag>? factionTags = null,
        IReadOnlyList<StoredCatalogTag>? missionTags = null,
        IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>? factionTagIds = null,
        IReadOnlyDictionary<Guid, IReadOnlyList<StoredSubfactionTags>>? subfactionTagIds = null,
        IReadOnlyDictionary<Guid, int>? factionMovementSpeeds = null,
        IReadOnlyDictionary<Guid, IReadOnlyList<StoredSubfactionMovementSpeed>>? subfactionMovementSpeeds = null,
        bool? rivalObjectivesEnabled = null,
        int? rivalObjectiveCampaignPoints = null)
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
                SpecialRules = [.. (specialRules ?? []).Select(ToDocument)],
                ForceStatuses = [.. (forceStatuses ?? []).Select(ToDocument)],
                PrivateObjectiveTypes = [.. (privateObjectiveTypes ?? []).Select(ToDocument)],
                FactionSpecialRules = MergeFactionSpecialRules(
                    factionSpecialRuleIds,
                    subfactionSpecialRules,
                    factionMovementSpeeds,
                    subfactionMovementSpeeds),
                PointsPerBattleWon = scoring.PointsPerWin,
                BattleScoring = ToDocument(scoring),
                MostTerritoriesCampaignPoints = ranking.MostTerritories,
                LongestTerritoryChainCampaignPoints = ranking.LongestTerritoryChain,
                MostBattlesWonCampaignPoints = ranking.MostBattlesWon,
                MostStructurePointsCampaignPoints = ranking.MostStructurePoints,
                PointsPerTerritoryCampaignPoints = ranking.PointsPerTerritory,
                AlliedRelicControlCampaignPoints = ranking.AlliedRelicControlPoints,
                MostTerritoriesTerrainTagId = ranking.MostTerritoriesTerrainTagId,
                LongestTerritoryChainTerrainTagId = ranking.LongestTerritoryChainTerrainTagId,
                MostStructurePointsStructureTagId = ranking.MostStructurePointsStructureTagId,
                PointsPerTerritoryTerrainTagId = ranking.PointsPerTerritoryTerrainTagId,
                SplitForceSupplyPenaltyPercent = splitForceSupplyPenaltyPercent,
                SplitForceSupplyPenaltyIsPercent = splitForceSupplyPenaltyIsPercent,
                StandardBattleResultQuestions = [.. (standardBattleResultQuestions ?? []).Select(ToDocument)],
                ArmyEscalations = [.. (armyEscalations ?? []).Select(ToDocument)],
                Missions = [.. (missions ?? []).Select(ToDocument)],
                TerrainTags = [.. (terrainTags ?? []).Select(ToDocument)],
                StructureTags = [.. (structureTags ?? []).Select(ToDocument)],
                FactionTags = [.. (factionTags ?? []).Select(ToDocument)],
                MissionTags = [.. (missionTags ?? []).Select(ToDocument)],
                FactionTagsAssignments =
                [
                    .. (factionTagIds ?? new Dictionary<Guid, IReadOnlyList<Guid>>()).Select(static pair =>
                        new FactionTagsDocument
                        {
                            FactionId = pair.Key,
                            TagIds = [.. pair.Value],
                        }),
                    .. (subfactionTagIds ?? new Dictionary<Guid, IReadOnlyList<StoredSubfactionTags>>())
                        .SelectMany(static pair => pair.Value.Select(item =>
                            new FactionTagsDocument
                            {
                                FactionId = pair.Key,
                                SubfactionName = item.Name,
                                TagIds = [.. item.TagIds],
                            })),
                ],
                RivalObjectivesEnabled = rivalObjectivesEnabled,
                RivalObjectiveCampaignPoints = rivalObjectiveCampaignPoints,
            },
            Options);
    }

    public static string Serialize(StoredCampaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        return Serialize(
            campaign.TerrainTypes,
            campaign.StructureTypes,
            campaign.ItemObjectiveTypes,
            campaign.PublicObjectiveTypes,
            campaign.BattleScoring,
            campaign.RankingObjectivePoints,
            campaign.SpecialRules,
            campaign.PrivateObjectiveTypes,
            campaign.Factions.ToDictionary(static faction => faction.Id, static faction => faction.SpecialRuleIds),
            campaign.ForceStatuses,
            campaign.SplitForceSupplyPenaltyPercent,
            campaign.SplitForceSupplyPenaltyIsPercent,
            campaign.StandardBattleResultQuestions,
            campaign.ArmyEscalations,
            campaign.Missions,
            campaign.Factions.ToDictionary(static faction => faction.Id, static faction => faction.SubfactionSpecialRules),
            campaign.TerrainTags,
            campaign.StructureTags,
            campaign.FactionTags,
            campaign.MissionTags,
            campaign.Factions.ToDictionary(static faction => faction.Id, static faction => faction.TagIds),
            campaign.Factions.ToDictionary(static faction => faction.Id, static faction => faction.SubfactionTags),
            campaign.Factions.ToDictionary(static faction => faction.Id, static faction => faction.ForceMovementSpeed),
            campaign.Factions.ToDictionary(static faction => faction.Id, static faction => faction.SubfactionMovementSpeeds),
            campaign.RivalObjectivesEnabled,
            campaign.RivalObjectiveCampaignPoints);
    }

    public static (bool Enabled, int Points) RivalObjectiveSettings(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return (true, RivalObjectiveRules.DefaultCampaignPoints);
        }

        var document = JsonSerializer.Deserialize<CatalogDocument>(json, Options);
        var points = document?.RivalObjectiveCampaignPoints is { } supplied && supplied >= 0
            ? supplied
            : RivalObjectiveRules.DefaultCampaignPoints;
        return (document?.RivalObjectivesEnabled ?? true, points);
    }

    public static (
        IReadOnlyList<StoredTerrainType> TerrainTypes,
        IReadOnlyList<StoredStructureType> StructureTypes,
        IReadOnlyList<StoredItemObjectiveType> ItemObjectiveTypes,
        IReadOnlyList<StoredPublicObjectiveType> PublicObjectiveTypes,
        BattleScoringSetup BattleScoring,
        GeneralPublicObjectivePoints RankingObjectivePoints,
        IReadOnlyList<StoredSpecialRule> SpecialRules,
        IReadOnlyList<StoredPrivateObjectiveType> PrivateObjectiveTypes,
        IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> FactionSpecialRuleIds,
        IReadOnlyDictionary<Guid, IReadOnlyList<SubfactionSpecialRulesDetail>> SubfactionSpecialRuleIds,
        IReadOnlyList<StoredForceStatus> ForceStatuses,
        int SplitForceSupplyPenaltyPercent,
        bool SplitForceSupplyPenaltyIsPercent,
        IReadOnlyList<StoredStandardBattleResultQuestion> StandardBattleResultQuestions,
        IReadOnlyList<RoundArmyEscalationSetup> ArmyEscalations,
        IReadOnlyList<StoredMission> Missions,
        IReadOnlyList<StoredCatalogTag> TerrainTags,
        IReadOnlyList<StoredCatalogTag> StructureTags,
        IReadOnlyList<StoredCatalogTag> FactionTags,
        IReadOnlyList<StoredCatalogTag> MissionTags,
        IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> FactionTagIds,
        IReadOnlyDictionary<Guid, IReadOnlyList<StoredSubfactionTags>> SubfactionTagIds)
        Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return EmptyCatalog();
        }

        var document = JsonSerializer.Deserialize<CatalogDocument>(json, Options);
        if (document is null)
        {
            return EmptyCatalog();
        }

        var terrains = document.TerrainTypes.Select(FromDocument).ToArray();
        var terrainTags = MigrateWaterTags(
            (document.TerrainTags ?? []).Select(FromDocument).ToArray(),
            terrains,
            document.TerrainTypes);
        var structures = document.StructureTypes.Select(FromDocument).ToArray();
        var catalogMissions = MergeMissions(
            (document.Missions ?? []).Select(FromDocument),
            terrains.SelectMany(static type => type.Missions).Concat(structures.SelectMany(static type => type.Missions)));
        var factionRules = document.FactionSpecialRules ?? [];
        var factionTagsAssignments = document.FactionTagsAssignments ?? [];
        return (
            terrains,
            structures,
            [.. (document.ItemObjectiveTypes ?? []).Select(FromDocument)],
            [.. (document.PublicObjectiveTypes ?? []).Select(FromDocument)],
            BattleScoringFrom(document),
            new GeneralPublicObjectivePoints(
                Math.Max(0, document.MostTerritoriesCampaignPoints),
                Math.Max(0, document.LongestTerritoryChainCampaignPoints),
                Math.Max(0, document.MostBattlesWonCampaignPoints),
                Math.Max(0, document.MostStructurePointsCampaignPoints),
                Math.Max(0, document.PointsPerTerritoryCampaignPoints),
                Math.Max(0, document.AlliedRelicControlCampaignPoints),
                document.MostTerritoriesTerrainTagId,
                document.LongestTerritoryChainTerrainTagId,
                document.MostStructurePointsStructureTagId,
                document.PointsPerTerritoryTerrainTagId),
            [.. (document.SpecialRules ?? []).Select(FromDocument)],
            [.. (document.PrivateObjectiveTypes ?? []).Select(FromDocument)],
            factionRules
                .Where(static item => string.IsNullOrWhiteSpace(item.SubfactionName))
                .GroupBy(static item => item.FactionId)
                .ToDictionary(
                    static group => group.Key,
                    static group => (IReadOnlyList<Guid>)group.SelectMany(static item => item.SpecialRuleIds).Distinct().ToArray()),
            factionRules
                .Where(static item => !string.IsNullOrWhiteSpace(item.SubfactionName))
                .GroupBy(static item => item.FactionId)
                .ToDictionary(
                    static group => group.Key,
                    static group => (IReadOnlyList<SubfactionSpecialRulesDetail>)group
                        .Select(static item => new SubfactionSpecialRulesDetail
                        {
                            Name = item.SubfactionName!,
                            SpecialRuleIds = item.SpecialRuleIds,
                        })
                        .ToArray()),
            [.. NormalizeForceStatuses((document.ForceStatuses ?? []).Select(FromDocument))],
            ReadSplitForcePenaltyValue(document),
            ReadSplitForcePenaltyIsPercent(document),
            ReadStandardBattleResultQuestions(document, catalogMissions),
            ArmyEscalationsFrom(document),
            catalogMissions,
            terrainTags,
            [.. (document.StructureTags ?? []).Select(FromDocument)],
            [.. (document.FactionTags ?? []).Select(FromDocument)],
            [.. (document.MissionTags ?? []).Select(FromDocument)],
            factionTagsAssignments
                .Where(static item => string.IsNullOrWhiteSpace(item.SubfactionName))
                .GroupBy(static item => item.FactionId)
                .ToDictionary(
                    static group => group.Key,
                    static group => (IReadOnlyList<Guid>)group.SelectMany(static item => item.TagIds).Distinct().ToArray()),
            factionTagsAssignments
                .Where(static item => !string.IsNullOrWhiteSpace(item.SubfactionName))
                .GroupBy(static item => item.FactionId)
                .ToDictionary(
                    static group => group.Key,
                    static group => (IReadOnlyList<StoredSubfactionTags>)group
                        .Select(static item => new StoredSubfactionTags
                        {
                            Name = item.SubfactionName!,
                            TagIds = item.TagIds,
                        })
                        .ToArray()));
    }

    private static (
        IReadOnlyList<StoredTerrainType>,
        IReadOnlyList<StoredStructureType>,
        IReadOnlyList<StoredItemObjectiveType>,
        IReadOnlyList<StoredPublicObjectiveType>,
        BattleScoringSetup,
        GeneralPublicObjectivePoints,
        IReadOnlyList<StoredSpecialRule>,
        IReadOnlyList<StoredPrivateObjectiveType>,
        IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>,
        IReadOnlyDictionary<Guid, IReadOnlyList<SubfactionSpecialRulesDetail>>,
        IReadOnlyList<StoredForceStatus>,
        int,
        bool,
        IReadOnlyList<StoredStandardBattleResultQuestion>,
        IReadOnlyList<RoundArmyEscalationSetup>,
        IReadOnlyList<StoredMission>,
        IReadOnlyList<StoredCatalogTag>,
        IReadOnlyList<StoredCatalogTag>,
        IReadOnlyList<StoredCatalogTag>,
        IReadOnlyList<StoredCatalogTag>,
        IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>,
        IReadOnlyDictionary<Guid, IReadOnlyList<StoredSubfactionTags>>) EmptyCatalog()
    {
        return ([], [], [], [], BattleScoringSetup.Straight(0), GeneralPublicObjectivePoints.None, [], [], new Dictionary<Guid, IReadOnlyList<Guid>>(), new Dictionary<Guid, IReadOnlyList<SubfactionSpecialRulesDetail>>(), [], HuntInEstaliaDefaults.SplitForceSupplyPenaltyValue, HuntInEstaliaDefaults.SplitForceSupplyPenaltyIsPercent, [], [], [], [], [], [], [], new Dictionary<Guid, IReadOnlyList<Guid>>(), new Dictionary<Guid, IReadOnlyList<StoredSubfactionTags>>());
    }

    public static (
        IReadOnlyDictionary<Guid, int> FactionSpeeds,
        IReadOnlyDictionary<Guid, IReadOnlyList<StoredSubfactionMovementSpeed>> SubfactionSpeeds)
        DeserializeMovementSpeeds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return (new Dictionary<Guid, int>(), new Dictionary<Guid, IReadOnlyList<StoredSubfactionMovementSpeed>>());
        }

        var document = JsonSerializer.Deserialize<CatalogDocument>(json, Options);
        var rows = document?.FactionSpecialRules ?? [];
        var factionSpeeds = rows
            .Where(static item => string.IsNullOrWhiteSpace(item.SubfactionName) && item.ForceMovementSpeed is not null)
            .GroupBy(static item => item.FactionId)
            .ToDictionary(
                static group => group.Key,
                static group => Math.Clamp(group.Last().ForceMovementSpeed ?? ForceMovementSpeeds.Default, ForceMovementSpeeds.Min, ForceMovementSpeeds.Max));
        var subfactionSpeeds = rows
            .Where(static item => !string.IsNullOrWhiteSpace(item.SubfactionName) && item.ForceMovementSpeed is not null)
            .GroupBy(static item => item.FactionId)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<StoredSubfactionMovementSpeed>)group
                    .Select(static item => new StoredSubfactionMovementSpeed
                    {
                        Name = item.SubfactionName!,
                        Speed = Math.Clamp(item.ForceMovementSpeed ?? ForceMovementSpeeds.Default, ForceMovementSpeeds.Min, ForceMovementSpeeds.Max),
                    })
                    .ToArray());
        return (factionSpeeds, subfactionSpeeds);
    }

    private static List<FactionSpecialRulesDocument> MergeFactionSpecialRules(
        IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>? factionSpecialRuleIds,
        IReadOnlyDictionary<Guid, IReadOnlyList<SubfactionSpecialRulesDetail>>? subfactionSpecialRules,
        IReadOnlyDictionary<Guid, int>? factionMovementSpeeds,
        IReadOnlyDictionary<Guid, IReadOnlyList<StoredSubfactionMovementSpeed>>? subfactionMovementSpeeds)
    {
        var byKey = new Dictionary<(Guid FactionId, string Subfaction), FactionSpecialRulesDocument>();
        void Upsert(Guid factionId, string? subfaction, IReadOnlyList<Guid>? ruleIds, int? speed)
        {
            var key = (factionId, subfaction ?? "");
            if (!byKey.TryGetValue(key, out var row))
            {
                row = new FactionSpecialRulesDocument
                {
                    FactionId = factionId,
                    SubfactionName = string.IsNullOrWhiteSpace(subfaction) ? null : subfaction,
                    SpecialRuleIds = [],
                };
                byKey[key] = row;
            }

            if (ruleIds is { Count: > 0 })
            {
                row.SpecialRuleIds = [.. ruleIds];
            }

            if (speed is not null)
            {
                row.ForceMovementSpeed = speed;
            }
        }

        foreach (var pair in factionSpecialRuleIds ?? new Dictionary<Guid, IReadOnlyList<Guid>>())
        {
            Upsert(pair.Key, null, pair.Value, null);
        }

        foreach (var pair in subfactionSpecialRules ?? new Dictionary<Guid, IReadOnlyList<SubfactionSpecialRulesDetail>>())
        {
            foreach (var item in pair.Value)
            {
                Upsert(pair.Key, item.Name, item.SpecialRuleIds, null);
            }
        }

        foreach (var pair in factionMovementSpeeds ?? new Dictionary<Guid, int>())
        {
            Upsert(pair.Key, null, null, pair.Value);
        }

        foreach (var pair in subfactionMovementSpeeds ?? new Dictionary<Guid, IReadOnlyList<StoredSubfactionMovementSpeed>>())
        {
            foreach (var item in pair.Value)
            {
                Upsert(pair.Key, item.Name, null, item.Speed);
            }
        }

        return [.. byKey.Values];
    }

    private static List<StoredCatalogTag> MigrateWaterTags(
        IReadOnlyList<StoredCatalogTag> tags,
        StoredTerrainType[] terrains,
        List<TerrainDocument> documents)
    {
        var tagList = tags.ToList();
        var water = tagList.FirstOrDefault(static tag => CatalogTags.IsWater(tag.Name));
        for (var index = 0; index < terrains.Length; index++)
        {
            var isWater = index < documents.Count && documents[index].IsWaterFeature;
            if (!isWater)
            {
                continue;
            }

            if (water is null)
            {
                water = new StoredCatalogTag
                {
                    Id = Guid.NewGuid(),
                    Name = CatalogTags.WaterName,
                };
                tagList.Add(water);
            }

            if (terrains[index].TagIds.Contains(water.Id))
            {
                continue;
            }

            terrains[index] = new StoredTerrainType
            {
                Id = terrains[index].Id,
                Name = terrains[index].Name,
                Color = terrains[index].Color,
                Missions = terrains[index].Missions,
                CampaignPoints = terrains[index].CampaignPoints,
                SupplyPoints = terrains[index].SupplyPoints,
                TagIds = [.. terrains[index].TagIds, water.Id],
            };
        }

        return tagList;
    }

    private static int ReadSplitForcePenaltyValue(CatalogDocument document)
    {
        var value = document.SplitForceSupplyPenaltyPercent;
        if (value is null || value < 0 || value > 100)
        {
            return document.SplitForceSupplyPenaltyIsPercent is null
                ? HuntInEstaliaDefaults.LegacySplitForceSupplyPenaltyPercent
                : HuntInEstaliaDefaults.SplitForceSupplyPenaltyValue;
        }

        return value.Value;
    }

    private static bool ReadSplitForcePenaltyIsPercent(CatalogDocument document)
    {
        return document.SplitForceSupplyPenaltyIsPercent
            ?? true;
    }

    private static IReadOnlyList<StoredMission> MergeMissions(
        IEnumerable<StoredMission> catalog,
        IEnumerable<StoredMission> nested)
    {
        var merged = new Dictionary<Guid, StoredMission>();
        foreach (var mission in catalog.Concat(nested))
        {
            merged.TryAdd(mission.Id, mission);
        }

        return [.. merged.Values];
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
            SupplyPoints = type.SupplyPoints,
            TagIds = [.. type.TagIds],
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
            SupplyPoints = type.SupplyPoints,
            PillageSupplyPoints = type.PillageSupplyPoints,
            DestroySupplyPoints = type.DestroySupplyPoints,
            TagIds = [.. type.TagIds],
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
            ResultQuestions = [.. mission.ResultQuestions.Select(ToDocument)],
            IsAttackerDefender = mission.IsAttackerDefender,
            HasArmyPointsAdvantage = mission.HasArmyPointsAdvantage,
            ArmyPointsAdvantageSide = mission.ArmyPointsAdvantageSide,
            ArmyPointsAdvantageIsPercent = mission.ArmyPointsAdvantageIsPercent,
            ArmyPointsAdvantageAmount = mission.ArmyPointsAdvantageAmount,
            HasSupplyPointsAdvantage = mission.HasSupplyPointsAdvantage,
            SupplyPointsAdvantageSide = mission.SupplyPointsAdvantageSide,
            SupplyPointsAdvantageAmount = mission.SupplyPointsAdvantageAmount,
            StatusChanges =
            [
                .. mission.StatusChanges.Select(static change => new MissionStatusChangeDocument
                {
                    Id = change.Id,
                    Outcome = change.Outcome,
                    WhenCurrentStatus = change.WhenCurrentStatus,
                    SetStatus = change.SetStatus,
                    LeaveUnchanged = change.LeaveUnchanged,
                }),
            ],
            TagIds = [.. mission.TagIds],
        };
    }

    private static CatalogTagDocument ToDocument(StoredCatalogTag tag)
    {
        return new CatalogTagDocument
        {
            Id = tag.Id,
            Name = tag.Name,
        };
    }

    private static StoredCatalogTag FromDocument(CatalogTagDocument tag)
    {
        return new StoredCatalogTag
        {
            Id = tag.Id == Guid.Empty ? Guid.NewGuid() : tag.Id,
            Name = tag.Name?.Trim() ?? string.Empty,
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
            SupplyPoints = Math.Max(0, type.SupplyPoints),
            TagIds = type.TagIds ?? [],
        };
    }

    private static StoredStructureType FromDocument(StructureDocument type)
    {
        var (IsBuildable, IsPillageable, IsDestructible) = StructureCatalog.DefaultFlags(type.Name, type.BuiltinSymbol);
        return new StoredStructureType
        {
            Id = type.Id,
            Name = type.Name,
            BuiltinSymbol = type.BuiltinSymbol,
            ImageStorageKey = type.ImageStorageKey,
            PillagedImageStorageKey = type.PillagedImageStorageKey,
            IsBuildable = type.IsBuildable ?? IsBuildable,
            IsPillageable = type.IsPillageable ?? IsPillageable,
            IsDestructible = type.IsDestructible ?? IsDestructible,
            Missions = [.. type.Missions.Select(FromDocument)],
            CampaignPoints = type.CampaignPoints,
            SupplyPoints = Math.Max(0, type.SupplyPoints),
            PillageSupplyPoints = Math.Max(0, type.PillageSupplyPoints),
            DestroySupplyPoints = Math.Max(0, type.DestroySupplyPoints),
            TagIds = type.TagIds ?? [],
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
            ResultQuestions = [.. (mission.ResultQuestions ?? []).Select(FromDocument)],
            IsAttackerDefender = mission.IsAttackerDefender,
            HasArmyPointsAdvantage = mission.HasArmyPointsAdvantage,
            ArmyPointsAdvantageSide = string.IsNullOrWhiteSpace(mission.ArmyPointsAdvantageSide)
                ? "Defender"
                : mission.ArmyPointsAdvantageSide,
            ArmyPointsAdvantageIsPercent = mission.ArmyPointsAdvantageIsPercent,
            ArmyPointsAdvantageAmount = mission.ArmyPointsAdvantageAmount,
            HasSupplyPointsAdvantage = mission.HasSupplyPointsAdvantage,
            SupplyPointsAdvantageSide = string.IsNullOrWhiteSpace(mission.SupplyPointsAdvantageSide)
                ? "Defender"
                : mission.SupplyPointsAdvantageSide,
            SupplyPointsAdvantageAmount = mission.SupplyPointsAdvantageAmount,
            StatusChanges =
            [
                .. (mission.StatusChanges ?? []).Select(static change => new StoredMissionStatusChange
                {
                    Id = change.Id == Guid.Empty ? Guid.NewGuid() : change.Id,
                    Outcome = string.IsNullOrWhiteSpace(change.Outcome) ? "Win" : change.Outcome,
                    WhenCurrentStatus = change.WhenCurrentStatus,
                    SetStatus = change.SetStatus,
                    LeaveUnchanged = change.LeaveUnchanged,
                }),
            ],
            TagIds = mission.TagIds ?? [],
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
            FlavorText = type.FlavorText,
            Choices = [.. type.Choices.Select(ToDocument)],
            SpecialRuleIds = [.. type.SpecialRuleIds],
            Effects = [.. type.Effects.Select(ToDocument)],
        };
    }

    private static ItemEffectDocument ToDocument(StoredItemObjectiveEffect effect)
    {
        return new ItemEffectDocument
        {
            Id = effect.Id,
            Kind = effect.Kind,
            Amount = effect.Amount,
            AmountIsPercent = effect.AmountIsPercent,
            StatusTypeIds = [.. effect.StatusTypeIds],
            ImmuneToAllStatuses = effect.ImmuneToAllStatuses,
            SuspendCurrentAllyGroup = effect.SuspendCurrentAllyGroup,
            ForcedAllyGroupName = effect.ForcedAllyGroupName,
            AlliedFactions =
            [
                .. effect.AlliedFactions.Select(static target => new ItemAllianceTargetDocument
                {
                    FactionId = target.FactionId,
                    Subfaction = target.Subfaction,
                }),
            ],
            CustomText = effect.CustomText,
            SuccessStatusTypeId = effect.SuccessStatusTypeId,
            FailureStatusTypeId = effect.FailureStatusTypeId,
        };
    }

    private static ItemChoiceDocument ToDocument(StoredItemObjectiveChoice choice)
    {
        return new ItemChoiceDocument
        {
            Id = choice.Id,
            Name = choice.Name,
            Results = [.. choice.Results.Select(ToDocument)],
        };
    }

    private static ItemChoiceResultDocument ToDocument(StoredItemObjectiveChoiceResult result)
    {
        return new ItemChoiceResultDocument
        {
            Id = result.Id,
            FlavorText = result.FlavorText,
            NewStateKey = result.NewStateKey,
            DestroyItem = result.DestroyItem,
            ReplacementItemTypeId = result.ReplacementItemTypeId,
            GrantedPrivateObjectiveTypeId = result.GrantedPrivateObjectiveTypeId,
            SetForceStatusName = result.SetForceStatusName,
        };
    }

    private static SpecialRuleDocument ToDocument(StoredSpecialRule rule)
    {
        return new SpecialRuleDocument
        {
            Id = rule.Id,
            Name = rule.Name,
            Text = rule.Text,
            EffectKey = rule.EffectKey,
        };
    }

    private static ForceStatusDocument ToDocument(StoredForceStatus status)
    {
        return new ForceStatusDocument
        {
            Id = status.Id,
            Name = status.Name,
            Effects = status.Effects,
            EnableTrigger = status.EnableTrigger,
            ClearTrigger = status.ClearTrigger,
            EnableConditions = ToConditionDocuments(status.EnableConditions, status.EnableTrigger, status.EnableOccurrences),
            ClearConditions = ToConditionDocuments(status.ClearConditions, status.ClearTrigger, status.ClearOccurrences),
            Priority = status.Priority,
            CancelsStatusIds = [.. status.CancelsStatusIds],
            ImmuneFactionIds = [.. status.ImmuneFactionIds],
            ImmuneSubfactions =
            [
                .. status.ImmuneSubfactions.Select(static item => new ForceStatusImmuneSubfactionDocument
                {
                    FactionId = item.FactionId,
                    Subfaction = item.Subfaction,
                }),
            ],
            TokenImageStorageKey = status.TokenImageStorageKey,
            EnableOccurrences = status.EnableOccurrences,
            ClearOccurrences = status.ClearOccurrences,
        };
    }

    private static List<ForceStatusConditionDocument> ToConditionDocuments(
        IReadOnlyList<StoredForceStatusCondition> listed,
        string trigger,
        int occurrences)
    {
        if (listed.Count > 0)
        {
            return
            [
                .. listed.Select(static condition => new ForceStatusConditionDocument
                {
                    Id = condition.Id,
                    Trigger = condition.Trigger,
                    Occurrences = condition.Occurrences,
                    LocationKind = condition.LocationKind,
                    LocationTypeId = condition.LocationTypeId,
                    LocationTagId = condition.LocationTagId,
                    RequiredStatusId = condition.RequiredStatusId,
                    RequiredQuestionId = condition.RequiredQuestionId,
                }),
            ];
        }

        if (string.IsNullOrWhiteSpace(trigger))
        {
            return [];
        }

        return
        [
            new ForceStatusConditionDocument
            {
                Trigger = trigger,
                Occurrences = occurrences,
                LocationKind = nameof(ConditionLocationKind.Any),
            },
        ];
    }

    private static IReadOnlyList<StoredForceStatusCondition> FromConditionDocuments(
        List<ForceStatusConditionDocument>? listed,
        string? trigger,
        int? occurrences)
    {
        if (listed is { Count: > 0 })
        {
            return
            [
                .. listed.Select(static condition => new StoredForceStatusCondition
                {
                    Id = condition.Id,
                    Trigger = condition.Trigger ?? string.Empty,
                    Occurrences = ForceStatusOccurrences.Normalize(condition.Occurrences),
                    LocationKind = string.IsNullOrWhiteSpace(condition.LocationKind)
                        ? nameof(ConditionLocationKind.Any)
                        : condition.LocationKind,
                    LocationTypeId = condition.LocationTypeId,
                    LocationTagId = condition.LocationTagId,
                    RequiredStatusId = condition.RequiredStatusId,
                    RequiredQuestionId = condition.RequiredQuestionId,
                }),
            ];
        }

        if (string.IsNullOrWhiteSpace(trigger))
        {
            return [];
        }

        return
        [
            new StoredForceStatusCondition
            {
                Trigger = trigger,
                Occurrences = ForceStatusOccurrences.Normalize(occurrences),
            },
        ];
    }

    private static PrivateObjectiveDocument ToDocument(StoredPrivateObjectiveType type)
    {
        return new PrivateObjectiveDocument
        {
            Id = type.Id,
            Name = type.Name,
            Description = type.Description,
            CampaignPoints = type.CampaignPoints,
            AllowedHolderKinds = [.. type.AllowedHolderKinds],
            ScoringKind = type.ScoringKind,
            AutomaticKind = type.AutomaticKind,
            RequiredCount = type.RequiredCount,
            StructureTypeId = type.StructureTypeId,
            TerritoryIds = [.. type.TerritoryIds],
            MatchesAnyStructureType = type.MatchesAnyStructureType,
            ItemObjectiveTypeId = type.ItemObjectiveTypeId,
            MatchesAnyItemObjective = type.MatchesAnyItemObjective,
            TargetKind = type.TargetKind,
            TargetSelection = type.TargetSelection,
            TargetId = type.TargetId,
            ForceStatusTypeIds = [.. type.ForceStatusTypeIds],
            StatusMatchKind = type.StatusMatchKind,
            PrerequisiteForceStatusTypeId = type.PrerequisiteForceStatusTypeId,
            PrerequisiteWasLost = type.PrerequisiteWasLost,
            StructureTagId = type.StructureTagId,
            TerrainTagId = type.TerrainTagId,
            ExcludedFactionIds = [.. type.ExcludedFactionIds],
            ExcludedAllyGroupIds = [.. type.ExcludedAllyGroupIds],
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
            FlavorText = type.FlavorText,
            Choices = [.. (type.Choices ?? []).Select(FromDocument)],
            SpecialRuleIds = type.SpecialRuleIds ?? [],
            Effects = [.. (type.Effects ?? []).Select(FromDocument)],
        };
    }

    private static StoredItemObjectiveEffect FromDocument(ItemEffectDocument effect)
    {
        return new StoredItemObjectiveEffect
        {
            Id = effect.Id == Guid.Empty ? Guid.NewGuid() : effect.Id,
            Kind = string.IsNullOrWhiteSpace(effect.Kind) ? nameof(ItemObjectiveEffectKind.Custom) : effect.Kind,
            Amount = effect.Amount,
            AmountIsPercent = effect.AmountIsPercent,
            StatusTypeIds = effect.StatusTypeIds ?? [],
            ImmuneToAllStatuses = effect.ImmuneToAllStatuses,
            SuspendCurrentAllyGroup = effect.SuspendCurrentAllyGroup,
            ForcedAllyGroupName = effect.ForcedAllyGroupName,
            AlliedFactions =
            [
                .. (effect.AlliedFactions ?? []).Select(static target => new StoredItemObjectiveAllianceTarget
                {
                    FactionId = target.FactionId,
                    Subfaction = target.Subfaction,
                }),
            ],
            CustomText = effect.CustomText,
            SuccessStatusTypeId = effect.SuccessStatusTypeId,
            FailureStatusTypeId = effect.FailureStatusTypeId,
        };
    }

    private static StoredItemObjectiveChoice FromDocument(ItemChoiceDocument choice)
    {
        return new StoredItemObjectiveChoice
        {
            Id = choice.Id,
            Name = choice.Name,
            Results = [.. choice.Results.Select(FromDocument)],
        };
    }

    private static StoredItemObjectiveChoiceResult FromDocument(ItemChoiceResultDocument result)
    {
        return new StoredItemObjectiveChoiceResult
        {
            Id = result.Id,
            FlavorText = result.FlavorText,
            NewStateKey = result.NewStateKey,
            DestroyItem = result.DestroyItem,
            ReplacementItemTypeId = result.ReplacementItemTypeId,
            GrantedPrivateObjectiveTypeId = result.GrantedPrivateObjectiveTypeId,
            SetForceStatusName = result.SetForceStatusName,
        };
    }

    private static StoredSpecialRule FromDocument(SpecialRuleDocument rule)
    {
        return new StoredSpecialRule
        {
            Id = rule.Id,
            Name = rule.Name,
            Text = rule.Text ?? string.Empty,
            EffectKey = rule.EffectKey,
        };
    }

    private static StoredForceStatus FromDocument(ForceStatusDocument status)
    {
        return new StoredForceStatus
        {
            Id = status.Id,
            Name = status.Name,
            Effects = status.Effects ?? string.Empty,
            EnableTrigger = status.EnableTrigger ?? string.Empty,
            ClearTrigger = status.ClearTrigger ?? string.Empty,
            EnableConditions = FromConditionDocuments(status.EnableConditions, status.EnableTrigger, status.EnableOccurrences),
            ClearConditions = FromConditionDocuments(status.ClearConditions, status.ClearTrigger, status.ClearOccurrences),
            Priority = status.Priority ?? -1,
            CancelsStatusIds = status.CancelsStatusIds ?? [],
            ImmuneFactionIds = status.ImmuneFactionIds ?? [],
            ImmuneSubfactions = FromImmuneSubfactionDocuments(status.ImmuneSubfactions),
            TokenImageStorageKey = status.TokenImageStorageKey,
            EnableOccurrences = ForceStatusOccurrences.Normalize(status.EnableOccurrences),
            ClearOccurrences = ForceStatusOccurrences.Normalize(status.ClearOccurrences),
        };
    }

    private static IReadOnlyList<StoredForceStatusImmuneSubfaction> FromImmuneSubfactionDocuments(
        List<ForceStatusImmuneSubfactionDocument>? listed)
    {
        if (listed is null || listed.Count == 0)
        {
            return [];
        }

        return
        [
            .. listed
                .Where(static item => item.FactionId != Guid.Empty && !string.IsNullOrWhiteSpace(item.Subfaction))
                .Select(static item => new StoredForceStatusImmuneSubfaction
                {
                    FactionId = item.FactionId,
                    Subfaction = item.Subfaction.Trim(),
                }),
        ];
    }

    private static StoredForceStatus[] NormalizeForceStatuses(IEnumerable<StoredForceStatus> statuses)
    {
        var list = statuses.ToArray();
        if (list.Length == 0)
        {
            return list;
        }

        var knownIds = list.Select(static status => status.Id).ToHashSet();
        var withCancels = list.Select(status => new StoredForceStatus
        {
            Id = status.Id,
            Name = status.Name,
            Effects = status.Effects,
            EnableTrigger = status.EnableTrigger,
            ClearTrigger = status.ClearTrigger,
            EnableConditions = status.EnableConditions,
            ClearConditions = status.ClearConditions,
            Priority = status.Priority,
            CancelsStatusIds =
            [
                .. status.CancelsStatusIds
                    .Where(id => id != status.Id && knownIds.Contains(id))
                    .Distinct(),
            ],
            ImmuneFactionIds = status.ImmuneFactionIds,
            ImmuneSubfactions = status.ImmuneSubfactions,
            TokenImageStorageKey = status.TokenImageStorageKey,
            EnableOccurrences = ForceStatusOccurrences.Normalize(status.EnableOccurrences),
            ClearOccurrences = ForceStatusOccurrences.Normalize(status.ClearOccurrences),
        }).ToArray();
        var priorities = withCancels.Select(static status => status.Priority).ToArray();
        var unique = priorities.Distinct().Count() == priorities.Length
            && priorities.All(ForceStatusPriority.IsValid);
        if (unique)
        {
            return withCancels;
        }

        return
        [
            .. withCancels.Select((status, index) => new StoredForceStatus
            {
                Id = status.Id,
                Name = status.Name,
                Effects = status.Effects,
                EnableTrigger = status.EnableTrigger,
                ClearTrigger = status.ClearTrigger,
                EnableConditions = status.EnableConditions,
                ClearConditions = status.ClearConditions,
                Priority = index,
                CancelsStatusIds = status.CancelsStatusIds,
                ImmuneFactionIds = status.ImmuneFactionIds,
                ImmuneSubfactions = status.ImmuneSubfactions,
                TokenImageStorageKey = status.TokenImageStorageKey,
                EnableOccurrences = status.EnableOccurrences,
                ClearOccurrences = status.ClearOccurrences,
            }),
        ];
    }

    private static StoredPrivateObjectiveType FromDocument(PrivateObjectiveDocument type)
    {
        return new StoredPrivateObjectiveType
        {
            Id = type.Id,
            Name = type.Name,
            Description = type.Description,
            CampaignPoints = type.CampaignPoints,
            AllowedHolderKinds = type.AllowedHolderKinds is { Count: > 0 }
                ? type.AllowedHolderKinds
                : ["Player", "Faction", "AllyGroup"],
            ScoringKind = string.IsNullOrWhiteSpace(type.ScoringKind) ? "Manual" : type.ScoringKind,
            AutomaticKind = string.IsNullOrWhiteSpace(type.AutomaticKind) ? "None" : type.AutomaticKind,
            RequiredCount = type.RequiredCount < 1 ? 1 : type.RequiredCount,
            StructureTypeId = type.StructureTypeId,
            TerritoryIds = type.TerritoryIds ?? [],
            MatchesAnyStructureType = type.MatchesAnyStructureType,
            ItemObjectiveTypeId = type.ItemObjectiveTypeId,
            MatchesAnyItemObjective = type.MatchesAnyItemObjective,
            TargetKind = string.IsNullOrWhiteSpace(type.TargetKind) ? "None" : type.TargetKind,
            TargetSelection = string.IsNullOrWhiteSpace(type.TargetSelection) ? "Specific" : type.TargetSelection,
            TargetId = type.TargetId,
            ForceStatusTypeIds = type.ForceStatusTypeIds ?? [],
            StatusMatchKind = string.IsNullOrWhiteSpace(type.StatusMatchKind) ? "None" : type.StatusMatchKind,
            PrerequisiteForceStatusTypeId = type.PrerequisiteForceStatusTypeId,
            PrerequisiteWasLost = type.PrerequisiteWasLost,
            StructureTagId = type.StructureTagId,
            TerrainTagId = type.TerrainTagId,
            ExcludedFactionIds = type.ExcludedFactionIds ?? [],
            ExcludedAllyGroupIds = type.ExcludedAllyGroupIds ?? [],
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

    private static ArmyEscalationDocument ToDocument(RoundArmyEscalationSetup row)
    {
        return new ArmyEscalationDocument
        {
            RoundNumber = row.RoundNumber,
            MaxArmyPoints = row.MaxArmyPoints,
            FreeSupplyPoints = row.FreeSupplyPoints,
            FreeCharacterCount = row.FreeCharacterCount,
        };
    }

    private static MissionQuestionDocument ToDocument(StoredMissionResultQuestion question)
    {
        return new MissionQuestionDocument
        {
            Id = question.Id,
            Prompt = question.Prompt,
            Kind = question.Kind,
            BattlePoints = question.BattlePoints,
            CampaignPoints = question.CampaignPoints,
            StandardQuestionId = question.StandardQuestionId,
        };
    }

    private static StandardQuestionDocument ToDocument(StoredStandardBattleResultQuestion question)
    {
        return new StandardQuestionDocument
        {
            Id = question.Id,
            Prompt = question.Prompt,
            Kind = question.Kind,
            BattlePoints = question.BattlePoints,
            CampaignPoints = question.CampaignPoints,
        };
    }

    private static StoredMissionResultQuestion FromDocument(MissionQuestionDocument question)
    {
        return new StoredMissionResultQuestion
        {
            Id = question.Id,
            Prompt = question.Prompt,
            Kind = string.IsNullOrWhiteSpace(question.Kind)
                ? nameof(MissionResultQuestionKind.Boolean)
                : question.Kind,
            BattlePoints = Math.Max(0, question.BattlePoints),
            CampaignPoints = Math.Max(0, question.CampaignPoints),
            StandardQuestionId = question.StandardQuestionId is { } catalogId && catalogId != Guid.Empty
                ? catalogId
                : null,
        };
    }

    private static StoredStandardBattleResultQuestion FromDocument(StandardQuestionDocument question)
    {
        return new StoredStandardBattleResultQuestion
        {
            Id = question.Id == Guid.Empty ? Guid.NewGuid() : question.Id,
            Prompt = question.Prompt,
            Kind = string.IsNullOrWhiteSpace(question.Kind)
                ? nameof(MissionResultQuestionKind.Boolean)
                : question.Kind,
            BattlePoints = Math.Max(0, question.BattlePoints),
            CampaignPoints = Math.Max(0, question.CampaignPoints),
        };
    }

    private static readonly Guid LegacyGeneralKillQuestionId = Guid.Parse("6b1f3c8e-9d24-4a11-8f70-a1b2c3d4e5f6");
    private static readonly Guid LegacySupplyLineQuestionId = Guid.Parse("7c2e4d9f-0e35-4b22-9a81-b2c3d4e5f607");

    private static List<StoredStandardBattleResultQuestion> ReadStandardBattleResultQuestions(
        CatalogDocument document,
        IReadOnlyList<StoredMission> missions)
    {
        if (document.StandardBattleResultQuestions is not null)
        {
            return [.. document.StandardBattleResultQuestions.Select(FromDocument)];
        }

        var catalog = new List<StoredStandardBattleResultQuestion>();
        if (document.AlwaysAskGeneralKill != false)
        {
            catalog.Add(new StoredStandardBattleResultQuestion
            {
                Id = LegacyGeneralKillQuestionId,
                Prompt = "Killed the enemy general",
                Kind = nameof(MissionResultQuestionKind.Boolean),
                BattlePoints = 0,
                CampaignPoints = Math.Max(0, document.GeneralKillCampaignPoints ?? 1),
            });
        }

        if (document.AlwaysAskSupplyLineDestroyed != false)
        {
            catalog.Add(new StoredStandardBattleResultQuestion
            {
                Id = LegacySupplyLineQuestionId,
                Prompt = "Destroyed the enemy supply line",
                Kind = nameof(MissionResultQuestionKind.Boolean),
                BattlePoints = 0,
                CampaignPoints = Math.Max(0, document.SupplyLineDestroyedCampaignPoints ?? 1),
            });
        }

        if (catalog.Count == 0)
        {
            return [];
        }

        foreach (var mission in missions)
        {
            var questions = mission.ResultQuestions.ToList();
            foreach (var standard in catalog)
            {
                if (questions.Any(question =>
                    question.StandardQuestionId == standard.Id
                    || string.Equals(question.Prompt, standard.Prompt, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                questions.Add(new StoredMissionResultQuestion
                {
                    Id = DeterministicQuestionId(mission.Id, standard.Id),
                    Prompt = standard.Prompt,
                    Kind = standard.Kind,
                    BattlePoints = standard.BattlePoints,
                    CampaignPoints = standard.CampaignPoints,
                    StandardQuestionId = standard.Id,
                });
            }

            mission.ResultQuestions = questions;
        }

        return catalog;
    }

    private static Guid DeterministicQuestionId(Guid missionId, Guid catalogId)
    {
        var left = missionId.ToByteArray();
        var right = catalogId.ToByteArray();
        for (var index = 0; index < left.Length; index++)
        {
            left[index] ^= right[index];
        }

        return new Guid(left);
    }

    private static IReadOnlyList<RoundArmyEscalationSetup> ArmyEscalationsFrom(CatalogDocument document)
    {
        var rows = document.ArmyEscalations ?? [];
        if (rows.Count == 0)
        {
            return [];
        }

        return
        [
            .. rows
                .Where(static row => row.RoundNumber > 0)
                .OrderBy(static row => row.RoundNumber)
                .Select(static row => new RoundArmyEscalationSetup(
                    row.RoundNumber,
                    Math.Max(0, row.MaxArmyPoints),
                    Math.Max(0, row.FreeSupplyPoints),
                    Math.Max(0, row.FreeCharacterCount))),
        ];
    }

    private sealed class CatalogDocument
    {
        public List<TerrainDocument> TerrainTypes { get; set; } = [];

        public List<StructureDocument> StructureTypes { get; set; } = [];

        public List<ItemObjectiveDocument> ItemObjectiveTypes { get; set; } = [];

        public List<PublicObjectiveDocument> PublicObjectiveTypes { get; set; } = [];

        public List<SpecialRuleDocument>? SpecialRules { get; set; }

        public List<ForceStatusDocument>? ForceStatuses { get; set; }

        public List<PrivateObjectiveDocument>? PrivateObjectiveTypes { get; set; }

        public List<FactionSpecialRulesDocument>? FactionSpecialRules { get; set; }

        public int PointsPerBattleWon { get; set; }

        public BattleScoringDocument? BattleScoring { get; set; }

        public int MostTerritoriesCampaignPoints { get; set; }

        public int LongestTerritoryChainCampaignPoints { get; set; }

        public int MostBattlesWonCampaignPoints { get; set; }

        public int MostStructurePointsCampaignPoints { get; set; }

        public int PointsPerTerritoryCampaignPoints { get; set; }

        public int AlliedRelicControlCampaignPoints { get; set; }

        public int? SplitForceSupplyPenaltyPercent { get; set; }

        public bool? SplitForceSupplyPenaltyIsPercent { get; set; }

        public bool? AlwaysAskGeneralKill { get; set; }

        public bool? AlwaysAskSupplyLineDestroyed { get; set; }

        public int? GeneralKillCampaignPoints { get; set; }

        public int? SupplyLineDestroyedCampaignPoints { get; set; }

        public List<StandardQuestionDocument>? StandardBattleResultQuestions { get; set; }

        public List<ArmyEscalationDocument>? ArmyEscalations { get; set; }

        public List<MissionDocument>? Missions { get; set; }

        public Guid? MostTerritoriesTerrainTagId { get; set; }

        public Guid? LongestTerritoryChainTerrainTagId { get; set; }

        public Guid? MostStructurePointsStructureTagId { get; set; }

        public Guid? PointsPerTerritoryTerrainTagId { get; set; }

        public List<CatalogTagDocument>? TerrainTags { get; set; }

        public List<CatalogTagDocument>? StructureTags { get; set; }

        public List<CatalogTagDocument>? FactionTags { get; set; }

        public List<CatalogTagDocument>? MissionTags { get; set; }

        public List<FactionTagsDocument>? FactionTagsAssignments { get; set; }

        public bool? RivalObjectivesEnabled { get; set; }

        public int? RivalObjectiveCampaignPoints { get; set; }
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

        public bool IsWaterFeature { get; set; }

        public int SupplyPoints { get; set; } = HuntInEstaliaDefaults.SupplyPoints;

        public List<Guid>? TagIds { get; set; }
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

        public int SupplyPoints { get; set; } = HuntInEstaliaDefaults.SupplyPoints;

        public int PillageSupplyPoints { get; set; } = HuntInEstaliaDefaults.SupplyPoints;

        public int DestroySupplyPoints { get; set; } = HuntInEstaliaDefaults.SupplyPoints;

        public List<Guid>? TagIds { get; set; }
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

        public string? FlavorText { get; set; }

        public List<ItemChoiceDocument>? Choices { get; set; }

        public List<Guid>? SpecialRuleIds { get; set; }

        public List<ItemEffectDocument>? Effects { get; set; }
    }

    private sealed class ItemEffectDocument
    {
        public Guid Id { get; set; }

        public string Kind { get; set; } = string.Empty;

        public int Amount { get; set; }

        public bool AmountIsPercent { get; set; }

        public List<Guid>? StatusTypeIds { get; set; }

        public bool ImmuneToAllStatuses { get; set; }

        public bool SuspendCurrentAllyGroup { get; set; }

        public string? ForcedAllyGroupName { get; set; }

        public List<ItemAllianceTargetDocument>? AlliedFactions { get; set; }

        public string? CustomText { get; set; }

        public Guid? SuccessStatusTypeId { get; set; }

        public Guid? FailureStatusTypeId { get; set; }
    }

    private sealed class ItemAllianceTargetDocument
    {
        public Guid FactionId { get; set; }

        public string? Subfaction { get; set; }
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

        public List<MissionQuestionDocument>? ResultQuestions { get; set; }

        public bool IsAttackerDefender { get; set; }

        public bool HasArmyPointsAdvantage { get; set; }

        public string? ArmyPointsAdvantageSide { get; set; }

        public bool ArmyPointsAdvantageIsPercent { get; set; }

        public int ArmyPointsAdvantageAmount { get; set; }

        public bool HasSupplyPointsAdvantage { get; set; }

        public string? SupplyPointsAdvantageSide { get; set; }

        public int SupplyPointsAdvantageAmount { get; set; }

        public List<MissionStatusChangeDocument>? StatusChanges { get; set; }

        public List<Guid>? TagIds { get; set; }
    }

    private sealed class MissionStatusChangeDocument
    {
        public Guid Id { get; set; }

        public string Outcome { get; set; } = "Win";

        public string? WhenCurrentStatus { get; set; }

        public string? SetStatus { get; set; }

        public bool LeaveUnchanged { get; set; }
    }

    private sealed class MissionQuestionDocument
    {
        public Guid Id { get; set; }

        public string Prompt { get; set; } = string.Empty;

        public string Kind { get; set; } = nameof(MissionResultQuestionKind.Boolean);

        public int BattlePoints { get; set; }

        public int CampaignPoints { get; set; }

        public Guid? StandardQuestionId { get; set; }
    }

    private sealed class StandardQuestionDocument
    {
        public Guid Id { get; set; }

        public string Prompt { get; set; } = string.Empty;

        public string Kind { get; set; } = nameof(MissionResultQuestionKind.Boolean);

        public int BattlePoints { get; set; }

        public int CampaignPoints { get; set; }
    }

    private sealed class ArmyEscalationDocument
    {
        public int RoundNumber { get; set; }

        public int MaxArmyPoints { get; set; }

        public int FreeSupplyPoints { get; set; }

        public int FreeCharacterCount { get; set; }
    }

    private sealed class ItemChoiceDocument
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public List<ItemChoiceResultDocument> Results { get; set; } = [];
    }

    private sealed class ItemChoiceResultDocument
    {
        public Guid Id { get; set; }

        public string? FlavorText { get; set; }

        public string? NewStateKey { get; set; }

        public bool DestroyItem { get; set; }

        public Guid? ReplacementItemTypeId { get; set; }

        public Guid? GrantedPrivateObjectiveTypeId { get; set; }

        public string? SetForceStatusName { get; set; }
    }

    private sealed class SpecialRuleDocument
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Text { get; set; }

        public string? EffectKey { get; set; }
    }

    private sealed class ForceStatusDocument
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Effects { get; set; }

        public string? EnableTrigger { get; set; }

        public string? ClearTrigger { get; set; }

        public int? Priority { get; set; }

        public List<Guid>? CancelsStatusIds { get; set; }

        public List<Guid>? ImmuneFactionIds { get; set; }

        public List<ForceStatusImmuneSubfactionDocument>? ImmuneSubfactions { get; set; }

        public string? TokenImageStorageKey { get; set; }

        public List<ForceStatusConditionDocument>? EnableConditions { get; set; }

        public List<ForceStatusConditionDocument>? ClearConditions { get; set; }

        public int? EnableOccurrences { get; set; }

        public int? ClearOccurrences { get; set; }
    }

    private sealed class ForceStatusImmuneSubfactionDocument
    {
        public Guid FactionId { get; set; }

        public string Subfaction { get; set; } = string.Empty;
    }

    private sealed class ForceStatusConditionDocument
    {
        public Guid Id { get; set; }

        public string? Trigger { get; set; }

        public int? Occurrences { get; set; }

        public string? LocationKind { get; set; }

        public Guid? LocationTypeId { get; set; }

        public Guid? LocationTagId { get; set; }

        public Guid? RequiredStatusId { get; set; }

        public Guid? RequiredQuestionId { get; set; }
    }

    private sealed class PrivateObjectiveDocument
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int CampaignPoints { get; set; }

        public List<string>? AllowedHolderKinds { get; set; }

        public string ScoringKind { get; set; } = "Manual";

        public string AutomaticKind { get; set; } = "None";

        public int RequiredCount { get; set; } = 1;

        public Guid? StructureTypeId { get; set; }

        public List<Guid>? TerritoryIds { get; set; }

        public bool MatchesAnyStructureType { get; set; }

        public Guid? ItemObjectiveTypeId { get; set; }

        public bool MatchesAnyItemObjective { get; set; }

        public string TargetKind { get; set; } = "None";

        public string TargetSelection { get; set; } = "Specific";

        public Guid? TargetId { get; set; }

        public List<Guid>? ForceStatusTypeIds { get; set; }

        public string StatusMatchKind { get; set; } = "None";

        public Guid? PrerequisiteForceStatusTypeId { get; set; }

        public bool PrerequisiteWasLost { get; set; }

        public Guid? StructureTagId { get; set; }

        public Guid? TerrainTagId { get; set; }

        public List<Guid>? ExcludedFactionIds { get; set; }

        public List<Guid>? ExcludedAllyGroupIds { get; set; }
    }

    private sealed class FactionSpecialRulesDocument
    {
        public Guid FactionId { get; set; }

        public string? SubfactionName { get; set; }

        public List<Guid> SpecialRuleIds { get; set; } = [];

        public int? ForceMovementSpeed { get; set; }
    }

    private sealed class CatalogTagDocument
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    private sealed class FactionTagsDocument
    {
        public Guid FactionId { get; set; }

        public string? SubfactionName { get; set; }

        public List<Guid> TagIds { get; set; } = [];
    }
}
