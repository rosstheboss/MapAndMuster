using System.Net;

namespace Campaign.Api.IntegrationTests;

[Collection("api")]
public sealed class ReadyHealthEndpointTests
{
    private readonly CampaignApiFactory _factory;

    public ReadyHealthEndpointTests(CampaignApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ReadyHealthIncludesPostgreSqlAndOmitsSecrets()
    {
        using var client = _factory.CreateClient();

        using var ready = await client.GetAsync(new Uri("/health/ready", UriKind.Relative));
        using var health = await client.GetAsync(new Uri("/health", UriKind.Relative));
        var body = await ready.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Equal("""{"status":"Healthy"}""", body);
        Assert.DoesNotContain("Host=", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Username=", body, StringComparison.Ordinal);
        Assert.DoesNotContain("campaign_tests", body, StringComparison.Ordinal);
    }
}
