using System.Diagnostics.CodeAnalysis;
using Campaign.Application.Campaigns;
using Campaign.Application.Common;
using Campaign.Application.Maps;
using Campaign.Application.Notifications;
using Campaign.Application.Ports;
using Campaign.Domain.Campaigns;
using Campaign.Domain.Play;

namespace Campaign.Application.Play;

/// <summary>
/// Loads and advances the public play board for a viewer who can see the campaign.
/// </summary>
public sealed class GetCampaignPlayHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;
    private readonly IUserAccountStore _accounts;
    private readonly CampaignNotificationPublisher? _notifications;

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    public GetCampaignPlayHandler(
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

    /// <summary>
    /// Returns the current play view after seeding and advancing windows.
    /// </summary>
    public async Task<OperationResult<CampaignPlayDetail>> HandleAsync(
        Guid campaignId,
        Guid userId,
        bool isAdministrator,
        CancellationToken cancellationToken)
    {
        var loaded = await CampaignPlayPipeline.LoadAsync(_campaigns, _clock, campaignId, userId, isAdministrator, cancellationToken)
            .ConfigureAwait(false);
        if (!loaded.IsSuccess || loaded.Campaign is null)
        {
            return OperationResults.Failure<CampaignPlayDetail>(
                loaded.ErrorCode ?? ErrorCodes.CampaignNotFound,
                loaded.Message ?? "The campaign was not found.");
        }

        var persisted = await CampaignPlayPipeline.PersistIfChangedAsync(_campaigns, loaded, cancellationToken)
            .ConfigureAwait(false);
        if (!persisted.IsSuccess || persisted.Campaign is null)
        {
            return OperationResults.Failure<CampaignPlayDetail>(
                persisted.ErrorCode ?? ErrorCodes.ConcurrencyConflict,
                persisted.Message ?? "The campaign could not be updated.");
        }

        if (_notifications is not null && loaded.Changed && loaded.Previous is not null)
        {
            await _notifications.PublishPlayAdvanceAsync(loaded.Previous, persisted.Campaign, cancellationToken)
                .ConfigureAwait(false);
        }

        return OperationResults.Success(
            await CampaignPlayMapper.ToDetailAsync(
                persisted.Campaign, userId, _clock.UtcNow, _accounts, cancellationToken, isAdministrator)
                .ConfigureAwait(false));
    }
}

/// <summary>
/// Saves a draft order.
/// </summary>
public sealed class SaveOrderDraftHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;
    private readonly IUserAccountStore _accounts;
    private readonly CampaignNotificationPublisher? _notifications;

    /// <summary>Initializes a new handler.</summary>
    public SaveOrderDraftHandler(ICampaignStore campaigns, IClock clock, IUserAccountStore accounts, CampaignNotificationPublisher? notifications = null)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(accounts);
        _campaigns = campaigns;
        _clock = clock;
        _accounts = accounts;
        _notifications = notifications;
    }

    /// <summary>Saves a draft for one force.</summary>
    public async Task<OperationResult<CampaignPlayDetail>> HandleAsync(
        SaveOrderDraftCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!Enum.TryParse<ActionKind>(command.Kind, ignoreCase: true, out var kind) || !Enum.IsDefined(kind))
        {
            return OperationResults.Failure<CampaignPlayDetail>("order.kind.invalid", "Choose a valid action.");
        }

        return await CampaignPlayPipeline.MutateAsync(
            _campaigns,
            _clock,
            _accounts,
            command.CampaignId,
            command.UserId,
            command.IsAdministrator,
            command.ExpectedRevision,
            (state, map, campaign, utcNow) =>
            {
                if (!CampaignPlayRules.TrySaveDraft(
                    state,
                    command.UserId,
                    command.ForceId,
                    kind,
                    command.TargetTerritoryId,
                    command.StructureTypeId,
                    map,
                    CampaignPlayPipeline.AllyGroups(campaign),
                    campaign.StructureTypes.Select(static type => type.Id).ToHashSet(),
                    utcNow,
                    out var next,
                    out var error))
                {
                    return PlayMutation.Fail(error);
                }

                return PlayMutation.Ok(next, map, preserveMap: true);
            },
            cancellationToken,
            _notifications).ConfigureAwait(false);
    }
}

/// <summary>
/// Commits the caller's drafts for the open action window.
/// </summary>
public sealed class CommitOrdersHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;
    private readonly IUserAccountStore _accounts;
    private readonly CampaignNotificationPublisher? _notifications;

    /// <summary>Initializes a new handler.</summary>
    public CommitOrdersHandler(ICampaignStore campaigns, IClock clock, IUserAccountStore accounts, CampaignNotificationPublisher? notifications = null)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(accounts);
        _campaigns = campaigns;
        _clock = clock;
        _accounts = accounts;
        _notifications = notifications;
    }

    /// <summary>Commits drafts.</summary>
    public Task<OperationResult<CampaignPlayDetail>> HandleAsync(PlayCommand command, CancellationToken cancellationToken)
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
            (state, map, campaign, utcNow) =>
            {
                if (!CampaignPlayRules.TryCommit(
                    state,
                    map,
                    command.UserId,
                    CampaignPlayPipeline.AllyGroups(campaign),
                    utcNow,
                    out var outcome,
                    out var error,
                    CampaignPlayPipeline.ForceStatuses(campaign)))
                {
                    return PlayMutation.Fail(error);
                }

                return PlayMutation.FromOutcome(outcome!);
            },
            cancellationToken,
            _notifications);
    }
}

/// <summary>
/// Withdraws a commitment.
/// </summary>
public sealed class UncommitOrdersHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;
    private readonly IUserAccountStore _accounts;
    private readonly CampaignNotificationPublisher? _notifications;

    /// <summary>Initializes a new handler.</summary>
    public UncommitOrdersHandler(ICampaignStore campaigns, IClock clock, IUserAccountStore accounts, CampaignNotificationPublisher? notifications = null)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(accounts);
        _campaigns = campaigns;
        _clock = clock;
        _accounts = accounts;
        _notifications = notifications;
    }

    /// <summary>Uncommits.</summary>
    public Task<OperationResult<CampaignPlayDetail>> HandleAsync(PlayCommand command, CancellationToken cancellationToken)
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
            (state, map, _, utcNow) =>
            {
                if (!CampaignPlayRules.TryUncommit(state, command.UserId, utcNow, out var next, out var error))
                {
                    return PlayMutation.Fail(error);
                }

                return PlayMutation.Ok(next, map, preserveMap: true);
            },
            cancellationToken,
            _notifications);
    }
}

/// <summary>
/// Submits a battle result.
/// </summary>
public sealed class SubmitBattleResultHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;
    private readonly IEmailOutbox _outbox;
    private readonly IUserAccountStore _accounts;
    private readonly CampaignNotificationPublisher? _notifications;

    /// <summary>Initializes a new handler.</summary>
    public SubmitBattleResultHandler(
        ICampaignStore campaigns,
        IClock clock,
        IEmailOutbox outbox,
        IUserAccountStore accounts,
        CampaignNotificationPublisher? notifications = null)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(outbox);
        ArgumentNullException.ThrowIfNull(accounts);
        _campaigns = campaigns;
        _clock = clock;
        _outbox = outbox;
        _accounts = accounts;
        _notifications = notifications;
    }

    /// <summary>Records a result.</summary>
    public async Task<OperationResult<CampaignPlayDetail>> HandleAsync(
        SubmitBattleResultCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var result = await CampaignPlayPipeline.MutateAsync(
            _campaigns,
            _clock,
            _accounts,
            command.CampaignId,
            command.UserId,
            command.IsAdministrator,
            command.ExpectedRevision,
            (state, map, campaign, utcNow) =>
            {
                if (!BattleScoreRequirements.TryRequire(campaign, command, out var scoreError))
                {
                    return PlayMutation.Fail(scoreError);
                }

                var membership = CampaignMapper.MembershipFor(campaign, command.UserId);
                var isStaff = command.IsAdministrator || membership?.IsGameMaster == true;
                if (!CampaignPlayCatalog.TryToReports(command.Reports, out var reports, out var reportError))
                {
                    return PlayMutation.Fail(reportError);
                }

                if (!CampaignPlayRules.TrySubmitBattleResult(
                    state,
                    command.UserId,
                    command.BattleId,
                    command.WinnerForceId,
                    command.IsDraw,
                    utcNow,
                    out var outcome,
                    out var error,
                    command.WinnerScore,
                    command.LoserScore,
                    CampaignPlayPipeline.ForceStatuses(campaign),
                    reports,
                    CampaignPlayCatalog.MissionQuestions(campaign, state.Battles.FirstOrDefault(item => item.Id == command.BattleId)?.TerritoryId ?? Guid.Empty),
                    isStaff,
                    map,
                    CampaignPlayCatalog.Supply(campaign),
                    CampaignPlayPipeline.AllyGroups(campaign),
                    CampaignPlayCatalog.PickIndex))
                {
                    return PlayMutation.Fail(error);
                }

                return PlayMutation.FromOutcome(outcome!);
            },
            cancellationToken,
            _notifications).ConfigureAwait(false);
        if (result.IsSuccess)
        {
            await CampaignPlayPipeline.NotifyManagersIfNeededAsync(
                _campaigns,
                _accounts,
                _outbox,
                command.CampaignId,
                result.Value?.Revision,
                cancellationToken).ConfigureAwait(false);
        }

        return result;
    }
}

/// <summary>
/// Accepts the opponent's battle result.
/// </summary>
public sealed class AcceptBattleResultHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;
    private readonly IUserAccountStore _accounts;
    private readonly CampaignNotificationPublisher? _notifications;

    /// <summary>Initializes a new handler.</summary>
    public AcceptBattleResultHandler(ICampaignStore campaigns, IClock clock, IUserAccountStore accounts, CampaignNotificationPublisher? notifications = null)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(accounts);
        _campaigns = campaigns;
        _clock = clock;
        _accounts = accounts;
        _notifications = notifications;
    }

    /// <summary>Accepts the opponent submission.</summary>
    public Task<OperationResult<CampaignPlayDetail>> HandleAsync(BattleActionCommand command, CancellationToken cancellationToken)
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
            (state, map, campaign, utcNow) =>
            {
                var membership = CampaignMapper.MembershipFor(campaign, command.UserId);
                var isStaff = command.IsAdministrator || membership?.IsGameMaster == true;
                if (!CampaignPlayRules.TryAcceptBattleResult(
                    state,
                    command.UserId,
                    command.BattleId,
                    utcNow,
                    out var outcome,
                    out var error,
                    CampaignPlayPipeline.ForceStatuses(campaign),
                    isStaff,
                    map,
                    CampaignPlayCatalog.Supply(campaign),
                    CampaignPlayPipeline.AllyGroups(campaign),
                    CampaignPlayCatalog.PickIndex))
                {
                    return PlayMutation.Fail(error);
                }

                return PlayMutation.FromOutcome(outcome!);
            },
            cancellationToken,
            _notifications);
    }
}

/// <summary>
/// Records a manager override for a disputed or open battle.
/// </summary>
public sealed class ResolveBattleHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;
    private readonly IUserAccountStore _accounts;
    private readonly CampaignNotificationPublisher? _notifications;

    /// <summary>Initializes a new handler.</summary>
    public ResolveBattleHandler(ICampaignStore campaigns, IClock clock, IUserAccountStore accounts, CampaignNotificationPublisher? notifications = null)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(accounts);
        _campaigns = campaigns;
        _clock = clock;
        _accounts = accounts;
        _notifications = notifications;
    }

    /// <summary>Applies a manager result.</summary>
    public Task<OperationResult<CampaignPlayDetail>> HandleAsync(
        SubmitBattleResultCommand command,
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
            (state, map, campaign, utcNow) =>
            {
                var membership = CampaignMapper.MembershipFor(campaign, command.UserId);
                if (membership?.IsGameMaster != true && !command.IsAdministrator)
                {
                    return PlayMutation.Fail(new Domain.Common.DomainError(
                        ErrorCodes.CampaignForbidden,
                        "Only a campaign manager or administrator can override battle results."));
                }

                if (!BattleScoreRequirements.TryRequire(campaign, command, out var scoreError))
                {
                    return PlayMutation.Fail(scoreError);
                }

                if (!CampaignPlayCatalog.TryToReports(command.Reports, out var reports, out var reportError))
                {
                    return PlayMutation.Fail(reportError);
                }

                if (!CampaignPlayRules.TryResolveBattle(
                    state,
                    command.UserId,
                    command.BattleId,
                    command.WinnerForceId,
                    command.IsDraw,
                    utcNow,
                    out var next,
                    out var error,
                    command.WinnerScore,
                    command.LoserScore,
                    CampaignPlayPipeline.ForceStatuses(campaign),
                    reports,
                    CampaignPlayCatalog.MissionQuestions(campaign, state.Battles.FirstOrDefault(item => item.Id == command.BattleId)?.TerritoryId ?? Guid.Empty),
                    map,
                    CampaignPlayCatalog.Supply(campaign),
                    CampaignPlayPipeline.AllyGroups(campaign),
                    CampaignPlayCatalog.PickIndex))
                {
                    return PlayMutation.Fail(error);
                }

                return PlayMutation.Ok(next, map, preserveMap: true);
            },
            cancellationToken,
            _notifications);
    }
}

/// <summary>
/// Records a retreat after a loss.
/// </summary>
public sealed class SubmitRetreatHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;
    private readonly IUserAccountStore _accounts;
    private readonly CampaignNotificationPublisher? _notifications;

    /// <summary>Initializes a new handler.</summary>
    public SubmitRetreatHandler(ICampaignStore campaigns, IClock clock, IUserAccountStore accounts, CampaignNotificationPublisher? notifications = null)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(accounts);
        _campaigns = campaigns;
        _clock = clock;
        _accounts = accounts;
        _notifications = notifications;
    }

    /// <summary>Saves a retreat destination.</summary>
    public Task<OperationResult<CampaignPlayDetail>> HandleAsync(SubmitRetreatCommand command, CancellationToken cancellationToken)
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
            (state, map, campaign, utcNow) =>
            {
                if (!CampaignPlayRules.TrySubmitRetreat(
                    state,
                    map,
                    command.UserId,
                    command.BattleId,
                    command.TargetTerritoryId,
                    utcNow,
                    out var outcome,
                    out var error,
                    CampaignPlayPipeline.ForceStatuses(campaign)))
                {
                    return PlayMutation.Fail(error);
                }

                return PlayMutation.FromOutcome(outcome!);
            },
            cancellationToken,
            _notifications);
    }
}

/// <summary>
/// Commits a surrender and retreat while the force is engaged.
/// </summary>
public sealed class SubmitSurrenderHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;
    private readonly IUserAccountStore _accounts;
    private readonly CampaignNotificationPublisher? _notifications;

    /// <summary>Initializes a new handler.</summary>
    public SubmitSurrenderHandler(
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

    /// <summary>Saves a committed surrender destination.</summary>
    public Task<OperationResult<CampaignPlayDetail>> HandleAsync(SubmitRetreatCommand command, CancellationToken cancellationToken)
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
            (state, map, campaign, utcNow) =>
            {
                if (!CampaignPlayRules.TrySubmitSurrender(
                    state,
                    map,
                    command.UserId,
                    command.BattleId,
                    command.TargetTerritoryId,
                    utcNow,
                    out var outcome,
                    out var error,
                    CampaignPlayPipeline.ForceStatuses(campaign),
                    CampaignPlayPipeline.AllyGroups(campaign),
                    campaign.BattleScoring))
                {
                    return PlayMutation.Fail(error);
                }

                return PlayMutation.FromOutcome(outcome!);
            },
            cancellationToken,
            _notifications);
    }
}

/// <summary>
/// Extends remaining windows and/or appends rounds.
/// </summary>
public sealed class ExtendCampaignScheduleHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;
    private readonly IUserAccountStore _accounts;
    private readonly CampaignNotificationPublisher? _notifications;

    /// <summary>Initializes a new handler.</summary>
    public ExtendCampaignScheduleHandler(ICampaignStore campaigns, IClock clock, IUserAccountStore accounts, CampaignNotificationPublisher? notifications = null)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(accounts);
        _campaigns = campaigns;
        _clock = clock;
        _accounts = accounts;
        _notifications = notifications;
    }

    /// <summary>Applies a schedule extension.</summary>
    public Task<OperationResult<CampaignPlayDetail>> HandleAsync(
        ExtendCampaignScheduleCommand command,
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
            (state, map, campaign, utcNow) =>
            {
                var membership = CampaignMapper.MembershipFor(campaign, command.UserId);
                if (membership?.IsGameMaster != true)
                {
                    return PlayMutation.Fail(new Domain.Common.DomainError(
                        ErrorCodes.CampaignForbidden,
                        "Only a campaign manager can extend the schedule."));
                }

                var extensions = new List<PhaseExtension>();
                foreach (var item in command.Extensions)
                {
                    if (!Enum.TryParse<DurationUnit>(item.DurationUnit, ignoreCase: true, out var unit) || !Enum.IsDefined(unit))
                    {
                        return PlayMutation.Fail(new Domain.Common.DomainError(
                            "schedule.duration.invalid",
                            "Choose minutes, hours, days, weeks, or months.",
                            "durationUnit"));
                    }

                    extensions.Add(new PhaseExtension(item.WindowId, new ScheduleDuration(item.DurationAmount, unit)));
                }

                if (!CampaignPlayRules.TryExtendSchedule(
                    state,
                    CampaignMapper.ToSchedule(campaign),
                    command.RoundCount,
                    extensions,
                    utcNow,
                    command.UserId,
                    out var outcome,
                    out var error))
                {
                    return PlayMutation.Fail(error);
                }

                return PlayMutation.FromOutcome(outcome!);
            },
            cancellationToken,
            _notifications);
    }
}

/// <summary>
/// Injects an ephemeral GM ringer battle against an idle player force.
/// </summary>
public sealed class InjectRingerBattleHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;
    private readonly IUserAccountStore _accounts;
    private readonly CampaignNotificationPublisher? _notifications;

    /// <summary>Initializes a new handler.</summary>
    public InjectRingerBattleHandler(ICampaignStore campaigns, IClock clock, IUserAccountStore accounts, CampaignNotificationPublisher? notifications = null)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(accounts);
        _campaigns = campaigns;
        _clock = clock;
        _accounts = accounts;
        _notifications = notifications;
    }

    /// <summary>Starts the ringer fight.</summary>
    public Task<OperationResult<CampaignPlayDetail>> HandleAsync(
        InjectRingerBattleCommand command,
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
            (state, map, campaign, utcNow) =>
            {
                var membership = CampaignMapper.MembershipFor(campaign, command.UserId);
                if (membership?.IsGameMaster != true && !command.IsAdministrator)
                {
                    return PlayMutation.Fail(new Domain.Common.DomainError(
                        ErrorCodes.CampaignForbidden,
                        "Only a campaign manager or administrator can inject a ringer battle."));
                }

                if (campaign.Factions.All(faction => faction.Id != command.RingerFactionId))
                {
                    return PlayMutation.Fail(new Domain.Common.DomainError(
                        "ringer.faction.invalid",
                        "Choose a campaign faction for the ringer.",
                        "ringerFactionId"));
                }

                if (!CampaignPlayRules.TryInjectRingerBattle(
                    state,
                    map,
                    command.UserId,
                    command.TargetForceId,
                    command.RingerFactionId,
                    command.MissionId,
                    command.PlayerIsDefender,
                    CampaignPlayCatalog.TerrainSetups(campaign),
                    CampaignPlayCatalog.StructureSetups(campaign),
                    CampaignPlayPipeline.AllyGroups(campaign),
                    utcNow,
                    CampaignPlayCatalog.PickIndex,
                    out var outcome,
                    out var error))
                {
                    return PlayMutation.Fail(error);
                }

                return PlayMutation.FromOutcome(outcome!);
            },
            cancellationToken,
            _notifications);
    }
}

/// <summary>
/// Assigns a faction to a player who has not chosen one yet.
/// </summary>
public sealed class ChooseFactionHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;
    private readonly IUserAccountStore _accounts;
    private readonly CampaignNotificationPublisher? _notifications;

    /// <summary>Initializes a new handler.</summary>
    public ChooseFactionHandler(ICampaignStore campaigns, IClock clock, IUserAccountStore accounts, CampaignNotificationPublisher? notifications = null)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(accounts);
        _campaigns = campaigns;
        _clock = clock;
        _accounts = accounts;
        _notifications = notifications;
    }

    /// <summary>Records or changes the faction choice before launch, and seeds a force when the campaign is in progress.</summary>
    public async Task<OperationResult<CampaignPlayDetail>> HandleAsync(
        ChooseFactionCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var existing = await _campaigns.FindByIdAsync(command.CampaignId, cancellationToken).ConfigureAwait(false);
        var membership = existing is null ? null : CampaignMapper.MembershipFor(existing, command.UserId);
        if (existing is null || membership is null || !membership.IsPlayer)
        {
            return OperationResults.Failure<CampaignPlayDetail>(ErrorCodes.CampaignNotFound, "The campaign was not found.");
        }

        if (membership.FactionId is not null && CampaignLifecycle.HasLaunched(existing, _clock.UtcNow))
        {
            return OperationResults.Failure<CampaignPlayDetail>("faction.already_chosen", "Your faction cannot be changed.");
        }

        var faction = existing.Factions.FirstOrDefault(item => item.Id == command.FactionId);
        if (faction is null)
        {
            return OperationResults.Failure<CampaignPlayDetail>("faction.invalid", "Choose a campaign faction.");
        }

        if (faction.RequiresSubfaction
            && (string.IsNullOrWhiteSpace(command.Subfaction)
                || !faction.Subfactions.Contains(command.Subfaction, StringComparer.Ordinal)))
        {
            return OperationResults.Failure<CampaignPlayDetail>("faction.subfaction.required", "Choose a subfaction.");
        }

        var memberships = existing.Memberships.Select(member =>
            member.UserId == command.UserId
                ? new StoredCampaignMembership
                {
                    UserId = member.UserId,
                    IsGameMaster = member.IsGameMaster,
                    IsPlayer = member.IsPlayer,
                    FactionId = command.FactionId,
                    Subfaction = string.IsNullOrWhiteSpace(command.Subfaction) ? null : command.Subfaction.Trim(),
                }
                : member).ToArray();
        var updated = CampaignMapClone.CloneWithMemberships(existing, memberships, _clock.UtcNow);
        if (CampaignLifecycle.HasLaunched(updated, _clock.UtcNow) && updated.PlayState is not null)
        {
            var map = CampaignLifecycle.ToPlayMap(updated);
            var ensured = CampaignPlayRules.EnsureForce(updated.PlayState, map, command.UserId, command.FactionId);
            var withObjective = PrivateObjectiveRules.EnsurePlayerAssignment(
                ensured.State,
                CampaignPlayCatalog.PrivateTypes(updated),
                command.UserId,
                _clock.UtcNow,
                CampaignPlayCatalog.PickIndex);
            updated = CloneWithPlay(
                updated,
                withObjective,
                ensured.PreserveMap ? updated.MapGraph : CampaignLifecycle.ApplyOwnership(updated.MapGraph!, ensured.Map));
        }

        var outcome = await _campaigns.UpdateAsync(updated, command.ExpectedRevision, cancellationToken).ConfigureAwait(false);
        if (!outcome.IsSuccess || outcome.Campaign is null)
        {
            return OperationResults.Failure<CampaignPlayDetail>(
                outcome.ErrorCode ?? ErrorCodes.CampaignNotFound,
                outcome.Message ?? "The faction could not be saved.");
        }

        var play = await new GetCampaignPlayHandler(_campaigns, _clock, _accounts, _notifications).HandleAsync(
            command.CampaignId,
            command.UserId,
            false,
            cancellationToken).ConfigureAwait(false);
        if (play.IsSuccess)
        {
            return play;
        }

        return OperationResults.Success(
            await CampaignPlayMapper.ToDetailAsync(outcome.Campaign, command.UserId, _clock.UtcNow, _accounts, cancellationToken)
                .ConfigureAwait(false));
    }

    private static StoredCampaign CloneWithPlay(StoredCampaign existing, CampaignPlayState play, StoredMapGraph? graph)
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
            UpdatedUtc = existing.UpdatedUtc,
            CreatedByUserId = existing.CreatedByUserId,
            Memberships = existing.Memberships,
            Factions = existing.Factions,
            AllyGroups = existing.AllyGroups,
            Links = existing.Links,
            TimeZoneId = existing.TimeZoneId,
            StartsUtc = existing.StartsUtc,
            EndsUtc = existing.EndsUtc,
            RoundCount = existing.RoundCount,
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
            PlayState = play,
        };
    }
}

/// <summary>
/// Starts a manager debug session.
/// </summary>
public sealed class EnterCampaignDebugHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;
    private readonly IUserAccountStore _accounts;
    private readonly CampaignNotificationPublisher? _notifications;

    /// <summary>Initializes a new handler.</summary>
    public EnterCampaignDebugHandler(ICampaignStore campaigns, IClock clock, IUserAccountStore accounts, CampaignNotificationPublisher? notifications = null)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(accounts);
        _campaigns = campaigns;
        _clock = clock;
        _accounts = accounts;
        _notifications = notifications;
    }

    /// <summary>Enters debug mode.</summary>
    public Task<OperationResult<CampaignPlayDetail>> HandleAsync(PlayCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return CampaignDebugAccess.MutateDebugAsync(
            _campaigns,
            _clock,
            _accounts,
            command,
            (state, _, utcNow) =>
            {
                if (!CampaignPlayRules.TryEnterDebug(state, command.UserId, utcNow, out var next, out var error))
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
/// Ends a manager debug session.
/// </summary>
public sealed class ExitCampaignDebugHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;
    private readonly IUserAccountStore _accounts;
    private readonly CampaignNotificationPublisher? _notifications;

    /// <summary>Initializes a new handler.</summary>
    public ExitCampaignDebugHandler(ICampaignStore campaigns, IClock clock, IUserAccountStore accounts, CampaignNotificationPublisher? notifications = null)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(accounts);
        _campaigns = campaigns;
        _clock = clock;
        _accounts = accounts;
        _notifications = notifications;
    }

    /// <summary>Exits debug mode.</summary>
    public Task<OperationResult<CampaignPlayDetail>> HandleAsync(PlayCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return CampaignDebugAccess.MutateDebugAsync(
            _campaigns,
            _clock,
            _accounts,
            command,
            (state, _, utcNow) =>
            {
                if (!CampaignPlayRules.TryExitDebug(state, command.UserId, utcNow, out var next, out var error))
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
/// Corrects a force order while in debug mode, re-resolving when the prior action window is already closed.
/// </summary>
public sealed class DebugCorrectOrderHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;
    private readonly IUserAccountStore _accounts;
    private readonly CampaignNotificationPublisher? _notifications;

    /// <summary>Initializes a new handler.</summary>
    public DebugCorrectOrderHandler(ICampaignStore campaigns, IClock clock, IUserAccountStore accounts, CampaignNotificationPublisher? notifications = null)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(accounts);
        _campaigns = campaigns;
        _clock = clock;
        _accounts = accounts;
        _notifications = notifications;
    }

    /// <summary>Applies a debug order correction.</summary>
    public async Task<OperationResult<CampaignPlayDetail>> HandleAsync(
        SaveOrderDraftCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!Enum.TryParse<ActionKind>(command.Kind, ignoreCase: true, out var kind) || !Enum.IsDefined(kind))
        {
            return OperationResults.Failure<CampaignPlayDetail>("order.kind.invalid", "Choose a valid action.");
        }

        return await CampaignDebugAccess.MutateDebugAsync(
            _campaigns,
            _clock,
            _accounts,
            new PlayCommand
            {
                UserId = command.UserId,
                IsAdministrator = command.IsAdministrator,
                CampaignId = command.CampaignId,
                ExpectedRevision = command.ExpectedRevision,
            },
            (state, map, utcNow, campaign) =>
            {
                if (!CampaignPlayRules.TryDebugCorrectOrder(
                    state,
                    command.UserId,
                    command.ForceId,
                    kind,
                    command.TargetTerritoryId,
                    command.StructureTypeId,
                    map,
                    CampaignPlayPipeline.AllyGroups(campaign),
                    campaign.StructureTypes.Select(static type => type.Id).ToHashSet(),
                    utcNow,
                    out var outcome,
                    out var error,
                    CampaignPlayCatalog.TerrainSetups(campaign),
                    CampaignPlayCatalog.StructureSetups(campaign),
                    CampaignPlayCatalog.PickIndex,
                    CampaignMapper.ToSchedule(campaign),
                    command.ReResolvePrevious))
                {
                    return PlayMutation.Fail(error);
                }

                return PlayMutation.FromOutcome(outcome!);
            },
            cancellationToken,
            _notifications).ConfigureAwait(false);
    }
}

/// <summary>
/// Reveals hidden item objectives while a manager or administrator is in debug mode.
/// </summary>
public sealed class RevealHiddenItemObjectivesHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;
    private readonly IUserAccountStore _accounts;
    private readonly CampaignNotificationPublisher? _notifications;

    /// <summary>Initializes a new handler.</summary>
    public RevealHiddenItemObjectivesHandler(ICampaignStore campaigns, IClock clock, IUserAccountStore accounts, CampaignNotificationPublisher? notifications = null)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(accounts);
        _campaigns = campaigns;
        _clock = clock;
        _accounts = accounts;
        _notifications = notifications;
    }

    /// <summary>Reveals hidden item objectives to all players.</summary>
    public Task<OperationResult<CampaignPlayDetail>> HandleAsync(PlayCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return CampaignDebugAccess.MutateDebugAsync(
            _campaigns,
            _clock,
            _accounts,
            command,
            (state, _, utcNow) =>
            {
                if (!ItemObjectiveRules.TryRevealHidden(state, command.UserId, utcNow, out var next, out var error)
                    || next is null)
                {
                    return PlayMutation.Fail(error ?? new Domain.Common.DomainError(
                        "debug.required",
                        "Enter debug mode before revealing hidden item objectives."));
                }

                return PlayMutation.Ok(next, new PlayMap([], []), preserveMap: true);
            },
            cancellationToken,
            _notifications);
    }
}

/// <summary>
/// Awards or revokes a public campaign objective for a player. Managers may do this without debug mode.
/// </summary>
public sealed class SetPublicObjectiveAwardHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;
    private readonly IUserAccountStore _accounts;
    private readonly CampaignNotificationPublisher? _notifications;

    /// <summary>Initializes a new handler.</summary>
    public SetPublicObjectiveAwardHandler(
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

    /// <summary>Records an award or revocation.</summary>
    public Task<OperationResult<CampaignPlayDetail>> HandleAsync(
        SetPublicObjectiveAwardCommand command,
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
                var membership = CampaignMapper.MembershipFor(campaign, command.UserId);
                if (membership?.IsGameMaster != true && !command.IsAdministrator)
                {
                    return PlayMutation.Fail(new Domain.Common.DomainError(
                        ErrorCodes.CampaignForbidden,
                        "Only a campaign manager can award public objectives."));
                }

                var objectiveIds = campaign.PublicObjectiveTypes
                    .Where(static type => type.CampaignPoints > 0)
                    .Select(static type => type.Id)
                    .ToHashSet();
                var playerIds = campaign.Memberships
                    .Where(static member => member.IsPlayer)
                    .Select(static member => member.UserId)
                    .ToHashSet();
                CampaignPlayState? next;
                Domain.Common.DomainError? error;
                if (command.Awarded)
                {
                    if (!PublicObjectiveAwardRules.TryAward(
                            state,
                            command.ObjectiveId,
                            command.PlayerUserId,
                            command.UserId,
                            utcNow,
                            objectiveIds,
                            playerIds,
                            out next,
                            out error)
                        || next is null)
                    {
                        return PlayMutation.Fail(error);
                    }
                }
                else if (!PublicObjectiveAwardRules.TryRevoke(
                        state,
                        command.ObjectiveId,
                        command.PlayerUserId,
                        command.UserId,
                        utcNow,
                        out next,
                        out error)
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
/// Grants a still-available private objective to a player, faction, or ally group.
/// </summary>
public sealed class GrantPrivateObjectiveHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;
    private readonly IUserAccountStore _accounts;
    private readonly CampaignNotificationPublisher? _notifications;

    /// <summary>Initializes a new handler.</summary>
    public GrantPrivateObjectiveHandler(
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

    /// <summary>Grants a specific or random still-available private objective.</summary>
    public Task<OperationResult<CampaignPlayDetail>> HandleAsync(
        GrantPrivateObjectiveCommand command,
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
                var membership = CampaignMapper.MembershipFor(campaign, command.UserId);
                if (membership?.IsGameMaster != true && !command.IsAdministrator)
                {
                    return PlayMutation.Fail(new Domain.Common.DomainError(
                        ErrorCodes.CampaignForbidden,
                        "Only a campaign manager can grant private objectives."));
                }

                if (!Enum.TryParse<PrivateObjectiveHolderKind>(command.HolderKind, true, out var holderKind))
                {
                    return PlayMutation.Fail(new Domain.Common.DomainError(
                        "privateObjective.holder.invalid",
                        "Choose a player, faction, or ally group.",
                        "holderKind"));
                }

                if (!PrivateObjectiveRules.TryGrant(
                        state,
                        CampaignPlayCatalog.PrivateTypes(campaign),
                        holderKind,
                        command.HolderId,
                        command.TypeId,
                        utcNow,
                        CampaignPlayCatalog.PickIndex,
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
/// Submits a manual private-objective claim for manager approval.
/// </summary>
public sealed class ClaimPrivateObjectiveHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;
    private readonly IUserAccountStore _accounts;
    private readonly CampaignNotificationPublisher? _notifications;

    /// <summary>Initializes a new handler.</summary>
    public ClaimPrivateObjectiveHandler(
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

    /// <summary>Claims a held manual private objective.</summary>
    public Task<OperationResult<CampaignPlayDetail>> HandleAsync(
        ClaimPrivateObjectiveCommand command,
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
                var assignment = state.PrivateObjectives.FirstOrDefault(item => item.Id == command.AssignmentId);
                var membership = CampaignMapper.MembershipFor(campaign, command.UserId);
                var allyGroupId = membership?.FactionId is { } factionId
                    ? CampaignPlayCatalog.AllyGroupByFaction(campaign).GetValueOrDefault(factionId)
                    : null;
                if (assignment is null
                    || !PrivateObjectiveRules.CanViewDetails(
                        assignment,
                        command.UserId,
                        membership?.FactionId,
                        allyGroupId,
                        staffView: false,
                        campaignCompleted: false)
                    || (assignment.HolderKind == PrivateObjectiveHolderKind.Player && assignment.HolderId != command.UserId)
                    || (assignment.HolderKind == PrivateObjectiveHolderKind.Faction && assignment.HolderId != membership?.FactionId)
                    || (assignment.HolderKind == PrivateObjectiveHolderKind.AllyGroup && assignment.HolderId != allyGroupId))
                {
                    return PlayMutation.Fail(new Domain.Common.DomainError(
                        "privateObjective.forbidden",
                        "Only a holder of that private objective can claim it."));
                }

                if (!PrivateObjectiveRules.TryClaim(state, command.AssignmentId, command.UserId, utcNow, out var next, out var error)
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
/// Approves or denies a claimed private objective.
/// </summary>
public sealed class ModeratePrivateObjectiveHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;
    private readonly IUserAccountStore _accounts;
    private readonly CampaignNotificationPublisher? _notifications;

    /// <summary>Initializes a new handler.</summary>
    public ModeratePrivateObjectiveHandler(
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

    /// <summary>Approves or denies a claim.</summary>
    public Task<OperationResult<CampaignPlayDetail>> HandleAsync(
        ModeratePrivateObjectiveCommand command,
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
                var membership = CampaignMapper.MembershipFor(campaign, command.UserId);
                if (membership?.IsGameMaster != true && !command.IsAdministrator)
                {
                    return PlayMutation.Fail(new Domain.Common.DomainError(
                        ErrorCodes.CampaignForbidden,
                        "Only a campaign manager can approve private objectives."));
                }

                CampaignPlayState? next;
                Domain.Common.DomainError? error;
                if (command.Approved)
                {
                    if (!PrivateObjectiveRules.TryApprove(
                            state,
                            command.AssignmentId,
                            command.UserId,
                            utcNow,
                            CampaignPlayCatalog.PrivateNames(campaign),
                            out next,
                            out error)
                        || next is null)
                    {
                        return PlayMutation.Fail(error);
                    }
                }
                else if (!PrivateObjectiveRules.TryDeny(state, command.AssignmentId, out next, out error) || next is null)
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
/// Resolves a configured holder choice on a possessed item objective.
/// </summary>
public sealed class ResolveItemObjectiveChoiceHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;
    private readonly IUserAccountStore _accounts;
    private readonly CampaignNotificationPublisher? _notifications;

    /// <summary>Initializes a new handler.</summary>
    public ResolveItemObjectiveChoiceHandler(
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

    /// <summary>Applies one configured item choice.</summary>
    public Task<OperationResult<CampaignPlayDetail>> HandleAsync(
        ResolveItemObjectiveChoiceCommand command,
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
                if (!ItemObjectiveChoiceRules.TryResolve(
                        state,
                        command.ItemId,
                        command.ChoiceId,
                        command.UserId,
                        CampaignPlayCatalog.ItemSetups(campaign),
                        utcNow,
                        CampaignPlayCatalog.PickIndex,
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

internal static class BattleScoreRequirements
{
    public static bool TryRequire(
        StoredCampaign campaign,
        SubmitBattleResultCommand command,
        [NotNullWhen(false)] out Domain.Common.DomainError? error)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        ArgumentNullException.ThrowIfNull(command);
        error = null;
        if (command.Reports is { Count: > 0 } || !campaign.BattleScoring.UseDifferential || command.IsDraw)
        {
            return true;
        }

        if (command.WinnerScore is null || command.LoserScore is null)
        {
            error = new Domain.Common.DomainError(
                "battle.score.required",
                "Differential scoring requires both the winner and loser scores.",
                "winnerScore");
            return false;
        }

        return true;
    }
}

internal static class CampaignDebugAccess
{
    public static Task<OperationResult<CampaignPlayDetail>> MutateDebugAsync(
        ICampaignStore campaigns,
        IClock clock,
        IUserAccountStore accounts,
        PlayCommand command,
        Func<CampaignPlayState, PlayMap, DateTimeOffset, PlayMutation> mutate,
        CancellationToken cancellationToken,
        CampaignNotificationPublisher? notifications = null)
    {
        return MutateDebugAsync(
            campaigns,
            clock,
            accounts,
            command,
            (state, map, utcNow, _) => mutate(state, map, utcNow),
            cancellationToken,
            notifications);
    }

    public static Task<OperationResult<CampaignPlayDetail>> MutateDebugAsync(
        ICampaignStore campaigns,
        IClock clock,
        IUserAccountStore accounts,
        PlayCommand command,
        Func<CampaignPlayState, PlayMap, DateTimeOffset, StoredCampaign, PlayMutation> mutate,
        CancellationToken cancellationToken,
        CampaignNotificationPublisher? notifications = null)
    {
        return CampaignPlayPipeline.MutateAsync(
            campaigns,
            clock,
            accounts,
            command.CampaignId,
            command.UserId,
            command.IsAdministrator,
            command.ExpectedRevision,
            (state, map, campaign, utcNow) =>
            {
                var membership = CampaignMapper.MembershipFor(campaign, command.UserId);
                if (membership?.IsGameMaster != true && !command.IsAdministrator)
                {
                    return PlayMutation.Fail(new Domain.Common.DomainError(
                        ErrorCodes.CampaignForbidden,
                        "Only a campaign manager or administrator can use debug mode."));
                }

                return mutate(state, map, utcNow, campaign);
            },
            cancellationToken,
            notifications);
    }
}

/// <summary>
/// Assigns a faction and optional subfaction to another player. The player may still change it before launch.
/// </summary>
public sealed class AssignPlayerFactionHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;
    private readonly IUserAccountStore _accounts;
    private readonly CampaignNotificationPublisher? _notifications;

    /// <summary>Initializes a new handler.</summary>
    public AssignPlayerFactionHandler(
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

    /// <summary>Sets another player's faction, including after launch for fixes and testing.</summary>
    public async Task<OperationResult<CampaignPlayDetail>> HandleAsync(
        AssignPlayerFactionCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var existing = await _campaigns.FindByIdAsync(command.CampaignId, cancellationToken).ConfigureAwait(false);
        if (existing is null || !CampaignAccess.CanView(existing, command.UserId, command.IsAdministrator))
        {
            return OperationResults.Failure<CampaignPlayDetail>(ErrorCodes.CampaignNotFound, "The campaign was not found.");
        }

        if (!CampaignAccess.CanStaffMembers(existing, command.UserId, command.IsAdministrator))
        {
            return OperationResults.Failure<CampaignPlayDetail>(
                ErrorCodes.CampaignForbidden,
                "Only a campaign manager or administrator can assign a faction.");
        }

        var target = CampaignMapper.MembershipFor(existing, command.TargetUserId);
        if (target is null || !target.IsPlayer)
        {
            return OperationResults.Failure<CampaignPlayDetail>(
                ErrorCodes.CampaignMemberNotFound,
                "That player is not in this campaign.");
        }

        var faction = existing.Factions.FirstOrDefault(item => item.Id == command.FactionId);
        if (faction is null)
        {
            return OperationResults.Failure<CampaignPlayDetail>("faction.invalid", "Choose a campaign faction.");
        }

        if (faction.RequiresSubfaction
            && (string.IsNullOrWhiteSpace(command.Subfaction)
                || !faction.Subfactions.Contains(command.Subfaction, StringComparer.Ordinal)))
        {
            return OperationResults.Failure<CampaignPlayDetail>("faction.subfaction.required", "Choose a subfaction.");
        }

        var memberships = existing.Memberships.Select(member =>
            member.UserId == command.TargetUserId
                ? new StoredCampaignMembership
                {
                    UserId = member.UserId,
                    IsGameMaster = member.IsGameMaster,
                    IsPlayer = member.IsPlayer,
                    FactionId = command.FactionId,
                    Subfaction = string.IsNullOrWhiteSpace(command.Subfaction) ? null : command.Subfaction.Trim(),
                }
                : member).ToArray();
        var updated = CampaignMapClone.CloneWithMemberships(existing, memberships, _clock.UtcNow);
        if (CampaignLifecycle.HasLaunched(updated, _clock.UtcNow) && updated.PlayState is not null)
        {
            var map = CampaignLifecycle.ToPlayMap(updated);
            var play = CampaignPlayRules.ReassignControllerFaction(updated.PlayState, command.TargetUserId, command.FactionId);
            var ensured = CampaignPlayRules.EnsureForce(play, map, command.TargetUserId, command.FactionId);
            var withObjective = PrivateObjectiveRules.EnsurePlayerAssignment(
                ensured.State,
                CampaignPlayCatalog.PrivateTypes(updated),
                command.TargetUserId,
                _clock.UtcNow,
                CampaignPlayCatalog.PickIndex);
            updated = CampaignMapClone.WithPlay(
                updated,
                withObjective,
                ensured.PreserveMap ? updated.MapGraph : CampaignLifecycle.ApplyOwnership(updated.MapGraph!, ensured.Map));
        }

        var outcome = await _campaigns.UpdateAsync(updated, command.ExpectedRevision, cancellationToken).ConfigureAwait(false);
        if (!outcome.IsSuccess || outcome.Campaign is null)
        {
            return OperationResults.Failure<CampaignPlayDetail>(
                outcome.ErrorCode ?? ErrorCodes.CampaignNotFound,
                outcome.Message ?? "The faction could not be assigned.");
        }

        var playDetail = await new GetCampaignPlayHandler(_campaigns, _clock, _accounts, _notifications)
            .HandleAsync(command.CampaignId, command.UserId, command.IsAdministrator, cancellationToken)
            .ConfigureAwait(false);
        if (playDetail.IsSuccess)
        {
            return playDetail;
        }

        return OperationResults.Success(
            await CampaignPlayMapper.ToDetailAsync(
                outcome.Campaign,
                command.UserId,
                _clock.UtcNow,
                _accounts,
                cancellationToken,
                command.IsAdministrator)
                .ConfigureAwait(false));
    }
}
