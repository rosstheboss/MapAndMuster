using MapAndMuster.Infrastructure.Identity;
using MapAndMuster.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MapAndMuster.Api;

/// <summary>
/// Applies pending EF Core migrations when a real database is configured.
/// </summary>
public static class DatabaseStartup
{
    /// <summary>
    /// Configuration key that controls whether the API applies migrations during process startup.
    /// </summary>
    public const string ApplyMigrationsKey = "Database:ApplyMigrationsOnStartup";

    /// <summary>
    /// Applies migrations when enabled, then runs identity maintenance. Identity seeding still runs when
    /// migrations are disabled, so Production can apply an EF bundle separately and still create the
    /// privileged administrator and Test 1–Test 30 on the next API start. Skips when the connection string is empty.
    /// </summary>
    /// <param name="app">The application.</param>
    /// <returns>A task that completes when migrations have been considered.</returns>
    public static async Task ApplyMigrationsAsync(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var connectionString = app.Configuration.GetConnectionString("Campaign");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var applyMigrations = app.Configuration.GetValue(ApplyMigrationsKey, true);
        if (applyMigrations)
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CampaignDbContext>();
            await dbContext.Database.MigrateAsync().ConfigureAwait(false);
        }

        var identity = scope.ServiceProvider.GetRequiredService<IdentityMaintenance>();
        await identity.EnsureAsync(CancellationToken.None).ConfigureAwait(false);
    }
}
