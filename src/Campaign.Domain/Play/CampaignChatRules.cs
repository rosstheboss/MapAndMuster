using System.Diagnostics.CodeAnalysis;
using Campaign.Domain.Common;
using Campaign.Domain.Identity;

namespace Campaign.Domain.Play;

/// <summary>
/// A campaign member who may post or be mentioned in chat.
/// </summary>
/// <param name="UserId">The member's user identifier.</param>
/// <param name="Username">The unique username.</param>
/// <param name="DisplayName">The name shown to other users.</param>
public sealed record CampaignChatMember(Guid UserId, string Username, string DisplayName);

/// <summary>
/// A current membership used to resolve private chat audiences.
/// </summary>
/// <param name="UserId">The member's user identifier.</param>
/// <param name="FactionId">The member's chosen faction, when any.</param>
/// <param name="AllyGroupId">The ally group of that faction, when any.</param>
public sealed record CampaignChatMembership(Guid UserId, Guid? FactionId, Guid? AllyGroupId);

/// <summary>
/// A faction that may be selected as a private chat audience.
/// </summary>
/// <param name="Id">The faction identifier.</param>
/// <param name="Name">The faction name.</param>
/// <param name="AllyGroupId">The ally group this faction belongs to, when any.</param>
public sealed record CampaignChatFaction(Guid Id, string Name, Guid? AllyGroupId);

/// <summary>
/// An ally group that may be selected as a private chat audience.
/// </summary>
/// <param name="Id">The ally-group identifier.</param>
/// <param name="Name">The ally-group name.</param>
public sealed record CampaignChatAllyGroup(Guid Id, string Name);

/// <summary>
/// Destination for a chat message.
/// </summary>
/// <param name="Kind">The audience kind.</param>
/// <param name="TargetUserId">The other member for a direct message.</param>
/// <param name="TargetFactionId">The faction for a faction message.</param>
/// <param name="TargetAllyGroupId">The ally group for an ally-group message.</param>
/// <param name="TargetLabel">A snapshot label for the private channel.</param>
public sealed record ChatChannel(
    ChatChannelKind Kind,
    Guid? TargetUserId = null,
    Guid? TargetFactionId = null,
    Guid? TargetAllyGroupId = null,
    string? TargetLabel = null)
{
    /// <summary>The public campaign channel, including game-log facts.</summary>
    public static ChatChannel Public { get; } = new(ChatChannelKind.Public);
}

/// <summary>
/// Validates and appends campaign chat messages, including private channels.
/// </summary>
public static class CampaignChatRules
{
    /// <summary>Maximum length of a chat message after trimming.</summary>
    public const int MessageMaxLength = 2000;

    /// <summary>Originator label for campaign-generated log facts.</summary>
    public const string CampaignOriginator = "Campaign";

    /// <summary>
    /// Posts a chat message from a current campaign member.
    /// </summary>
    public static bool TryPost(
        CampaignPlayState state,
        Guid userId,
        string? message,
        IReadOnlyList<CampaignChatMember> members,
        DateTimeOffset utcNow,
        [NotNullWhen(true)] out CampaignPlayState? next,
        [NotNullWhen(false)] out DomainError? error,
        ChatChannel? channel = null,
        IReadOnlyList<CampaignChatMembership>? memberships = null,
        IReadOnlyList<CampaignChatFaction>? factions = null,
        IReadOnlyList<CampaignChatAllyGroup>? allyGroups = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(members);
        next = null;
        error = null;
        var destination = channel ?? ChatChannel.Public;

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

        if (!TryResolveChannel(
                destination,
                userId,
                members,
                factions ?? [],
                allyGroups ?? [],
                out var resolved,
                out error))
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
            actor.DisplayName,
            resolved.Kind,
            resolved.TargetUserId,
            resolved.TargetFactionId,
            resolved.TargetAllyGroupId,
            resolved.TargetLabel));
        return true;
    }

    /// <summary>
    /// Returns whether <paramref name="viewerUserId"/> may see <paramref name="entry"/>.
    /// Private chats are omitted unless the viewer is a participant or an administrator in debug mode.
    /// Campaign managers do not receive other members' private chats.
    /// </summary>
    public static bool CanView(
        PlayLogEntry entry,
        Guid viewerUserId,
        IReadOnlyList<CampaignChatMembership> memberships,
        bool inspectPrivateLogs)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(memberships);
        if (!entry.IsPrivateChat)
        {
            return true;
        }

        if (inspectPrivateLogs)
        {
            return true;
        }

        return AudienceUserIds(entry, memberships).Contains(viewerUserId);
    }

    /// <summary>
    /// Members who may read a chat entry, including the sender.
    /// </summary>
    public static IReadOnlySet<Guid> AudienceUserIds(
        PlayLogEntry entry,
        IReadOnlyList<CampaignChatMembership> memberships)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(memberships);
        var ids = new HashSet<Guid>();
        if (entry.ActorUserId is { } actor)
        {
            ids.Add(actor);
        }

        if (!entry.IsPrivateChat)
        {
            foreach (var membership in memberships)
            {
                ids.Add(membership.UserId);
            }

            return ids;
        }

        switch (entry.ChatChannelKind)
        {
            case ChatChannelKind.Direct:
                if (entry.ChatTargetUserId is { } target)
                {
                    ids.Add(target);
                }

                break;
            case ChatChannelKind.Faction:
                foreach (var membership in memberships)
                {
                    if (membership.FactionId == entry.ChatTargetFactionId)
                    {
                        ids.Add(membership.UserId);
                    }
                }

                break;
            case ChatChannelKind.AllyGroup:
                foreach (var membership in memberships)
                {
                    if (membership.AllyGroupId == entry.ChatTargetAllyGroupId)
                    {
                        ids.Add(membership.UserId);
                    }
                }

                break;
        }

        return ids;
    }

    /// <summary>
    /// Members tagged in <paramref name="text"/> who currently belong to the campaign.
    /// </summary>
    public static IReadOnlyList<CampaignChatMember> ResolveMentions(
        string text,
        IReadOnlyList<CampaignChatMember> members)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(members);
        var mentioned = new List<CampaignChatMember>();
        var seen = new HashSet<Guid>();
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
                index++;
                continue;
            }

            foreach (var member in members)
            {
                if (seen.Contains(member.UserId))
                {
                    continue;
                }

                if (member.Username.Equals(match, StringComparison.OrdinalIgnoreCase)
                    || member.DisplayName.Equals(match, StringComparison.OrdinalIgnoreCase))
                {
                    mentioned.Add(member);
                    seen.Add(member.UserId);
                    break;
                }
            }

            index += 1 + match.Length;
        }

        return mentioned;
    }

    /// <summary>
    /// Returns whether <paramref name="atIndex"/> starts a mention in <paramref name="text"/>.
    /// </summary>
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

    private static bool TryResolveChannel(
        ChatChannel channel,
        Guid actorUserId,
        IReadOnlyList<CampaignChatMember> members,
        IReadOnlyList<CampaignChatFaction> factions,
        IReadOnlyList<CampaignChatAllyGroup> allyGroups,
        out ChatChannel resolved,
        [NotNullWhen(false)] out DomainError? error)
    {
        resolved = channel;
        error = null;
        switch (channel.Kind)
        {
            case ChatChannelKind.Public:
                resolved = ChatChannel.Public;
                return true;
            case ChatChannelKind.Direct:
                if (channel.TargetUserId is null || channel.TargetUserId == actorUserId)
                {
                    error = new DomainError(
                        "chat.channel.invalid",
                        "Choose another campaign member to send a private message.",
                        "channel");
                    return false;
                }

                var target = members.FirstOrDefault(member => member.UserId == channel.TargetUserId);
                if (target is null)
                {
                    error = new DomainError(
                        "chat.channel.invalid",
                        "You can only send a private message to a current campaign member.",
                        "channel");
                    return false;
                }

                resolved = new ChatChannel(ChatChannelKind.Direct, target.UserId, TargetLabel: target.DisplayName);
                return true;
            case ChatChannelKind.Faction:
                var faction = factions.FirstOrDefault(item => item.Id == channel.TargetFactionId);
                if (faction is null)
                {
                    error = new DomainError(
                        "chat.channel.invalid",
                        "Choose a faction in this campaign.",
                        "channel");
                    return false;
                }

                resolved = new ChatChannel(ChatChannelKind.Faction, TargetFactionId: faction.Id, TargetLabel: faction.Name);
                return true;
            case ChatChannelKind.AllyGroup:
                var group = allyGroups.FirstOrDefault(item => item.Id == channel.TargetAllyGroupId);
                if (group is null)
                {
                    error = new DomainError(
                        "chat.channel.invalid",
                        "Choose an ally group in this campaign.",
                        "channel");
                    return false;
                }

                resolved = new ChatChannel(
                    ChatChannelKind.AllyGroup,
                    TargetAllyGroupId: group.Id,
                    TargetLabel: group.Name);
                return true;
            default:
                error = new DomainError("chat.channel.invalid", "Choose a chat channel.", "channel");
                return false;
        }
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
