using System.Text.Json;
using MapAndMuster.Application.Campaigns;
using MapAndMuster.Domain.Campaigns;
using MapAndMuster.Domain.Maps;

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
        BattleReportRulesSetup? battleReportRules = null,
        IReadOnlyList<RoundArmyEscalationSetup>? armyEscalations = null,
        IReadOnlyList<StoredMission>? missions = null,
        IReadOnlyDictionary<Guid, IReadOnlyList<SubfactionSpecialRulesDetail>>? subfactionSpecialRules = null)
    {
        ArgumentNullException.ThrowIfNull(terrainTypes);
        ArgumentNullException.ThrowIfNull(structureTypes);
        var scoring = battleScoring ?? BattleScoringSetup.Default;
        var ranking = rankingObjectivePoints ?? GeneralPublicObjectivePoints.None;
        var reportRules = battleReportRules ?? BattleReportRulesSetup.Default;
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
                FactionSpecialRules =
                [
                    .. (factionSpecialRuleIds ?? new Dictionary<Guid, IReadOnlyList<Guid>>()).Select(static pair =>
                        new FactionSpecialRulesDocument
                        {
                            FactionId = pair.Key,
                            SpecialRuleIds = [.. pair.Value],
                        }),
                    .. (subfactionSpecialRules ?? new Dictionary<Guid, IReadOnlyList<SubfactionSpecialRulesDetail>>())
                        .SelectMany(static pair => pair.Value.Select(item =>
                            new FactionSpecialRulesDocument
                            {
                                FactionId = pair.Key,
                                SubfactionName = item.Name,
                                SpecialRuleIds = [.. item.SpecialRuleIds],
                            })),
                ],
                PointsPerBattleWon = scoring.PointsPerWin,
                BattleScoring = ToDocument(scoring),
                MostTerritoriesCampaignPoints = ranking.MostTerritories,
                LongestTerritoryChainCampaignPoints = ranking.LongestTerritoryChain,
                MostBattlesWonCampaignPoints = ranking.MostBattlesWon,
                MostStructurePointsCampaignPoints = ranking.MostStructurePoints,
                PointsPerTerritoryCampaignPoints = ranking.PointsPerTerritory,
                AlliedRelicControlCampaignPoints = ranking.AlliedRelicControlPoints,
                SplitForceSupplyPenaltyPercent = splitForceSupplyPenaltyPercent,
                SplitForceSupplyPenaltyIsPercent = splitForceSupplyPenaltyIsPercent,
                AlwaysAskGeneralKill = reportRules.AlwaysAskGeneralKill,
                AlwaysAskSupplyLineDestroyed = reportRules.AlwaysAskSupplyLineDestroyed,
                GeneralKillCampaignPoints = reportRules.GeneralKillCampaignPoints,
                SupplyLineDestroyedCampaignPoints = reportRules.SupplyLineDestroyedCampaignPoints,
                ArmyEscalations = [.. (armyEscalations ?? []).Select(ToDocument)],
                Missions = [.. (missions ?? []).Select(ToDocument)],
            },
            Options);
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
        BattleReportRulesSetup BattleReportRules,
        IReadOnlyList<RoundArmyEscalationSetup> ArmyEscalations,
        IReadOnlyList<StoredMission> Missions)
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
        var structures = document.StructureTypes.Select(FromDocument).ToArray();
        var catalogMissions = MergeMissions(
            (document.Missions ?? []).Select(FromDocument),
            terrains.SelectMany(static type => type.Missions).Concat(structures.SelectMany(static type => type.Missions)));
        var factionRules = document.FactionSpecialRules ?? [];
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
                Math.Max(0, document.AlliedRelicControlCampaignPoints)),
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
            [.. (document.ForceStatuses ?? []).Select(FromDocument)],
            ReadSplitForcePenaltyValue(document),
            ReadSplitForcePenaltyIsPercent(document),
            new BattleReportRulesSetup(
                document.AlwaysAskGeneralKill ?? HuntInEstaliaDefaults.AlwaysAskGeneralKill,
                document.AlwaysAskSupplyLineDestroyed ?? HuntInEstaliaDefaults.AlwaysAskSupplyLineDestroyed,
                document.GeneralKillCampaignPoints ?? HuntInEstaliaDefaults.GeneralKillCampaignPoints,
                document.SupplyLineDestroyedCampaignPoints ?? HuntInEstaliaDefaults.SupplyLineDestroyedCampaignPoints),
            ArmyEscalationsFrom(document),
            catalogMissions);
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
        BattleReportRulesSetup,
        IReadOnlyList<RoundArmyEscalationSetup>,
        IReadOnlyList<StoredMission>) EmptyCatalog()
    {
        return ([], [], [], [], BattleScoringSetup.Straight(0), GeneralPublicObjectivePoints.None, [], [], new Dictionary<Guid, IReadOnlyList<Guid>>(), new Dictionary<Guid, IReadOnlyList<SubfactionSpecialRulesDetail>>(), [], HuntInEstaliaDefaults.SplitForceSupplyPenaltyValue, HuntInEstaliaDefaults.SplitForceSupplyPenaltyIsPercent, BattleReportRulesSetup.Default, [], []);
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
            IsWaterFeature = type.IsWaterFeature,
            SupplyPoints = type.SupplyPoints,
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
            IsWaterFeature = type.IsWaterFeature,
            SupplyPoints = type.SupplyPoints > 0 ? type.SupplyPoints : HuntInEstaliaDefaults.SupplyPoints,
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
            SupplyPoints = type.SupplyPoints > 0 ? type.SupplyPoints : HuntInEstaliaDefaults.SupplyPoints,
            PillageSupplyPoints = type.PillageSupplyPoints > 0 ? type.PillageSupplyPoints : HuntInEstaliaDefaults.SupplyPoints,
            DestroySupplyPoints = type.DestroySupplyPoints > 0 ? type.DestroySupplyPoints : HuntInEstaliaDefaults.SupplyPoints,
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
        };
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
        };
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
        };
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

        public List<ArmyEscalationDocument>? ArmyEscalations { get; set; }

        public List<MissionDocument>? Missions { get; set; }
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
    }

    private sealed class MissionQuestionDocument
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
    }

    private sealed class FactionSpecialRulesDocument
    {
        public Guid FactionId { get; set; }

        public string? SubfactionName { get; set; }

        public List<Guid> SpecialRuleIds { get; set; } = [];
    }
}
