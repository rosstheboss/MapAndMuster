using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MapAndMuster.Api.Contracts;
using MapAndMuster.Application.Play;
using MapAndMuster.Application.Ports;
using MapAndMuster.Infrastructure.Email;
using MapAndMuster.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MapAndMuster.Api.IntegrationTests;

/// <summary>
/// Covers the deadline-driven advance that replaces what client polling used to trigger.
/// </summary>
/// <remarks>
/// Exercises <see cref="AdvanceDueCampaignsHandler"/> directly rather than waiting on the hosted
/// service, so the tests are deterministic and do not sleep on a real deadline.
/// </remarks>
[Collection("api")]
public sealed class PhaseDeadlineAdvanceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private const string ValidPassword = "Correct-Horse-1";

    private readonly MapAndMusterApiFactory _factory;

    public PhaseDeadlineAdvanceTests(MapAndMusterApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ADueCampaignAdvancesWithoutAnyClientRequest()
    {
        using var owner = _factory.CreateClient();
        var ownerName = UniqueName("deadline");
        await RegisterConfirmAndLoginAsync(owner, $"{ownerName}@example.test", ownerName);

        // Starts in the past, so the first phase window is already due to open.
        var started = DateTime.UtcNow.AddDays(-2);
        var created = await CreateCampaignAsync(owner, "Deadline War", started);
        Assert.Equal("InProgress", created.Status);

        var before = await LoadStoredAsync(created.Id);
        Assert.Null(before.PlayState);

        await RunDueAdvanceAsync();

        var after = await LoadStoredAsync(created.Id);
        Assert.NotNull(after.PlayState);
        Assert.NotEmpty(after.PlayState.Windows);
        Assert.True(after.Revision > created.Revision);
    }

    [Fact]
    public async Task RunningTwiceIsIdempotent()
    {
        using var owner = _factory.CreateClient();
        var ownerName = UniqueName("idem");
        await RegisterConfirmAndLoginAsync(owner, $"{ownerName}@example.test", ownerName);

        var created = await CreateCampaignAsync(owner, "Idempotent War", DateTime.UtcNow.AddDays(-2));
        await RunDueAdvanceAsync();
        var afterFirst = await LoadStoredAsync(created.Id);

        await RunDueAdvanceAsync();
        var afterSecond = await LoadStoredAsync(created.Id);

        // A repeated pass must not write again, because nothing new has fallen due.
        Assert.Equal(afterFirst.Revision, afterSecond.Revision);
    }

    [Fact]
    public async Task ScheduledCampaignReportsItsStartAsTheNextDeadline()
    {
        using var owner = _factory.CreateClient();
        var ownerName = UniqueName("future");
        await RegisterConfirmAndLoginAsync(owner, $"{ownerName}@example.test", ownerName);

        var starts = DateTime.UtcNow.AddDays(30);
        var created = await CreateCampaignAsync(owner, "Future War", starts);
        Assert.Equal("Scheduled", created.Status);

        var next = await RunDueAdvanceAsync();

        // Something is scheduled, so the worker must be told to wake rather than sleep forever.
        Assert.NotNull(next);
        Assert.True(next > DateTimeOffset.UtcNow);

        var after = await LoadStoredAsync(created.Id);
        Assert.Null(after.PlayState);
        Assert.Equal(created.Revision, after.Revision);
    }

    private async Task<DateTimeOffset?> RunDueAdvanceAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<AdvanceDueCampaignsHandler>();
        return await handler.RunAsync(CancellationToken.None);
    }

    private async Task<Application.Campaigns.StoredCampaign> LoadStoredAsync(Guid campaignId)
    {
        using var scope = _factory.Services.CreateScope();
        var campaigns = scope.ServiceProvider.GetRequiredService<ICampaignStore>();
        var campaign = await campaigns.FindByIdAsync(campaignId, CancellationToken.None);
        Assert.NotNull(campaign);
        return campaign;
    }

    private static async Task<CampaignDetailResponse> CreateCampaignAsync(
        HttpClient client,
        string name,
        DateTime startsUtc)
    {
        using var response = await client.PostAsJsonAsync("/api/campaigns", new SaveCampaignRequest
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
            StartsAtLocal = startsUtc.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture),
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
