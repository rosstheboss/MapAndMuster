using MapAndMuster.Application.Campaigns;
using MapAndMuster.Application.Common;
using MapAndMuster.Application.Maps;
using MapAndMuster.Application.Ports;
using MapAndMuster.Domain.Campaigns;
using MapAndMuster.Domain.Play;

namespace MapAndMuster.Backend.UnitTests.Campaigns;

public sealed class CampaignPresetHandlerTests
{
    [Fact]
    public async Task SaveRejectsNonAdministrators()
    {
        var campaigns = new PresetCampaignStore();
        var handler = new SaveCampaignPresetHandler(campaigns, new FakePresetStore(), new FixedClock());
        var result = await handler.HandleAsync(
            new SaveCampaignPresetCommand
            {
                CampaignId = campaigns.Campaign.Id,
                UserId = Guid.NewGuid(),
                IsAdministrator = false,
                Name = "Frontier War",
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CampaignForbidden, result.ErrorCode);
    }

    [Fact]
    public async Task SaveCopiesCatalogAndMapForAdministrators()
    {
        var campaigns = new PresetCampaignStore();
        var presets = new FakePresetStore();
        var handler = new SaveCampaignPresetHandler(campaigns, presets, new FixedClock());
        var result = await handler.HandleAsync(
            new SaveCampaignPresetCommand
            {
                CampaignId = campaigns.Campaign.Id,
                UserId = Guid.NewGuid(),
                IsAdministrator = true,
                Name = "Frontier War",
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Frontier War", result.Value.Name);
        Assert.True(result.Value.HasMap);
        Assert.Single(presets.Items);
        Assert.Equal("maps/border.png", presets.Items[0].MapStorageKey);
        Assert.NotNull(presets.Items[0].MapGraph);
        Assert.Equal("Northmarch", presets.Items[0].MapGraph!.Territories[0].Name);
    }

    [Fact]
    public async Task SaveCopiesFactionFlagsAndCatalogLogos()
    {
        var campaigns = new PresetCampaignStore();
        campaigns.AttachCatalogLogos();
        var presets = new FakePresetStore();
        var handler = new SaveCampaignPresetHandler(campaigns, presets, new FixedClock());
        var result = await handler.HandleAsync(
            new SaveCampaignPresetCommand
            {
                CampaignId = campaigns.Campaign.Id,
                UserId = Guid.NewGuid(),
                IsAdministrator = true,
                Name = "Frontier War",
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var saved = Assert.Single(presets.Items);
        Assert.Equal("flags/north.png", saved.Factions.Single(faction => faction.Name == "North").FlagImageStorageKey);
        Assert.True(saved.Factions.Single(faction => faction.Name == "North").TintFlagImage);
        Assert.Equal("structures/town.png", saved.StructureTypes.Single(type => type.Name == "Town").ImageStorageKey);
        Assert.Equal("items/crown.png", saved.ItemObjectiveTypes.Single(type => type.Name == "Crown").ImageStorageKey);
    }

    [Fact]
    public async Task ApplyCopiesMapImageAndOverlayGraph()
    {
        var campaigns = new PresetCampaignStore();
        var originalGraph = campaigns.Campaign.MapGraph;
        var originalKey = campaigns.Campaign.MapStorageKey;
        var presets = new FakePresetStore();
        var saved = await new SaveCampaignPresetHandler(campaigns, presets, new FixedClock()).HandleAsync(
            new SaveCampaignPresetCommand
            {
                CampaignId = campaigns.Campaign.Id,
                UserId = Guid.NewGuid(),
                IsAdministrator = true,
                Name = "Frontier War",
            },
            CancellationToken.None);
        campaigns.StripMap();

        var result = await new ApplyCampaignPresetHandler(campaigns, presets, new FixedClock()).HandleAsync(
            new ApplyCampaignPresetCommand
            {
                CampaignId = campaigns.Campaign.Id,
                PresetId = saved.Value!.Id,
                UserId = Guid.NewGuid(),
                IsAdministrator = true,
                Revision = campaigns.Campaign.Revision,
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.HasMap);
        Assert.Equal(originalKey, campaigns.Campaign.MapStorageKey);
        Assert.Equal(originalGraph!.Territories[0].Name, campaigns.Campaign.MapGraph!.Territories[0].Name);
    }

    [Fact]
    public async Task ApplyCopiesCatalogLogosOntoMatchingNames()
    {
        var campaigns = new PresetCampaignStore();
        campaigns.AttachCatalogLogos();
        var presets = new FakePresetStore();
        var saved = await new SaveCampaignPresetHandler(campaigns, presets, new FixedClock()).HandleAsync(
            new SaveCampaignPresetCommand
            {
                CampaignId = campaigns.Campaign.Id,
                UserId = Guid.NewGuid(),
                IsAdministrator = true,
                Name = "Frontier War",
            },
            CancellationToken.None);
        var northId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        campaigns.RetargetFactionsAndClearLogos(northId);

        var result = await new ApplyCampaignPresetHandler(campaigns, presets, new FixedClock()).HandleAsync(
            new ApplyCampaignPresetCommand
            {
                CampaignId = campaigns.Campaign.Id,
                PresetId = saved.Value!.Id,
                UserId = Guid.NewGuid(),
                IsAdministrator = true,
                Revision = campaigns.Campaign.Revision,
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var north = Assert.Single(campaigns.Campaign.Factions, faction => faction.Name == "North");
        Assert.Equal(northId, north.Id);
        Assert.Equal("flags/north.png", north.FlagImageStorageKey);
        Assert.True(north.TintFlagImage);
        Assert.True(result.Value!.Factions.Single(faction => faction.Name == "North").HasFlagImage);
        Assert.Equal("structures/town.png", campaigns.Campaign.StructureTypes.Single(type => type.Name == "Town").ImageStorageKey);
        Assert.Equal("items/crown.png", campaigns.Campaign.ItemObjectiveTypes.Single(type => type.Name == "Crown").ImageStorageKey);
    }

    [Fact]
    public async Task ApplyRemapsOverlayTerrainOntoTheTargetCatalog()
    {
        var campaigns = new PresetCampaignStore();
        var presets = new FakePresetStore();
        var saved = await new SaveCampaignPresetHandler(campaigns, presets, new FixedClock()).HandleAsync(
            new SaveCampaignPresetCommand
            {
                CampaignId = campaigns.Campaign.Id,
                UserId = Guid.NewGuid(),
                IsAdministrator = true,
                Name = "Frontier War",
            },
            CancellationToken.None);
        var targetTerrain = Guid.Parse("77777777-7777-7777-7777-777777777777");
        campaigns.RetargetPlains(targetTerrain);

        var result = await new ApplyCampaignPresetHandler(campaigns, presets, new FixedClock()).HandleAsync(
            new ApplyCampaignPresetCommand
            {
                CampaignId = campaigns.Campaign.Id,
                PresetId = saved.Value!.Id,
                UserId = Guid.NewGuid(),
                IsAdministrator = true,
                Revision = campaigns.Campaign.Revision,
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Northmarch", campaigns.Campaign.MapGraph!.Territories[0].Name);
        Assert.Equal(targetTerrain, campaigns.Campaign.MapGraph.Territories[0].TerrainTypeId);
    }

    [Fact]
    public async Task SaveOverwritesAnExistingPresetName()
    {
        var campaigns = new PresetCampaignStore();
        var presets = new FakePresetStore();
        var handler = new SaveCampaignPresetHandler(campaigns, presets, new FixedClock());
        var first = await handler.HandleAsync(
            new SaveCampaignPresetCommand
            {
                CampaignId = campaigns.Campaign.Id,
                UserId = Guid.NewGuid(),
                IsAdministrator = true,
                Name = "Frontier War",
            },
            CancellationToken.None);
        var second = await handler.HandleAsync(
            new SaveCampaignPresetCommand
            {
                CampaignId = campaigns.Campaign.Id,
                UserId = Guid.NewGuid(),
                IsAdministrator = true,
                Name = "frontier war",
            },
            CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value!.Id, second.Value!.Id);
        Assert.Single(presets.Items);
        Assert.Equal("frontier war", presets.Items[0].Name);
    }

    [Fact]
    public async Task SaveOverwritesWhenTheNameDiffersOnlyByWhitespace()
    {
        var campaigns = new PresetCampaignStore();
        var presets = new FakePresetStore();
        var handler = new SaveCampaignPresetHandler(campaigns, presets, new FixedClock());
        var first = await handler.HandleAsync(
            new SaveCampaignPresetCommand
            {
                CampaignId = campaigns.Campaign.Id,
                UserId = Guid.NewGuid(),
                IsAdministrator = true,
                Name = "The Hunt in Estalia",
            },
            CancellationToken.None);
        var second = await handler.HandleAsync(
            new SaveCampaignPresetCommand
            {
                CampaignId = campaigns.Campaign.Id,
                UserId = Guid.NewGuid(),
                IsAdministrator = true,
                Name = "  The Hunt   in Estalia  ",
            },
            CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value!.Id, second.Value!.Id);
        Assert.Single(presets.Items);
        Assert.Equal("The Hunt in Estalia", presets.Items[0].Name);
    }

    [Fact]
    public async Task ExportRejectsNonAdministrators()
    {
        var campaigns = new PresetCampaignStore();
        var handler = new ExportCampaignPresetHandler(
            campaigns,
            new FakePresetStore(),
            new MemoryMapStorage(),
            new MemoryAssetStorage(),
            new RecordingPackageCodec());
        var result = await handler.HandleAsync(
            new ExportCampaignPresetCommand
            {
                CampaignId = campaigns.Campaign.Id,
                UserId = Guid.NewGuid(),
                IsAdministrator = false,
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CampaignForbidden, result.ErrorCode);
    }

    [Fact]
    public async Task ExportWritesTheCampaignMapAndOverlay()
    {
        var campaigns = new PresetCampaignStore();
        var maps = new MemoryMapStorage();
        maps.Files["maps/border.png"] = [7, 8, 9];
        var codec = new RecordingPackageCodec();
        var handler = new ExportCampaignPresetHandler(
            campaigns,
            new FakePresetStore(),
            maps,
            new MemoryAssetStorage(),
            codec);
        var result = await handler.HandleAsync(
            new ExportCampaignPresetCommand
            {
                CampaignId = campaigns.Campaign.Id,
                UserId = Guid.NewGuid(),
                IsAdministrator = true,
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.EndsWith(".mapandmuster-preset", result.Value.DownloadName, StringComparison.Ordinal);
        Assert.Equal("maps/border.png", codec.WrittenCampaign?.MapStorageKey);
        Assert.Equal(new byte[] { 7, 8, 9 }, codec.WrittenFiles["maps/border.png"]);
        Assert.Equal("Northmarch", codec.WrittenCampaign?.MapGraph?.Territories[0].Name);
    }

    [Fact]
    public async Task ExportWritesFactionFlagsAndCatalogLogos()
    {
        var campaigns = new PresetCampaignStore();
        campaigns.AttachCatalogLogos();
        var maps = new MemoryMapStorage();
        maps.Files["maps/border.png"] = [7, 8, 9];
        var assets = new MemoryAssetStorage();
        assets.Files["flags/north.png"] = [1, 2, 3];
        assets.Files["structures/town.png"] = [4, 5, 6];
        assets.Files["items/crown.png"] = [10, 11, 12];
        var codec = new RecordingPackageCodec();
        var handler = new ExportCampaignPresetHandler(
            campaigns,
            new FakePresetStore(),
            maps,
            assets,
            codec);
        var result = await handler.HandleAsync(
            new ExportCampaignPresetCommand
            {
                CampaignId = campaigns.Campaign.Id,
                UserId = Guid.NewGuid(),
                IsAdministrator = true,
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new byte[] { 1, 2, 3 }, codec.WrittenFiles["flags/north.png"]);
        Assert.Equal(new byte[] { 4, 5, 6 }, codec.WrittenFiles["structures/town.png"]);
        Assert.Equal(new byte[] { 10, 11, 12 }, codec.WrittenFiles["items/crown.png"]);
        Assert.Equal("flags/north.png", codec.WrittenCampaign?.Factions.Single(faction => faction.Name == "North").FlagImageStorageKey);
    }

    [Fact]
    public async Task ImportRejectsNonAdministrators()
    {
        var handler = new ImportCampaignPresetHandler(
            new FakePresetStore(),
            new MemoryMapStorage(),
            new MemoryAssetStorage(),
            new PassingImageProcessor(),
            new PassingDocumentProcessor(),
            new RecordingPackageCodec(),
            new FixedClock());
        var result = await handler.HandleAsync(
            new ImportCampaignPresetCommand
            {
                UserId = Guid.NewGuid(),
                IsAdministrator = false,
                Content = [1, 2, 3],
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CampaignForbidden, result.ErrorCode);
    }

    [Fact]
    public async Task ImportStoresCopiedFilesAndUpsertsByName()
    {
        var presets = new FakePresetStore();
        var maps = new MemoryMapStorage();
        var codec = new RecordingPackageCodec
        {
            Contents = new CampaignPresetPackageContents
            {
                Name = "Frontier War",
                Campaign = new PresetCampaignStore().Campaign,
                Files = new Dictionary<string, byte[]>(StringComparer.Ordinal)
                {
                    ["maps/border.png"] = [11, 12, 13],
                },
            },
        };
        var handler = new ImportCampaignPresetHandler(
            presets,
            maps,
            new MemoryAssetStorage(),
            new PassingImageProcessor(),
            new PassingDocumentProcessor(),
            codec,
            new FixedClock());
        var result = await handler.HandleAsync(
            new ImportCampaignPresetCommand
            {
                UserId = Guid.NewGuid(),
                IsAdministrator = true,
                Content = [1, 2, 3],
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Frontier War", result.Value.Name);
        Assert.True(result.Value.HasMap);
        Assert.Single(presets.Items);
        Assert.Equal("maps/saved.png", presets.Items[0].MapStorageKey);
        Assert.True(maps.Files.ContainsKey("maps/saved.png"));
    }

    [Fact]
    public async Task ImportRemapsCatalogLogoKeys()
    {
        var campaigns = new PresetCampaignStore();
        campaigns.AttachCatalogLogos();
        var presets = new FakePresetStore();
        var assets = new MemoryAssetStorage();
        var codec = new RecordingPackageCodec
        {
            Contents = new CampaignPresetPackageContents
            {
                Name = "Frontier War",
                Campaign = campaigns.Campaign,
                Files = new Dictionary<string, byte[]>(StringComparer.Ordinal)
                {
                    ["maps/border.png"] = [11, 12, 13],
                    ["flags/north.png"] = [1, 2, 3],
                    ["structures/town.png"] = [4, 5, 6],
                    ["items/crown.png"] = [7, 8, 9],
                },
            },
        };
        var handler = new ImportCampaignPresetHandler(
            presets,
            new MemoryMapStorage(),
            assets,
            new PassingImageProcessor(),
            new PassingDocumentProcessor(),
            codec,
            new FixedClock());
        var result = await handler.HandleAsync(
            new ImportCampaignPresetCommand
            {
                UserId = Guid.NewGuid(),
                IsAdministrator = true,
                Content = [1, 2, 3],
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var imported = Assert.Single(presets.Items);
        Assert.Equal("flags/saved.png", imported.Factions.Single(faction => faction.Name == "North").FlagImageStorageKey);
        Assert.Equal("structures/saved.png", imported.StructureTypes.Single(type => type.Name == "Town").ImageStorageKey);
        Assert.Equal("items/saved.png", imported.ItemObjectiveTypes.Single(type => type.Name == "Crown").ImageStorageKey);
        Assert.True(assets.Files.ContainsKey("flags/saved.png"));
        Assert.True(assets.Files.ContainsKey("structures/saved.png"));
        Assert.True(assets.Files.ContainsKey("items/saved.png"));
    }

    [Fact]
    public async Task ImportReprocessesStoredMapsUsingThePackageSizeLimit()
    {
        var presets = new FakePresetStore();
        var maps = new MemoryMapStorage();
        var images = new PassingImageProcessor();
        var codec = new RecordingPackageCodec
        {
            Contents = new CampaignPresetPackageContents
            {
                Name = "Frontier War",
                Campaign = new PresetCampaignStore().Campaign,
                Files = new Dictionary<string, byte[]>(StringComparer.Ordinal)
                {
                    ["maps/border.png"] = new byte[(20 * 1024 * 1024) + 1],
                },
            },
        };
        var handler = new ImportCampaignPresetHandler(
            presets,
            maps,
            new MemoryAssetStorage(),
            images,
            new PassingDocumentProcessor(),
            codec,
            new FixedClock());
        var result = await handler.HandleAsync(
            new ImportCampaignPresetCommand
            {
                UserId = Guid.NewGuid(),
                IsAdministrator = true,
                Content = [1, 2, 3],
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ImportCampaignPresetHandler.MaxPackageBytes, images.LastMaxBytes);
        Assert.Equal((20 * 1024 * 1024) + 1, images.LastLength);
        Assert.True(maps.Files.ContainsKey("maps/saved.png"));
    }

    [Fact]
    public async Task ImportRejectsAnEmptyPackage()
    {
        var handler = new ImportCampaignPresetHandler(
            new FakePresetStore(),
            new MemoryMapStorage(),
            new MemoryAssetStorage(),
            new PassingImageProcessor(),
            new PassingDocumentProcessor(),
            new RecordingPackageCodec(),
            new FixedClock());
        var result = await handler.HandleAsync(
            new ImportCampaignPresetCommand
            {
                UserId = Guid.NewGuid(),
                IsAdministrator = true,
                Content = [],
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CampaignPresetPackageInvalid, result.ErrorCode);
    }

    [Fact]
    public async Task ImportAcceptsAPackageLargerThanTheDefaultHostLimit()
    {
        var presets = new FakePresetStore();
        var maps = new MemoryMapStorage();
        var codec = new RecordingPackageCodec
        {
            Contents = new CampaignPresetPackageContents
            {
                Name = "Frontier War",
                Campaign = new PresetCampaignStore().Campaign,
                Files = new Dictionary<string, byte[]>(StringComparer.Ordinal)
                {
                    ["maps/border.png"] = [11, 12, 13],
                },
            },
        };
        var handler = new ImportCampaignPresetHandler(
            presets,
            maps,
            new MemoryAssetStorage(),
            new PassingImageProcessor(),
            new PassingDocumentProcessor(),
            codec,
            new FixedClock());
        var result = await handler.HandleAsync(
            new ImportCampaignPresetCommand
            {
                UserId = Guid.NewGuid(),
                IsAdministrator = true,
                Content = new byte[(24 * 1024 * 1024) + 1],
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Frontier War", result.Value.Name);
        Assert.Single(presets.Items);
    }
}

file sealed class FixedClock : IClock
{
    public DateTimeOffset UtcNow { get; } = new(2026, 8, 18, 0, 0, 0, TimeSpan.Zero);
}

file sealed class FakePresetStore : ICampaignPresetStore
{
    public List<StoredCampaign> Items { get; } = [];

    public Task<IReadOnlyList<CampaignPresetListItem>> ListAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<CampaignPresetListItem>>(
            [.. Items.Select(ToListItem)]);
    }

    public Task<StoredCampaign?> FindByIdAsync(Guid presetId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Items.FirstOrDefault(item => item.Id == presetId));
    }

    public Task<CampaignPresetListItem> UpsertFromCampaignAsync(
        string name,
        StoredCampaign campaign,
        Guid createdByUserId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var displayName = CampaignSetupRules.CollapseName(name);
        var key = CampaignSetupRules.UniqueNameKey(displayName);
        var existing = Items.FirstOrDefault(item => CampaignSetupRules.UniqueNameKey(item.Name) == key);
        var stored = Copy(campaign, existing?.Id ?? Guid.NewGuid(), displayName, createdByUserId, utcNow);
        if (existing is not null)
        {
            Items.Remove(existing);
        }

        Items.Add(stored);
        return Task.FromResult(ToListItem(stored));
    }

    public Task<bool> IsStorageKeyInUseAsync(
        string storageKey,
        Guid? excludingPresetId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(
            Items.Any(item => item.Id != excludingPresetId && item.MapStorageKey == storageKey));
    }

    private static CampaignPresetListItem ToListItem(StoredCampaign campaign)
    {
        return new CampaignPresetListItem
        {
            Id = campaign.Id,
            Name = campaign.Name,
            HasMap = !string.IsNullOrWhiteSpace(campaign.MapStorageKey) || campaign.MapGraph is not null,
        };
    }

    private static StoredCampaign Copy(
        StoredCampaign campaign,
        Guid id,
        string name,
        Guid createdByUserId,
        DateTimeOffset utcNow)
    {
        return new StoredCampaign
        {
            Id = id,
            Name = name,
            Description = campaign.Description,
            PlayerSlotCount = campaign.PlayerSlotCount,
            IsPrivate = false,
            IsPubliclyViewable = true,
            CreatorIsParticipant = campaign.CreatorIsParticipant,
            MapStorageKey = campaign.MapStorageKey,
            Revision = 1,
            CreatedUtc = utcNow,
            UpdatedUtc = utcNow,
            CreatedByUserId = createdByUserId,
            Memberships = [],
            Factions = campaign.Factions,
            AllyGroups = campaign.AllyGroups,
            Links = campaign.Links,
            TimeZoneId = campaign.TimeZoneId,
            StartsUtc = utcNow,
            EndsUtc = utcNow,
            RoundCount = campaign.RoundCount,
            RoundLengthAmount = campaign.RoundLengthAmount,
            RoundLengthUnit = campaign.RoundLengthUnit,
            Phases = campaign.Phases,
            MapGraph = campaign.MapGraph,
            TerrainTypes = campaign.TerrainTypes,
            StructureTypes = campaign.StructureTypes,
            ItemObjectiveTypes = campaign.ItemObjectiveTypes,
            PublicObjectiveTypes = campaign.PublicObjectiveTypes,
            SpecialRules = campaign.SpecialRules,
            ForceStatuses = campaign.ForceStatuses,
            PrivateObjectiveTypes = campaign.PrivateObjectiveTypes,
            BattleScoring = campaign.BattleScoring,
            RankingObjectivePoints = campaign.RankingObjectivePoints,
            SplitForceSupplyPenaltyPercent = campaign.SplitForceSupplyPenaltyPercent,
            SplitForceSupplyPenaltyIsPercent = campaign.SplitForceSupplyPenaltyIsPercent,
            BattleReportRules = campaign.BattleReportRules,
            ArmyEscalations = campaign.ArmyEscalations,
            Missions = campaign.Missions,
        };
    }
}

file sealed class PresetCampaignStore : ICampaignStore
{
    public StoredCampaign Campaign { get; set; } = CreateCampaign();

    public void StripMap()
    {
        Campaign = CloneCampaign(Campaign, "maps/other.png", new StoredMapGraph { Territories = [], Adjacencies = [] });
    }

    public void AttachCatalogLogos()
    {
        Campaign = CloneCampaign(
            Campaign,
            Campaign.MapStorageKey,
            Campaign.MapGraph,
            factions:
            [
                new StoredFaction
                {
                    Id = Campaign.Factions[0].Id,
                    Name = Campaign.Factions[0].Name,
                    Color = Campaign.Factions[0].Color,
                    Subfactions = Campaign.Factions[0].Subfactions,
                    RequiresSubfaction = false,
                    FlagImageStorageKey = "flags/north.png",
                    TintFlagImage = true,
                },
                Campaign.Factions[1],
            ],
            structureTypes:
            [
                new StoredStructureType
                {
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    Name = "Town",
                    BuiltinSymbol = null,
                    ImageStorageKey = "structures/town.png",
                    IsBuildable = true,
                    IsPillageable = true,
                    IsDestructible = true,
                    Missions = [],
                },
            ],
            itemObjectiveTypes:
            [
                new StoredItemObjectiveType
                {
                    Id = Guid.Parse("88888888-8888-8888-8888-888888888888"),
                    Name = "Crown",
                    IsHiddenUntilFound = true,
                    Placement = "Random",
                    AllowOnSpawn = false,
                    ImageStorageKey = "items/crown.png",
                },
            ]);
    }

    public void RetargetFactionsAndClearLogos(Guid northId)
    {
        Campaign = CloneCampaign(
            Campaign,
            Campaign.MapStorageKey,
            Campaign.MapGraph,
            factions:
            [
                new StoredFaction
                {
                    Id = northId,
                    Name = "North",
                    Color = "#2563EB",
                    Subfactions = [],
                    RequiresSubfaction = false,
                },
                new StoredFaction
                {
                    Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                    Name = "South",
                    Color = "#DC2626",
                    Subfactions = [],
                    RequiresSubfaction = false,
                },
            ],
            structureTypes: [],
            itemObjectiveTypes: []);
    }

    public void RetargetPlains(Guid terrainId)
    {
        Campaign = CloneCampaign(
            Campaign,
            "maps/other.png",
            new StoredMapGraph { Territories = [], Adjacencies = [] },
            terrainTypes:
            [
                new StoredTerrainType
                {
                    Id = terrainId,
                    Name = "Plains",
                    Color = "#7CB342",
                    Missions = [],
                },
            ]);
    }

    public Task<StoredCampaign> AddAsync(StoredCampaign campaign, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task<StoredCampaign?> FindByIdAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        return Task.FromResult(campaignId == Campaign.Id ? Campaign : null);
    }

    public Task<IReadOnlyList<StoredCampaign>> ListForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<StoredCampaign>>([Campaign]);
    }

    public Task<IReadOnlyList<StoredCampaign>> ListDiscoverableAsync(
        Guid userId,
        bool isAdministrator,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<StoredCampaign>>([Campaign]);
    }

    public Task<UpdateStoredCampaignOutcome> UpdateAsync(
        StoredCampaign campaign,
        int expectedRevision,
        CancellationToken cancellationToken)
    {
        if (campaign.Id != Campaign.Id || expectedRevision != Campaign.Revision)
        {
            return Task.FromResult(new UpdateStoredCampaignOutcome
            {
                IsSuccess = false,
                ErrorCode = ErrorCodes.ConcurrencyConflict,
                Message = "The campaign was changed by another request. Reload and try again.",
            });
        }

        Campaign = CloneCampaign(campaign, campaign.MapStorageKey, campaign.MapGraph, Campaign.Revision + 1);
        return Task.FromResult(new UpdateStoredCampaignOutcome { IsSuccess = true, Campaign = Campaign });
    }

    private static StoredCampaign CreateCampaign()
    {
        return new StoredCampaign
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "Border War",
            PlayerSlotCount = 8,
            IsPrivate = false,
            IsPubliclyViewable = true,
            CreatorIsParticipant = true,
            MapStorageKey = "maps/border.png",
            Revision = 2,
            CreatedUtc = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero),
            UpdatedUtc = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero),
            CreatedByUserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Memberships = [],
            Factions =
            [
                new StoredFaction
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Name = "North",
                    Color = "#2563EB",
                    Subfactions = [],
                    RequiresSubfaction = false,
                },
                new StoredFaction
                {
                    Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Name = "South",
                    Color = "#DC2626",
                    Subfactions = [],
                    RequiresSubfaction = false,
                },
            ],
            AllyGroups = [],
            Links = [],
            TimeZoneId = "UTC",
            StartsUtc = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero),
            EndsUtc = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero),
            RoundCount = 8,
            RoundLengthAmount = 1,
            RoundLengthUnit = "Weeks",
            Phases =
            [
                new StoredRoundPhase { Kind = "Action", DurationAmount = 3, DurationUnit = "Days" },
                new StoredRoundPhase { Kind = "Action", DurationAmount = 3, DurationUnit = "Days" },
                new StoredRoundPhase { Kind = "Battle", DurationAmount = 1, DurationUnit = "Days" },
            ],
            MapGraph = SampleGraph(),
            TerrainTypes =
            [
                new StoredTerrainType
                {
                    Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                    Name = "Plains",
                    Color = "#7CB342",
                    Missions = [],
                },
            ],
            StructureTypes = [],
            SplitForceSupplyPenaltyPercent = HuntInEstaliaDefaults.SplitForceSupplyPenaltyValue,
            SplitForceSupplyPenaltyIsPercent = HuntInEstaliaDefaults.SplitForceSupplyPenaltyIsPercent,
            BattleReportRules = BattleReportRulesSetup.Default,
            ArmyEscalations = HuntInEstaliaDefaults.ArmyEscalations(8),
        };
    }

    private static StoredMapGraph SampleGraph()
    {
        return new StoredMapGraph
        {
            Territories =
            [
                new TerritoryDetail
                {
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    DisplayNumber = 1,
                    Name = "Northmarch",
                    Polygon =
                    [
                        new MapPointDetail { X = 0.1, Y = 0.1 },
                        new MapPointDetail { X = 0.3, Y = 0.1 },
                        new MapPointDetail { X = 0.3, Y = 0.3 },
                        new MapPointDetail { X = 0.1, Y = 0.3 },
                    ],
                    TerrainTypeId = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                },
            ],
            Adjacencies = [],
        };
    }

    private static StoredCampaign CloneCampaign(
        StoredCampaign campaign,
        string? mapStorageKey,
        StoredMapGraph? mapGraph,
        int? revision = null,
        IReadOnlyList<StoredTerrainType>? terrainTypes = null,
        IReadOnlyList<StoredFaction>? factions = null,
        IReadOnlyList<StoredStructureType>? structureTypes = null,
        IReadOnlyList<StoredItemObjectiveType>? itemObjectiveTypes = null)
    {
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
            MapStorageKey = mapStorageKey,
            Revision = revision ?? campaign.Revision,
            CreatedUtc = campaign.CreatedUtc,
            UpdatedUtc = campaign.UpdatedUtc,
            CreatedByUserId = campaign.CreatedByUserId,
            Memberships = campaign.Memberships,
            Factions = factions ?? campaign.Factions,
            AllyGroups = campaign.AllyGroups,
            Links = campaign.Links,
            TimeZoneId = campaign.TimeZoneId,
            StartsUtc = campaign.StartsUtc,
            EndsUtc = campaign.EndsUtc,
            RoundCount = campaign.RoundCount,
            RoundLengthAmount = campaign.RoundLengthAmount,
            RoundLengthUnit = campaign.RoundLengthUnit,
            Phases = campaign.Phases,
            MapGraph = mapGraph,
            PlayState = campaign.PlayState,
            TerrainTypes = terrainTypes ?? campaign.TerrainTypes,
            StructureTypes = structureTypes ?? campaign.StructureTypes,
            ItemObjectiveTypes = itemObjectiveTypes ?? campaign.ItemObjectiveTypes,
            PublicObjectiveTypes = campaign.PublicObjectiveTypes,
            SpecialRules = campaign.SpecialRules,
            Missions = campaign.Missions,
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

    public Task<bool> DeleteAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task<bool> IsStorageKeyInUseAsync(
        string storageKey,
        Guid? excludingCampaignId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }

    public Task<UpdateStoredCampaignOutcome> UpdateMapGraphAsync(
        Guid campaignId,
        StoredMapGraph graph,
        int expectedRevision,
        DateTimeOffset updatedUtc,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task<UpdateStoredCampaignOutcome> UpdatePlayStateAsync(
        Guid campaignId,
        CampaignPlayState playState,
        StoredMapGraph? mapGraph,
        DateTimeOffset endsUtc,
        int roundCount,
        int expectedRevision,
        DateTimeOffset updatedUtc,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }
}

file sealed class MemoryMapStorage : ICampaignMapStorage
{
    public Dictionary<string, byte[]> Files { get; } = new(StringComparer.Ordinal);

    public Task<string> SaveAsync(ReadOnlyMemory<byte> content, string fileExtension, CancellationToken cancellationToken)
    {
        var key = $"maps/saved{fileExtension}";
        Files[key] = content.ToArray();
        return Task.FromResult(key);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        Files.Remove(storageKey);
        return Task.CompletedTask;
    }

    public Task<StoredCampaignMap?> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        return Task.FromResult(
            Files.TryGetValue(storageKey, out var bytes) ? new StoredCampaignMap(bytes, "image/png") : null);
    }
}

file sealed class MemoryAssetStorage : ICampaignAssetStorage
{
    public Dictionary<string, byte[]> Files { get; } = new(StringComparer.Ordinal);

    public Task<string> SaveAsync(
        string folder,
        ReadOnlyMemory<byte> content,
        string fileExtension,
        string contentType,
        CancellationToken cancellationToken)
    {
        var key = $"{folder}/saved{fileExtension}";
        Files[key] = content.ToArray();
        return Task.FromResult(key);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        Files.Remove(storageKey);
        return Task.CompletedTask;
    }

    public Task<StoredCampaignAsset?> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        return Task.FromResult(
            Files.TryGetValue(storageKey, out var bytes)
                ? new StoredCampaignAsset(bytes, "application/octet-stream")
                : null);
    }
}

file sealed class PassingImageProcessor : ICampaignMapProcessor
{
    public long LastMaxBytes { get; private set; } = ICampaignMapProcessor.MaxUploadBytes;

    public long? LastLength { get; private set; }

    public async Task<ProcessedCampaignMapResult> ProcessAsync(
        Stream content,
        string contentType,
        long? length,
        CancellationToken cancellationToken,
        int maxDimension = ICampaignMapProcessor.MapMaxDimension,
        long maxBytes = ICampaignMapProcessor.MaxUploadBytes)
    {
        LastMaxBytes = maxBytes;
        LastLength = length;
        using var copy = new MemoryStream();
        await content.CopyToAsync(copy, cancellationToken).ConfigureAwait(false);
        return new ProcessedCampaignMapResult
        {
            IsSuccess = true,
            Content = copy.ToArray(),
            FileExtension = ".png",
        };
    }
}

file sealed class PassingDocumentProcessor : ICampaignDocumentProcessor
{
    public Task<ProcessedCampaignDocumentResult> ProcessAsync(
        Stream content,
        string contentType,
        string fileName,
        long? length,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new ProcessedCampaignDocumentResult
        {
            IsSuccess = true,
            Content = [1],
            FileExtension = ".pdf",
            ContentType = "application/pdf",
            FileName = fileName,
        });
    }
}

file sealed class RecordingPackageCodec : ICampaignPresetPackageCodec
{
    public StoredCampaign? WrittenCampaign { get; private set; }

    public IReadOnlyDictionary<string, byte[]> WrittenFiles { get; private set; } =
        new Dictionary<string, byte[]>(StringComparer.Ordinal);

    public CampaignPresetPackageContents? Contents { get; set; }

    public byte[] Write(StoredCampaign campaign, IReadOnlyDictionary<string, byte[]> files)
    {
        WrittenCampaign = campaign;
        WrittenFiles = files;
        return [1, 2, 3];
    }

    public OperationResult<CampaignPresetPackageContents> Read(ReadOnlyMemory<byte> content)
    {
        if (Contents is null)
        {
            return OperationResults.Failure<CampaignPresetPackageContents>(
                ErrorCodes.CampaignPresetPackageInvalid,
                "The campaign preset file is not a valid Map & Muster package.");
        }

        return OperationResults.Success(Contents);
    }
}
