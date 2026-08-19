namespace Campaign.Domain.Campaigns;

/// <summary>
/// Generic per-round army size used when setup omits escalations or adds rounds.
/// </summary>
public static class ArmyEscalationDefaults
{
    /// <summary>Default maximum army points for a round.</summary>
    public const int MaxArmyPoints = 1000;

    /// <summary>Default free supply points granted each round.</summary>
    public const int FreeSupplyPoints = 1;

    /// <summary>Default free characters whose base cost does not count against supply.</summary>
    public const int FreeCharacterCount = 1;

    /// <summary>
    /// Returns one generic army-escalation row per round.
    /// </summary>
    /// <param name="roundCount">The configured round count.</param>
    /// <returns>Escalation rows numbered from 1.</returns>
    public static IReadOnlyList<RoundArmyEscalationSetup> ForRoundCount(int roundCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(roundCount, 1);
        var rows = new RoundArmyEscalationSetup[roundCount];
        for (var round = 1; round <= roundCount; round++)
        {
            rows[round - 1] = new RoundArmyEscalationSetup(
                round,
                MaxArmyPoints,
                FreeSupplyPoints,
                FreeCharacterCount);
        }

        return rows;
    }

    /// <summary>
    /// Keeps existing rows for overlapping rounds and fills missing rounds with generic defaults.
    /// </summary>
    /// <param name="existing">Stored or entered rows, which may be shorter than the round count.</param>
    /// <param name="roundCount">The configured round count.</param>
    /// <returns>Exactly <paramref name="roundCount"/> rows numbered from 1.</returns>
    public static IReadOnlyList<RoundArmyEscalationSetup> PadToRoundCount(
        IReadOnlyList<RoundArmyEscalationSetup>? existing,
        int roundCount)
    {
        var defaults = ForRoundCount(roundCount);
        if (existing is null || existing.Count == 0)
        {
            return defaults;
        }

        var byRound = new Dictionary<int, RoundArmyEscalationSetup>();
        foreach (var row in existing)
        {
            byRound.TryAdd(row.RoundNumber, row);
        }

        var rows = new RoundArmyEscalationSetup[roundCount];
        for (var round = 1; round <= roundCount; round++)
        {
            rows[round - 1] = byRound.TryGetValue(round, out var row)
                ? new RoundArmyEscalationSetup(
                    round,
                    row.MaxArmyPoints,
                    row.FreeSupplyPoints,
                    row.FreeCharacterCount)
                : defaults[round - 1];
        }

        return rows;
    }
}
