using System.Diagnostics.CodeAnalysis;
using MapAndMuster.Domain.Campaigns;
using MapAndMuster.Domain.Common;

namespace MapAndMuster.Domain.Play;

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
    /// Extra campaign points from scored mission questions.
    /// </summary>
    public static int ExtraCampaignPoints(
        BattleParticipantReport report,
        IReadOnlyList<MissionResultQuestionSetup> questions)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(questions);
        var total = 0;
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
                report.SupplyCategories,
                report.UsedExtraBlackPowder,
                report.MagicalSupplyRerolls)),
        ];
    }

    /// <summary>
    /// Extra Black Powder and Magical Supply may be declared only by forces that have those keys.
    /// Magical Supply rerolls cannot exceed leftover unused composition supply when a leftover map is provided.
    /// </summary>
    public static bool TryValidateSpecialRuleUses(
        IReadOnlyList<BattleParticipantReport> reports,
        IReadOnlyList<CampaignForce> forces,
        SpecialRuleContext rules,
        IReadOnlyDictionary<Guid, int>? leftoverSupplyByForce,
        [NotNullWhen(false)] out DomainError? error)
    {
        ArgumentNullException.ThrowIfNull(reports);
        ArgumentNullException.ThrowIfNull(forces);
        ArgumentNullException.ThrowIfNull(rules);
        error = null;
        foreach (var report in reports)
        {
            var force = forces.FirstOrDefault(item => item.Id == report.ForceId);
            if (force is null)
            {
                continue;
            }

            if (report.UsedExtraBlackPowder && !rules.Has(force, SpecialRuleEffectKeys.PreparedForBattle))
            {
                error = new DomainError(
                    "battle.result.extra_black_powder.forbidden",
                    "Only a force with Prepared for Battle can use Extra Black Powder.",
                    "usedExtraBlackPowder");
                return false;
            }

            if (report.MagicalSupplyRerolls > 0 && !rules.Has(force, SpecialRuleEffectKeys.MagicalSupply))
            {
                error = new DomainError(
                    "battle.result.magical_supply.forbidden",
                    "Only a force with Magical Supply can declare leftover supply as rerolls.",
                    "magicalSupplyRerolls");
                return false;
            }

            if (leftoverSupplyByForce is not null
                && leftoverSupplyByForce.TryGetValue(report.ForceId, out var leftover)
                && report.MagicalSupplyRerolls > leftover)
            {
                error = new DomainError(
                    "battle.result.magical_supply.exceeds_leftover",
                    "Magical Supply rerolls cannot exceed leftover unused supply for this battle.",
                    "magicalSupplyRerolls");
                return false;
            }
        }

        return true;
    }

    private static bool ReportEquals(BattleParticipantReport left, BattleParticipantReport right)
    {
        if (left.VictoryPoints != right.VictoryPoints
            || left.ArmyPoints != right.ArmyPoints
            || left.DifferentialBattlePoints != right.DifferentialBattlePoints
            || left.BonusBattlePoints != right.BonusBattlePoints
            || left.SupplyCostingUnitCount != right.SupplyCostingUnitCount
            || left.UsedExtraBlackPowder != right.UsedExtraBlackPowder
            || left.MagicalSupplyRerolls != right.MagicalSupplyRerolls
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
