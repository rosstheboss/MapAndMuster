using MapAndMuster.Application.Campaigns;
using MapAndMuster.Application.Common;
using MapAndMuster.Application.Identity;
using MapAndMuster.Application.Maps;
using MapAndMuster.Application.News;
using MapAndMuster.Application.Notifications;
using MapAndMuster.Application.Ports;
using MapAndMuster.Domain.Identity;
using MapAndMuster.Domain.News;
using MapAndMuster.Domain.Play;

namespace MapAndMuster.Backend.UnitTests.News;

public sealed class NewsHandlersTests
{
    [Fact]
    public async Task SaveRejectsNonAdministrators()
    {
        var handler = new SaveNewsArticleHandler(new FakeNewsStore(), new FixedClock());
        var result = await handler.HandleAsync(
            new SaveNewsArticleCommand
            {
                UserId = Guid.NewGuid(),
                IsAdministrator = false,
                Title = "Season opening",
                BodyMarkdown = "Welcome to the new season.",
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CampaignForbidden, result.ErrorCode);
    }

    [Fact]
    public async Task SaveCreatesAnArticleForAdministrators()
    {
        var store = new FakeNewsStore();
        var handler = new SaveNewsArticleHandler(store, new FixedClock());
        var result = await handler.HandleAsync(
            new SaveNewsArticleCommand
            {
                UserId = Guid.NewGuid(),
                IsAdministrator = true,
                Title = "Season opening",
                BodyMarkdown = "Welcome to the new season.",
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Season opening", result.Value.Title);
        Assert.Contains("<p>", result.Value.BodyHtml, StringComparison.Ordinal);
        Assert.Equal(1, (await new GetNewsPageHandler(store).HandleAsync(1, CancellationToken.None)).Value!.TotalPages);
        Assert.Single((await new GetNewsPageHandler(store).HandleAsync(1, CancellationToken.None)).Value!.Articles);
    }

    [Fact]
    public async Task NewsPagesTwoArticlesAtATime()
    {
        var store = new FakeNewsStore();
        var handler = new SaveNewsArticleHandler(store, new FixedClock());
        for (var index = 1; index <= 3; index++)
        {
            var result = await handler.HandleAsync(
                new SaveNewsArticleCommand
                {
                    UserId = Guid.NewGuid(),
                    IsAdministrator = true,
                    Title = $"Bulletin {index}",
                    BodyMarkdown = $"Body {index}.",
                },
                CancellationToken.None);
            Assert.True(result.IsSuccess);
        }

        var first = await new GetNewsPageHandler(store).HandleAsync(1, CancellationToken.None);
        Assert.True(first.IsSuccess);
        Assert.Equal(2, first.Value!.TotalPages);
        Assert.Equal(2, first.Value.Articles.Count);
        var second = await new GetNewsPageHandler(store).HandleAsync(2, CancellationToken.None);
        Assert.True(second.IsSuccess);
        Assert.Single(second.Value!.Articles);
    }
}

public sealed class HomeBoardHandlerTests
{
    [Fact]
    public async Task EmptyBoardWhenThereAreNoNoticesOrLiveActions()
    {
        var accounts = new FakeProfileStore();
        var handler = new GetHomeBoardHandler(
            new FakeNoticeStore(),
            new EmptyCampaignStore(),
            accounts,
            new FixedClock());

        var result = await handler.HandleAsync(accounts.User.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task StoredNoticeIdsUseDashedGuidFormat()
    {
        var accounts = new FakeProfileStore();
        var noticeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var store = new FakeNoticeStore();
        store.Unread.Add(new UserNotification
        {
            Id = noticeId,
            UserId = accounts.User.Id,
            Kind = "CampaignChat",
            Title = "New mention",
            Body = "You were mentioned.",
            Path = "/campaigns/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
            CreatedUtc = new DateTimeOffset(2026, 8, 16, 12, 0, 0, TimeSpan.Zero),
            DedupeKey = "chat:1",
        });
        var handler = new GetHomeBoardHandler(store, new EmptyCampaignStore(), accounts, new FixedClock());

        var result = await handler.HandleAsync(accounts.User.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value!);
        Assert.Equal(noticeId.ToString("D"), item.Id);
    }

    [Fact]
    public async Task MarkAllReadMarksStoredNotices()
    {
        var store = new FakeNoticeStore();
        var handler = new MarkAllNotificationsReadHandler(store, new FixedClock());

        var result = await handler.HandleAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, store.MarkAllCalls);
    }
}

file sealed class FixedClock : IClock
{
    public DateTimeOffset UtcNow { get; } = new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);
}

file sealed class FakeNewsStore : INewsStore
{
    private readonly List<NewsArticle> _articles = [];

    public Task<NewsPage> GetPageAsync(int page, CancellationToken cancellationToken)
    {
        var total = _articles.Count;
        var pageSize = NewsArticleRules.HomePageSize;
        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);
        var normalized = page < 1 ? 1 : page;
        if (totalPages > 0 && normalized > totalPages)
        {
            normalized = totalPages;
        }

        var articles = _articles
            .OrderByDescending(item => item.PublishedUtc)
            .ThenByDescending(item => item.Id)
            .Skip(Math.Max(0, (normalized - 1) * pageSize))
            .Take(pageSize)
            .ToArray();
        return Task.FromResult(new NewsPage
        {
            Page = total == 0 ? 1 : normalized,
            TotalPages = totalPages,
            Articles = articles,
        });
    }

    public Task<NewsArticle?> FindByIdAsync(Guid articleId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_articles.FirstOrDefault(item => item.Id == articleId));
    }

    public Task<NewsArticle> AddAsync(NewsArticle article, CancellationToken cancellationToken)
    {
        _articles.Add(article);
        return Task.FromResult(article);
    }

    public Task<NewsArticle?> UpdateAsync(NewsArticle article, CancellationToken cancellationToken)
    {
        return Task.FromResult<NewsArticle?>(article);
    }

    public Task<bool> DeleteAsync(Guid articleId, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }
}

file sealed class FakeNoticeStore : IUserNotificationStore
{
    public List<UserNotification> Unread { get; } = [];

    public int MarkAllCalls { get; private set; }

    public Task<bool> TryAddAsync(NewUserNotification notification, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<UserNotification>> ListUnreadAsync(Guid userId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<UserNotification>>([.. Unread]);
    }

    public Task<bool> MarkReadAsync(Guid notificationId, Guid userId, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }

    public Task<int> MarkAllReadAsync(Guid userId, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        MarkAllCalls++;
        return Task.FromResult(Unread.Count);
    }
}

file sealed class EmptyCampaignStore : ICampaignStore
{
    public Task<StoredCampaign> AddAsync(StoredCampaign campaign, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task<StoredCampaign?> FindByIdAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        return Task.FromResult<StoredCampaign?>(null);
    }

    public Task<IReadOnlyList<StoredCampaign>> ListForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<StoredCampaign>>([]);
    }

    public Task<IReadOnlyList<StoredCampaign>> ListDiscoverableAsync(
        Guid userId,
        bool isAdministrator,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<StoredCampaign>>([]);
    }

    public Task<UpdateStoredCampaignOutcome> UpdateAsync(StoredCampaign campaign, int expectedRevision, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task<bool> DeleteAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }

    public Task<bool> IsStorageKeyInUseAsync(string storageKey, Guid? excludingCampaignId, CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }

    public Task<UpdateStoredCampaignOutcome> UpdateMapGraphAsync(
        Guid campaignId,
        StoredMapGraph graph,
        int expectedRevision,
        DateTimeOffset updatedUtc,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task<UpdateStoredCampaignOutcome> UpdatePlayStateAsync(
        Guid campaignId,
        CampaignPlayState playState,
        StoredMapGraph? mapGraph,
        DateTimeOffset endsUtc,
        int roundCount,
        int expectedRevision,
        DateTimeOffset updatedUtc,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }
}

file sealed class FakeProfileStore : IUserAccountStore
{
    public UserAccount User { get; } = new()
    {
        Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        Email = "ada@example.test",
        Username = "ada",
        FirstName = "Ada",
        LastName = "Lovelace",
        City = "Halifax",
        Country = "Canada",
        DisplayNameMode = DisplayNameMode.Username,
        CreatedUtc = new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero),
        UpdatedUtc = new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero),
        ProfileRevision = 1,
        EmailConfirmed = true,
    };

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
        return Task.FromResult<UserAccount?>(userId == User.Id ? User : null);
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
}
