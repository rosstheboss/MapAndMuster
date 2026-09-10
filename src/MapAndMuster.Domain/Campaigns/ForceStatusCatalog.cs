namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// Named force-status preset copied into a campaign. Normal is not a catalog status.
/// Descriptions are generic campaign-app text, not proprietary rules prose.
/// </summary>
public static class ForceStatusCatalog
{
    /// <summary>Preset identifier used by setup and The Hunt in Estalia campaign preset.</summary>
    public const string StandardPresetId = "standard-force-statuses";

    /// <summary>Player-facing preset name.</summary>
    public const string StandardPresetName = "Standard force statuses";

    /// <summary>
    /// Standard statuses in list order with priorities 0..4: Diseased, Shaken, Confident,
    /// Exhausted, and Well Rested. Exhausted cancels Well Rested.
    /// </summary>
    public static IReadOnlyList<ForceStatusPreset> Standard { get; } =
    [
        new(
            "Diseased",
            "In battle, before deployment, roll a D6 for every non-Character, non-War Machine, non-Chariot unit. " +
            "On a 1 that unit is Sick and rerolls 6s to Wound unless it has Poisoned attacks. " +
            "The app displays this and does not resolve the tabletop effect. " +
            "Gained after three consecutive actions in water-feature territories, a fought defeat on water, " +
            "surrender after two water-feature actions, contagion from another faction, rejoining a Diseased split, " +
            "or a plague-bearing combat win. Cleared by Hold at a Capital City, City, Supply Depot, or Town. " +
            "Priority 0, so it outranks other standard statuses unless a cancel-out applies.",
            ForceStatusEnableTrigger.Disease,
            ForceStatusClearTrigger.HoldAtSettlement,
            0,
            []),
        new(
            "Shaken",
            "Tabletop battles fought while shaken use the campaign sheet's shaken modifiers. " +
            "The app displays this and does not resolve the tabletop effect.",
            ForceStatusEnableTrigger.BattleLostOrRetreat,
            ForceStatusClearTrigger.Hold,
            1,
            []),
        new(
            "Confident",
            "Tabletop battles fought while confident use the campaign sheet's confident modifiers. " +
            "The app displays this and does not resolve the tabletop effect.",
            ForceStatusEnableTrigger.BattleWon,
            ForceStatusClearTrigger.BattleLostOrRetreat,
            2,
            []),
        new(
            "Exhausted",
            "Tabletop battles fought while exhausted use the campaign sheet's fatigue modifiers. " +
            "The app displays this and does not resolve the tabletop effect. " +
            "Cancels Well Rested: gaining Exhausted while Well Rested leaves the force with no status.",
            ForceStatusEnableTrigger.AfterBattle,
            ForceStatusClearTrigger.Hold,
            3,
            ["Well Rested"]),
        new(
            "Well Rested",
            "Tabletop battles fought while well rested use the campaign sheet's rest modifiers. " +
            "The app displays this and does not resolve the tabletop effect. Hold is the rest action " +
            "that grants this status.",
            ForceStatusEnableTrigger.Hold,
            ForceStatusClearTrigger.AfterMoveOrBattle,
            4,
            []),
    ];

    /// <summary>
    /// Materializes the standard preset with unique identifiers, sequential priorities, and
    /// Exhausted cancelling Well Rested.
    /// </summary>
    public static IReadOnlyList<ForceStatusSetup> CreateStandardSetups()
    {
        return CreateSetups(Standard);
    }

    /// <summary>
    /// Materializes preset rows, assigning identifiers and resolving cancel-out names.
    /// </summary>
    public static IReadOnlyList<ForceStatusSetup> CreateSetups(IReadOnlyList<ForceStatusPreset> presets)
    {
        ArgumentNullException.ThrowIfNull(presets);
        var ids = presets.ToDictionary(
            static preset => preset.Name,
            static _ => Guid.NewGuid(),
            StringComparer.OrdinalIgnoreCase);
        return
        [
            .. presets.Select(preset => new ForceStatusSetup(
                ids[preset.Name],
                preset.Name,
                preset.Effects,
                [new ForceStatusEnableCondition(preset.EnableTrigger, preset.EnableOccurrences)],
                [new ForceStatusClearCondition(preset.ClearTrigger, preset.ClearOccurrences)],
                preset.Priority,
                [
                    .. preset.CancelsStatusNames
                        .Select(name => ids.TryGetValue(name, out var id) ? id : Guid.Empty)
                        .Where(id => id != Guid.Empty),
                ])),
        ];
    }
}

/// <summary>
/// One named entry in the standard force-status preset.
/// </summary>
/// <param name="Name">The status name.</param>
/// <param name="Effects">Tabletop effect text.</param>
/// <param name="EnableTrigger">When the status is applied.</param>
/// <param name="ClearTrigger">When the status returns to Normal.</param>
/// <param name="Priority">Unique ranking from 0 (highest) downward in list order.</param>
/// <param name="CancelsStatusNames">Other preset names this status cancels to Normal.</param>
/// <param name="EnableOccurrences">Consecutive enable-trigger matches required.</param>
/// <param name="ClearOccurrences">Consecutive clear-trigger matches required.</param>
public sealed record ForceStatusPreset(
    string Name,
    string Effects,
    ForceStatusEnableTrigger EnableTrigger,
    ForceStatusClearTrigger ClearTrigger,
    int Priority,
    IReadOnlyList<string> CancelsStatusNames,
    int EnableOccurrences = ForceStatusOccurrences.Default,
    int ClearOccurrences = ForceStatusOccurrences.Default);
