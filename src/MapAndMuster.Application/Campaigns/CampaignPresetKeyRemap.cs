namespace MapAndMuster.Application.Campaigns;

/// <summary>
/// Rewrites uploaded-file storage keys after a portable preset is copied onto a new host.
/// </summary>
internal static class CampaignPresetKeyRemap
{
    public static StoredCampaign Remap(StoredCampaign campaign, IReadOnlyDictionary<string, string> keys)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        ArgumentNullException.ThrowIfNull(keys);
        return new StoredCampaign
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Description = campaign.Description,
            PlayerSlotCount = campaign.PlayerSlotCount,
            IsPrivate = campaign.IsPrivate,
            IsPubliclyViewable = campaign.IsPubliclyViewable,
            CreatorIsParticipant = campaign.CreatorIsParticipant,
            MapStorageKey = RemapKey(campaign.MapStorageKey, keys),
            Revision = campaign.Revision,
            CreatedUtc = campaign.CreatedUtc,
            UpdatedUtc = campaign.UpdatedUtc,
            CreatedByUserId = campaign.CreatedByUserId,
            Memberships = [],
            Factions =
            [
                .. campaign.Factions.Select(faction => new StoredFaction
                {
                    Id = faction.Id,
                    Name = faction.Name,
                    Color = faction.Color,
                    Subfactions = faction.Subfactions,
                    SubfactionAppearances =
                    [
                        .. faction.SubfactionAppearances.Select(appearance => new StoredSubfactionAppearance
                        {
                            Name = appearance.Name,
                            Color = appearance.Color,
                            FlagSource = appearance.FlagSource,
                            FlagImageStorageKey = RemapKey(appearance.FlagImageStorageKey, keys),
                            TintFlagImage = appearance.TintFlagImage,
                        }),
                    ],
                    AllyGroupName = faction.AllyGroupName,
                    RequiresSubfaction = faction.RequiresSubfaction,
                    FlagImageStorageKey = RemapKey(faction.FlagImageStorageKey, keys),
                    TintFlagImage = faction.TintFlagImage,
                    SpecialRuleIds = faction.SpecialRuleIds,
                    SubfactionSpecialRules = faction.SubfactionSpecialRules,
                }),
            ],
            AllyGroups = campaign.AllyGroups,
            Links = campaign.Links,
            TimeZoneId = campaign.TimeZoneId,
            StartsUtc = campaign.StartsUtc,
            EndsUtc = campaign.EndsUtc,
            ClosedUtc = campaign.ClosedUtc,
            RoundCount = campaign.RoundCount,
            RoundLengthAmount = campaign.RoundLengthAmount,
            RoundLengthUnit = campaign.RoundLengthUnit,
            Phases = campaign.Phases,
            MapGraph = campaign.MapGraph,
            TerrainTypes = [.. campaign.TerrainTypes.Select(type => RemapTerrain(type, keys))],
            StructureTypes = [.. campaign.StructureTypes.Select(type => RemapStructure(type, keys))],
            ItemObjectiveTypes = [.. campaign.ItemObjectiveTypes.Select(type => RemapItem(type, keys))],
            PublicObjectiveTypes = campaign.PublicObjectiveTypes,
            SpecialRules = campaign.SpecialRules,
            Missions = [.. campaign.Missions.Select(mission => RemapMission(mission, keys))],
            ForceStatuses = campaign.ForceStatuses,
            PrivateObjectiveTypes = campaign.PrivateObjectiveTypes,
            BattleScoring = campaign.BattleScoring,
            RankingObjectivePoints = campaign.RankingObjectivePoints,
            SplitForceSupplyPenaltyPercent = campaign.SplitForceSupplyPenaltyPercent,
            SplitForceSupplyPenaltyIsPercent = campaign.SplitForceSupplyPenaltyIsPercent,
            BattleReportRules = campaign.BattleReportRules,
            ArmyEscalations = campaign.ArmyEscalations,
        };
    }

    private static StoredTerrainType RemapTerrain(StoredTerrainType type, IReadOnlyDictionary<string, string> keys)
    {
        return new StoredTerrainType
        {
            Id = type.Id,
            Name = type.Name,
            Color = type.Color,
            Missions = [.. type.Missions.Select(mission => RemapMission(mission, keys))],
            CampaignPoints = type.CampaignPoints,
            SupplyPoints = type.SupplyPoints,
            IsWaterFeature = type.IsWaterFeature,
        };
    }

    private static StoredStructureType RemapStructure(StoredStructureType type, IReadOnlyDictionary<string, string> keys)
    {
        return new StoredStructureType
        {
            Id = type.Id,
            Name = type.Name,
            BuiltinSymbol = type.BuiltinSymbol,
            ImageStorageKey = RemapKey(type.ImageStorageKey, keys),
            PillagedImageStorageKey = RemapKey(type.PillagedImageStorageKey, keys),
            IsBuildable = type.IsBuildable,
            IsPillageable = type.IsPillageable,
            IsDestructible = type.IsDestructible,
            Missions = [.. type.Missions.Select(mission => RemapMission(mission, keys))],
            CampaignPoints = type.CampaignPoints,
            SupplyPoints = type.SupplyPoints,
            PillageSupplyPoints = type.PillageSupplyPoints,
            DestroySupplyPoints = type.DestroySupplyPoints,
        };
    }

    private static StoredItemObjectiveType RemapItem(StoredItemObjectiveType type, IReadOnlyDictionary<string, string> keys)
    {
        return new StoredItemObjectiveType
        {
            Id = type.Id,
            Name = type.Name,
            IsHiddenUntilFound = type.IsHiddenUntilFound,
            Placement = type.Placement,
            AllowOnSpawn = type.AllowOnSpawn,
            BuiltinSymbol = type.BuiltinSymbol,
            Color = type.Color,
            ImageStorageKey = RemapKey(type.ImageStorageKey, keys),
            CampaignPoints = type.CampaignPoints,
            FlavorText = type.FlavorText,
            Choices = type.Choices,
            SpecialRuleIds = type.SpecialRuleIds,
        };
    }

    private static StoredMission RemapMission(StoredMission mission, IReadOnlyDictionary<string, string> keys)
    {
        return new StoredMission
        {
            Id = mission.Id,
            Name = mission.Name,
            Url = mission.Url,
            FileStorageKey = RemapKey(mission.FileStorageKey, keys),
            FileName = mission.FileName,
            ResultQuestions = mission.ResultQuestions,
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

    private static string? RemapKey(string? storageKey, IReadOnlyDictionary<string, string> keys)
    {
        if (!CatalogFileBinder.IsUserUploadedFileKey(storageKey))
        {
            return storageKey;
        }

        return keys.TryGetValue(storageKey, out var remapped) ? remapped : null;
    }
}
