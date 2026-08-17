using Campaign.Application;
using Campaign.Application.Ports;
using Campaign.Infrastructure.Campaigns;
using Campaign.Infrastructure.Email;
using Campaign.Infrastructure.Identity;
using Campaign.Infrastructure.Persistence;
using Campaign.Infrastructure.Security;
using Campaign.Infrastructure.Storage;
using Campaign.Infrastructure.Time;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Campaign.Infrastructure;

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

        var connectionString = configuration.GetConnectionString("Campaign");
        var smtpHost = configuration[$"{EmailOptions.SectionName}:SmtpHost"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Host=127.0.0.1;Database=campaign_unconfigured;Username=campaign;Password=campaign";
        }
        else if (!string.IsNullOrWhiteSpace(smtpHost))
        {
            services.AddHostedService<OutboxEmailProcessor>();
        }

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
        services.AddScoped<ICampaignStore, CampaignStore>();
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
}
