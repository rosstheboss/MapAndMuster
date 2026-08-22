using System.Diagnostics.CodeAnalysis;
using MapAndMuster.Application.Campaigns;
using MapAndMuster.Domain.Campaigns;
using MapAndMuster.Domain.Common;
using MapAndMuster.Domain.Play;

namespace MapAndMuster.Application.Play;

/// <summary>
/// Maps stored catalogs onto play-time private-objective and item-choice rules.
/// </summary>
internal static class CampaignPlayCatalog
{
    public static Func<int, int> PickIndex { get; } = static count => count <= 0 ? 0 : Random.Shared.Next(count);

    public static IReadOnlyList<PrivateObjectiveTypePlayRules> PrivateTypes(StoredCampaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        return
        [
            .. campaign.PrivateObjectiveTypes.Select(static type => new PrivateObjectiveTypePlayRules(
                type.Id,
                type.Name,
                type.CampaignPoints,
                [
                    .. type.AllowedHolderKinds
                        .Select(static kind => Enum.TryParse<PrivateObjectiveHolderKind>(kind, true, out var parsed)
                            ? parsed
                            : (PrivateObjectiveHolderKind?)null)
                        .OfType<PrivateObjectiveHolderKind>(),
                ],
                Enum.TryParse<PrivateObjectiveScoringKind>(type.ScoringKind, true, out var scoring)
                    ? scoring
                    : PrivateObjectiveScoringKind.Manual,
                Enum.TryParse<PrivateObjectiveAutomaticKind>(type.AutomaticKind, true, out var automatic)
                    ? automatic
                    : PrivateObjectiveAutomaticKind.None,
                type.RequiredCount,
                type.StructureTypeId,
                type.TerritoryIds)),
        ];
    }

    public static IReadOnlyList<ItemObjectiveTypePlayRules> ItemPlayRules(StoredCampaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        return
        [
            .. campaign.ItemObjectiveTypes.Select(static type => new ItemObjectiveTypePlayRules(
                type.Id,
                type.Name,
                type.IsHiddenUntilFound,
                Enum.TryParse<ItemObjectivePlacementKind>(type.Placement, true, out var placement)
                    ? placement
                    : ItemObjectivePlacementKind.Random,
                type.AllowOnSpawn,
                type.FlavorText)),
        ];
    }

    public static IReadOnlyList<ItemObjectiveTypeSetup> ItemSetups(StoredCampaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        return
        [
            .. campaign.ItemObjectiveTypes.Select(static type => new ItemObjectiveTypeSetup(
                type.Id,
                type.Name,
                type.IsHiddenUntilFound,
                Enum.TryParse<ItemObjectivePlacementKind>(type.Placement, true, out var placement)
                    ? placement
                    : ItemObjectivePlacementKind.Random,
                type.AllowOnSpawn,
                type.BuiltinSymbol,
                type.Color,
                clearImage: false,
                type.CampaignPoints,
                type.FlavorText,
                [
                    .. type.Choices.Select(static choice => new ItemObjectiveChoiceSetup(
                        choice.Id,
                        choice.Name,
                        [
                            .. choice.Results.Select(static result => new ItemObjectiveChoiceResultSetup(
                                result.Id,
                                result.FlavorText,
                                result.NewStateKey,
                                result.DestroyItem,
                                result.ReplacementItemTypeId,
                                result.GrantedPrivateObjectiveTypeId)),
                        ])),
                ],
                type.SpecialRuleIds)),
        ];
    }

    public static IReadOnlyDictionary<Guid, string> PrivateNames(StoredCampaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        return campaign.PrivateObjectiveTypes.ToDictionary(static type => type.Id, static type => type.Name);
    }

    public static IReadOnlyDictionary<Guid, Guid?> AllyGroupByFaction(StoredCampaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        var groups = campaign.AllyGroups.ToDictionary(static group => group.Name, static group => group.Id, StringComparer.Ordinal);
        return campaign.Factions.ToDictionary(
            static faction => faction.Id,
            faction => faction.AllyGroupName is { } name && groups.TryGetValue(name, out var id)
                ? id
                : (Guid?)null);
    }

    public static IReadOnlyDictionary<Guid, Guid> FactionByPlayer(StoredCampaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        return campaign.Memberships
            .Where(static member => member.IsPlayer && member.FactionId is not null)
            .ToDictionary(static member => member.UserId, static member => member.FactionId!.Value);
    }

    public static IReadOnlyList<PrivateObjectiveTerritory> Territories(PlayMap map)
    {
        ArgumentNullException.ThrowIfNull(map);
        return
        [
            .. map.Territories.Select(static territory => new PrivateObjectiveTerritory(
                territory.Id,
                territory.OwnerFactionId,
                territory.StructureTypeId,
                territory.StructureCondition)),
        ];
    }

    public static CampaignPlayState ApplyEffects(
        StoredCampaign campaign,
        CampaignPlayState state,
        PlayMap map,
        DateTimeOffset utcNow,
        DateTimeOffset endsUtc)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(map);
        var types = PrivateTypes(campaign);
        var next = state;
        foreach (var player in campaign.Memberships.Where(static member => member.IsPlayer).OrderBy(static member => member.UserId))
        {
            next = PrivateObjectiveRules.EnsurePlayerAssignment(next, types, player.UserId, utcNow, PickIndex);
        }

        next = PrivateObjectiveRules.EvaluateAutomatic(
            next,
            types,
            Territories(map),
            FactionByPlayer(campaign),
            AllyGroupByFaction(campaign),
            next.BrokenAllyFactionIds.ToHashSet(),
            utcNow);
        var progress = next.Evaluate(campaign.StartsUtc, endsUtc, utcNow);
        if (progress.Status == CampaignStatus.Completed)
        {
            next = PrivateObjectiveRules.RevealRemainingAtCompletion(next, PrivateNames(campaign), utcNow);
        }

        return next;
    }

    public static SupplyCatalog Supply(StoredCampaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        return new SupplyCatalog(
            campaign.TerrainTypes.ToDictionary(static type => type.Id, static type => type.SupplyPoints),
            campaign.StructureTypes.ToDictionary(
                static type => type.Id,
                static type => new StructureSupplyRules(type.SupplyPoints, type.PillageSupplyPoints, type.DestroySupplyPoints)),
            campaign.SplitForceSupplyPenaltyPercent,
            ArmyEscalationDefaults.PadToRoundCount(campaign.ArmyEscalations, Math.Max(1, campaign.RoundCount)),
            FactionByPlayer(campaign),
            campaign.Factions.ToDictionary(static faction => faction.Id, static faction => faction.AllyGroupName),
            campaign.PlayState?.BrokenAllyFactionIds.ToHashSet() ?? [],
            SpecialRules(campaign),
            SubfactionsByPlayer(campaign),
            campaign.SplitForceSupplyPenaltyIsPercent);
    }

    public static SpecialRuleContext SpecialRules(StoredCampaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        var catalog = campaign.SpecialRules
            .Select(static rule => new SpecialRuleSetup(rule.Id, rule.Name, rule.Text, rule.EffectKey))
            .ToArray();
        var factionIds = campaign.Factions.ToDictionary(
            static faction => faction.Id,
            static faction => (IReadOnlyList<Guid>)faction.SpecialRuleIds);
        var subfactionIds = new Dictionary<(Guid FactionId, string Subfaction), IReadOnlyList<Guid>>();
        foreach (var faction in campaign.Factions)
        {
            foreach (var assignment in faction.SubfactionSpecialRules)
            {
                subfactionIds[(faction.Id, assignment.Name)] = assignment.SpecialRuleIds;
            }
        }

        return new SpecialRuleContext(catalog, factionIds, subfactionIds);
    }

    public static IReadOnlyDictionary<Guid, string?> SubfactionsByPlayer(StoredCampaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        return campaign.Memberships
            .Where(static member => member.IsPlayer)
            .GroupBy(static member => member.UserId)
            .ToDictionary(static group => group.Key, static group => group.First().Subfaction);
    }

    public static IReadOnlyList<MissionResultQuestionSetup> MissionQuestions(
        StoredCampaign campaign,
        Guid territoryId,
        Guid? missionId = null)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        if (missionId is { } assignedId)
        {
            var assigned = FindMission(campaign, assignedId);
            return assigned is null ? [] : [.. assigned.ResultQuestions.Select(ToQuestion)];
        }

        var territory = campaign.MapGraph?.Territories.FirstOrDefault(item => item.Id == territoryId);
        if (territory is null)
        {
            return [];
        }

        var questions = new List<MissionResultQuestionSetup>();
        var terrain = campaign.TerrainTypes.FirstOrDefault(type => type.Id == territory.TerrainTypeId);
        if (terrain is not null)
        {
            questions.AddRange(terrain.Missions.SelectMany(static mission => mission.ResultQuestions).Select(ToQuestion));
        }

        var structureId = campaign.PlayState?.Structures
            .FirstOrDefault(item => item.TerritoryId == territoryId)
            ?.StructureTypeId
            ?? territory.StructureTypeId;
        var structure = structureId is { } id
            ? campaign.StructureTypes.FirstOrDefault(type => type.Id == id)
            : null;
        if (structure is not null)
        {
            questions.AddRange(structure.Missions.SelectMany(static mission => mission.ResultQuestions).Select(ToQuestion));
        }

        return questions;
    }

    public static StoredMission? FindMission(StoredCampaign campaign, Guid missionId)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        return CampaignMapper.CatalogMissions(campaign).FirstOrDefault(item => item.Id == missionId)
            ?? campaign.TerrainTypes.SelectMany(static type => type.Missions).FirstOrDefault(item => item.Id == missionId)
            ?? campaign.StructureTypes.SelectMany(static type => type.Missions).FirstOrDefault(item => item.Id == missionId);
    }

    public static IReadOnlyList<TerrainTypeSetup> TerrainSetups(StoredCampaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        return
        [
            .. campaign.TerrainTypes.Select(static type => new TerrainTypeSetup(
                type.Id,
                type.Name,
                type.Color,
                [.. type.Missions.Select(ToMissionSetup)],
                type.IsWaterFeature,
                type.SupplyPoints)),
        ];
    }

    public static IReadOnlyList<StructureTypeSetup> StructureSetups(StoredCampaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        return
        [
            .. campaign.StructureTypes.Select(static type => new StructureTypeSetup(
                type.Id,
                type.Name,
                type.BuiltinSymbol,
                false,
                false,
                type.IsBuildable,
                type.IsPillageable,
                type.IsDestructible,
                [.. type.Missions.Select(ToMissionSetup)],
                type.CampaignPoints,
                type.SupplyPoints,
                type.PillageSupplyPoints,
                type.DestroySupplyPoints)),
        ];
    }

    public static MissionSetup ToMissionSetup(StoredMission mission)
    {
        ArgumentNullException.ThrowIfNull(mission);
        return new MissionSetup(
            mission.Id,
            mission.Name,
            mission.Url,
            false,
            [.. mission.ResultQuestions.Select(ToQuestion)],
            mission.IsAttackerDefender,
            mission.HasArmyPointsAdvantage,
            Enum.TryParse<MissionAdvantageSide>(mission.ArmyPointsAdvantageSide, true, out var armySide)
                ? armySide
                : MissionAdvantageSide.Defender,
            mission.ArmyPointsAdvantageIsPercent,
            mission.ArmyPointsAdvantageAmount,
            mission.HasSupplyPointsAdvantage,
            Enum.TryParse<MissionAdvantageSide>(mission.SupplyPointsAdvantageSide, true, out var supplySide)
                ? supplySide
                : MissionAdvantageSide.Defender,
            mission.SupplyPointsAdvantageAmount);
    }

    public static bool TryToReports(
        IReadOnlyList<BattleParticipantReportInput>? reports,
        [NotNullWhen(true)] out IReadOnlyList<BattleParticipantReport>? mapped,
        [NotNullWhen(false)] out DomainError? error)
    {
        mapped = [];
        error = null;
        if (reports is null || reports.Count == 0)
        {
            return true;
        }

        var next = new List<BattleParticipantReport>(reports.Count);
        foreach (var report in reports)
        {
            if (!ArmyListRules.TryNormalizeText(report.ArmyListText, out error, out var armyListText))
            {
                mapped = null;
                return false;
            }

            var rawCategories = new List<ArmyListSupplyCategory>();
            foreach (var category in report.SupplyCategories ?? [])
            {
                if (string.IsNullOrWhiteSpace(category.Name))
                {
                    error = new DomainError(
                        "armyListCategories.name",
                        $"Category names must be 1 to {ArmyListRules.CategoryNameMaxLength} characters.",
                        "supplyCategories");
                    mapped = null;
                    return false;
                }

                rawCategories.Add(
                    new ArmyListSupplyCategory(
                        category.Name,
                        Math.Max(0, category.UnitCount),
                        Math.Max(0, category.SupplyPoints),
                        category.CostsSupply));
            }

            if (!ArmyListRules.TryNormalizeCategories(rawCategories, out error, out var normalizedCategories))
            {
                mapped = null;
                return false;
            }

            next.Add(
                new BattleParticipantReport(
                    report.ForceId,
                    report.VictoryPoints,
                    report.ArmyPoints,
                    report.DifferentialBattlePoints,
                    report.BonusBattlePoints,
                    report.KilledEnemyGeneral,
                    report.DestroyedEnemySupplyLine,
                    [
                        .. (report.Answers ?? []).Select(static answer => new BattleQuestionAnswer(
                            answer.QuestionId,
                            answer.BooleanValue,
                            answer.BattlePointsValue)),
                    ],
                    report.SupplyCostingUnitCount,
                    armyListText,
                    ArmyListRules.NormalizeGameSystem(report.ArmyListGameSystem),
                    ArmyListRules.ParseBuilder(report.ArmyListBuilder),
                    normalizedCategories,
                    report.UsedExtraBlackPowder,
                    Math.Max(0, report.MagicalSupplyRerolls)));
        }

        mapped = next;
        return true;
    }

    public static IReadOnlyDictionary<Guid, int> ExtraBattleReportPoints(StoredCampaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        var play = campaign.PlayState ?? CampaignPlayState.Empty;
        var extras = new Dictionary<Guid, int>();
        foreach (var battle in play.Battles)
        {
            if (battle.Status is not BattleStatus.Finalized and not BattleStatus.GMResolved)
            {
                continue;
            }

            var submission = play.BattleSubmissions
                .Where(item => item.BattleId == battle.Id
                    && item.Reports.Count > 0
                    && item.IsDraw == battle.IsDraw
                    && item.WinnerForceId == battle.WinnerForceId)
                .OrderByDescending(static item => item.SubmittedUtc)
                .FirstOrDefault();
            if (submission is null)
            {
                continue;
            }

            var questions = MissionQuestions(campaign, battle.TerritoryId, battle.MissionId);
            foreach (var report in submission.Reports)
            {
                var force = play.Forces.FirstOrDefault(item => item.Id == report.ForceId);
                if (force is null)
                {
                    continue;
                }

                extras[force.ControllerUserId] = extras.GetValueOrDefault(force.ControllerUserId)
                    + BattleResultRules.ExtraCampaignPoints(report, campaign.BattleReportRules, questions);
            }
        }

        return extras;
    }

    private static MissionResultQuestionSetup ToQuestion(StoredMissionResultQuestion question)
    {
        var kind = Enum.TryParse<MissionResultQuestionKind>(question.Kind, true, out var parsed)
            ? parsed
            : MissionResultQuestionKind.Boolean;
        return new MissionResultQuestionSetup(
            question.Id,
            string.IsNullOrWhiteSpace(question.Prompt) ? "Question" : question.Prompt,
            kind,
            Math.Max(0, question.BattlePoints),
            Math.Max(0, question.CampaignPoints));
    }
}
