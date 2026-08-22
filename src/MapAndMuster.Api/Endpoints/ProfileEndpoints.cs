using System.Security.Claims;
using MapAndMuster.Api.Contracts;
using MapAndMuster.Application.Common;
using MapAndMuster.Application.Identity;
using MapAndMuster.Application.Ports;
using Microsoft.AspNetCore.Mvc;

namespace MapAndMuster.Api.Endpoints;

/// <summary>
/// Maps profile HTTP endpoints.
/// </summary>
public static class ProfileEndpoints
{
    /// <summary>
    /// Maps profile routes.
    /// </summary>
    /// <param name="app">The application.</param>
    public static void MapProfileEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api/profiles").WithTags("Profiles");

        group.MapGet("/me", GetOwnAsync)
            .RequireAuthorization()
            .WithName("GetOwnProfile")
            .Produces<OwnProfileResponse>()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPut("/me", UpdateOwnAsync)
            .RequireAuthorization()
            .WithName("UpdateOwnProfile")
            .Produces<OwnProfileResponse>()
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict);

        group.MapPost("/me/avatar", UploadAvatarAsync)
            .RequireAuthorization()
            .RequireRateLimiting(IdentityHttp.UploadRateLimitPolicy)
            .DisableAntiforgery()
            .WithName("UploadOwnAvatar")
            .Produces<OwnProfileResponse>()
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        group.MapGet("/{username}", GetPublicAsync)
            .AllowAnonymous()
            .WithName("GetPublicProfile")
            .Produces<PublicProfileResponse>()
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/{username}/avatar", GetAvatarAsync)
            .AllowAnonymous()
            .WithName("GetPublicAvatar")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetOwnAsync(
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

    private static async Task<IResult> UpdateOwnAsync(
        ClaimsPrincipal principal,
        [FromBody] UpdateProfileRequest request,
        UpdateProfileHandler handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var userId = principal.GetUserId();
        if (userId is null)
        {
            return IdentityHttp.Problem(ErrorCodes.Unauthorized, "Sign in to continue.");
        }

        if (!IdentityHttp.TryParseDisplayNameMode(request.DisplayNameMode, out var displayNameMode))
        {
            return IdentityHttp.Problem("displayNameMode.invalid", "Choose whether other users see your username or full name.");
        }

        var result = await handler.HandleAsync(
                new UpdateProfileCommand
                {
                    UserId = userId.Value,
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
                    InAppNotificationsEnabled = request.InAppNotificationsEnabled,
                    EmailNotificationsEnabled = request.EmailNotificationsEnabled,
                    PreferredChatLanguage = request.PreferredChatLanguage,
                    ProfileRevision = request.ProfileRevision,
                },
                cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess || result.Value is null)
        {
            return IdentityHttp.Problem(result);
        }

        return Results.Ok(ProfileResponses.FromAccount(
            result.Value,
            principal.IsAdministrator(),
            principal.GetImpersonatorUserId() is not null));
    }

    private static async Task<IResult> UploadAvatarAsync(
        ClaimsPrincipal principal,
        HttpRequest request,
        UploadAvatarHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        if (userId is null)
        {
            return IdentityHttp.Problem(ErrorCodes.Unauthorized, "Sign in to continue.");
        }

        if (!request.HasFormContentType)
        {
            return IdentityHttp.Problem(ErrorCodes.UploadInvalidType, "Upload a JPEG, PNG, or WebP image.");
        }

        var form = await request.ReadFormAsync(cancellationToken).ConfigureAwait(false);
        var file = form.Files.GetFile("avatar");
        if (file is null)
        {
            return IdentityHttp.Problem(ErrorCodes.UploadInvalidType, "Choose a profile picture to upload.");
        }

        await using var stream = file.OpenReadStream();
        var result = await handler.HandleAsync(
                new UploadAvatarCommand
                {
                    UserId = userId.Value,
                    Content = stream,
                    ContentType = file.ContentType,
                    Length = file.Length,
                },
                cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess || result.Value is null)
        {
            return IdentityHttp.Problem(result);
        }

        return Results.Ok(ProfileResponses.FromAccount(
            result.Value,
            principal.IsAdministrator(),
            principal.GetImpersonatorUserId() is not null));
    }

    private static async Task<IResult> GetPublicAsync(
        string username,
        ClaimsPrincipal principal,
        GetPublicProfileHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
                username,
                principal.GetUserId(),
                principal.Identity?.IsAuthenticated == true && principal.IsAdministrator(),
                cancellationToken)
            .ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
        {
            return IdentityHttp.Problem(result);
        }

        return Results.Ok(ProfileResponses.FromPublic(result.Value));
    }

    private static async Task<IResult> GetAvatarAsync(
        string username,
        IUserAccountStore accounts,
        IAvatarStorage storage,
        CancellationToken cancellationToken)
    {
        var account = await accounts.FindByUsernameAsync(username, cancellationToken).ConfigureAwait(false);
        if (account?.AvatarStorageKey is null)
        {
            return Results.NotFound();
        }

        var file = await storage.OpenReadAsync(account.AvatarStorageKey, cancellationToken).ConfigureAwait(false);
        if (file is null)
        {
            return Results.NotFound();
        }

        return Results.File(file.Content, file.ContentType);
    }
}
