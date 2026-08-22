using System.Diagnostics.CodeAnalysis;
using MapAndMuster.Domain.Common;
using MapAndMuster.Domain.Identity;
using MapAndMuster.Domain.Play;

namespace MapAndMuster.Domain.Chat;

/// <summary>
/// Validates public site-wide chat. Messages are never stored on a campaign log.
/// </summary>
public static class SiteChatRules
{
    /// <summary>Maximum length of a chat message after trimming.</summary>
    public const int MessageMaxLength = CampaignChatRules.MessageMaxLength;

    /// <summary>Newest messages returned in one board read.</summary>
    public const int RecentMessageLimit = 200;

    /// <summary>In-app path for site-chat notices.</summary>
    public const string BoardPath = "/campaigns/all";

    /// <summary>
    /// Posts a public site-chat message from a signed-in account.
    /// </summary>
    public static bool TryPost(
        Guid authorUserId,
        string? message,
        string? language,
        IReadOnlyList<CampaignChatMember> members,
        DateTimeOffset utcNow,
        bool isAdministrator,
        bool sendAsAdministrator,
        Guid? targetUserId,
        [NotNullWhen(true)] out SiteChatMessage? posted,
        [NotNullWhen(false)] out DomainError? error)
    {
        ArgumentNullException.ThrowIfNull(members);
        posted = null;
        error = null;

        var actor = members.FirstOrDefault(member => member.UserId == authorUserId);
        if (actor is null)
        {
            error = new DomainError("sitechat.forbidden", "Sign in to chat.");
            return false;
        }

        var trimmed = message?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            error = new DomainError("chat.message.required", "Enter a chat message.", "message");
            return false;
        }

        if (trimmed.Length > MessageMaxLength)
        {
            error = new DomainError(
                "chat.message.too_long",
                $"Chat messages are limited to {MessageMaxLength} characters.",
                "message");
            return false;
        }

        if (ProhibitedLanguage.ContainsProhibitedTerm(trimmed))
        {
            error = ProhibitedLanguage.ErrorFor("message", "Chat message");
            return false;
        }

        if (!ChatLanguages.TryParse(language, out error, out var parsedLanguage))
        {
            return false;
        }

        if (!CampaignChatRules.TryValidateMentions(
                trimmed,
                members,
                out error,
                CampaignChatRules.SiteUnknownMentionMessage))
        {
            return false;
        }

        if (sendAsAdministrator)
        {
            if (!isAdministrator)
            {
                error = new DomainError(
                    "sitechat.admin.forbidden",
                    "Only administrators can send administrator messages.");
                return false;
            }

            if (targetUserId is { } targetId)
            {
                if (targetId == authorUserId)
                {
                    error = new DomainError(
                        "sitechat.admin.target.invalid",
                        "Choose another person for a directed administrator message.",
                        "targetUserId");
                    return false;
                }

                var target = members.FirstOrDefault(member => member.UserId == targetId);
                if (target is null)
                {
                    error = new DomainError(
                        "sitechat.admin.target.invalid",
                        "Choose a person with an account on this site.",
                        "targetUserId");
                    return false;
                }

                posted = new SiteChatMessage(
                    Guid.NewGuid(),
                    utcNow,
                    actor.UserId,
                    actor.Username,
                    actor.DisplayName,
                    trimmed,
                    parsedLanguage,
                    SiteChatKind.Admin,
                    target.UserId,
                    target.Username,
                    target.DisplayName);
                return true;
            }

            posted = new SiteChatMessage(
                Guid.NewGuid(),
                utcNow,
                actor.UserId,
                actor.Username,
                actor.DisplayName,
                trimmed,
                parsedLanguage,
                SiteChatKind.Admin,
                TargetUserId: null,
                TargetUsername: null,
                TargetDisplayName: null);
            return true;
        }

        if (targetUserId is not null)
        {
            error = new DomainError(
                "sitechat.channel.invalid",
                "Site chat between players is public. Only administrators can direct a message at one person.",
                "targetUserId");
            return false;
        }

        posted = new SiteChatMessage(
            Guid.NewGuid(),
            utcNow,
            actor.UserId,
            actor.Username,
            actor.DisplayName,
            trimmed,
            parsedLanguage,
            SiteChatKind.Player,
            TargetUserId: null,
            TargetUsername: null,
            TargetDisplayName: null);
        return true;
    }

    /// <summary>
    /// Authors hidden from <paramref name="viewerUserId"/> because either person blocked the other.
    /// Administrator announcements are never hidden this way.
    /// </summary>
    public static IReadOnlySet<Guid> HiddenAuthorIds(
        Guid viewerUserId,
        IReadOnlyList<SiteChatBlock> blocks)
    {
        ArgumentNullException.ThrowIfNull(blocks);
        var hidden = new HashSet<Guid>();
        foreach (var block in blocks)
        {
            if (block.BlockerUserId == viewerUserId)
            {
                hidden.Add(block.BlockedUserId);
            }
            else if (block.BlockedUserId == viewerUserId)
            {
                hidden.Add(block.BlockerUserId);
            }
        }

        hidden.Remove(viewerUserId);
        return hidden;
    }

    /// <summary>
    /// Returns whether the viewer may see <paramref name="message"/>.
    /// Player messages from a mutually blocked author are omitted. Administrator messages are always visible.
    /// </summary>
    public static bool CanView(SiteChatMessage message, Guid viewerUserId, IReadOnlySet<Guid> hiddenAuthorIds)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(hiddenAuthorIds);
        if (message.Kind == SiteChatKind.Admin)
        {
            return true;
        }

        if (message.AuthorUserId == viewerUserId)
        {
            return true;
        }

        return !hiddenAuthorIds.Contains(message.AuthorUserId);
    }

    /// <summary>
    /// Validates a block toggle. Blocking is one-way in storage and two-way in visibility.
    /// </summary>
    public static bool TryValidateBlock(
        Guid blockerUserId,
        Guid blockedUserId,
        IReadOnlyList<CampaignChatMember> members,
        [NotNullWhen(false)] out DomainError? error)
    {
        ArgumentNullException.ThrowIfNull(members);
        error = null;
        if (blockerUserId == blockedUserId)
        {
            error = new DomainError("sitechat.block.self", "You cannot block yourself.", "userId");
            return false;
        }

        if (members.All(member => member.UserId != blockedUserId))
        {
            error = new DomainError("sitechat.block.unknown", "That person was not found.", "userId");
            return false;
        }

        return true;
    }
}
