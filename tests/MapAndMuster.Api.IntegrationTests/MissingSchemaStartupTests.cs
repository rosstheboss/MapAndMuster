using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace MapAndMuster.Api.IntegrationTests;

public sealed class MissingSchemaStartupTests
{
    [Fact]
    public async Task RefusesToStartWhenStartupMigrationsAreOffAndTheSchemaIsMissing()
    {
        await using var postgres = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("mapandmuster_missing_schema")
            .WithUsername("mapandmuster")
            .WithPassword("mapandmuster")
            .Build();
        await postgres.StartAsync();

        var storagePath = Path.Combine(Path.GetTempPath(), "mapandmuster-missing-schema", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(storagePath);

        try
        {
            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseSetting("ConnectionStrings:Campaign", postgres.GetConnectionString());
                builder.UseSetting("Database:ApplyMigrationsOnStartup", "false");
                builder.UseSetting("Email:SmtpHost", string.Empty);
                builder.UseSetting("Email:Provider", "Smtp");
                builder.UseSetting("Storage:RootPath", storagePath);
                builder.UseSetting("PublicWeb:Origin", "http://localhost");
            });

            var exception = Assert.Throws<InvalidOperationException>(() => _ = factory.Services);
            Assert.Contains("eng/run-migrations", exception.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("AspNetRoles", exception.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("mapandmuster", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(storagePath))
            {
                Directory.Delete(storagePath, recursive: true);
            }
        }
    }
}
