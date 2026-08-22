using MapAndMuster.Domain.Chat;
using MapAndMuster.Domain.Play;

namespace MapAndMuster.Application.Chat;

/// <summary>
/// A site-chat message shaped for the authenticated viewer.
/// </summary>
public sealed class SiteChatMessageDetail
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
public sealed class SiteChatMemberDetail
{
    /// <summary>Gets the account identifier.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets the unique username.</summary>
    public required string Username { get; init; }

    /// <summary>Gets the name shown to other users.</summary>
    public required string DisplayName { get; init; }
}

/// <summary>
/// The public site-chat board for one viewer.
/// </summary>
public sealed class SiteChatBoard
{
    /// <summary>Gets messages the viewer may see, oldest first.</summary>
    public required IReadOnlyList<SiteChatMessageDetail> Messages { get; init; }

    /// <summary>Gets people who can be tagged.</summary>
    public required IReadOnlyList<SiteChatMemberDetail> MentionableUsers { get; init; }

    /// <summary>Gets people the viewer currently blocks.</summary>
    public required IReadOnlyList<SiteChatMemberDetail> BlockedUsers { get; init; }

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
/// Input required to post a site-chat message.
/// </summary>
public sealed class PostSiteChatCommand
{
    /// <summary>Gets the authenticated user.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets whether the caller is a system administrator.</summary>
    public required bool IsAdministrator { get; init; }

    /// <summary>Gets the message text.</summary>
    public required string? Message { get; init; }

    /// <summary>Gets the language flag. Blank becomes English.</summary>
    public string? Language { get; init; }

    /// <summary>Gets whether this is an administrator announcement.</summary>
    public bool SendAsAdministrator { get; init; }

    /// <summary>Gets the directed admin recipient, when any.</summary>
    public Guid? TargetUserId { get; init; }
}

/// <summary>
/// Input required to add or remove a site-chat block.
/// </summary>
public sealed class SetSiteChatBlockCommand
{
    /// <summary>Gets the authenticated user.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets whether the caller is a system administrator.</summary>
    public required bool IsAdministrator { get; init; }

    /// <summary>Gets the other person.</summary>
    public required Guid TargetUserId { get; init; }

    /// <summary>Gets whether the target should be on the viewer's block list.</summary>
    public required bool Blocked { get; init; }
}

/// <summary>
/// Maps stored site-chat facts onto viewer models.
/// </summary>
public static class SiteChatMapper
{
    /// <summary>
    /// Builds the board for one viewer.
    /// </summary>
    public static SiteChatBoard ToBoard(
        IReadOnlyList<SiteChatMessage> messages,
        IReadOnlyList<CampaignChatMember> members,
        IReadOnlyList<SiteChatBlock> blocks,
        Guid viewerUserId,
        bool isAdministrator,
        string preferredLanguage)
    {
        ArgumentNullException.ThrowIfNull(messages);
        ArgumentNullException.ThrowIfNull(members);
        ArgumentNullException.ThrowIfNull(blocks);
        var hidden = SiteChatRules.HiddenAuthorIds(viewerUserId, blocks);
        var blockedIds = blocks
            .Where(block => block.BlockerUserId == viewerUserId)
            .Select(block => block.BlockedUserId)
            .ToHashSet();
        var byId = members.ToDictionary(static member => member.UserId);
        return new SiteChatBoard
        {
            Messages =
            [
                .. messages
                    .Where(message => SiteChatRules.CanView(message, viewerUserId, hidden))
                    .Select(ToDetail),
            ],
            MentionableUsers = [.. members.Select(ToMember)],
            BlockedUsers =
            [
                .. blockedIds
                    .Select(id => byId.GetValueOrDefault(id))
                    .Where(static member => member is not null)
                    .Select(static member => ToMember(member!))
                    .OrderBy(static member => member.DisplayName, StringComparer.OrdinalIgnoreCase),
            ],
            Languages = [.. ChatLanguages.All.Select(static language => language.ToString())],
            PreferredLanguage = preferredLanguage,
            CanChat = true,
            CanSendAdminMessages = isAdministrator,
        };
    }

    /// <summary>
    /// Maps a stored message.
    /// </summary>
    public static SiteChatMessageDetail ToDetail(SiteChatMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return new SiteChatMessageDetail
        {
            Id = message.Id,
            PostedUtc = message.PostedUtc,
            AuthorUserId = message.AuthorUserId,
            AuthorUsername = message.AuthorUsername,
            AuthorDisplayName = message.AuthorDisplayName,
            Body = message.Body,
            Language = message.Language.ToString(),
            Kind = message.Kind.ToString(),
            TargetUserId = message.TargetUserId,
            TargetUsername = message.TargetUsername,
            TargetDisplayName = message.TargetDisplayName,
        };
    }

    /// <summary>
    /// Maps a mentionable person.
    /// </summary>
    public static SiteChatMemberDetail ToMember(CampaignChatMember member)
    {
        ArgumentNullException.ThrowIfNull(member);
        return new SiteChatMemberDetail
        {
            UserId = member.UserId,
            Username = member.Username,
            DisplayName = member.DisplayName,
        };
    }
}
