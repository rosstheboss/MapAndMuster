using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MapAndMuster.Api.Contracts;
using MapAndMuster.Infrastructure.Email;
using MapAndMuster.Infrastructure.Identity;
using MapAndMuster.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace MapAndMuster.Api.IntegrationTests;

[Collection("api")]
public sealed class SiteChatEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly MapAndMusterApiFactory _factory;

    public SiteChatEndpointTests(MapAndMusterApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SiteChatIsPublicIndependentOfCampaignsAndHonorsBlocks()
    {
        using var ada = _factory.CreateClient();
        var adaName = UniqueName("sada");
        await RegisterConfirmAndLoginAsync(ada, $"{adaName}@example.test", adaName);

        using var bob = _factory.CreateClient();
        var bobName = UniqueName("sbob");
        await RegisterConfirmAndLoginAsync(bob, $"{bobName}@example.test", bobName);

        using var posted = await ada.PostAsJsonAsync(
            "/api/site-chat",
            new { message = $"Hello @{bobName}", language = "English" });
        Assert.Equal(HttpStatusCode.OK, posted.StatusCode);
        var adaBoard = await posted.Content.ReadFromJsonAsync<SiteChatBoardResponse>(JsonOptions);
        Assert.NotNull(adaBoard);
        Assert.Contains(
            adaBoard.Messages,
            item => item.Kind == "Player" && item.Language == "English" && item.Body.Contains($"@{bobName}", StringComparison.Ordinal));

        using var bobPosted = await bob.PostAsJsonAsync("/api/site-chat", new { message = "Hi from Bob", language = "Spanish" });
        Assert.Equal(HttpStatusCode.OK, bobPosted.StatusCode);

        var bobBoard = await bob.GetFromJsonAsync<SiteChatBoardResponse>("/api/site-chat", JsonOptions);
        Assert.NotNull(bobBoard);
        Assert.Contains(bobBoard.Messages, item => item.AuthorUsername == adaName);
        Assert.Contains(bobBoard.Messages, item => item.AuthorUsername == bobName && item.Language == "Spanish");

        var bobNotices = await bob.GetFromJsonAsync<HomeAttentionItemResponse[]>("/api/notifications", JsonOptions);
        Assert.NotNull(bobNotices);
        Assert.Contains(bobNotices, item => item.Kind == "SiteChatMention" && item.Path == "/campaigns/all");

        var bobId = bobBoard.MentionableUsers.Single(user => user.Username == bobName).UserId;
        using var blocked = await ada.PutAsJsonAsync($"/api/site-chat/blocks/{bobId}", new { blocked = true });
        Assert.Equal(HttpStatusCode.OK, blocked.StatusCode);
        var afterBlock = await blocked.Content.ReadFromJsonAsync<SiteChatBoardResponse>(JsonOptions);
        Assert.NotNull(afterBlock);
        Assert.DoesNotContain(afterBlock.Messages, item => item.AuthorUsername == bobName);
        Assert.Contains(afterBlock.BlockedUsers, user => user.Username == bobName);

        var bobAfter = await bob.GetFromJsonAsync<SiteChatBoardResponse>("/api/site-chat", JsonOptions);
        Assert.NotNull(bobAfter);
        Assert.DoesNotContain(bobAfter.Messages, item => item.AuthorUsername == adaName);
        Assert.Empty(bobAfter.BlockedUsers);
    }

    [Fact]
    public async Task UnknownMentionAndProhibitedLanguageAreRejected()
    {
        using var client = _factory.CreateClient();
        var username = UniqueName("schat");
        await RegisterConfirmAndLoginAsync(client, $"{username}@example.test", username);

        using var unknown = await client.PostAsJsonAsync("/api/site-chat", new { message = "Hi @stranger" });
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);

        using var banned = await client.PostAsJsonAsync("/api/site-chat", new { message = "This is shit" });
        Assert.Equal(HttpStatusCode.BadRequest, banned.StatusCode);
    }

    [Fact]
    public async Task AdministratorCanAnnounceToEveryone()
    {
        using var admin = _factory.CreateClient();
        var adminName = UniqueName("sadm");
        await RegisterConfirmAndLoginAsync(admin, $"{adminName}@example.test", adminName);
        await MakeAdministratorAsync(adminName);
        using var login = await admin.PostAsJsonAsync(
            "/api/auth/login",
            new { email = $"{adminName}@example.test", password = ValidPassword });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        using var player = _factory.CreateClient();
        var playerName = UniqueName("splr");
        await RegisterConfirmAndLoginAsync(player, $"{playerName}@example.test", playerName);

        using var posted = await admin.PostAsJsonAsync(
            "/api/site-chat",
            new { message = "Please read the news.", sendAsAdministrator = true });
        Assert.Equal(HttpStatusCode.OK, posted.StatusCode);
        var board = await posted.Content.ReadFromJsonAsync<SiteChatBoardResponse>(JsonOptions);
        Assert.NotNull(board);
        Assert.True(board.CanSendAdminMessages);
        Assert.Contains(board.Messages, item => item.Kind == "Admin" && item.Body == "Please read the news.");

        var notices = await player.GetFromJsonAsync<HomeAttentionItemResponse[]>("/api/notifications", JsonOptions);
        Assert.NotNull(notices);
        Assert.Contains(notices, item => item.Kind == "SiteAdminMessage" && item.Path == "/campaigns/all");
        Assert.DoesNotContain("Please read the news.", notices.Single(item => item.Kind == "SiteAdminMessage").Body);
    }

    [Fact]
    public async Task DirectedAdminMessageNotifiesOnlyTheTargetAndStaysPublic()
    {
        using var admin = _factory.CreateClient();
        var adminName = UniqueName("sadm");
        await RegisterConfirmAndLoginAsync(admin, $"{adminName}@example.test", adminName);
        await MakeAdministratorAsync(adminName);
        using var login = await admin.PostAsJsonAsync(
            "/api/auth/login",
            new { email = $"{adminName}@example.test", password = ValidPassword });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        using var target = _factory.CreateClient();
        var targetName = UniqueName("stgt");
        await RegisterConfirmAndLoginAsync(target, $"{targetName}@example.test", targetName);

        using var bystander = _factory.CreateClient();
        var bystanderName = UniqueName("sbyd");
        await RegisterConfirmAndLoginAsync(bystander, $"{bystanderName}@example.test", bystanderName);

        var board = await admin.GetFromJsonAsync<SiteChatBoardResponse>("/api/site-chat", JsonOptions);
        Assert.NotNull(board);
        var targetId = board.MentionableUsers.Single(user => user.Username == targetName).UserId;

        using var posted = await admin.PostAsJsonAsync(
            "/api/site-chat",
            new { message = "Please update your profile.", sendAsAdministrator = true, targetUserId = targetId });
        Assert.Equal(HttpStatusCode.OK, posted.StatusCode);
        var after = await posted.Content.ReadFromJsonAsync<SiteChatBoardResponse>(JsonOptions);
        Assert.NotNull(after);
        Assert.Contains(
            after.Messages,
            item => item.Kind == "Admin" && item.TargetUsername == targetName && item.Body == "Please update your profile.");

        var bystanderBoard = await bystander.GetFromJsonAsync<SiteChatBoardResponse>("/api/site-chat", JsonOptions);
        Assert.NotNull(bystanderBoard);
        Assert.Contains(bystanderBoard.Messages, item => item.Kind == "Admin" && item.TargetUsername == targetName);

        var targetNotices = await target.GetFromJsonAsync<HomeAttentionItemResponse[]>("/api/notifications", JsonOptions);
        Assert.NotNull(targetNotices);
        Assert.Contains(targetNotices, item => item.Kind == "SiteAdminMessage");

        var bystanderNotices = await bystander.GetFromJsonAsync<HomeAttentionItemResponse[]>("/api/notifications", JsonOptions);
        Assert.NotNull(bystanderNotices);
        Assert.DoesNotContain(bystanderNotices, item => item.Kind == "SiteAdminMessage");
    }

    [Fact]
    public async Task SiteChatDoesNotAppearOnCampaignLogsAndViceVersa()
    {
        using var client = _factory.CreateClient();
        var username = UniqueName("sisol");
        await RegisterConfirmAndLoginAsync(client, $"{username}@example.test", username);

        using var createdResponse = await client.PostAsJsonAsync("/api/campaigns", ValidCampaignBody("Isolation War"));
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<CampaignDetailResponse>(JsonOptions);
        Assert.NotNull(created);

        using var sitePosted = await client.PostAsJsonAsync(
            "/api/site-chat",
            new { message = "Site-only marker 7f3c" });
        Assert.Equal(HttpStatusCode.OK, sitePosted.StatusCode);

        var campaign = await client.GetFromJsonAsync<CampaignDetailResponse>($"/api/campaigns/{created.Id}", JsonOptions);
        Assert.NotNull(campaign);
        Assert.DoesNotContain(campaign.Log, item => item.Summary.Contains("Site-only marker 7f3c", StringComparison.Ordinal));

        using var campaignPosted = await client.PostAsJsonAsync(
            $"/api/campaigns/{created.Id}/chat",
            new PostCampaignChatRequest { Revision = campaign.Revision, Message = "Campaign-only marker 9a2b" });
        Assert.Equal(HttpStatusCode.OK, campaignPosted.StatusCode);

        var siteBoard = await client.GetFromJsonAsync<SiteChatBoardResponse>("/api/site-chat", JsonOptions);
        Assert.NotNull(siteBoard);
        Assert.DoesNotContain(siteBoard.Messages, item => item.Body.Contains("Campaign-only marker 9a2b", StringComparison.Ordinal));
        Assert.Contains(siteBoard.Messages, item => item.Body.Contains("Site-only marker 7f3c", StringComparison.Ordinal));
    }

    [Fact]
    public async Task UnauthenticatedSiteChatIsRejected()
    {
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync("/api/site-chat");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task RegisterConfirmAndLoginAsync(HttpClient client, string email, string username)
    {
        using var registerResponse = await client.PostAsJsonAsync("/api/auth/register", CreateRegisterBody(email, username));
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        await ConfirmEmailAsync(email);
        using var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, password = ValidPassword });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    private async Task ConfirmEmailAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CampaignDbContext>();
        var message = dbContext.OutboxMessages
            .Where(item => item.Type == EmailOutbox.ConfirmEmailType)
            .OrderByDescending(item => item.CreatedUtc)
            .AsEnumerable()
            .First(item => item.Payload.Contains(email, StringComparison.Ordinal));
        var payload = JsonSerializer.Deserialize<OutboxEmailPayload>(message.Payload);
        Assert.NotNull(payload);

        using var client = _factory.CreateClient();
        using var confirm = await client.PostAsJsonAsync(
            "/api/auth/confirm-email",
            new { userId = payload.UserId, token = payload.Token });
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);
    }

    private async Task MakeAdministratorAsync(string username)
    {
        using var scope = _factory.Services.CreateScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        if (!await roles.RoleExistsAsync("Administrator").ConfigureAwait(false))
        {
            Assert.True((await roles.CreateAsync(new IdentityRole<Guid>("Administrator")).ConfigureAwait(false)).Succeeded);
        }

        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByNameAsync(username).ConfigureAwait(false);
        Assert.NotNull(user);
        Assert.True((await users.AddToRoleAsync(user, "Administrator").ConfigureAwait(false)).Succeeded);
    }

    private static object CreateRegisterBody(string email, string username)
    {
        return new
        {
            email,
            username,
            password = ValidPassword,
            firstName = "Ada",
            lastName = "Lovelace",
            city = "Halifax",
            region = "Nova Scotia",
            country = "Canada",
            timeZoneId = "America/Halifax",
            displayNameMode = "Username",
        };
    }

    private static SaveCampaignRequest ValidCampaignBody(string name)
    {
        return new SaveCampaignRequest
        {
            Name = name,
            Description = "A contested frontier.",
            PlayerCount = 8,
            IsPrivate = false,
            IsPubliclyViewable = true,
            CreatorIsParticipant = true,
            Factions =
            [
                new FactionRequest { Name = "North", Subfactions = ["Riders"] },
                new FactionRequest { Name = "South" },
            ],
            TimeZoneId = "UTC",
            StartsAtLocal = "2099-01-05T12:00",
            RoundCount = 8,
            RoundLengthAmount = 1,
            RoundLengthUnit = "Weeks",
            Phases =
            [
                new RoundPhaseRequest { Kind = "Action", DurationAmount = 3, DurationUnit = "Days" },
                new RoundPhaseRequest { Kind = "Action", DurationAmount = 3, DurationUnit = "Days" },
                new RoundPhaseRequest { Kind = "Battle", DurationAmount = 1, DurationUnit = "Days" },
            ],
        };
    }

    private static string UniqueName(string prefix)
    {
        return $"{prefix}{Guid.NewGuid():N}"[..20];
    }

    private const string ValidPassword = "Correct-Horse-1";
}
