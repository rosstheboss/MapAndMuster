using MapAndMuster.Application;
using MapAndMuster.Application.Ports;
using MapAndMuster.Infrastructure.Campaigns;
using MapAndMuster.Infrastructure.Email;
using MapAndMuster.Infrastructure.Identity;
using MapAndMuster.Infrastructure.Persistence;
using MapAndMuster.Infrastructure.Security;
using MapAndMuster.Infrastructure.Storage;
using MapAndMuster.Infrastructure.Time;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MapAndMuster.Infrastructure;

/// <summary>
/// Registers infrastructure adapters with the composition root.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Adds persistence, Identity stores, file storage, email outbox, and the system clock.
    /// </summary>
    /// <param name="services">The application service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddCampaignInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddCampaignApplication();
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.Configure<PublicWebOptions>(configuration.GetSection(PublicWebOptions.SectionName));
        services.Configure<IdentityBootstrapOptions>(configuration.GetSection(IdentityBootstrapOptions.SectionName));

        var connectionString = configuration.GetConnectionString("Campaign");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Host=127.0.0.1;Database=campaign_unconfigured;Username=campaign;Password=campaign";
        }
        else
        {
            connectionString = PostgresConnectionString.Normalize(connectionString);
        }

        RegisterEmailDelivery(services, configuration);

        services.AddDbContext<CampaignDbContext>(options => options.UseNpgsql(connectionString));
        services
            .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_";
                options.SignIn.RequireConfirmedEmail = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
            })
            .AddEntityFrameworkStores<CampaignDbContext>()
            .AddDefaultTokenProviders();

        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IUserAccountStore, UserAccountStore>();
        services.AddScoped<IdentityMaintenance>();
        services.AddScoped<LocalTestCampaignSeeder>();
        services.AddSingleton<ICampaignPresetPackageCodec, CampaignPresetPackageCodec>();
        services.AddScoped<ICampaignStore, CampaignStore>();
        services.AddScoped<ICampaignLogReadStore, CampaignLogReadStore>();
        services.AddScoped<ICampaignPresetStore, CampaignPresetStore>();
        services.AddScoped<IUserNotificationStore, UserNotificationStore>();
        services.AddScoped<INewsStore, NewsStore>();
        services.AddScoped<ISiteChatStore, SiteChatStore>();
        services.AddScoped<IEmailOutbox, EmailOutbox>();
        services.AddSingleton<ISecretHasher, Pbkdf2SecretHasher>();
        services.AddSingleton<IAvatarImageProcessor, AvatarImageProcessor>();
        services.AddSingleton<IAvatarStorage, FileAvatarStorage>();
        services.AddSingleton<ICampaignMapProcessor, CampaignMapProcessor>();
        services.AddSingleton<FileCampaignMapStorage>();
        services.AddSingleton<ICampaignMapStorage>(static services => services.GetRequiredService<FileCampaignMapStorage>());
        services.AddSingleton<ICampaignAssetStorage>(static services => services.GetRequiredService<FileCampaignMapStorage>());
        services.AddSingleton<ICampaignDocumentProcessor, CampaignDocumentProcessor>();

        return services;
    }

    private static void RegisterEmailDelivery(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient(ResendEmailSender.HttpClientName, client =>
        {
            client.BaseAddress = new Uri("https://api.resend.com/");
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddTransient<IEmailSender>(CreateEmailSender);

        var emailOptions = configuration.GetSection(EmailOptions.SectionName).Get<EmailOptions>() ?? new EmailOptions();
        if (!string.IsNullOrWhiteSpace(configuration.GetConnectionString("Campaign")) && emailOptions.IsDeliveryConfigured)
        {
            services.AddHostedService<OutboxEmailProcessor>();
        }
    }

    private static IEmailSender CreateEmailSender(IServiceProvider services)
    {
        var options = services.GetRequiredService<IOptions<EmailOptions>>().Value;
        if (options.UsesResend)
        {
            var factory = services.GetRequiredService<IHttpClientFactory>();
            return new ResendEmailSender(factory.CreateClient(ResendEmailSender.HttpClientName), options);
        }

        return new SmtpEmailSender(options);
    }
}
