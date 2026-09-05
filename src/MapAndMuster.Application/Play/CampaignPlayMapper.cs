using MapAndMuster.Application.Campaigns;
using MapAndMuster.Application.Identity;
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
                        ? CampaignPlayRules.EligibleMoves(map, force, play.ItemObjectives, specialRules)
                        : [],
                    MoveHops = force.ControllerUserId == viewerUserId || staffView
                        ? [.. CampaignPlayRules.EligibleMoveHops(map, force, specialRules).Select(static hop => new PlayMoveHopDetail
                        {
                            ViaTerritoryId = hop.ViaTerritoryId,
                            TargetTerritoryId = hop.TargetTerritoryId,
                        })]
                        : [],
                    AvailableActions = (force.ControllerUserId == viewerUserId || staffView) && !force.InBattle
                        ? [.. ActionResolution.EligibleActions(play, map, force, allyGroups, specialRules).Select(static kind => kind.ToString())]
                        : [],
                    Subfaction = force.Subfaction,
                    CanMoveTwoTerritories = specialRules.Has(force, SpecialRuleEffectKeys.Crusaders),
                    CanDestroyImmediately = FactionSpecialRulePolicies.CanDestroyImmediately(force, specialRules),
                    CanUseExtraBlackPowder = specialRules.Has(force, SpecialRuleEffectKeys.PreparedForBattle),
                    CanUseMagicalSupply = specialRules.Has(force, SpecialRuleEffectKeys.MagicalSupply),
                    HiddenRelicNearby = FactionSpecialRulePolicies.HiddenRelicAdjacent(map, force, play.ItemObjectives, specialRules),
                    BattleReminders = BattleRemindersFor(campaign, force, specialRules),
                    Supply = force.ControllerUserId == viewerUserId || staffView
                        ? ToForceSupply(play, map, campaign, force, window)
                        : null,
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
                            DestroyImmediately = draft.DestroyImmediately,
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
                .. play.RequiredOrderPlayers(window.Id)
                    .Select(userId => new PlayCommitmentDetail
                    {
                        UserId = userId,
                        Username = names.GetValueOrDefault(userId),
                        IsCommitted = play.Commitments.Any(item => item.WindowId == window.Id && item.UserId == userId),
                    }),
            ];
        }

        if (window is { Kind: RoundPhaseKind.Battle, Status: PhaseWindowStatus.Open })
        {
            return
            [
                .. play.RequiredBattlePlayers(window.Id)
                    .Select(userId => new PlayCommitmentDetail
                    {
                        UserId = userId,
                        Username = names.GetValueOrDefault(userId),
                        IsCommitted = play.HasCompletedBattleDuties(window.Id, userId),
                    }),
            ];
        }

        return [];
    }

    private static bool ViewerIsCommitted(CampaignPlayState play, PhaseWindow? window, Guid viewerUserId)
    {
        if (window is { Kind: RoundPhaseKind.Action, Status: PhaseWindowStatus.Open })
        {
            return play.Commitments.Any(item => item.WindowId == window.Id && item.UserId == viewerUserId);
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
                    DestroyImmediately = draft.DestroyImmediately,
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
        var needsRetreat = myForce is not null
            && battle.Status is BattleStatus.Finalized or BattleStatus.GMResolved
            && !play.Retreats.Any(item => item.BattleId == battle.Id && item.ForceId == myForce.Id)
            && (battle.IsNoContest || battle.IsDraw || battle.WinnerForceId != myForce.Id);
        var canSurrender = myForce is not null
            && myForce.InBattle
            && battle.Status is not BattleStatus.Finalized and not BattleStatus.GMResolved
            && !battle.SurrenderedForceIds.Contains(myForce.Id)
            && !play.Retreats.Any(item => item.BattleId == battle.Id && item.ForceId == myForce.Id && item.IsSurrender);
        var round = play.CurrentWindow()?.RoundNumber
            ?? (play.Windows.Count > 0 ? play.Windows[^1].RoundNumber : 1);
        var catalog = CampaignPlayCatalog.Supply(campaign);
        var allies = campaign.Factions.ToDictionary(static faction => faction.Id, static faction => faction.AllyGroupName);
        var reportingForces = battle.ReportingForceIds
            .Select(forceId => play.Forces.FirstOrDefault(force => force.Id == forceId))
            .OfType<CampaignForce>()
            .ToArray();
        var sides = BattleMatchRules.Sides(reportingForces, allies, play.BrokenAllyFactionIds);
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
            WinnerForceId = battle.WinnerForceId,
            IsDraw = battle.IsDraw,
            WinnerScore = battle.WinnerScore,
            LoserScore = battle.LoserScore,
            NeedsRetreat = needsRetreat,
            CanSurrender = canSurrender,
            RetreatTargets = (needsRetreat || canSurrender) && myForce is not null
                ? CampaignPlayRules.EligibleRetreats(
                    map,
                    myForce,
                    CampaignPlayCatalog.SpecialRules(campaign),
                    play.Forces,
                    campaign.Factions.ToDictionary(static faction => faction.Id, static faction => faction.AllyGroupName),
                    play.BrokenAllyFactionIds)
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
            .. VisiblePlayLogEntries(campaign, viewerUserId, inspectPrivateChat)
                .Select(item => ToLogEntry(item, campaign, map, play, names)),
        ];
    }

    internal static IReadOnlyList<PlayLogEntry> VisiblePlayLogEntries(
        StoredCampaign campaign,
        Guid viewerUserId,
        bool inspectPrivateChat)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        var play = campaign.PlayState ?? CampaignPlayState.Empty;
        var memberships = CampaignChatContext.Memberships(campaign);
        return
        [
            .. play.Log
                .Where(entry => CampaignChatRules.CanView(entry, viewerUserId, memberships, inspectPrivateChat))
                .OrderBy(static item => item.OccurredUtc)
                .ThenBy(static item => item.Id),
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
            var appearance = faction is null
                ? null
                : FactionAppearance.Resolve(faction, membership.Subfaction);
            PlayerSupplySnapshot? supply = null;
            if (campaign.PlayState is { Forces.Count: > 0 } play && membership.IsPlayer)
            {
                var map = CampaignLifecycle.ToPlayMap(campaign);
                var round = play.CurrentWindow()?.RoundNumber
                    ?? (play.Windows.Count > 0 ? play.Windows[^1].RoundNumber : 1);
                supply = SupplyRules.ForPlayer(play, map, CampaignPlayCatalog.Supply(campaign), membership.UserId, round);
            }

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
            PlayLogKind.ResolvedAction =>
                $"{actor} resolved {action} in {territory}"
                    + (entry.TargetTerritoryId is null || entry.TargetTerritoryId == entry.TerritoryId
                        ? "."
                        : $" toward {target}."),
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
                $"{actor} retreated from {territory} to {target}.",
            PlayLogKind.PlayerSurrendered =>
                $"{actor} surrendered in {territory} and retreated to {target}.",
            PlayLogKind.RetreatCollisionResolved =>
                $"{actor} was displaced from {territory} to {target} after a retreat collision.",
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
            PlayLogKind.PrivateObjectiveRevealed =>
                $"{(entry.ActorUserId is null ? "A private objective" : actor + " revealed a private objective")}: {entry.Message ?? "a private objective"}.",
            PlayLogKind.ItemObjectiveDestroyed =>
                $"{actor} destroyed {entry.Message ?? "an item objective"}.",
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

    private static IReadOnlyList<string> BattleRemindersFor(
        StoredCampaign campaign,
        CampaignForce force,
        SpecialRuleContext rules)
    {
        return
        [
            .. campaign.SpecialRules
                .Where(rule => !string.IsNullOrWhiteSpace(rule.EffectKey)
                    && rules.Has(force, rule.EffectKey!)
                    && !string.IsNullOrWhiteSpace(rule.Text))
                .Select(static rule => $"{rule.Name}: {rule.Text}"),
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
