using System.Globalization;
using MapAndMuster.Application.Campaigns;
using MapAndMuster.Application.Identity;
using MapAndMuster.Application.Maps;
using MapAndMuster.Application.Ports;
using MapAndMuster.Domain.Campaigns;
using MapAndMuster.Domain.Play;

namespace MapAndMuster.Application.Play;

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
        var specialRules = CampaignPlayCatalog.SpecialRules(campaign);
        var allyGroups = campaign.Factions.ToDictionary(static faction => faction.Id, static faction => faction.AllyGroupName);
        var window = play.CurrentWindow();
        var accountContext = await ResolveAccountsAsync(campaign, accounts, cancellationToken).ConfigureAwait(false);
        var names = Usernames(accountContext);
        var participants = accounts is null
            ? (IReadOnlyList<CampaignParticipantDetail>)[]
            : Participants(campaign, accountContext);
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
        var scoring = CampaignPointStandingsMapper.ToScoring(campaign, participants, viewerUserId, staffView, utcNow);
        var catalog = CampaignMapper.ToDetail(
            campaign,
            viewerUserId,
            utcNow,
            participants: participants,
            staffView: staffView,
            isAdministrator: isAdministrator);
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

        var commitments = CommitmentsFor(play, window, names);

        var battles = play.Battles
            .Where(battle => battle.Status is BattleStatus.Pending or BattleStatus.AwaitingResults or BattleStatus.Disputed
                || (window is not null && battle.BattleWindowId == window.Id))
            .Select(battle => ToBattle(play, map, campaign, battle, viewerUserId, canDebug))
            .ToArray();

        _ = revealed;
        return new CampaignPlayDetail
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Revision = campaign.Revision,
            CanManage = membership?.IsGameMaster == true || isAdministrator,
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
            HasMap = CampaignMapper.HasMapData(campaign),
            AssetTags = CampaignAssetTagMap.Build(campaign),
            FactionId = membership?.FactionId,
            CanChooseFaction = CampaignMapper.CanChooseFaction(membership, progress.Status),
            IsCommitted = ViewerIsCommitted(play, window, viewerUserId),
            ViewerSupply = ToViewerSupply(play, map, campaign, viewerUserId, membership?.IsPlayer == true, window),
            RoundCount = campaign.RoundCount,
            MinRoundCount = Math.Max(progress.CurrentRound ?? CampaignSetupRules.MinRoundCount, CampaignSetupRules.MinRoundCount),
            RemainingWindows = remaining,
            Factions = catalog.Factions,
            StructureTypes = catalog.StructureTypes,
            ItemObjectives = VisibleItems(play, campaign, viewerUserId, staffView),
            BrokenAllyFactionIds = play.BrokenAllyFactionIds,
            Standings = scoring.Standings,
            PublicObjectiveLeaderboards = scoring.Leaderboards,
            PrivateObjectives = catalog.PrivateObjectives,
            PrivateObjectiveUnclaimedCounts = catalog.PrivateObjectiveUnclaimedCounts,
            RivalObjectives = catalog.RivalObjectives,
            SpecialRules = catalog.SpecialRules,
            ForceStatuses = catalog.ForceStatuses,
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
                    StatusName = force.StatusName,
                    StatusEffects = campaign.ForceStatuses
                        .FirstOrDefault(status => string.Equals(status.Name, force.StatusName, StringComparison.OrdinalIgnoreCase))
                        ?.Effects,
                    MoveTargets = force.ControllerUserId == viewerUserId || staffView
                        ? CampaignPlayRules.EligibleMoves(map, force, play.ItemObjectives, specialRules, play.Forces)
                        : [],
                    MoveHops = force.ControllerUserId == viewerUserId || staffView
                        ? [.. CampaignPlayRules.EligibleMoveHops(map, force, specialRules, play.ItemObjectives, play.Forces).Select(static hop => new PlayMoveHopDetail
                        {
                            ViaTerritoryId = hop.ViaTerritoryId,
                            TargetTerritoryId = hop.TargetTerritoryId,
                            IntermediateTerritoryIds = hop.IntermediateTerritoryIds,
                        })]
                        : [],
                    AvailableActions = force.ControllerUserId == viewerUserId || staffView
                        ? [.. ActionResolution.EligibleActions(play, map, force, allyGroups, specialRules).Select(static kind => kind.ToString())]
                        : [],
                    Subfaction = force.Subfaction,
                    CanMoveTwoTerritories = ForceMovementRules.EffectiveSpeed(force, specialRules, map, play.ItemObjectives, play.Forces) >= 2,
                    MovementSpeed = ForceMovementRules.EffectiveSpeed(force, specialRules, map, play.ItemObjectives, play.Forces),
                    CanDestroyImmediately = FactionSpecialRulePolicies.CanDestroyImmediately(force, specialRules),
                    CanUseExtraBlackPowder = specialRules.Has(force, SpecialRuleEffectKeys.PreparedForBattle),
                    CanUseMagicalSupply = specialRules.Has(force, SpecialRuleEffectKeys.MagicalSupply),
                    HiddenRelicNearby = (force.ControllerUserId == viewerUserId || staffView)
                        && FactionSpecialRulePolicies.HiddenRelicAdjacent(map, force, play.ItemObjectives, specialRules),
                    BattleReminders = BattleRemindersFor(campaign, force, specialRules, map, play),
                    Supply = force.ControllerUserId == viewerUserId || staffView
                        ? ToForceSupply(play, map, campaign, force, window)
                        : null,
                    CanChooseTeleportDestination = (force.ControllerUserId == viewerUserId || staffView)
                        && ItemObjectiveEffectRules.CanChosenTeleport(
                            force,
                            map,
                            play.ItemObjectives,
                            specialRules,
                            play.CurrentWindow()?.RoundNumber ?? 0),
                    TeleportTargets = (force.ControllerUserId == viewerUserId || staffView)
                        && ItemObjectiveEffectRules.CanChosenTeleport(
                            force,
                            map,
                            play.ItemObjectives,
                            specialRules,
                            play.CurrentWindow()?.RoundNumber ?? 0)
                        ? TeleportActionRules.ChosenDestinations(
                            map,
                            force,
                            play.Forces,
                            allyGroups,
                            play.BrokenAllyFactionIds,
                            play.AllyBetrayals)
                        : [],
                    IsRandomTeleportLocked = (force.ControllerUserId == viewerUserId || staffView)
                        && force.PendingRandomTeleportDestinationId is not null,
                    IsTeleporting = force.PendingRandomTeleportDestinationId is not null
                        || ((force.ControllerUserId == viewerUserId || staffView)
                            && currentActionId is { } teleportWindow
                            && play.LatestSubmission(teleportWindow, force.Id) is { } teleportOrder
                            && TeleportActionRules.IsTeleport(teleportOrder.Kind)),
                    DroppableItemObjectiveIds = (force.ControllerUserId == viewerUserId || staffView)
                        ? [.. play.ItemObjectives
                            .Where(item => item.PossessorForceId == force.Id && TeleportActionRules.CanDropOnMove(item))
                            .Select(static item => item.Id)]
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
                            ViaTerritoryId = draft.ViaTerritoryId,
                            ViaPath = draft.ViaPath,
                            DestroyImmediately = draft.DestroyImmediately,
                            DroppedItemObjectiveIds = draft.DroppedItemObjectiveIds,
                        }),
                ]
                : [],
            Orders = orders,
            DebugDrafts = DebugDraftsFor(play, staffView),
            Commitments = commitments,
            Battles = battles,
            Log = ToLogEntries(
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
            MapTerritories =
            [
                .. map.Territories.Select(static territory => new PlayMapTerritoryDetail
                {
                    Id = territory.Id,
                    OwnerFactionId = territory.OwnerFactionId,
                    OwnerSubfaction = territory.OwnerSubfaction,
                    StructureTypeId = territory.StructureTypeId,
                    StructureCondition = territory.StructureCondition.ToString(),
                }),
            ],
        };
    }

    private static IReadOnlyList<PlayCommitmentDetail> CommitmentsFor(
        CampaignPlayState play,
        PhaseWindow? window,
        IReadOnlyDictionary<Guid, string> names)
    {
        if (window is { Kind: RoundPhaseKind.Action, Status: PhaseWindowStatus.Open })
        {
            return
            [
                .. play.ActionRosterPlayers()
                    .Select(userId => new PlayCommitmentDetail
                    {
                        UserId = userId,
                        Username = names.GetValueOrDefault(userId),
                        IsCommitted = play.IsActionCommitted(window.Id, userId),
                    }),
            ];
        }

        if (window is { Kind: RoundPhaseKind.Battle, Status: PhaseWindowStatus.Open })
        {
            return
            [
                .. play.RequiredBattlePlayers(window.Id)
                    .Select(userId =>
                    {
                        var pending = play.PendingBattleDuties(window.Id, userId);
                        return new PlayCommitmentDetail
                        {
                            UserId = userId,
                            Username = names.GetValueOrDefault(userId),
                            IsCommitted = !pending.NeedsResult && !pending.NeedsRetreat,
                            NeedsResult = pending.NeedsResult,
                            NeedsRetreat = pending.NeedsRetreat,
                        };
                    }),
            ];
        }

        return [];
    }

    private static bool ViewerIsCommitted(CampaignPlayState play, PhaseWindow? window, Guid viewerUserId)
    {
        if (window is { Kind: RoundPhaseKind.Action, Status: PhaseWindowStatus.Open })
        {
            return play.IsActionCommitted(window.Id, viewerUserId);
        }

        if (window is { Kind: RoundPhaseKind.Battle, Status: PhaseWindowStatus.Open }
            && play.RequiredBattlePlayers(window.Id).Contains(viewerUserId))
        {
            return play.HasCompletedBattleDuties(window.Id, viewerUserId);
        }

        return false;
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
                    ViaTerritoryId = draft.ViaTerritoryId,
                    ViaPath = draft.ViaPath,
                    DestroyImmediately = draft.DestroyImmediately,
                    DroppedItemObjectiveIds = draft.DroppedItemObjectiveIds,
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
                    ViaTerritoryId = submission?.ViaTerritoryId,
                    ViaPath = submission?.ViaPath ?? [],
                    DestroyImmediately = submission?.DestroyImmediately == true,
                };
            }),
        ];
    }

    private static PlayBattleDetail ToBattle(
        CampaignPlayState play,
        PlayMap map,
        StoredCampaign campaign,
        CampaignBattle battle,
        Guid viewerUserId,
        bool canStaff)
    {
        var myForce = play.Forces.FirstOrDefault(force =>
            force.ControllerUserId == viewerUserId && battle.ParticipantForceIds.Contains(force.Id));
        var opponent = play.Forces.FirstOrDefault(force =>
            force.ControllerUserId != viewerUserId && battle.ParticipantForceIds.Contains(force.Id));
        var mine = play.LatestBattleSubmission(battle.Id, viewerUserId);
        var theirs = opponent is null ? null : play.LatestBattleSubmission(battle.Id, opponent.ControllerUserId);
        var myRetreat = myForce is null ? null : play.RetreatFor(battle.Id, myForce.Id);
        var awaitingRetreat = CampaignPlayRules.BattleAwaitsRetreat(play, battle);
        var needsRetreat = myForce is not null
            && battle.Status is BattleStatus.Finalized or BattleStatus.GMResolved
            && (myRetreat is null || !myRetreat.IsCommitted)
            && (battle.IsNoContest || battle.IsDraw || battle.WinnerForceId != myForce.Id);
        var canSurrender = myForce is not null
            && myForce.InBattle
            && battle.Status is not BattleStatus.Finalized and not BattleStatus.GMResolved
            && !battle.SurrenderedForceIds.Contains(myForce.Id)
            && !play.Retreats.Any(item =>
                item.BattleId == battle.Id && item.ForceId == myForce.Id && item.IsSurrender && item.IsCommitted);
        var round = play.CurrentWindow()?.RoundNumber
            ?? (play.Windows.Count > 0 ? play.Windows[^1].RoundNumber : 1);
        var catalog = CampaignPlayCatalog.Supply(campaign);
        var allies = campaign.Factions.ToDictionary(static faction => faction.Id, static faction => faction.AllyGroupName);
        var reportingForces = battle.ReportingForceIds
            .Select(forceId => play.Forces.FirstOrDefault(force => force.Id == forceId))
            .OfType<CampaignForce>()
            .ToArray();
        var sides = BattleMatchRules.Sides(
            reportingForces,
            allies,
            play.BrokenAllyFactionIds,
            play.BrokenAllySubfactions,
            allyBetrayals: play.AllyBetrayals);
        var assignment = ResolveMissingMission(play, map, campaign, battle, allies);
        var missionId = battle.MissionId ?? assignment?.MissionId;
        var attackerForceId = battle.AttackerForceId ?? assignment?.AttackerForceId;
        var defenderForceId = battle.DefenderForceId ?? assignment?.DefenderForceId;
        var questions = CampaignPlayCatalog.MissionQuestions(campaign, battle.TerritoryId, missionId);
        var mission = missionId is { } resolvedMissionId
            ? CampaignPlayCatalog.FindMission(campaign, resolvedMissionId)
            : null;
        var missionSetup = mission is null ? null : CampaignPlayCatalog.ToMissionSetup(mission);
        var forceSupplies = battle.ParticipantForceIds
            .Select(forceId => play.Forces.FirstOrDefault(force => force.Id == forceId))
            .OfType<CampaignForce>()
            .Select(force =>
            {
                var snapshot = SupplyRules.ForForce(play, map, catalog, force, round);
                var temporary = play.PlayerSupplies
                    .FirstOrDefault(item => item.UserId == force.ControllerUserId)
                    ?.TemporarySupplyPoints ?? 0;
                var sideCount = sides.FirstOrDefault(side => side.Any(member => member.Id == force.Id))?.Count ?? 1;
                var alliedArmy = AlliedArmyPointRules.ForceArmyPoints(snapshot.MaxArmyPoints, sideCount);
                var armyAdvantaged = missionSetup is not null
                    && MissionAdvantageRules.IsAdvantagedSide(
                        force.Id,
                        missionSetup.ArmyPointsAdvantageSide,
                        attackerForceId,
                        defenderForceId,
                        sides);
                var supplyAdvantaged = missionSetup is not null
                    && MissionAdvantageRules.IsAdvantagedSide(
                        force.Id,
                        missionSetup.SupplyPointsAdvantageSide,
                        attackerForceId,
                        defenderForceId,
                        sides);
                var mapSupply = missionSetup is null
                    ? snapshot.MapSupplyPoints
                    : MissionAdvantageRules.ApplySupplyPoints(snapshot.MapSupplyPoints, missionSetup, supplyAdvantaged);
                var allowance = missionSetup is null
                    ? snapshot.ForceAllowancePoints
                    : MissionAdvantageRules.ApplySupplyPoints(snapshot.ForceAllowancePoints, missionSetup, supplyAdvantaged);
                var current = allowance + temporary;
                var contributions = ToContributions(snapshot, campaign);
                if (temporary != 0)
                {
                    contributions =
                    [
                        .. contributions,
                        new SupplyContributionDetail
                        {
                            Kind = nameof(SupplyContributionKind.Temporary),
                            Label = "Temporary supply",
                            Points = temporary,
                        },
                    ];
                }
                if (missionSetup is not null && mapSupply != snapshot.MapSupplyPoints)
                {
                    contributions =
                    [
                        .. contributions,
                        new SupplyContributionDetail
                        {
                            Kind = nameof(SupplyContributionKind.MissionAdvantage),
                            Label = "Mission supply advantage",
                            Points = mapSupply - snapshot.MapSupplyPoints,
                        },
                    ];
                }

                return new PlayBattleForceSupplyDetail
                {
                    ForceId = force.Id,
                    UserId = force.ControllerUserId,
                    ForceAllowancePoints = allowance,
                    CurrentSupplyPoints = current,
                    TemporarySupplyPoints = temporary,
                    MapSupplyPoints = mapSupply,
                    RoundFreeSupplyPoints = snapshot.RoundFreeSupplyPoints,
                    SplitPenaltyPoints = snapshot.SplitPenaltyPoints,
                    RoundMaxArmyPoints = snapshot.MaxArmyPoints,
                    AlliedArmyPoints = missionSetup is null
                        ? alliedArmy
                        : MissionAdvantageRules.ApplyArmyPoints(alliedArmy, missionSetup, armyAdvantaged),
                    FreeCharacterCount = snapshot.FreeCharacterCount,
                    IsSplit = snapshot.IsSplit,
                    Contributions = contributions,
                };
            })
            .ToArray();
        var viewerSupply = forceSupplies.FirstOrDefault(item => item.UserId == viewerUserId);
        return new PlayBattleDetail
        {
            Id = battle.Id,
            TerritoryId = battle.TerritoryId,
            Status = battle.Status.ToString(),
            ParticipantForceIds = battle.ParticipantForceIds,
            ActiveForceIds = battle.ActiveForceIds,
            WaitingForceIds = battle.WaitingForceIds,
            ReportingForceIds = battle.ReportingForceIds,
            IsNoContest = battle.IsNoContest,
            IsRinger = battle.IsRinger,
            RingerFactionId = battle.RingerFactionId,
            IsMine = myForce is not null,
            MySubmission = ToSubmission(mine),
            OpponentSubmission = myForce is null && !canStaff ? null : ToSubmission(theirs),
            ArmyLists = myForce is null && !canStaff
                ? []
                : [
                    .. battle.ParticipantForceIds
                        .Select(forceId => play.LatestArmyList(battle.Id, forceId))
                        .OfType<BattleArmyListSubmission>()
                        .Select(ToArmyList),
                ],
            WinnerForceId = battle.WinnerForceId,
            IsDraw = battle.IsDraw,
            WinnerScore = battle.WinnerScore,
            LoserScore = battle.LoserScore,
            NeedsRetreat = needsRetreat,
            AwaitingRetreat = awaitingRetreat,
            IsRetreatCommitted = myRetreat is { IsCommitted: true, IsSurrender: false },
            IsSurrenderCommitted = myRetreat is { IsCommitted: true, IsSurrender: true },
            RetreatDraftTargetId = myRetreat?.TargetTerritoryId,
            CanSurrender = canSurrender,
            RetreatTargets = (needsRetreat || canSurrender || myRetreat is not null) && myForce is not null
                ? CampaignPlayRules.EligibleRetreats(
                    map,
                    myForce,
                    CampaignPlayCatalog.SpecialRules(campaign),
                    play.Forces,
                    campaign.Factions.ToDictionary(static faction => faction.Id, static faction => faction.AllyGroupName),
                    play.BrokenAllyFactionIds,
                    play.AllyBetrayals,
                    play.ItemObjectives,
                    play.Battles)
                : [],
            ResultQuestions =
            [
                .. questions.Select(static question => new MissionResultQuestionDetail
                {
                    Id = question.Id,
                    Prompt = question.Prompt,
                    Kind = question.Kind.ToString(),
                    BattlePoints = question.BattlePoints,
                    CampaignPoints = question.CampaignPoints,
                }),
            ],
            ViewerSupplyPoints = viewerSupply?.CurrentSupplyPoints,
            ForceSupplies = forceSupplies,
            CanStaffConfirm = canStaff
                && battle.Status is BattleStatus.AwaitingResults or BattleStatus.Disputed
                && play.BattleSubmissions.Any(item => item.BattleId == battle.Id && item.AcceptedSubmissionId is null),
            Mission = mission is null ? null : CampaignMapper.ToMission(mission),
            AttackerForceId = attackerForceId,
            DefenderForceId = defenderForceId,
        };
    }

    private static BattleMissionAssignment? ResolveMissingMission(
        CampaignPlayState play,
        PlayMap map,
        StoredCampaign campaign,
        CampaignBattle battle,
        IReadOnlyDictionary<Guid, string?> allies)
    {
        if (battle.MissionId is not null)
        {
            return null;
        }

        var present = battle.ParticipantForceIds
            .Select(forceId => play.Forces.FirstOrDefault(force => force.Id == forceId))
            .OfType<CampaignForce>()
            .ToArray();
        if (present.Length == 0)
        {
            return null;
        }

        var lastAction = play.Windows.LastOrDefault(item =>
            item.Kind == RoundPhaseKind.Action && item.Status == PhaseWindowStatus.Resolved);
        var arrivalKinds = present.ToDictionary(
            static force => force.Id,
            force => lastAction is null
                ? ActionKind.Hold
                : play.LatestSubmission(lastAction.Id, force.Id)?.Kind ?? ActionKind.Hold);
        return BattleMissionRules.Choose(
            map.Territory(battle.TerritoryId),
            present,
            arrivalKinds,
            allies,
            play.BrokenAllyFactionIds,
            CampaignPlayCatalog.TerrainSetups(campaign),
            CampaignPlayCatalog.StructureSetups(campaign),
            static _ => 0);
    }

    private static PlayBattleSubmissionDetail? ToSubmission(BattleResultSubmission? submission)
    {
        return submission is null
            ? null
            : new PlayBattleSubmissionDetail
            {
                SubmitterUserId = submission.SubmitterUserId,
                WinnerForceId = submission.WinnerForceId,
                IsDraw = submission.IsDraw,
                WinnerScore = submission.WinnerScore,
                LoserScore = submission.LoserScore,
                SubmittedUtc = submission.SubmittedUtc,
                Reports =
                [
                    .. submission.Reports.Select(static report => new BattleParticipantReportDetail
                    {
                        ForceId = report.ForceId,
                        VictoryPoints = report.VictoryPoints,
                        ArmyPoints = report.ArmyPoints,
                        DifferentialBattlePoints = report.DifferentialBattlePoints,
                        BonusBattlePoints = report.BonusBattlePoints,
                        SupplyCostingUnitCount = report.SupplyCostingUnitCount,
                        UsedExtraBlackPowder = report.UsedExtraBlackPowder,
                        MagicalSupplyRerolls = report.MagicalSupplyRerolls,
                        ArmyListText = report.ArmyListText,
                        ArmyListGameSystem = report.ArmyListGameSystem,
                        ArmyListBuilder = report.ArmyListBuilder.ToString(),
                        SupplyCategories =
                        [
                            .. report.SupplyCategories.Select(static category => new ArmyListSupplyCategoryDetail
                            {
                                Name = category.Name,
                                UnitCount = category.UnitCount,
                                SupplyPoints = category.SupplyPoints,
                                CostsSupply = category.CostsSupply,
                            }),
                        ],
                        Answers =
                        [
                            .. report.Answers.Select(static answer => new BattleQuestionAnswerDetail
                            {
                                QuestionId = answer.QuestionId,
                                BooleanValue = answer.BooleanValue,
                                BattlePointsValue = answer.BattlePointsValue,
                            }),
                        ],
                    }),
                ],
            };
    }

    private static PlayBattleArmyListDetail ToArmyList(BattleArmyListSubmission list)
    {
        return new PlayBattleArmyListDetail
        {
            ForceId = list.ForceId,
            SubmitterUserId = list.SubmitterUserId,
            SubmittedUtc = list.SubmittedUtc,
            ArmyPoints = list.ArmyPoints,
            SupplyCostingUnitCount = list.SupplyCostingUnitCount,
            ArmyListText = list.ArmyListText,
            ArmyListGameSystem = list.ArmyListGameSystem,
            ArmyListBuilder = list.ArmyListBuilder.ToString(),
            SupplyCategories =
            [
                .. list.SupplyCategories.Select(static category => new ArmyListSupplyCategoryDetail
                {
                    Name = category.Name,
                    UnitCount = category.UnitCount,
                    SupplyPoints = category.SupplyPoints,
                    CostsSupply = category.CostsSupply,
                }),
            ],
        };
    }

    internal static IReadOnlyList<PlayLogEntryDetail> ToLogEntries(
        StoredCampaign campaign,
        IReadOnlyDictionary<Guid, string> names,
        Guid viewerUserId,
        bool inspectPrivateChat)
    {
        var play = campaign.PlayState ?? CampaignPlayState.Empty;
        var map = CampaignLifecycle.ToPlayMap(campaign);
        return
        [
            .. VisiblePlayLogEntries(campaign, viewerUserId, inspectPrivateChat, names)
                .Select(item => ToLogEntry(item, campaign, map, play, names)),
        ];
    }

    internal static IReadOnlyList<PlayLogEntry> VisiblePlayLogEntries(
        StoredCampaign campaign,
        Guid viewerUserId,
        bool inspectPrivateChat,
        IReadOnlyDictionary<Guid, string>? names = null)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        var play = campaign.PlayState ?? CampaignPlayState.Empty;
        var memberships = CampaignChatContext.Memberships(campaign);
        var displayNames = names ?? new Dictionary<Guid, string>();
        return
        [
            .. PlayLogDisplayOrder.Sort(
                    play.Log
                        .Select(static (entry, index) => (entry, index))
                        .Where(item =>
                            CampaignChatRules.CanView(item.entry, viewerUserId, memberships, inspectPrivateChat)),
                    play,
                    displayNames)
                .Select(static item => item.Entry),
        ];
    }

    /// <summary>
    /// Resolves every account a campaign read needs in two queries: one batch account lookup
    /// covering memberships and play-log actors, and one administrator role check.
    /// </summary>
    /// <remarks>
    /// Callers that need both usernames and participants must resolve this once and pass it to
    /// <see cref="Usernames"/> and <see cref="Participants"/>. Resolving per projection is what
    /// previously issued one query per member, twice per request.
    /// </remarks>
    internal static async Task<CampaignAccountContext> ResolveAccountsAsync(
        StoredCampaign campaign,
        IUserAccountStore? accounts,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        if (accounts is null)
        {
            return CampaignAccountContext.Empty;
        }

        var memberIds = campaign.Memberships.Select(static member => member.UserId).ToArray();
        var userIds = memberIds.AsEnumerable();
        if (campaign.PlayState is { } play)
        {
            userIds = userIds.Concat(
                play.Log
                    .Select(static entry => entry.ActorUserId)
                    .Where(static id => id is { } && id != Guid.Empty)
                    .Select(static id => id!.Value));
        }

        var resolved = await accounts
            .FindManyByIdAsync([.. userIds.Distinct()], cancellationToken)
            .ConfigureAwait(false);
        var administratorIds = await accounts
            .FindAdministratorIdsAsync(memberIds, cancellationToken)
            .ConfigureAwait(false);
        return new CampaignAccountContext(resolved, administratorIds);
    }

    internal static Dictionary<Guid, string> Usernames(CampaignAccountContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var names = new Dictionary<Guid, string>(context.Accounts.Count);
        foreach (var (userId, account) in context.Accounts)
        {
            names[userId] = account.Username;
        }

        return names;
    }

    internal static async Task<Dictionary<Guid, string>> UsernamesAsync(
        StoredCampaign campaign,
        IUserAccountStore? accounts,
        CancellationToken cancellationToken)
    {
        var context = await ResolveAccountsAsync(campaign, accounts, cancellationToken).ConfigureAwait(false);
        return Usernames(context);
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
        ArgumentNullException.ThrowIfNull(accounts);
        var context = await ResolveAccountsAsync(campaign, accounts, cancellationToken).ConfigureAwait(false);
        return Participants(campaign, context);
    }

    internal static IReadOnlyList<CampaignParticipantDetail> Participants(
        StoredCampaign campaign,
        CampaignAccountContext context)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        ArgumentNullException.ThrowIfNull(context);
        var administratorIds = context.AdministratorIds;

        // The supply map, round, and catalog are the same for every member. Resolve them once
        // rather than rebuilding the play map inside the loop.
        var supplyContext = SupplyContextFor(campaign);

        var participants = new List<CampaignParticipantDetail>();
        foreach (var membership in campaign.Memberships)
        {
            if (!context.Accounts.TryGetValue(membership.UserId, out var account))
            {
                continue;
            }

            var profile = ProfileMapper.ToPublic(account);
            var faction = membership.FactionId is { } factionId
                ? campaign.Factions.FirstOrDefault(item => item.Id == factionId)
                : null;
            var appearance = faction is null
                ? null
                : FactionAppearance.Resolve(faction, membership.Subfaction);
            PlayerSupplySnapshot? supply = null;
            if (supplyContext is { } inputs && membership.IsPlayer)
            {
                supply = SupplyRules.ForPlayer(
                    inputs.Play,
                    inputs.Map,
                    inputs.Catalog,
                    membership.UserId,
                    inputs.Round);
            }

            var delinquencies = DelinquenciesFor(campaign, membership.UserId);
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
                FactionColor = appearance?.Color,
                HasFlagImage = appearance?.HasFlagImage == true,
                TintFlagImage = appearance?.TintFlagImage == true,
                AllyGroupName = faction?.AllyGroupName,
                CurrentSupplyPoints = supply?.CurrentSupplyPoints,
                TemporarySupplyPoints = supply?.TemporarySupplyPoints,
                MapSupplyPoints = supply?.MapSupplyPoints,
                RoundFreeSupplyPoints = supply?.RoundFreeSupplyPoints,
                MaxArmyPoints = supply?.MaxArmyPoints,
                FreeCharacterCount = supply?.FreeCharacterCount,
                SplitPenaltyPoints = supply?.SplitPenaltyPoints,
                Contributions = supply is null ? [] : ToContributions(supply, campaign),
                DelinquencyCount = delinquencies.Count,
                Delinquencies = delinquencies.Items,
            });
        }

        var byUser = participants.ToDictionary(static item => item.UserId);
        return
        [
            .. participants
                .Select(participant => WithTraitorVictims(participant, campaign, byUser))
                .OrderBy(static participant => participant.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static participant => participant.Username, StringComparer.OrdinalIgnoreCase),
        ];
    }

    /// <summary>
    /// Resolves the per-campaign inputs <see cref="SupplyRules.ForPlayer"/> needs, or
    /// <see langword="null"/> when no player can have supply yet.
    /// </summary>
    private static (CampaignPlayState Play, PlayMap Map, SupplyCatalog Catalog, int Round)? SupplyContextFor(
        StoredCampaign campaign)
    {
        if (campaign.PlayState is not { Forces.Count: > 0 } play)
        {
            return null;
        }

        var round = play.CurrentWindow()?.RoundNumber
            ?? (play.Windows.Count > 0 ? play.Windows[^1].RoundNumber : 1);
        return (play, CampaignLifecycle.ToPlayMap(campaign), CampaignPlayCatalog.Supply(campaign), round);
    }

    private static CampaignParticipantDetail WithTraitorVictims(
        CampaignParticipantDetail participant,
        StoredCampaign campaign,
        Dictionary<Guid, CampaignParticipantDetail> byUser)
    {
        var play = campaign.PlayState;
        if (play is null || play.AllyBetrayals.Count == 0)
        {
            return participant;
        }

        var victims = new List<TraitorVictimDetail>();
        var seen = new HashSet<(Guid FactionId, string Subfaction, Guid User)>();
        foreach (var betrayal in play.AllyBetrayals.Where(item => item.TraitorUserId == participant.UserId))
        {
            var key = (
                betrayal.BetrayedFactionId,
                betrayal.BetrayedSubfaction ?? string.Empty,
                betrayal.BetrayedUserId ?? Guid.Empty);
            if (!seen.Add(key))
            {
                continue;
            }

            var faction = campaign.Factions.FirstOrDefault(item => item.Id == betrayal.BetrayedFactionId);
            byUser.TryGetValue(betrayal.BetrayedUserId ?? Guid.Empty, out var victim);
            victims.Add(new TraitorVictimDetail
            {
                UserId = betrayal.BetrayedUserId,
                Username = victim?.Username,
                DisplayName = victim?.DisplayName,
                FactionName = faction?.Name ?? "Unknown faction",
                Subfaction = betrayal.BetrayedSubfaction ?? victim?.Subfaction,
            });
        }

        if (victims.Count == 0)
        {
            return participant;
        }

        return new CampaignParticipantDetail
        {
            UserId = participant.UserId,
            Username = participant.Username,
            DisplayName = participant.DisplayName,
            IsPlayer = participant.IsPlayer,
            IsGameMaster = participant.IsGameMaster,
            IsAdministrator = participant.IsAdministrator,
            FactionName = participant.FactionName,
            Subfaction = participant.Subfaction,
            FactionId = participant.FactionId,
            FactionColor = participant.FactionColor,
            HasFlagImage = participant.HasFlagImage,
            TintFlagImage = participant.TintFlagImage,
            AllyGroupName = participant.AllyGroupName,
            CurrentSupplyPoints = participant.CurrentSupplyPoints,
            TemporarySupplyPoints = participant.TemporarySupplyPoints,
            MapSupplyPoints = participant.MapSupplyPoints,
            RoundFreeSupplyPoints = participant.RoundFreeSupplyPoints,
            MaxArmyPoints = participant.MaxArmyPoints,
            FreeCharacterCount = participant.FreeCharacterCount,
            SplitPenaltyPoints = participant.SplitPenaltyPoints,
            Contributions = participant.Contributions,
            TraitorVictims = victims,
            DelinquencyCount = participant.DelinquencyCount,
            Delinquencies = participant.Delinquencies,
        };
    }

    private static (int Count, IReadOnlyList<ParticipantDelinquencyDetail> Items) DelinquenciesFor(
        StoredCampaign campaign,
        Guid userId)
    {
        var play = campaign.PlayState;
        if (play is null)
        {
            return (0, []);
        }

        var forceIds = play.Forces
            .Where(force => force.ControllerUserId == userId)
            .Select(force => force.Id)
            .ToHashSet();
        var records = play.Delinquencies.Where(item => forceIds.Contains(item.ForceId)).ToArray();
        var count = records.Sum(item => item.OffenceCount);
        var names = (campaign.MapGraph?.Territories ?? [])
            .ToDictionary(static territory => territory.Id, static territory => TerritoryDisplayName(territory));
        var items = records
            .SelectMany(static item => item.Offences)
            .OrderBy(static item => item.WindowEndsUtc)
            .ThenBy(static item => item.WindowId)
            .Select(item => new ParticipantDelinquencyDetail
            {
                RoundNumber = item.RoundNumber,
                PhaseNumber = item.PhaseNumber,
                PhaseKind = item.Kind.ToString(),
                KindOrdinal = item.KindOrdinal,
                WindowEndsUtc = item.WindowEndsUtc,
                TerritoryId = item.TerritoryId,
                TerritoryName = item.TerritoryId is { } territoryId && names.TryGetValue(territoryId, out var name)
                    ? name
                    : null,
            })
            .ToArray();
        return (count, items);
    }

    private static string TerritoryDisplayName(TerritoryDetail territory)
    {
        var name = territory.Name?.Trim();
        return string.IsNullOrWhiteSpace(name)
            ? territory.DisplayNumber.ToString(CultureInfo.InvariantCulture)
            : name;
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
                .Where(item => !item.IsDestroyed
                    && (item.IsRevealed
                    || staffView
                    || (item.PossessorForceId is { } forceId
                        && forcesById.TryGetValue(forceId, out var possessor)
                        && possessor.ControllerUserId == viewerUserId)))
                .Select(item =>
                {
                    types.TryGetValue(item.TypeId, out var type);
                    var locationVisible = item.IsRevealed || staffView;
                    var isHolder = item.PossessorForceId is { } possessorId
                        && forcesById.TryGetValue(possessorId, out var holder)
                        && holder.ControllerUserId == viewerUserId;
                    var showSecrets = isHolder || staffView;
                    return new PlayItemObjectiveDetail
                    {
                        Id = item.Id,
                        TypeId = item.TypeId,
                        Name = item.Name,
                        TerritoryId = locationVisible ? item.TerritoryId : null,
                        PossessorForceId = item.PossessorForceId is { } ownedId
                            && (locationVisible
                                || (forcesById.TryGetValue(ownedId, out var owner)
                                    && owner.ControllerUserId == viewerUserId))
                            ? item.PossessorForceId
                            : null,
                        IsRevealed = item.IsRevealed,
                        BuiltinSymbol = type?.BuiltinSymbol ?? "Crown",
                        Color = type?.Color ?? "#C45C26",
                        HasImage = !string.IsNullOrWhiteSpace(type?.ImageStorageKey),
                        FlavorText = showSecrets ? item.FlavorText : null,
                        StateKey = showSecrets ? item.StateKey : null,
                        IsDestroyed = item.IsDestroyed,
                        ResolvedChoiceId = showSecrets ? item.ResolvedChoiceId : null,
                        Choices = showSecrets && item.ResolvedChoiceId is null
                            ? [.. (type?.Choices ?? []).Select(static choice => new ItemObjectiveChoiceDetail
                            {
                                Id = choice.Id,
                                Name = choice.Name,
                                Results = [],
                            })]
                            : [],
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
            PlayLogKind.ActionCancelled =>
                FormatActionCancelled(entry, actor, territory, campaign, map, names),
            PlayLogKind.ResolvedAction =>
                FormatResolvedAction(entry, actor, territory, target),
            PlayLogKind.BattleCreated =>
                $"A battle started in {territory} between {participants}.",
            PlayLogKind.BattleFinalized =>
                play.Battles.FirstOrDefault(item => item.Id == entry.BattleId) is { IsRinger: true } ringer
                    ? ringer.IsNoContest || ringer.IsDraw
                        ? $"Ringer battle in {territory} ended with no winner."
                        : ringer.WinnerForceId is { } ringerWinner
                            ? $"Ringer battle in {territory} was finalized. Winner: {ForceController(play, ringerWinner, names)}."
                            : $"Ringer battle in {territory} was finalized. The ringer won."
                    : play.Battles.FirstOrDefault(item => item.Id == entry.BattleId)?.IsNoContest == true
                    ? $"Battle in {territory} ended with no winner."
                    : entry.ForceId is { } winner
                    ? $"Battle in {territory} was finalized. Winner: {ForceController(play, winner, names)}."
                    : $"Battle in {territory} was finalized as a draw.",
            PlayLogKind.BattleDisputed =>
                $"Battle in {territory} is disputed because the submitted results conflict.",
            PlayLogKind.BattleGmResolved =>
                entry.ForceId is { } gmWinner
                    ? $"{actor} overrode the battle result in {territory}. Winner: {ForceController(play, gmWinner, names)}."
                    : $"{actor} overrode the battle result in {territory} as a draw.",
            PlayLogKind.PlayerRetreat =>
                $"{actor} retreated force at {territory} to {target}.",
            PlayLogKind.PlayerSurrendered =>
                $"{actor} surrendered in {territory} and retreated to {target}.",
            PlayLogKind.RetreatCollisionResolved =>
                $"{actor} was sent from {territory} to {target} because enemy factions retreated to the same territory.",
            PlayLogKind.BattleMatchAdvanced =>
                $"The next pairing in {territory} is {participants}.",
            PlayLogKind.DefaultRetreat =>
                $"A missing retreat for {actor} was assigned to {target}.",
            PlayLogKind.UnresolvedBattleHeldOpen =>
                $"Battle in {territory} stayed open for a manager because no results were submitted.",
            PlayLogKind.NoResultForcedRetreat =>
                $"Neither side reported in {territory}; the fighting forces were forced to retreat.",
            PlayLogKind.DelinquencyThreshold =>
                $"{actor}'s force reached three missed-order offences and may be kicked.",
            PlayLogKind.RingerBattleCreated =>
                $"{actor} started a ringer battle in {territory}.",
            PlayLogKind.RingerBattleVoided =>
                $"The ringer battle in {territory} was voided because nobody reported.",
            PlayLogKind.CampaignClosed =>
                actor == "A force"
                    ? "A manager ended the campaign."
                    : $"{actor} ended the campaign.",
            PlayLogKind.CampaignEnded =>
                entry.Message ?? "The campaign ended.",
            PlayLogKind.CampaignStarted =>
                "The campaign started.",
            PlayLogKind.PhaseChanged =>
                entry.Message ?? "A new phase began.",
            PlayLogKind.RivalObjectiveRevealed =>
                FormatRivalObjectiveRevealed(entry, actor, names),
            PlayLogKind.ScheduleExtended =>
                ScheduleExtendedSummary(actor, entry.Message),
            PlayLogKind.ForcesRejoined =>
                $"{actor} merged forces at {territory}.",
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
                $"{(entry.ForceId is { } takenId ? ForceController(play, takenId, names) : actor)}'s force at {territory} picked up {entry.Message ?? "an item objective"}.",
            PlayLogKind.ItemObjectiveDropped =>
                $"{(entry.ForceId is { } droppedId ? ForceController(play, droppedId, names) : actor)}'s force dropped {entry.Message ?? "an item objective"} at {territory}.",
            PlayLogKind.ItemObjectivesStaffRevealed =>
                $"{actor} revealed hidden item objectives.",
            PlayLogKind.PublicObjectiveAwarded =>
                $"{actor} awarded {PublicObjectiveName(campaign, entry.Message)}.",
            PlayLogKind.PublicObjectiveRevoked =>
                $"{actor} revoked {PublicObjectiveName(campaign, entry.Message)}.",
            PlayLogKind.PrivateObjectiveRevealed =>
                $"{(entry.ActorUserId is null ? "A private objective" : actor + " revealed a private objective")}: {entry.Message ?? "a private objective"}.",
            PlayLogKind.ItemObjectiveDestroyed =>
                $"{actor} destroyed {entry.Message ?? "an item objective"}.",
            PlayLogKind.ForceStatusChanged =>
                entry.ForceId is { } statusForce
                    ? $"{ForceController(play, statusForce, names)}: {entry.Message ?? "status changed."}"
                    : entry.Message ?? $"{actor} recorded a force status change.",
            PlayLogKind.AllianceBetrayed =>
                FormatAllianceBetrayed(entry, actor, territory, campaign, play, names),
            PlayLogKind.RandomTeleportPreparing =>
                $"{(entry.ForceId is { } preparingId ? ForceController(play, preparingId, names) : actor)}'s force at {territory} is preparing to teleport to a random location.",
            _ => $"{actor} recorded a campaign change in {territory}.",
        };
    }

    private static string FormatRivalObjectiveRevealed(
        PlayLogEntry entry,
        string actor,
        IReadOnlyDictionary<Guid, string> names)
    {
        var rivalLabel = Guid.TryParseExact(entry.Message, "N", out var rivalId) || Guid.TryParse(entry.Message, out rivalId)
            ? ActorName(rivalId, names)
            : null;
        return rivalLabel is null
            ? $"{actor} defeated their secret rival."
            : $"{actor} defeated their secret rival {rivalLabel}.";
    }

    private static string ScheduleExtendedSummary(string actor, string? message)
    {
        var body = string.IsNullOrWhiteSpace(message)
            ? "lengthened remaining phases or added rounds."
            : message.Trim();
        var who = actor == "A force" ? "A manager" : actor;
        return $"{who} {body}";
    }

    private static string FormatActionCancelled(
        PlayLogEntry entry,
        string actor,
        string territory,
        StoredCampaign campaign,
        PlayMap map,
        IReadOnlyDictionary<Guid, string> names)
    {
        var action = ActionKindLabel(entry.ActionKind);
        if (!PlayLogFacts.TryReadActionCancelled(entry.Message, out var reason, out var interrupterUserId, out var placeTerritoryId))
        {
            return $"{actor}'s force at {territory} action {action} was cancelled because the action was interrupted.";
        }

        var interrupter = ActorName(interrupterUserId, names);
        var place = TerritoryLabel(campaign, map, placeTerritoryId);
        return reason switch
        {
            PlayLogFacts.InterruptBackstab =>
                $"{actor}'s force at {territory} action {action} was cancelled because treacherous player {interrupter} backstabbed {actor} at {place}.",
            _ =>
                $"{actor}'s force at {territory} action {action} was cancelled because enemy player {interrupter} moved into {place}.",
        };
    }

    private static string ActionKindLabel(ActionKind? kind)
    {
        return kind switch
        {
            ActionKind.TeleportRandomly => "Teleport Randomly",
            ActionKind.TeleportToSpecificTerritory => "Teleport to Specific Territory",
            ActionKind.Teleport => "Teleport",
            null => "Hold",
            { } value => value.ToString(),
        };
    }

    private static string FormatResolvedAction(PlayLogEntry entry, string actor, string territory, string target)
    {
        var structure = PlayLogFacts.TryReadDestroyedStructure(entry.Message, out var destroyed)
            ? destroyed
            : string.IsNullOrWhiteSpace(entry.Message) ? "structure" : entry.Message;
        return entry.ActionKind switch
        {
            ActionKind.Move => $"{actor} moved force at {territory} to {target}.",
            ActionKind.Hold => $"{actor} held in {territory}.",
            ActionKind.Split => $"{actor} split force at {territory} into {target}.",
            ActionKind.Build => $"{actor} built a {structure} at {territory}.",
            ActionKind.Repair => $"{actor} repaired the {structure} at {territory}.",
            ActionKind.Pillage when PlayLogFacts.TryReadDestroyedStructure(entry.Message, out _) =>
                $"{actor} destroyed {structure} at {territory}.",
            ActionKind.Pillage => $"{actor} pillaged {structure} at {territory}.",
            ActionKind.Retreat => $"{actor} retreated force at {territory} to {target}.",
            ActionKind.TeleportRandomly =>
                entry.TargetTerritoryId is null || entry.TargetTerritoryId == entry.TerritoryId
                    ? $"{actor} prepared to teleport randomly from {territory}."
                    : $"{actor} teleported randomly from {territory} to {target}.",
            ActionKind.TeleportToSpecificTerritory => $"{actor} teleported from {territory} to {target}.",
            ActionKind.Teleport =>
                entry.TargetTerritoryId is null || entry.TargetTerritoryId == entry.TerritoryId
                    ? $"{actor} prepared to teleport randomly from {territory}."
                    : $"{actor} teleported from {territory} to {target}.",
            _ => $"{actor} resolved {entry.ActionKind?.ToString() ?? "Hold"} in {territory}"
                + (entry.TargetTerritoryId is null || entry.TargetTerritoryId == entry.TerritoryId
                    ? "."
                    : $" toward {target}."),
        };
    }

    private static string FormatAllianceBetrayed(
        PlayLogEntry entry,
        string actor,
        string territory,
        StoredCampaign campaign,
        CampaignPlayState play,
        IReadOnlyDictionary<Guid, string> names)
    {
        if (!PlayLogFacts.TryReadBetrayal(entry.Message, out var kind, out var victimUserId, out var factionId, out var structureName)
            && entry.RelatedForceIds.Count > 0)
        {
            var victimForce = play.Forces.FirstOrDefault(item => item.Id == entry.RelatedForceIds[0]);
            kind = PlayLogFacts.BetrayalAttack;
            victimUserId = victimForce?.ControllerUserId;
            factionId = victimForce?.FactionId ?? default;
            structureName = null;
        }

        if (kind is null)
        {
            return $"{actor} betrayed an ally in {territory}.";
        }

        var victim = victimUserId is { } id
            ? ActorName(id, names)
            : PlayerOfFaction(campaign, play, factionId, names);
        var faction = FactionName(campaign, factionId);
        var structure = string.IsNullOrWhiteSpace(structureName) ? "structure" : structureName;
        return kind switch
        {
            PlayLogFacts.BetrayalAttack =>
                $"Treachery! {actor} launched a surprise attack against {victim} at {territory}. They are now locked in battle. {actor} is no longer allies with {faction}.",
            PlayLogFacts.BetrayalPillage =>
                $"Treachery! {actor} betrayed {victim} and pillaged the {structure} at {territory}. {actor} is no longer allies with {faction}.",
            PlayLogFacts.BetrayalDestroy =>
                $"Treachery! {actor} betrayed {victim} and destroyed the {structure} at {territory}. {actor} is no longer allies with {faction}.",
            PlayLogFacts.BetrayalClaim =>
                $"Treachery! {actor} claimed the land held by their ally {victim}. {actor} is no longer allies with {faction}.",
            _ => $"{actor} betrayed an ally in {territory}.",
        };
    }

    private static string FactionName(StoredCampaign campaign, Guid factionId)
    {
        var faction = campaign.Factions.FirstOrDefault(item => item.Id == factionId);
        return string.IsNullOrWhiteSpace(faction?.Name) ? "a faction" : faction.Name;
    }

    private static string PlayerOfFaction(
        StoredCampaign campaign,
        CampaignPlayState play,
        Guid factionId,
        IReadOnlyDictionary<Guid, string> names)
    {
        var member = campaign.Memberships.FirstOrDefault(item => item.FactionId == factionId);
        if (member is not null)
        {
            return ActorName(member.UserId, names);
        }

        var force = play.Forces.FirstOrDefault(item => item.FactionId == factionId);
        return ActorName(force?.ControllerUserId, names);
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

    private static IReadOnlyList<string> BattleRemindersFor(
        StoredCampaign campaign,
        CampaignForce force,
        SpecialRuleContext rules,
        PlayMap map,
        CampaignPlayState play)
    {
        return
        [
            .. campaign.SpecialRules
                .Where(rule => rules.HeldItemHas(force, rule.Id) && !string.IsNullOrWhiteSpace(rule.Text))
                .Select(static rule => $"{rule.Name}: {rule.Text}"),
            .. ItemObjectiveEffectRules.CustomReminders(force, map, play.ItemObjectives, rules),
        ];
    }

    private static PlayerSupplyViewDetail ToForceSupply(
        CampaignPlayState play,
        PlayMap map,
        StoredCampaign campaign,
        CampaignForce force,
        PhaseWindow? window)
    {
        var round = window?.RoundNumber
            ?? (play.Windows.Count > 0 ? play.Windows[^1].RoundNumber : 1);
        var snapshot = SupplyRules.ForForce(play, map, CampaignPlayCatalog.Supply(campaign), force, round);
        snapshot = snapshot with
        {
            MaxArmyPoints = ItemObjectiveEffectRules.AdjustArmyPoints(
                snapshot.MaxArmyPoints,
                force,
                map,
                play.ItemObjectives,
                CampaignPlayCatalog.SpecialRules(campaign)),
        };
        return new PlayerSupplyViewDetail
        {
            CurrentSupplyPoints = snapshot.ForceAllowancePoints,
            TemporarySupplyPoints = 0,
            MapSupplyPoints = snapshot.MapSupplyPoints,
            RoundFreeSupplyPoints = snapshot.RoundFreeSupplyPoints,
            SplitPenaltyPoints = snapshot.SplitPenaltyPoints,
            ForceAllowancePoints = snapshot.ForceAllowancePoints,
            Contributions = ToContributions(snapshot, campaign),
        };
    }

    private static PlayerSupplyViewDetail? ToViewerSupply(
        CampaignPlayState play,
        PlayMap map,
        StoredCampaign campaign,
        Guid viewerUserId,
        bool isPlayer,
        PhaseWindow? window)
    {
        if (!isPlayer || play.Forces.Count == 0)
        {
            return null;
        }

        var round = window?.RoundNumber
            ?? (play.Windows.Count > 0 ? play.Windows[^1].RoundNumber : 1);
        var snapshot = SupplyRules.ForPlayer(play, map, CampaignPlayCatalog.Supply(campaign), viewerUserId, round);
        return new PlayerSupplyViewDetail
        {
            CurrentSupplyPoints = snapshot.CurrentSupplyPoints,
            TemporarySupplyPoints = snapshot.TemporarySupplyPoints,
            MapSupplyPoints = snapshot.MapSupplyPoints,
            RoundFreeSupplyPoints = snapshot.RoundFreeSupplyPoints,
            SplitPenaltyPoints = snapshot.SplitPenaltyPoints,
            ForceAllowancePoints = snapshot.ForceAllowancePoints,
            Contributions = ToContributions(snapshot, campaign),
        };
    }

    private static IReadOnlyList<SupplyContributionDetail> ToContributions(
        PlayerSupplySnapshot snapshot,
        StoredCampaign campaign)
    {
        var territories = campaign.MapGraph?.Territories.ToDictionary(static item => item.Id) ?? [];
        var terrains = campaign.TerrainTypes.ToDictionary(static item => item.Id);
        var specialNames = campaign.SpecialRules
            .Where(static rule => !string.IsNullOrWhiteSpace(rule.EffectKey))
            .GroupBy(static rule => rule.EffectKey!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static group => group.Key, static group => group.First().Name, StringComparer.OrdinalIgnoreCase);
        return
        [
            .. snapshot.Contributions.Select(item =>
            {
                territories.TryGetValue(item.TerritoryId ?? Guid.Empty, out var territory);
                var place = TerritoryPlace(territory, item.TerritoryId);
                var label = item.Kind switch
                {
                    SupplyContributionKind.TerritoryTerrain => TerrainLabel(place, territory, terrains),
                    SupplyContributionKind.TerritoryStructure => StructureLabel(place, item.SourceName),
                    SupplyContributionKind.SpecialRule => SpecialLabel(item.SourceName, place, specialNames),
                    _ => item.SourceName,
                };
                if (item.IsAllied && item.Kind is SupplyContributionKind.TerritoryTerrain or SupplyContributionKind.TerritoryStructure)
                {
                    label = $"Allied {label}";
                }

                return new SupplyContributionDetail
                {
                    Kind = item.Kind.ToString(),
                    TerritoryId = item.TerritoryId,
                    Label = label,
                    Points = item.Points,
                    IsAllied = item.IsAllied,
                };
            }),
        ];
    }

    private static string TerritoryPlace(Maps.TerritoryDetail? territory, Guid? territoryId)
    {
        if (territory is not null)
        {
            return string.IsNullOrWhiteSpace(territory.Name) ? $"Territory {territory.DisplayNumber}" : territory.Name;
        }

        return "Unknown territory";
    }

    private static string TerrainLabel(
        string place,
        Maps.TerritoryDetail? territory,
        Dictionary<Guid, StoredTerrainType> terrains)
    {
        if (territory is not null && terrains.TryGetValue(territory.TerrainTypeId, out var terrain))
        {
            return $"{place} terrain ({terrain.Name})";
        }

        return $"{place} terrain";
    }

    private static string StructureLabel(string place, string sourceName)
    {
        return string.IsNullOrWhiteSpace(sourceName) ? place : $"{place} {sourceName}";
    }

    private static string SpecialLabel(
        string sourceName,
        string place,
        IReadOnlyDictionary<string, string> specialNames)
    {
        var rule = specialNames.GetValueOrDefault(sourceName) ?? sourceName;
        return $"{rule} ({place})";
    }
}

/// <summary>
/// Accounts and administrator flags for one campaign read, resolved once per request.
/// </summary>
/// <param name="Accounts">Accounts for memberships and play-log actors, keyed by identifier.</param>
/// <param name="AdministratorIds">Which members hold the system Administrator role.</param>
internal sealed record CampaignAccountContext(
    IReadOnlyDictionary<Guid, UserAccount> Accounts,
    IReadOnlySet<Guid> AdministratorIds)
{
    /// <summary>Gets an empty context, used when no account store is available.</summary>
    public static CampaignAccountContext Empty { get; } =
        new(new Dictionary<Guid, UserAccount>(), new HashSet<Guid>());
}
