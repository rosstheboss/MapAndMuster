namespace MapAndMuster.Infrastructure.Persistence;

/// <summary>
/// A public site-wide chat message. Campaign logs never include these rows.
/// </summary>
public sealed class SiteChatMessageRecord
{
    /// <summary>Gets or sets the message identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets when the message was posted, in UTC.</summary>
    public DateTimeOffset PostedUtc { get; set; }

    /// <summary>Gets or sets the author.</summary>
    public Guid AuthorUserId { get; set; }

    /// <summary>Gets or sets the author's username snapshot.</summary>
    public string AuthorUsername { get; set; } = string.Empty;

    /// <summary>Gets or sets the author's display-name snapshot.</summary>
    public string AuthorDisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the message text.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>Gets or sets the language flag name.</summary>
    public string Language { get; set; } = "English";

    /// <summary>Gets or sets the kind name.</summary>
    public string Kind { get; set; } = "Player";

    /// <summary>Gets or sets the directed admin recipient, when any.</summary>
    public Guid? TargetUserId { get; set; }

    /// <summary>Gets or sets the directed recipient username snapshot, when any.</summary>
    public string? TargetUsername { get; set; }

    /// <summary>Gets or sets the directed recipient display-name snapshot, when any.</summary>
    public string? TargetDisplayName { get; set; }
}

/// <summary>
/// A directed site-chat block owned by one user.
/// </summary>
public sealed class SiteChatBlockRecord
{
    /// <summary>Gets or sets the user who owns this block.</summary>
    public Guid BlockerUserId { get; set; }

    /// <summary>Gets or sets the user hidden by this block.</summary>
    public Guid BlockedUserId { get; set; }
}
