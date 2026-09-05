namespace MapAndMuster.Domain.Play;

/// <summary>
/// Formats the public campaign-log snapshot of final scores and remaining item objectives.
/// </summary>
public static class CampaignCompletionLogRules
{
    /// <summary>
    /// Builds the log message for a completed campaign, or a later manager revision of that snapshot.
    /// </summary>
    public static string Format(
        IReadOnlyList<(string Player, int Total)> scores,
        IReadOnlyList<string> itemLines,
        bool revised)
    {
        ArgumentNullException.ThrowIfNull(scores);
        ArgumentNullException.ThrowIfNull(itemLines);
        var scoresText = scores.Count == 0
            ? "no players scored"
            : string.Join("; ", scores.Select(static row => $"{row.Player} {row.Total}"));
        var itemsText = itemLines.Count == 0
            ? "none remained"
            : string.Join("; ", itemLines);
        var prefix = revised ? "Updated final scores" : "The campaign ended. Final scores";
        return $"{prefix}: {scoresText}. Item objectives: {itemsText}.";
    }
}
