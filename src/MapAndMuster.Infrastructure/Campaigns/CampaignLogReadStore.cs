using MapAndMuster.Application.Ports;
using MapAndMuster.Infrastructure.Persistence;
using MapAndMuster.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace MapAndMuster.Infrastructure.Campaigns;

/// <summary>
/// PostgreSQL store for campaign-log last-read marks.
/// </summary>
public sealed class CampaignLogReadStore : ICampaignLogReadStore
{
    private readonly CampaignDbContext _dbContext;

    /// <summary>
    /// Initializes a store.
    /// </summary>
    public CampaignLogReadStore(CampaignDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<DateTimeOffset?> GetLastReadUtcAsync(
        Guid campaignId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var mark = await _dbContext.CampaignLogReadMarks
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.CampaignId == campaignId && item.UserId == userId, cancellationToken)
            .ConfigureAwait(false);
        return mark?.LastReadUtc;
    }

    /// <inheritdoc />
    public async Task MarkReadAsync(
        Guid campaignId,
        Guid userId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var mark = await _dbContext.CampaignLogReadMarks
            .FirstOrDefaultAsync(item => item.CampaignId == campaignId && item.UserId == userId, cancellationToken)
            .ConfigureAwait(false);
        if (mark is null)
        {
            _dbContext.CampaignLogReadMarks.Add(new CampaignLogReadMarkRecord
            {
                CampaignId = campaignId,
                UserId = userId,
                LastReadUtc = utcNow,
            });
        }
        else if (mark.LastReadUtc < utcNow)
        {
            mark.LastReadUtc = utcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
