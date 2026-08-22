using MapAndMuster.Application.Campaigns;
using MapAndMuster.Application.Common;
using MapAndMuster.Application.Maps;
using MapAndMuster.Application.Ports;
using MapAndMuster.Infrastructure.Persistence;
using MapAndMuster.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace MapAndMuster.Infrastructure.Campaigns;

/// <summary>
/// EF Core persistence for campaigns and memberships.
/// </summary>
public sealed class CampaignStore : ICampaignStore
{
    private readonly CampaignDbContext _dbContext;

    /// <summary>
    /// Initializes a new store.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public CampaignStore(CampaignDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<StoredCampaign> AddAsync(StoredCampaign campaign, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        var record = ToRecord(campaign);
        _dbContext.Campaigns.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _dbContext.ChangeTracker.Clear();
        var stored = await FindByIdAsync(record.Id, cancellationToken).ConfigureAwait(false);
        return stored ?? throw new InvalidOperationException("The campaign was not found after it was created.");
    }

    /// <inheritdoc />
    public async Task<StoredCampaign?> FindByIdAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        var record = await QueryCampaigns()
            .AsNoTracking()
            .FirstOrDefaultAsync(campaign => campaign.Id == campaignId, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToStored(record);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StoredCampaign>> ListForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var records = await QueryCampaigns()
            .AsNoTracking()
            .Where(campaign => campaign.Memberships.Any(membership => membership.UserId == userId))
            .OrderByDescending(campaign => campaign.UpdatedUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return [.. records.Select(ToStored)];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StoredCampaign>> ListDiscoverableAsync(
        Guid userId,
        bool isAdministrator,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var query = QueryCampaigns().AsNoTracking();
        if (!isAdministrator)
        {
            query = query.Where(campaign =>
                campaign.IsPubliclyViewable
                || campaign.StartsUtc > utcNow
                || campaign.Memberships.Any(membership => membership.UserId == userId));
        }

        var records = await query
            .OrderByDescending(campaign => campaign.UpdatedUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return [.. records.Select(ToStored)];
    }

    /// <inheritdoc />
    public async Task<UpdateStoredCampaignOutcome> UpdateAsync(
        StoredCampaign campaign,
        int expectedRevision,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(campaign);

        var exists = await _dbContext.Campaigns
            .AnyAsync(item => item.Id == campaign.Id, cancellationToken)
            .ConfigureAwait(false);
        if (!exists)
        {
            return new UpdateStoredCampaignOutcome
            {
                IsSuccess = false,
                ErrorCode = ErrorCodes.CampaignNotFound,
                Message = "The campaign was not found.",
            };
        }

        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        // Apply scalars and replace children in SQL. Graph-replace through the change tracker
        // raises false concurrency conflicts on faction rows.
        var catalogJson = CatalogJson.Serialize(
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
            campaign.BattleReportRules,
            campaign.ArmyEscalations,
            campaign.Missions,
            campaign.Factions.ToDictionary(static faction => faction.Id, static faction => faction.SubfactionSpecialRules));
        var playJson = PlayStateJson.Serialize(campaign.PlayState);
        var mapGraphJson = campaign.MapGraph is null ? null : MapGraphJson.Serialize(campaign.MapGraph);
        var affected = await _dbContext.Campaigns
            .Where(item => item.Id == campaign.Id && item.Revision == expectedRevision)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(item => item.Name, campaign.Name)
                    .SetProperty(item => item.Description, campaign.Description)
                    .SetProperty(item => item.PlayerSlotCount, campaign.PlayerSlotCount)
                    .SetProperty(item => item.IsPrivate, campaign.IsPrivate)
                    .SetProperty(item => item.IsPubliclyViewable, campaign.IsPubliclyViewable)
                    .SetProperty(item => item.JoinPasswordHash, campaign.JoinPasswordHash)
                    .SetProperty(item => item.CreatorIsParticipant, campaign.CreatorIsParticipant)
                    .SetProperty(item => item.City, campaign.City)
                    .SetProperty(item => item.Region, campaign.Region)
                    .SetProperty(item => item.Country, campaign.Country)
                    .SetProperty(item => item.MapStorageKey, campaign.MapStorageKey)
                    .SetProperty(item => item.MapGraphJson, mapGraphJson)
                    .SetProperty(item => item.CatalogJson, catalogJson)
                    .SetProperty(item => item.PlayStateJson, playJson)
                    .SetProperty(item => item.TimeZoneId, campaign.TimeZoneId)
                    .SetProperty(item => item.StartsUtc, campaign.StartsUtc)
                    .SetProperty(item => item.EndsUtc, campaign.EndsUtc)
                    .SetProperty(item => item.RoundCount, campaign.RoundCount)
                    .SetProperty(item => item.RoundLengthAmount, campaign.RoundLengthAmount)
                    .SetProperty(item => item.RoundLengthUnit, campaign.RoundLengthUnit)
                    .SetProperty(item => item.UpdatedUtc, campaign.UpdatedUtc)
                    .SetProperty(item => item.Revision, expectedRevision + 1),
                cancellationToken)
            .ConfigureAwait(false);
        if (affected == 0)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return new UpdateStoredCampaignOutcome
            {
                IsSuccess = false,
                ErrorCode = ErrorCodes.ConcurrencyConflict,
                Message = "The campaign was changed by another request. Reload and try again.",
            };
        }

        await _dbContext.Set<CampaignFactionRecord>()
            .Where(faction => faction.CampaignId == campaign.Id)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await _dbContext.Set<CampaignAllyGroupRecord>()
            .Where(group => group.CampaignId == campaign.Id)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await _dbContext.Set<CampaignLinkRecord>()
            .Where(link => link.CampaignId == campaign.Id)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await _dbContext.Set<CampaignMembershipRecord>()
            .Where(membership => membership.CampaignId == campaign.Id)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await _dbContext.Set<CampaignRoundPhaseRecord>()
            .Where(phase => phase.CampaignId == campaign.Id)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        _dbContext.ChangeTracker.Clear();
        var record = await _dbContext.Campaigns
            .FirstAsync(item => item.Id == campaign.Id, cancellationToken)
            .ConfigureAwait(false);
        AddChildren(record, campaign);
        MarkGraphAdded(record);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return new UpdateStoredCampaignOutcome
            {
                IsSuccess = false,
                ErrorCode = ErrorCodes.ConcurrencyConflict,
                Message = "The campaign was changed by another request. Reload and try again.",
            };
        }

        _dbContext.ChangeTracker.Clear();
        var stored = await FindByIdAsync(campaign.Id, cancellationToken).ConfigureAwait(false);
        return new UpdateStoredCampaignOutcome
        {
            IsSuccess = true,
            Campaign = stored ?? throw new InvalidOperationException("The campaign was not found after it was updated."),
        };
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        var record = await _dbContext.Campaigns.FirstOrDefaultAsync(campaign => campaign.Id == campaignId, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return false;
        }

        _dbContext.Campaigns.Remove(record);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> IsStorageKeyInUseAsync(
        string storageKey,
        Guid? excludingCampaignId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        var query = _dbContext.Campaigns.AsNoTracking();
        if (excludingCampaignId is { } excluded)
        {
            query = query.Where(campaign => campaign.Id != excluded);
        }

        return await query
            .AnyAsync(
                campaign => campaign.MapStorageKey == storageKey
                    || campaign.Factions.Any(faction => faction.FlagImageStorageKey == storageKey)
                    || (campaign.CatalogJson != null && campaign.CatalogJson.Contains(storageKey)),
                cancellationToken)
            .ConfigureAwait(false)
            || await _dbContext.CampaignPresets
                .AsNoTracking()
                .AnyAsync(
                    preset => preset.MapStorageKey == storageKey
                        || (preset.CatalogJson != null && preset.CatalogJson.Contains(storageKey))
                        || (preset.SettingsJson != null && preset.SettingsJson.Contains(storageKey)),
                    cancellationToken)
                .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<UpdateStoredCampaignOutcome> UpdateMapGraphAsync(
        Guid campaignId,
        StoredMapGraph graph,
        int expectedRevision,
        DateTimeOffset updatedUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(graph);

        var exists = await _dbContext.Campaigns
            .AnyAsync(item => item.Id == campaignId, cancellationToken)
            .ConfigureAwait(false);
        if (!exists)
        {
            return new UpdateStoredCampaignOutcome
            {
                IsSuccess = false,
                ErrorCode = ErrorCodes.CampaignNotFound,
                Message = "The campaign was not found.",
            };
        }

        var json = MapGraphJson.Serialize(graph);
        var affected = await _dbContext.Campaigns
            .Where(item => item.Id == campaignId && item.Revision == expectedRevision)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(item => item.MapGraphJson, json)
                    .SetProperty(item => item.UpdatedUtc, updatedUtc)
                    .SetProperty(item => item.Revision, expectedRevision + 1),
                cancellationToken)
            .ConfigureAwait(false);
        if (affected == 0)
        {
            return new UpdateStoredCampaignOutcome
            {
                IsSuccess = false,
                ErrorCode = ErrorCodes.ConcurrencyConflict,
                Message = "The campaign was changed by another request. Reload and try again.",
            };
        }

        var stored = await FindByIdAsync(campaignId, cancellationToken).ConfigureAwait(false);
        return new UpdateStoredCampaignOutcome
        {
            IsSuccess = true,
            Campaign = stored ?? throw new InvalidOperationException("The campaign was not found after the map graph was saved."),
        };
    }

    /// <inheritdoc />
    public async Task<UpdateStoredCampaignOutcome> UpdatePlayStateAsync(
        Guid campaignId,
        MapAndMuster.Domain.Play.CampaignPlayState playState,
        StoredMapGraph? mapGraph,
        DateTimeOffset endsUtc,
        int roundCount,
        int expectedRevision,
        DateTimeOffset updatedUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(playState);

        var exists = await _dbContext.Campaigns
            .AnyAsync(item => item.Id == campaignId, cancellationToken)
            .ConfigureAwait(false);
        if (!exists)
        {
            return new UpdateStoredCampaignOutcome
            {
                IsSuccess = false,
                ErrorCode = ErrorCodes.CampaignNotFound,
                Message = "The campaign was not found.",
            };
        }

        var playJson = PlayStateJson.Serialize(playState);
        var affected = 0;
        if (mapGraph is null)
        {
            affected = await _dbContext.Campaigns
                .Where(item => item.Id == campaignId && item.Revision == expectedRevision)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(item => item.PlayStateJson, playJson)
                        .SetProperty(item => item.EndsUtc, endsUtc)
                        .SetProperty(item => item.RoundCount, roundCount)
                        .SetProperty(item => item.UpdatedUtc, updatedUtc)
                        .SetProperty(item => item.Revision, expectedRevision + 1),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            var mapJson = MapGraphJson.Serialize(mapGraph);
            affected = await _dbContext.Campaigns
                .Where(item => item.Id == campaignId && item.Revision == expectedRevision)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(item => item.PlayStateJson, playJson)
                        .SetProperty(item => item.MapGraphJson, mapJson)
                        .SetProperty(item => item.EndsUtc, endsUtc)
                        .SetProperty(item => item.RoundCount, roundCount)
                        .SetProperty(item => item.UpdatedUtc, updatedUtc)
                        .SetProperty(item => item.Revision, expectedRevision + 1),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        if (affected == 0)
        {
            return new UpdateStoredCampaignOutcome
            {
                IsSuccess = false,
                ErrorCode = ErrorCodes.ConcurrencyConflict,
                Message = "The campaign was changed by another request. Reload and try again.",
            };
        }

        var stored = await FindByIdAsync(campaignId, cancellationToken).ConfigureAwait(false);
        return new UpdateStoredCampaignOutcome
        {
            IsSuccess = true,
            Campaign = stored ?? throw new InvalidOperationException("The campaign was not found after play state was saved."),
        };
    }

    private IQueryable<CampaignRecord> QueryCampaigns()
    {
        return _dbContext.Campaigns
            .Include(campaign => campaign.Memberships)
            .Include(campaign => campaign.AllyGroups)
            .Include(campaign => campaign.Factions)
                .ThenInclude(faction => faction.Subfactions)
            .Include(campaign => campaign.Factions)
                .ThenInclude(faction => faction.AllyGroup)
            .Include(campaign => campaign.Links)
            .Include(campaign => campaign.Phases);
    }

    private static CampaignRecord ToRecord(StoredCampaign campaign)
    {
        var record = new CampaignRecord
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
            MapGraphJson = campaign.MapGraph is null ? null : MapGraphJson.Serialize(campaign.MapGraph),
            PlayStateJson = PlayStateJson.Serialize(campaign.PlayState),
            CatalogJson = CatalogJson.Serialize(
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
                campaign.BattleReportRules,
                campaign.ArmyEscalations,
                campaign.Missions,
                campaign.Factions.ToDictionary(static faction => faction.Id, static faction => faction.SubfactionSpecialRules)),
            Revision = campaign.Revision,
            CreatedUtc = campaign.CreatedUtc,
            UpdatedUtc = campaign.UpdatedUtc,
            CreatedByUserId = campaign.CreatedByUserId,
            TimeZoneId = campaign.TimeZoneId,
            StartsUtc = campaign.StartsUtc,
            EndsUtc = campaign.EndsUtc,
            RoundCount = campaign.RoundCount,
            RoundLengthAmount = campaign.RoundLengthAmount,
            RoundLengthUnit = campaign.RoundLengthUnit,
        };
        AddChildren(record, campaign);
        return record;
    }

    /// <summary>
    /// Marks replacement child rows as inserted. EF Core otherwise treats them as existing
    /// and issues UPDATEs after the previous rows were deleted.
    /// </summary>
    private void MarkGraphAdded(CampaignRecord record)
    {
        foreach (var group in record.AllyGroups)
        {
            _dbContext.Entry(group).State = EntityState.Added;
        }

        foreach (var faction in record.Factions)
        {
            _dbContext.Entry(faction).State = EntityState.Added;
            foreach (var subfaction in faction.Subfactions)
            {
                _dbContext.Entry(subfaction).State = EntityState.Added;
            }
        }

        foreach (var link in record.Links)
        {
            _dbContext.Entry(link).State = EntityState.Added;
        }

        foreach (var membership in record.Memberships)
        {
            _dbContext.Entry(membership).State = EntityState.Added;
        }

        foreach (var phase in record.Phases)
        {
            _dbContext.Entry(phase).State = EntityState.Added;
        }
    }

    private static void AddChildren(CampaignRecord record, StoredCampaign campaign)
    {
        var groupOrder = 0;
        var groupsByName = new Dictionary<string, CampaignAllyGroupRecord>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in campaign.AllyGroups)
        {
            var groupRecord = new CampaignAllyGroupRecord
            {
                Id = group.Id,
                CampaignId = record.Id,
                Name = group.Name,
                Color = group.Color,
                SortOrder = groupOrder++,
            };
            record.AllyGroups.Add(groupRecord);
            groupsByName[group.Name] = groupRecord;
        }

        var factionOrder = 0;
        foreach (var faction in campaign.Factions)
        {
            CampaignAllyGroupRecord? allyGroup = null;
            if (faction.AllyGroupName is not null)
            {
                groupsByName.TryGetValue(faction.AllyGroupName, out allyGroup);
            }

            var factionRecord = new CampaignFactionRecord
            {
                Id = faction.Id,
                CampaignId = record.Id,
                Name = faction.Name,
                Color = faction.Color,
                RequiresSubfaction = faction.RequiresSubfaction,
                FlagImageStorageKey = faction.FlagImageStorageKey,
                AllyGroup = allyGroup,
                SortOrder = factionOrder++,
            };
            var subOrder = 0;
            foreach (var subfaction in faction.Subfactions)
            {
                factionRecord.Subfactions.Add(new CampaignSubfactionRecord
                {
                    Name = subfaction,
                    SortOrder = subOrder++,
                });
            }

            record.Factions.Add(factionRecord);
        }

        var linkOrder = 0;
        foreach (var link in campaign.Links)
        {
            record.Links.Add(new CampaignLinkRecord
            {
                CampaignId = record.Id,
                Label = link.Label,
                Url = link.Url,
                SortOrder = linkOrder++,
            });
        }

        foreach (var membership in campaign.Memberships)
        {
            record.Memberships.Add(new CampaignMembershipRecord
            {
                CampaignId = record.Id,
                UserId = membership.UserId,
                IsGameMaster = membership.IsGameMaster,
                IsPlayer = membership.IsPlayer,
                FactionId = membership.FactionId,
                Subfaction = membership.Subfaction,
            });
        }

        var phaseOrder = 0;
        foreach (var phase in campaign.Phases)
        {
            record.Phases.Add(new CampaignRoundPhaseRecord
            {
                CampaignId = record.Id,
                Kind = phase.Kind,
                DurationAmount = phase.DurationAmount,
                DurationUnit = phase.DurationUnit,
                EndPhaseEarlyIfAble = phase.EndPhaseEarlyIfAble,
                SortOrder = phaseOrder++,
            });
        }
    }

    private static StoredCampaign ToStored(CampaignRecord record)
    {
        var (TerrainTypes, StructureTypes, ItemObjectiveTypes, PublicObjectiveTypes, BattleScoring, RankingObjectivePoints, SpecialRules, PrivateObjectiveTypes, FactionSpecialRuleIds, SubfactionSpecialRuleIds, ForceStatuses, SplitForceSupplyPenaltyPercent, SplitForceSupplyPenaltyIsPercent, BattleReportRules, ArmyEscalations, Missions) = CatalogJson.Deserialize(record.CatalogJson);
        return new StoredCampaign
        {
            Id = record.Id,
            Name = record.Name,
            Description = record.Description,
            PlayerSlotCount = record.PlayerSlotCount,
            IsPrivate = record.IsPrivate,
            IsPubliclyViewable = record.IsPubliclyViewable,
            JoinPasswordHash = record.JoinPasswordHash,
            CreatorIsParticipant = record.CreatorIsParticipant,
            City = record.City,
            Region = record.Region,
            Country = record.Country,
            MapStorageKey = record.MapStorageKey,
            Revision = record.Revision,
            CreatedUtc = record.CreatedUtc,
            UpdatedUtc = record.UpdatedUtc,
            CreatedByUserId = record.CreatedByUserId,
            MapGraph = MapGraphJson.Deserialize(record.MapGraphJson),
            PlayState = PlayStateJson.Deserialize(record.PlayStateJson),
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
            SplitForceSupplyPenaltyIsPercent = SplitForceSupplyPenaltyIsPercent,
            BattleReportRules = BattleReportRules,
            ArmyEscalations = ArmyEscalations,
            Missions = Missions,
            Memberships =
            [
                .. record.Memberships.Select(membership => new StoredCampaignMembership
                {
                    UserId = membership.UserId,
                    IsGameMaster = membership.IsGameMaster,
                    IsPlayer = membership.IsPlayer,
                    FactionId = membership.FactionId,
                    Subfaction = membership.Subfaction,
                }),
            ],
            AllyGroups =
            [
                .. record.AllyGroups
                    .OrderBy(group => group.SortOrder)
                    .Select(group => new StoredAllyGroup
                    {
                        Id = group.Id,
                        Name = group.Name,
                        Color = string.IsNullOrWhiteSpace(group.Color) ? "#4B5563" : group.Color,
                    }),
            ],
            Factions =
            [
                .. record.Factions
                    .OrderBy(faction => faction.SortOrder)
                    .Select(faction => new StoredFaction
                    {
                        Id = faction.Id,
                        Name = faction.Name,
                        Color = faction.Color,
                        RequiresSubfaction = faction.RequiresSubfaction,
                        Subfactions =
                        [
                            .. faction.Subfactions
                                .OrderBy(subfaction => subfaction.SortOrder)
                                .Select(subfaction => subfaction.Name),
                        ],
                        AllyGroupName = faction.AllyGroup?.Name,
                        FlagImageStorageKey = faction.FlagImageStorageKey,
                        SpecialRuleIds = FactionSpecialRuleIds.GetValueOrDefault(faction.Id) ?? [],
                        SubfactionSpecialRules = SubfactionSpecialRuleIds.GetValueOrDefault(faction.Id) ?? [],
                    }),
            ],
            Links =
            [
                .. record.Links
                    .OrderBy(link => link.SortOrder)
                    .Select(link => new StoredCampaignLink
                    {
                        Id = link.Id,
                        Label = link.Label,
                        Url = link.Url,
                    }),
            ],
            TimeZoneId = record.TimeZoneId,
            StartsUtc = record.StartsUtc,
            EndsUtc = record.EndsUtc,
            RoundCount = record.RoundCount,
            RoundLengthAmount = record.RoundLengthAmount,
            RoundLengthUnit = record.RoundLengthUnit,
            Phases =
            [
                .. record.Phases
                    .OrderBy(phase => phase.SortOrder)
                    .Select(phase => new StoredRoundPhase
                    {
                        Kind = phase.Kind,
                        DurationAmount = phase.DurationAmount,
                        DurationUnit = phase.DurationUnit,
                        EndPhaseEarlyIfAble = phase.EndPhaseEarlyIfAble,
                    }),
            ],
        };
    }
}
