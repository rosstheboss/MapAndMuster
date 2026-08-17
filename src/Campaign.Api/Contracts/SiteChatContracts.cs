using Campaign.Application.Chat;

namespace Campaign.Api.Contracts;

/// <summary>
/// A public site-chat message.
/// </summary>
public sealed class SiteChatMessageResponse
{
    /// <summary>Gets the message identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets when the message was posted, in UTC.</summary>
    public required DateTimeOffset PostedUtc { get; init; }

    /// <summary>Gets the author.</summary>
    public required Guid AuthorUserId { get; init; }

    /// <summary>Gets the author's username snapshot.</summary>
    public required string AuthorUsername { get; init; }

    /// <summary>Gets the author's display-name snapshot.</summary>
    public required string AuthorDisplayName { get; init; }

    /// <summary>Gets the message text.</summary>
    public required string Body { get; init; }

    /// <summary>Gets the language flag name.</summary>
    public required string Language { get; init; }

    /// <summary>Gets the kind name: Player or Admin.</summary>
    public required string Kind { get; init; }

    /// <summary>Gets the directed admin recipient, when any.</summary>
    public Guid? TargetUserId { get; init; }

    /// <summary>Gets the directed recipient username snapshot, when any.</summary>
    public string? TargetUsername { get; init; }

    /// <summary>Gets the directed recipient display-name snapshot, when any.</summary>
    public string? TargetDisplayName { get; init; }
}

/// <summary>
/// A person who can be tagged, blocked, or selected as an admin target.
/// </summary>
public sealed class SiteChatMemberResponse
{
    /// <summary>Gets the account identifier.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets the unique username.</summary>
    public required string Username { get; init; }

    /// <summary>Gets the name shown to other users.</summary>
    public required string DisplayName { get; init; }
}

/// <summary>
/// The public site-chat board for the authenticated viewer.
/// </summary>
public sealed class SiteChatBoardResponse
{
    /// <summary>Gets messages the viewer may see, oldest first.</summary>
    public required IReadOnlyList<SiteChatMessageResponse> Messages { get; init; }

    /// <summary>Gets people who can be tagged.</summary>
    public required IReadOnlyList<SiteChatMemberResponse> MentionableUsers { get; init; }

    /// <summary>Gets people the viewer currently blocks.</summary>
    public required IReadOnlyList<SiteChatMemberResponse> BlockedUsers { get; init; }

    /// <summary>Gets supported language flag names.</summary>
    public required IReadOnlyList<string> Languages { get; init; }

    /// <summary>Gets the viewer's profile default compose language.</summary>
    public required string PreferredLanguage { get; init; }

    /// <summary>Gets whether the viewer may post.</summary>
    public required bool CanChat { get; init; }

    /// <summary>Gets whether the viewer may send administrator announcements.</summary>
    public required bool CanSendAdminMessages { get; init; }
}

/// <summary>
/// Request to post a site-chat message.
/// </summary>
public sealed class PostSiteChatRequest
{
    /// <summary>Gets the message text.</summary>
    public string? Message { get; init; }

    /// <summary>Gets the language flag. Blank becomes English.</summary>
    public string? Language { get; init; }

    /// <summary>Gets whether this is an administrator announcement.</summary>
    public bool SendAsAdministrator { get; init; }

    /// <summary>Gets the directed admin recipient, when any.</summary>
    public Guid? TargetUserId { get; init; }
}

/// <summary>
/// Request to add or remove a site-chat block.
/// </summary>
public sealed class SetSiteChatBlockRequest
{
    /// <summary>Gets whether the target should be on the viewer's block list.</summary>
    public required bool Blocked { get; init; }
}

/// <summary>
/// Maps site-chat application models onto HTTP contracts.
/// </summary>
public static class SiteChatResponses
{
    /// <summary>
    /// Maps the board.
    /// </summary>
    public static SiteChatBoardResponse FromBoard(SiteChatBoard board)
    {
        ArgumentNullException.ThrowIfNull(board);
        return new SiteChatBoardResponse
        {
            Messages = [.. board.Messages.Select(FromMessage)],
            MentionableUsers = [.. board.MentionableUsers.Select(FromMember)],
            BlockedUsers = [.. board.BlockedUsers.Select(FromMember)],
            Languages = board.Languages,
            PreferredLanguage = board.PreferredLanguage,
            CanChat = board.CanChat,
            CanSendAdminMessages = board.CanSendAdminMessages,
        };
    }

    private static SiteChatMessageResponse FromMessage(SiteChatMessageDetail message)
    {
        return new SiteChatMessageResponse
        {
            Id = message.Id,
            PostedUtc = message.PostedUtc,
            AuthorUserId = message.AuthorUserId,
            AuthorUsername = message.AuthorUsername,
            AuthorDisplayName = message.AuthorDisplayName,
            Body = message.Body,
            Language = message.Language,
            Kind = message.Kind,
            TargetUserId = message.TargetUserId,
            TargetUsername = message.TargetUsername,
            TargetDisplayName = message.TargetDisplayName,
        };
    }

    private static SiteChatMemberResponse FromMember(SiteChatMemberDetail member)
    {
        return new SiteChatMemberResponse
        {
            UserId = member.UserId,
            Username = member.Username,
            DisplayName = member.DisplayName,
        };
    }
}
