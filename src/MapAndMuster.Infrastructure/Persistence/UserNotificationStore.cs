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

        _dbContext.UserNotifications.Add(new UserNotificationRecord
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
        });

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
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
        var records = await _dbContext.UserNotifications
            .Where(item => item.UserId == userId && item.ReadUtc == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var record in records)
        {
            record.ReadUtc = utcNow;
        }

        if (records.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return records.Count;
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
