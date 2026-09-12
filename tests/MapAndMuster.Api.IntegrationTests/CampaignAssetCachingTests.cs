using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MapAndMuster.Api.Contracts;
using MapAndMuster.Infrastructure.Email;
using MapAndMuster.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MapAndMuster.Api.IntegrationTests;

/// <summary>
/// Covers the cache contract for campaign assets: URLs that change only when the file does,
/// and conditional requests that cost nothing.
/// </summary>
[Collection("api")]
public sealed class CampaignAssetCachingTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>A one-pixel PNG. Generic and fictional content; only the bytes matter here.</summary>
    private static readonly byte[] PngBytes = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    private const string ValidPassword = "Correct-Horse-1";

    private readonly MapAndMusterApiFactory _factory;

    public CampaignAssetCachingTests(MapAndMusterApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AMapAssetTagSurvivesAnUnrelatedRevisionBump()
    {
        using var owner = _factory.CreateClient();
        var ownerName = UniqueName("assettag");
        await RegisterConfirmAndLoginAsync(owner, $"{ownerName}@example.test", ownerName);
        var created = await CreateCampaignAsync(owner, "Asset Tags");
        var withMap = await UploadMapAsync(owner, created.Id, created.Revision);

        var tag = Assert.Contains("map", withMap.AssetTags);
        Assert.False(string.IsNullOrWhiteSpace(tag));

        // Chat is the frequent revision bump that used to invalidate every asset URL.
        using var chat = await owner.PostAsJsonAsync(
            $"/api/campaigns/{withMap.Id}/chat",
            new PostCampaignChatRequest { Revision = withMap.Revision, Message = "Scouts report movement." });
        Assert.Equal(HttpStatusCode.OK, chat.StatusCode);

        var after = await GetCampaignAsync(owner, withMap.Id);
        Assert.True(after.Revision > withMap.Revision, "Posting chat should bump the campaign revision.");
        Assert.Equal(tag, Assert.Contains("map", after.AssetTags));
    }

    [Fact]
    public async Task ARepeatMapRequestIsAnsweredWithoutABody()
    {
        using var owner = _factory.CreateClient();
        var ownerName = UniqueName("assetetag");
        await RegisterConfirmAndLoginAsync(owner, $"{ownerName}@example.test", ownerName);
        var created = await CreateCampaignAsync(owner, "Asset ETags");
        var withMap = await UploadMapAsync(owner, created.Id, created.Revision);

        using var first = await owner.GetAsync($"/api/campaigns/{withMap.Id}/map");
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var entityTag = first.Headers.ETag;
        Assert.NotNull(entityTag);

        var cacheControl = first.Headers.CacheControl;
        Assert.NotNull(cacheControl);
        Assert.True(cacheControl.Private, "Campaign assets are member-only and must not be cached by shared caches.");
        Assert.Equal(TimeSpan.FromDays(365), cacheControl.MaxAge);

        var body = await first.Content.ReadAsByteArrayAsync();
        Assert.NotEmpty(body);

        using var conditional = new HttpRequestMessage(HttpMethod.Get, $"/api/campaigns/{withMap.Id}/map");
        conditional.Headers.IfNoneMatch.Add(entityTag);
        using var second = await owner.SendAsync(conditional);

        Assert.Equal(HttpStatusCode.NotModified, second.StatusCode);
        Assert.Empty(await second.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task AConditionalRequestFromANonViewerIsStillRefused()
    {
        using var owner = _factory.CreateClient();
        var ownerName = UniqueName("assetown");
        await RegisterConfirmAndLoginAsync(owner, $"{ownerName}@example.test", ownerName);
        var created = await CreateCampaignAsync(owner, "Private Assets", isPubliclyViewable: false);
        var withMap = await UploadMapAsync(owner, created.Id, created.Revision);
        var entityTag = new EntityTagHeaderValue($"\"{withMap.AssetTags["map"]}\"");

        using var outsider = _factory.CreateClient();
        var outsiderName = UniqueName("assetout");
        await RegisterConfirmAndLoginAsync(outsider, $"{outsiderName}@example.test", outsiderName);

        // A caller who somehow learns a tag must not be able to turn it into a cheap 304 that
        // confirms the asset exists. Authorization runs before the tag is even considered.
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/campaigns/{withMap.Id}/map");
        request.Headers.IfNoneMatch.Add(entityTag);
        using var response = await outsider.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<CampaignDetailResponse> UploadMapAsync(HttpClient client, Guid campaignId, int revision)
    {
        using var form = new MultipartFormDataContent();
        var image = new ByteArrayContent(PngBytes);
        image.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(image, "map", "map.png");
        form.Add(new StringContent(revision.ToString(CultureInfo.InvariantCulture)), "revision");

        using var response = await client.PostAsync($"/api/campaigns/{campaignId}/map", form);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<CampaignDetailResponse>(JsonOptions);
        Assert.NotNull(detail);
        return detail;
    }

    private static async Task<CampaignDetailResponse> GetCampaignAsync(HttpClient client, Guid campaignId)
    {
        using var response = await client.GetAsync($"/api/campaigns/{campaignId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<CampaignDetailResponse>(JsonOptions);
        Assert.NotNull(detail);
        return detail;
    }

    private static async Task<CampaignDetailResponse> CreateCampaignAsync(
        HttpClient client,
        string name,
        bool isPubliclyViewable = true)
    {
        using var response = await client.PostAsJsonAsync("/api/campaigns", new SaveCampaignRequest
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
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CampaignDetailResponse>(JsonOptions);
        Assert.NotNull(created);
        return created;
    }

    private async Task RegisterConfirmAndLoginAsync(HttpClient client, string email, string username)
    {
        using var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
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
        });
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        await ConfirmEmailAsync(email);
        using var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, password = ValidPassword });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    private async Task ConfirmEmailAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CampaignDbContext>();
        var message = await dbContext.OutboxMessages
            .Where(item => item.Type == EmailOutbox.ConfirmEmailType && item.Payload.Contains(email))
            .OrderByDescending(item => item.CreatedUtc)
            .FirstAsync();
        var payload = JsonSerializer.Deserialize<OutboxEmailPayload>(message.Payload);
        Assert.NotNull(payload);

        using var client = _factory.CreateClient();
        using var confirm = await client.PostAsJsonAsync(
            "/api/auth/confirm-email",
            new { userId = payload.UserId, token = payload.Token });
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);
    }

    private static string UniqueName(string prefix)
    {
        return $"{prefix}{Guid.NewGuid():N}"[..20];
    }
}
