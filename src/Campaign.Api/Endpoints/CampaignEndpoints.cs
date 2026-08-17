using System.Security.Claims;
using Campaign.Api.Contracts;
using Campaign.Application.Campaigns;
using Campaign.Application.Common;
using Campaign.Application.Maps;
using Campaign.Application.Play;
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

        group.MapGet("/all", ListAllAsync)
            .WithName("ListAllCampaigns")
            .Produces<IReadOnlyList<CampaignListItemResponse>>();

        group.MapPost("", CreateAsync)
            .WithName("CreateCampaign")
            .Produces<CampaignDetailResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        group.MapGet("/{campaignId:guid}", GetAsync)
            .WithName("GetCampaign")
            .Produces<CampaignDetailResponse>()
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapPost("/{campaignId:guid}/chat", PostChatAsync)
            .WithName("PostCampaignChat")
            .Produces<CampaignDetailResponse>()
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict);

        group.MapPost("/{campaignId:guid}/join", JoinAsync)
            .WithName("JoinCampaign")
            .Produces<CampaignListItemResponse>()
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict);

        group.MapPost("/{campaignId:guid}/leave", LeaveAsync)
            .WithName("LeaveCampaign")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapPost("/{campaignId:guid}/duplicate", DuplicateAsync)
            .WithName("DuplicateCampaign")
            .Produces<CampaignDetailResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
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

        group.MapGet("/{campaignId:guid}/map/graph", GetMapGraphAsync)
            .WithName("GetCampaignMapGraph")
            .Produces<MapGraphResponse>()
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapPut("/{campaignId:guid}/map/graph", SaveMapGraphAsync)
            .WithName("SaveCampaignMapGraph")
            .Produces<MapGraphResponse>()
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict);

        group.MapGet("/{campaignId:guid}/structures/{structureTypeId:guid}/image", GetStructureImageAsync)
            .WithName("GetCampaignStructureImage")
            .Produces(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapPost("/{campaignId:guid}/structures/{structureTypeId:guid}/image", UploadStructureImageAsync)
            .RequireRateLimiting(IdentityHttp.UploadRateLimitPolicy)
            .DisableAntiforgery()
            .WithName("UploadCampaignStructureImage")
            .Produces<CampaignDetailResponse>()
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/{campaignId:guid}/structures/{structureTypeId:guid}/pillaged-image", GetPillagedStructureImageAsync)
            .WithName("GetCampaignPillagedStructureImage")
            .Produces(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapPost("/{campaignId:guid}/structures/{structureTypeId:guid}/pillaged-image", UploadPillagedStructureImageAsync)
            .RequireRateLimiting(IdentityHttp.UploadRateLimitPolicy)
            .DisableAntiforgery()
            .WithName("UploadCampaignPillagedStructureImage")
            .Produces<CampaignDetailResponse>()
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/{campaignId:guid}/factions/{factionId:guid}/flag", GetFactionFlagAsync)
            .WithName("GetCampaignFactionFlag")
            .Produces(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapPost("/{campaignId:guid}/factions/{factionId:guid}/flag", UploadFactionFlagAsync)
            .RequireRateLimiting(IdentityHttp.UploadRateLimitPolicy)
            .DisableAntiforgery()
            .WithName("UploadCampaignFactionFlag")
            .Produces<CampaignDetailResponse>()
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/{campaignId:guid}/missions/{missionId:guid}/file", GetMissionFileAsync)
            .WithName("GetCampaignMissionFile")
            .Produces(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapPost("/{campaignId:guid}/missions/{missionId:guid}/file", UploadMissionFileAsync)
            .RequireRateLimiting(IdentityHttp.UploadRateLimitPolicy)
            .DisableAntiforgery()
            .WithName("UploadCampaignMissionFile")
            .Produces<CampaignDetailResponse>()
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/{campaignId:guid}/play", GetPlayAsync)
            .WithName("GetCampaignPlay")
            .Produces<CampaignPlayResponse>()
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapPost("/{campaignId:guid}/play/faction", ChooseFactionAsync)
            .WithName("ChooseCampaignFaction")
            .Produces<CampaignPlayResponse>()
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict);

        group.MapPost("/{campaignId:guid}/play/draft", SaveDraftAsync)
            .WithName("SaveCampaignOrderDraft")
            .Produces<CampaignPlayResponse>()
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict);

        group.MapPost("/{campaignId:guid}/play/commit", CommitAsync)
            .WithName("CommitCampaignOrders")
            .Produces<CampaignPlayResponse>()
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict);

        group.MapPost("/{campaignId:guid}/play/uncommit", UncommitAsync)
            .WithName("UncommitCampaignOrders")
            .Produces<CampaignPlayResponse>()
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict);

        group.MapPost("/{campaignId:guid}/play/battle-result", SubmitBattleResultAsync)
            .WithName("SubmitCampaignBattleResult")
            .Produces<CampaignPlayResponse>()
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict);

        group.MapPost("/{campaignId:guid}/play/accept-result", AcceptBattleResultAsync)
            .WithName("AcceptCampaignBattleResult")
            .Produces<CampaignPlayResponse>()
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict);

        group.MapPost("/{campaignId:guid}/play/retreat", SubmitRetreatAsync)
            .WithName("SubmitCampaignRetreat")
            .Produces<CampaignPlayResponse>()
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict);

        group.MapPost("/{campaignId:guid}/play/gm-resolve-battle", ResolveBattleAsync)
            .WithName("ResolveCampaignBattle")
            .Produces<CampaignPlayResponse>()
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict);

        group.MapPost("/{campaignId:guid}/play/extend-schedule", ExtendScheduleAsync)
            .WithName("ExtendCampaignSchedule")
            .Produces<CampaignPlayResponse>()
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict);

        group.MapPost("/{campaignId:guid}/play/debug/enter", EnterDebugAsync)
            .WithName("EnterCampaignDebug")
            .Produces<CampaignPlayResponse>()
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict);

        group.MapPost("/{campaignId:guid}/play/debug/exit", ExitDebugAsync)
            .WithName("ExitCampaignDebug")
            .Produces<CampaignPlayResponse>()
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict);

        group.MapPost("/{campaignId:guid}/play/debug/correct-order", DebugCorrectOrderAsync)
            .WithName("DebugCorrectCampaignOrder")
            .Produces<CampaignPlayResponse>()
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict);

        group.MapPost("/{campaignId:guid}/play/debug/reveal-hidden-objectives", RevealHiddenItemObjectivesAsync)
            .WithName("RevealHiddenItemObjectives")
            .Produces<CampaignPlayResponse>()
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict);
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

    private static async Task<IResult> ListAllAsync(
        ClaimsPrincipal principal,
        ListDiscoverableCampaignsHandler handler,
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
                    IsPubliclyViewable = request.IsPubliclyViewable,
                    JoinPassword = request.JoinPassword,
                    CreatorIsParticipant = request.CreatorIsParticipant,
                    City = request.City,
                    Region = request.Region,
                    Country = request.Country,
                    Factions = CampaignResponses.ToFactionInputs(request.Factions),
                    AllyGroups = CampaignResponses.ToAllyGroupInputs(request.AllyGroups),
                    Links = CampaignResponses.ToLinkInputs(request.Links),
                    Schedule = CampaignResponses.ToScheduleInput(request),
                    TerrainTypes = CampaignResponses.ToTerrainTypeInputs(request.TerrainTypes),
                    StructureTypes = CampaignResponses.ToStructureTypeInputs(request.StructureTypes),
                    ItemObjectiveTypes = CampaignResponses.ToItemObjectiveTypeInputs(request.ItemObjectiveTypes),
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

        var result = await handler.HandleAsync(
                campaignId,
                userId.Value,
                cancellationToken,
                principal.IsAdministrator())
            .ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
        {
            return IdentityHttp.Problem(result);
        }

        return Results.Ok(CampaignResponses.FromDetail(result.Value));
    }

    private static async Task<IResult> PostChatAsync(
        Guid campaignId,
        PostCampaignChatRequest request,
        ClaimsPrincipal principal,
        PostCampaignChatHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        if (userId is null)
        {
            return IdentityHttp.Problem(ErrorCodes.Unauthorized, "Sign in to continue.");
        }

        var result = await handler.HandleAsync(
                new PostCampaignChatCommand
                {
                    UserId = userId.Value,
                    IsAdministrator = principal.IsAdministrator(),
                    CampaignId = campaignId,
                    ExpectedRevision = request.Revision,
                    ChannelKind = request.ChannelKind,
                    TargetId = request.TargetId,
                    Message = request.Message,
                },
                cancellationToken)
            .ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
        {
            return IdentityHttp.Problem(result);
        }

        return Results.Ok(CampaignResponses.FromDetail(result.Value));
    }

    private static async Task<IResult> JoinAsync(
        Guid campaignId,
        ClaimsPrincipal principal,
        [FromBody] JoinCampaignRequest? request,
        JoinCampaignHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        if (userId is null)
        {
            return IdentityHttp.Problem(ErrorCodes.Unauthorized, "Sign in to continue.");
        }

        var result = await handler.HandleAsync(
                new JoinCampaignCommand
                {
                    UserId = userId.Value,
                    IsAdministrator = principal.IsAdministrator(),
                    CampaignId = campaignId,
                    JoinPassword = request?.JoinPassword,
                },
                cancellationToken)
            .ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
        {
            return IdentityHttp.Problem(result);
        }

        return Results.Ok(CampaignResponses.FromListItem(result.Value));
    }

    private static async Task<IResult> LeaveAsync(
        Guid campaignId,
        ClaimsPrincipal principal,
        LeaveCampaignHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        if (userId is null)
        {
            return IdentityHttp.Problem(ErrorCodes.Unauthorized, "Sign in to continue.");
        }

        var result = await handler.HandleAsync(
                new LeaveCampaignCommand
                {
                    UserId = userId.Value,
                    CampaignId = campaignId,
                },
                cancellationToken)
            .ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return IdentityHttp.Problem(result);
        }

        return Results.NoContent();
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
                    IsPubliclyViewable = request.IsPubliclyViewable,
                    JoinPassword = request.JoinPassword,
                    CreatorIsParticipant = request.CreatorIsParticipant,
                    City = request.City,
                    Region = request.Region,
                    Country = request.Country,
                    Factions = CampaignResponses.ToFactionInputs(request.Factions),
                    AllyGroups = CampaignResponses.ToAllyGroupInputs(request.AllyGroups),
                    Links = CampaignResponses.ToLinkInputs(request.Links),
                    Schedule = CampaignResponses.ToScheduleInput(request),
                    TerrainTypes = CampaignResponses.ToTerrainTypeInputs(request.TerrainTypes),
                    StructureTypes = CampaignResponses.ToStructureTypeInputs(request.StructureTypes),
                    ItemObjectiveTypes = CampaignResponses.ToItemObjectiveTypeInputs(request.ItemObjectiveTypes),
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

    private static async Task<IResult> DuplicateAsync(
        Guid campaignId,
        ClaimsPrincipal principal,
        DuplicateCampaignHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        if (userId is null)
        {
            return IdentityHttp.Problem(ErrorCodes.Unauthorized, "Sign in to continue.");
        }

        var result = await handler.HandleAsync(
                new DuplicateCampaignCommand
                {
                    UserId = userId.Value,
                    CampaignId = campaignId,
                },
                cancellationToken)
            .ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
        {
            return IdentityHttp.Problem(result);
        }

        return Results.Created($"/api/campaigns/{result.Value.Id}", CampaignResponses.FromDetail(result.Value));
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

        var result = await handler.HandleAsync(
                campaignId,
                userId.Value,
                cancellationToken,
                principal.IsAdministrator())
            .ConfigureAwait(false);
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

    private static async Task<IResult> GetMapGraphAsync(
        Guid campaignId,
        ClaimsPrincipal principal,
        GetCampaignMapGraphHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        if (userId is null)
        {
            return IdentityHttp.Problem(ErrorCodes.Unauthorized, "Sign in to continue.");
        }

        var result = await handler.HandleAsync(
                campaignId,
                userId.Value,
                cancellationToken,
                principal.IsAdministrator())
            .ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
        {
            return IdentityHttp.Problem(result);
        }

        return Results.Ok(CampaignResponses.FromMapGraph(result.Value));
    }

    private static async Task<IResult> SaveMapGraphAsync(
        Guid campaignId,
        ClaimsPrincipal principal,
        [FromBody] SaveMapGraphRequest request,
        SaveCampaignMapGraphHandler handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var userId = principal.GetUserId();
        if (userId is null)
        {
            return IdentityHttp.Problem(ErrorCodes.Unauthorized, "Sign in to continue.");
        }

        var result = await handler.HandleAsync(
                new SaveCampaignMapGraphCommand
                {
                    UserId = userId.Value,
                    CampaignId = campaignId,
                    ExpectedRevision = request.Revision,
                    Territories = CampaignResponses.ToTerritoryInputs(request.Territories),
                    Adjacencies = CampaignResponses.ToAdjacencyInputs(request.Adjacencies),
                    ItemObjectivePlacements =
                    [
                        .. (request.ItemObjectivePlacements ?? []).Select(static item => new ItemObjectivePlacementInput
                        {
                            TypeId = item.TypeId,
                            TerritoryId = item.TerritoryId,
                        }),
                    ],
                },
                cancellationToken)
            .ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
        {
            return IdentityHttp.Problem(result);
        }

        return Results.Ok(CampaignResponses.FromMapGraph(result.Value));
    }

    private static Task<IResult> GetStructureImageAsync(
        Guid campaignId,
        Guid structureTypeId,
        ClaimsPrincipal principal,
        GetStructureImageHandler handler,
        CancellationToken cancellationToken)
    {
        return GetStructureImageCoreAsync(campaignId, structureTypeId, principal, handler, pillaged: false, cancellationToken);
    }

    private static Task<IResult> UploadStructureImageAsync(
        Guid campaignId,
        Guid structureTypeId,
        ClaimsPrincipal principal,
        HttpRequest request,
        UploadStructureImageHandler handler,
        CancellationToken cancellationToken)
    {
        return UploadStructureImageCoreAsync(campaignId, structureTypeId, principal, request, handler, pillaged: false, cancellationToken);
    }

    private static Task<IResult> GetPillagedStructureImageAsync(
        Guid campaignId,
        Guid structureTypeId,
        ClaimsPrincipal principal,
        GetStructureImageHandler handler,
        CancellationToken cancellationToken)
    {
        return GetStructureImageCoreAsync(campaignId, structureTypeId, principal, handler, pillaged: true, cancellationToken);
    }

    private static Task<IResult> UploadPillagedStructureImageAsync(
        Guid campaignId,
        Guid structureTypeId,
        ClaimsPrincipal principal,
        HttpRequest request,
        UploadStructureImageHandler handler,
        CancellationToken cancellationToken)
    {
        return UploadStructureImageCoreAsync(campaignId, structureTypeId, principal, request, handler, pillaged: true, cancellationToken);
    }

    private static async Task<IResult> GetStructureImageCoreAsync(
        Guid campaignId,
        Guid structureTypeId,
        ClaimsPrincipal principal,
        GetStructureImageHandler handler,
        bool pillaged,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        if (userId is null)
        {
            return IdentityHttp.Problem(ErrorCodes.Unauthorized, "Sign in to continue.");
        }

        var result = await handler.HandleAsync(
                campaignId,
                structureTypeId,
                userId.Value,
                cancellationToken,
                principal.IsAdministrator(),
                pillaged)
            .ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
        {
            return IdentityHttp.Problem(result);
        }

        return Results.File(result.Value.Content, result.Value.ContentType);
    }

    private static async Task<IResult> UploadStructureImageCoreAsync(
        Guid campaignId,
        Guid structureTypeId,
        ClaimsPrincipal principal,
        HttpRequest request,
        UploadStructureImageHandler handler,
        bool pillaged,
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
        var file = form.Files.GetFile("image") ?? form.Files.GetFile("file");
        if (file is null)
        {
            return IdentityHttp.Problem(ErrorCodes.UploadInvalidType, "Choose a structure image to upload.");
        }

        if (!int.TryParse(form["revision"].ToString(), out var revision))
        {
            return IdentityHttp.Problem("campaign.revision.required", "The campaign revision is required.");
        }

        await using var stream = file.OpenReadStream();
        var result = await handler.HandleAsync(
                new UploadStructureImageCommand
                {
                    UserId = userId.Value,
                    CampaignId = campaignId,
                    StructureTypeId = structureTypeId,
                    ExpectedRevision = revision,
                    Content = stream,
                    ContentType = file.ContentType,
                    Length = file.Length,
                    Pillaged = pillaged,
                },
                cancellationToken)
            .ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
        {
            return IdentityHttp.Problem(result);
        }

        return Results.Ok(CampaignResponses.FromDetail(result.Value));
    }

    private static async Task<IResult> GetFactionFlagAsync(
        Guid campaignId,
        Guid factionId,
        ClaimsPrincipal principal,
        GetFactionFlagHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        if (userId is null)
        {
            return IdentityHttp.Problem(ErrorCodes.Unauthorized, "Sign in to continue.");
        }

        var result = await handler.HandleAsync(
                campaignId,
                factionId,
                userId.Value,
                cancellationToken,
                principal.IsAdministrator())
            .ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
        {
            return IdentityHttp.Problem(result);
        }

        return Results.File(result.Value.Content, result.Value.ContentType);
    }

    private static async Task<IResult> UploadFactionFlagAsync(
        Guid campaignId,
        Guid factionId,
        ClaimsPrincipal principal,
        HttpRequest request,
        UploadFactionFlagHandler handler,
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
        var file = form.Files.GetFile("image") ?? form.Files.GetFile("file");
        if (file is null)
        {
            return IdentityHttp.Problem(ErrorCodes.UploadInvalidType, "Choose a faction flag image to upload.");
        }

        if (!int.TryParse(form["revision"].ToString(), out var revision))
        {
            return IdentityHttp.Problem("campaign.revision.required", "The campaign revision is required.");
        }

        await using var stream = file.OpenReadStream();
        var result = await handler.HandleAsync(
                new UploadFactionFlagCommand
                {
                    UserId = userId.Value,
                    CampaignId = campaignId,
                    FactionId = factionId,
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

    private static async Task<IResult> GetMissionFileAsync(
        Guid campaignId,
        Guid missionId,
        ClaimsPrincipal principal,
        GetMissionFileHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        if (userId is null)
        {
            return IdentityHttp.Problem(ErrorCodes.Unauthorized, "Sign in to continue.");
        }

        var result = await handler.HandleAsync(
                campaignId,
                missionId,
                userId.Value,
                cancellationToken,
                principal.IsAdministrator())
            .ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
        {
            return IdentityHttp.Problem(result);
        }

        return Results.File(
            result.Value.Content,
            result.Value.ContentType,
            result.Value.DownloadName);
    }

    private static async Task<IResult> UploadMissionFileAsync(
        Guid campaignId,
        Guid missionId,
        ClaimsPrincipal principal,
        HttpRequest request,
        UploadMissionFileHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        if (userId is null)
        {
            return IdentityHttp.Problem(ErrorCodes.Unauthorized, "Sign in to continue.");
        }

        if (!request.HasFormContentType)
        {
            return IdentityHttp.Problem(ErrorCodes.UploadInvalidType, "Upload a PDF or Word document.");
        }

        var form = await request.ReadFormAsync(cancellationToken).ConfigureAwait(false);
        var file = form.Files.GetFile("file") ?? form.Files.GetFile("document");
        if (file is null)
        {
            return IdentityHttp.Problem(ErrorCodes.UploadInvalidType, "Choose a mission document to upload.");
        }

        if (!int.TryParse(form["revision"].ToString(), out var revision))
        {
            return IdentityHttp.Problem("campaign.revision.required", "The campaign revision is required.");
        }

        await using var stream = file.OpenReadStream();
        var result = await handler.HandleAsync(
                new UploadMissionFileCommand
                {
                    UserId = userId.Value,
                    CampaignId = campaignId,
                    MissionId = missionId,
                    ExpectedRevision = revision,
                    Content = stream,
                    ContentType = file.ContentType,
                    FileName = file.FileName,
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

    private static async Task<IResult> GetPlayAsync(
        Guid campaignId,
        ClaimsPrincipal principal,
        GetCampaignPlayHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        if (userId is null)
        {
            return IdentityHttp.Problem(ErrorCodes.Unauthorized, "Sign in to continue.");
        }

        var result = await handler
            .HandleAsync(campaignId, userId.Value, principal.IsAdministrator(), cancellationToken)
            .ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
        {
            return IdentityHttp.Problem(result);
        }

        return Results.Ok(PlayResponses.FromDetail(result.Value));
    }

    private static async Task<IResult> ChooseFactionAsync(
        Guid campaignId,
        ChooseFactionRequest request,
        ClaimsPrincipal principal,
        ChooseFactionHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        if (userId is null)
        {
            return IdentityHttp.Problem(ErrorCodes.Unauthorized, "Sign in to continue.");
        }

        var result = await handler.HandleAsync(
                new ChooseFactionCommand
                {
                    UserId = userId.Value,
                    CampaignId = campaignId,
                    ExpectedRevision = request.Revision,
                    FactionId = request.FactionId,
                    Subfaction = request.Subfaction,
                },
                cancellationToken)
            .ConfigureAwait(false);
        return PlayResult(result);
    }

    private static async Task<IResult> SaveDraftAsync(
        Guid campaignId,
        SaveOrderDraftRequest request,
        ClaimsPrincipal principal,
        SaveOrderDraftHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        if (userId is null)
        {
            return IdentityHttp.Problem(ErrorCodes.Unauthorized, "Sign in to continue.");
        }

        var result = await handler.HandleAsync(
                new SaveOrderDraftCommand
                {
                    UserId = userId.Value,
                    IsAdministrator = principal.IsAdministrator(),
                    CampaignId = campaignId,
                    ExpectedRevision = request.Revision,
                    ForceId = request.ForceId,
                    Kind = request.Kind,
                    TargetTerritoryId = request.TargetTerritoryId,
                    StructureTypeId = request.StructureTypeId,
                },
                cancellationToken)
            .ConfigureAwait(false);
        return PlayResult(result);
    }

    private static async Task<IResult> CommitAsync(
        Guid campaignId,
        PlayRevisionRequest request,
        ClaimsPrincipal principal,
        CommitOrdersHandler handler,
        CancellationToken cancellationToken)
    {
        return await PlayCommandAsync(campaignId, request, principal, handler.HandleAsync, cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<IResult> UncommitAsync(
        Guid campaignId,
        PlayRevisionRequest request,
        ClaimsPrincipal principal,
        UncommitOrdersHandler handler,
        CancellationToken cancellationToken)
    {
        return await PlayCommandAsync(campaignId, request, principal, handler.HandleAsync, cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<IResult> SubmitBattleResultAsync(
        Guid campaignId,
        SubmitBattleResultRequest request,
        ClaimsPrincipal principal,
        SubmitBattleResultHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        if (userId is null)
        {
            return IdentityHttp.Problem(ErrorCodes.Unauthorized, "Sign in to continue.");
        }

        var result = await handler.HandleAsync(
                new SubmitBattleResultCommand
                {
                    UserId = userId.Value,
                    IsAdministrator = principal.IsAdministrator(),
                    CampaignId = campaignId,
                    ExpectedRevision = request.Revision,
                    BattleId = request.BattleId,
                    WinnerForceId = request.WinnerForceId,
                    IsDraw = request.IsDraw,
                },
                cancellationToken)
            .ConfigureAwait(false);
        return PlayResult(result);
    }

    private static async Task<IResult> AcceptBattleResultAsync(
        Guid campaignId,
        BattleActionRequest request,
        ClaimsPrincipal principal,
        AcceptBattleResultHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        if (userId is null)
        {
            return IdentityHttp.Problem(ErrorCodes.Unauthorized, "Sign in to continue.");
        }

        var result = await handler.HandleAsync(
                new BattleActionCommand
                {
                    UserId = userId.Value,
                    IsAdministrator = principal.IsAdministrator(),
                    CampaignId = campaignId,
                    ExpectedRevision = request.Revision,
                    BattleId = request.BattleId,
                },
                cancellationToken)
            .ConfigureAwait(false);
        return PlayResult(result);
    }

    private static async Task<IResult> SubmitRetreatAsync(
        Guid campaignId,
        SubmitRetreatRequest request,
        ClaimsPrincipal principal,
        SubmitRetreatHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        if (userId is null)
        {
            return IdentityHttp.Problem(ErrorCodes.Unauthorized, "Sign in to continue.");
        }

        var result = await handler.HandleAsync(
                new SubmitRetreatCommand
                {
                    UserId = userId.Value,
                    IsAdministrator = principal.IsAdministrator(),
                    CampaignId = campaignId,
                    ExpectedRevision = request.Revision,
                    BattleId = request.BattleId,
                    TargetTerritoryId = request.TargetTerritoryId,
                },
                cancellationToken)
            .ConfigureAwait(false);
        return PlayResult(result);
    }

    private static async Task<IResult> ResolveBattleAsync(
        Guid campaignId,
        SubmitBattleResultRequest request,
        ClaimsPrincipal principal,
        ResolveBattleHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        if (userId is null)
        {
            return IdentityHttp.Problem(ErrorCodes.Unauthorized, "Sign in to continue.");
        }

        var result = await handler.HandleAsync(
                new SubmitBattleResultCommand
                {
                    UserId = userId.Value,
                    IsAdministrator = principal.IsAdministrator(),
                    CampaignId = campaignId,
                    ExpectedRevision = request.Revision,
                    BattleId = request.BattleId,
                    WinnerForceId = request.WinnerForceId,
                    IsDraw = request.IsDraw,
                },
                cancellationToken)
            .ConfigureAwait(false);
        return PlayResult(result);
    }

    private static async Task<IResult> ExtendScheduleAsync(
        Guid campaignId,
        ExtendCampaignScheduleRequest request,
        ClaimsPrincipal principal,
        ExtendCampaignScheduleHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        if (userId is null)
        {
            return IdentityHttp.Problem(ErrorCodes.Unauthorized, "Sign in to continue.");
        }

        var result = await handler.HandleAsync(
                new ExtendCampaignScheduleCommand
                {
                    UserId = userId.Value,
                    IsAdministrator = principal.IsAdministrator(),
                    CampaignId = campaignId,
                    ExpectedRevision = request.Revision,
                    RoundCount = request.RoundCount,
                    Extensions =
                    [
                        .. (request.Extensions ?? []).Select(static item => new PhaseExtensionInput
                        {
                            WindowId = item.WindowId,
                            DurationAmount = item.DurationAmount,
                            DurationUnit = item.DurationUnit,
                        }),
                    ],
                },
                cancellationToken)
            .ConfigureAwait(false);
        return PlayResult(result);
    }

    private static async Task<IResult> EnterDebugAsync(
        Guid campaignId,
        PlayRevisionRequest request,
        ClaimsPrincipal principal,
        EnterCampaignDebugHandler handler,
        CancellationToken cancellationToken)
    {
        return await PlayCommandAsync(campaignId, request, principal, handler.HandleAsync, cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<IResult> ExitDebugAsync(
        Guid campaignId,
        PlayRevisionRequest request,
        ClaimsPrincipal principal,
        ExitCampaignDebugHandler handler,
        CancellationToken cancellationToken)
    {
        return await PlayCommandAsync(campaignId, request, principal, handler.HandleAsync, cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<IResult> DebugCorrectOrderAsync(
        Guid campaignId,
        SaveOrderDraftRequest request,
        ClaimsPrincipal principal,
        DebugCorrectOrderHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        if (userId is null)
        {
            return IdentityHttp.Problem(ErrorCodes.Unauthorized, "Sign in to continue.");
        }

        var result = await handler.HandleAsync(
                new SaveOrderDraftCommand
                {
                    UserId = userId.Value,
                    IsAdministrator = principal.IsAdministrator(),
                    CampaignId = campaignId,
                    ExpectedRevision = request.Revision,
                    ForceId = request.ForceId,
                    Kind = request.Kind,
                    TargetTerritoryId = request.TargetTerritoryId,
                    StructureTypeId = request.StructureTypeId,
                },
                cancellationToken)
            .ConfigureAwait(false);
        return PlayResult(result);
    }

    private static async Task<IResult> RevealHiddenItemObjectivesAsync(
        Guid campaignId,
        PlayRevisionRequest request,
        ClaimsPrincipal principal,
        RevealHiddenItemObjectivesHandler handler,
        CancellationToken cancellationToken)
    {
        return await PlayCommandAsync(campaignId, request, principal, handler.HandleAsync, cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<IResult> PlayCommandAsync(
        Guid campaignId,
        PlayRevisionRequest request,
        ClaimsPrincipal principal,
        Func<PlayCommand, CancellationToken, Task<Campaign.Application.Common.OperationResult<Campaign.Application.Play.CampaignPlayDetail>>> handle,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        if (userId is null)
        {
            return IdentityHttp.Problem(ErrorCodes.Unauthorized, "Sign in to continue.");
        }

        var result = await handle(
                new PlayCommand
                {
                    UserId = userId.Value,
                    IsAdministrator = principal.IsAdministrator(),
                    CampaignId = campaignId,
                    ExpectedRevision = request.Revision,
                },
                cancellationToken)
            .ConfigureAwait(false);
        return PlayResult(result);
    }

    private static IResult PlayResult(Campaign.Application.Common.OperationResult<Campaign.Application.Play.CampaignPlayDetail> result)
    {
        if (!result.IsSuccess || result.Value is null)
        {
            return IdentityHttp.Problem(result);
        }

        return Results.Ok(PlayResponses.FromDetail(result.Value));
    }
}
