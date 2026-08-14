using System.Security.Claims;
using Campaign.Api.Contracts;
using Campaign.Application.Campaigns;
using Campaign.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Campaign.Api.Endpoints;

/// <summary>
/// Maps campaign HTTP endpoints.
/// </summary>
public static class CampaignEndpoints
{
    /// <summary>
    /// Maps campaign routes.
    /// </summary>
    /// <param name="app">The application.</param>
    public static void MapCampaignEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api/campaigns").WithTags("Campaigns").RequireAuthorization();

        group.MapGet("", ListAsync)
            .WithName("ListCampaigns")
            .Produces<IReadOnlyList<CampaignListItemResponse>>();

        group.MapPost("", CreateAsync)
            .WithName("CreateCampaign")
            .Produces<CampaignDetailResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        group.MapGet("/{campaignId:guid}", GetAsync)
            .WithName("GetCampaign")
            .Produces<CampaignDetailResponse>()
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapPut("/{campaignId:guid}", UpdateAsync)
            .WithName("UpdateCampaign")
            .Produces<CampaignDetailResponse>()
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict);

        group.MapDelete("/{campaignId:guid}", DeleteAsync)
            .WithName("DeleteCampaign")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/{campaignId:guid}/map", GetMapAsync)
            .WithName("GetCampaignMap")
            .Produces(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapPost("/{campaignId:guid}/map", UploadMapAsync)
            .RequireRateLimiting(IdentityHttp.UploadRateLimitPolicy)
            .DisableAntiforgery()
            .WithName("UploadCampaignMap")
            .Produces<CampaignDetailResponse>()
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> ListAsync(
        ClaimsPrincipal principal,
        ListCampaignsHandler handler,
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

        return Results.Ok(result.Value.Select(CampaignResponses.FromListItem).ToArray());
    }

    private static async Task<IResult> CreateAsync(
        ClaimsPrincipal principal,
        [FromBody] SaveCampaignRequest request,
        CreateCampaignHandler handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var userId = principal.GetUserId();
        if (userId is null)
        {
            return IdentityHttp.Problem(ErrorCodes.Unauthorized, "Sign in to continue.");
        }

        var result = await handler.HandleAsync(
                new CreateCampaignCommand
                {
                    UserId = userId.Value,
                    Name = request.Name,
                    Description = request.Description,
                    PlayerCount = request.PlayerCount,
                    IsPrivate = request.IsPrivate,
                    JoinPassword = request.JoinPassword,
                    CreatorIsParticipant = request.CreatorIsParticipant,
                    Factions = CampaignResponses.ToFactionInputs(request.Factions),
                    AllyGroups = CampaignResponses.ToAllyGroupInputs(request.AllyGroups),
                    Links = CampaignResponses.ToLinkInputs(request.Links),
                },
                cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess || result.Value is null)
        {
            return IdentityHttp.Problem(result);
        }

        return Results.Created($"/api/campaigns/{result.Value.Id}", CampaignResponses.FromDetail(result.Value));
    }

    private static async Task<IResult> GetAsync(
        Guid campaignId,
        ClaimsPrincipal principal,
        GetCampaignHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        if (userId is null)
        {
            return IdentityHttp.Problem(ErrorCodes.Unauthorized, "Sign in to continue.");
        }

        var result = await handler.HandleAsync(campaignId, userId.Value, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
        {
            return IdentityHttp.Problem(result);
        }

        return Results.Ok(CampaignResponses.FromDetail(result.Value));
    }

    private static async Task<IResult> UpdateAsync(
        Guid campaignId,
        ClaimsPrincipal principal,
        [FromBody] SaveCampaignRequest request,
        UpdateCampaignHandler handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var userId = principal.GetUserId();
        if (userId is null)
        {
            return IdentityHttp.Problem(ErrorCodes.Unauthorized, "Sign in to continue.");
        }

        if (request.Revision is null)
        {
            return IdentityHttp.Problem("campaign.revision.required", "The campaign revision is required.");
        }

        var result = await handler.HandleAsync(
                new UpdateCampaignCommand
                {
                    UserId = userId.Value,
                    CampaignId = campaignId,
                    ExpectedRevision = request.Revision.Value,
                    Name = request.Name,
                    Description = request.Description,
                    PlayerCount = request.PlayerCount,
                    IsPrivate = request.IsPrivate,
                    JoinPassword = request.JoinPassword,
                    CreatorIsParticipant = request.CreatorIsParticipant,
                    Factions = CampaignResponses.ToFactionInputs(request.Factions),
                    AllyGroups = CampaignResponses.ToAllyGroupInputs(request.AllyGroups),
                    Links = CampaignResponses.ToLinkInputs(request.Links),
                },
                cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess || result.Value is null)
        {
            return IdentityHttp.Problem(result);
        }

        return Results.Ok(CampaignResponses.FromDetail(result.Value));
    }

    private static async Task<IResult> DeleteAsync(
        Guid campaignId,
        ClaimsPrincipal principal,
        DeleteCampaignHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        if (userId is null)
        {
            return IdentityHttp.Problem(ErrorCodes.Unauthorized, "Sign in to continue.");
        }

        var result = await handler.HandleAsync(campaignId, userId.Value, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return IdentityHttp.Problem(result);
        }

        return Results.NoContent();
    }

    private static async Task<IResult> GetMapAsync(
        Guid campaignId,
        ClaimsPrincipal principal,
        GetCampaignMapHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        if (userId is null)
        {
            return IdentityHttp.Problem(ErrorCodes.Unauthorized, "Sign in to continue.");
        }

        var result = await handler.HandleAsync(campaignId, userId.Value, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
        {
            return IdentityHttp.Problem(result);
        }

        return Results.File(result.Value.Content, result.Value.ContentType);
    }

    private static async Task<IResult> UploadMapAsync(
        Guid campaignId,
        ClaimsPrincipal principal,
        HttpRequest request,
        UploadCampaignMapHandler handler,
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
        var file = form.Files.GetFile("map");
        if (file is null)
        {
            return IdentityHttp.Problem(ErrorCodes.UploadInvalidType, "Choose a campaign map to upload.");
        }

        if (!int.TryParse(form["revision"].ToString(), out var revision))
        {
            return IdentityHttp.Problem("campaign.revision.required", "The campaign revision is required.");
        }

        await using var stream = file.OpenReadStream();
        var result = await handler.HandleAsync(
                new UploadCampaignMapCommand
                {
                    UserId = userId.Value,
                    CampaignId = campaignId,
                    ExpectedRevision = revision,
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

        return Results.Ok(CampaignResponses.FromDetail(result.Value));
    }
}
