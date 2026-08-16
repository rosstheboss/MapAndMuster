using System.Diagnostics.CodeAnalysis;
using Campaign.Domain.Common;
using Campaign.Domain.Identity;

namespace Campaign.Domain.Play;

/// <summary>
/// A campaign member who may post or be mentioned in the public log.
/// </summary>
/// <param name="UserId">The member's user identifier.</param>
/// <param name="Username">The unique username.</param>
/// <param name="DisplayName">The name shown to other users.</param>
public sealed record CampaignChatMember(Guid UserId, string Username, string DisplayName);

/// <summary>
/// Validates and appends public campaign chat messages.
/// </summary>
public static class CampaignChatRules
{
    /// <summary>Maximum length of a chat message after trimming.</summary>
    public const int MessageMaxLength = 2000;

    /// <summary>Originator label for campaign-generated log facts.</summary>
    public const string CampaignOriginator = "Campaign";

    /// <summary>
    /// Posts a public chat message from a current campaign member.
    /// </summary>
    /// <param name="state">The current play state, which may be empty before launch.</param>
    /// <param name="userId">The posting member.</param>
    /// <param name="message">The chat text.</param>
    /// <param name="members">Current campaign members who may post or be tagged.</param>
    /// <param name="utcNow">The current UTC instant.</param>
    /// <param name="next">The play state with the chat entry appended.</param>
    /// <param name="error">The validation error when posting fails.</param>
    /// <returns><see langword="true"/> when the message was recorded.</returns>
    public static bool TryPost(
        CampaignPlayState state,
        Guid userId,
        string? message,
        IReadOnlyList<CampaignChatMember> members,
        DateTimeOffset utcNow,
        [NotNullWhen(true)] out CampaignPlayState? next,
        [NotNullWhen(false)] out DomainError? error)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(members);
        next = null;
        error = null;

        var actor = members.FirstOrDefault(member => member.UserId == userId);
        if (actor is null)
        {
            error = new DomainError("chat.forbidden", "Only campaign members can chat in this log.");
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

        if (!TryValidateMentions(trimmed, members, out error))
        {
            return false;
        }

        next = state.AppendLog(new PlayLogEntry(
            Guid.NewGuid(),
            utcNow,
            PlayLogKind.PlayerChat,
            windowId: null,
            forceId: null,
            actor.UserId,
            territoryId: null,
            targetTerritoryId: null,
            battleId: null,
            actionKind: null,
            [],
            trimmed,
            actor.DisplayName));
        return true;
    }

    /// <summary>
    /// Returns whether <paramref name="atIndex"/> starts a mention in <paramref name="text"/>.
    /// </summary>
    /// <param name="text">The message text.</param>
    /// <param name="atIndex">The index of an '@' character.</param>
    /// <returns><see langword="true"/> when this '@' begins a member tag.</returns>
    public static bool IsMentionStart(string text, int atIndex)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (atIndex < 0 || atIndex >= text.Length || text[atIndex] != '@')
        {
            return false;
        }

        if (atIndex > 0 && text[atIndex - 1] == '\\')
        {
            return false;
        }

        if (atIndex == 0)
        {
            return true;
        }

        return !char.IsLetterOrDigit(text[atIndex - 1]);
    }

    private static bool TryValidateMentions(
        string text,
        IReadOnlyList<CampaignChatMember> members,
        [NotNullWhen(false)] out DomainError? error)
    {
        error = null;
        var tokens = MentionTokens(members);
        var index = 0;
        while (index < text.Length)
        {
            if (text[index] == '\\' && index + 1 < text.Length && text[index + 1] == '@')
            {
                index += 2;
                continue;
            }

            if (text[index] != '@' || !IsMentionStart(text, index))
            {
                index++;
                continue;
            }

            var remainder = text[(index + 1)..];
            var match = tokens
                .Where(token => remainder.StartsWith(token, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(static token => token.Length)
                .FirstOrDefault();
            if (string.IsNullOrEmpty(match))
            {
                error = new DomainError(
                    "chat.mention.unknown",
                    "You can only tag people who have joined this campaign.",
                    "message");
                return false;
            }

            index += 1 + match.Length;
        }

        return true;
    }

    private static string[] MentionTokens(IReadOnlyList<CampaignChatMember> members)
    {
        var usernames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var displayNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var member in members)
        {
            if (!string.IsNullOrWhiteSpace(member.Username))
            {
                usernames.Add(member.Username);
            }

            if (!string.IsNullOrWhiteSpace(member.DisplayName)
                && !usernames.Contains(member.DisplayName))
            {
                displayNames[member.DisplayName] = displayNames.GetValueOrDefault(member.DisplayName) + 1;
            }
        }

        return
        [
            .. usernames,
            .. displayNames.Where(static pair => pair.Value == 1).Select(static pair => pair.Key),
        ];
    }
}
