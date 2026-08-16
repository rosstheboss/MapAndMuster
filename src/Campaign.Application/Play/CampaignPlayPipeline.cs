using Campaign.Application.Campaigns;
using Campaign.Application.Common;
using Campaign.Application.Maps;
using Campaign.Application.Ports;
using Campaign.Domain.Campaigns;
using Campaign.Domain.Common;
using Campaign.Domain.Play;

namespace Campaign.Application.Play;

internal static class CampaignPlayPipeline
{
    public static async Task<PlayLoad> LoadAsync(
        ICampaignStore campaigns,
        IClock clock,
        Guid campaignId,
        Guid userId,
        bool isAdministrator,
        CancellationToken cancellationToken)
    {
        var campaign = await campaigns.FindByIdAsync(campaignId, cancellationToken).ConfigureAwait(false);
        if (campaign is null || !CampaignAccess.CanView(campaign, userId, isAdministrator))
        {
            return PlayLoad.Fail(ErrorCodes.CampaignNotFound, "The campaign was not found.");
        }

        var utcNow = clock.UtcNow;
        if (CampaignLifecycle.Progress(campaign, utcNow).Status == CampaignStatus.Scheduled)
        {
            return PlayLoad.Fail("play.not_started", "This campaign has not started yet.");
        }

        var map = CampaignLifecycle.ToPlayMap(campaign);
        var players = campaign.Memberships
            .Where(static member => member.IsPlayer)
            .Select(member => new PlayerFactionAssignment(member.UserId, member.FactionId))
            .ToArray();
        var seeded = CampaignPlayRules.Seed(
            campaign.PlayState ?? CampaignPlayState.Empty,
            map,
            CampaignMapper.ToSchedule(campaign),
            players,
            utcNow);
        var schedule = CampaignMapper.ToSchedule(campaign);
        var advanced = CampaignPlayRules.Advance(
            seeded.State,
            seeded.Map,
            schedule,
            AllyGroups(campaign),
            utcNow);
        var nextGraph = campaign.MapGraph is null || advanced.PreserveMap
            ? campaign.MapGraph
            : CampaignLifecycle.ApplyOwnership(campaign.MapGraph, advanced.Map);
        var nextCampaign = Clone(
            campaign,
            advanced.State,
            nextGraph,
            advanced.PreserveSchedule ? campaign.EndsUtc : advanced.EndsUtc,
            advanced.PreserveSchedule || advanced.RoundCount == 0 ? campaign.RoundCount : advanced.RoundCount,
            utcNow);
        return new PlayLoad
        {
            IsSuccess = true,
            Campaign = nextCampaign,
            OriginalRevision = campaign.Revision,
            Changed = !ReferenceEquals(advanced.State, campaign.PlayState)
                || nextCampaign.EndsUtc != campaign.EndsUtc
                || nextCampaign.RoundCount != campaign.RoundCount
                || !ReferenceEquals(nextGraph, campaign.MapGraph),
        };
    }

    public static async Task<PlayLoad> PersistIfChangedAsync(
        ICampaignStore campaigns,
        PlayLoad loaded,
        CancellationToken cancellationToken)
    {
        if (!loaded.Changed || loaded.Campaign is null)
        {
            return loaded;
        }

        var outcome = await campaigns.UpdatePlayStateAsync(
            loaded.Campaign.Id,
            loaded.Campaign.PlayState!,
            loaded.Campaign.MapGraph,
            loaded.Campaign.EndsUtc,
            loaded.Campaign.RoundCount,
            loaded.OriginalRevision,
            loaded.Campaign.UpdatedUtc,
            cancellationToken).ConfigureAwait(false);
        if (!outcome.IsSuccess || outcome.Campaign is null)
        {
            return PlayLoad.Fail(
                outcome.ErrorCode ?? ErrorCodes.ConcurrencyConflict,
                outcome.Message ?? "The campaign could not be updated.");
        }

        return new PlayLoad
        {
            IsSuccess = true,
            Campaign = outcome.Campaign,
            OriginalRevision = outcome.Campaign.Revision,
            Changed = false,
        };
    }

    public static async Task<OperationResult<CampaignPlayDetail>> MutateAsync(
        ICampaignStore campaigns,
        IClock clock,
        IUserAccountStore accounts,
        Guid campaignId,
        Guid userId,
        bool isAdministrator,
        int expectedRevision,
        Func<CampaignPlayState, PlayMap, StoredCampaign, DateTimeOffset, PlayMutation> mutate,
        CancellationToken cancellationToken)
    {
        var loaded = await LoadAsync(campaigns, clock, campaignId, userId, isAdministrator, cancellationToken)
            .ConfigureAwait(false);
        if (!loaded.IsSuccess || loaded.Campaign is null)
        {
            return OperationResults.Failure<CampaignPlayDetail>(
                loaded.ErrorCode ?? ErrorCodes.CampaignNotFound,
                loaded.Message ?? "The campaign was not found.");
        }

        var membership = CampaignMapper.MembershipFor(loaded.Campaign, userId);
        if (membership is null || (!membership.IsPlayer && !membership.IsGameMaster && !isAdministrator))
        {
            return OperationResults.Failure<CampaignPlayDetail>(
                ErrorCodes.CampaignForbidden,
                "Only players and managers can play this campaign.");
        }

        if (loaded.OriginalRevision != expectedRevision && loaded.Campaign.Revision != expectedRevision)
        {
            return OperationResults.Failure<CampaignPlayDetail>(
                ErrorCodes.ConcurrencyConflict,
                "The campaign was changed by another request. Reload and try again.");
        }

        var campaign = loaded.Campaign;
        var map = CampaignLifecycle.ToPlayMap(campaign);
        var mutation = mutate(campaign.PlayState ?? CampaignPlayState.Empty, map, campaign, clock.UtcNow);
        if (!mutation.IsSuccess)
        {
            return OperationResults.Failure<CampaignPlayDetail>(
                mutation.Error?.Code ?? ErrorCodes.ValidationFailed,
                mutation.Error?.Message ?? "The play command was invalid.");
        }

        var workingMap = mutation.PreserveMap ? map : mutation.Map;
        var advanced = CampaignPlayRules.Advance(
            mutation.State,
            workingMap,
            CampaignMapper.ToSchedule(campaign),
            AllyGroups(campaign),
            clock.UtcNow);
        var nextGraph = campaign.MapGraph is null || (mutation.PreserveMap && advanced.PreserveMap)
            ? campaign.MapGraph
            : CampaignLifecycle.ApplyOwnership(campaign.MapGraph, advanced.PreserveMap ? workingMap : advanced.Map);
        var next = Clone(
            campaign,
            advanced.State,
            nextGraph,
            mutation.PreserveSchedule && advanced.PreserveSchedule
                ? campaign.EndsUtc
                : advanced.EndsUtc == default ? campaign.EndsUtc : advanced.EndsUtc,
            mutation.PreserveSchedule && advanced.PreserveSchedule || advanced.RoundCount == 0
                ? campaign.RoundCount
                : advanced.RoundCount,
            clock.UtcNow);
        var outcome = await campaigns.UpdatePlayStateAsync(
            next.Id,
            next.PlayState!,
            next.MapGraph,
            next.EndsUtc,
            next.RoundCount,
            expectedRevision,
            next.UpdatedUtc,
            cancellationToken).ConfigureAwait(false);
        if (!outcome.IsSuccess || outcome.Campaign is null)
        {
            return OperationResults.Failure<CampaignPlayDetail>(
                outcome.ErrorCode ?? ErrorCodes.ConcurrencyConflict,
                outcome.Message ?? "The campaign could not be updated.");
        }

        return OperationResults.Success(
            await CampaignPlayMapper.ToDetailAsync(outcome.Campaign, userId, clock.UtcNow, accounts, cancellationToken)
                .ConfigureAwait(false));
    }

    public static IReadOnlyDictionary<Guid, string?> AllyGroups(StoredCampaign campaign)
    {
        return campaign.Factions.ToDictionary(static faction => faction.Id, static faction => faction.AllyGroupName);
    }

    public static StoredCampaign Clone(
        StoredCampaign existing,
        CampaignPlayState play,
        StoredMapGraph? graph,
        DateTimeOffset endsUtc,
        int roundCount,
        DateTimeOffset updatedUtc)
    {
        return new StoredCampaign
        {
            Id = existing.Id,
            Name = existing.Name,
            Description = existing.Description,
            PlayerSlotCount = existing.PlayerSlotCount,
            IsPrivate = existing.IsPrivate,
            IsPubliclyViewable = existing.IsPubliclyViewable,
            JoinPasswordHash = existing.JoinPasswordHash,
            CreatorIsParticipant = existing.CreatorIsParticipant,
            City = existing.City,
            Region = existing.Region,
            Country = existing.Country,
            MapStorageKey = existing.MapStorageKey,
            Revision = existing.Revision,
            CreatedUtc = existing.CreatedUtc,
            UpdatedUtc = updatedUtc,
            CreatedByUserId = existing.CreatedByUserId,
            Memberships = existing.Memberships,
            Factions = existing.Factions,
            AllyGroups = existing.AllyGroups,
            Links = existing.Links,
            TimeZoneId = existing.TimeZoneId,
            StartsUtc = existing.StartsUtc,
            EndsUtc = endsUtc,
            RoundCount = roundCount,
            RoundLengthAmount = existing.RoundLengthAmount,
            RoundLengthUnit = existing.RoundLengthUnit,
            Phases = existing.Phases,
            MapGraph = graph ?? existing.MapGraph,
            TerrainTypes = existing.TerrainTypes,
            StructureTypes = existing.StructureTypes,
            PlayState = play,
        };
    }

    public static Task NotifyManagersIfNeededAsync(
        ICampaignStore campaigns,
        IUserAccountStore accounts,
        IEmailOutbox outbox,
        Guid campaignId,
        int? revision,
        CancellationToken cancellationToken)
    {
        _ = (campaigns, accounts, outbox, campaignId, revision, cancellationToken);
        return Task.CompletedTask;
    }
}

internal sealed class PlayLoad
{
    public required bool IsSuccess { get; init; }

    public StoredCampaign? Campaign { get; init; }

    public int OriginalRevision { get; init; }

    public bool Changed { get; init; }

    public string? ErrorCode { get; init; }

    public string? Message { get; init; }

    public static PlayLoad Fail(string errorCode, string message)
    {
        return new PlayLoad { IsSuccess = false, ErrorCode = errorCode, Message = message };
    }
}

internal sealed class PlayMutation
{
    public required bool IsSuccess { get; init; }

    public CampaignPlayState State { get; init; } = CampaignPlayState.Empty;

    public PlayMap Map { get; init; } = new([], []);

    public DateTimeOffset EndsUtc { get; init; }

    public int RoundCount { get; init; }

    public bool PreserveMap { get; init; }

    public bool PreserveSchedule { get; init; }

    public DomainError? Error { get; init; }

    public static PlayMutation Fail(DomainError? error)
    {
        return new PlayMutation { IsSuccess = false, Error = error };
    }

    public static PlayMutation Ok(CampaignPlayState? state, PlayMap map, bool preserveMap = false)
    {
        return new PlayMutation
        {
            IsSuccess = true,
            State = state ?? CampaignPlayState.Empty,
            Map = map,
            PreserveMap = preserveMap,
            PreserveSchedule = true,
        };
    }

    public static PlayMutation FromOutcome(PlayOutcome outcome)
    {
        return new PlayMutation
        {
            IsSuccess = true,
            State = outcome.State,
            Map = outcome.Map,
            EndsUtc = outcome.EndsUtc,
            RoundCount = outcome.RoundCount,
            PreserveMap = outcome.PreserveMap,
            PreserveSchedule = outcome.PreserveSchedule,
        };
    }
}
