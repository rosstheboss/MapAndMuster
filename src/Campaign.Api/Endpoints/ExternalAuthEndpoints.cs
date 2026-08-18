using System.Security.Claims;
using Campaign.Api.Contracts;
using Campaign.Application.Common;
using Campaign.Application.Identity;
using Campaign.Application.Ports;
using Campaign.Infrastructure.Email;
using Campaign.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Campaign.Api.Endpoints;

/// <summary>
/// Maps external-login challenge, callback, and completion endpoints.
/// </summary>
public static class ExternalAuthEndpoints
{
    /// <summary>
    /// Maps external authentication routes.
    /// </summary>
    /// <param name="app">The application.</param>
    public static void MapExternalAuthEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapGet("/external/{provider}/challenge", ChallengeAsync)
            .AllowAnonymous()
            .RequireRateLimiting(IdentityHttp.AuthRateLimitPolicy)
            .WithName("ExternalChallenge");

        group.MapGet("/external/callback", CallbackAsync)
            .AllowAnonymous()
            .WithName("ExternalCallback");

        group.MapGet("/external/pending", GetPendingAsync)
            .AllowAnonymous()
            .WithName("GetPendingExternalRegistration")
            .Produces<PendingExternalProfileResponse>()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/external/complete", CompleteAsync)
            .AllowAnonymous()
            .RequireRateLimiting(IdentityHttp.AuthRateLimitPolicy)
            .WithName("CompleteExternalRegistration")
            .Produces<OwnProfileResponse>()
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);
    }

    private static IResult ChallengeAsync(
        string provider,
        IConfiguration configuration,
        SignInManager<ApplicationUser> signInManager,
        HttpRequest request)
    {
        if (!ExternalAuthentication.IsConfigured(configuration, provider))
        {
            return IdentityHttp.Problem(ErrorCodes.ExternalProviderUnavailable, "That sign-in provider is not configured.");
        }

        var redirectUrl = $"{request.Scheme}://{request.Host}/api/auth/external/callback";
        var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Results.Challenge(properties, [provider]);
    }

    private static async Task<IResult> CallbackAsync(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IdentityMaintenance identity,
        IOptions<PublicWebOptions> webOptions)
    {
        var origin = webOptions.Value.Origin.TrimEnd('/');
        var info = await signInManager.GetExternalLoginInfoAsync().ConfigureAwait(false);
        if (info is null)
        {
            return Results.Redirect($"{origin}/login?error=external");
        }

        var existing = await signInManager.ExternalLoginSignInAsync(
                info.LoginProvider,
                info.ProviderKey,
                isPersistent: true,
                bypassTwoFactor: true)
            .ConfigureAwait(false);
        if (existing.Succeeded)
        {
            var signedIn = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey).ConfigureAwait(false);
            if (signedIn is not null)
            {
                await identity.PromoteIfPrivilegedAsync(signedIn).ConfigureAwait(false);
            }

            return Results.Redirect($"{origin}/");
        }

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (!string.IsNullOrWhiteSpace(email))
        {
            var existingEmail = await userManager.FindByEmailAsync(email).ConfigureAwait(false);
            if (existingEmail is not null)
            {
                return Results.Redirect($"{origin}/login?error=link-required");
            }
        }

        var pendingIdentity = new ClaimsIdentity(IdentityHttp.ExternalRegistrationScheme);
        pendingIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, info.ProviderKey));
        pendingIdentity.AddClaim(new Claim(ExternalAuthentication.ProviderClaim, info.LoginProvider));
        if (!string.IsNullOrWhiteSpace(email))
        {
            pendingIdentity.AddClaim(new Claim(ClaimTypes.Email, email));
        }

        CopyClaim(info.Principal, pendingIdentity, ClaimTypes.GivenName);
        CopyClaim(info.Principal, pendingIdentity, ClaimTypes.Surname);
        CopyClaim(info.Principal, pendingIdentity, ClaimTypes.Name);
        CopyClaim(info.Principal, pendingIdentity, ExternalAuthentication.AvatarUrlClaim);
        if (info.Principal.FindFirstValue("email_verified") is { } verified)
        {
            pendingIdentity.AddClaim(new Claim("email_verified", verified));
        }

        await signInManager.Context.SignInAsync(
                IdentityHttp.ExternalRegistrationScheme,
                new ClaimsPrincipal(pendingIdentity))
            .ConfigureAwait(false);

        return Results.Redirect($"{origin}/complete-external");
    }

    private static async Task<IResult> GetPendingAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = await httpContext.AuthenticateAsync(IdentityHttp.ExternalRegistrationScheme).ConfigureAwait(false);
        if (!result.Succeeded || result.Principal is null)
        {
            return IdentityHttp.Problem(ErrorCodes.ExternalProfileIncomplete, "Finish signing in with the external provider first.");
        }

        var principal = result.Principal;
        var fullName = principal.FindFirstValue(ClaimTypes.Name);
        var firstName = principal.FindFirstValue(ClaimTypes.GivenName) ?? SplitFirst(fullName);
        var lastName = principal.FindFirstValue(ClaimTypes.Surname) ?? SplitLast(fullName);

        return Results.Ok(new PendingExternalProfileResponse(
            principal.FindFirstValue(ExternalAuthentication.ProviderClaim) ?? string.Empty,
            principal.FindFirstValue(ClaimTypes.Email),
            firstName,
            lastName,
            principal.FindFirstValue(ExternalAuthentication.AvatarUrlClaim)));
    }

    private static async Task<IResult> CompleteAsync(
        [FromBody] CompleteExternalRegistrationRequest request,
        HttpContext httpContext,
        CompleteExternalRegistrationHandler handler,
        SignInManager<ApplicationUser> signInManager,
        IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var pending = await httpContext.AuthenticateAsync(IdentityHttp.ExternalRegistrationScheme).ConfigureAwait(false);
        if (!pending.Succeeded || pending.Principal is null)
        {
            return IdentityHttp.Problem(ErrorCodes.ExternalProfileIncomplete, "Finish signing in with the external provider first.");
        }

        if (!IdentityHttp.TryParseDisplayNameMode(request.DisplayNameMode, out var displayNameMode))
        {
            return IdentityHttp.Problem("displayNameMode.invalid", "Choose whether other users see your username or full name.");
        }

        var principal = pending.Principal;
        var email = principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            return IdentityHttp.Problem(ErrorCodes.EmailInvalid, "The external provider did not supply an email address.");
        }

        var provider = principal.FindFirstValue(ExternalAuthentication.ProviderClaim);
        var providerKey = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(providerKey))
        {
            return IdentityHttp.Problem(ErrorCodes.ExternalProfileIncomplete, "Finish signing in with the external provider first.");
        }

        Stream? avatar = await DownloadAvatarAsync(
                httpClientFactory,
                principal.FindFirstValue(ExternalAuthentication.AvatarUrlClaim),
                cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var created = await handler.HandleAsync(
                    new CompleteExternalRegistrationCommand
                    {
                        Email = email,
                        EmailConfirmed = true,
                        Username = request.Username,
                        FirstName = request.FirstName,
                        MiddleInitial = request.MiddleInitial,
                        LastName = request.LastName,
                        Suffix = request.Suffix,
                        City = request.City,
                        Region = request.Region,
                        Country = request.Country,
                        TimeZoneId = request.TimeZoneId,
                        DisplayNameMode = displayNameMode,
                        Provider = provider,
                        ProviderKey = providerKey,
                        AvatarContent = avatar,
                        AvatarContentType = "image/png",
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            if (!created.IsSuccess || created.Value is null)
            {
                return IdentityHttp.Problem(created);
            }

            var user = await signInManager.UserManager.FindByIdAsync(created.Value.Id.ToString()).ConfigureAwait(false);
            if (user is null)
            {
                return IdentityHttp.Problem(ErrorCodes.ProfileNotFound, "The profile was not found.");
            }

            await httpContext.SignOutAsync(IdentityHttp.ExternalRegistrationScheme).ConfigureAwait(false);
            var identity = httpContext.RequestServices.GetRequiredService<IdentityMaintenance>();
            await identity.PromoteIfPrivilegedAsync(user).ConfigureAwait(false);
            await signInManager.SignInAsync(user, isPersistent: true).ConfigureAwait(false);
            return Results.Ok(ProfileResponses.FromAccount(
                created.Value,
                await signInManager.UserManager.IsInRoleAsync(user, IdentityMaintenance.AdministratorRole).ConfigureAwait(false)));
        }
        finally
        {
            if (avatar is not null)
            {
                await avatar.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private static void CopyClaim(ClaimsPrincipal source, ClaimsIdentity target, string claimType)
    {
        var value = source.FindFirstValue(claimType);
        if (!string.IsNullOrWhiteSpace(value))
        {
            target.AddClaim(new Claim(claimType, value));
        }
    }

    private static string SplitFirst(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return string.Empty;
        }

        var parts = fullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return parts[0];
    }

    private static string SplitLast(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return string.Empty;
        }

        var parts = fullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? parts[1] : string.Empty;
    }

    private static async Task<Stream?> DownloadAvatarAsync(
        IHttpClientFactory httpClientFactory,
        string? url,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(url)
            || !Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps)
        {
            return null;
        }

        try
        {
            var client = httpClientFactory.CreateClient("external-avatar");
            using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var declaredLength = response.Content.Headers.ContentLength;
            if (declaredLength > IAvatarImageProcessor.MaxUploadBytes)
            {
                return null;
            }

            var memory = new MemoryStream();
            await response.Content.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);
            if (memory.Length > IAvatarImageProcessor.MaxUploadBytes)
            {
                await memory.DisposeAsync().ConfigureAwait(false);
                return null;
            }

            memory.Position = 0;
            return memory;
        }
#pragma warning disable CA1031
        catch (Exception)
#pragma warning restore CA1031
        {
            return null;
        }
    }
}

/// <summary>
/// Prefill data imported from an external provider before the user finishes registration.
/// </summary>
/// <param name="Provider">The provider name.</param>
/// <param name="Email">The imported email, if any.</param>
/// <param name="FirstName">The imported first name, if any.</param>
/// <param name="LastName">The imported last name, if any.</param>
/// <param name="AvatarUrl">The imported avatar URL, if any.</param>
public sealed record PendingExternalProfileResponse(
    string Provider,
    string? Email,
    string? FirstName,
    string? LastName,
    string? AvatarUrl);
