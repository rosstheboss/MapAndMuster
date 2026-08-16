using Campaign.Application.Campaigns;
using Campaign.Application.Identity;
using Campaign.Application.Ports;
using Campaign.Domain.Campaigns;
using Campaign.Domain.Play;

namespace Campaign.Application.Play;

internal static class CampaignPlayMapper
{
    public static async Task<CampaignPlayDetail> ToDetailAsync(
        StoredCampaign campaign,
        Guid viewerUserId,
        DateTimeOffset utcNow,
        IUserAccountStore? accounts,
        CancellationToken cancellationToken)
    {
        var membership = CampaignMapper.MembershipFor(campaign, viewerUserId);
        var progress = CampaignLifecycle.Progress(campaign, utcNow);
        var play = campaign.PlayState ?? CampaignPlayState.Empty;
        var map = CampaignLifecycle.ToPlayMap(campaign);
        var window = play.CurrentWindow();
        var names = await UsernamesAsync(campaign, accounts, cancellationToken).ConfigureAwait(false);
        var mentionable = accounts is null
            ? (IReadOnlyList<CampaignLogMemberDetail>)[]
            : await ChatMembersAsync(campaign, accounts, cancellationToken).ConfigureAwait(false);
        var remaining = play.Windows
            .Where(item => item.Status != PhaseWindowStatus.Resolved)
            .Select(item => new PlayWindowDetail
            {
                Id = item.Id,
                RoundNumber = item.RoundNumber,
                PhaseNumber = item.PhaseNumber,
                Kind = item.Kind.ToString(),
                Label = CampaignPhaseLabels.Format(
                    CampaignMapper.ToSchedule(campaign).Phases,
                    item.PhaseNumber,
                    item.Kind),
                EndsUtc = item.EndsUtc,
            })
            .ToArray();
        var myForces = play.Forces.Where(force => force.ControllerUserId == viewerUserId).ToArray();
        var revealed = window is null || window.Status == PhaseWindowStatus.Resolved || window.Kind != RoundPhaseKind.Action;
        var currentActionId = window is { Kind: RoundPhaseKind.Action, Status: PhaseWindowStatus.Open } ? window.Id : (Guid?)null;
        var orders = new List<PlayOrderDetail>();
        if (currentActionId is { } actionId)
        {
            foreach (var force in myForces)
            {
                var submission = play.LatestSubmission(actionId, force.Id);
                if (submission is not null)
                {
                    orders.Add(new PlayOrderDetail
                    {
                        ForceId = force.Id,
                        Kind = submission.Kind.ToString(),
                        TargetTerritoryId = submission.TargetTerritoryId,
                        IsRevealed = false,
                    });
                }
            }
        }
        else if (window is not null)
        {
            foreach (var force in play.Forces)
            {
                var previous = play.Windows.LastOrDefault(item =>
                    item.Kind == RoundPhaseKind.Action && item.Status == PhaseWindowStatus.Resolved);
                if (previous is null)
                {
                    continue;
                }

                var submission = play.LatestSubmission(previous.Id, force.Id);
                if (submission is null)
                {
                    continue;
                }

                orders.Add(new PlayOrderDetail
                {
                    ForceId = force.Id,
                    Kind = submission.Kind.ToString(),
                    TargetTerritoryId = submission.TargetTerritoryId,
                    IsRevealed = true,
                });
            }
        }

        var commitments = currentActionId is { } commitWindow
            ? play.RequiredOrderPlayers(commitWindow)
                .Select(userId => new PlayCommitmentDetail
                {
                    UserId = userId,
                    Username = names.GetValueOrDefault(userId),
                    IsCommitted = play.Commitments.Any(item => item.WindowId == commitWindow && item.UserId == userId),
                })
                .ToArray()
            : [];

        var battles = play.Battles
            .Where(battle => window is not null && battle.BattleWindowId == window.Id)
            .Select(battle => ToBattle(play, map, battle, viewerUserId))
            .ToArray();

        _ = revealed;
        return new CampaignPlayDetail
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Revision = campaign.Revision,
            CanManage = membership?.IsGameMaster == true,
            IsParticipant = membership?.IsPlayer == true,
            CanChat = membership is not null,
            MentionableMembers = mentionable,
            Status = progress.Status.ToString(),
            CurrentRound = progress.CurrentRound,
            CurrentPhaseNumber = progress.CurrentPhaseNumber,
            CurrentPhaseKind = progress.CurrentPhaseKind?.ToString(),
            CurrentPhaseLabel = progress.CurrentPhaseKind is null || progress.CurrentPhaseNumber is null
                ? null
                : CampaignPhaseLabels.Format(
                    CampaignMapper.ToSchedule(campaign).Phases,
                    progress.CurrentPhaseNumber.Value,
                    progress.CurrentPhaseKind.Value),
            CurrentPhaseStartsUtc = progress.CurrentPhaseStartsUtc,
            CurrentPhaseEndsUtc = progress.CurrentPhaseEndsUtc,
            CurrentWindowId = window?.Id,
            HasMap = !string.IsNullOrWhiteSpace(campaign.MapStorageKey),
            FactionId = membership?.FactionId,
            CanChooseFaction = membership?.IsPlayer == true
                && membership.FactionId is null,
            IsCommitted = currentActionId is { } id
                && play.Commitments.Any(item => item.WindowId == id && item.UserId == viewerUserId),
            RoundCount = campaign.RoundCount,
            MinRoundCount = Math.Max(progress.CurrentRound ?? CampaignSetupRules.MinRoundCount, CampaignSetupRules.MinRoundCount),
            RemainingWindows = remaining,
            Factions = CampaignMapper.ToDetail(campaign, viewerUserId, utcNow).Factions,
            StructureTypes = CampaignMapper.ToDetail(campaign, viewerUserId, utcNow).StructureTypes,
            Forces =
            [
                .. play.Forces.Select(force => new PlayForceDetail
                {
                    Id = force.Id,
                    ControllerUserId = force.ControllerUserId,
                    ControllerUsername = names.GetValueOrDefault(force.ControllerUserId),
                    FactionId = force.FactionId,
                    TerritoryId = force.TerritoryId,
                    IsMine = force.ControllerUserId == viewerUserId,
                    InBattle = force.InBattle,
                    MoveTargets = force.ControllerUserId == viewerUserId
                        ? CampaignPlayRules.EligibleMoves(map, force)
                        : [],
                    AvailableActions = force.ControllerUserId == viewerUserId
                        ? [.. ActionResolution.EligibleActions(
                            play,
                            map,
                            force,
                            campaign.Factions.ToDictionary(static faction => faction.Id, static faction => faction.AllyGroupName),
                            campaign.StructureTypes.Count > 0).Select(static kind => kind.ToString())]
                        : [],
                }),
            ],
            MyDrafts = currentActionId is { } draftWindow
                ?
                [
                    .. play.Drafts
                        .Where(draft => draft.WindowId == draftWindow && myForces.Any(force => force.Id == draft.ForceId))
                        .Select(draft => new PlayDraftDetail
                        {
                            ForceId = draft.ForceId,
                            Kind = draft.Kind.ToString(),
                            TargetTerritoryId = draft.TargetTerritoryId,
                            StructureTypeId = draft.StructureTypeId,
                        }),
                ]
                : [],
            Orders = orders,
            Commitments = commitments,
            Battles = battles,
            Log =
            [
                .. play.Log
                    .OrderBy(static item => item.OccurredUtc)
                    .ThenBy(static item => item.Id)
                    .Select(item => ToLogEntry(item, campaign, map, play, names)),
            ],
            PlayersMissingFaction =
            [
                .. campaign.Memberships
                    .Where(member => member.IsPlayer && member.FactionId is null)
                    .Select(member => names.GetValueOrDefault(member.UserId) ?? member.UserId.ToString()),
            ],
        };
    }

    private static PlayBattleDetail ToBattle(
        CampaignPlayState play,
        PlayMap map,
        CampaignBattle battle,
        Guid viewerUserId)
    {
        var myForce = play.Forces.FirstOrDefault(force =>
            force.ControllerUserId == viewerUserId && battle.ParticipantForceIds.Contains(force.Id));
        var opponent = play.Forces.FirstOrDefault(force =>
            force.ControllerUserId != viewerUserId && battle.ParticipantForceIds.Contains(force.Id));
        var mine = myForce is null ? null : play.LatestBattleSubmission(battle.Id, viewerUserId);
        var theirs = opponent is null ? null : play.LatestBattleSubmission(battle.Id, opponent.ControllerUserId);
        var needsRetreat = myForce is not null
            && battle.Status is BattleStatus.Finalized or BattleStatus.GMResolved
            && !battle.IsDraw
            && battle.WinnerForceId != myForce.Id
            && !play.Retreats.Any(item => item.BattleId == battle.Id && item.ForceId == myForce.Id);
        return new PlayBattleDetail
        {
            Id = battle.Id,
            TerritoryId = battle.TerritoryId,
            Status = battle.Status.ToString(),
            ParticipantForceIds = battle.ParticipantForceIds,
            IsMine = myForce is not null,
            MySubmission = mine is null
                ? null
                : new PlayBattleSubmissionDetail
                {
                    SubmitterUserId = mine.SubmitterUserId,
                    WinnerForceId = mine.WinnerForceId,
                    IsDraw = mine.IsDraw,
                },
            OpponentSubmission = theirs is null || myForce is null
                ? null
                : new PlayBattleSubmissionDetail
                {
                    SubmitterUserId = theirs.SubmitterUserId,
                    WinnerForceId = theirs.WinnerForceId,
                    IsDraw = theirs.IsDraw,
                },
            WinnerForceId = battle.WinnerForceId,
            IsDraw = battle.IsDraw,
            NeedsRetreat = needsRetreat,
            RetreatTargets = needsRetreat && myForce is not null
                ? CampaignPlayRules.EligibleRetreats(map, myForce)
                : [],
        };
    }

    internal static IReadOnlyList<PlayLogEntryDetail> ToLogEntries(
        StoredCampaign campaign,
        IReadOnlyDictionary<Guid, string> names)
    {
        var play = campaign.PlayState ?? CampaignPlayState.Empty;
        var map = CampaignLifecycle.ToPlayMap(campaign);
        return
        [
            .. play.Log
                .OrderBy(static item => item.OccurredUtc)
                .ThenBy(static item => item.Id)
                .Select(item => ToLogEntry(item, campaign, map, play, names)),
        ];
    }

    internal static async Task<Dictionary<Guid, string>> UsernamesAsync(
        StoredCampaign campaign,
        IUserAccountStore? accounts,
        CancellationToken cancellationToken)
    {
        var names = new Dictionary<Guid, string>();
        if (accounts is null)
        {
            return names;
        }

        foreach (var userId in campaign.Memberships.Select(static member => member.UserId).Distinct())
        {
            var account = await accounts.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false);
            if (account is not null)
            {
                names[userId] = account.Username;
            }
        }

        return names;
    }

    internal static async Task<IReadOnlyList<CampaignLogMemberDetail>> ChatMembersAsync(
        StoredCampaign campaign,
        IUserAccountStore accounts,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        ArgumentNullException.ThrowIfNull(accounts);
        var members = new List<CampaignLogMemberDetail>();
        foreach (var membership in campaign.Memberships.OrderBy(static member => member.UserId))
        {
            var account = await accounts.FindByIdAsync(membership.UserId, cancellationToken).ConfigureAwait(false);
            if (account is null)
            {
                continue;
            }

            var profile = ProfileMapper.ToPublic(account);
            members.Add(new CampaignLogMemberDetail
            {
                UserId = membership.UserId,
                Username = profile.Username,
                DisplayName = profile.DisplayName,
            });
        }

        return members;
    }

    private static PlayLogEntryDetail ToLogEntry(
        PlayLogEntry entry,
        StoredCampaign campaign,
        PlayMap map,
        CampaignPlayState play,
        IReadOnlyDictionary<Guid, string> names)
    {
        return new PlayLogEntryDetail
        {
            Id = entry.Id,
            OccurredUtc = entry.OccurredUtc,
            Kind = entry.Kind.ToString(),
            Originator = entry.Kind == PlayLogKind.PlayerChat
                ? entry.ActorDisplayName ?? ActorName(entry.ActorUserId, names)
                : CampaignChatRules.CampaignOriginator,
            Summary = FormatLog(entry, campaign, map, play, names),
            TerritoryId = entry.TerritoryId,
            ForceId = entry.ForceId,
            BattleId = entry.BattleId,
            IsSystemAdjustment = entry.IsSystemAdjustment,
        };
    }

    private static string FormatLog(
        PlayLogEntry entry,
        StoredCampaign campaign,
        PlayMap map,
        CampaignPlayState play,
        IReadOnlyDictionary<Guid, string> names)
    {
        var actor = ActorName(entry.ActorUserId, names);
        var territory = TerritoryLabel(campaign, map, entry.TerritoryId);
        var target = TerritoryLabel(campaign, map, entry.TargetTerritoryId);
        var action = entry.ActionKind?.ToString() ?? "Hold";
        var participants = string.Join(
            " and ",
            entry.RelatedForceIds
                .Select(id => play.Forces.FirstOrDefault(force => force.Id == id))
                .OfType<CampaignForce>()
                .Select(force => ActorName(force.ControllerUserId, names))
                .Distinct());
        return entry.Kind switch
        {
            PlayLogKind.MissingOrderHold =>
                $"No order was submitted for {actor}; the force Held in {territory}.",
            PlayLogKind.DeadlineDraftSubmitted =>
                $"The deadline submitted {actor}'s latest {action} draft.",
            PlayLogKind.InvalidOrderHold =>
                $"{actor}'s submitted {action} was invalid and became Hold.",
            PlayLogKind.ConflictingBuildHold =>
                $"Competing structure actions in {territory} became Hold for {actor}.",
            PlayLogKind.ResolvedAction =>
                $"{actor} resolved {action} in {territory}"
                    + (entry.TargetTerritoryId is null || entry.TargetTerritoryId == entry.TerritoryId
                        ? "."
                        : $" toward {target}."),
            PlayLogKind.BattleCreated =>
                $"A battle started in {territory} between {participants}.",
            PlayLogKind.BattleFinalized =>
                entry.ForceId is { } winner
                    ? $"Battle in {territory} was finalized. Winner: {ForceController(play, winner, names)}."
                    : $"Battle in {territory} was finalized as a draw.",
            PlayLogKind.BattleDisputed =>
                $"Battle in {territory} is disputed because the submitted results conflict.",
            PlayLogKind.BattleGmResolved =>
                entry.ForceId is { } gmWinner
                    ? $"A manager overrode the battle result in {territory}. Winner: {ForceController(play, gmWinner, names)}."
                    : $"A manager overrode the battle result in {territory} as a draw.",
            PlayLogKind.PlayerRetreat =>
                $"{actor} retreated from {territory} to {target}.",
            PlayLogKind.DefaultRetreat =>
                $"A missing retreat for {actor} used the spawn fallback at {target}.",
            PlayLogKind.UnresolvedBattleHeldOpen =>
                $"Battle in {territory} stayed open for a manager because no results were submitted.",
            PlayLogKind.CampaignStarted =>
                "The campaign started.",
            PlayLogKind.ScheduleExtended =>
                actor == "A force"
                    ? "A manager lengthened remaining phases or added rounds."
                    : $"{actor} lengthened remaining phases or added rounds.",
            PlayLogKind.ForcesRejoined =>
                $"{actor}'s forces rejoined in {territory} and now share one action.",
            PlayLogKind.PlayerChat =>
                entry.Message ?? string.Empty,
            _ => $"{actor} recorded a campaign change in {territory}.",
        };
    }

    private static string ActorName(Guid? userId, IReadOnlyDictionary<Guid, string> names)
    {
        if (userId is { } id && names.TryGetValue(id, out var username) && !string.IsNullOrWhiteSpace(username))
        {
            return username;
        }

        return "A force";
    }

    private static string ForceController(CampaignPlayState play, Guid forceId, IReadOnlyDictionary<Guid, string> names)
    {
        var force = play.Forces.FirstOrDefault(item => item.Id == forceId);
        return ActorName(force?.ControllerUserId, names);
    }

    private static string TerritoryLabel(StoredCampaign campaign, PlayMap map, Guid? territoryId)
    {
        if (territoryId is null)
        {
            return "a territory";
        }

        var named = campaign.MapGraph?.Territories.FirstOrDefault(item => item.Id == territoryId.Value);
        if (!string.IsNullOrWhiteSpace(named?.Name))
        {
            return named.Name;
        }

        var playTerritory = map.Territory(territoryId.Value);
        return playTerritory is null ? "a territory" : $"territory {playTerritory.DisplayNumber}";
    }
}
