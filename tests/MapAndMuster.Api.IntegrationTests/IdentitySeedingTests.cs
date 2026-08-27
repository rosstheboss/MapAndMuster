using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MapAndMuster.Api.Contracts;
using MapAndMuster.Application.Identity;
using MapAndMuster.Infrastructure.Identity;
using MapAndMuster.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace MapAndMuster.Api.IntegrationTests;

public sealed class IdentitySeedApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string BootstrapAdminPassword = "Bootstrap-Admin-1!";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("mapandmuster_identity_seed")
        .WithUsername("mapandmuster")
        .WithPassword("mapandmuster")
        .Build();

    private readonly string _storagePath = Path.Combine(Path.GetTempPath(), "mapandmuster-identity-seed", Guid.NewGuid().ToString("N"));

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
        var port = _postgres.GetMappedPublicPort(5432);
        builder.UseSetting(
            "ConnectionStrings:Campaign",
            $"postgresql://mapandmuster:mapandmuster@{_postgres.Hostname}:{port}/mapandmuster_identity_seed");
        builder.UseSetting("Email:SmtpHost", string.Empty);
        builder.UseSetting("Email:Provider", "Smtp");
        builder.UseSetting("Storage:RootPath", _storagePath);
        builder.UseSetting("PublicWeb:Origin", "http://localhost");
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
        builder.UseSetting("Identity:SeedTestAccounts", "true");
        builder.UseSetting(IdentityBootstrapOptions.BootstrapAdminPasswordKey, BootstrapAdminPassword);
        builder.UseSetting(
            IdentityBootstrapOptions.BootstrapAdminEmailKey,
            IdentityMaintenance.DevelopmentBootstrapAdminEmail);
    }
}

[CollectionDefinition("identity-seed")]
public sealed class IdentitySeedTestGroup : ICollectionFixture<IdentitySeedApiFactory>
{
}

[Collection("identity-seed")]
public sealed class IdentitySeedingTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IdentitySeedApiFactory _factory;

    public IdentitySeedingTests(IdentitySeedApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StartupCreatesThePrivilegedAdministratorAndTestAccountsWithoutResettingPasswords()
    {
        using var client = _factory.CreateClient();
        using var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = IdentityMaintenance.DevelopmentBootstrapAdminEmail, password = IdentitySeedApiFactory.BootstrapAdminPassword });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var profile = await login.Content.ReadFromJsonAsync<OwnProfileResponse>(JsonOptions);
        Assert.NotNull(profile);
        Assert.Equal(IdentityMaintenance.PrivilegedUsername, profile.Username);
        Assert.Equal("Admin", profile.FirstName);
        Assert.Equal("Operator", profile.LastName);
        Assert.Equal("Testville", profile.City);
        Assert.True(profile.IsAdministrator);
        Assert.False(profile.IsTestAccount);

        using var testUsersResponse = await client.GetAsync("/api/auth/test-users");
        Assert.Equal(HttpStatusCode.OK, testUsersResponse.StatusCode);
        var testUsers = await testUsersResponse.Content.ReadFromJsonAsync<TestAccountResponse[]>(JsonOptions);
        Assert.NotNull(testUsers);
        Assert.Equal(TestAccountCatalog.Count, testUsers.Length);
        Assert.Equal("test1", testUsers[0].Username);
        Assert.Equal("Test 1", testUsers[0].DisplayName);
        Assert.Equal(TestAccountCatalog.Username(TestAccountCatalog.Count), testUsers[^1].Username);

        using var testLogin = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = TestAccountCatalog.Email(1), password = IdentitySeedApiFactory.BootstrapAdminPassword });
        Assert.Equal(HttpStatusCode.Unauthorized, testLogin.StatusCode);

        using var impersonate = await client.PostAsync($"/api/auth/test-users/{testUsers[0].Id}/impersonate", null);
        Assert.Equal(HttpStatusCode.OK, impersonate.StatusCode);
        var impersonated = await impersonate.Content.ReadFromJsonAsync<OwnProfileResponse>(JsonOptions);
        Assert.NotNull(impersonated);
        Assert.True(impersonated.IsTestAccount);
        Assert.True(impersonated.IsImpersonating);
        Assert.Equal(1, impersonated.TestAccountNumber);

        using var scope = _factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var identity = scope.ServiceProvider.GetRequiredService<IdentityMaintenance>();
        var admin = await users.FindByEmailAsync(IdentityMaintenance.DevelopmentBootstrapAdminEmail);
        Assert.NotNull(admin);

        const string changedPassword = "Changed-Admin-2!";
        var token = await users.GeneratePasswordResetTokenAsync(admin);
        var reset = await users.ResetPasswordAsync(admin, token, changedPassword);
        Assert.True(reset.Succeeded, string.Join("; ", reset.Errors.Select(static error => error.Description)));

        await identity.EnsureAsync(CancellationToken.None);

        using var verifyClient = _factory.CreateClient();
        using var oldPassword = await verifyClient.PostAsJsonAsync(
            "/api/auth/login",
            new { email = IdentityMaintenance.DevelopmentBootstrapAdminEmail, password = IdentitySeedApiFactory.BootstrapAdminPassword });
        Assert.Equal(HttpStatusCode.Unauthorized, oldPassword.StatusCode);

        using var newPassword = await verifyClient.PostAsJsonAsync(
            "/api/auth/login",
            new { email = IdentityMaintenance.DevelopmentBootstrapAdminEmail, password = changedPassword });
        Assert.Equal(HttpStatusCode.OK, newPassword.StatusCode);
    }
}

[Collection("api")]
public sealed class IdentityTestingIsolationTests
{
    private readonly MapAndMusterApiFactory _factory;

    public IdentityTestingIsolationTests(MapAndMusterApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TestingEnvironmentDoesNotSeedAdministratorOrTestAccounts()
    {
        using var scope = _factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        Assert.Null(await users.FindByEmailAsync(IdentityMaintenance.DevelopmentBootstrapAdminEmail));
        Assert.Null(await users.FindByNameAsync(IdentityMaintenance.PrivilegedUsername));
        Assert.Null(await users.FindByNameAsync(TestAccountCatalog.Username(1)));
        Assert.Null(await users.FindByNameAsync(TestAccountCatalog.Username(30)));
    }
}
