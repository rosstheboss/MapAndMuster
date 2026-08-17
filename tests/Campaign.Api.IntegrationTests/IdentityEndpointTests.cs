using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Campaign.Api.Contracts;
using Campaign.Infrastructure.Email;
using Campaign.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Campaign.Api.IntegrationTests;

[Collection("api")]
public sealed class IdentityEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly CampaignApiFactory _factory;

    public IdentityEndpointTests(CampaignApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RegisterConfirmLoginAndLogout()
    {
        using var client = _factory.CreateClient();
        var username = UniqueName("player");
        var email = $"{username}@example.test";

        using var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            CreateRegisterBody(email, username));
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        using var unconfirmedLogin = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password = ValidPassword });
        Assert.Equal(HttpStatusCode.Forbidden, unconfirmedLogin.StatusCode);

        await ConfirmEmailAsync(email);

        using var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password = ValidPassword });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var me = await loginResponse.Content.ReadFromJsonAsync<OwnProfileResponse>(JsonOptions);
        Assert.NotNull(me);
        Assert.Equal(email, me.Email);
        Assert.Equal(username, me.Username);
        Assert.Equal("Ada", me.FirstName);
        Assert.Equal("Lovelace", me.LastName);
        Assert.Equal("Halifax", me.City);
        Assert.NotEqual(default, me.CreatedUtc);
        Assert.Equal(me.CreatedUtc, me.UpdatedUtc);

        using var meResponse = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

        using var logoutResponse = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        using var afterLogout = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }

    [Fact]
    public async Task DuplicateUsernameIsRejected()
    {
        using var client = _factory.CreateClient();
        var username = UniqueName("taken");
        await RegisterAndConfirmAsync(client, $"{username}@example.test", username);

        using var duplicate = await client.PostAsJsonAsync(
            "/api/auth/register",
            CreateRegisterBody($"{username}-two@example.test", username));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task InvalidPasswordIsRejected()
    {
        using var client = _factory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "missing@example.test", password = "wrong" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UnauthenticatedProfileAccessIsRejected()
    {
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync("/api/profiles/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PublicProfileOmitsPrivateFields()
    {
        using var owner = _factory.CreateClient();
        var username = UniqueName("public");
        var email = $"{username}@example.test";
        await RegisterConfirmAndLoginAsync(owner, email, username);

        using var update = await owner.PutAsJsonAsync(
            "/api/profiles/me",
            new
            {
                username,
                firstName = "Ada",
                middleInitial = "L",
                lastName = "Lovelace",
                city = "Halifax",
                region = "Nova Scotia",
                country = "Canada",
                timeZoneId = "America/Halifax",
                displayNameMode = "Username",
                profileRevision = 1,
            });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var own = await owner.GetFromJsonAsync<OwnProfileResponse>("/api/profiles/me", JsonOptions);
        Assert.NotNull(own);
        Assert.Equal("America/Halifax", own.TimeZoneId);
        Assert.Equal("English", own.PreferredChatLanguage);

        using var stranger = _factory.CreateClient();
        using var publicResponse = await stranger.GetAsync($"/api/profiles/{username}");
        Assert.Equal(HttpStatusCode.OK, publicResponse.StatusCode);

        var json = await publicResponse.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.Equal(username, root.GetProperty("username").GetString());
        Assert.Equal("Halifax", root.GetProperty("city").GetString());
        Assert.False(root.TryGetProperty("email", out _));
        Assert.False(root.TryGetProperty("firstName", out _));
        Assert.False(root.TryGetProperty("lastName", out _));
        Assert.False(root.TryGetProperty("createdUtc", out _));
        Assert.False(root.TryGetProperty("updatedUtc", out _));
        Assert.False(root.TryGetProperty("timeZoneId", out _));
        Assert.False(root.TryGetProperty("profileRevision", out _));
        Assert.False(root.TryGetProperty("preferredChatLanguage", out _));
        Assert.DoesNotContain(email, json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Lovelace", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnknownTimeZoneIsRejected()
    {
        using var client = _factory.CreateClient();
        var username = UniqueName("tzbad");
        await RegisterConfirmAndLoginAsync(client, $"{username}@example.test", username);

        using var update = await client.PutAsJsonAsync(
            "/api/profiles/me",
            new
            {
                username,
                firstName = "Ada",
                lastName = "Lovelace",
                city = "Halifax",
                region = "Nova Scotia",
                country = "Canada",
                displayNameMode = "Username",
                timeZoneId = "Not/AZone",
                profileRevision = 1,
            });
        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
    }

    [Fact]
    public async Task PreferredChatLanguageCanBeUpdated()
    {
        using var client = _factory.CreateClient();
        var username = UniqueName("clang");
        await RegisterConfirmAndLoginAsync(client, $"{username}@example.test", username);

        using var update = await client.PutAsJsonAsync(
            "/api/profiles/me",
            new
            {
                username,
                firstName = "Ada",
                lastName = "Lovelace",
                city = "Halifax",
                region = "Nova Scotia",
                country = "Canada",
                timeZoneId = "America/Halifax",
                displayNameMode = "Username",
                preferredChatLanguage = "Spanish",
                profileRevision = 1,
            });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var profile = await update.Content.ReadFromJsonAsync<OwnProfileResponse>(JsonOptions);
        Assert.NotNull(profile);
        Assert.Equal("Spanish", profile.PreferredChatLanguage);
    }

    [Fact]
    public async Task PublicProfileCanShowFullNameWhenOwnerOptsIn()
    {
        using var owner = _factory.CreateClient();
        var username = UniqueName("named");
        await RegisterConfirmAndLoginAsync(owner, $"{username}@example.test", username);

        using var update = await owner.PutAsJsonAsync(
            "/api/profiles/me",
            new
            {
                username,
                firstName = "Ada",
                lastName = "Lovelace",
                city = "Halifax",
                region = "Nova Scotia",
                country = "Canada",
                timeZoneId = "America/Halifax",
                displayNameMode = "FullName",
                profileRevision = 1,
            });
        var updated = await update.Content.ReadFromJsonAsync<OwnProfileResponse>(JsonOptions);
        Assert.NotNull(updated);
        Assert.True(updated.UpdatedUtc > updated.CreatedUtc);

        using var stranger = _factory.CreateClient();
        var publicProfile = await stranger.GetFromJsonAsync<PublicProfileResponse>($"/api/profiles/{username}", JsonOptions);
        Assert.NotNull(publicProfile);
        Assert.Equal("Ada Lovelace", publicProfile.DisplayName);
        Assert.True(publicProfile.ShowsFullName);
        Assert.Equal(username, publicProfile.Username);
        Assert.NotNull(publicProfile.Campaigns);
    }

    [Fact]
    public async Task PublicProfileListsVisibleCampaignsAndOmitsHiddenPrivateOnes()
    {
        using var owner = _factory.CreateClient();
        var ownerName = UniqueName("host");
        await RegisterConfirmAndLoginAsync(owner, $"{ownerName}@example.test", ownerName);
        using var openResponse = await owner.PostAsJsonAsync("/api/campaigns", ValidOpenCampaign("Open War"));
        var open = await openResponse.Content.ReadFromJsonAsync<CampaignDetailResponse>(JsonOptions);
        Assert.NotNull(open);

        using var hiddenResponse = await owner.PostAsJsonAsync(
            "/api/campaigns",
            ValidOpenCampaign("Secret War", isPubliclyViewable: false));
        var hidden = await hiddenResponse.Content.ReadFromJsonAsync<CampaignDetailResponse>(JsonOptions);
        Assert.NotNull(hidden);

        using var sharedResponse = await owner.PostAsJsonAsync(
            "/api/campaigns",
            ValidOpenCampaign("Shared War", isPubliclyViewable: false));
        var shared = await sharedResponse.Content.ReadFromJsonAsync<CampaignDetailResponse>(JsonOptions);
        Assert.NotNull(shared);

        using var stranger = _factory.CreateClient();
        var strangerName = UniqueName("guest");
        await RegisterConfirmAndLoginAsync(stranger, $"{strangerName}@example.test", strangerName);
        using var joined = await stranger.PostAsJsonAsync($"/api/campaigns/{shared.Id}/join", new JoinCampaignRequest());
        Assert.Equal(HttpStatusCode.OK, joined.StatusCode);

        var profile = await stranger.GetFromJsonAsync<PublicProfileResponse>($"/api/profiles/{ownerName}", JsonOptions);
        Assert.NotNull(profile);
        Assert.Contains(profile.Campaigns, campaign => campaign.Id == open.Id && campaign.Name == "Open War");
        Assert.Contains(profile.Campaigns, campaign => campaign.Id == shared.Id && campaign.Name == "Shared War");
        Assert.DoesNotContain(profile.Campaigns, campaign => campaign.Id == hidden.Id);

        using var anonymous = _factory.CreateClient();
        var publicProfile = await anonymous.GetFromJsonAsync<PublicProfileResponse>($"/api/profiles/{ownerName}", JsonOptions);
        Assert.NotNull(publicProfile);
        Assert.Contains(publicProfile.Campaigns, campaign => campaign.Id == open.Id);
        Assert.DoesNotContain(publicProfile.Campaigns, campaign => campaign.Id == hidden.Id);
        Assert.DoesNotContain(publicProfile.Campaigns, campaign => campaign.Id == shared.Id);
    }

    [Fact]
    public async Task AvatarUploadRejectsSvgAndAcceptsPng()
    {
        using var client = _factory.CreateClient();
        var username = UniqueName("avatar");
        await RegisterConfirmAndLoginAsync(client, $"{username}@example.test", username);

        using var svgContent = new MultipartFormDataContent();
        var svg = new ByteArrayContent(Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>"));
        svg.Headers.ContentType = new MediaTypeHeaderValue("image/svg+xml");
        svgContent.Add(svg, "avatar", "avatar.svg");
        using var svgResponse = await client.PostAsync("/api/profiles/me/avatar", svgContent);
        Assert.Equal(HttpStatusCode.BadRequest, svgResponse.StatusCode);

        using var pngContent = new MultipartFormDataContent();
        var png = new ByteArrayContent(PngBytes);
        png.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        pngContent.Add(png, "avatar", "avatar.png");
        using var pngResponse = await client.PostAsync("/api/profiles/me/avatar", pngContent);
        Assert.Equal(HttpStatusCode.OK, pngResponse.StatusCode);

        using var avatarResponse = await client.GetAsync($"/api/profiles/{username}/avatar");
        Assert.Equal(HttpStatusCode.OK, avatarResponse.StatusCode);
        Assert.Equal("image/jpeg", avatarResponse.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task ConcurrentProfileUpdatesReturnConflict()
    {
        var username = UniqueName("race");
        var email = $"{username}@example.test";
        using var first = _factory.CreateClient();
        await RegisterConfirmAndLoginAsync(first, email, username);

        using var second = _factory.CreateClient();
        using var login = await second.PostAsJsonAsync("/api/auth/login", new { email, password = ValidPassword });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var body = new
        {
            username,
            firstName = "Ada",
            lastName = "Lovelace",
            city = "Halifax",
            region = "Nova Scotia",
            country = "Canada",
            timeZoneId = "America/Halifax",
            displayNameMode = "Username",
            profileRevision = 1,
        };

        using var firstUpdate = await first.PutAsJsonAsync("/api/profiles/me", body);
        using var secondUpdate = await second.PutAsJsonAsync("/api/profiles/me", body);
        var statuses = new[] { firstUpdate.StatusCode, secondUpdate.StatusCode };
        Assert.Contains(HttpStatusCode.OK, statuses);
        Assert.Contains(HttpStatusCode.Conflict, statuses);
    }

    [Fact]
    public async Task ExternalProvidersAreEmptyWhenUnconfigured()
    {
        using var client = _factory.CreateClient();
        var providers = await client.GetFromJsonAsync<ExternalProviderResponse[]>("/api/auth/external-providers", JsonOptions);
        Assert.NotNull(providers);
        Assert.Empty(providers);
    }

    [Fact]
    public async Task WeakPasswordIsRejectedWithFieldErrors()
    {
        using var client = _factory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                email = "weak@example.test",
                username = UniqueName("weak"),
                password = "short",
                firstName = "Ada",
                lastName = "Lovelace",
                city = "Halifax",
                region = "Nova Scotia",
                country = "Canada",
                timeZoneId = "America/Halifax",
                displayNameMode = "Username",
            });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Contains("Password is too short", body.Message, StringComparison.Ordinal);
        Assert.Contains(body.Errors ?? [], error => error.Field == "password");
    }

    [Fact]
    public async Task ProhibitedUsernameIsRejected()
    {
        using var client = _factory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            CreateRegisterBody("banned@example.test", "fuckyou"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Contains("prohibited language", body.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReservedUsernameIsRejected()
    {
        using var client = _factory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            CreateRegisterBody("reserved@example.test", "everyone"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("username.reserved", body.Code);
        Assert.Contains("reserved", body.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ChangePasswordRequiresTheCurrentPassword()
    {
        using var client = _factory.CreateClient();
        var username = UniqueName("pwchg");
        var email = $"{username}@example.test";
        await RegisterConfirmAndLoginAsync(client, email, username);

        using var wrong = await client.PostAsJsonAsync(
            "/api/auth/change-password",
            new { currentPassword = "Wrong-Password-1", newPassword = "Correct-Horse-2!" });
        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);

        using var changed = await client.PostAsJsonAsync(
            "/api/auth/change-password",
            new { currentPassword = ValidPassword, newPassword = "Correct-Horse-2!" });
        Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);

        using var logout = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        using var oldPassword = await client.PostAsJsonAsync("/api/auth/login", new { email, password = ValidPassword });
        Assert.Equal(HttpStatusCode.Unauthorized, oldPassword.StatusCode);

        using var newPassword = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password = "Correct-Horse-2!" });
        Assert.Equal(HttpStatusCode.OK, newPassword.StatusCode);
    }

    private async Task RegisterAndConfirmAsync(HttpClient client, string email, string username)
    {
        using var registerResponse = await client.PostAsJsonAsync("/api/auth/register", CreateRegisterBody(email, username));
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        await ConfirmEmailAsync(email);
    }

    private async Task RegisterConfirmAndLoginAsync(HttpClient client, string email, string username)
    {
        await RegisterAndConfirmAsync(client, email, username);
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

    private static SaveCampaignRequest ValidOpenCampaign(string name, bool isPubliclyViewable = true)
    {
        return new SaveCampaignRequest
        {
            Name = name,
            Description = "A contested frontier.",
            PlayerCount = 8,
            IsPrivate = false,
            IsPubliclyViewable = isPubliclyViewable,
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

    private static readonly byte[] PngBytes = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");
}
