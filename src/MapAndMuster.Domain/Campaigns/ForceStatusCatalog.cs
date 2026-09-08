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
    /// Standard statuses in catalog order: Diseased, Shaken, Confident, Exhausted, and Well Rested.
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
            "Diseased overrides other catalog statuses.",
            ForceStatusEnableTrigger.Disease,
            ForceStatusClearTrigger.HoldAtSettlement),
        new(
            "Shaken",
            "Tabletop battles fought while shaken use the campaign sheet's shaken modifiers. " +
            "The app displays this and does not resolve the tabletop effect.",
            ForceStatusEnableTrigger.BattleLostOrRetreat,
            ForceStatusClearTrigger.Hold),
        new(
            "Confident",
            "Tabletop battles fought while confident use the campaign sheet's confident modifiers. " +
            "The app displays this and does not resolve the tabletop effect.",
            ForceStatusEnableTrigger.BattleWon,
            ForceStatusClearTrigger.BattleLostOrRetreat),
        new(
            "Exhausted",
            "Tabletop battles fought while exhausted use the campaign sheet's fatigue modifiers. " +
            "The app displays this and does not resolve the tabletop effect.",
            ForceStatusEnableTrigger.AfterBattle,
            ForceStatusClearTrigger.Hold),
        new(
            "Well Rested",
            "Tabletop battles fought while well rested use the campaign sheet's rest modifiers. " +
            "The app displays this and does not resolve the tabletop effect. Hold is the rest action " +
            "that grants this status.",
            ForceStatusEnableTrigger.Hold,
            ForceStatusClearTrigger.AfterMoveOrBattle),
    ];
}

/// <summary>
/// One named entry in the standard force-status preset.
/// </summary>
/// <param name="Name">The status name.</param>
/// <param name="Effects">Tabletop effect text.</param>
/// <param name="EnableTrigger">When the status is applied.</param>
/// <param name="ClearTrigger">When the status returns to Normal.</param>
public sealed record ForceStatusPreset(
    string Name,
    string Effects,
    ForceStatusEnableTrigger EnableTrigger,
    ForceStatusClearTrigger ClearTrigger);
