namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// Mechanical effects an item objective may grant its possessor. Custom is display-only.
/// </summary>
public enum ItemObjectiveEffectKind
{
    /// <summary>A defeated opponent is sent to spawn instead of choosing a retreat.</summary>
    PushDefeatedOpponentToSpawn = 0,

    /// <summary>Adds to the holder's force movement speed.</summary>
    AddMovementSpeed = 1,

    /// <summary>Adds or subtracts map supply, never below 1 while this effect applies.</summary>
    ModifySupply = 2,

    /// <summary>Grants the Teleport Randomly action to a secret allied or Neutral non-spawn territory.</summary>
    TeleportToRandomEmptyNonSpawn = 3,

    /// <summary>The holder has a catalog status while possessing the item.</summary>
    InflictStatusWhileHeld = 4,

    /// <summary>The holder is immune to all statuses or to listed statuses.</summary>
    ImmuneToStatuses = 5,

    /// <summary>Other forces sharing the holder's territory gain listed statuses.</summary>
    InflictStatusOnSharedTerritory = 6,

    /// <summary>Suppresses effects of item objectives whose location is adjacent.</summary>
    NullifyAdjacentItemObjectives = 7,

    /// <summary>Adds army points by a number or a percent of the round cap.</summary>
    ModifyArmyPoints = 8,

    /// <summary>Suspends the holder's ally group and/or forces additional alliances.</summary>
    OverrideAlliances = 9,

    /// <summary>Display-only reminder for tabletop play. Does not execute map code.</summary>
    Custom = 10,

    /// <summary>
    /// Grants Teleport to Specific Territory, which recharges over three action phases.
    /// </summary>
    TeleportToChosenNonSpawnOncePerRound = 11,
}
