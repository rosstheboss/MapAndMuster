using MapAndMuster.Application.News;
using MapAndMuster.Application.Notifications;
using MapAndMuster.Application.Ports;
using MapAndMuster.Domain.News;
using Microsoft.EntityFrameworkCore;

namespace MapAndMuster.Infrastructure.Persistence;

/// <summary>
/// PostgreSQL store for in-app user notifications.
/// </summary>
public sealed class UserNotificationStore : IUserNotificationStore
{
    private readonly CampaignDbContext _dbContext;

    /// <summary>
    /// Initializes a store.
    /// </summary>
    public UserNotificationStore(CampaignDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<bool> TryAddAsync(
        NewUserNotification notification,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var exists = await _dbContext.UserNotifications
            .AnyAsync(
                item => item.UserId == notification.UserId && item.DedupeKey == notification.DedupeKey,
                cancellationToken)
            .ConfigureAwait(false);
        if (exists)
        {
            return false;
        }

        _dbContext.UserNotifications.Add(ToRecord(notification, utcNow));

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateException)
        {
            _dbContext.ChangeTracker.Clear();
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<string>> TryAddManyAsync(
        IReadOnlyList<NewUserNotification> notifications,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notifications);
        var accepted = new HashSet<string>(StringComparer.Ordinal);
        if (notifications.Count == 0)
        {
            return accepted;
        }

        var userIds = notifications.Select(static item => item.UserId).Distinct().ToArray();
        var dedupeKeys = notifications.Select(static item => item.DedupeKey).Distinct(StringComparer.Ordinal).ToArray();
        var existing = await _dbContext.UserNotifications
            .AsNoTracking()
            .Where(item => userIds.Contains(item.UserId) && dedupeKeys.Contains(item.DedupeKey))
            .Select(item => new { item.UserId, item.DedupeKey })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var seen = existing.Select(item => DedupeIdentity(item.UserId, item.DedupeKey)).ToHashSet(StringComparer.Ordinal);
        var pending = new List<NewUserNotification>();
        foreach (var notification in notifications)
        {
            if (seen.Add(DedupeIdentity(notification.UserId, notification.DedupeKey)))
            {
                pending.Add(notification);
            }
        }

        if (pending.Count == 0)
        {
            return accepted;
        }

        foreach (var notification in pending)
        {
            _dbContext.UserNotifications.Add(ToRecord(notification, utcNow));
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            foreach (var notification in pending)
            {
                accepted.Add(notification.DedupeKey);
            }

            return accepted;
        }
        catch (DbUpdateException)
        {
            // A concurrent fan-out inserted one of these first. The batch is all-or-nothing, so
            // fall back to per-notice inserts rather than dropping the ones that would have
            // succeeded.
            _dbContext.ChangeTracker.Clear();
            foreach (var notification in pending)
            {
                if (await TryAddAsync(notification, utcNow, cancellationToken).ConfigureAwait(false))
                {
                    accepted.Add(notification.DedupeKey);
                }
            }

            return accepted;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserNotification>> ListUnreadAsync(Guid userId, CancellationToken cancellationToken)
    {
        var records = await _dbContext.UserNotifications
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.ReadUtc == null)
            .OrderByDescending(item => item.CreatedUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return [.. records.Select(Map)];
    }

    /// <inheritdoc />
    public async Task<bool> MarkReadAsync(
        Guid notificationId,
        Guid userId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var record = await _dbContext.UserNotifications
            .FirstOrDefaultAsync(item => item.Id == notificationId && item.UserId == userId, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return false;
        }

        if (record.ReadUtc is null)
        {
            record.ReadUtc = utcNow;
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<int> MarkAllReadAsync(Guid userId, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        // One UPDATE. Loading each unread notice only to stamp one column made a long-unread
        // history cost proportional to its size.
        return await _dbContext.UserNotifications
            .Where(item => item.UserId == userId && item.ReadUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.ReadUtc, utcNow), cancellationToken)
            .ConfigureAwait(false);
    }

    private static string DedupeIdentity(Guid userId, string dedupeKey) => $"{userId:N}|{dedupeKey}";

    private static UserNotificationRecord ToRecord(NewUserNotification notification, DateTimeOffset utcNow)
    {
        return new UserNotificationRecord
        {
            Id = Guid.NewGuid(),
            UserId = notification.UserId,
            Kind = notification.Kind.ToString(),
            CampaignId = notification.CampaignId,
            CampaignName = notification.CampaignName,
            Title = notification.Title,
            Body = notification.Body,
            Path = notification.Path,
            DedupeKey = notification.DedupeKey,
            CreatedUtc = utcNow,
        };
    }

    private static UserNotification Map(UserNotificationRecord record)
    {
        return new UserNotification
        {
            Id = record.Id,
            UserId = record.UserId,
            Kind = record.Kind,
            CampaignId = record.CampaignId,
            CampaignName = record.CampaignName,
            Title = record.Title,
            Body = record.Body,
            Path = record.Path,
            CreatedUtc = record.CreatedUtc,
            ReadUtc = record.ReadUtc,
            DedupeKey = record.DedupeKey,
        };
    }
}

/// <summary>
/// PostgreSQL store for site-wide news articles.
/// </summary>
public sealed class NewsStore : INewsStore
{
    private readonly CampaignDbContext _dbContext;

    /// <summary>
    /// Initializes a store.
    /// </summary>
    public NewsStore(CampaignDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<NewsPage> GetPageAsync(int page, CancellationToken cancellationToken)
    {
        var total = await _dbContext.NewsArticles.CountAsync(cancellationToken).ConfigureAwait(false);
        var pageSize = NewsArticleRules.HomePageSize;
        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);
        var normalized = page < 1 ? 1 : page;
        if (totalPages > 0 && normalized > totalPages)
        {
            normalized = totalPages;
        }

        IReadOnlyList<NewsArticleRecord> records = total == 0
            ? []
            : await _dbContext.NewsArticles
                .AsNoTracking()
                .OrderByDescending(item => item.PublishedUtc)
                .ThenByDescending(item => item.Id)
                .Skip((normalized - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        return new NewsPage
        {
            Page = total == 0 ? 1 : normalized,
            TotalPages = totalPages,
            Articles = [.. records.Select(Map)],
        };
    }

    /// <inheritdoc />
    public async Task<NewsArticle?> FindByIdAsync(Guid articleId, CancellationToken cancellationToken)
    {
        var record = await _dbContext.NewsArticles
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == articleId, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : Map(record);
    }

    /// <inheritdoc />
    public async Task<NewsArticle> AddAsync(NewsArticle article, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(article);
        var record = new NewsArticleRecord
        {
            Id = article.Id,
            Title = article.Title,
            BodyMarkdown = article.BodyMarkdown,
            PublishedUtc = article.PublishedUtc,
            UpdatedUtc = article.UpdatedUtc,
            AuthorUserId = article.AuthorUserId,
        };
        _dbContext.NewsArticles.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Map(record);
    }

    /// <inheritdoc />
    public async Task<NewsArticle?> UpdateAsync(NewsArticle article, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(article);
        var record = await _dbContext.NewsArticles
            .FirstOrDefaultAsync(item => item.Id == article.Id, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return null;
        }

        record.Title = article.Title;
        record.BodyMarkdown = article.BodyMarkdown;
        record.UpdatedUtc = article.UpdatedUtc;
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Map(record);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid articleId, CancellationToken cancellationToken)
    {
        var record = await _dbContext.NewsArticles
            .FirstOrDefaultAsync(item => item.Id == articleId, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return false;
        }

        _dbContext.NewsArticles.Remove(record);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    private static NewsArticle Map(NewsArticleRecord record)
    {
        return new NewsArticle
        {
            Id = record.Id,
            Title = record.Title,
            BodyMarkdown = record.BodyMarkdown,
            PublishedUtc = record.PublishedUtc,
            UpdatedUtc = record.UpdatedUtc,
            AuthorUserId = record.AuthorUserId,
        };
    }
}
