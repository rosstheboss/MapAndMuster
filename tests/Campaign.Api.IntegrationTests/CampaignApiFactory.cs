using Campaign.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Campaign.Api.IntegrationTests;

public sealed class CampaignApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("campaign_tests")
        .WithUsername("campaign")
        .WithPassword("campaign")
        .Build();

    private readonly string _storagePath = Path.Combine(Path.GetTempPath(), "campaign-test-storage", Guid.NewGuid().ToString("N"));

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        Directory.CreateDirectory(_storagePath);
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CampaignDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
        if (Directory.Exists(_storagePath))
        {
            Directory.Delete(_storagePath, recursive: true);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Campaign", _postgres.GetConnectionString());
        builder.UseSetting("Email:SmtpHost", string.Empty);
        builder.UseSetting("Email:Provider", "Smtp");
        builder.UseSetting("Storage:RootPath", _storagePath);
        builder.UseSetting("PublicWeb:Origin", "http://localhost");
    }
}

[CollectionDefinition("api")]
public sealed class ApiTestGroup : ICollectionFixture<CampaignApiFactory>
{
}
