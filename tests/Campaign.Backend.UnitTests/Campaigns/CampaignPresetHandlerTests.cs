using Campaign.Application.Campaigns;
using Campaign.Application.Common;
using Campaign.Application.Maps;
using Campaign.Application.Ports;
using Campaign.Domain.Campaigns;
using Campaign.Domain.Play;

namespace Campaign.Backend.UnitTests.Campaigns;

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
        var existing = Items.FirstOrDefault(item =>
            string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
        var stored = Copy(campaign, existing?.Id ?? Guid.NewGuid(), name.Trim(), createdByUserId, utcNow);
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
            BattleReportRules = campaign.BattleReportRules,
            ArmyEscalations = campaign.ArmyEscalations,
        };
    }
}

file sealed class PresetCampaignStore : ICampaignStore
{
    public StoredCampaign Campaign { get; } = new()
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
        MapGraph = new StoredMapGraph { Territories = [], Adjacencies = [] },
        TerrainTypes = [],
        StructureTypes = [],
        SplitForceSupplyPenaltyPercent = HuntInEstaliaDefaults.SplitForceSupplyPenaltyPercent,
        BattleReportRules = BattleReportRulesSetup.Default,
        ArmyEscalations = HuntInEstaliaDefaults.ArmyEscalations(8),
    };

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
        throw new NotSupportedException();
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
