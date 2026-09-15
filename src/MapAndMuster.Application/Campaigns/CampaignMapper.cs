using MapAndMuster.Application.Play;
using MapAndMuster.Domain.Campaigns;
using MapAndMuster.Domain.Identity;
using MapAndMuster.Domain.Play;

namespace MapAndMuster.Application.Campaigns;

/// <summary>
/// Maps stored campaigns onto member-visible models. Join password hashes are omitted.
/// </summary>
public static class CampaignMapper
{
    /// <summary>
    /// Maps a stored campaign onto a list item for the specified viewer.
    /// </summary>
    /// <param name="campaign">The stored campaign.</param>
    /// <param name="viewerUserId">The viewing user's identifier.</param>
    /// <param name="utcNow">The current UTC instant.</param>
    /// <param name="isAdministrator">Whether the caller is a system administrator.</param>
    /// <returns>The list item.</returns>
    public static CampaignListItem ToListItem(
        StoredCampaign campaign,
        Guid viewerUserId,
        DateTimeOffset utcNow,
        bool isAdministrator = false)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        var membership = MembershipFor(campaign, viewerUserId);
        var progress = CampaignLifecycle.Progress(campaign, utcNow);
        return new CampaignListItem
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Description = campaign.Description,
            PlayerSlotCount = campaign.PlayerSlotCount,
            OccupiedPlayerSlots = OccupiedPlayerSlots(campaign),
            IsPrivate = campaign.IsPrivate,
            IsPubliclyViewable = campaign.IsPubliclyViewable,
            CanManage = membership?.IsGameMaster == true || isAdministrator,
            IsParticipant = membership?.IsPlayer == true,
            CanView = CampaignAccess.CanView(campaign, viewerUserId, isAdministrator),
            CanJoin = CampaignAccess.CanJoin(campaign, viewerUserId, utcNow),
            CanLeave = CampaignAccess.CanLeave(campaign, viewerUserId),
            City = campaign.City,
            Region = campaign.Region,
            Country = campaign.Country,
            Status = progress.Status.ToString(),
            StartsUtc = campaign.StartsUtc,
            EndsUtc = campaign.ClosedUtc ?? campaign.EndsUtc,
            CurrentRound = progress.CurrentRound,
            CurrentPhaseLabel = FormatCurrentPhaseLabel(campaign, progress),
            CurrentPhaseKind = progress.CurrentPhaseKind?.ToString(),
            CurrentPhaseEndsUtc = progress.CurrentPhaseEndsUtc,
            CanPlay = (membership?.IsPlayer == true || membership?.IsGameMaster == true)
                && progress.Status == CampaignStatus.InProgress,
            CanChooseFaction = CanChooseFaction(membership, progress.Status),
            IsCommitted = ViewerIsCommitted(campaign, viewerUserId),
        };
    }

    /// <summary>
    /// Maps a stored campaign onto a member detail for the specified viewer.
    /// </summary>
    /// <param name="campaign">The stored campaign.</param>
    /// <param name="viewerUserId">The viewing user's identifier.</param>
    /// <param name="utcNow">The current UTC instant.</param>
    /// <param name="log">The public campaign log, when already mapped.</param>
    /// <param name="mentionableMembers">Current members who may be tagged in chat.</param>
    /// <param name="chatChannels">Compose targets for the viewer.</param>
    /// <param name="inspectPrivateChat">Whether the viewer may inspect all private chats.</param>
    /// <param name="participants">Members attached to the campaign, when already mapped.</param>
    /// <param name="staffView">Whether hidden item objectives are visible to the viewer.</param>
    /// <param name="isAdministrator">Whether the caller is a system administrator.</param>
    /// <returns>The detail.</returns>
    public static CampaignDetail ToDetail(
        StoredCampaign campaign,
        Guid viewerUserId,
        DateTimeOffset utcNow,
        IReadOnlyList<PlayLogEntryDetail>? log = null,
        IReadOnlyList<CampaignLogMemberDetail>? mentionableMembers = null,
        IReadOnlyList<ChatChannelDetail>? chatChannels = null,
        bool inspectPrivateChat = false,
        IReadOnlyList<CampaignParticipantDetail>? participants = null,
        bool staffView = false,
        bool isAdministrator = false)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        var membership = MembershipFor(campaign, viewerUserId);
        var schedule = ToSchedule(campaign);
        var progress = CampaignLifecycle.Progress(campaign, utcNow);
        var mappedParticipants = participants ?? [];
        var scoring = CampaignPointStandingsMapper.ToScoring(campaign, mappedParticipants, viewerUserId, staffView, utcNow);
        var canStaff = membership?.IsGameMaster == true || isAdministrator;
        var completed = progress.Status == CampaignStatus.Completed;
        var viewerAllyGroupId = ViewerAllyGroupId(campaign, membership?.FactionId);
        return new CampaignDetail
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Description = campaign.Description,
            PlayerSlotCount = campaign.PlayerSlotCount,
            OccupiedPlayerSlots = OccupiedPlayerSlots(campaign),
            IsPrivate = campaign.IsPrivate,
            IsPubliclyViewable = campaign.IsPubliclyViewable,
            CreatorIsParticipant = campaign.CreatorIsParticipant,
            City = campaign.City,
            Region = campaign.Region,
            Country = campaign.Country,
            HasMap = HasMapData(campaign),
            AssetTags = CampaignAssetTagMap.Build(campaign),
            CanManage = membership?.IsGameMaster == true || isAdministrator,
            IsParticipant = membership?.IsPlayer == true,
            Revision = campaign.Revision,
            CreatedUtc = campaign.CreatedUtc,
            UpdatedUtc = campaign.UpdatedUtc,
            Factions = [.. campaign.Factions.Select(faction => new FactionDetail
            {
                Id = faction.Id,
                Name = faction.Name,
                Color = faction.Color,
                Subfactions = faction.Subfactions,
                AllyGroupName = faction.AllyGroupName,
                AllyGroupId = AllyGroupIdFor(campaign, faction.AllyGroupName),
                RequiresSubfaction = faction.RequiresSubfaction,
                HasFlagImage = !string.IsNullOrWhiteSpace(faction.FlagImageStorageKey),
                TintFlagImage = faction.TintFlagImage,
                SpecialRuleIds = faction.SpecialRuleIds,
                SubfactionSpecialRules = faction.SubfactionSpecialRules
                    .Select(static item => new SubfactionSpecialRulesDetail
                    {
                        Name = item.Name,
                        SpecialRuleIds = item.SpecialRuleIds,
                    })
                    .ToArray(),
                TagIds = faction.TagIds,
                SubfactionTags = faction.SubfactionTags,
                SubfactionAppearances = faction.SubfactionAppearances
                    .Select(static item => new SubfactionAppearanceDetail
                    {
                        Name = item.Name,
                        Color = item.Color,
                        FlagSource = item.FlagSource,
                        HasFlagImage = !string.IsNullOrWhiteSpace(item.FlagImageStorageKey),
                        TintFlagImage = item.TintFlagImage,
                    })
                    .ToArray(),
                ForceMovementSpeed = faction.ForceMovementSpeed,
                SubfactionMovementSpeeds = faction.SubfactionMovementSpeeds,
            })],
            TerrainTypes = [.. campaign.TerrainTypes.Select(static type => new TerrainTypeDetail
            {
                Id = type.Id,
                Name = type.Name,
                Color = type.Color,
                Missions = [.. type.Missions.Select(ToMission)],
                CampaignPoints = type.CampaignPoints,
                TagIds = type.TagIds,
                SupplyPoints = type.SupplyPoints,
            })],
            StructureTypes = [.. campaign.StructureTypes.Select(static type => new StructureTypeDetail
            {
                Id = type.Id,
                Name = type.Name,
                BuiltinSymbol = type.ImageStorageKey is null ? type.BuiltinSymbol : null,
                HasImage = !string.IsNullOrWhiteSpace(type.ImageStorageKey),
                HasPillagedImage = !string.IsNullOrWhiteSpace(type.PillagedImageStorageKey),
                IsBuildable = type.IsBuildable,
                IsPillageable = type.IsPillageable,
                IsDestructible = type.IsDestructible,
                Missions = [.. type.Missions.Select(ToMission)],
                CampaignPoints = type.CampaignPoints,
                SupplyPoints = type.SupplyPoints,
                PillageSupplyPoints = type.PillageSupplyPoints,
                DestroySupplyPoints = type.DestroySupplyPoints,
                TagIds = type.TagIds,
            })],
            ItemObjectiveTypes = [.. campaign.ItemObjectiveTypes.Select(type => ToItemObjectiveType(type, canStaff))],
            PublicObjectiveTypes = [.. campaign.PublicObjectiveTypes.Select(static type => new PublicObjectiveTypeDetail
            {
                Id = type.Id,
                Name = type.Name,
                Description = type.Description,
                CampaignPoints = type.CampaignPoints,
            })],
            SpecialRules = [.. campaign.SpecialRules.Select(static rule => new SpecialRuleDetail
            {
                Id = rule.Id,
                Name = rule.Name,
                Text = rule.Text,
                EffectKey = rule.EffectKey,
            })],
            Missions = [.. CatalogMissions(campaign).Select(ToMission)],
            TerrainTags = [.. campaign.TerrainTags.Select(ToTag)],
            StructureTags = [.. campaign.StructureTags.Select(ToTag)],
            FactionTags = [.. campaign.FactionTags.Select(ToTag)],
            MissionTags = [.. campaign.MissionTags.Select(ToTag)],
            ForceStatuses = [.. campaign.ForceStatuses.Select(static status => new ForceStatusDetail
            {
                Id = status.Id,
                Name = status.Name,
                Effects = status.Effects,
                EnableTrigger = status.EnableTrigger,
                ClearTrigger = status.ClearTrigger,
                EnableConditions = DetailConditions(status.EnableConditions, status.EnableTrigger, status.EnableOccurrences),
                ClearConditions = DetailConditions(status.ClearConditions, status.ClearTrigger, status.ClearOccurrences),
                Priority = status.Priority,
                CancelsStatusIds = status.CancelsStatusIds,
                EnableOccurrences = status.EnableOccurrences,
                ClearOccurrences = status.ClearOccurrences,
                ImmuneFactionIds = status.ImmuneFactionIds,
                ImmuneSubfactions =
                [
                    .. status.ImmuneSubfactions.Select(static item => new ForceStatusImmuneSubfactionDetail
                    {
                        FactionId = item.FactionId,
                        Subfaction = item.Subfaction,
                    }),
                ],
                HasTokenImage = !string.IsNullOrWhiteSpace(status.TokenImageStorageKey),
            })],
            PrivateObjectiveTypes = VisiblePrivateTypes(campaign, viewerUserId, membership?.FactionId, viewerAllyGroupId, canStaff, completed, membership?.Subfaction),
            PrivateObjectives = VisiblePrivateAssignments(campaign, viewerUserId, membership?.FactionId, viewerAllyGroupId, canStaff, completed, membership?.Subfaction),
            PrivateObjectiveUnclaimedCounts = UnclaimedCounts(campaign, mappedParticipants),
            RivalObjectives = VisibleRivalObjectives(campaign, viewerUserId, canStaff, completed, mappedParticipants),
            RivalObjectivesEnabled = campaign.RivalObjectivesEnabled,
            RivalObjectiveCampaignPoints = campaign.RivalObjectiveCampaignPoints,
            PointsPerBattleWon = campaign.BattleScoring.PointsPerWin,
            PointsPerBattleDraw = campaign.BattleScoring.PointsPerDraw,
            UseDifferentialBattleScoring = campaign.BattleScoring.UseDifferential,
            DifferentialMultiplier = campaign.BattleScoring.DifferentialMultiplier,
            DifferentialMinimum = campaign.BattleScoring.DifferentialMinimum,
            DifferentialMaximum = campaign.BattleScoring.DifferentialMaximum,
            AllowNegativeDifferential = campaign.BattleScoring.AllowNegativeDifferential,
            MostTerritoriesCampaignPoints = campaign.RankingObjectivePoints.MostTerritories,
            LongestTerritoryChainCampaignPoints = campaign.RankingObjectivePoints.LongestTerritoryChain,
            MostBattlesWonCampaignPoints = campaign.RankingObjectivePoints.MostBattlesWon,
            MostStructurePointsCampaignPoints = campaign.RankingObjectivePoints.MostStructurePoints,
            PointsPerTerritoryCampaignPoints = campaign.RankingObjectivePoints.PointsPerTerritory,
            AlliedRelicControlCampaignPoints = campaign.RankingObjectivePoints.AlliedRelicControlPoints,
            MostTerritoriesTerrainTagId = campaign.RankingObjectivePoints.MostTerritoriesTerrainTagId,
            LongestTerritoryChainTerrainTagId = campaign.RankingObjectivePoints.LongestTerritoryChainTerrainTagId,
            MostStructurePointsStructureTagId = campaign.RankingObjectivePoints.MostStructurePointsStructureTagId,
            PointsPerTerritoryTerrainTagId = campaign.RankingObjectivePoints.PointsPerTerritoryTerrainTagId,
            SplitForceSupplyPenaltyPercent = campaign.SplitForceSupplyPenaltyPercent,
            SplitForceSupplyPenaltyIsPercent = campaign.SplitForceSupplyPenaltyIsPercent,
            StandardBattleResultQuestions =
            [
                .. campaign.StandardBattleResultQuestions.Select(static question => new StandardBattleResultQuestionDetail
                {
                    Id = question.Id,
                    Prompt = question.Prompt,
                    Kind = question.Kind,
                    BattlePoints = question.BattlePoints,
                    CampaignPoints = question.CampaignPoints,
                }),
            ],
            RoundEscalations =
            [
                .. schedule.ArmyEscalations.Select(static row => new RoundArmyEscalationDetail
                {
                    RoundNumber = row.RoundNumber,
                    MaxArmyPoints = row.MaxArmyPoints,
                    FreeSupplyPoints = row.FreeSupplyPoints,
                    FreeCharacterCount = row.FreeCharacterCount,
                }),
            ],
            BrokenAllyFactionIds = campaign.PlayState?.BrokenAllyFactionIds ?? [],
            AllyGroups = [.. campaign.AllyGroups.Select(static group => new AllyGroupDetail
            {
                Id = group.Id,
                Name = group.Name,
                Color = group.Color,
            })],
            Links = [.. campaign.Links.Select(static link => new CampaignLinkDetail
            {
                Id = link.Id,
                Label = link.Label,
                Url = link.Url,
            })],
            TimeZoneId = schedule.TimeZone.Id,
            StartsAtLocal = schedule.StartsAtLocal,
            StartsUtc = schedule.StartsUtc,
            EndsUtc = schedule.EndsUtc,
            RoundCount = schedule.RoundCount,
            RoundLengthAmount = schedule.RoundLength.Amount,
            RoundLengthUnit = schedule.RoundLength.Unit.ToString(),
            Phases =
            [
                .. schedule.Phases.Select(static phase => new RoundPhaseDetail
                {
                    Kind = phase.Kind.ToString(),
                    DurationAmount = phase.Duration.Amount,
                    DurationUnit = phase.Duration.Unit.ToString(),
                    EndPhaseEarlyIfAble = phase.EndPhaseEarlyIfAble,
                }),
            ],
            Status = progress.Status.ToString(),
            CurrentRound = progress.CurrentRound,
            CurrentPhaseNumber = progress.CurrentPhaseNumber,
            CurrentPhaseKind = progress.CurrentPhaseKind?.ToString(),
            CurrentPhaseStartsUtc = progress.CurrentPhaseStartsUtc,
            CurrentPhaseEndsUtc = progress.CurrentPhaseEndsUtc,
            FactionId = membership?.FactionId,
            Subfaction = membership?.Subfaction,
            CanPlay = (membership?.IsPlayer == true || membership?.IsGameMaster == true)
                && progress.Status == CampaignStatus.InProgress,
            CanChooseFaction = CanChooseFaction(membership, progress.Status),
            CanChat = membership is not null,
            CanInspectPrivateChat = inspectPrivateChat,
            Participants = mappedParticipants,
            MentionableMembers = mentionableMembers ?? [],
            ChatChannels = chatChannels ?? [],
            Log = log ?? [],
            Standings = scoring.Standings,
            PublicObjectiveLeaderboards = scoring.Leaderboards,
        };
    }

    /// <summary>
    /// Counts memberships that occupy a player slot.
    /// </summary>
    /// <param name="campaign">The stored campaign.</param>
    /// <returns>The occupied slot count.</returns>
    public static int OccupiedPlayerSlots(StoredCampaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        return campaign.Memberships.Count(static membership => membership.IsPlayer);
    }

    /// <summary>
    /// Whether a player may still choose or change their faction.
    /// Players may change until the campaign starts; after launch a chosen faction is locked.
    /// </summary>
    /// <param name="membership">The viewer's membership, if any.</param>
    /// <param name="status">The campaign lifecycle status.</param>
    /// <returns><see langword="true"/> when the faction picker should be offered.</returns>
    internal static bool CanChooseFaction(StoredCampaignMembership? membership, CampaignStatus status)
    {
        if (membership?.IsPlayer != true || status == CampaignStatus.Completed)
        {
            return false;
        }

        return membership.FactionId is null || status == CampaignStatus.Scheduled;
    }

    /// <summary>
    /// Whether the viewer has committed required orders for the currently open action window.
    /// </summary>
    internal static bool ViewerIsCommitted(StoredCampaign campaign, Guid viewerUserId)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        var play = campaign.PlayState;
        if (play is null || play.CurrentWindow() is not { Kind: RoundPhaseKind.Action, Status: PhaseWindowStatus.Open } window)
        {
            return false;
        }

        return play.IsActionCommitted(window.Id, viewerUserId);
    }

    /// <summary>
    /// Returns the viewer's membership, if any.
    /// </summary>
    /// <param name="campaign">The stored campaign.</param>
    /// <param name="viewerUserId">The viewing user's identifier.</param>
    /// <returns>The membership, or <see langword="null"/>.</returns>
    public static StoredCampaignMembership? MembershipFor(StoredCampaign campaign, Guid viewerUserId)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        return campaign.Memberships.FirstOrDefault(membership => membership.UserId == viewerUserId);
    }

    private static ItemObjectiveTypeDetail ToItemObjectiveType(StoredItemObjectiveType type, bool includeSecrets)
    {
        return new ItemObjectiveTypeDetail
        {
            Id = type.Id,
            Name = type.Name,
            IsHiddenUntilFound = type.IsHiddenUntilFound,
            Placement = type.Placement,
            AllowOnSpawn = type.AllowOnSpawn,
            BuiltinSymbol = type.BuiltinSymbol,
            Color = type.Color,
            HasImage = !string.IsNullOrWhiteSpace(type.ImageStorageKey),
            CampaignPoints = type.CampaignPoints,
            FlavorText = includeSecrets ? type.FlavorText : null,
            Choices = includeSecrets
                ? [.. type.Choices.Select(static choice => new ItemObjectiveChoiceDetail
                {
                    Id = choice.Id,
                    Name = choice.Name,
                    Results =
                    [
                        .. choice.Results.Select(static result => new ItemObjectiveChoiceResultDetail
                        {
                            Id = result.Id,
                            FlavorText = result.FlavorText,
                            NewStateKey = result.NewStateKey,
                            DestroyItem = result.DestroyItem,
                            ReplacementItemTypeId = result.ReplacementItemTypeId,
                            GrantedPrivateObjectiveTypeId = result.GrantedPrivateObjectiveTypeId,
                            SetForceStatusName = result.SetForceStatusName,
                        }),
                    ],
                })]
                : [],
            SpecialRuleIds = type.SpecialRuleIds,
            Effects =
            [
                .. type.Effects.Select(static effect => new ItemObjectiveEffectDetail
                {
                    Id = effect.Id,
                    Kind = effect.Kind,
                    Amount = effect.Amount,
                    AmountIsPercent = effect.AmountIsPercent,
                    StatusTypeIds = effect.StatusTypeIds,
                    ImmuneToAllStatuses = effect.ImmuneToAllStatuses,
                    SuspendCurrentAllyGroup = effect.SuspendCurrentAllyGroup,
                    ForcedAllyGroupName = effect.ForcedAllyGroupName,
                    AlliedFactions =
                    [
                        .. effect.AlliedFactions.Select(static target => new ItemObjectiveAllianceTargetDetail
                        {
                            FactionId = target.FactionId,
                            Subfaction = target.Subfaction,
                        }),
                    ],
                    CustomText = effect.CustomText,
                    SuccessStatusTypeId = effect.SuccessStatusTypeId,
                    FailureStatusTypeId = effect.FailureStatusTypeId,
                }),
            ],
        };
    }

    private static IReadOnlyList<PrivateObjectiveTypeDetail> VisiblePrivateTypes(
        StoredCampaign campaign,
        Guid viewerUserId,
        Guid? viewerFactionId,
        Guid? viewerAllyGroupId,
        bool staffView,
        bool campaignCompleted,
        string? viewerSubfaction)
    {
        var assignments = campaign.PlayState?.PrivateObjectives ?? [];
        return
        [
            .. campaign.PrivateObjectiveTypes
                .Where(type => staffView
                    || assignments.Any(item =>
                        item.TypeId == type.Id
                        && PrivateObjectiveRules.CanViewDetails(
                            item,
                            viewerUserId,
                            viewerFactionId,
                            viewerAllyGroupId,
                            staffView,
                            campaignCompleted,
                            viewerSubfaction)))
                .Select(type =>
                {
                    var visible = staffView
                        || assignments.Any(item =>
                            item.TypeId == type.Id
                            && PrivateObjectiveRules.CanViewDetails(
                                item,
                                viewerUserId,
                                viewerFactionId,
                                viewerAllyGroupId,
                                staffView,
                                campaignCompleted,
                                viewerSubfaction));
                    return new PrivateObjectiveTypeDetail
                    {
                        Id = type.Id,
                        Name = visible ? type.Name : null,
                        Description = visible ? type.Description : null,
                        CampaignPoints = visible ? type.CampaignPoints : null,
                        AllowedHolderKinds = type.AllowedHolderKinds,
                        ScoringKind = type.ScoringKind,
                        AutomaticKind = visible ? type.AutomaticKind : null,
                        RequiredCount = type.RequiredCount,
                        StructureTypeId = visible ? type.StructureTypeId : null,
                        TerritoryIds = visible ? type.TerritoryIds : [],
                        MatchesAnyStructureType = visible && type.MatchesAnyStructureType,
                        ItemObjectiveTypeId = visible ? type.ItemObjectiveTypeId : null,
                        MatchesAnyItemObjective = visible && type.MatchesAnyItemObjective,
                        TargetKind = visible ? type.TargetKind : nameof(PrivateObjectiveTargetKind.None),
                        TargetSelection = visible ? type.TargetSelection : nameof(PrivateObjectiveTargetSelection.Specific),
                        TargetId = visible ? type.TargetId : null,
                        ForceStatusTypeIds = visible ? type.ForceStatusTypeIds : [],
                        StatusMatchKind = visible ? type.StatusMatchKind : nameof(PrivateObjectiveStatusMatchKind.None),
                        PrerequisiteForceStatusTypeId = visible ? type.PrerequisiteForceStatusTypeId : null,
                        PrerequisiteWasLost = visible && type.PrerequisiteWasLost,
                        StructureTagId = visible ? type.StructureTagId : null,
                        TerrainTagId = visible ? type.TerrainTagId : null,
                        ExcludedFactionIds = type.ExcludedFactionIds,
                        ExcludedAllyGroupIds = type.ExcludedAllyGroupIds,
                    };
                }),
        ];
    }

    private static IReadOnlyList<PrivateObjectiveAssignmentDetail> VisiblePrivateAssignments(
        StoredCampaign campaign,
        Guid viewerUserId,
        Guid? viewerFactionId,
        Guid? viewerAllyGroupId,
        bool staffView,
        bool campaignCompleted,
        string? viewerSubfaction)
    {
        var play = campaign.PlayState;
        if (play is null)
        {
            return [];
        }

        var types = campaign.PrivateObjectiveTypes.ToDictionary(static type => type.Id);
        var playRules = CampaignPlayCatalog.PrivateTypes(campaign).ToDictionary(static type => type.Id);
        var map = CampaignLifecycle.ToPlayMap(campaign);
        var territories = CampaignPlayCatalog.Territories(map);
        var factionByPlayer = CampaignPlayCatalog.FactionByPlayer(campaign);
        var allyGroupByFaction = CampaignPlayCatalog.AllyGroupByFaction(campaign);
        var subfactionByPlayer = CampaignPlayCatalog.SubfactionByPlayer(campaign);
        var brokenAllyFactionIds = play.BrokenAllyFactionIds.ToHashSet();
        return
        [
            .. play.PrivateObjectives.Select(assignment =>
            {
                types.TryGetValue(assignment.TypeId, out var type);
                playRules.TryGetValue(assignment.TypeId, out var rules);
                var visible = PrivateObjectiveRules.CanViewDetails(
                    assignment,
                    viewerUserId,
                    viewerFactionId,
                    viewerAllyGroupId,
                    staffView,
                    campaignCompleted,
                    viewerSubfaction);
                var progress = visible && rules is not null
                    ? PrivateObjectiveRules.AutomaticProgress(
                        assignment,
                        rules,
                        play,
                        territories,
                        factionByPlayer,
                        allyGroupByFaction,
                        brokenAllyFactionIds,
                        map,
                        subfactionByPlayer)
                    : null;
                return new PrivateObjectiveAssignmentDetail
                {
                    Id = assignment.Id,
                    TypeId = assignment.TypeId,
                    HolderKind = assignment.HolderKind.ToString(),
                    HolderId = assignment.HolderId,
                    HolderSubfaction = assignment.HolderSubfaction,
                    Status = assignment.Status.ToString(),
                    ScoringKind = assignment.ScoringKind.ToString(),
                    Name = visible ? type?.Name : null,
                    Description = visible ? type?.Description : null,
                    CampaignPoints = visible ? type?.CampaignPoints : null,
                    CurrentCount = visible ? progress?.Current : null,
                    RequiredCount = visible ? progress?.Required : null,
                    CanClaim = assignment.ScoringKind == PrivateObjectiveScoringKind.Manual
                        && assignment.Status == PrivateObjectiveAssignmentStatus.Assigned
                        && IsHolder(assignment, viewerUserId, viewerFactionId, viewerAllyGroupId, viewerSubfaction),
                    CanModerate = staffView
                        && assignment.ScoringKind == PrivateObjectiveScoringKind.Manual
                        && assignment.Status is PrivateObjectiveAssignmentStatus.Assigned or PrivateObjectiveAssignmentStatus.Claimed,
                };
            }),
        ];
    }

    private static IReadOnlyList<PrivateObjectiveUnclaimedCountDetail> UnclaimedCounts(
        StoredCampaign campaign,
        IReadOnlyList<CampaignParticipantDetail> participants)
    {
        var play = campaign.PlayState;
        if (play is null)
        {
            return [];
        }

        var names = new Dictionary<(PrivateObjectiveHolderKind Kind, Guid Id), string>();
        foreach (var participant in participants.Where(static item => item.IsPlayer))
        {
            names[(PrivateObjectiveHolderKind.Player, participant.UserId)] = participant.DisplayName;
            names[(PrivateObjectiveHolderKind.Traitor, participant.UserId)] = participant.DisplayName;
        }

        foreach (var faction in campaign.Factions)
        {
            names[(PrivateObjectiveHolderKind.Faction, faction.Id)] = faction.Name;
        }

        foreach (var group in campaign.AllyGroups)
        {
            names[(PrivateObjectiveHolderKind.AllyGroup, group.Id)] = group.Name;
        }

        return
        [
            .. PrivateObjectiveRules.UnclaimedCounts(play.PrivateObjectives)
                .Select(item =>
                {
                    var count = item.Count;
                    if (item.HolderKind == PrivateObjectiveHolderKind.Player
                        && play.RivalObjectives.Count(rival =>
                            rival.HolderUserId == item.HolderId && rival.IsActive) is { } rivalCount
                        && rivalCount > 0)
                    {
                        count += rivalCount;
                    }

                    return new PrivateObjectiveUnclaimedCountDetail
                    {
                        HolderKind = item.HolderKind.ToString(),
                        HolderId = item.HolderId,
                        HolderName = names.GetValueOrDefault((item.HolderKind, item.HolderId)) ?? "Unknown",
                        Count = count,
                    };
                }),
            .. play.RivalObjectives
                .Where(static rival => rival.IsActive)
                .Select(static rival => rival.HolderUserId)
                .Distinct()
                .Where(holderId => !play.PrivateObjectives.Any(item =>
                    item.HolderKind == PrivateObjectiveHolderKind.Player && item.HolderId == holderId))
                .Select(holderId => new PrivateObjectiveUnclaimedCountDetail
                {
                    HolderKind = nameof(PrivateObjectiveHolderKind.Player),
                    HolderId = holderId,
                    HolderName = names.GetValueOrDefault((PrivateObjectiveHolderKind.Player, holderId)) ?? "Unknown",
                    Count = play.RivalObjectives.Count(rival => rival.HolderUserId == holderId && rival.IsActive),
                }),
        ];
    }

    private static IReadOnlyList<RivalObjectiveAssignmentDetail> VisibleRivalObjectives(
        StoredCampaign campaign,
        Guid viewerUserId,
        bool staffView,
        bool campaignCompleted,
        IReadOnlyList<CampaignParticipantDetail> participants)
    {
        var play = campaign.PlayState;
        if (play is null || !campaign.RivalObjectivesEnabled)
        {
            return [];
        }

        var displayNames = participants.ToDictionary(static item => item.UserId, static item => item.DisplayName);
        return
        [
            .. play.RivalObjectives
                .Where(assignment =>
                    RivalObjectiveRules.CanViewDetails(assignment, viewerUserId, staffView, campaignCompleted)
                    || assignment.Status == PrivateObjectiveAssignmentStatus.Revealed
                    || assignment.HolderUserId == viewerUserId)
                .Select(assignment =>
                {
                    var visible = RivalObjectiveRules.CanViewDetails(
                        assignment,
                        viewerUserId,
                        staffView,
                        campaignCompleted);
                    var (factionName, subfaction) = visible
                        ? RivalFaction(campaign, participants, assignment.RivalUserId)
                        : (null, null);
                    return new RivalObjectiveAssignmentDetail
                    {
                        Id = assignment.Id,
                        HolderUserId = assignment.HolderUserId,
                        Status = assignment.Status.ToString(),
                        RivalUserId = visible ? assignment.RivalUserId : null,
                        RivalDisplayName = visible
                            ? displayNames.GetValueOrDefault(assignment.RivalUserId)
                            : null,
                        RivalFactionName = factionName,
                        RivalSubfaction = subfaction,
                        CampaignPoints = visible ? assignment.CampaignPoints : null,
                    };
                }),
        ];
    }

    private static (string? FactionName, string? Subfaction) RivalFaction(
        StoredCampaign campaign,
        IReadOnlyList<CampaignParticipantDetail> participants,
        Guid rivalUserId)
    {
        var participant = participants.FirstOrDefault(item => item.UserId == rivalUserId);
        var factionName = TrimToNull(participant?.FactionName);
        var subfaction = TrimToNull(participant?.Subfaction);
        if (factionName is not null)
        {
            return (factionName, subfaction);
        }

        var force = campaign.PlayState?.Forces.FirstOrDefault(item => item.ControllerUserId == rivalUserId);
        if (force is null)
        {
            return (null, subfaction);
        }

        var fromForce = campaign.Factions.FirstOrDefault(item => item.Id == force.FactionId)?.Name;
        return (TrimToNull(fromForce), subfaction ?? TrimToNull(force.Subfaction));
    }

    private static string? TrimToNull(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static bool IsHolder(
        PrivateObjectiveAssignment assignment,
        Guid viewerUserId,
        Guid? viewerFactionId,
        Guid? viewerAllyGroupId,
        string? viewerSubfaction)
    {
        return assignment.HolderKind switch
        {
            PrivateObjectiveHolderKind.Player => assignment.HolderId == viewerUserId,
            PrivateObjectiveHolderKind.Traitor => assignment.HolderId == viewerUserId,
            PrivateObjectiveHolderKind.Faction => viewerFactionId is { } faction
                && assignment.HolderId == faction
                && (string.IsNullOrWhiteSpace(assignment.HolderSubfaction)
                    || string.Equals(assignment.HolderSubfaction, viewerSubfaction, StringComparison.OrdinalIgnoreCase)),
            PrivateObjectiveHolderKind.AllyGroup => viewerAllyGroupId is { } group && assignment.HolderId == group,
            _ => false,
        };
    }

    private static Guid? ViewerAllyGroupId(StoredCampaign campaign, Guid? factionId)
    {
        if (factionId is not { } id)
        {
            return null;
        }

        var faction = campaign.Factions.FirstOrDefault(item => item.Id == id);
        return AllyGroupIdFor(campaign, faction?.AllyGroupName);
    }

    private static Guid? AllyGroupIdFor(StoredCampaign campaign, string? allyGroupName)
    {
        if (allyGroupName is not { } name)
        {
            return null;
        }

        return campaign.AllyGroups.FirstOrDefault(group =>
            string.Equals(group.Name, name, StringComparison.OrdinalIgnoreCase))?.Id;
    }

    private static string? FormatCurrentPhaseLabel(StoredCampaign campaign, CampaignProgress progress)
    {
        if (progress.Status != CampaignStatus.InProgress
            || progress.CurrentPhaseKind is null
            || progress.CurrentPhaseNumber is null)
        {
            return null;
        }

        var schedule = ToSchedule(campaign);
        return CampaignPhaseLabels.Format(schedule.Phases, progress.CurrentPhaseNumber.Value, progress.CurrentPhaseKind.Value);
    }

    /// <summary>
    /// Rebuilds the validated schedule from persistence fields.
    /// </summary>
    /// <param name="campaign">The stored campaign.</param>
    /// <returns>The schedule.</returns>
    public static CampaignSchedule ToSchedule(StoredCampaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        if (!IanaTimeZone.TryCreate(campaign.TimeZoneId, out var timeZone, out _))
        {
            timeZone = IanaTimeZone.TryCreate(IanaTimeZone.UtcId, out var utc, out _)
                ? utc
                : throw new InvalidOperationException("UTC is required.");
        }

        var roundLength = new ScheduleDuration(
            campaign.RoundLengthAmount,
            Enum.Parse<DurationUnit>(campaign.RoundLengthUnit, ignoreCase: true));
        var phases = campaign.Phases
            .Select(phase => new RoundPhaseSetup(
                Enum.Parse<RoundPhaseKind>(phase.Kind, ignoreCase: true),
                new ScheduleDuration(phase.DurationAmount, Enum.Parse<DurationUnit>(phase.DurationUnit, ignoreCase: true)),
                phase.EndPhaseEarlyIfAble))
            .ToArray();

        return new CampaignSchedule(
            timeZone,
            campaign.StartsUtc,
            campaign.EndsUtc,
            campaign.RoundCount,
            roundLength,
            phases,
            ArmyEscalationDefaults.PadToRoundCount(campaign.ArmyEscalations, Math.Max(1, campaign.RoundCount)));
    }

    internal static MissionDetail ToMission(StoredMission mission)
    {
        return new MissionDetail
        {
            Id = mission.Id,
            Name = mission.Name,
            Url = mission.Url,
            HasFile = !string.IsNullOrWhiteSpace(mission.FileStorageKey),
            FileName = mission.FileName,
            ResultQuestions =
            [
                .. mission.ResultQuestions.Select(static question => new MissionResultQuestionDetail
                {
                    Id = question.Id,
                    Prompt = question.Prompt,
                    Kind = question.Kind,
                    BattlePoints = question.BattlePoints,
                    CampaignPoints = question.CampaignPoints,
                    StandardQuestionId = question.StandardQuestionId,
                }),
            ],
            IsAttackerDefender = mission.IsAttackerDefender,
            HasArmyPointsAdvantage = mission.HasArmyPointsAdvantage,
            ArmyPointsAdvantageSide = mission.ArmyPointsAdvantageSide,
            ArmyPointsAdvantageIsPercent = mission.ArmyPointsAdvantageIsPercent,
            ArmyPointsAdvantageAmount = mission.ArmyPointsAdvantageAmount,
            HasSupplyPointsAdvantage = mission.HasSupplyPointsAdvantage,
            SupplyPointsAdvantageSide = mission.SupplyPointsAdvantageSide,
            SupplyPointsAdvantageAmount = mission.SupplyPointsAdvantageAmount,
            StatusChanges =
            [
                .. mission.StatusChanges.Select(static change => new MissionStatusChangeDetail
                {
                    Id = change.Id,
                    Outcome = change.Outcome,
                    WhenCurrentStatus = change.WhenCurrentStatus,
                    SetStatus = change.SetStatus,
                    LeaveUnchanged = change.LeaveUnchanged,
                }),
            ],
            TagIds = mission.TagIds,
        };
    }

    private static CatalogTagDetail ToTag(StoredCatalogTag tag)
    {
        return new CatalogTagDetail
        {
            Id = tag.Id,
            Name = tag.Name,
        };
    }

    private static IReadOnlyList<ForceStatusConditionDetail> DetailConditions(
        IReadOnlyList<StoredForceStatusCondition> listed,
        string trigger,
        int occurrences)
    {
        if (listed.Count > 0)
        {
            return
            [
                .. listed.Select(static condition => new ForceStatusConditionDetail
                {
                    Id = condition.Id,
                    Trigger = condition.Trigger,
                    Occurrences = ForceStatusOccurrences.Normalize(condition.Occurrences),
                    LocationKind = string.IsNullOrWhiteSpace(condition.LocationKind)
                        ? nameof(ConditionLocationKind.Any)
                        : condition.LocationKind,
                    LocationTypeId = condition.LocationTypeId,
                    LocationTagId = condition.LocationTagId,
                    RequiredStatusId = condition.RequiredStatusId,
                    RequiredQuestionId = condition.RequiredQuestionId,
                }),
            ];
        }

        if (string.IsNullOrWhiteSpace(trigger))
        {
            return [];
        }

        return
        [
            new ForceStatusConditionDetail
            {
                Trigger = trigger,
                Occurrences = ForceStatusOccurrences.Normalize(occurrences),
            },
        ];
    }

    internal static IReadOnlyList<StoredMission> CatalogMissions(StoredCampaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        if (campaign.Missions.Count > 0)
        {
            return campaign.Missions;
        }

        var seen = new Dictionary<Guid, StoredMission>();
        foreach (var mission in campaign.TerrainTypes.SelectMany(static type => type.Missions)
            .Concat(campaign.StructureTypes.SelectMany(static type => type.Missions)))
        {
            seen.TryAdd(mission.Id, mission);
        }

        return [.. seen.Values];
    }

    internal static bool HasMapData(StoredCampaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        return !string.IsNullOrWhiteSpace(campaign.MapStorageKey)
            || campaign.MapGraph?.Territories.Count > 0;
    }
}
