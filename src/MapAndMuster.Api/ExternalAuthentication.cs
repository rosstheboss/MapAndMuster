using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using MapAndMuster.Api.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;

namespace MapAndMuster.Api;

/// <summary>
/// Optional Google, Facebook, and Discord login. Providers are registered only when credentials are configured.
/// </summary>
public static class ExternalAuthentication
{
    /// <summary>
    /// Google authentication scheme name.
    /// </summary>
    public const string Google = "Google";

    /// <summary>
    /// Facebook authentication scheme name.
    /// </summary>
    public const string Facebook = "Facebook";

    /// <summary>
    /// Discord authentication scheme name.
    /// </summary>
    public const string Discord = "Discord";

    /// <summary>
    /// Claim type for an imported avatar URL.
    /// </summary>
    public const string AvatarUrlClaim = "campaign.avatar_url";

    /// <summary>
    /// Claim type for the originating provider name.
    /// </summary>
    public const string ProviderClaim = "campaign.provider";

    /// <summary>
    /// Returns providers that have credentials configured.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The configured providers.</returns>
    public static IReadOnlyList<ExternalProviderResponse> GetConfiguredProviders(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var providers = new List<ExternalProviderResponse>();
        if (HasCredentials(configuration, "Authentication:Google:ClientId", "Authentication:Google:ClientSecret"))
        {
            providers.Add(new ExternalProviderResponse(Google, "Google"));
        }

        if (HasCredentials(configuration, "Authentication:Facebook:AppId", "Authentication:Facebook:AppSecret"))
        {
            providers.Add(new ExternalProviderResponse(Facebook, "Facebook"));
        }

        if (HasCredentials(configuration, "Authentication:Discord:ClientId", "Authentication:Discord:ClientSecret"))
        {
            providers.Add(new ExternalProviderResponse(Discord, "Discord"));
        }

        return providers;
    }

    /// <summary>
    /// Adds configured external authentication handlers.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddCampaignExternalAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddAuthentication()
            .AddCookie(IdentityHttp.ExternalRegistrationScheme, options =>
            {
                options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
                options.Cookie.Name = "campaign.external";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            });

        if (HasCredentials(configuration, "Authentication:Google:ClientId", "Authentication:Google:ClientSecret"))
        {
            services.AddAuthentication().AddGoogle(Google, options =>
            {
                options.ClientId = configuration["Authentication:Google:ClientId"]!;
                options.ClientSecret = configuration["Authentication:Google:ClientSecret"]!;
                options.CallbackPath = "/api/auth/external/google/callback";
                options.ClaimActions.MapJsonKey(AvatarUrlClaim, "picture");
            });
        }

        if (HasCredentials(configuration, "Authentication:Facebook:AppId", "Authentication:Facebook:AppSecret"))
        {
            services.AddAuthentication().AddFacebook(Facebook, options =>
            {
                options.AppId = configuration["Authentication:Facebook:AppId"]!;
                options.AppSecret = configuration["Authentication:Facebook:AppSecret"]!;
                options.CallbackPath = "/api/auth/external/facebook/callback";
                options.Fields.Add("first_name");
                options.Fields.Add("last_name");
                options.Fields.Add("picture");
                options.ClaimActions.MapJsonKey(ClaimTypes.GivenName, "first_name");
                options.ClaimActions.MapJsonKey(ClaimTypes.Surname, "last_name");
                options.Events.OnCreatingTicket = context =>
                {
                    MapFacebookPicture(context);
                    return Task.CompletedTask;
                };
            });
        }

        if (HasCredentials(configuration, "Authentication:Discord:ClientId", "Authentication:Discord:ClientSecret"))
        {
            services.AddAuthentication().AddOAuth(Discord, Discord, options =>
            {
                options.ClientId = configuration["Authentication:Discord:ClientId"]!;
                options.ClientSecret = configuration["Authentication:Discord:ClientSecret"]!;
                options.CallbackPath = "/api/auth/external/discord/callback";
                options.AuthorizationEndpoint = "https://discord.com/api/oauth2/authorize";
                options.TokenEndpoint = "https://discord.com/api/oauth2/token";
                options.UserInformationEndpoint = "https://discord.com/api/users/@me";
                options.Scope.Add("identify");
                options.Scope.Add("email");
                options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
                options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
                options.ClaimActions.MapJsonKey(ClaimTypes.Name, "global_name");
                options.ClaimActions.MapJsonKey("urn:discord:username", "username");
                options.Events.OnCreatingTicket = async context =>
                {
                    await MapDiscordUserAsync(context).ConfigureAwait(false);
                };
            });
        }

        return services;
    }

    /// <summary>
    /// Returns whether a named provider is configured.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="provider">The provider scheme name.</param>
    /// <returns><see langword="true"/> when the provider can be challenged.</returns>
    public static bool IsConfigured(IConfiguration configuration, string provider)
    {
        return GetConfiguredProviders(configuration)
            .Any(item => string.Equals(item.Name, provider, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasCredentials(IConfiguration configuration, string idKey, string secretKey)
    {
        return !string.IsNullOrWhiteSpace(configuration[idKey])
            && !string.IsNullOrWhiteSpace(configuration[secretKey]);
    }

    private static void MapFacebookPicture(OAuthCreatingTicketContext context)
    {
        using var document = JsonDocument.Parse(context.User.GetRawText());
        if (document.RootElement.TryGetProperty("picture", out var picture)
            && picture.TryGetProperty("data", out var data)
            && data.TryGetProperty("url", out var url))
        {
            context.Identity?.AddClaim(new Claim(AvatarUrlClaim, url.GetString() ?? string.Empty));
        }
    }

    private static async Task MapDiscordUserAsync(OAuthCreatingTicketContext context)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await context.Backchannel.SendAsync(request).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
        context.RunClaimActions(payload.RootElement);

        var id = payload.RootElement.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;
        var avatar = payload.RootElement.TryGetProperty("avatar", out var avatarElement) ? avatarElement.GetString() : null;
        if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(avatar))
        {
            context.Identity?.AddClaim(new Claim(AvatarUrlClaim, $"https://cdn.discordapp.com/avatars/{id}/{avatar}.png"));
        }
    }
}
