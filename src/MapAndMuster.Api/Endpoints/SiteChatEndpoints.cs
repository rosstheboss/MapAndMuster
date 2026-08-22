using System.Security.Claims;
using MapAndMuster.Api.Contracts;
using MapAndMuster.Application.Chat;
using MapAndMuster.Application.Common;

namespace MapAndMuster.Api.Endpoints;

/// <summary>
/// Maps public site-chat HTTP endpoints. These routes are independent of campaign logs.
/// </summary>
public static class SiteChatEndpoints
{
    /// <summary>
    /// Maps site-chat routes.
    /// </summary>
    public static void MapSiteChatEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api/site-chat").WithTags("SiteChat").RequireAuthorization();
        group.MapGet("", GetBoardAsync)
            .WithName("GetSiteChat")
            .Produces<SiteChatBoardResponse>()
            .Produces(StatusCodes.Status401Unauthorized);
        group.MapPost("", PostAsync)
            .RequireRateLimiting(IdentityHttp.ChatRateLimitPolicy)
            .WithName("PostSiteChat")
            .Produces<SiteChatBoardResponse>()
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);
        group.MapPut("/blocks/{userId:guid}", SetBlockAsync)
            .WithName("SetSiteChatBlock")
            .Produces<SiteChatBoardResponse>()
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> GetBoardAsync(
        ClaimsPrincipal principal,
        GetSiteChatHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        if (userId is null)
        {
            return IdentityHttp.Problem(ErrorCodes.Unauthorized, "Sign in to continue.");
        }

        var result = await handler.HandleAsync(userId.Value, principal.IsAdministrator(), cancellationToken)
            .ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
        {
            return IdentityHttp.Problem(result);
        }

        return Results.Ok(SiteChatResponses.FromBoard(result.Value));
    }

    private static async Task<IResult> PostAsync(
        PostSiteChatRequest request,
        ClaimsPrincipal principal,
        PostSiteChatHandler handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var userId = principal.GetUserId();
        if (userId is null)
        {
            return IdentityHttp.Problem(ErrorCodes.Unauthorized, "Sign in to continue.");
        }

        var result = await handler.HandleAsync(
                new PostSiteChatCommand
                {
                    UserId = userId.Value,
                    IsAdministrator = principal.IsAdministrator(),
                    Message = request.Message,
                    Language = request.Language,
                    SendAsAdministrator = request.SendAsAdministrator,
                    TargetUserId = request.TargetUserId,
                },
                cancellationToken)
            .ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
        {
            return IdentityHttp.Problem(result);
        }

        return Results.Ok(SiteChatResponses.FromBoard(result.Value));
    }

    private static async Task<IResult> SetBlockAsync(
        Guid userId,
        SetSiteChatBlockRequest request,
        ClaimsPrincipal principal,
        SetSiteChatBlockHandler handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var viewerId = principal.GetUserId();
        if (viewerId is null)
        {
            return IdentityHttp.Problem(ErrorCodes.Unauthorized, "Sign in to continue.");
        }

        var result = await handler.HandleAsync(
                new SetSiteChatBlockCommand
                {
                    UserId = viewerId.Value,
                    IsAdministrator = principal.IsAdministrator(),
                    TargetUserId = userId,
                    Blocked = request.Blocked,
                },
                cancellationToken)
            .ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
        {
            return IdentityHttp.Problem(result);
        }

        return Results.Ok(SiteChatResponses.FromBoard(result.Value));
    }
}
