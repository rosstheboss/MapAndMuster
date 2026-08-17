using Campaign.Application.Common;
using Campaign.Application.Ports;
using Campaign.Domain.News;

namespace Campaign.Application.News;

/// <summary>
/// Reads one news article page for the home board.
/// </summary>
public sealed class GetNewsPageHandler
{
    private readonly INewsStore _news;

    /// <summary>
    /// Initializes a handler.
    /// </summary>
    public GetNewsPageHandler(INewsStore news)
    {
        ArgumentNullException.ThrowIfNull(news);
        _news = news;
    }

    /// <summary>
    /// Returns the requested article page. Page 1 is the newest article.
    /// </summary>
    public async Task<OperationResult<NewsPage>> HandleAsync(int page, CancellationToken cancellationToken)
    {
        var normalized = page < 1 ? 1 : page;
        var result = await _news.GetPageAsync(normalized, cancellationToken).ConfigureAwait(false);
        return OperationResults.Success(result);
    }
}

/// <summary>
/// Creates or replaces a news article. Administrators only.
/// </summary>
public sealed class SaveNewsArticleHandler
{
    private readonly INewsStore _news;
    private readonly IClock _clock;

    /// <summary>
    /// Initializes a handler.
    /// </summary>
    public SaveNewsArticleHandler(INewsStore news, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(news);
        ArgumentNullException.ThrowIfNull(clock);
        _news = news;
        _clock = clock;
    }

    /// <summary>
    /// Saves an article when the caller is an administrator.
    /// </summary>
    public async Task<OperationResult<NewsArticle>> HandleAsync(
        SaveNewsArticleCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!command.IsAdministrator)
        {
            return OperationResults.Failure<NewsArticle>(ErrorCodes.CampaignForbidden, "Only administrators can edit news.");
        }

        if (!NewsArticleRules.TryCreate(command.Title, command.BodyMarkdown, out var title, out var body, out var error))
        {
            return OperationResults.Failure<NewsArticle>(error.Code, error.Message);
        }

        var utcNow = _clock.UtcNow;
        if (command.ArticleId is { } articleId)
        {
            var existing = await _news.FindByIdAsync(articleId, cancellationToken).ConfigureAwait(false);
            if (existing is null)
            {
                return OperationResults.Failure<NewsArticle>("news.not_found", "The news article was not found.");
            }

            var updated = await _news.UpdateAsync(
                    new NewsArticle
                    {
                        Id = existing.Id,
                        Title = title,
                        BodyMarkdown = body,
                        PublishedUtc = existing.PublishedUtc,
                        UpdatedUtc = utcNow,
                        AuthorUserId = existing.AuthorUserId,
                    },
                    cancellationToken)
                .ConfigureAwait(false);
            return updated is null
                ? OperationResults.Failure<NewsArticle>("news.not_found", "The news article was not found.")
                : OperationResults.Success(updated);
        }

        var created = await _news.AddAsync(
                new NewsArticle
                {
                    Id = Guid.NewGuid(),
                    Title = title,
                    BodyMarkdown = body,
                    PublishedUtc = utcNow,
                    UpdatedUtc = utcNow,
                    AuthorUserId = command.UserId,
                },
                cancellationToken)
            .ConfigureAwait(false);
        return OperationResults.Success(created);
    }
}

/// <summary>
/// Deletes a news article. Administrators only.
/// </summary>
public sealed class DeleteNewsArticleHandler
{
    private readonly INewsStore _news;

    /// <summary>
    /// Initializes a handler.
    /// </summary>
    public DeleteNewsArticleHandler(INewsStore news)
    {
        ArgumentNullException.ThrowIfNull(news);
        _news = news;
    }

    /// <summary>
    /// Deletes the article when the caller is an administrator.
    /// </summary>
    public async Task<OperationResult> HandleAsync(Guid articleId, Guid userId, bool isAdministrator, CancellationToken cancellationToken)
    {
        _ = userId;
        if (!isAdministrator)
        {
            return OperationResult.Failure(ErrorCodes.CampaignForbidden, "Only administrators can edit news.");
        }

        var deleted = await _news.DeleteAsync(articleId, cancellationToken).ConfigureAwait(false);
        return deleted
            ? OperationResult.Success()
            : OperationResult.Failure("news.not_found", "The news article was not found.");
    }
}
