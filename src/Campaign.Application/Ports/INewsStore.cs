using Campaign.Application.News;

namespace Campaign.Application.Ports;

/// <summary>
/// Persistence for site-wide news articles.
/// </summary>
public interface INewsStore
{
    /// <summary>
    /// Returns one published article page. Page 1 is the newest article.
    /// </summary>
    Task<NewsPage> GetPageAsync(int page, CancellationToken cancellationToken);

    /// <summary>
    /// Finds an article by identifier.
    /// </summary>
    Task<NewsArticle?> FindByIdAsync(Guid articleId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates an article.
    /// </summary>
    Task<NewsArticle> AddAsync(NewsArticle article, CancellationToken cancellationToken);

    /// <summary>
    /// Replaces an article when it exists.
    /// </summary>
    Task<NewsArticle?> UpdateAsync(NewsArticle article, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes an article when it exists.
    /// </summary>
    Task<bool> DeleteAsync(Guid articleId, CancellationToken cancellationToken);
}
