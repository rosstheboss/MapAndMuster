using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MapAndMuster.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core migrations using the local docker-compose database.
/// </summary>
public sealed class CampaignDbContextFactory : IDesignTimeDbContextFactory<CampaignDbContext>
{
    /// <inheritdoc />
    public CampaignDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CampaignDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=mapandmuster;Username=mapandmuster;Password=mapandmuster")
            .Options;

        return new CampaignDbContext(options);
    }
}
