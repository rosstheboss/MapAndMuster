using System.Text.Json;
using Campaign.Domain.Campaigns;
using Campaign.Domain.Play;

namespace Campaign.Infrastructure.Campaigns;

/// <summary>
/// Serializes launched play state for JSONB storage.
/// </summary>
internal static class PlayStateJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static string? Serialize(CampaignPlayState? state)
    {
        if (state is null || state.Windows.Count == 0 && state.Forces.Count == 0)
        {
            return state is null ? null : JsonSerializer.Serialize(ToDocument(state), Options);
        }

        return JsonSerializer.Serialize(ToDocument(state), Options);
    }

    public static CampaignPlayState? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        var document = JsonSerializer.Deserialize<PlayDocument>(json, Options);
        return document is null ? null : FromDocument(document);
    }

    private static PlayDocument ToDocument(CampaignPlayState state)
    {
        return new PlayDocument
        {
            Windows = [.. state.Windows.Select(static window => new WindowDocument
            {
                Id = window.Id,
                RoundNumber = window.RoundNumber,
                PhaseNumber = window.PhaseNumber,
                Kind = window.Kind.ToString(),
                PlannedAmount = window.PlannedAmount,
                PlannedUnit = window.PlannedUnit.ToString(),
                StartsUtc = window.StartsUtc,
                EndsUtc = window.EndsUtc,
                Status = window.Status.ToString(),
            })],
            Forces = [.. state.Forces.Select(static force => new ForceDocument
            {
                Id = force.Id,
                ControllerUserId = force.ControllerUserId,
                FactionId = force.FactionId,
                TerritoryId = force.TerritoryId,
                InBattle = force.InBattle,
                StatusName = force.StatusName,
            })],
            Drafts = [.. state.Drafts.Select(static draft => new DraftDocument
            {
                WindowId = draft.WindowId,
                ForceId = draft.ForceId,
                Kind = draft.Kind.ToString(),
                TargetTerritoryId = draft.TargetTerritoryId,
                StructureTypeId = draft.StructureTypeId,
                UpdatedUtc = draft.UpdatedUtc,
            })],
            Submissions = [.. state.Submissions.Select(static item => new SubmissionDocument
            {
                Id = item.Id,
                WindowId = item.WindowId,
                ForceId = item.ForceId,
                Kind = item.Kind.ToString(),
                TargetTerritoryId = item.TargetTerritoryId,
                StructureTypeId = item.StructureTypeId,
                Source = item.Source.ToString(),
                SubmittedUtc = item.SubmittedUtc,
                ActorUserId = item.ActorUserId,
            })],
            Commitments = [.. state.Commitments.Select(static item => new CommitmentDocument
            {
                WindowId = item.WindowId,
                UserId = item.UserId,
                CommittedUtc = item.CommittedUtc,
            })],
            Battles = [.. state.Battles.Select(static battle => new BattleDocument
            {
                Id = battle.Id,
                TerritoryId = battle.TerritoryId,
                SourceWindowId = battle.SourceWindowId,
                BattleWindowId = battle.BattleWindowId,
                Status = battle.Status.ToString(),
                ParticipantForceIds = [.. battle.ParticipantForceIds],
                ActiveForceIds = [.. battle.ActiveForceIds],
                WaitingForceIds = [.. battle.WaitingForceIds],
                SurrenderedForceIds = [.. battle.SurrenderedForceIds],
                WinnerForceId = battle.WinnerForceId,
                IsDraw = battle.IsDraw,
                IsNoContest = battle.IsNoContest,
                CreatedUtc = battle.CreatedUtc,
                WinnerScore = battle.WinnerScore,
                LoserScore = battle.LoserScore,
                MissionId = battle.MissionId,
                AttackerForceId = battle.AttackerForceId,
                DefenderForceId = battle.DefenderForceId,
            })],
            BattleSubmissions = [.. state.BattleSubmissions.Select(static item => new BattleSubmissionDocument
            {
                Id = item.Id,
                BattleId = item.BattleId,
                SubmitterUserId = item.SubmitterUserId,
                WinnerForceId = item.WinnerForceId,
                IsDraw = item.IsDraw,
                AcceptedSubmissionId = item.AcceptedSubmissionId,
                SubmittedUtc = item.SubmittedUtc,
                WinnerScore = item.WinnerScore,
                LoserScore = item.LoserScore,
                Reports = [.. item.Reports.Select(static report => new BattleReportDocument
                {
                    ForceId = report.ForceId,
                    VictoryPoints = report.VictoryPoints,
                    ArmyPoints = report.ArmyPoints,
                    DifferentialBattlePoints = report.DifferentialBattlePoints,
                    BonusBattlePoints = report.BonusBattlePoints,
                    KilledEnemyGeneral = report.KilledEnemyGeneral,
                    DestroyedEnemySupplyLine = report.DestroyedEnemySupplyLine,
                    SupplyCostingUnitCount = report.SupplyCostingUnitCount,
                    ArmyListText = report.ArmyListText,
                    ArmyListGameSystem = report.ArmyListGameSystem,
                    ArmyListBuilder = report.ArmyListBuilder.ToString(),
                    SupplyCategories = [.. report.SupplyCategories.Select(static category => new ArmyListCategoryDocument
                    {
                        Name = category.Name,
                        UnitCount = category.UnitCount,
                        SupplyPoints = category.SupplyPoints,
                        CostsSupply = category.CostsSupply,
                    })],
                    Answers = [.. report.Answers.Select(static answer => new BattleAnswerDocument
                    {
                        QuestionId = answer.QuestionId,
                        BooleanValue = answer.BooleanValue,
                        BattlePointsValue = answer.BattlePointsValue,
                    })],
                })],
            })],
            Retreats = [.. state.Retreats.Select(static item => new RetreatDocument
            {
                Id = item.Id,
                BattleId = item.BattleId,
                ForceId = item.ForceId,
                TargetTerritoryId = item.TargetTerritoryId,
                IsDefault = item.IsDefault,
                IsSurrender = item.IsSurrender,
                SubmittedUtc = item.SubmittedUtc,
            })],
            BrokenAllyFactionIds = [.. state.BrokenAllyFactionIds],
            Structures = [.. state.Structures.Select(static item => new StructureDocument
            {
                TerritoryId = item.TerritoryId,
                StructureTypeId = item.StructureTypeId,
                Condition = item.Condition.ToString(),
            })],
            ItemObjectives = [.. state.ItemObjectives.Select(ToItem)],
            Log = [.. state.Log.Select(static item => new LogDocument
            {
                Id = item.Id,
                OccurredUtc = item.OccurredUtc,
                Kind = item.Kind.ToString(),
                WindowId = item.WindowId,
                ForceId = item.ForceId,
                ActorUserId = item.ActorUserId,
                TerritoryId = item.TerritoryId,
                TargetTerritoryId = item.TargetTerritoryId,
                BattleId = item.BattleId,
                ActionKind = item.ActionKind?.ToString(),
                RelatedForceIds = [.. item.RelatedForceIds],
                Message = item.Message,
                ActorDisplayName = item.ActorDisplayName,
                ChatChannelKind = item.ChatChannelKind.ToString(),
                ChatTargetUserId = item.ChatTargetUserId,
                ChatTargetFactionId = item.ChatTargetFactionId,
                ChatTargetAllyGroupId = item.ChatTargetAllyGroupId,
                ChatTargetLabel = item.ChatTargetLabel,
            })],
            Snapshots = [.. state.Snapshots.Select(static item => new SnapshotDocument
            {
                WindowId = item.WindowId,
                Forces = [.. item.Forces.Select(static force => new ForceDocument
                {
                    Id = force.Id,
                    ControllerUserId = force.ControllerUserId,
                    FactionId = force.FactionId,
                    TerritoryId = force.TerritoryId,
                    InBattle = force.InBattle,
                    StatusName = force.StatusName,
                })],
                Structures = [.. item.Structures.Select(static structure => new StructureDocument
                {
                    TerritoryId = structure.TerritoryId,
                    StructureTypeId = structure.StructureTypeId,
                    Condition = structure.Condition.ToString(),
                })],
                BrokenAllyFactionIds = [.. item.BrokenAllyFactionIds],
                Territories = [.. item.Territories.Select(static territory => new TerritorySnapshotDocument
                {
                    TerritoryId = territory.TerritoryId,
                    OwnerFactionId = territory.OwnerFactionId,
                    StructureTypeId = territory.StructureTypeId,
                    StructureName = territory.StructureName,
                    Condition = territory.Condition.ToString(),
                })],
                ItemObjectives = [.. item.ItemObjectives.Select(ToItem)],
            })],
            DebugActorUserId = state.DebugActorUserId,
            DebugStartedUtc = state.DebugStartedUtc,
            PublicObjectiveAwards = [.. state.PublicObjectiveAwards.Select(static item => new PublicObjectiveAwardDocument
            {
                Id = item.Id,
                ObjectiveId = item.ObjectiveId,
                PlayerUserId = item.PlayerUserId,
                IsActive = item.IsActive,
                ActorUserId = item.ActorUserId,
                AwardedUtc = item.AwardedUtc,
            })],
            PrivateObjectives = [.. state.PrivateObjectives.Select(static item => new PrivateObjectiveDocument
            {
                Id = item.Id,
                TypeId = item.TypeId,
                HolderKind = item.HolderKind.ToString(),
                HolderId = item.HolderId,
                ScoringKind = item.ScoringKind.ToString(),
                Status = item.Status.ToString(),
                AssignedUtc = item.AssignedUtc,
                ClaimedUtc = item.ClaimedUtc,
                RevealedUtc = item.RevealedUtc,
                ClaimedByUserId = item.ClaimedByUserId,
                ApprovedByUserId = item.ApprovedByUserId,
            })],
            StructureDestructions = [.. state.StructureDestructions.Select(static item => new StructureDestructionDocument
            {
                Id = item.Id,
                TerritoryId = item.TerritoryId,
                StructureTypeId = item.StructureTypeId,
                ActorFactionId = item.ActorFactionId,
                ActorUserId = item.ActorUserId,
                DestroyedUtc = item.DestroyedUtc,
            })],
            PlayerSupplies = [.. state.PlayerSupplies.Select(static item => new PlayerSupplyDocument
            {
                UserId = item.UserId,
                TemporarySupplyPoints = item.TemporarySupplyPoints,
            })],
        };
    }

    private static CampaignPlayState FromDocument(PlayDocument document)
    {
        return new CampaignPlayState(
            [.. document.Windows.Select(static window => new PhaseWindow(
                window.Id,
                window.RoundNumber,
                window.PhaseNumber,
                Enum.Parse<RoundPhaseKind>(window.Kind, true),
                window.PlannedAmount,
                Enum.Parse<DurationUnit>(window.PlannedUnit, true),
                window.StartsUtc,
                window.EndsUtc,
                Enum.Parse<PhaseWindowStatus>(window.Status, true)))],
            [.. document.Forces.Select(static force => new CampaignForce(
                force.Id,
                force.ControllerUserId,
                force.FactionId,
                force.TerritoryId,
                force.InBattle,
                force.StatusName))],
            [.. document.Drafts.Select(static draft => new OrderDraft(
                draft.WindowId,
                draft.ForceId,
                Enum.Parse<ActionKind>(draft.Kind, true),
                draft.TargetTerritoryId,
                draft.StructureTypeId,
                draft.UpdatedUtc))],
            [.. document.Submissions.Select(static item => new OrderSubmission(
                item.Id,
                item.WindowId,
                item.ForceId,
                Enum.Parse<ActionKind>(item.Kind, true),
                item.TargetTerritoryId,
                item.StructureTypeId,
                Enum.Parse<OrderSource>(item.Source, true),
                item.SubmittedUtc,
                item.ActorUserId))],
            [.. document.Commitments.Select(static item => new PlayerCommitment(item.WindowId, item.UserId, item.CommittedUtc))],
            [.. document.Battles.Select(static battle => new CampaignBattle(
                battle.Id,
                battle.TerritoryId,
                battle.SourceWindowId,
                battle.BattleWindowId,
                Enum.Parse<BattleStatus>(battle.Status, true),
                battle.ParticipantForceIds,
                battle.WinnerForceId,
                battle.IsDraw,
                battle.CreatedUtc,
                battle.WinnerScore,
                battle.LoserScore,
                battle.ActiveForceIds,
                battle.WaitingForceIds,
                battle.SurrenderedForceIds,
                battle.IsNoContest,
                battle.MissionId,
                battle.AttackerForceId,
                battle.DefenderForceId))],
            [.. document.BattleSubmissions.Select(static item => new BattleResultSubmission(
                item.Id,
                item.BattleId,
                item.SubmitterUserId,
                item.WinnerForceId,
                item.IsDraw,
                item.AcceptedSubmissionId,
                item.SubmittedUtc,
                item.WinnerScore,
                item.LoserScore,
                [.. (item.Reports ?? []).Select(static report => new BattleParticipantReport(
                    report.ForceId,
                    Math.Max(0, report.VictoryPoints),
                    Math.Max(0, report.ArmyPoints),
                    Math.Max(0, report.DifferentialBattlePoints),
                    Math.Max(0, report.BonusBattlePoints),
                    report.KilledEnemyGeneral,
                    report.DestroyedEnemySupplyLine,
                    [.. (report.Answers ?? []).Select(static answer => new BattleQuestionAnswer(
                        answer.QuestionId,
                        answer.BooleanValue,
                        answer.BattlePointsValue is null ? null : Math.Max(0, answer.BattlePointsValue.Value)))],
                    Math.Max(0, report.SupplyCostingUnitCount),
                    string.IsNullOrWhiteSpace(report.ArmyListText) ? null : report.ArmyListText.Trim(),
                    ArmyListRules.NormalizeGameSystem(report.ArmyListGameSystem),
                    ArmyListRules.ParseBuilder(report.ArmyListBuilder),
                    [.. (report.SupplyCategories ?? [])
                        .Where(static category => !string.IsNullOrWhiteSpace(category.Name))
                        .Select(static category => new ArmyListSupplyCategory(
                            category.Name,
                            Math.Max(0, category.UnitCount),
                            Math.Max(0, category.SupplyPoints),
                            category.CostsSupply))]))]))],
            [.. document.Retreats.Select(static item => new RetreatOrder(
                item.Id,
                item.BattleId,
                item.ForceId,
                item.TargetTerritoryId,
                item.IsDefault,
                item.SubmittedUtc,
                item.IsSurrender))],
            document.BrokenAllyFactionIds,
            [.. document.Structures.Select(static item => new TerritoryStructureState(
                item.TerritoryId,
                item.StructureTypeId,
                Enum.Parse<StructureCondition>(item.Condition, true)))],
            [.. (document.ItemObjectives ?? []).Select(FromItem)],
            [.. (document.Log ?? []).Select(static item => new PlayLogEntry(
                item.Id,
                item.OccurredUtc,
                Enum.Parse<PlayLogKind>(item.Kind, true),
                item.WindowId,
                item.ForceId,
                item.ActorUserId,
                item.TerritoryId,
                item.TargetTerritoryId,
                item.BattleId,
                string.IsNullOrWhiteSpace(item.ActionKind) ? null : Enum.Parse<ActionKind>(item.ActionKind, true),
                item.RelatedForceIds,
                item.Message,
                item.ActorDisplayName,
                Enum.TryParse<ChatChannelKind>(item.ChatChannelKind, true, out var channelKind)
                    ? channelKind
                    : ChatChannelKind.Public,
                item.ChatTargetUserId,
                item.ChatTargetFactionId,
                item.ChatTargetAllyGroupId,
                item.ChatTargetLabel))],
            ToSnapshots(document),
            document.DebugActorUserId,
            document.DebugStartedUtc,
            [.. (document.PublicObjectiveAwards ?? []).Select(static item => new PublicObjectiveAward(
                item.Id,
                item.ObjectiveId,
                item.PlayerUserId,
                item.IsActive,
                item.ActorUserId,
                item.AwardedUtc))],
            [.. (document.PrivateObjectives ?? []).Select(static item => new PrivateObjectiveAssignment(
                item.Id,
                item.TypeId,
                Enum.Parse<PrivateObjectiveHolderKind>(item.HolderKind, true),
                item.HolderId,
                Enum.Parse<PrivateObjectiveScoringKind>(item.ScoringKind, true),
                Enum.Parse<PrivateObjectiveAssignmentStatus>(item.Status, true),
                item.AssignedUtc,
                item.ClaimedUtc,
                item.RevealedUtc,
                item.ClaimedByUserId,
                item.ApprovedByUserId))],
            [.. (document.StructureDestructions ?? []).Select(static item => new StructureDestructionFact(
                item.Id,
                item.TerritoryId,
                item.StructureTypeId,
                item.ActorFactionId,
                item.ActorUserId,
                item.DestroyedUtc))],
            [.. (document.PlayerSupplies ?? []).Select(static item => new PlayerSupplyBalance(
                item.UserId,
                Math.Max(0, item.TemporarySupplyPoints)))]);
    }

    private static IReadOnlyList<ActionWindowSnapshot> ToSnapshots(PlayDocument document)
    {
        return
        [
            .. (document.Snapshots ?? []).Select(static item => new ActionWindowSnapshot(
                item.WindowId,
                [.. item.Forces.Select(static force => new CampaignForce(
                    force.Id,
                    force.ControllerUserId,
                    force.FactionId,
                    force.TerritoryId,
                    force.InBattle,
                    force.StatusName))],
                [.. item.Structures.Select(static structure => new TerritoryStructureState(
                    structure.TerritoryId,
                    structure.StructureTypeId,
                    Enum.Parse<StructureCondition>(structure.Condition, true)))],
                item.BrokenAllyFactionIds,
                [.. item.Territories.Select(static territory => new TerritorySnapshot(
                    territory.TerritoryId,
                    territory.OwnerFactionId,
                    territory.StructureTypeId,
                    territory.StructureName,
                    Enum.Parse<StructureCondition>(territory.Condition, true)))],
                [.. (item.ItemObjectives ?? []).Select(FromItem)])),
        ];
    }

    private static ItemObjectiveDocument ToItem(CampaignItemObjective item)
    {
        return new ItemObjectiveDocument
        {
            Id = item.Id,
            TypeId = item.TypeId,
            Name = item.Name,
            TerritoryId = item.TerritoryId,
            PossessorForceId = item.PossessorForceId,
            IsRevealed = item.IsRevealed,
            OriginalTerritoryId = item.OriginalTerritoryId,
            WasHiddenUntilFound = item.WasHiddenUntilFound,
            FlavorText = item.FlavorText,
            StateKey = item.StateKey,
            IsDestroyed = item.IsDestroyed,
            ResolvedChoiceId = item.ResolvedChoiceId,
        };
    }

    private static CampaignItemObjective FromItem(ItemObjectiveDocument item)
    {
        return new CampaignItemObjective(
            item.Id,
            item.TypeId,
            item.Name,
            item.TerritoryId,
            item.PossessorForceId,
            item.IsRevealed,
            item.OriginalTerritoryId,
            item.WasHiddenUntilFound,
            item.FlavorText,
            item.StateKey,
            item.IsDestroyed,
            item.ResolvedChoiceId);
    }

    private sealed class PlayDocument
    {
        public List<WindowDocument> Windows { get; set; } = [];
        public List<ForceDocument> Forces { get; set; } = [];
        public List<DraftDocument> Drafts { get; set; } = [];
        public List<SubmissionDocument> Submissions { get; set; } = [];
        public List<CommitmentDocument> Commitments { get; set; } = [];
        public List<BattleDocument> Battles { get; set; } = [];
        public List<BattleSubmissionDocument> BattleSubmissions { get; set; } = [];
        public List<RetreatDocument> Retreats { get; set; } = [];
        public List<Guid> BrokenAllyFactionIds { get; set; } = [];
        public List<StructureDocument> Structures { get; set; } = [];
        public List<ItemObjectiveDocument>? ItemObjectives { get; set; }
        public List<LogDocument> Log { get; set; } = [];
        public List<SnapshotDocument>? Snapshots { get; set; }
        public Guid? DebugActorUserId { get; set; }
        public DateTimeOffset? DebugStartedUtc { get; set; }
        public List<PublicObjectiveAwardDocument>? PublicObjectiveAwards { get; set; }
        public List<PrivateObjectiveDocument>? PrivateObjectives { get; set; }
        public List<StructureDestructionDocument>? StructureDestructions { get; set; }
        public List<PlayerSupplyDocument>? PlayerSupplies { get; set; }
    }

    private sealed class WindowDocument
    {
        public Guid Id { get; set; }
        public int RoundNumber { get; set; }
        public int PhaseNumber { get; set; }
        public string Kind { get; set; } = "";
        public int PlannedAmount { get; set; }
        public string PlannedUnit { get; set; } = "";
        public DateTimeOffset StartsUtc { get; set; }
        public DateTimeOffset EndsUtc { get; set; }
        public string Status { get; set; } = "";
    }

    private sealed class ForceDocument
    {
        public Guid Id { get; set; }
        public Guid ControllerUserId { get; set; }
        public Guid FactionId { get; set; }
        public Guid TerritoryId { get; set; }
        public bool InBattle { get; set; }
        public string? StatusName { get; set; }
    }

    private sealed class DraftDocument
    {
        public Guid WindowId { get; set; }
        public Guid ForceId { get; set; }
        public string Kind { get; set; } = "";
        public Guid? TargetTerritoryId { get; set; }
        public Guid? StructureTypeId { get; set; }
        public DateTimeOffset UpdatedUtc { get; set; }
    }

    private sealed class SubmissionDocument
    {
        public Guid Id { get; set; }
        public Guid WindowId { get; set; }
        public Guid ForceId { get; set; }
        public string Kind { get; set; } = "";
        public Guid? TargetTerritoryId { get; set; }
        public Guid? StructureTypeId { get; set; }
        public string Source { get; set; } = "";
        public DateTimeOffset SubmittedUtc { get; set; }
        public Guid ActorUserId { get; set; }
    }

    private sealed class CommitmentDocument
    {
        public Guid WindowId { get; set; }
        public Guid UserId { get; set; }
        public DateTimeOffset CommittedUtc { get; set; }
    }

    private sealed class BattleDocument
    {
        public Guid Id { get; set; }
        public Guid TerritoryId { get; set; }
        public Guid SourceWindowId { get; set; }
        public Guid? BattleWindowId { get; set; }
        public string Status { get; set; } = "";
        public List<Guid> ParticipantForceIds { get; set; } = [];
        public List<Guid> ActiveForceIds { get; set; } = [];
        public List<Guid> WaitingForceIds { get; set; } = [];
        public List<Guid> SurrenderedForceIds { get; set; } = [];
        public Guid? WinnerForceId { get; set; }
        public bool IsDraw { get; set; }
        public bool IsNoContest { get; set; }
        public DateTimeOffset CreatedUtc { get; set; }
        public int? WinnerScore { get; set; }
        public int? LoserScore { get; set; }
        public Guid? MissionId { get; set; }
        public Guid? AttackerForceId { get; set; }
        public Guid? DefenderForceId { get; set; }
    }

    private sealed class BattleSubmissionDocument
    {
        public Guid Id { get; set; }
        public Guid BattleId { get; set; }
        public Guid SubmitterUserId { get; set; }
        public Guid? WinnerForceId { get; set; }
        public bool IsDraw { get; set; }
        public Guid? AcceptedSubmissionId { get; set; }
        public DateTimeOffset SubmittedUtc { get; set; }
        public int? WinnerScore { get; set; }
        public int? LoserScore { get; set; }
        public List<BattleReportDocument>? Reports { get; set; }
    }

    private sealed class BattleReportDocument
    {
        public Guid ForceId { get; set; }
        public int VictoryPoints { get; set; }
        public int ArmyPoints { get; set; }
        public int DifferentialBattlePoints { get; set; }
        public int BonusBattlePoints { get; set; }
        public bool KilledEnemyGeneral { get; set; }
        public bool DestroyedEnemySupplyLine { get; set; }
        public int SupplyCostingUnitCount { get; set; }
        public string? ArmyListText { get; set; }
        public string? ArmyListGameSystem { get; set; }
        public string? ArmyListBuilder { get; set; }
        public List<ArmyListCategoryDocument>? SupplyCategories { get; set; }
        public List<BattleAnswerDocument>? Answers { get; set; }
    }

    private sealed class ArmyListCategoryDocument
    {
        public string Name { get; set; } = string.Empty;
        public int UnitCount { get; set; }
        public int SupplyPoints { get; set; }
        public bool CostsSupply { get; set; }
    }

    private sealed class BattleAnswerDocument
    {
        public Guid QuestionId { get; set; }
        public bool? BooleanValue { get; set; }
        public int? BattlePointsValue { get; set; }
    }

    private sealed class PlayerSupplyDocument
    {
        public Guid UserId { get; set; }
        public int TemporarySupplyPoints { get; set; }
    }

    private sealed class RetreatDocument
    {
        public Guid Id { get; set; }
        public Guid BattleId { get; set; }
        public Guid ForceId { get; set; }
        public Guid TargetTerritoryId { get; set; }
        public bool IsDefault { get; set; }
        public bool IsSurrender { get; set; }
        public DateTimeOffset SubmittedUtc { get; set; }
    }

    private sealed class StructureDocument
    {
        public Guid TerritoryId { get; set; }
        public Guid? StructureTypeId { get; set; }
        public string Condition { get; set; } = "";
    }

    private sealed class PublicObjectiveAwardDocument
    {
        public Guid Id { get; set; }
        public Guid ObjectiveId { get; set; }
        public Guid PlayerUserId { get; set; }
        public bool IsActive { get; set; }
        public Guid ActorUserId { get; set; }
        public DateTimeOffset AwardedUtc { get; set; }
    }

    private sealed class ItemObjectiveDocument
    {
        public Guid Id { get; set; }
        public Guid TypeId { get; set; }
        public string Name { get; set; } = "";
        public Guid? TerritoryId { get; set; }
        public Guid? PossessorForceId { get; set; }
        public bool IsRevealed { get; set; }
        public Guid OriginalTerritoryId { get; set; }
        public bool WasHiddenUntilFound { get; set; }
        public string? FlavorText { get; set; }
        public string? StateKey { get; set; }
        public bool IsDestroyed { get; set; }
        public Guid? ResolvedChoiceId { get; set; }
    }

    private sealed class LogDocument
    {
        public Guid Id { get; set; }
        public DateTimeOffset OccurredUtc { get; set; }
        public string Kind { get; set; } = "";
        public Guid? WindowId { get; set; }
        public Guid? ForceId { get; set; }
        public Guid? ActorUserId { get; set; }
        public Guid? TerritoryId { get; set; }
        public Guid? TargetTerritoryId { get; set; }
        public Guid? BattleId { get; set; }
        public string? ActionKind { get; set; }
        public List<Guid> RelatedForceIds { get; set; } = [];
        public string? Message { get; set; }
        public string? ActorDisplayName { get; set; }
        public string? ChatChannelKind { get; set; }
        public Guid? ChatTargetUserId { get; set; }
        public Guid? ChatTargetFactionId { get; set; }
        public Guid? ChatTargetAllyGroupId { get; set; }
        public string? ChatTargetLabel { get; set; }
    }

    private sealed class SnapshotDocument
    {
        public Guid WindowId { get; set; }
        public List<ForceDocument> Forces { get; set; } = [];
        public List<StructureDocument> Structures { get; set; } = [];
        public List<Guid> BrokenAllyFactionIds { get; set; } = [];
        public List<TerritorySnapshotDocument> Territories { get; set; } = [];
        public List<ItemObjectiveDocument>? ItemObjectives { get; set; }
    }

    private sealed class TerritorySnapshotDocument
    {
        public Guid TerritoryId { get; set; }
        public Guid? OwnerFactionId { get; set; }
        public Guid? StructureTypeId { get; set; }
        public string? StructureName { get; set; }
        public string Condition { get; set; } = "";
    }

    private sealed class PrivateObjectiveDocument
    {
        public Guid Id { get; set; }
        public Guid TypeId { get; set; }
        public string HolderKind { get; set; } = "";
        public Guid HolderId { get; set; }
        public string ScoringKind { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTimeOffset AssignedUtc { get; set; }
        public DateTimeOffset? ClaimedUtc { get; set; }
        public DateTimeOffset? RevealedUtc { get; set; }
        public Guid? ClaimedByUserId { get; set; }
        public Guid? ApprovedByUserId { get; set; }
    }

    private sealed class StructureDestructionDocument
    {
        public Guid Id { get; set; }
        public Guid TerritoryId { get; set; }
        public Guid StructureTypeId { get; set; }
        public Guid ActorFactionId { get; set; }
        public Guid ActorUserId { get; set; }
        public DateTimeOffset DestroyedUtc { get; set; }
    }
}
