using Campaign.Application.Chat;
using Campaign.Application.Identity;
using Campaign.Application.Notifications;
using Campaign.Application.Ports;
using Campaign.Domain.Chat;
using Campaign.Domain.Identity;
using Campaign.Domain.Notifications;

namespace Campaign.Backend.UnitTests.Chat;

public sealed class SiteChatHandlerTests
{
    private static readonly Guid Ada = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Bob = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TestUser = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly DateTimeOffset Now = new(2026, 8, 17, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task PostStoresAPublicMessageAndOmitsCampaignFields()
    {
        var chat = new FakeChatStore();
        var accounts = new FakeAccounts();
        var notices = new FakeNoticeStore();
        var handler = CreatePost(chat, accounts, notices);

        var result = await handler.HandleAsync(
            new PostSiteChatCommand
            {
                UserId = Ada,
                IsAdministrator = false,
                Message = "Hello @bob",
                Language = "Spanish",
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var stored = Assert.Single(chat.Messages);
        Assert.Equal("Hello @bob", stored.Body);
        Assert.Equal(ChatLanguage.Spanish, stored.Language);
        Assert.Equal(SiteChatKind.Player, stored.Kind);
        var mention = Assert.Single(notices.Added);
        Assert.Equal(NotificationKind.SiteChatMention, mention.Kind);
        Assert.Null(mention.CampaignId);
        Assert.Equal(SiteChatRules.BoardPath, mention.Path);
        Assert.Contains(result.Value!.Messages, item => item.Body == "Hello @bob");
    }

    [Fact]
    public async Task BlockHidesPlayerMessagesFromBothViewers()
    {
        var chat = new FakeChatStore();
        var accounts = new FakeAccounts();
        var post = CreatePost(chat, accounts, new FakeNoticeStore());
        Assert.True((await post.HandleAsync(
            new PostSiteChatCommand { UserId = Ada, IsAdministrator = false, Message = "From Ada" },
            CancellationToken.None)).IsSuccess);
        Assert.True((await post.HandleAsync(
            new PostSiteChatCommand { UserId = Bob, IsAdministrator = false, Message = "From Bob" },
            CancellationToken.None)).IsSuccess);

        var block = new SetSiteChatBlockHandler(chat, accounts, new GetSiteChatHandler(chat, accounts));
        var blocked = await block.HandleAsync(
            new SetSiteChatBlockCommand { UserId = Ada, IsAdministrator = false, TargetUserId = Bob, Blocked = true },
            CancellationToken.None);
        Assert.True(blocked.IsSuccess);
        Assert.DoesNotContain(blocked.Value!.Messages, item => item.AuthorUserId == Bob);
        Assert.Contains(blocked.Value.BlockedUsers, item => item.UserId == Bob);

        var bobBoard = await new GetSiteChatHandler(chat, accounts).HandleAsync(Bob, false, CancellationToken.None);
        Assert.DoesNotContain(bobBoard.Value!.Messages, item => item.AuthorUserId == Ada);
        Assert.Empty(bobBoard.Value.BlockedUsers);

        var notices = new FakeNoticeStore();
        var mentionAfterBlock = CreatePost(chat, accounts, notices);
        Assert.True((await mentionAfterBlock.HandleAsync(
            new PostSiteChatCommand { UserId = Ada, IsAdministrator = false, Message = "Hello @bob" },
            CancellationToken.None)).IsSuccess);
        Assert.Empty(notices.Added);
    }

    [Fact]
    public async Task AdminBroadcastNotifiesEveryoneElse()
    {
        var chat = new FakeChatStore();
        var accounts = new FakeAccounts();
        var notices = new FakeNoticeStore();
        var handler = CreatePost(chat, accounts, notices);

        var result = await handler.HandleAsync(
            new PostSiteChatCommand
            {
                UserId = Ada,
                IsAdministrator = true,
                Message = "Read the news.",
                SendAsAdministrator = true,
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(SiteChatKind.Admin.ToString(), Assert.Single(result.Value!.Messages).Kind);
        var notice = Assert.Single(notices.Added);
        Assert.Equal(NotificationKind.SiteAdminMessage, notice.Kind);
        Assert.Equal(Bob, notice.UserId);
    }

    [Fact]
    public async Task DirectedAdminMessageNotifiesOnlyTheTarget()
    {
        var chat = new FakeChatStore();
        var accounts = new FakeAccounts();
        var notices = new FakeNoticeStore();
        var handler = CreatePost(chat, accounts, notices);

        var result = await handler.HandleAsync(
            new PostSiteChatCommand
            {
                UserId = Ada,
                IsAdministrator = true,
                Message = "Please update your profile.",
                SendAsAdministrator = true,
                TargetUserId = Bob,
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var stored = Assert.Single(result.Value!.Messages);
        Assert.Equal(SiteChatKind.Admin.ToString(), stored.Kind);
        Assert.Equal(Bob, stored.TargetUserId);
        var notice = Assert.Single(notices.Added);
        Assert.Equal(NotificationKind.SiteAdminMessage, notice.Kind);
        Assert.Equal(Bob, notice.UserId);
    }

    [Fact]
    public async Task TestAccountCannotPostSiteChat()
    {
        var chat = new FakeChatStore();
        var accounts = new FakeAccounts();
        var handler = CreatePost(chat, accounts, new FakeNoticeStore());

        var result = await handler.HandleAsync(
            new PostSiteChatCommand { UserId = TestUser, IsAdministrator = false, Message = "Hello from a test account." },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("sitechat.test_account", result.ErrorCode);
        Assert.Empty(chat.Messages);
    }

    [Fact]
    public async Task SiteChatDoesNotReadCampaignLogs()
    {
        var chat = new FakeChatStore();
        var result = await new GetSiteChatHandler(chat, new FakeAccounts()).HandleAsync(Ada, false, CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Messages);
        Assert.Equal(2, result.Value.MentionableUsers.Count);
    }

    private static PostSiteChatHandler CreatePost(FakeChatStore chat, FakeAccounts accounts, FakeNoticeStore notices)
    {
        var clock = new FakeClock();
        return new PostSiteChatHandler(
            chat,
            accounts,
            clock,
            new GetSiteChatHandler(chat, accounts),
            new SiteChatNotificationPublisher(notices, accounts, new FakeOutbox(), clock));
    }

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeOutbox : IEmailOutbox
    {
        public Task QueueEmailConfirmationAsync(string email, Guid userId, string token, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task QueuePasswordResetAsync(string email, Guid userId, string token, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task QueueUserNoticeAsync(
            string email,
            Guid userId,
            string subject,
            string body,
            string path,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeNoticeStore : IUserNotificationStore
    {
        public List<NewUserNotification> Added { get; } = [];

        public Task<bool> TryAddAsync(NewUserNotification notification, DateTimeOffset utcNow, CancellationToken cancellationToken)
        {
            Added.Add(notification);
            return Task.FromResult(true);
        }

        public Task<IReadOnlyList<UserNotification>> ListUnreadAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<UserNotification>>([]);
        }

        public Task<bool> MarkReadAsync(Guid notificationId, Guid userId, DateTimeOffset utcNow, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }

    private sealed class FakeChatStore : ISiteChatStore
    {
        public List<SiteChatMessage> Messages { get; } = [];

        public List<SiteChatBlock> Blocks { get; } = [];

        public Task<IReadOnlyList<SiteChatMessage>> ListRecentAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<SiteChatMessage>>([.. Messages.TakeLast(SiteChatRules.RecentMessageLimit)]);
        }

        public Task AddAsync(SiteChatMessage message, CancellationToken cancellationToken)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<SiteChatBlock>> ListBlocksAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<SiteChatBlock>>(Blocks);
        }

        public Task SetBlockAsync(Guid blockerUserId, Guid blockedUserId, bool blocked, CancellationToken cancellationToken)
        {
            Blocks.RemoveAll(item => item.BlockerUserId == blockerUserId && item.BlockedUserId == blockedUserId);
            if (blocked)
            {
                Blocks.Add(new SiteChatBlock(blockerUserId, blockedUserId));
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeAccounts : IUserAccountStore
    {
        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<bool> UsernameExistsAsync(string username, Guid? userIdToIgnore, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<CreateLocalAccountOutcome> CreateLocalAccountAsync(CreateLocalAccountRequest request, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<CreateLocalAccountOutcome> CreateExternalAccountAsync(CreateExternalAccountRequest request, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<UserAccount?> FindByIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult<UserAccount?>(Account(userId));
        }

        public Task<UserAccount?> FindByUsernameAsync(string username, CancellationToken cancellationToken)
        {
            return Task.FromResult<UserAccount?>(null);
        }

        public Task<UpdateProfileOutcome> UpdateProfileAsync(UpdateStoredProfileRequest request, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<ChangePasswordOutcome> ChangePasswordAsync(
            Guid userId,
            string currentPassword,
            string newPassword,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<string?> ReplaceAvatarKeyAsync(Guid userId, string? avatarStorageKey, CancellationToken cancellationToken)
        {
            return Task.FromResult<string?>(null);
        }

        public Task<IReadOnlyList<MentionableAccount>> ListMentionableAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<MentionableAccount>>(
            [
                new() { UserId = Ada, Username = "ada", DisplayName = "ada" },
                new() { UserId = Bob, Username = "bob", DisplayName = "bob" },
            ]);
        }

        public Task<IReadOnlyList<UserAccount>> ListAllAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<UserAccount>>([Account(Ada)!, Account(Bob)!]);
        }

        private static UserAccount? Account(Guid userId)
        {
            if (userId == TestUser)
            {
                return new UserAccount
                {
                    Id = userId,
                    Email = "test1@users.invalid",
                    Username = "test1",
                    FirstName = "Test",
                    LastName = "Account",
                    City = "Testville",
                    Country = "Testland",
                    DisplayNameMode = DisplayNameMode.Username,
                    CreatedUtc = Now,
                    UpdatedUtc = Now,
                    ProfileRevision = 1,
                    EmailConfirmed = true,
                    PreferredChatLanguage = "English",
                    IsTestAccount = true,
                    TestAccountNumber = 1,
                };
            }

            if (userId != Ada && userId != Bob)
            {
                return null;
            }

            return new UserAccount
            {
                Id = userId,
                Email = $"{(userId == Ada ? "ada" : "bob")}@example.test",
                Username = userId == Ada ? "ada" : "bob",
                FirstName = "Test",
                LastName = "User",
                City = "Halifax",
                Country = "Canada",
                DisplayNameMode = DisplayNameMode.Username,
                CreatedUtc = Now,
                UpdatedUtc = Now,
                ProfileRevision = 1,
                EmailConfirmed = true,
                PreferredChatLanguage = "English",
            };
        }
    }
}
