namespace MapAndMuster.Domain.Chat;

/// <summary>
/// Who authored a site-chat message.
/// </summary>
public enum SiteChatKind
{
    /// <summary>An ordinary signed-in user's public message.</summary>
    Player = 0,

    /// <summary>An administrator announcement, either to everyone or directed at one person.</summary>
    Admin = 1,
}

/// <summary>
/// A public site-wide chat message. Campaign logs never include these entries.
/// </summary>
/// <param name="Id">The message identifier.</param>
/// <param name="PostedUtc">When the message was posted, in UTC.</param>
/// <param name="AuthorUserId">The authoring account.</param>
/// <param name="AuthorUsername">The author's username snapshot.</param>
/// <param name="AuthorDisplayName">The author's display-name snapshot.</param>
/// <param name="Body">The trimmed message text.</param>
/// <param name="Language">The language flag chosen by the author.</param>
/// <param name="Kind">Player chat or an administrator announcement.</param>
/// <param name="TargetUserId">The directed admin recipient, when any.</param>
/// <param name="TargetUsername">The directed recipient username snapshot, when any.</param>
/// <param name="TargetDisplayName">The directed recipient display-name snapshot, when any.</param>
public sealed record SiteChatMessage(
    Guid Id,
    DateTimeOffset PostedUtc,
    Guid AuthorUserId,
    string AuthorUsername,
    string AuthorDisplayName,
    string Body,
    ChatLanguage Language,
    SiteChatKind Kind,
    Guid? TargetUserId,
    string? TargetUsername,
    string? TargetDisplayName);

/// <summary>
/// A directed site-chat block. Either direction hides both authors from each other.
/// </summary>
/// <param name="BlockerUserId">The user who owns this block.</param>
/// <param name="BlockedUserId">The user hidden by this block.</param>
public sealed record SiteChatBlock(Guid BlockerUserId, Guid BlockedUserId);
