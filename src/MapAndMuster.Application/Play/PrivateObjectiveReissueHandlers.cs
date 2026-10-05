using MapAndMuster.Application.Campaigns;
using MapAndMuster.Application.Common;
using MapAndMuster.Application.Notifications;
using MapAndMuster.Application.Ports;
using MapAndMuster.Domain.Campaigns;
using MapAndMuster.Domain.Common;
using MapAndMuster.Domain.Play;

namespace MapAndMuster.Application.Play;

/// <summary>
/// Asks a campaign manager to replace an unrevealed private objective the holder cannot achieve.
/// </summary>
public sealed class RequestPrivateObjectiveReissueHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;
    private readonly IUserAccountStore _accounts;
    private readonly CampaignNotificationPublisher? _notifications;

    /// <summary>Initializes a new handler.</summary>
    public RequestPrivateObjectiveReissueHandler(
        ICampaignStore campaigns,
        IClock clock,
        IUserAccountStore accounts,
        CampaignNotificationPublisher? notifications = null)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(accounts);
        _campaigns = campaigns;
        _clock = clock;
        _accounts = accounts;
        _notifications = notifications;
    }

    /// <summary>Records a reissue request for a live campaign.</summary>
    public Task<OperationResult<CampaignPlayDetail>> HandleAsync(
        RequestPrivateObjectiveReissueCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return CampaignPlayPipeline.MutateAsync(
            _campaigns,
            _clock,
            _accounts,
            command.CampaignId,
            command.UserId,
            command.IsAdministrator,
            command.ExpectedRevision,
            (state, _, campaign, utcNow) =>
            {
                if (!PrivateObjectiveReissueAccess.IsLive(campaign, utcNow, out var liveError))
                {
                    return PlayMutation.Fail(liveError);
                }

                if (!PrivateObjectiveReissueAccess.IsHolder(state, campaign, command.UserId, command.AssignmentId))
                {
                    return PlayMutation.Fail(new DomainError(
                        "privateObjective.forbidden",
                        "Only a holder of that private objective can request a reissue."));
                }

                if (!PrivateObjectiveRules.TryRequestReissue(
                        state,
                        command.AssignmentId,
                        command.UserId,
                        utcNow,
                        out var next,
                        out var error)
                    || next is null)
                {
                    return PlayMutation.Fail(error);
                }

                return PlayMutation.Ok(next, new PlayMap([], []), preserveMap: true);
            },
            cancellationToken,
            _notifications);
    }
}

/// <summary>
/// Approves or denies a pending private-objective reissue.
/// </summary>
public sealed class DecidePrivateObjectiveReissueHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;
    private readonly IUserAccountStore _accounts;
    private readonly CampaignNotificationPublisher? _notifications;

    /// <summary>Initializes a new handler.</summary>
    public DecidePrivateObjectiveReissueHandler(
        ICampaignStore campaigns,
        IClock clock,
        IUserAccountStore accounts,
        CampaignNotificationPublisher? notifications = null)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(accounts);
        _campaigns = campaigns;
        _clock = clock;
        _accounts = accounts;
        _notifications = notifications;
    }

    /// <summary>Approves a replacement or denies the request.</summary>
    public Task<OperationResult<CampaignPlayDetail>> HandleAsync(
        DecidePrivateObjectiveReissueCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return CampaignPlayPipeline.MutateAsync(
            _campaigns,
            _clock,
            _accounts,
            command.CampaignId,
            command.UserId,
            command.IsAdministrator,
            command.ExpectedRevision,
            (state, _, campaign, utcNow) =>
            {
                if (!PrivateObjectiveReissueAccess.IsStaff(campaign, command.UserId, command.IsAdministrator))
                {
                    return PlayMutation.Fail(new DomainError(
                        ErrorCodes.CampaignForbidden,
                        "Only a campaign manager can decide a private-objective reissue."));
                }

                if (!PrivateObjectiveReissueAccess.IsLive(campaign, utcNow, out var liveError))
                {
                    return PlayMutation.Fail(liveError);
                }

                if (!PrivateObjectiveRules.TryDecideReissue(
                        state,
                        command.AssignmentId,
                        command.UserId,
                        command.Approved,
                        command.Note,
                        utcNow,
                        CampaignPlayCatalog.PrivateTypes(campaign),
                        CampaignPlayCatalog.PickIndex,
                        out var next,
                        out var error,
                        CampaignPlayCatalog.FactionByPlayer(campaign),
                        CampaignPlayCatalog.AllyGroupByFaction(campaign),
                        PrivateObjectiveReissueAccess.PlayerIds(campaign),
                        [.. campaign.Factions.Select(static faction => faction.Id)],
                        [.. campaign.AllyGroups.Select(static group => group.Id)])
                    || next is null)
                {
                    return PlayMutation.Fail(error);
                }

                return PlayMutation.Ok(next, new PlayMap([], []), preserveMap: true);
            },
            cancellationToken,
            _notifications);
    }
}

/// <summary>
/// Replaces a private objective immediately for the acting manager or during that manager's debug session.
/// </summary>
public sealed class ReissuePrivateObjectiveHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;
    private readonly IUserAccountStore _accounts;
    private readonly CampaignNotificationPublisher? _notifications;

    /// <summary>Initializes a new handler.</summary>
    public ReissuePrivateObjectiveHandler(
        ICampaignStore campaigns,
        IClock clock,
        IUserAccountStore accounts,
        CampaignNotificationPublisher? notifications = null)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(accounts);
        _campaigns = campaigns;
        _clock = clock;
        _accounts = accounts;
        _notifications = notifications;
    }

    /// <summary>Reissues without a pending request.</summary>
    public Task<OperationResult<CampaignPlayDetail>> HandleAsync(
        ReissuePrivateObjectiveCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return CampaignPlayPipeline.MutateAsync(
            _campaigns,
            _clock,
            _accounts,
            command.CampaignId,
            command.UserId,
            command.IsAdministrator,
            command.ExpectedRevision,
            (state, _, campaign, utcNow) =>
            {
                if (!PrivateObjectiveReissueAccess.IsStaff(campaign, command.UserId, command.IsAdministrator))
                {
                    return PlayMutation.Fail(new DomainError(
                        ErrorCodes.CampaignForbidden,
                        "Only a campaign manager can reissue a private objective."));
                }

                if (!PrivateObjectiveReissueAccess.IsLive(campaign, utcNow, out var liveError))
                {
                    return PlayMutation.Fail(liveError);
                }

                var assignment = state.PrivateObjectives.FirstOrDefault(item => item.Id == command.AssignmentId);
                var ownsIt = assignment?.HolderKind is PrivateObjectiveHolderKind.Player or PrivateObjectiveHolderKind.Traitor
                    && assignment.HolderId == command.UserId;
                var debugging = state.DebugActorUserId == command.UserId;
                if (assignment is null || (!ownsIt && !debugging))
                {
                    return PlayMutation.Fail(new DomainError(
                        "privateObjective.reissue.forbidden",
                        "Reissue your own private objective, or reissue another player's while you are in debug mode."));
                }

                if (!PrivateObjectiveRules.TryReissueImmediately(
                        state,
                        command.AssignmentId,
                        command.UserId,
                        command.Note,
                        utcNow,
                        CampaignPlayCatalog.PrivateTypes(campaign),
                        CampaignPlayCatalog.PickIndex,
                        out var next,
                        out var error,
                        CampaignPlayCatalog.FactionByPlayer(campaign),
                        CampaignPlayCatalog.AllyGroupByFaction(campaign),
                        PrivateObjectiveReissueAccess.PlayerIds(campaign),
                        [.. campaign.Factions.Select(static faction => faction.Id)],
                        [.. campaign.AllyGroups.Select(static group => group.Id)])
                    || next is null)
                {
                    return PlayMutation.Fail(error);
                }

                return PlayMutation.Ok(next, new PlayMap([], []), preserveMap: true);
            },
            cancellationToken,
            _notifications);
    }
}

internal static class PrivateObjectiveReissueAccess
{
    public static bool IsLive(StoredCampaign campaign, DateTimeOffset utcNow, out DomainError? error)
    {
        if (CampaignLifecycle.Progress(campaign, utcNow).Status == CampaignStatus.InProgress)
        {
            error = null;
            return true;
        }

        error = new DomainError(
            "privateObjective.not_live",
            "Private objectives can be reissued only while the campaign is in progress.");
        return false;
    }

    public static bool IsStaff(StoredCampaign campaign, Guid userId, bool isAdministrator)
    {
        var membership = CampaignMapper.MembershipFor(campaign, userId);
        return membership?.IsGameMaster == true || isAdministrator;
    }

    public static bool IsHolder(CampaignPlayState state, StoredCampaign campaign, Guid userId, Guid assignmentId)
    {
        var assignment = state.PrivateObjectives.FirstOrDefault(item => item.Id == assignmentId);
        if (assignment is null)
        {
            return false;
        }

        var membership = CampaignMapper.MembershipFor(campaign, userId);
        var allyGroupId = membership?.FactionId is { } factionId
            ? CampaignPlayCatalog.AllyGroupByFaction(campaign).GetValueOrDefault(factionId)
            : null;
        return assignment.HolderKind switch
        {
            PrivateObjectiveHolderKind.Player or PrivateObjectiveHolderKind.Traitor => assignment.HolderId == userId,
            PrivateObjectiveHolderKind.Faction => assignment.HolderId == membership?.FactionId
                && (string.IsNullOrWhiteSpace(assignment.HolderSubfaction)
                    || string.Equals(assignment.HolderSubfaction, membership?.Subfaction, StringComparison.OrdinalIgnoreCase)),
            PrivateObjectiveHolderKind.AllyGroup => assignment.HolderId == allyGroupId,
            _ => false,
        };
    }

    public static List<Guid> PlayerIds(StoredCampaign campaign)
    {
        return [.. campaign.Memberships.Where(static member => member.IsPlayer).Select(static member => member.UserId)];
    }
}
