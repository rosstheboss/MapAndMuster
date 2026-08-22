using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MapAndMuster.Api.IntegrationTests;

public sealed class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetLiveHealthReturnsSuccess()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health/live", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("""{"status":"Healthy"}""", body);
        Assert.False(body.Contains("Host=", StringComparison.Ordinal));
        Assert.False(body.Contains("Password", StringComparison.OrdinalIgnoreCase));
        Assert.False(string.IsNullOrWhiteSpace(response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single()));
    }

    [Fact]
    public async Task GetHealthWithoutDatabaseChecksIsHealthy()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task EchoesSafeCorrelationId()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, "req-42");

        using var response = await client.SendAsync(request);

        Assert.Equal("req-42", response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single());
    }

    [Fact]
    public async Task RejectsUnsafeCorrelationId()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, "req/unsafe");

        using var response = await client.SendAsync(request);

        var echoed = response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single();
        Assert.NotEqual("req/unsafe", echoed);
        Assert.False(string.IsNullOrWhiteSpace(echoed));
    }

    [Fact]
    public async Task DevelopmentHealthIsNotRedirectedToHttps()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.UseSetting("HTTPS_PORT", "7247"));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync(new Uri("/health/live", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }
}
