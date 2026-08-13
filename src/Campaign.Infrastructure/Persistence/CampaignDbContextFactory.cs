using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Campaign.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core migrations using the local docker-compose database.
/// </summary>
public sealed class CampaignDbContextFactory : IDesignTimeDbContextFactory<CampaignDbContext>
{
    /// <inheritdoc />
    public CampaignDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CampaignDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=campaign;Username=campaign;Password=campaign")
            .Options;

        return new CampaignDbContext(options);
    }
}
