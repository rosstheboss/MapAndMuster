using MapAndMuster.Application.Campaigns;
using MapAndMuster.Application.Common;
using MapAndMuster.Application.Maps;
using MapAndMuster.Application.Notifications;
using MapAndMuster.Application.Ports;
using MapAndMuster.Domain.Campaigns;
using MapAndMuster.Domain.Common;
using MapAndMuster.Domain.Play;

namespace MapAndMuster.Application.Play;

internal static class CampaignPlayPipeline
{
    public static async Task<PlayLoad> LoadAsync(
        ICampaignStore campaigns,
        IClock clock,
        Guid campaignId,
        Guid userId,
        bool isAdministrator,
        CancellationToken cancellationToken,
        IUserAccountStore? accounts = null)
    {
        var campaign = await campaigns.FindByIdAsync(campaignId, cancellationToken).ConfigureAwait(false);
        if (campaign is null || !CampaignAccess.CanView(campaign, userId, isAdministrator))
        {
            return PlayLoad.Fail(ErrorCodes.CampaignNotFound, "The campaign was not found.");
        }

        var utcNow = clock.UtcNow;
        if (campaign.ClosedUtc is not null)
        {
            return new PlayLoad
            {
                IsSuccess = true,
                Campaign = campaign,
                Previous = campaign,
                OriginalRevision = campaign.Revision,
                Changed = false,
            };
        }

        if (CampaignLifecycle.Progress(campaign, utcNow).Status == CampaignStatus.Scheduled)
        {
            return PlayLoad.Fail("play.not_started", "This campaign has not started yet.");
        }

        var map = CampaignLifecycle.ToPlayMap(campaign);
        var players = campaign.Memberships
            .Where(static member => member.IsPlayer)
            .Select(member => new PlayerFactionAssignment(member.UserId, member.FactionId, member.Subfaction))
            .ToArray();
        var itemTypes = CampaignPlayCatalog.ItemPlayRules(campaign);
        var placements = (campaign.MapGraph?.ItemObjectivePlacements ?? [])
            .Select(static item => new ItemObjectiveMapPlacement(item.TypeId, item.TerritoryId))
            .ToArray();
        var seeded = CampaignPlayRules.Seed(
            campaign.PlayState ?? CampaignPlayState.Empty,
            map,
            CampaignMapper.ToSchedule(campaign),
            players,
            utcNow,
            itemTypes,
            placements,
            CampaignPlayCatalog.PickIndex,
            CampaignPlayCatalog.PrivateTypes(campaign),
            [.. campaign.Factions.Select(static faction => faction.Id)],
            [.. campaign.AllyGroups.Select(static group => group.Id)],
            CampaignPlayCatalog.SpecialRules(campaign),
            CampaignPlayCatalog.AllyGroupByFaction(campaign));
        var schedule = CampaignMapper.ToSchedule(campaign);
        var advanced = CampaignPlayRules.Advance(
            seeded.State,
            seeded.Map,
            schedule,
            AllyGroups(campaign),
            utcNow,
            ForceStatuses(campaign),
            CampaignPlayCatalog.PickIndex,
            CampaignPlayCatalog.TerrainSetups(campaign),
            CampaignPlayCatalog.StructureSetups(campaign),
            CampaignPlayCatalog.SpecialRules(campaign));
        var effected = CampaignPlayCatalog.ApplyEffects(campaign, advanced.State, advanced.Map, utcNow);
        effected = await CampaignCompletionLog.SyncAsync(
                campaign,
                effected,
                utcNow,
                accounts,
                revised: false,
                cancellationToken)
            .ConfigureAwait(false);
        var nextGraph = campaign.MapGraph is null || advanced.PreserveMap
            ? campaign.MapGraph
            : CampaignLifecycle.ApplyOwnership(campaign.MapGraph, advanced.Map);
        var nextCampaign = Clone(
            campaign,
            effected,
            nextGraph,
            advanced.PreserveSchedule ? campaign.EndsUtc : advanced.EndsUtc,
            advanced.PreserveSchedule || advanced.RoundCount == 0 ? campaign.RoundCount : advanced.RoundCount,
            utcNow);
        return new PlayLoad
        {
            IsSuccess = true,
            Campaign = nextCampaign,
            Previous = campaign,
            OriginalRevision = campaign.Revision,
            Changed = !ReferenceEquals(effected, campaign.PlayState)
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
            if (outcome.ErrorCode == ErrorCodes.ConcurrencyConflict)
            {
                var current = await campaigns.FindByIdAsync(loaded.Campaign.Id, cancellationToken).ConfigureAwait(false);
                if (current is not null)
                {
                    return new PlayLoad
                    {
                        IsSuccess = true,
                        Campaign = current,
                        Previous = loaded.Previous,
                        OriginalRevision = current.Revision,
                        Changed = false,
                    };
                }
            }

            return PlayLoad.Fail(
                outcome.ErrorCode ?? ErrorCodes.ConcurrencyConflict,
                outcome.Message ?? "The campaign could not be updated.");
        }

        return new PlayLoad
        {
            IsSuccess = true,
            Campaign = outcome.Campaign,
            Previous = loaded.Previous,
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
        CancellationToken cancellationToken,
        CampaignNotificationPublisher? notifications = null,
        bool allowWhenClosed = false)
    {
        var loaded = await LoadAsync(campaigns, clock, campaignId, userId, isAdministrator, cancellationToken, accounts)
            .ConfigureAwait(false);
        if (!loaded.IsSuccess || loaded.Campaign is null)
        {
            return OperationResults.Failure<CampaignPlayDetail>(
                loaded.ErrorCode ?? ErrorCodes.CampaignNotFound,
                loaded.Message ?? "The campaign was not found.");
        }

        if (loaded.Campaign.ClosedUtc is not null && !allowWhenClosed)
        {
            return OperationResults.Failure<CampaignPlayDetail>(
                "play.ended",
                "This campaign has ended.");
        }

        var membership = CampaignMapper.MembershipFor(loaded.Campaign, userId);
        if (membership is null || (!membership.IsPlayer && !membership.IsGameMaster && !isAdministrator))
        {
            return OperationResults.Failure<CampaignPlayDetail>(
                ErrorCodes.CampaignForbidden,
                "Only players and managers can play this campaign.");
        }

        OperationResult<CampaignPlayDetail>? lastConflict = null;
        const int maxAttempts = 3;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            if (attempt > 0)
            {
                loaded = await LoadAsync(campaigns, clock, campaignId, userId, isAdministrator, cancellationToken, accounts)
                    .ConfigureAwait(false);
                if (!loaded.IsSuccess || loaded.Campaign is null)
                {
                    return OperationResults.Failure<CampaignPlayDetail>(
                        loaded.ErrorCode ?? ErrorCodes.CampaignNotFound,
                        loaded.Message ?? "The campaign was not found.");
                }

                if (loaded.Campaign.ClosedUtc is not null && !allowWhenClosed)
                {
                    return OperationResults.Failure<CampaignPlayDetail>(
                        "play.ended",
                        "This campaign has ended.");
                }
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
                clock.UtcNow,
                ForceStatuses(campaign),
                CampaignPlayCatalog.PickIndex,
                CampaignPlayCatalog.TerrainSetups(campaign),
                CampaignPlayCatalog.StructureSetups(campaign),
                CampaignPlayCatalog.SpecialRules(campaign));
            var playMap = advanced.PreserveMap ? workingMap : advanced.Map;
            var endsUtc = mutation.PreserveSchedule && advanced.PreserveSchedule
                ? campaign.EndsUtc
                : advanced.EndsUtc == default ? campaign.EndsUtc : advanced.EndsUtc;
            var effected = CampaignPlayCatalog.ApplyEffects(campaign, advanced.State, playMap, clock.UtcNow);
            effected = await CampaignCompletionLog.SyncAsync(
                    campaign,
                    effected,
                    clock.UtcNow,
                    accounts,
                    revised: true,
                    cancellationToken)
                .ConfigureAwait(false);
            var nextGraph = campaign.MapGraph is null || (mutation.PreserveMap && advanced.PreserveMap)
                ? campaign.MapGraph
                : CampaignLifecycle.ApplyOwnership(campaign.MapGraph, playMap);
            var next = Clone(
                campaign,
                effected,
                nextGraph,
                endsUtc,
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
                attempt == 0 ? expectedRevision : loaded.OriginalRevision,
                next.UpdatedUtc,
                cancellationToken).ConfigureAwait(false);
            if (outcome.IsSuccess && outcome.Campaign is not null)
            {
                if (notifications is not null && loaded.Previous is not null)
                {
                    await notifications.PublishPlayAdvanceAsync(loaded.Previous, outcome.Campaign, cancellationToken)
                        .ConfigureAwait(false);
                }

                return OperationResults.Success(
                    await CampaignPlayMapper.ToDetailAsync(
                            outcome.Campaign, userId, clock.UtcNow, accounts, cancellationToken, isAdministrator)
                        .ConfigureAwait(false));
            }

            if (outcome.ErrorCode != ErrorCodes.ConcurrencyConflict)
            {
                return OperationResults.Failure<CampaignPlayDetail>(
                    outcome.ErrorCode ?? ErrorCodes.CampaignNotFound,
                    outcome.Message ?? "The campaign could not be updated.");
            }

            lastConflict = OperationResults.Failure<CampaignPlayDetail>(
                ErrorCodes.ConcurrencyConflict,
                outcome.Message ?? "The campaign was changed by another request. Reload and try again.");
        }

        return lastConflict ?? OperationResults.Failure<CampaignPlayDetail>(
            ErrorCodes.ConcurrencyConflict,
            "The campaign was changed by another request. Reload and try again.");
    }

    public static IReadOnlyDictionary<Guid, string?> AllyGroups(StoredCampaign campaign)
    {
        return campaign.Factions.ToDictionary(static faction => faction.Id, static faction => faction.AllyGroupName);
    }

    public static IReadOnlyList<ForceStatusSetup> ForceStatuses(StoredCampaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        return
        [
            .. campaign.ForceStatuses
                .Where(static status =>
                    Enum.TryParse<ForceStatusEnableTrigger>(status.EnableTrigger, true, out _)
                    && Enum.TryParse<ForceStatusClearTrigger>(status.ClearTrigger, true, out _))
                .Select(static status => new ForceStatusSetup(
                    status.Id,
                    status.Name,
                    status.Effects,
                    Enum.Parse<ForceStatusEnableTrigger>(status.EnableTrigger, true),
                    Enum.Parse<ForceStatusClearTrigger>(status.ClearTrigger, true))),
        ];
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
            ClosedUtc = existing.ClosedUtc,
            RoundCount = roundCount,
            RoundLengthAmount = existing.RoundLengthAmount,
            RoundLengthUnit = existing.RoundLengthUnit,
            Phases = existing.Phases,
            MapGraph = graph ?? existing.MapGraph,
            TerrainTypes = existing.TerrainTypes,
            StructureTypes = existing.StructureTypes,
            ItemObjectiveTypes = existing.ItemObjectiveTypes,
            PublicObjectiveTypes = existing.PublicObjectiveTypes,
            SpecialRules = existing.SpecialRules,
            Missions = existing.Missions,
            ForceStatuses = existing.ForceStatuses,
            PrivateObjectiveTypes = existing.PrivateObjectiveTypes,
            BattleScoring = existing.BattleScoring,
            RankingObjectivePoints = existing.RankingObjectivePoints,
            SplitForceSupplyPenaltyPercent = existing.SplitForceSupplyPenaltyPercent,
            SplitForceSupplyPenaltyIsPercent = existing.SplitForceSupplyPenaltyIsPercent,
            StandardBattleResultQuestions = existing.StandardBattleResultQuestions,
            ArmyEscalations = existing.ArmyEscalations,
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

    public StoredCampaign? Previous { get; init; }

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
