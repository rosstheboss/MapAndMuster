using Campaign.Application.News;
using Campaign.Application.Notifications;

namespace Campaign.Api.Contracts;

/// <summary>
/// Home-page attention item.
/// </summary>
public sealed class HomeAttentionItemResponse
{
    /// <summary>Gets a stable identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the kind name.</summary>
    public required string Kind { get; init; }

    /// <summary>Gets the campaign, when any.</summary>
    public Guid? CampaignId { get; init; }

    /// <summary>Gets the campaign name.</summary>
    public string? CampaignName { get; init; }

    /// <summary>Gets the title.</summary>
    public required string Title { get; init; }

    /// <summary>Gets the summary.</summary>
    public required string Body { get; init; }

    /// <summary>Gets the path to open.</summary>
    public required string Path { get; init; }

    /// <summary>Gets when the item was created, in UTC.</summary>
    public required DateTimeOffset CreatedUtc { get; init; }
}

/// <summary>
/// A published news article.
/// </summary>
public sealed class NewsArticleResponse
{
    /// <summary>Gets the article identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the title.</summary>
    public required string Title { get; init; }

    /// <summary>Gets the markdown source.</summary>
    public required string BodyMarkdown { get; init; }

    /// <summary>Gets sanitized HTML rendered from the markdown.</summary>
    public required string BodyHtml { get; init; }

    /// <summary>Gets when the article was published, in UTC.</summary>
    public required DateTimeOffset PublishedUtc { get; init; }

    /// <summary>Gets when the article was last edited, in UTC.</summary>
    public required DateTimeOffset UpdatedUtc { get; init; }
}

/// <summary>
/// One article page for the home news board.
/// </summary>
public sealed class NewsPageResponse
{
    /// <summary>Gets the 1-based page number.</summary>
    public required int Page { get; init; }

    /// <summary>Gets the total article count.</summary>
    public required int TotalPages { get; init; }

    /// <summary>Gets the article on this page, when any exist.</summary>
    public NewsArticleResponse? Article { get; init; }
}

/// <summary>
/// Request to create or replace a news article.
/// </summary>
public sealed class SaveNewsArticleRequest
{
    /// <summary>Gets the title.</summary>
    public required string Title { get; init; }

    /// <summary>Gets the markdown body.</summary>
    public required string BodyMarkdown { get; init; }
}

/// <summary>
/// Maps home-board and news models onto HTTP contracts.
/// </summary>
public static class HomeBoardResponses
{
    /// <summary>
    /// Maps an attention item.
    /// </summary>
    public static HomeAttentionItemResponse FromAttention(HomeAttentionItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return new HomeAttentionItemResponse
        {
            Id = item.Id,
            Kind = item.Kind,
            CampaignId = item.CampaignId,
            CampaignName = item.CampaignName,
            Title = item.Title,
            Body = item.Body,
            Path = item.Path,
            CreatedUtc = item.CreatedUtc,
        };
    }

    /// <summary>
    /// Maps a news page.
    /// </summary>
    public static NewsPageResponse FromNews(NewsPage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        return new NewsPageResponse
        {
            Page = page.Page,
            TotalPages = page.TotalPages,
            Article = page.Article is null ? null : FromArticle(page.Article),
        };
    }

    /// <summary>
    /// Maps a news article.
    /// </summary>
    public static NewsArticleResponse FromArticle(NewsArticle article)
    {
        ArgumentNullException.ThrowIfNull(article);
        return new NewsArticleResponse
        {
            Id = article.Id,
            Title = article.Title,
            BodyMarkdown = article.BodyMarkdown,
            BodyHtml = article.BodyHtml,
            PublishedUtc = article.PublishedUtc,
            UpdatedUtc = article.UpdatedUtc,
        };
    }
}
