namespace Campaign.Domain.Campaigns;

/// <summary>
/// Display labels for round phases. Action windows are numbered in round order.
/// </summary>
public static class CampaignPhaseLabels
{
    /// <summary>
    /// Formats the current in-progress phase for listings and campaign pages.
    /// </summary>
    /// <param name="phases">The ordered phases in a round.</param>
    /// <param name="phaseNumber">The 1-based phase index in the round.</param>
    /// <param name="kind">The current phase kind.</param>
    /// <returns>The display label, such as "Action 1" or "Battle".</returns>
    public static string Format(IReadOnlyList<RoundPhaseSetup> phases, int phaseNumber, RoundPhaseKind kind)
    {
        ArgumentNullException.ThrowIfNull(phases);
        if (kind == RoundPhaseKind.Battle)
        {
            var battleCount = phases.Count(static phase => phase.Kind == RoundPhaseKind.Battle);
            if (battleCount <= 1)
            {
                return "Battle";
            }

            var battlesThroughCurrent = phases
                .Take(Math.Clamp(phaseNumber, 1, phases.Count))
                .Count(static phase => phase.Kind == RoundPhaseKind.Battle);
            return $"Battle {battlesThroughCurrent}";
        }

        var actionsThroughCurrent = phases
            .Take(Math.Clamp(phaseNumber, 1, Math.Max(phases.Count, 1)))
            .Count(static phase => phase.Kind == RoundPhaseKind.Action);
        return $"Action {Math.Max(actionsThroughCurrent, 1)}";
    }
}
