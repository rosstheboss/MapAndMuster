using Campaign.Application.Campaigns;
using Campaign.Application.Identity;
using Campaign.Application.Maps;
using Microsoft.Extensions.DependencyInjection;

namespace Campaign.Application;

/// <summary>
/// Registers application use cases.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Adds campaign application handlers.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddCampaignApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<RegisterAccountHandler>();
        services.AddScoped<UpdateProfileHandler>();
        services.AddScoped<UploadAvatarHandler>();
        services.AddScoped<GetOwnProfileHandler>();
        services.AddScoped<GetPublicProfileHandler>();
        services.AddScoped<CompleteExternalRegistrationHandler>();
        services.AddScoped<ChangePasswordHandler>();
        services.AddScoped<CreateCampaignHandler>();
        services.AddScoped<UpdateCampaignHandler>();
        services.AddScoped<ListCampaignsHandler>();
        services.AddScoped<GetCampaignHandler>();
        services.AddScoped<DeleteCampaignHandler>();
        services.AddScoped<UploadCampaignMapHandler>();
        services.AddScoped<GetCampaignMapHandler>();
        services.AddScoped<GetCampaignMapGraphHandler>();
        services.AddScoped<SaveCampaignMapGraphHandler>();
        services.AddScoped<UploadStructureImageHandler>();
        services.AddScoped<GetStructureImageHandler>();
        services.AddScoped<UploadMissionFileHandler>();
        services.AddScoped<GetMissionFileHandler>();

        return services;
    }
}
