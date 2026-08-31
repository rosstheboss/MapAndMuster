using MapAndMuster.Domain.Play;

namespace MapAndMuster.Backend.UnitTests.Play;

public sealed class CampaignChatRulesTests
{
    private static readonly Guid PlayerOne = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid PlayerTwo = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Outsider = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 20, 45, 23, TimeSpan.FromHours(-4));

    [Fact]
    public void MemberCanPostAPublicChatMessage()
    {
        Assert.True(CampaignChatRules.TryPost(
            CampaignPlayState.Empty,
            PlayerOne,
            "Hey, everybody! This is a message to all of you.",
            Members(),
            Now,
            out var next,
            out _));
        var entry = Assert.Single(next!.Log);
        Assert.Equal(PlayLogKind.PlayerChat, entry.Kind);
        Assert.Equal(ChatChannelKind.Public, entry.ChatChannelKind);
        Assert.False(entry.IsPrivateChat);
        Assert.Equal(PlayerOne, entry.ActorUserId);
        Assert.Equal("northplayer", entry.ActorDisplayName);
        Assert.Equal("Hey, everybody! This is a message to all of you.", entry.Message);
    }

    [Fact]
    public void DirectMessageIsVisibleOnlyToTheTwoMembers()
    {
        Assert.True(CampaignChatRules.TryPost(
            CampaignPlayState.Empty,
            PlayerOne,
            "Secret plan",
            Members(),
            Now,
            out var next,
            out _,
            new ChatChannel(ChatChannelKind.Direct, PlayerTwo),
            Memberships(),
            Factions(),
            AllyGroups()));
        var entry = Assert.Single(next!.Log);
        Assert.True(entry.IsPrivateChat);
        Assert.Equal("southplayer", entry.ChatTargetLabel);
        Assert.True(CampaignChatRules.CanView(entry, PlayerOne, Memberships(), inspectPrivateLogs: false));
        Assert.True(CampaignChatRules.CanView(entry, PlayerTwo, Memberships(), inspectPrivateLogs: false));
        Assert.False(CampaignChatRules.CanView(entry, Outsider, Memberships(), inspectPrivateLogs: false));
        Assert.True(CampaignChatRules.CanView(entry, Outsider, Memberships(), inspectPrivateLogs: true));
    }

    [Fact]
    public void FactionChatIsVisibleToThatFactionAndTheSender()
    {
        var northFaction = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        Assert.True(CampaignChatRules.TryPost(
            CampaignPlayState.Empty,
            PlayerOne,
            "North only",
            Members(),
            Now,
            out var next,
            out _,
            new ChatChannel(ChatChannelKind.Faction, TargetFactionId: northFaction),
            Memberships(northFaction, southFaction: Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")),
            Factions(northFaction, Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")),
            AllyGroups()));
        var entry = Assert.Single(next!.Log);
        Assert.True(CampaignChatRules.CanView(entry, PlayerOne, Memberships(northFaction, Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")), false));
        Assert.False(CampaignChatRules.CanView(entry, PlayerTwo, Memberships(northFaction, Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")), false));
    }

    [Fact]
    public void MentionsAreResolvedForNotifications()
    {
        var mentioned = CampaignChatRules.ResolveMentions("Hi @southplayer", Members());
        Assert.Equal(PlayerTwo, Assert.Single(mentioned).UserId);
    }

    [Fact]
    public void UnreadCountsMentionsAndPrivateMessagesAfterLastRead()
    {
        Assert.True(CampaignChatRules.TryPost(
            CampaignPlayState.Empty,
            PlayerOne,
            "Hi @southplayer",
            Members(),
            Now.AddMinutes(-10),
            out var mentioned,
            out _));
        Assert.True(CampaignChatRules.TryPost(
            mentioned!,
            PlayerOne,
            "Hello everyone",
            Members(),
            Now.AddMinutes(-8),
            out var publicChat,
            out _));
        Assert.True(CampaignChatRules.TryPost(
            publicChat!,
            PlayerOne,
            "Secret plan",
            Members(),
            Now.AddMinutes(-5),
            out var withPrivate,
            out _,
            new ChatChannel(ChatChannelKind.Direct, PlayerTwo),
            Memberships(),
            Factions(),
            AllyGroups()));
        Assert.True(CampaignChatRules.TryPost(
            withPrivate!,
            PlayerTwo,
            "I will reply later",
            Members(),
            Now.AddMinutes(-1),
            out var withOwn,
            out _));

        var unread = CampaignChatRules.CountUnread(withOwn!.Log, PlayerTwo, lastReadUtc: null, Members());
        Assert.Equal(1, unread.MentionCount);
        Assert.Equal(1, unread.PrivateCount);

        var afterRead = CampaignChatRules.CountUnread(withOwn.Log, PlayerTwo, Now.AddMinutes(-6), Members());
        Assert.Equal(0, afterRead.MentionCount);
        Assert.Equal(1, afterRead.PrivateCount);

        var ownUnread = CampaignChatRules.CountUnread(withOwn.Log, PlayerOne, lastReadUtc: null, Members());
        Assert.Equal(0, ownUnread.MentionCount);
        Assert.Equal(0, ownUnread.PrivateCount);
    }

    [Fact]
    public void OutsiderCannotPost()
    {
        Assert.False(CampaignChatRules.TryPost(
            CampaignPlayState.Empty,
            Outsider,
            "Hello",
            Members(),
            Now,
            out _,
            out var error));
        Assert.Equal("chat.forbidden", error!.Code);
    }

    [Fact]
    public void MentionMustBeACurrentMember()
    {
        Assert.False(CampaignChatRules.TryPost(
            CampaignPlayState.Empty,
            PlayerOne,
            "Hi @stranger",
            Members(),
            Now,
            out _,
            out var error));
        Assert.Equal("chat.mention.unknown", error!.Code);
    }

    [Fact]
    public void UsernameAndDisplayNameMentionsAreAccepted()
    {
        Assert.True(CampaignChatRules.TryPost(
            CampaignPlayState.Empty,
            PlayerOne,
            "Hi @southplayer and @Ada Lovelace",
            Members(southDisplayName: "Ada Lovelace"),
            Now,
            out var next,
            out _));
        Assert.Equal("Hi @southplayer and @Ada Lovelace", Assert.Single(next!.Log).Message);
    }

    [Fact]
    public void EscapedAtSignIsNotAMention()
    {
        Assert.True(CampaignChatRules.TryPost(
            CampaignPlayState.Empty,
            PlayerOne,
            @"See \@stranger for notes",
            Members(),
            Now,
            out var next,
            out _));
        Assert.Equal(@"See \@stranger for notes", Assert.Single(next!.Log).Message);
    }

    [Fact]
    public void EmailAddressesAreNotMentions()
    {
        Assert.True(CampaignChatRules.TryPost(
            CampaignPlayState.Empty,
            PlayerOne,
            "Write ada@example.test",
            Members(),
            Now,
            out _,
            out _));
    }

    [Fact]
    public void EmptyAndOversizedMessagesAreRejected()
    {
        Assert.False(CampaignChatRules.TryPost(
            CampaignPlayState.Empty, PlayerOne, "   ", Members(), Now, out _, out var empty));
        Assert.Equal("chat.message.required", empty!.Code);

        Assert.False(CampaignChatRules.TryPost(
            CampaignPlayState.Empty,
            PlayerOne,
            new string('a', CampaignChatRules.MessageMaxLength + 1),
            Members(),
            Now,
            out _,
            out var tooLong));
        Assert.Equal("chat.message.too_long", tooLong!.Code);
    }

    [Fact]
    public void LaunchPreservesEarlierChatInTheLog()
    {
        Assert.True(CampaignChatRules.TryPost(
            CampaignPlayState.Empty,
            PlayerOne,
            "Ready to play",
            Members(),
            Now,
            out var chatting,
            out _));
        var seeded = CampaignPlayRules.Seed(
            chatting!,
            new PlayMap(
                [
                    new PlayTerritory(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), 1, null, null, null, null, StructureCondition.Operational),
                ],
                []),
            CreateSchedule(Now.AddHours(-1)),
            [new PlayerFactionAssignment(PlayerOne, Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"))],
            Now);

        Assert.Contains(seeded.State.Log, item => item.Kind == PlayLogKind.PlayerChat && item.Message == "Ready to play");
        Assert.Contains(seeded.State.Log, item => item.Kind == PlayLogKind.CampaignStarted);
    }

    private static IReadOnlyList<CampaignChatMember> Members(string southDisplayName = "southplayer")
    {
        return
        [
            new CampaignChatMember(PlayerOne, "northplayer", "northplayer"),
            new CampaignChatMember(PlayerTwo, "southplayer", southDisplayName),
        ];
    }

    private static IReadOnlyList<CampaignChatMembership> Memberships(
        Guid? northFaction = null,
        Guid? southFaction = null)
    {
        return
        [
            new CampaignChatMembership(PlayerOne, northFaction, null),
            new CampaignChatMembership(PlayerTwo, southFaction, null),
        ];
    }

    private static IReadOnlyList<CampaignChatFaction> Factions(Guid? northId = null, Guid? southId = null)
    {
        return
        [
            new CampaignChatFaction(northId ?? Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "North", null),
            new CampaignChatFaction(southId ?? Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), "South", null),
        ];
    }

    private static IReadOnlyList<CampaignChatAllyGroup> AllyGroups()
    {
        return [];
    }

    private static MapAndMuster.Domain.Campaigns.CampaignSchedule CreateSchedule(DateTimeOffset startsUtc)
    {
        var succeeded = MapAndMuster.Domain.Campaigns.CampaignSetupRules.TryCreate(
            "Border War",
            null,
            8,
            false,
            null,
            false,
            true,
            0,
            [new MapAndMuster.Domain.Campaigns.FactionInput { Name = "North" }, new MapAndMuster.Domain.Campaigns.FactionInput { Name = "South" }],
            null,
            null,
            new MapAndMuster.Domain.Campaigns.CampaignScheduleInput
            {
                TimeZoneId = "UTC",
                StartsAtLocal = "2026-08-15T12:00",
                RoundCount = 3,
                RoundLengthAmount = 10,
                RoundLengthUnit = "Minutes",
                Phases =
                [
                    new MapAndMuster.Domain.Campaigns.RoundPhaseInput { Kind = "Action", DurationAmount = 6, DurationUnit = "Minutes" },
                    new MapAndMuster.Domain.Campaigns.RoundPhaseInput { Kind = "Battle", DurationAmount = 4, DurationUnit = "Minutes" },
                ],
            },
            out var setup,
            out _,
            out _);
        Assert.True(succeeded);
        return new MapAndMuster.Domain.Campaigns.CampaignSchedule(
            setup!.Schedule.TimeZone,
            startsUtc,
            startsUtc.AddHours(1),
            setup.Schedule.RoundCount,
            setup.Schedule.RoundLength,
            setup.Schedule.Phases);
    }
}
