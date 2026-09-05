namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// Bundled Hunt in Estalia values for supply clamps, battle-report defaults, and that preset's army table.
/// </summary>
public static class HuntInEstaliaDefaults
{
    /// <summary>Default supply points for new terrain and structure catalog rows.</summary>
    public const int SupplyPoints = 1;

    /// <summary>Supply subtracted from map supply when a player has split forces. Used as a raw amount unless percent mode is on.</summary>
    public const int SplitForceSupplyPenaltyValue = 1;

    /// <summary>Whether the split-force supply penalty is a percent of map supply. The default is a raw amount.</summary>
    public const bool SplitForceSupplyPenaltyIsPercent = false;

    /// <summary>Legacy catalog percent used when older JSON omits the raw-or-percent flag.</summary>
    public const int LegacySplitForceSupplyPenaltyPercent = 25;

    /// <summary>Each split force keeps at least this many map supply points after the penalty.</summary>
    public const int SplitForceMinimumMapSupply = 1;

    /// <summary>Army points after an attacker/defender mission advantage are never below this.</summary>
    public const int MinimumArmyPoints = 500;

    /// <summary>Supply points after an attacker/defender mission advantage are never below this.</summary>
    public const int MinimumSupplyPoints = 1;

    /// <summary>Percent added to a side's round army-point cap for each extra allied player in the same fight.</summary>
    public const int AlliedExtraPlayerArmyPercent = 25;

    /// <summary>Allied army-point shares round up to this increment.</summary>
    public const int AlliedArmyPointsRoundTo = 10;

    private static readonly (int MaxArmyPoints, int FreeSupplyPoints, int FreeCharacterCount)[] Template =
    [
        (500, 1, 1),
        (750, 1, 1),
        (1000, 1, 1),
        (1250, 2, 1),
        (1500, 2, 1),
        (2000, 2, 2),
        (2500, 3, 2),
        (3000, 3, 2),
    ];

    /// <summary>
    /// Returns one army-escalation row per round, copying the last Hunt in Estalia row when the campaign is longer.
    /// </summary>
    /// <param name="roundCount">The configured round count.</param>
    /// <returns>Escalation rows numbered from 1.</returns>
    public static IReadOnlyList<RoundArmyEscalationSetup> ArmyEscalations(int roundCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(roundCount, 1);
        var rows = new RoundArmyEscalationSetup[roundCount];
        var last = Template[^1];
        for (var round = 1; round <= roundCount; round++)
        {
            var (MaxArmyPoints, FreeSupplyPoints, FreeCharacterCount) = round <= Template.Length ? Template[round - 1] : last;
            rows[round - 1] = new RoundArmyEscalationSetup(
                round,
                MaxArmyPoints,
                FreeSupplyPoints,
                FreeCharacterCount);
        }

        return rows;
    }
}
