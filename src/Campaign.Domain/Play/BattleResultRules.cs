using System.Diagnostics.CodeAnalysis;
using Campaign.Domain.Campaigns;
using Campaign.Domain.Common;

namespace Campaign.Domain.Play;

/// <summary>
/// Normalizes structured battle reports and derives winner, draw, and extra campaign points.
/// </summary>
public static class BattleResultRules
{
    /// <summary>
    /// Whether two submissions describe the same tabletop outcome.
    /// </summary>
    public static bool AreEquivalent(BattleResultSubmission left, BattleResultSubmission right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        if (left.IsDraw != right.IsDraw
            || left.WinnerForceId != right.WinnerForceId
            || left.WinnerScore != right.WinnerScore
            || left.LoserScore != right.LoserScore)
        {
            return false;
        }

        if (left.Reports.Count == 0 && right.Reports.Count == 0)
        {
            return true;
        }

        if (left.Reports.Count != right.Reports.Count)
        {
            return false;
        }

        foreach (var report in left.Reports.OrderBy(static item => item.ForceId))
        {
            var other = right.Reports.FirstOrDefault(item => item.ForceId == report.ForceId);
            if (other is null || !ReportEquals(report, other))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Fills winner, draw, and scores from structured per-force reports.
    /// </summary>
    public static bool TryDeriveOutcome(
        IReadOnlyList<Guid> participantForceIds,
        IReadOnlyList<BattleParticipantReport> reports,
        out Guid? winnerForceId,
        out bool isDraw,
        out int? winnerScore,
        out int? loserScore,
        [NotNullWhen(false)] out DomainError? error)
    {
        ArgumentNullException.ThrowIfNull(participantForceIds);
        ArgumentNullException.ThrowIfNull(reports);
        winnerForceId = null;
        isDraw = false;
        winnerScore = null;
        loserScore = null;
        error = null;
        if (reports.Count != participantForceIds.Count
            || reports.Select(static item => item.ForceId).Distinct().Count() != reports.Count
            || reports.Any(report => !participantForceIds.Contains(report.ForceId)))
        {
            error = new DomainError(
                "battle.result.reports.invalid",
                "Report results for every participating force.",
                "reports");
            return false;
        }

        var ranked = reports.OrderByDescending(static item => item.TotalBattlePoints).ThenBy(static item => item.ForceId).ToArray();
        var leader = ranked[0];
        isDraw = ranked.Length > 1 && ranked.All(item => item.TotalBattlePoints == leader.TotalBattlePoints);
        if (isDraw)
        {
            return true;
        }

        winnerForceId = leader.ForceId;
        var loser = ranked.First(item => item.ForceId != leader.ForceId);
        winnerScore = leader.DifferentialBattlePoints;
        loserScore = loser.DifferentialBattlePoints;
        return true;
    }

    /// <summary>
    /// Extra campaign points from general kills, supply-line destruction, and scored mission questions.
    /// </summary>
    public static int ExtraCampaignPoints(
        BattleParticipantReport report,
        BattleReportRulesSetup rules,
        IReadOnlyList<MissionResultQuestionSetup> questions)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(questions);
        var total = 0;
        if (report.KilledEnemyGeneral)
        {
            total += rules.GeneralKillCampaignPoints;
        }

        if (report.DestroyedEnemySupplyLine)
        {
            total += rules.SupplyLineDestroyedCampaignPoints;
        }

        foreach (var question in questions)
        {
            var answer = report.Answers.FirstOrDefault(item => item.QuestionId == question.Id);
            if (answer is null)
            {
                continue;
            }

            if (question.Kind == MissionResultQuestionKind.Boolean && answer.BooleanValue == true)
            {
                total += question.CampaignPoints;
            }
            else if (question.Kind == MissionResultQuestionKind.BattlePoints && (answer.BattlePointsValue ?? 0) > 0)
            {
                total += question.CampaignPoints;
            }
        }

        return total;
    }

    /// <summary>
    /// Applies catalog battle points onto boolean answers so totals are comparable.
    /// </summary>
    public static IReadOnlyList<BattleParticipantReport> WithScoredAnswers(
        IReadOnlyList<BattleParticipantReport> reports,
        IReadOnlyList<MissionResultQuestionSetup> questions)
    {
        ArgumentNullException.ThrowIfNull(reports);
        ArgumentNullException.ThrowIfNull(questions);
        return
        [
            .. reports.Select(report => new BattleParticipantReport(
                report.ForceId,
                report.VictoryPoints,
                report.ArmyPoints,
                report.DifferentialBattlePoints,
                report.BonusBattlePoints,
                report.KilledEnemyGeneral,
                report.DestroyedEnemySupplyLine,
                [
                    .. report.Answers.Select(answer =>
                    {
                        var question = questions.FirstOrDefault(item => item.Id == answer.QuestionId);
                        if (question is null || question.Kind != MissionResultQuestionKind.Boolean)
                        {
                            return answer;
                        }

                        var points = answer.BooleanValue == true ? question.BattlePoints : 0;
                        return new BattleQuestionAnswer(answer.QuestionId, answer.BooleanValue, points);
                    }),
                ],
                report.SupplyCostingUnitCount,
                report.ArmyListText,
                report.ArmyListGameSystem,
                report.ArmyListBuilder,
                report.SupplyCategories)),
        ];
    }

    private static bool ReportEquals(BattleParticipantReport left, BattleParticipantReport right)
    {
        if (left.VictoryPoints != right.VictoryPoints
            || left.ArmyPoints != right.ArmyPoints
            || left.DifferentialBattlePoints != right.DifferentialBattlePoints
            || left.BonusBattlePoints != right.BonusBattlePoints
            || left.KilledEnemyGeneral != right.KilledEnemyGeneral
            || left.DestroyedEnemySupplyLine != right.DestroyedEnemySupplyLine
            || left.SupplyCostingUnitCount != right.SupplyCostingUnitCount
            || left.Answers.Count != right.Answers.Count)
        {
            return false;
        }

        foreach (var answer in left.Answers.OrderBy(static item => item.QuestionId))
        {
            var other = right.Answers.FirstOrDefault(item => item.QuestionId == answer.QuestionId);
            if (other is null
                || other.BooleanValue != answer.BooleanValue
                || other.BattlePointsValue != answer.BattlePointsValue)
            {
                return false;
            }
        }

        return true;
    }
}
