namespace Campaign.Infrastructure.Persistence;

/// <summary>
/// A stored in-app notice for one user.
/// </summary>
public sealed class UserNotificationRecord
{
    /// <summary>Gets or sets the notice identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the recipient.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the kind name.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the related campaign.</summary>
    public Guid? CampaignId { get; set; }

    /// <summary>Gets or sets the campaign name snapshot.</summary>
    public string? CampaignName { get; set; }

    /// <summary>Gets or sets the title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the safe body.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>Gets or sets the in-app path.</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>Gets or sets the dedupe key.</summary>
    public string DedupeKey { get; set; } = string.Empty;

    /// <summary>Gets or sets when the notice was created, in UTC.</summary>
    public DateTimeOffset CreatedUtc { get; set; }

    /// <summary>Gets or sets when the recipient marked it read.</summary>
    public DateTimeOffset? ReadUtc { get; set; }
}

/// <summary>
/// A site-wide news article authored by an administrator.
/// </summary>
public sealed class NewsArticleRecord
{
    /// <summary>Gets or sets the article identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the markdown source.</summary>
    public string BodyMarkdown { get; set; } = string.Empty;

    /// <summary>Gets or sets when the article was published, in UTC.</summary>
    public DateTimeOffset PublishedUtc { get; set; }

    /// <summary>Gets or sets when the article was last edited, in UTC.</summary>
    public DateTimeOffset UpdatedUtc { get; set; }

    /// <summary>Gets or sets the authoring administrator.</summary>
    public Guid AuthorUserId { get; set; }
}
