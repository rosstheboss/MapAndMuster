using Campaign.Application.Campaigns;
using Campaign.Application.Common;
using Campaign.Application.Maps;
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

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    public GetCampaignPlayHandler(ICampaignStore campaigns, IClock clock, IUserAccountStore accounts)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(accounts);
        _campaigns = campaigns;
        _clock = clock;
        _accounts = accounts;
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

    /// <summary>Initializes a new handler.</summary>
    public SaveOrderDraftHandler(ICampaignStore campaigns, IClock clock, IUserAccountStore accounts)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(accounts);
        _campaigns = campaigns;
        _clock = clock;
        _accounts = accounts;
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
            cancellationToken).ConfigureAwait(false);
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

    /// <summary>Initializes a new handler.</summary>
    public CommitOrdersHandler(ICampaignStore campaigns, IClock clock, IUserAccountStore accounts)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(accounts);
        _campaigns = campaigns;
        _clock = clock;
        _accounts = accounts;
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
                    out var error))
                {
                    return PlayMutation.Fail(error);
                }

                return PlayMutation.FromOutcome(outcome!);
            },
            cancellationToken);
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

    /// <summary>Initializes a new handler.</summary>
    public UncommitOrdersHandler(ICampaignStore campaigns, IClock clock, IUserAccountStore accounts)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(accounts);
        _campaigns = campaigns;
        _clock = clock;
        _accounts = accounts;
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
            cancellationToken);
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

    /// <summary>Initializes a new handler.</summary>
    public SubmitBattleResultHandler(
        ICampaignStore campaigns,
        IClock clock,
        IEmailOutbox outbox,
        IUserAccountStore accounts)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(outbox);
        ArgumentNullException.ThrowIfNull(accounts);
        _campaigns = campaigns;
        _clock = clock;
        _outbox = outbox;
        _accounts = accounts;
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
            (state, map, _, utcNow) =>
            {
                if (!CampaignPlayRules.TrySubmitBattleResult(
                    state,
                    command.UserId,
                    command.BattleId,
                    command.WinnerForceId,
                    command.IsDraw,
                    utcNow,
                    out var outcome,
                    out var error))
                {
                    return PlayMutation.Fail(error);
                }

                return PlayMutation.FromOutcome(outcome!);
            },
            cancellationToken).ConfigureAwait(false);
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

    /// <summary>Initializes a new handler.</summary>
    public AcceptBattleResultHandler(ICampaignStore campaigns, IClock clock, IUserAccountStore accounts)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(accounts);
        _campaigns = campaigns;
        _clock = clock;
        _accounts = accounts;
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
            (state, map, _, utcNow) =>
            {
                if (!CampaignPlayRules.TryAcceptBattleResult(
                    state,
                    command.UserId,
                    command.BattleId,
                    utcNow,
                    out var outcome,
                    out var error))
                {
                    return PlayMutation.Fail(error);
                }

                return PlayMutation.FromOutcome(outcome!);
            },
            cancellationToken);
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

    /// <summary>Initializes a new handler.</summary>
    public ResolveBattleHandler(ICampaignStore campaigns, IClock clock, IUserAccountStore accounts)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(accounts);
        _campaigns = campaigns;
        _clock = clock;
        _accounts = accounts;
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

                if (!CampaignPlayRules.TryResolveBattle(
                    state,
                    command.UserId,
                    command.BattleId,
                    command.WinnerForceId,
                    command.IsDraw,
                    utcNow,
                    out var next,
                    out var error))
                {
                    return PlayMutation.Fail(error);
                }

                return PlayMutation.Ok(next, map, preserveMap: true);
            },
            cancellationToken);
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

    /// <summary>Initializes a new handler.</summary>
    public SubmitRetreatHandler(ICampaignStore campaigns, IClock clock, IUserAccountStore accounts)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(accounts);
        _campaigns = campaigns;
        _clock = clock;
        _accounts = accounts;
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
            (state, map, _, utcNow) =>
            {
                if (!CampaignPlayRules.TrySubmitRetreat(
                    state,
                    map,
                    command.UserId,
                    command.BattleId,
                    command.TargetTerritoryId,
                    utcNow,
                    out var outcome,
                    out var error))
                {
                    return PlayMutation.Fail(error);
                }

                return PlayMutation.FromOutcome(outcome!);
            },
            cancellationToken);
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

    /// <summary>Initializes a new handler.</summary>
    public ExtendCampaignScheduleHandler(ICampaignStore campaigns, IClock clock, IUserAccountStore accounts)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(accounts);
        _campaigns = campaigns;
        _clock = clock;
        _accounts = accounts;
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
            cancellationToken);
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

    /// <summary>Initializes a new handler.</summary>
    public ChooseFactionHandler(ICampaignStore campaigns, IClock clock, IUserAccountStore accounts)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(accounts);
        _campaigns = campaigns;
        _clock = clock;
        _accounts = accounts;
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
            updated = CloneWithPlay(updated, ensured.State, ensured.PreserveMap ? updated.MapGraph : CampaignLifecycle.ApplyOwnership(updated.MapGraph!, ensured.Map));
        }

        var outcome = await _campaigns.UpdateAsync(updated, command.ExpectedRevision, cancellationToken).ConfigureAwait(false);
        if (!outcome.IsSuccess || outcome.Campaign is null)
        {
            return OperationResults.Failure<CampaignPlayDetail>(
                outcome.ErrorCode ?? ErrorCodes.CampaignNotFound,
                outcome.Message ?? "The faction could not be saved.");
        }

        var play = await new GetCampaignPlayHandler(_campaigns, _clock, _accounts).HandleAsync(
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

    /// <summary>Initializes a new handler.</summary>
    public EnterCampaignDebugHandler(ICampaignStore campaigns, IClock clock, IUserAccountStore accounts)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(accounts);
        _campaigns = campaigns;
        _clock = clock;
        _accounts = accounts;
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
            cancellationToken);
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

    /// <summary>Initializes a new handler.</summary>
    public ExitCampaignDebugHandler(ICampaignStore campaigns, IClock clock, IUserAccountStore accounts)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(accounts);
        _campaigns = campaigns;
        _clock = clock;
        _accounts = accounts;
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
            cancellationToken);
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

    /// <summary>Initializes a new handler.</summary>
    public DebugCorrectOrderHandler(ICampaignStore campaigns, IClock clock, IUserAccountStore accounts)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(accounts);
        _campaigns = campaigns;
        _clock = clock;
        _accounts = accounts;
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
                    out var error))
                {
                    return PlayMutation.Fail(error);
                }

                return PlayMutation.FromOutcome(outcome!);
            },
            cancellationToken).ConfigureAwait(false);
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
        CancellationToken cancellationToken)
    {
        return CampaignDebugAccess.MutateDebugAsync(
            campaigns,
            clock,
            accounts,
            command,
            (state, map, utcNow, _) => mutate(state, map, utcNow),
            cancellationToken);
    }

    public static Task<OperationResult<CampaignPlayDetail>> MutateDebugAsync(
        ICampaignStore campaigns,
        IClock clock,
        IUserAccountStore accounts,
        PlayCommand command,
        Func<CampaignPlayState, PlayMap, DateTimeOffset, StoredCampaign, PlayMutation> mutate,
        CancellationToken cancellationToken)
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
            cancellationToken);
    }
}
