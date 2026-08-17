using Campaign.Domain.Chat;
using Campaign.Domain.Play;

namespace Campaign.Backend.UnitTests.Chat;

public sealed class SiteChatRulesTests
{
    private static readonly Guid PlayerOne = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid PlayerTwo = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid PlayerThree = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly DateTimeOffset Now = new(2026, 8, 17, 19, 45, 0, TimeSpan.Zero);

    [Fact]
    public void SignedInUserCanPostPublicEnglishChat()
    {
        Assert.True(SiteChatRules.TryPost(
            PlayerOne,
            "Hello @southplayer",
            null,
            Members(),
            Now,
            isAdministrator: false,
            sendAsAdministrator: false,
            targetUserId: null,
            out var posted,
            out _));
        Assert.Equal(SiteChatKind.Player, posted!.Kind);
        Assert.Equal(ChatLanguage.English, posted.Language);
        Assert.Equal(PlayerOne, posted.AuthorUserId);
        Assert.Equal("northplayer", posted.AuthorDisplayName);
        Assert.Null(posted.TargetUserId);
    }

    [Fact]
    public void UnknownSiteMentionIsRejected()
    {
        Assert.False(SiteChatRules.TryPost(
            PlayerOne,
            "Hi @stranger",
            "English",
            Members(),
            Now,
            false,
            false,
            null,
            out _,
            out var error));
        Assert.Equal("chat.mention.unknown", error!.Code);
        Assert.Contains("account", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProhibitedLanguageIsRejected()
    {
        Assert.False(SiteChatRules.TryPost(
            PlayerOne,
            "This is shit",
            "English",
            Members(),
            Now,
            false,
            false,
            null,
            out _,
            out var error));
        Assert.Equal("message.prohibited", error!.Code);
    }

    [Fact]
    public void UnsupportedLanguageIsRejected()
    {
        Assert.False(SiteChatRules.TryPost(
            PlayerOne,
            "Hola",
            "Klingon",
            Members(),
            Now,
            false,
            false,
            null,
            out _,
            out var error));
        Assert.Equal("sitechat.language.invalid", error!.Code);
    }

    [Fact]
    public void PlayerCannotDirectAMessageAtOnePerson()
    {
        Assert.False(SiteChatRules.TryPost(
            PlayerOne,
            "Secret",
            "English",
            Members(),
            Now,
            false,
            false,
            PlayerTwo,
            out _,
            out var error));
        Assert.Equal("sitechat.channel.invalid", error!.Code);
    }

    [Fact]
    public void NonAdminCannotSendAdminMessage()
    {
        Assert.False(SiteChatRules.TryPost(
            PlayerOne,
            "Announcement",
            "English",
            Members(),
            Now,
            isAdministrator: false,
            sendAsAdministrator: true,
            targetUserId: null,
            out _,
            out var error));
        Assert.Equal("sitechat.admin.forbidden", error!.Code);
    }

    [Fact]
    public void AdminCanAnnounceToEveryone()
    {
        Assert.True(SiteChatRules.TryPost(
            PlayerOne,
            "Please read the news.",
            "English",
            Members(),
            Now,
            isAdministrator: true,
            sendAsAdministrator: true,
            targetUserId: null,
            out var posted,
            out _));
        Assert.Equal(SiteChatKind.Admin, posted!.Kind);
        Assert.Null(posted.TargetUserId);
    }

    [Fact]
    public void AdminCanDirectAPublicAnnouncementAtOnePerson()
    {
        Assert.True(SiteChatRules.TryPost(
            PlayerOne,
            "Please update your profile.",
            "French",
            Members(),
            Now,
            isAdministrator: true,
            sendAsAdministrator: true,
            targetUserId: PlayerTwo,
            out var posted,
            out _));
        Assert.Equal(SiteChatKind.Admin, posted!.Kind);
        Assert.Equal(ChatLanguage.French, posted.Language);
        Assert.Equal(PlayerTwo, posted.TargetUserId);
        Assert.Equal("southplayer", posted.TargetUsername);
    }

    [Fact]
    public void BlockHidesPlayerMessagesBothWaysAndLeavesAdminVisible()
    {
        var blocks = new[] { new SiteChatBlock(PlayerOne, PlayerTwo) };
        var hiddenForOne = SiteChatRules.HiddenAuthorIds(PlayerOne, blocks);
        var hiddenForTwo = SiteChatRules.HiddenAuthorIds(PlayerTwo, blocks);
        Assert.Contains(PlayerTwo, hiddenForOne);
        Assert.Contains(PlayerOne, hiddenForTwo);
        Assert.DoesNotContain(PlayerThree, hiddenForOne);

        var playerMessage = new SiteChatMessage(
            Guid.NewGuid(),
            Now,
            PlayerTwo,
            "southplayer",
            "southplayer",
            "Hello",
            ChatLanguage.English,
            SiteChatKind.Player,
            null,
            null,
            null);
        var adminMessage = playerMessage with { Kind = SiteChatKind.Admin, AuthorUserId = PlayerTwo };
        Assert.False(SiteChatRules.CanView(playerMessage, PlayerOne, hiddenForOne));
        Assert.True(SiteChatRules.CanView(adminMessage, PlayerOne, hiddenForOne));
        Assert.True(SiteChatRules.CanView(playerMessage, PlayerTwo, hiddenForOne));
    }

    [Fact]
    public void CannotBlockYourself()
    {
        Assert.False(SiteChatRules.TryValidateBlock(PlayerOne, PlayerOne, Members(), out var error));
        Assert.Equal("sitechat.block.self", error!.Code);
    }

    private static IReadOnlyList<CampaignChatMember> Members()
    {
        return
        [
            new CampaignChatMember(PlayerOne, "northplayer", "northplayer"),
            new CampaignChatMember(PlayerTwo, "southplayer", "southplayer"),
            new CampaignChatMember(PlayerThree, "eastplayer", "eastplayer"),
        ];
    }
}
