using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MapAndMuster.Api.Contracts;
using MapAndMuster.Infrastructure.Email;
using MapAndMuster.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace MapAndMuster.Api.IntegrationTests;

/// <summary>
/// Guards the per-request database cost of the campaign read endpoints the client calls most.
/// These endpoints previously issued one Identity query per member, twice per request, so a
/// campaign with more members cost proportionally more. The assertions below fail if that
/// returns: the statement count must not grow with membership size.
/// </summary>
[Collection("api")]
public sealed class CampaignReadQueryBudgetTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Ceiling for a single campaign read. Generous enough to absorb unrelated query changes
    /// while still catching per-member fan-out.
    /// </summary>
    private const int StatementCeiling = 20;

    private const string ValidPassword = "Correct-Horse-1";

    private readonly MapAndMusterApiFactory _factory;

    public CampaignReadQueryBudgetTests(MapAndMusterApiFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("log")]
    [InlineData("")]
    public async Task CampaignReadStatementCountDoesNotGrowWithMemberCount(string suffix)
    {
        using var owner = _factory.CreateClient();
        var ownerName = UniqueName("gm");
        await RegisterConfirmAndLoginAsync(owner, $"{ownerName}@example.test", ownerName);

        using var createdResponse = await owner.PostAsJsonAsync("/api/campaigns", ValidCampaignBody("Query Budget"));
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<CampaignDetailResponse>(JsonOptions);
        Assert.NotNull(created);

        var route = string.IsNullOrEmpty(suffix)
            ? $"/api/campaigns/{created.Id}"
            : $"/api/campaigns/{created.Id}/{suffix}";

        using var counter = new EfCommandCounter();

        // Warm the request path first. The initial call also compiles queries and loads the
        // authentication cookie pipeline, which would otherwise be counted as campaign cost.
        await AssertReadSucceedsAsync(owner, route);

        var withOneMember = await CountStatementsAsync(counter, owner, route);

        // Without this the equality assertion below would pass vacuously if the diagnostic
        // subscription ever stopped observing commands.
        Assert.True(withOneMember > 0, $"No database statements were observed for GET {route}.");

        var joiners = new List<HttpClient>();
        try
        {
            for (var index = 0; index < 4; index++)
            {
                var client = _factory.CreateClient();
                joiners.Add(client);
                var playerName = UniqueName($"p{index}");
                await RegisterConfirmAndLoginAsync(client, $"{playerName}@example.test", playerName);
                using var joined = await client.PostAsJsonAsync(
                    $"/api/campaigns/{created.Id}/join",
                    new JoinCampaignRequest());
                Assert.Equal(HttpStatusCode.OK, joined.StatusCode);
            }

            var detail = await owner.GetFromJsonAsync<CampaignDetailResponse>(
                $"/api/campaigns/{created.Id}",
                JsonOptions);
            Assert.NotNull(detail);
            Assert.Equal(5, detail.Participants.Count);

            var withFiveMembers = await CountStatementsAsync(counter, owner, route);

            Assert.True(
                withOneMember == withFiveMembers,
                $"GET {route} issued {withOneMember} statements with 1 member and {withFiveMembers} with 5. "
                    + "Campaign reads must not query per member.");
            Assert.True(
                withFiveMembers <= StatementCeiling,
                $"GET {route} issued {withFiveMembers} database statements, above the ceiling of {StatementCeiling}.");
        }
        finally
        {
            foreach (var client in joiners)
            {
                client.Dispose();
            }
        }
    }

    [Fact]
    public async Task MarkingTheLogReadDoesNotLoadTheWholeCampaign()
    {
        using var owner = _factory.CreateClient();
        var ownerName = UniqueName("reader");
        await RegisterConfirmAndLoginAsync(owner, $"{ownerName}@example.test", ownerName);

        using var createdResponse = await owner.PostAsJsonAsync("/api/campaigns", ValidCampaignBody("Read Marks"));
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<CampaignDetailResponse>(JsonOptions);
        Assert.NotNull(created);

        var route = $"/api/campaigns/{created.Id}/log/read";
        using var warm = await owner.PostAsync(route, content: null);
        Assert.Equal(HttpStatusCode.NoContent, warm.StatusCode);

        using var counter = new EfCommandCounter();
        counter.Start();
        using var response = await owner.PostAsync(route, content: null);
        counter.Stop();
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Authorization uses the narrow access snapshot: one campaign statement plus the
        // read-mark write. The full campaign load used a split query across six collections.
        Assert.True(
            counter.Count <= 6,
            $"POST {route} issued {counter.Count} database statements; it should only authorize and write a mark.");
    }

    private static async Task<int> CountStatementsAsync(EfCommandCounter counter, HttpClient client, string route)
    {
        counter.Start();
        await AssertReadSucceedsAsync(client, route);
        counter.Stop();
        return counter.Count;
    }

    private static async Task AssertReadSucceedsAsync(HttpClient client, string route)
    {
        using var response = await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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
}

/// <summary>
/// Counts executed EF Core commands by observing the Entity Framework diagnostic source.
/// Observing diagnostics keeps the production service registrations untouched, so the counts
/// reflect the pipeline the application actually runs.
/// </summary>
internal sealed class EfCommandCounter : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>, IDisposable
{
    private readonly List<IDisposable> _subscriptions = [];
    private int _count;
    private volatile bool _counting;

    public EfCommandCounter()
    {
        _subscriptions.Add(DiagnosticListener.AllListeners.Subscribe(this));
    }

    /// <summary>Gets the number of commands executed since the last <see cref="Start"/>.</summary>
    public int Count => Volatile.Read(ref _count);

    /// <summary>Resets the counter and begins counting.</summary>
    public void Start()
    {
        Volatile.Write(ref _count, 0);
        _counting = true;
    }

    /// <summary>Stops counting so later setup work is not attributed to the measured request.</summary>
    public void Stop()
    {
        _counting = false;
    }

    public void OnNext(DiagnosticListener listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
        if (string.Equals(listener.Name, DbLoggerCategory.Name, StringComparison.Ordinal))
        {
            _subscriptions.Add(listener.Subscribe(this));
        }
    }

    public void OnNext(KeyValuePair<string, object?> value)
    {
        if (_counting && string.Equals(value.Key, RelationalEventId.CommandExecuting.Name, StringComparison.Ordinal))
        {
            Interlocked.Increment(ref _count);
        }
    }

    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }

    public void Dispose()
    {
        _counting = false;
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }

        _subscriptions.Clear();
    }
}
