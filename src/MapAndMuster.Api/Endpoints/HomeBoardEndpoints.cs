using System.Security.Claims;
using MapAndMuster.Api.Contracts;
using MapAndMuster.Application.Common;
using MapAndMuster.Application.News;
using MapAndMuster.Application.Notifications;
using Microsoft.AspNetCore.Mvc;

namespace MapAndMuster.Api.Endpoints;

/// <summary>
/// Maps home-board notification and news HTTP endpoints.
/// </summary>
public static class HomeBoardEndpoints
{
    /// <summary>
    /// Maps notification and news routes.
    /// </summary>
    public static void MapHomeBoardEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var notices = app.MapGroup("/api/notifications").WithTags("Notifications").RequireAuthorization();
        notices.MapGet("", ListNotificationsAsync)
            .WithName("ListNotifications")
            .Produces<IReadOnlyList<HomeAttentionItemResponse>>();
        notices.MapPost("/read-all", MarkAllReadAsync)
            .WithName("MarkAllNotificationsRead")
            .Produces(StatusCodes.Status204NoContent);
        notices.MapPost("/{notificationId:guid}/read", MarkReadAsync)
            .WithName("MarkNotificationRead")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        var news = app.MapGroup("/api/news").WithTags("News").RequireAuthorization();
        news.MapGet("", GetNewsAsync)
            .WithName("GetNewsPage")
            .Produces<NewsPageResponse>();
        news.MapPost("", CreateNewsAsync)
            .WithName("CreateNewsArticle")
            .Produces<NewsArticleResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden);
        news.MapPut("/{articleId:guid}", UpdateNewsAsync)
            .WithName("UpdateNewsArticle")
            .Produces<NewsArticleResponse>()
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);
        news.MapDelete("/{articleId:guid}", DeleteNewsAsync)
            .WithName("DeleteNewsArticle")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> ListNotificationsAsync(
        ClaimsPrincipal principal,
        GetHomeBoardHandler handler,
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

        return Results.Ok(result.Value.Select(HomeBoardResponses.FromAttention).ToArray());
    }

    private static async Task<IResult> MarkReadAsync(
        Guid notificationId,
        ClaimsPrincipal principal,
        MarkNotificationReadHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        if (userId is null)
        {
            return IdentityHttp.Problem(ErrorCodes.Unauthorized, "Sign in to continue.");
        }

        var result = await handler.HandleAsync(notificationId, userId.Value, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? Results.NoContent() : IdentityHttp.Problem(result);
    }

    private static async Task<IResult> MarkAllReadAsync(
        ClaimsPrincipal principal,
        MarkAllNotificationsReadHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        if (userId is null)
        {
            return IdentityHttp.Problem(ErrorCodes.Unauthorized, "Sign in to continue.");
        }

        var result = await handler.HandleAsync(userId.Value, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? Results.NoContent() : IdentityHttp.Problem(result);
    }

    private static async Task<IResult> GetNewsAsync(
        GetNewsPageHandler handler,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1)
    {
        var result = await handler.HandleAsync(page, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
        {
            return IdentityHttp.Problem(result);
        }

        return Results.Ok(HomeBoardResponses.FromNews(result.Value));
    }

    private static async Task<IResult> CreateNewsAsync(
        SaveNewsArticleRequest request,
        ClaimsPrincipal principal,
        SaveNewsArticleHandler handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var userId = principal.GetUserId();
        if (userId is null)
        {
            return IdentityHttp.Problem(ErrorCodes.Unauthorized, "Sign in to continue.");
        }

        var result = await handler.HandleAsync(
                new SaveNewsArticleCommand
                {
                    UserId = userId.Value,
                    IsAdministrator = principal.IsAdministrator(),
                    Title = request.Title,
                    BodyMarkdown = request.BodyMarkdown,
                },
                cancellationToken)
            .ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
        {
            return IdentityHttp.Problem(result);
        }

        return Results.Created($"/api/news/{result.Value.Id}", HomeBoardResponses.FromArticle(result.Value));
    }

    private static async Task<IResult> UpdateNewsAsync(
        Guid articleId,
        SaveNewsArticleRequest request,
        ClaimsPrincipal principal,
        SaveNewsArticleHandler handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var userId = principal.GetUserId();
        if (userId is null)
        {
            return IdentityHttp.Problem(ErrorCodes.Unauthorized, "Sign in to continue.");
        }

        var result = await handler.HandleAsync(
                new SaveNewsArticleCommand
                {
                    UserId = userId.Value,
                    IsAdministrator = principal.IsAdministrator(),
                    ArticleId = articleId,
                    Title = request.Title,
                    BodyMarkdown = request.BodyMarkdown,
                },
                cancellationToken)
            .ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
        {
            return IdentityHttp.Problem(result);
        }

        return Results.Ok(HomeBoardResponses.FromArticle(result.Value));
    }

    private static async Task<IResult> DeleteNewsAsync(
        Guid articleId,
        ClaimsPrincipal principal,
        DeleteNewsArticleHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        if (userId is null)
        {
            return IdentityHttp.Problem(ErrorCodes.Unauthorized, "Sign in to continue.");
        }

        var result = await handler.HandleAsync(articleId, userId.Value, principal.IsAdministrator(), cancellationToken)
            .ConfigureAwait(false);
        return result.IsSuccess ? Results.NoContent() : IdentityHttp.Problem(result);
    }
}
