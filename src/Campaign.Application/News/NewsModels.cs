using Campaign.Domain.News;

namespace Campaign.Application.News;

/// <summary>
/// A published news article for the home board.
/// </summary>
public sealed class NewsArticle
{
    /// <summary>Gets the article identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the title.</summary>
    public required string Title { get; init; }

    /// <summary>Gets the markdown source.</summary>
    public required string BodyMarkdown { get; init; }

    /// <summary>Gets sanitized HTML rendered from the markdown.</summary>
    public string BodyHtml => NewsMarkdown.ToHtml(BodyMarkdown);

    /// <summary>Gets when the article was published, in UTC.</summary>
    public required DateTimeOffset PublishedUtc { get; init; }

    /// <summary>Gets when the article was last edited, in UTC.</summary>
    public required DateTimeOffset UpdatedUtc { get; init; }

    /// <summary>Gets the authoring administrator.</summary>
    public required Guid AuthorUserId { get; init; }
}

/// <summary>
/// One article page for the home news board.
/// </summary>
public sealed class NewsPage
{
    /// <summary>Gets the 1-based page number.</summary>
    public required int Page { get; init; }

    /// <summary>Gets the total article count.</summary>
    public required int TotalPages { get; init; }

    /// <summary>Gets the article on this page, when any exist.</summary>
    public NewsArticle? Article { get; init; }
}

/// <summary>
/// Command to create a news article.
/// </summary>
public sealed class SaveNewsArticleCommand
{
    /// <summary>Gets the administrator.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets whether the caller is an administrator.</summary>
    public required bool IsAdministrator { get; init; }

    /// <summary>Gets the article to replace, when editing.</summary>
    public Guid? ArticleId { get; init; }

    /// <summary>Gets the title.</summary>
    public required string Title { get; init; }

    /// <summary>Gets the markdown body.</summary>
    public required string BodyMarkdown { get; init; }
}
