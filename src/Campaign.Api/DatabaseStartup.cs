using Campaign.Infrastructure.Identity;
using Campaign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Campaign.Api;

/// <summary>
/// Applies pending EF Core migrations when a real database is configured.
/// </summary>
public static class DatabaseStartup
{
    /// <summary>
    /// Applies migrations. Skips when the connection string is the unconfigured placeholder.
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
        var dbContext = scope.ServiceProvider.GetRequiredService<CampaignDbContext>();
        await dbContext.Database.MigrateAsync().ConfigureAwait(false);
        var identity = scope.ServiceProvider.GetRequiredService<IdentityMaintenance>();
        await identity.EnsureAsync(CancellationToken.None).ConfigureAwait(false);
    }
}
