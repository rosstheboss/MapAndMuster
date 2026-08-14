using Campaign.Application.Campaigns;
using Campaign.Application.Common;
using Campaign.Application.Ports;
using Campaign.Infrastructure.Persistence;
using Campaign.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Campaign.Infrastructure.Campaigns;

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
        var affected = await _dbContext.Campaigns
            .Where(item => item.Id == campaign.Id && item.Revision == expectedRevision)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(item => item.Name, campaign.Name)
                    .SetProperty(item => item.Description, campaign.Description)
                    .SetProperty(item => item.PlayerSlotCount, campaign.PlayerSlotCount)
                    .SetProperty(item => item.IsPrivate, campaign.IsPrivate)
                    .SetProperty(item => item.JoinPasswordHash, campaign.JoinPasswordHash)
                    .SetProperty(item => item.CreatorIsParticipant, campaign.CreatorIsParticipant)
                    .SetProperty(item => item.MapStorageKey, campaign.MapStorageKey)
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
            JoinPasswordHash = campaign.JoinPasswordHash,
            CreatorIsParticipant = campaign.CreatorIsParticipant,
            MapStorageKey = campaign.MapStorageKey,
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
                CampaignId = record.Id,
                Name = group.Name,
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
                CampaignId = record.Id,
                Name = faction.Name,
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
                SortOrder = phaseOrder++,
            });
        }
    }

    private static StoredCampaign ToStored(CampaignRecord record)
    {
        return new StoredCampaign
        {
            Id = record.Id,
            Name = record.Name,
            Description = record.Description,
            PlayerSlotCount = record.PlayerSlotCount,
            IsPrivate = record.IsPrivate,
            JoinPasswordHash = record.JoinPasswordHash,
            CreatorIsParticipant = record.CreatorIsParticipant,
            MapStorageKey = record.MapStorageKey,
            Revision = record.Revision,
            CreatedUtc = record.CreatedUtc,
            UpdatedUtc = record.UpdatedUtc,
            CreatedByUserId = record.CreatedByUserId,
            Memberships =
            [
                .. record.Memberships.Select(membership => new StoredCampaignMembership
                {
                    UserId = membership.UserId,
                    IsGameMaster = membership.IsGameMaster,
                    IsPlayer = membership.IsPlayer,
                }),
            ],
            AllyGroups =
            [
                .. record.AllyGroups
                    .OrderBy(group => group.SortOrder)
                    .Select(group => new StoredAllyGroup { Id = group.Id, Name = group.Name }),
            ],
            Factions =
            [
                .. record.Factions
                    .OrderBy(faction => faction.SortOrder)
                    .Select(faction => new StoredFaction
                    {
                        Id = faction.Id,
                        Name = faction.Name,
                        Subfactions =
                        [
                            .. faction.Subfactions
                                .OrderBy(subfaction => subfaction.SortOrder)
                                .Select(subfaction => subfaction.Name),
                        ],
                        AllyGroupName = faction.AllyGroup?.Name,
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
                    }),
            ],
        };
    }
}
