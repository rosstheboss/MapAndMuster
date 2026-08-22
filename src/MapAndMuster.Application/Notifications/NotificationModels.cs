using MapAndMuster.Domain.Notifications;

namespace MapAndMuster.Application.Notifications;

/// <summary>
/// An in-app notice stored for a user. Email copies omit secrets and private chat text.
/// </summary>
public sealed class UserNotification
{
    /// <summary>Gets the notification identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the recipient.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets the notice kind name.</summary>
    public required string Kind { get; init; }

    /// <summary>Gets the related campaign, when any.</summary>
    public Guid? CampaignId { get; init; }

    /// <summary>Gets the campaign name snapshot.</summary>
    public string? CampaignName { get; init; }

    /// <summary>Gets the short title.</summary>
    public required string Title { get; init; }

    /// <summary>Gets the safe summary. Private chat bodies are never included.</summary>
    public required string Body { get; init; }

    /// <summary>Gets the in-app path to open, such as /campaigns/{id}.</summary>
    public required string Path { get; init; }

    /// <summary>Gets when the notice was created, in UTC.</summary>
    public required DateTimeOffset CreatedUtc { get; init; }

    /// <summary>Gets when the recipient marked it read, if ever.</summary>
    public DateTimeOffset? ReadUtc { get; init; }

    /// <summary>Gets the dedupe key used to avoid repeat notices for the same event.</summary>
    public required string DedupeKey { get; init; }
}

/// <summary>
/// Request to create a notice for one user.
/// </summary>
public sealed class NewUserNotification
{
    /// <summary>Gets the recipient.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets the kind.</summary>
    public required NotificationKind Kind { get; init; }

    /// <summary>Gets the campaign identifier.</summary>
    public Guid? CampaignId { get; init; }

    /// <summary>Gets the campaign name snapshot.</summary>
    public string? CampaignName { get; init; }

    /// <summary>Gets the title.</summary>
    public required string Title { get; init; }

    /// <summary>Gets the safe body.</summary>
    public required string Body { get; init; }

    /// <summary>Gets the in-app path.</summary>
    public required string Path { get; init; }

    /// <summary>Gets the dedupe key.</summary>
    public required string DedupeKey { get; init; }
}

/// <summary>
/// Home-page item requiring the viewer's attention.
/// </summary>
public sealed class HomeAttentionItem
{
    /// <summary>Gets a stable identifier for the list.</summary>
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
