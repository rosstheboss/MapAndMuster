using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace MapAndMuster.Api.IntegrationTests;

public sealed class ProductionConfigurationTests
{
    [Fact]
    public void DevelopmentDoesNotRequireProductionSecrets()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        ProductionConfiguration.Validate(configuration, new TestHostEnvironment { EnvironmentName = Environments.Development });
    }

    [Fact]
    public void TestingDoesNotRequireProductionSecrets()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        ProductionConfiguration.Validate(configuration, new TestHostEnvironment { EnvironmentName = "Testing" });
    }

    [Fact]
    public void ProductionRequiresNamedKeysAndOmitsValuesFromTheError()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PublicWeb:Origin"] = "https://mapandmuster.com",
            ["Email:Provider"] = "Resend",
            ["Email:FromAddress"] = "noreply@mapandmuster.com",
        }).Build();

        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfiguration.Validate(configuration, new TestHostEnvironment()));

        Assert.Contains("ConnectionStrings:Campaign", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Email:Resend:ApiKey", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Identity:BootstrapAdminPassword", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Identity:BootstrapAdminEmail", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("noreply@mapandmuster.com", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("https://mapandmuster.com", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductionRejectsLocalhostPublicOrigin()
    {
        var configuration = CompleteProductionSettings();
        configuration["PublicWeb:Origin"] = "http://localhost:4200";
        var built = new ConfigurationBuilder().AddInMemoryCollection(configuration).Build();

        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfiguration.Validate(built, new TestHostEnvironment()));

        Assert.Contains("PublicWeb:Origin", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("localhost:4200", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductionRejectsUnknownEmailProvider()
    {
        var configuration = CompleteProductionSettings();
        configuration["Email:Provider"] = "Unknown";
        var built = new ConfigurationBuilder().AddInMemoryCollection(configuration).Build();

        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfiguration.Validate(built, new TestHostEnvironment()));

        Assert.Contains("Email:Provider", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Unknown", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductionErrorOmitsBootstrapAdminPasswordValue()
    {
        var configuration = CompleteProductionSettings();
        configuration.Remove("ConnectionStrings:Campaign");
        var built = new ConfigurationBuilder().AddInMemoryCollection(configuration).Build();

        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfiguration.Validate(built, new TestHostEnvironment()));

        Assert.Contains("ConnectionStrings:Campaign", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Bootstrap-Admin-1!", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("admin@example.test", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AcceptsCompleteResendProductionSettings()
    {
        var built = new ConfigurationBuilder().AddInMemoryCollection(CompleteProductionSettings()).Build();
        ProductionConfiguration.Validate(built, new TestHostEnvironment());
    }

    [Fact]
    public void AcceptsCompleteResendStagingSettings()
    {
        var configuration = CompleteProductionSettings();
        configuration["PublicWeb:Origin"] = "https://staging.mapandmuster.com";
        var built = new ConfigurationBuilder().AddInMemoryCollection(configuration).Build();
        ProductionConfiguration.Validate(built, new TestHostEnvironment { EnvironmentName = "Staging" });
    }

    [Fact]
    public void StagingRejectsPublicOriginWithoutStagingDnsLabel()
    {
        var configuration = CompleteProductionSettings();
        configuration["PublicWeb:Origin"] = "https://mapandmuster.com";
        var built = new ConfigurationBuilder().AddInMemoryCollection(configuration).Build();

        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfiguration.Validate(built, new TestHostEnvironment { EnvironmentName = "Staging" }));

        Assert.Contains("staging", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("mapandmuster.com", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductionRejectsStagingPublicOrigin()
    {
        var configuration = CompleteProductionSettings();
        configuration["PublicWeb:Origin"] = "https://staging.mapandmuster.com";
        var built = new ConfigurationBuilder().AddInMemoryCollection(configuration).Build();

        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfiguration.Validate(built, new TestHostEnvironment()));

        Assert.Contains("staging", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("staging.mapandmuster.com", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductionHostFailsFastWithoutSecrets()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("ConnectionStrings:Campaign", exception.ToString(), StringComparison.Ordinal);
    }

    private static Dictionary<string, string?> CompleteProductionSettings()
    {
        return new Dictionary<string, string?>
        {
            ["ConnectionStrings:Campaign"] = "Host=db.example.test;Database=mapandmuster;Username=mapandmuster;Password=not-a-real-secret",
            ["PublicWeb:Origin"] = "https://mapandmuster.com",
            ["Email:Provider"] = "Resend",
            ["Email:FromAddress"] = "noreply@mapandmuster.com",
            ["Email:Resend:ApiKey"] = "re_not_a_real_secret",
            ["Identity:BootstrapAdminPassword"] = "Bootstrap-Admin-1!",
            ["Identity:BootstrapAdminEmail"] = "admin@example.test",
        };
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;

        public string ApplicationName { get; set; } = "MapAndMuster.Api";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
