using MapAndMuster.Application.Campaigns;
using MapAndMuster.Domain.Play;

namespace MapAndMuster.Application.Play;

/// <summary>
/// Builds chat audiences and compose targets from campaign membership.
/// </summary>
internal static class CampaignChatContext
{
    /// <summary>
    /// Administrators may inspect private chat only while they are the active debug actor.
    /// Campaign managers never receive other members' private chats.
    /// </summary>
    public static bool CanInspectPrivateChat(bool isAdministrator, Guid viewerUserId, CampaignPlayState? play)
    {
        return isAdministrator && play?.DebugActorUserId == viewerUserId;
    }

    public static IReadOnlyList<CampaignChatMembership> Memberships(StoredCampaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        return
        [
            .. campaign.Memberships.Select(member => new CampaignChatMembership(
                member.UserId,
                member.FactionId,
                AllyGroupIdFor(campaign, member.FactionId))),
        ];
    }

    public static IReadOnlyList<CampaignChatFaction> Factions(StoredCampaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        return
        [
            .. campaign.Factions.Select(faction => new CampaignChatFaction(
                faction.Id,
                faction.Name,
                AllyGroupIdFor(campaign, faction.Id))),
        ];
    }

    public static IReadOnlyList<CampaignChatAllyGroup> AllyGroups(StoredCampaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        return [.. campaign.AllyGroups.Select(static group => new CampaignChatAllyGroup(group.Id, group.Name))];
    }

    public static IReadOnlyList<ChatChannelDetail> Channels(
        StoredCampaign campaign,
        Guid viewerUserId,
        IReadOnlyList<CampaignLogMemberDetail> members)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        ArgumentNullException.ThrowIfNull(members);
        var channels = new List<ChatChannelDetail>
        {
            new() { Kind = nameof(ChatChannelKind.Public), Label = "Everyone" },
        };

        foreach (var member in members.Where(item => item.UserId != viewerUserId).OrderBy(static item => item.DisplayName))
        {
            channels.Add(new ChatChannelDetail
            {
                Kind = nameof(ChatChannelKind.Direct),
                TargetId = member.UserId,
                Label = member.DisplayName,
            });
        }

        foreach (var faction in campaign.Factions.OrderBy(static item => item.Name))
        {
            channels.Add(new ChatChannelDetail
            {
                Kind = nameof(ChatChannelKind.Faction),
                TargetId = faction.Id,
                Label = faction.Name,
            });
        }

        foreach (var group in campaign.AllyGroups.OrderBy(static item => item.Name))
        {
            channels.Add(new ChatChannelDetail
            {
                Kind = nameof(ChatChannelKind.AllyGroup),
                TargetId = group.Id,
                Label = group.Name,
            });
        }

        return channels;
    }

    public static bool TryParseChannel(string? kind, Guid? targetId, out ChatChannel channel, out string? error)
    {
        channel = ChatChannel.Public;
        error = null;
        if (string.IsNullOrWhiteSpace(kind)
            || kind.Equals(nameof(ChatChannelKind.Public), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!Enum.TryParse<ChatChannelKind>(kind, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
        {
            error = "Choose a chat channel.";
            return false;
        }

        channel = parsed switch
        {
            ChatChannelKind.Direct => new ChatChannel(ChatChannelKind.Direct, TargetUserId: targetId),
            ChatChannelKind.Faction => new ChatChannel(ChatChannelKind.Faction, TargetFactionId: targetId),
            ChatChannelKind.AllyGroup => new ChatChannel(ChatChannelKind.AllyGroup, TargetAllyGroupId: targetId),
            _ => ChatChannel.Public,
        };
        return true;
    }

    private static Guid? AllyGroupIdFor(StoredCampaign campaign, Guid? factionId)
    {
        if (factionId is null)
        {
            return null;
        }

        var faction = campaign.Factions.FirstOrDefault(item => item.Id == factionId);
        if (faction?.AllyGroupName is null)
        {
            return null;
        }

        return campaign.AllyGroups
            .FirstOrDefault(group => string.Equals(group.Name, faction.AllyGroupName, StringComparison.Ordinal))
            ?.Id;
    }
}
