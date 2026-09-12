using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MapAndMuster.Api.Contracts;
using MapAndMuster.Infrastructure.Email;
using MapAndMuster.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MapAndMuster.Api.IntegrationTests;

/// <summary>
/// Covers the Server-Sent Events stream that replaced client polling.
/// </summary>
[Collection("api")]
public sealed class CampaignStreamEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly TimeSpan EventTimeout = TimeSpan.FromSeconds(20);

    private const string ValidPassword = "Correct-Horse-1";

    private readonly MapAndMusterApiFactory _factory;

    public CampaignStreamEndpointTests(MapAndMusterApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StreamEmitsRevisionOnlyWhenTheCampaignChanges()
    {
        using var owner = _factory.CreateClient();
        var ownerName = UniqueName("streamgm");
        await RegisterConfirmAndLoginAsync(owner, $"{ownerName}@example.test", ownerName);
        var created = await CreateCampaignAsync(owner, "Stream Updates");

        using var streamCts = new CancellationTokenSource(EventTimeout);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/campaigns/{created.Id}/stream");
        using var response = await owner.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            streamCts.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("no", Assert.Single(response.Headers.GetValues("X-Accel-Buffering")));

        await using var body = await response.Content.ReadAsStreamAsync(streamCts.Token);
        using var reader = new StreamReader(body);

        // Posting chat bumps the revision through UpdatePlayStateAsync, which publishes.
        using var posted = await owner.PostAsJsonAsync(
            $"/api/campaigns/{created.Id}/chat",
            new PostCampaignChatRequest { Revision = created.Revision, Message = "Scouts report movement." },
            streamCts.Token);
        Assert.Equal(HttpStatusCode.OK, posted.StatusCode);

        var heartbeat = await ReadNextFrameAsync(reader, streamCts.Token);
        Assert.Equal("heartbeat", heartbeat.Event);
        Assert.Equal("{}", heartbeat.Data);

        var frame = await ReadEventAsync(reader, streamCts.Token);
        Assert.Equal("play", frame.Event);

        // The frame must carry nothing but a revision. Chat text, orders, and relic locations
        // stay behind the authorized read endpoints.
        using var payload = JsonDocument.Parse(frame.Data);
        Assert.Equal(JsonValueKind.Object, payload.RootElement.ValueKind);
        var only = Assert.Single(payload.RootElement.EnumerateObject());
        Assert.Equal("revision", only.Name);
        Assert.True(only.Value.GetInt32() > created.Revision);
        Assert.DoesNotContain("Scouts report movement", frame.Data, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StreamIsNotFoundForANonViewerOfAPrivateCampaign()
    {
        using var owner = _factory.CreateClient();
        var ownerName = UniqueName("privategm");
        await RegisterConfirmAndLoginAsync(owner, $"{ownerName}@example.test", ownerName);
        var created = await CreateCampaignAsync(owner, "Hidden Stream", isPubliclyViewable: false);

        using var outsider = _factory.CreateClient();
        var outsiderName = UniqueName("outsider");
        await RegisterConfirmAndLoginAsync(outsider, $"{outsiderName}@example.test", outsiderName);

        using var cts = new CancellationTokenSource(EventTimeout);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/campaigns/{created.Id}/stream");
        using var response = await outsider.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ConcurrentStreamsDoNotExhaustTheDatabaseConnectionPool()
    {
        using var owner = _factory.CreateClient();
        var ownerName = UniqueName("poolgm");
        await RegisterConfirmAndLoginAsync(owner, $"{ownerName}@example.test", ownerName);
        var created = await CreateCampaignAsync(owner, "Pool Pressure");

        // Comfortably more than the configured Npgsql pool size. If the stream handler retained a
        // scoped DbContext, these connections would hold pooled database connections open and
        // the ordinary request below would time out waiting for one.
        const int StreamCount = 150;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var responses = new List<HttpResponseMessage>(StreamCount);
        var bodies = new List<Stream>(StreamCount);
        try
        {
            for (var index = 0; index < StreamCount; index++)
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"/api/campaigns/{created.Id}/stream");
                var response = await owner.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                request.Dispose();
                responses.Add(response);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                bodies.Add(await response.Content.ReadAsStreamAsync(cts.Token));
            }

            using var detail = await owner.GetAsync($"/api/campaigns/{created.Id}", cts.Token);
            Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        }
        finally
        {
            foreach (var body in bodies)
            {
                await body.DisposeAsync();
            }

            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }

    [Fact]
    public async Task SiteChatStreamEmitsWhenAMessageIsPosted()
    {
        using var client = _factory.CreateClient();
        var username = UniqueName("sitechat");
        await RegisterConfirmAndLoginAsync(client, $"{username}@example.test", username);

        using var cts = new CancellationTokenSource(EventTimeout);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/site-chat/stream");
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var body = await response.Content.ReadAsStreamAsync(cts.Token);
        using var reader = new StreamReader(body);

        using var posted = await client.PostAsJsonAsync(
            "/api/site-chat",
            new PostSiteChatRequest { Message = "Anyone up for a campaign?" },
            cts.Token);
        Assert.Equal(HttpStatusCode.OK, posted.StatusCode);

        var frame = await ReadEventAsync(reader, cts.Token);
        Assert.Equal("site-chat", frame.Event);
        Assert.DoesNotContain("Anyone up for", frame.Data, StringComparison.Ordinal);
    }

    /// <summary>
    /// Reads frames until a campaign or site-chat event arrives, skipping heartbeats and comments.
    /// </summary>
    private static async Task<(string Event, string Data)> ReadEventAsync(
        StreamReader reader,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            var frame = await ReadNextFrameAsync(reader, cancellationToken);
            if (!string.Equals(frame.Event, "heartbeat", StringComparison.Ordinal))
            {
                return frame;
            }
        }
    }

    /// <summary>
    /// Reads the next SSE frame, including heartbeats.
    /// </summary>
    private static async Task<(string Event, string Data)> ReadNextFrameAsync(
        StreamReader reader,
        CancellationToken cancellationToken)
    {
        string? name = null;
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (line.StartsWith(':'))
            {
                continue;
            }

            if (line.StartsWith("event: ", StringComparison.Ordinal))
            {
                name = line["event: ".Length..];
                continue;
            }

            if (line.StartsWith("data: ", StringComparison.Ordinal))
            {
                return (name ?? string.Empty, line["data: ".Length..]);
            }
        }

        throw new InvalidOperationException("The stream closed before an event arrived.");
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
