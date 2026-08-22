using Campaign.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Campaign.Api;

/// <summary>
/// Maps production-safe health endpoints that omit connection strings, secrets, and exception details.
/// </summary>
public static class HealthCheckHosting
{
    /// <summary>
    /// Live process endpoint.
    /// </summary>
    public const string LivePath = "/health/live";

    /// <summary>
    /// Readiness endpoint, including PostgreSQL when a connection string is configured.
    /// </summary>
    public const string ReadyPath = "/health/ready";

    /// <summary>
    /// Default production health path. Same checks as <see cref="ReadyPath"/>.
    /// </summary>
    public const string HealthPath = "/health";

    /// <summary>
    /// Registers health checks. Adds a PostgreSQL check only when a real connection string is present.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddCampaignHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var healthChecks = services.AddHealthChecks();
        if (!string.IsNullOrWhiteSpace(configuration.GetConnectionString("Campaign")))
        {
            healthChecks.AddDbContextCheck<CampaignDbContext>("postgresql", tags: ["ready"]);
        }

        return services;
    }

    /// <summary>
    /// Maps live, ready, and combined health endpoints.
    /// </summary>
    /// <param name="app">The application.</param>
    /// <returns>The same application.</returns>
    public static WebApplication MapCampaignHealthChecks(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.MapHealthChecks(LivePath, new HealthCheckOptions
        {
            Predicate = static _ => false,
            ResponseWriter = WriteAsync,
        });
        app.MapHealthChecks(ReadyPath, new HealthCheckOptions
        {
            Predicate = static check => check.Tags.Contains("ready"),
            ResponseWriter = WriteAsync,
        });
        app.MapHealthChecks(HealthPath, new HealthCheckOptions
        {
            Predicate = static check => check.Tags.Contains("ready"),
            ResponseWriter = WriteAsync,
        });
        return app;
    }

    private static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        var status = report.Status.ToString();
        return context.Response.WriteAsync($$"""{"status":"{{status}}"}""", context.RequestAborted);
    }
}
