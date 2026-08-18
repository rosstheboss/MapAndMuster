using Campaign.Application.Campaigns;
using Campaign.Application.Chat;
using Campaign.Application.Identity;
using Campaign.Application.Maps;
using Campaign.Application.News;
using Campaign.Application.Notifications;
using Campaign.Application.Play;
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
        services.AddScoped<ListDiscoverableCampaignsHandler>();
        services.AddScoped<GetCampaignHandler>();
        services.AddScoped<PostCampaignChatHandler>();
        services.AddScoped<JoinCampaignHandler>();
        services.AddScoped<LeaveCampaignHandler>();
        services.AddScoped<SearchCampaignUsersHandler>();
        services.AddScoped<AddCampaignMemberHandler>();
        services.AddScoped<KickCampaignMemberHandler>();
        services.AddScoped<AssignPlayerFactionHandler>();
        services.AddScoped<DeleteCampaignHandler>();
        services.AddScoped<DuplicateCampaignHandler>();
        services.AddScoped<UploadCampaignMapHandler>();
        services.AddScoped<GetCampaignMapHandler>();
        services.AddScoped<GetCampaignMapGraphHandler>();
        services.AddScoped<SaveCampaignMapGraphHandler>();
        services.AddScoped<UploadStructureImageHandler>();
        services.AddScoped<GetStructureImageHandler>();
        services.AddScoped<UploadItemObjectiveImageHandler>();
        services.AddScoped<GetItemObjectiveImageHandler>();
        services.AddScoped<UploadFactionFlagHandler>();
        services.AddScoped<GetFactionFlagHandler>();
        services.AddScoped<UploadMissionFileHandler>();
        services.AddScoped<GetMissionFileHandler>();
        services.AddScoped<GetCampaignPlayHandler>();
        services.AddScoped<SaveOrderDraftHandler>();
        services.AddScoped<CommitOrdersHandler>();
        services.AddScoped<UncommitOrdersHandler>();
        services.AddScoped<SubmitBattleResultHandler>();
        services.AddScoped<AcceptBattleResultHandler>();
        services.AddScoped<ResolveBattleHandler>();
        services.AddScoped<SubmitRetreatHandler>();
        services.AddScoped<ExtendCampaignScheduleHandler>();
        services.AddScoped<ChooseFactionHandler>();
        services.AddScoped<EnterCampaignDebugHandler>();
        services.AddScoped<ExitCampaignDebugHandler>();
        services.AddScoped<DebugCorrectOrderHandler>();
        services.AddScoped<RevealHiddenItemObjectivesHandler>();
        services.AddScoped<SetPublicObjectiveAwardHandler>();
        services.AddScoped<GrantPrivateObjectiveHandler>();
        services.AddScoped<ClaimPrivateObjectiveHandler>();
        services.AddScoped<ModeratePrivateObjectiveHandler>();
        services.AddScoped<ResolveItemObjectiveChoiceHandler>();

        services.AddScoped<CampaignNotificationPublisher>();
        services.AddScoped<GetHomeBoardHandler>();
        services.AddScoped<MarkNotificationReadHandler>();
        services.AddScoped<GetNewsPageHandler>();
        services.AddScoped<SaveNewsArticleHandler>();
        services.AddScoped<DeleteNewsArticleHandler>();
        services.AddScoped<GetSiteChatHandler>();
        services.AddScoped<PostSiteChatHandler>();
        services.AddScoped<SetSiteChatBlockHandler>();
        services.AddScoped<SiteChatNotificationPublisher>();

        return services;
    }
}
