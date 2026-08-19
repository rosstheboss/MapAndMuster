using System.Security.Claims;
using System.Text.Json;
using Campaign.Api.Contracts;
using Campaign.Application.Common;
using Campaign.Application.Identity;
using Campaign.Application.Ports;
using Campaign.Domain.Identity;
using Campaign.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Campaign.Api.Endpoints;

/// <summary>
/// Maps authentication HTTP endpoints.
/// </summary>
public static class AuthEndpoints
{
    /// <summary>
    /// Maps authentication routes.
    /// </summary>
    /// <param name="app">The application.</param>
    public static void MapAuthEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", RegisterAsync)
            .AllowAnonymous()
            .RequireRateLimiting(IdentityHttp.AuthRateLimitPolicy)
            .WithName("Register")
            .Produces<RegisterResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict);

        group.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .RequireRateLimiting(IdentityHttp.AuthRateLimitPolicy)
            .WithName("Login")
            .Produces<OwnProfileResponse>()
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden);

        group.MapPost("/logout", LogoutAsync)
            .RequireAuthorization()
            .WithName("Logout")
            .Produces(StatusCodes.Status204NoContent);

        group.MapGet("/me", GetMeAsync)
            .RequireAuthorization()
            .WithName("GetCurrentUser")
            .Produces<OwnProfileResponse>()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/confirm-email", ConfirmEmailAsync)
            .AllowAnonymous()
            .RequireRateLimiting(IdentityHttp.AuthRateLimitPolicy)
            .WithName("ConfirmEmail")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/resend-confirmation", ResendConfirmationAsync)
            .AllowAnonymous()
            .RequireRateLimiting(IdentityHttp.AuthRateLimitPolicy)
            .WithName("ResendConfirmation")
            .Produces(StatusCodes.Status202Accepted);

        group.MapPost("/forgot-password", ForgotPasswordAsync)
            .AllowAnonymous()
            .RequireRateLimiting(IdentityHttp.AuthRateLimitPolicy)
            .WithName("ForgotPassword")
            .Produces(StatusCodes.Status202Accepted);

        group.MapPost("/reset-password", ResetPasswordAsync)
            .AllowAnonymous()
            .RequireRateLimiting(IdentityHttp.AuthRateLimitPolicy)
            .WithName("ResetPassword")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/change-password", ChangePasswordAsync)
            .RequireAuthorization()
            .RequireRateLimiting(IdentityHttp.AuthRateLimitPolicy)
            .WithName("ChangePassword")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);

        group.MapGet("/test-users", ListTestUsersAsync)
            .RequireAuthorization()
            .WithName("ListTestUsers")
            .Produces<IReadOnlyList<TestAccountResponse>>()
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden);

        group.MapPost("/test-users/{userId:guid}/impersonate", ImpersonateTestUserAsync)
            .RequireAuthorization()
            .RequireRateLimiting(IdentityHttp.AuthRateLimitPolicy)
            .WithName("ImpersonateTestUser")
            .Produces<OwnProfileResponse>()
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapPost("/stop-impersonation", StopImpersonationAsync)
            .RequireAuthorization()
            .WithName("StopImpersonation")
            .Produces<OwnProfileResponse>()
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden);

        group.MapGet("/external-providers", GetExternalProviders)
            .AllowAnonymous()
            .WithName("GetExternalProviders")
            .Produces<IReadOnlyList<ExternalProviderResponse>>();
    }

    private static async Task<IResult> RegisterAsync(
        HttpRequest httpRequest,
        RegisterAccountHandler handler,
        UserManager<ApplicationUser> userManager,
        IdentityMaintenance identity,
        CancellationToken cancellationToken)
    {
        RegisterRequest? request;
        Stream? avatar = null;
        string? avatarContentType = null;
        long? avatarLength = null;

        if (httpRequest.HasFormContentType)
        {
            var form = await httpRequest.ReadFormAsync(cancellationToken).ConfigureAwait(false);
            request = new RegisterRequest
            {
                Email = form["email"].ToString(),
                Username = form["username"].ToString(),
                Password = form["password"].ToString(),
                FirstName = form["firstName"].ToString(),
                MiddleInitial = NullIfEmpty(form["middleInitial"].ToString()),
                LastName = form["lastName"].ToString(),
                Suffix = NullIfEmpty(form["suffix"].ToString()),
                City = form["city"].ToString(),
                Region = NullIfEmpty(form["region"].ToString()),
                Country = form["country"].ToString(),
                TimeZoneId = NullIfEmpty(form["timeZoneId"].ToString()),
                DisplayNameMode = form["displayNameMode"].ToString(),
            };

            var file = form.Files.GetFile("avatar");
            if (file is not null)
            {
                avatar = file.OpenReadStream();
                avatarContentType = file.ContentType;
                avatarLength = file.Length;
            }
        }
        else
        {
            try
            {
                request = await httpRequest.ReadFromJsonAsync<RegisterRequest>(cancellationToken).ConfigureAwait(false);
            }
            catch (JsonException)
            {
                return IdentityHttp.Problem(
                    "request.invalid",
                    "A complete registration body is required.");
            }
        }

        if (request is null)
        {
            return IdentityHttp.Problem("request.invalid", "A registration body is required.");
        }

        if (!IdentityHttp.TryParseDisplayNameMode(request.DisplayNameMode, out var displayNameMode))
        {
            return IdentityHttp.Problem("displayNameMode.invalid", "Choose whether other users see your username or full name.");
        }

        try
        {
            var result = await handler.HandleAsync(
                    new RegisterAccountCommand
                    {
                        Email = request.Email,
                        Username = request.Username,
                        Password = request.Password,
                        FirstName = request.FirstName,
                        MiddleInitial = request.MiddleInitial,
                        LastName = request.LastName,
                        Suffix = request.Suffix,
                        City = request.City,
                        Region = request.Region,
                        Country = request.Country,
                        TimeZoneId = request.TimeZoneId,
                        DisplayNameMode = displayNameMode,
                        AvatarContent = avatar,
                        AvatarContentType = avatarContentType,
                        AvatarLength = avatarLength,
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            if (!result.IsSuccess || result.Value is null)
            {
                return IdentityHttp.Problem(result);
            }

            var created = await userManager.FindByIdAsync(result.Value.UserId.ToString()).ConfigureAwait(false);
            if (created is not null)
            {
                await identity.PromoteIfPrivilegedAsync(created).ConfigureAwait(false);
            }

            return Results.Created(
                $"/api/profiles/{result.Value.Username}",
                new RegisterResponse(result.Value.UserId, result.Value.Username));
        }
        finally
        {
            if (avatar is not null)
            {
                await avatar.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginRequest request,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        GetOwnProfileHandler profiles,
        IdentityMaintenance identity,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userManager.FindByEmailAsync(request.Email).ConfigureAwait(false);
        if (user is null || user.IsTestAccount)
        {
            return IdentityHttp.Problem(ErrorCodes.InvalidCredentials, "Email or password is incorrect.");
        }

        var result = await signInManager.PasswordSignInAsync(user, request.Password, isPersistent: true, lockoutOnFailure: true)
            .ConfigureAwait(false);
        if (result.IsLockedOut)
        {
            return IdentityHttp.Problem(ErrorCodes.LockedOut, "This account is locked. Try again later.");
        }

        if (result.IsNotAllowed)
        {
            return IdentityHttp.Problem(ErrorCodes.EmailNotConfirmed, "Confirm your email before signing in.");
        }

        if (!result.Succeeded)
        {
            return IdentityHttp.Problem(ErrorCodes.InvalidCredentials, "Email or password is incorrect.");
        }

        await identity.PromoteIfPrivilegedAsync(user).ConfigureAwait(false);

        var profile = await profiles.HandleAsync(user.Id, cancellationToken).ConfigureAwait(false);
        if (!profile.IsSuccess || profile.Value is null)
        {
            return IdentityHttp.Problem(profile);
        }

        return Results.Ok(ProfileResponses.FromAccount(
            profile.Value,
            await signInManager.UserManager.IsInRoleAsync(user, IdentityMaintenance.AdministratorRole).ConfigureAwait(false)));
    }

    private static async Task<IResult> LogoutAsync(SignInManager<ApplicationUser> signInManager)
    {
        await signInManager.SignOutAsync().ConfigureAwait(false);
        return Results.NoContent();
    }

    private static async Task<IResult> GetMeAsync(
        ClaimsPrincipal principal,
        GetOwnProfileHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        if (userId is null)
        {
            return IdentityHttp.Problem(ErrorCodes.Unauthorized, "Sign in to continue.");
        }

        var result = await handler.HandleAsync(userId.Value, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
        {
            return IdentityHttp.Problem(result);
        }

        return Results.Ok(ProfileResponses.FromAccount(
            result.Value,
            principal.IsAdministrator(),
            principal.GetImpersonatorUserId() is not null));
    }

    private static async Task<IResult> ConfirmEmailAsync(
        [FromBody] ConfirmEmailRequest request,
        UserManager<ApplicationUser> userManager)
    {
        ArgumentNullException.ThrowIfNull(request);
        var user = await userManager.FindByIdAsync(request.UserId.ToString()).ConfigureAwait(false);
        if (user is null)
        {
            return IdentityHttp.Problem("auth.confirm_failed", "The confirmation link is invalid.");
        }

        var confirmed = await userManager.ConfirmEmailAsync(user, request.Token).ConfigureAwait(false);
        if (!confirmed.Succeeded)
        {
            return IdentityHttp.Problem("auth.confirm_failed", "The confirmation link is invalid.");
        }

        return Results.NoContent();
    }

    private static async Task<IResult> ResendConfirmationAsync(
        [FromBody] ResendConfirmationRequest request,
        UserManager<ApplicationUser> userManager,
        IEmailOutbox outbox,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var user = await userManager.FindByEmailAsync(request.Email).ConfigureAwait(false);
        if (user is not null && !user.EmailConfirmed)
        {
            var token = await userManager.GenerateEmailConfirmationTokenAsync(user).ConfigureAwait(false);
            await outbox.QueueEmailConfirmationAsync(user.Email ?? request.Email, user.Id, token, cancellationToken)
                .ConfigureAwait(false);
        }

        return Results.Accepted();
    }

    private static async Task<IResult> ForgotPasswordAsync(
        [FromBody] ForgotPasswordRequest request,
        UserManager<ApplicationUser> userManager,
        IEmailOutbox outbox,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var user = await userManager.FindByEmailAsync(request.Email).ConfigureAwait(false);
        if (user is not null && user.EmailConfirmed)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user).ConfigureAwait(false);
            await outbox.QueuePasswordResetAsync(user.Email ?? request.Email, user.Id, token, cancellationToken)
                .ConfigureAwait(false);
        }

        return Results.Accepted();
    }

    private static async Task<IResult> ResetPasswordAsync(
        [FromBody] ResetPasswordRequest request,
        UserManager<ApplicationUser> userManager)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!PasswordPolicy.TryValidate(request.Password, out var passwordError))
        {
            return IdentityHttp.Problem(passwordError.Code, passwordError.Message);
        }

        var user = await userManager.FindByIdAsync(request.UserId.ToString()).ConfigureAwait(false);
        if (user is null)
        {
            return IdentityHttp.Problem(ErrorCodes.PasswordInvalid, "The reset link is invalid.");
        }

        var reset = await userManager.ResetPasswordAsync(user, request.Token, request.Password).ConfigureAwait(false);
        if (!reset.Succeeded)
        {
            return IdentityHttp.Problem(ErrorCodes.PasswordInvalid, string.Join(" ", reset.Errors.Select(error => error.Description)));
        }

        return Results.NoContent();
    }

    private static async Task<IResult> ChangePasswordAsync(
        ClaimsPrincipal principal,
        [FromBody] ChangePasswordRequest request,
        ChangePasswordHandler handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var userId = principal.GetUserId();
        if (userId is null)
        {
            return IdentityHttp.Problem(ErrorCodes.Unauthorized, "Sign in to continue.");
        }

        var result = await handler.HandleAsync(
                new ChangePasswordCommand
                {
                    UserId = userId.Value,
                    CurrentPassword = request.CurrentPassword,
                    NewPassword = request.NewPassword,
                },
                cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return IdentityHttp.Problem(result);
        }

        return Results.NoContent();
    }

    private static async Task<IResult> ListTestUsersAsync(
        ClaimsPrincipal principal,
        IUserAccountStore accounts,
        CancellationToken cancellationToken)
    {
        if (!principal.IsAdministrator())
        {
            return IdentityHttp.Problem(ErrorCodes.ImpersonationForbidden, "Only an administrator can list test users.");
        }

        var users = await accounts.ListTestAccountsAsync(cancellationToken).ConfigureAwait(false);
        return Results.Ok(users.Select(static user => new TestAccountResponse
        {
            Id = user.Id,
            Number = user.TestAccountNumber ?? 0,
            Username = user.Username,
            DisplayName = TestAccountCatalog.TryDisplayName(user, out var name) ? name : user.Username,
        }).ToArray());
    }

    private static async Task<IResult> ImpersonateTestUserAsync(
        Guid userId,
        ClaimsPrincipal principal,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        GetOwnProfileHandler profiles,
        CancellationToken cancellationToken)
    {
        var actorId = principal.IsAdministrator() ? principal.GetUserId() : principal.GetImpersonatorUserId();
        if (actorId is null)
        {
            return IdentityHttp.Problem(ErrorCodes.ImpersonationForbidden, "Only an administrator can test as these users.");
        }

        var target = await userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (target is null || !target.IsTestAccount)
        {
            return IdentityHttp.Problem(ErrorCodes.TestAccountNotFound, "The test user was not found.");
        }

        await signInManager.SignInWithClaimsAsync(
                target,
                isPersistent: true,
                [new Claim(IdentityHttp.ImpersonatorClaimType, actorId.Value.ToString())])
            .ConfigureAwait(false);
        var profile = await profiles.HandleAsync(target.Id, cancellationToken).ConfigureAwait(false);
        if (!profile.IsSuccess || profile.Value is null)
        {
            return IdentityHttp.Problem(profile);
        }

        return Results.Ok(ProfileResponses.FromAccount(profile.Value, isAdministrator: false, isImpersonating: true));
    }

    private static async Task<IResult> StopImpersonationAsync(
        ClaimsPrincipal principal,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        GetOwnProfileHandler profiles,
        CancellationToken cancellationToken)
    {
        var impersonatorId = principal.GetImpersonatorUserId();
        if (impersonatorId is null)
        {
            return IdentityHttp.Problem(ErrorCodes.ImpersonationForbidden, "You are not testing as another user.");
        }

        var admin = await userManager.FindByIdAsync(impersonatorId.Value.ToString()).ConfigureAwait(false);
        if (admin is null || !await userManager.IsInRoleAsync(admin, IdentityMaintenance.AdministratorRole).ConfigureAwait(false))
        {
            return IdentityHttp.Problem(ErrorCodes.ImpersonationForbidden, "You are not testing as another user.");
        }

        await signInManager.SignInAsync(admin, isPersistent: true).ConfigureAwait(false);
        var profile = await profiles.HandleAsync(admin.Id, cancellationToken).ConfigureAwait(false);
        if (!profile.IsSuccess || profile.Value is null)
        {
            return IdentityHttp.Problem(profile);
        }

        return Results.Ok(ProfileResponses.FromAccount(profile.Value, isAdministrator: true));
    }

    private static IResult GetExternalProviders(IConfiguration configuration)
    {
        return Results.Ok(ExternalAuthentication.GetConfiguredProviders(configuration));
    }

    private static string? NullIfEmpty(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
