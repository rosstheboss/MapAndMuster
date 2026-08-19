using Campaign.Application.Campaigns;
using Campaign.Application.Ports;
using Campaign.Infrastructure.Persistence;
using Campaign.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Campaign.Infrastructure.Campaigns;

/// <summary>
/// EF Core persistence for named campaign presets.
/// </summary>
public sealed class CampaignPresetStore : ICampaignPresetStore
{
    private readonly CampaignDbContext _dbContext;

    /// <summary>
    /// Initializes a store.
    /// </summary>
    public CampaignPresetStore(CampaignDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CampaignPresetListItem>> ListAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.CampaignPresets
            .AsNoTracking()
            .OrderBy(preset => preset.Name)
            .Select(preset => new CampaignPresetListItem
            {
                Id = preset.Id,
                Name = preset.Name,
                HasMap = preset.MapStorageKey != null || preset.MapGraphJson != null,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<StoredCampaign?> FindByIdAsync(Guid presetId, CancellationToken cancellationToken)
    {
        var record = await _dbContext.CampaignPresets
            .AsNoTracking()
            .FirstOrDefaultAsync(preset => preset.Id == presetId, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToStored(record);
    }

    /// <inheritdoc />
    public async Task<CampaignPresetListItem> UpsertFromCampaignAsync(
        string name,
        StoredCampaign campaign,
        Guid createdByUserId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(campaign);

        var normalized = name.Trim().ToUpperInvariant();
        var record = await _dbContext.CampaignPresets
            .FirstOrDefaultAsync(preset => preset.NormalizedName == normalized, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            record = new CampaignPresetRecord
            {
                Id = Guid.NewGuid(),
                CreatedUtc = utcNow,
            };
            _dbContext.CampaignPresets.Add(record);
        }

        record.Name = name.Trim();
        record.NormalizedName = normalized;
        record.CatalogJson = CatalogJson.Serialize(
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
            campaign.BattleReportRules,
            campaign.ArmyEscalations,
            campaign.Missions);
        record.SettingsJson = CampaignPresetSettingsJson.Serialize(campaign);
        record.MapGraphJson = campaign.MapGraph is null ? null : MapGraphJson.Serialize(campaign.MapGraph);
        record.MapStorageKey = campaign.MapStorageKey;
        record.UpdatedUtc = utcNow;
        record.CreatedByUserId = createdByUserId;
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new CampaignPresetListItem
        {
            Id = record.Id,
            Name = record.Name,
            HasMap = record.MapStorageKey != null || record.MapGraphJson != null,
        };
    }

    /// <inheritdoc />
    public async Task<bool> IsStorageKeyInUseAsync(
        string storageKey,
        Guid? excludingPresetId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        var query = _dbContext.CampaignPresets.AsNoTracking();
        if (excludingPresetId is { } excluded)
        {
            query = query.Where(preset => preset.Id != excluded);
        }

        return await query
            .AnyAsync(
                preset => preset.MapStorageKey == storageKey
                    || (preset.CatalogJson != null && preset.CatalogJson.Contains(storageKey))
                    || (preset.SettingsJson != null && preset.SettingsJson.Contains(storageKey)),
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static StoredCampaign ToStored(CampaignPresetRecord record)
    {
        var (TerrainTypes, StructureTypes, ItemObjectiveTypes, PublicObjectiveTypes, BattleScoring, RankingObjectivePoints, SpecialRules, PrivateObjectiveTypes, _, ForceStatuses, SplitForceSupplyPenaltyPercent, BattleReportRules, ArmyEscalations, Missions) = CatalogJson.Deserialize(record.CatalogJson);
        var settings = CampaignPresetSettingsJson.Deserialize(record.SettingsJson);
        var created = record.CreatedUtc;
        return new StoredCampaign
        {
            Id = record.Id,
            Name = record.Name,
            Description = settings.Description,
            PlayerSlotCount = Math.Max(2, settings.PlayerSlotCount),
            IsPrivate = false,
            IsPubliclyViewable = true,
            CreatorIsParticipant = settings.CreatorIsParticipant,
            MapStorageKey = record.MapStorageKey,
            Revision = 1,
            CreatedUtc = created,
            UpdatedUtc = record.UpdatedUtc,
            CreatedByUserId = record.CreatedByUserId,
            Memberships = [],
            Factions = settings.Factions,
            AllyGroups = settings.AllyGroups,
            Links = settings.Links,
            TimeZoneId = string.IsNullOrWhiteSpace(settings.TimeZoneId) ? "UTC" : settings.TimeZoneId,
            StartsUtc = created,
            EndsUtc = created,
            RoundCount = Math.Max(3, settings.RoundCount),
            RoundLengthAmount = Math.Max(1, settings.RoundLengthAmount),
            RoundLengthUnit = string.IsNullOrWhiteSpace(settings.RoundLengthUnit) ? "Weeks" : settings.RoundLengthUnit,
            Phases = settings.Phases,
            MapGraph = MapGraphJson.Deserialize(record.MapGraphJson),
            TerrainTypes = TerrainTypes,
            StructureTypes = StructureTypes,
            ItemObjectiveTypes = ItemObjectiveTypes,
            PublicObjectiveTypes = PublicObjectiveTypes,
            SpecialRules = SpecialRules,
            ForceStatuses = ForceStatuses,
            PrivateObjectiveTypes = PrivateObjectiveTypes,
            BattleScoring = BattleScoring,
            RankingObjectivePoints = RankingObjectivePoints,
            SplitForceSupplyPenaltyPercent = SplitForceSupplyPenaltyPercent,
            BattleReportRules = BattleReportRules,
            ArmyEscalations = ArmyEscalations,
            Missions = Missions,
        };
    }
}
