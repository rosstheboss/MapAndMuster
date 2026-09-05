namespace MapAndMuster.Application.Campaigns;

/// <summary>
/// Copies uploaded catalog files from a saved preset onto another campaign by catalog name.
/// </summary>
internal static class CampaignPresetCatalogFiles
{
    public static StoredCampaign CopyOnto(StoredCampaign campaign, StoredCampaign preset)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        ArgumentNullException.ThrowIfNull(preset);
        return new StoredCampaign
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Description = campaign.Description,
            PlayerSlotCount = campaign.PlayerSlotCount,
            IsPrivate = campaign.IsPrivate,
            IsPubliclyViewable = campaign.IsPubliclyViewable,
            JoinPasswordHash = campaign.JoinPasswordHash,
            CreatorIsParticipant = campaign.CreatorIsParticipant,
            City = campaign.City,
            Region = campaign.Region,
            Country = campaign.Country,
            MapStorageKey = campaign.MapStorageKey,
            Revision = campaign.Revision,
            CreatedUtc = campaign.CreatedUtc,
            UpdatedUtc = campaign.UpdatedUtc,
            CreatedByUserId = campaign.CreatedByUserId,
            Memberships = campaign.Memberships,
            Factions = Merge(campaign.Factions, preset.Factions, static faction => faction.Name, CopyFactionFiles),
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
            PlayState = campaign.PlayState,
            TerrainTypes = Merge(campaign.TerrainTypes, preset.TerrainTypes, static type => type.Name, CopyTerrainFiles),
            StructureTypes = Merge(
                campaign.StructureTypes,
                preset.StructureTypes,
                static type => type.Name,
                CopyStructureFiles),
            ItemObjectiveTypes = Merge(
                campaign.ItemObjectiveTypes,
                preset.ItemObjectiveTypes,
                static type => type.Name,
                CopyItemFiles),
            PublicObjectiveTypes = campaign.PublicObjectiveTypes,
            SpecialRules = campaign.SpecialRules,
            Missions = Merge(campaign.Missions, preset.Missions, static mission => mission.Name, CopyMissionFiles),
            ForceStatuses = campaign.ForceStatuses,
            PrivateObjectiveTypes = campaign.PrivateObjectiveTypes,
            BattleScoring = campaign.BattleScoring,
            RankingObjectivePoints = campaign.RankingObjectivePoints,
            SplitForceSupplyPenaltyPercent = campaign.SplitForceSupplyPenaltyPercent,
            SplitForceSupplyPenaltyIsPercent = campaign.SplitForceSupplyPenaltyIsPercent,
            StandardBattleResultQuestions = campaign.StandardBattleResultQuestions,
            ArmyEscalations = campaign.ArmyEscalations,
        };
    }

    private static List<T> Merge<T>(
        IReadOnlyList<T> destination,
        IReadOnlyList<T> preset,
        Func<T, string> nameSelector,
        Func<T, T, T> copyFiles)
    {
        var presetByName = CatalogFileBinder.IndexByName(preset, nameSelector);
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var merged = new List<T>(destination.Count + preset.Count);
        foreach (var item in destination)
        {
            var name = nameSelector(item).Trim();
            if (name.Length > 0 && presetByName.TryGetValue(name, out var source))
            {
                used.Add(name);
                merged.Add(copyFiles(item, source));
                continue;
            }

            merged.Add(item);
        }

        foreach (var source in preset)
        {
            var name = nameSelector(source).Trim();
            if (name.Length > 0 && used.Add(name))
            {
                merged.Add(source);
            }
        }

        return merged;
    }

    private static StoredFaction CopyFactionFiles(StoredFaction destination, StoredFaction source)
    {
        var hasFlag = CatalogFileBinder.IsUserUploadedFileKey(source.FlagImageStorageKey);
        return new StoredFaction
        {
            Id = destination.Id,
            Name = destination.Name,
            Color = destination.Color,
            Subfactions = destination.Subfactions,
            SubfactionAppearances = Merge(
                destination.SubfactionAppearances,
                source.SubfactionAppearances,
                static appearance => appearance.Name,
                CopyAppearanceFiles),
            AllyGroupName = destination.AllyGroupName,
            RequiresSubfaction = destination.RequiresSubfaction,
            FlagImageStorageKey = hasFlag ? source.FlagImageStorageKey : destination.FlagImageStorageKey,
            TintFlagImage = hasFlag ? source.TintFlagImage : destination.TintFlagImage,
            SpecialRuleIds = destination.SpecialRuleIds,
            SubfactionSpecialRules = destination.SubfactionSpecialRules,
        };
    }

    private static StoredSubfactionAppearance CopyAppearanceFiles(
        StoredSubfactionAppearance destination,
        StoredSubfactionAppearance source)
    {
        var hasFlag = CatalogFileBinder.IsUserUploadedFileKey(source.FlagImageStorageKey);
        return new StoredSubfactionAppearance
        {
            Name = destination.Name,
            Color = destination.Color,
            FlagSource = hasFlag ? source.FlagSource : destination.FlagSource,
            FlagImageStorageKey = hasFlag ? source.FlagImageStorageKey : destination.FlagImageStorageKey,
            TintFlagImage = hasFlag ? source.TintFlagImage : destination.TintFlagImage,
        };
    }

    private static StoredTerrainType CopyTerrainFiles(StoredTerrainType destination, StoredTerrainType source)
    {
        return new StoredTerrainType
        {
            Id = destination.Id,
            Name = destination.Name,
            Color = destination.Color,
            Missions = Merge(destination.Missions, source.Missions, static mission => mission.Name, CopyMissionFiles),
            CampaignPoints = destination.CampaignPoints,
            SupplyPoints = destination.SupplyPoints,
            IsWaterFeature = destination.IsWaterFeature,
        };
    }

    private static StoredStructureType CopyStructureFiles(StoredStructureType destination, StoredStructureType source)
    {
        var hasImage = CatalogFileBinder.IsUserUploadedFileKey(source.ImageStorageKey);
        var hasPillaged = CatalogFileBinder.IsUserUploadedFileKey(source.PillagedImageStorageKey);
        return new StoredStructureType
        {
            Id = destination.Id,
            Name = destination.Name,
            BuiltinSymbol = hasImage ? source.BuiltinSymbol : destination.BuiltinSymbol,
            ImageStorageKey = hasImage ? source.ImageStorageKey : destination.ImageStorageKey,
            PillagedImageStorageKey = hasPillaged ? source.PillagedImageStorageKey : destination.PillagedImageStorageKey,
            IsBuildable = destination.IsBuildable,
            IsPillageable = destination.IsPillageable,
            IsDestructible = destination.IsDestructible,
            Missions = Merge(destination.Missions, source.Missions, static mission => mission.Name, CopyMissionFiles),
            CampaignPoints = destination.CampaignPoints,
            SupplyPoints = destination.SupplyPoints,
            PillageSupplyPoints = destination.PillageSupplyPoints,
            DestroySupplyPoints = destination.DestroySupplyPoints,
        };
    }

    private static StoredItemObjectiveType CopyItemFiles(StoredItemObjectiveType destination, StoredItemObjectiveType source)
    {
        var hasImage = CatalogFileBinder.IsUserUploadedFileKey(source.ImageStorageKey);
        return new StoredItemObjectiveType
        {
            Id = destination.Id,
            Name = destination.Name,
            IsHiddenUntilFound = destination.IsHiddenUntilFound,
            Placement = destination.Placement,
            AllowOnSpawn = destination.AllowOnSpawn,
            BuiltinSymbol = hasImage ? source.BuiltinSymbol : destination.BuiltinSymbol,
            Color = destination.Color,
            ImageStorageKey = hasImage ? source.ImageStorageKey : destination.ImageStorageKey,
            CampaignPoints = destination.CampaignPoints,
            FlavorText = destination.FlavorText,
            Choices = destination.Choices,
            SpecialRuleIds = destination.SpecialRuleIds,
        };
    }

    private static StoredMission CopyMissionFiles(StoredMission destination, StoredMission source)
    {
        var hasFile = CatalogFileBinder.IsUserUploadedFileKey(source.FileStorageKey);
        return new StoredMission
        {
            Id = destination.Id,
            Name = destination.Name,
            Url = destination.Url,
            FileStorageKey = hasFile ? source.FileStorageKey : destination.FileStorageKey,
            FileName = hasFile ? source.FileName : destination.FileName,
            ResultQuestions = destination.ResultQuestions,
            IsAttackerDefender = destination.IsAttackerDefender,
            HasArmyPointsAdvantage = destination.HasArmyPointsAdvantage,
            ArmyPointsAdvantageSide = destination.ArmyPointsAdvantageSide,
            ArmyPointsAdvantageIsPercent = destination.ArmyPointsAdvantageIsPercent,
            ArmyPointsAdvantageAmount = destination.ArmyPointsAdvantageAmount,
            HasSupplyPointsAdvantage = destination.HasSupplyPointsAdvantage,
            SupplyPointsAdvantageSide = destination.SupplyPointsAdvantageSide,
            SupplyPointsAdvantageAmount = destination.SupplyPointsAdvantageAmount,
        };
    }
}
