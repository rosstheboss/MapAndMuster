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
        CancellationToken cancellationToken,
        bool isAdministrator = false)
    {
        var membership = CampaignMapper.MembershipFor(campaign, viewerUserId);
        var progress = CampaignLifecycle.Progress(campaign, utcNow);
        var play = campaign.PlayState ?? CampaignPlayState.Empty;
        var map = CampaignLifecycle.ToPlayMap(campaign);
        var window = play.CurrentWindow();
        var names = await UsernamesAsync(campaign, accounts, cancellationToken).ConfigureAwait(false);
        var participants = accounts is null
            ? (IReadOnlyList<CampaignParticipantDetail>)[]
            : await ParticipantsAsync(campaign, accounts, cancellationToken).ConfigureAwait(false);
        var mentionable = accounts is null
            ? (IReadOnlyList<CampaignLogMemberDetail>)[]
            : ToChatMembers(participants);
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
        var canDebug = membership?.IsGameMaster == true || isAdministrator;
        var isDebugActive = play.DebugActorUserId is not null;
        var staffView = canDebug && isDebugActive;
        var scoring = CampaignPointStandingsMapper.ToScoring(campaign, participants, viewerUserId, staffView);
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
            CanDebug = canDebug,
            IsDebugActive = isDebugActive,
            DebugActorUserId = play.DebugActorUserId,
            IsParticipant = membership?.IsPlayer == true,
            CanChat = membership is not null,
            CanInspectPrivateChat = CampaignChatContext.CanInspectPrivateChat(isAdministrator, viewerUserId, play),
            MentionableMembers = mentionable,
            ChatChannels = membership is null ? [] : CampaignChatContext.Channels(campaign, viewerUserId, mentionable),
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
            CanChooseFaction = CampaignMapper.CanChooseFaction(membership, progress.Status),
            IsCommitted = currentActionId is { } id
                && play.Commitments.Any(item => item.WindowId == id && item.UserId == viewerUserId),
            RoundCount = campaign.RoundCount,
            MinRoundCount = Math.Max(progress.CurrentRound ?? CampaignSetupRules.MinRoundCount, CampaignSetupRules.MinRoundCount),
            RemainingWindows = remaining,
            Factions = CampaignMapper.ToDetail(campaign, viewerUserId, utcNow).Factions,
            StructureTypes = CampaignMapper.ToDetail(campaign, viewerUserId, utcNow).StructureTypes,
            ItemObjectives = VisibleItems(play, campaign, viewerUserId, staffView),
            BrokenAllyFactionIds = play.BrokenAllyFactionIds,
            Standings = scoring.Standings,
            PublicObjectiveLeaderboards = scoring.Leaderboards,
            PointsPerBattleWon = campaign.BattleScoring.PointsPerWin,
            PointsPerBattleDraw = campaign.BattleScoring.PointsPerDraw,
            UseDifferentialBattleScoring = campaign.BattleScoring.UseDifferential,
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
                    MoveTargets = force.ControllerUserId == viewerUserId || staffView
                        ? CampaignPlayRules.EligibleMoves(map, force)
                        : [],
                    AvailableActions = force.ControllerUserId == viewerUserId || staffView
                        ? [.. ActionResolution.EligibleActions(
                            play,
                            map,
                            force,
                            campaign.Factions.ToDictionary(static faction => faction.Id, static faction => faction.AllyGroupName)).Select(static kind => kind.ToString())]
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
            DebugDrafts = DebugDraftsFor(play, staffView),
            Commitments = commitments,
            Battles = battles,
            Log = VisibleLogEntries(
                campaign,
                names,
                viewerUserId,
                CampaignChatContext.CanInspectPrivateChat(isAdministrator, viewerUserId, play)),
            PlayersMissingFaction =
            [
                .. campaign.Memberships
                    .Where(member => member.IsPlayer && member.FactionId is null)
                    .Select(member => names.GetValueOrDefault(member.UserId) ?? member.UserId.ToString()),
            ],
        };
    }

    private static IReadOnlyList<PlayDraftDetail> DebugDraftsFor(CampaignPlayState play, bool staffView)
    {
        if (!staffView)
        {
            return [];
        }

        var window = play.CurrentWindow();
        if (window is { Kind: RoundPhaseKind.Action, Status: PhaseWindowStatus.Open })
        {
            return
            [
                .. play.Drafts
                    .Where(draft => draft.WindowId == window.Id)
                    .Select(draft => new PlayDraftDetail
                    {
                        ForceId = draft.ForceId,
                        Kind = draft.Kind.ToString(),
                        TargetTerritoryId = draft.TargetTerritoryId,
                        StructureTypeId = draft.StructureTypeId,
                    }),
            ];
        }

        var lastAction = play.Windows.LastOrDefault(item =>
            item.Kind == RoundPhaseKind.Action && item.Status == PhaseWindowStatus.Resolved);
        if (lastAction is null || window is null || window.Status != PhaseWindowStatus.Open)
        {
            return [];
        }

        var lastIndex = play.Windows.ToList().FindIndex(item => item.Id == lastAction.Id);
        if (lastIndex < 0 || lastIndex + 1 >= play.Windows.Count || play.Windows[lastIndex + 1].Id != window.Id)
        {
            return [];
        }

        return
        [
            .. play.Forces.Select(force =>
            {
                var submission = play.LatestSubmission(lastAction.Id, force.Id);
                return new PlayDraftDetail
                {
                    ForceId = force.Id,
                    Kind = (submission?.Kind ?? ActionKind.Hold).ToString(),
                    TargetTerritoryId = submission?.TargetTerritoryId,
                    StructureTypeId = submission?.StructureTypeId,
                };
            }),
        ];
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
                    WinnerScore = mine.WinnerScore,
                    LoserScore = mine.LoserScore,
                },
            OpponentSubmission = theirs is null || myForce is null
                ? null
                : new PlayBattleSubmissionDetail
                {
                    SubmitterUserId = theirs.SubmitterUserId,
                    WinnerForceId = theirs.WinnerForceId,
                    IsDraw = theirs.IsDraw,
                    WinnerScore = theirs.WinnerScore,
                    LoserScore = theirs.LoserScore,
                },
            WinnerForceId = battle.WinnerForceId,
            IsDraw = battle.IsDraw,
            WinnerScore = battle.WinnerScore,
            LoserScore = battle.LoserScore,
            NeedsRetreat = needsRetreat,
            RetreatTargets = needsRetreat && myForce is not null
                ? CampaignPlayRules.EligibleRetreats(map, myForce)
                : [],
        };
    }

    internal static IReadOnlyList<PlayLogEntryDetail> ToLogEntries(
        StoredCampaign campaign,
        IReadOnlyDictionary<Guid, string> names,
        Guid viewerUserId,
        bool inspectPrivateChat)
    {
        return VisibleLogEntries(campaign, names, viewerUserId, inspectPrivateChat);
    }

    private static IReadOnlyList<PlayLogEntryDetail> VisibleLogEntries(
        StoredCampaign campaign,
        IReadOnlyDictionary<Guid, string> names,
        Guid viewerUserId,
        bool inspectPrivateChat)
    {
        var play = campaign.PlayState ?? CampaignPlayState.Empty;
        var map = CampaignLifecycle.ToPlayMap(campaign);
        var memberships = CampaignChatContext.Memberships(campaign);
        return
        [
            .. play.Log
                .Where(entry => CampaignChatRules.CanView(entry, viewerUserId, memberships, inspectPrivateChat))
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

        var userIds = campaign.Memberships.Select(static member => member.UserId);
        if (campaign.PlayState is { } play)
        {
            userIds = userIds.Concat(
                play.Log
                    .Select(static entry => entry.ActorUserId)
                    .Where(static id => id is { } && id != Guid.Empty)
                    .Select(static id => id!.Value));
        }

        foreach (var userId in userIds.Distinct())
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
        var participants = await ParticipantsAsync(campaign, accounts, cancellationToken).ConfigureAwait(false);
        return ToChatMembers(participants);
    }

    internal static IReadOnlyList<CampaignLogMemberDetail> ToChatMembers(
        IReadOnlyList<CampaignParticipantDetail> participants)
    {
        ArgumentNullException.ThrowIfNull(participants);
        return
        [
            .. participants.Select(static participant => new CampaignLogMemberDetail
            {
                UserId = participant.UserId,
                Username = participant.Username,
                DisplayName = participant.DisplayName,
            }),
        ];
    }

    internal static async Task<IReadOnlyList<CampaignParticipantDetail>> ParticipantsAsync(
        StoredCampaign campaign,
        IUserAccountStore accounts,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        ArgumentNullException.ThrowIfNull(accounts);
        var administratorIds = await accounts
            .FindAdministratorIdsAsync(
                [.. campaign.Memberships.Select(static membership => membership.UserId)],
                cancellationToken)
            .ConfigureAwait(false);
        var participants = new List<CampaignParticipantDetail>();
        foreach (var membership in campaign.Memberships)
        {
            var account = await accounts.FindByIdAsync(membership.UserId, cancellationToken).ConfigureAwait(false);
            if (account is null)
            {
                continue;
            }

            var profile = ProfileMapper.ToPublic(account);
            var faction = membership.FactionId is { } factionId
                ? campaign.Factions.FirstOrDefault(item => item.Id == factionId)
                : null;
            participants.Add(new CampaignParticipantDetail
            {
                UserId = membership.UserId,
                Username = profile.Username,
                DisplayName = profile.DisplayName,
                IsPlayer = membership.IsPlayer,
                IsGameMaster = membership.IsGameMaster,
                IsAdministrator = administratorIds.Contains(membership.UserId),
                FactionName = faction?.Name,
                Subfaction = membership.Subfaction,
                FactionId = faction?.Id,
                FactionColor = faction?.Color,
                HasFlagImage = !string.IsNullOrWhiteSpace(faction?.FlagImageStorageKey),
                AllyGroupName = faction?.AllyGroupName,
            });
        }

        return
        [
            .. participants.OrderBy(static participant => participant.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static participant => participant.Username, StringComparer.OrdinalIgnoreCase),
        ];
    }

    private static IReadOnlyList<PlayItemObjectiveDetail> VisibleItems(
        CampaignPlayState play,
        StoredCampaign campaign,
        Guid viewerUserId,
        bool staffView)
    {
        var types = campaign.ItemObjectiveTypes.ToDictionary(static type => type.Id);
        var forcesById = play.Forces.ToDictionary(static force => force.Id);
        return
        [
            .. play.ItemObjectives
                .Where(item => item.IsRevealed
                    || staffView
                    || (item.PossessorForceId is { } forceId
                        && forcesById.TryGetValue(forceId, out var possessor)
                        && possessor.ControllerUserId == viewerUserId))
                .Select(item =>
                {
                    types.TryGetValue(item.TypeId, out var type);
                    var locationVisible = item.IsRevealed || staffView;
                    return new PlayItemObjectiveDetail
                    {
                        Id = item.Id,
                        TypeId = item.TypeId,
                        Name = item.Name,
                        TerritoryId = locationVisible ? item.TerritoryId : null,
                        PossessorForceId = item.PossessorForceId is { } possessorId
                            && (locationVisible
                                || (forcesById.TryGetValue(possessorId, out var holder)
                                    && holder.ControllerUserId == viewerUserId))
                            ? item.PossessorForceId
                            : null,
                        IsRevealed = item.IsRevealed,
                        BuiltinSymbol = type?.BuiltinSymbol ?? "Crown",
                        Color = type?.Color ?? "#C45C26",
                        HasImage = !string.IsNullOrWhiteSpace(type?.ImageStorageKey),
                    };
                }),
        ];
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
            OriginatorUsername = entry.Kind == PlayLogKind.PlayerChat && entry.ActorUserId is { } actorId
                ? names.GetValueOrDefault(actorId)
                : null,
            Summary = FormatLog(entry, campaign, map, play, names),
            TerritoryId = entry.TerritoryId,
            ForceId = entry.ForceId,
            BattleId = entry.BattleId,
            IsSystemAdjustment = entry.IsSystemAdjustment,
            ChannelKind = entry.ChatChannelKind.ToString(),
            ChannelLabel = entry.ChatTargetLabel,
            IsPrivate = entry.IsPrivateChat,
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
                    ? $"{actor} overrode the battle result in {territory}. Winner: {ForceController(play, gmWinner, names)}."
                    : $"{actor} overrode the battle result in {territory} as a draw.",
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
            PlayLogKind.DebugEntered =>
                $"{actor} entered debug mode.",
            PlayLogKind.DebugExited =>
                $"{actor} exited debug mode.",
            PlayLogKind.DebugOrderCorrected =>
                entry.ActionKind is { } corrected
                    ? $"{actor} corrected an order to {corrected}."
                    : $"{actor} corrected an order in debug mode.",
            PlayLogKind.DebugActionReresolved =>
                $"{actor} re-resolved the previous action window.",
            PlayLogKind.ItemObjectiveFound =>
                $"{(entry.ForceId is { } foundId ? ForceController(play, foundId, names) : actor)} found {entry.Message ?? "an item objective"} in {territory}.",
            PlayLogKind.ItemObjectivePickedUp =>
                $"{(entry.ForceId is { } takenId ? ForceController(play, takenId, names) : actor)} took {entry.Message ?? "an item objective"}.",
            PlayLogKind.ItemObjectiveDropped =>
                $"{(entry.ForceId is { } droppedId ? ForceController(play, droppedId, names) : actor)} dropped {entry.Message ?? "an item objective"} in {territory}.",
            PlayLogKind.ItemObjectivesStaffRevealed =>
                $"{actor} revealed hidden item objectives.",
            PlayLogKind.PublicObjectiveAwarded =>
                $"{actor} awarded {PublicObjectiveName(campaign, entry.Message)}.",
            PlayLogKind.PublicObjectiveRevoked =>
                $"{actor} revoked {PublicObjectiveName(campaign, entry.Message)}.",
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

    private static string PublicObjectiveName(StoredCampaign campaign, string? objectiveId)
    {
        if (Guid.TryParse(objectiveId, out var id))
        {
            var type = campaign.PublicObjectiveTypes.FirstOrDefault(item => item.Id == id);
            if (type is not null)
            {
                return type.Name;
            }
        }

        return "a public objective";
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
