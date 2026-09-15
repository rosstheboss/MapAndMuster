namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// When a configured force status is applied. Normal is the absence of a status.
/// </summary>
public enum ForceStatusEnableTrigger
{
    /// <summary>Applied after the force Holds.</summary>
    Hold = 0,

    /// <summary>Applied after the force fights a resolved battle, including a draw.</summary>
    AfterBattle = 1,

    /// <summary>Applied after the force wins a resolved battle.</summary>
    BattleWon = 2,

    /// <summary>Applied after the force loses a resolved battle or is forced to retreat.</summary>
    BattleLostOrRetreat = 3,

    /// <summary>Legacy occupying-water trigger. Mapped to ConsecutiveActions on the Water terrain tag.</summary>
    OccupyingWater = 4,

    /// <summary>Legacy named-Diseased engine trigger. Mapped to Water catalog conditions.</summary>
    Disease = 5,

    /// <summary>Applied after any resolved action phase while the location still matches.</summary>
    ConsecutiveActions = 6,

    /// <summary>Applied after the force surrenders. Occurrences count consecutive matching action phases at that location.</summary>
    Surrender = 7,

    /// <summary>Applied after a successful Build.</summary>
    Build = 8,

    /// <summary>Applied after a successful Pillage that does not destroy the structure.</summary>
    Pillage = 9,

    /// <summary>Applied after a successful Repair.</summary>
    Repair = 10,

    /// <summary>Applied after a successful destroy (second Pillage or DestroyImmediately).</summary>
    Destroy = 11,

    /// <summary>
    /// Applied after occupying a territory with another force that currently has this status.
    /// Passing through during a multi-territory Move does not count.
    /// </summary>
    OccupyingWithThisStatus = 12,

    /// <summary>
    /// Applied after occupying a territory with another force that currently has a chosen status.
    /// Passing through during a multi-territory Move does not count.
    /// </summary>
    OccupyingWithSpecifiedStatus = 13,

    /// <summary>
    /// Applied after a resolved battle when this force or an opposing participant achieved a chosen
    /// standard battle-result question.
    /// </summary>
    StandardBattleResultQuestion = 14,

    /// <summary>
    /// Applied after an action phase while the force has no chain access to a structure type or tag.
    /// </summary>
    CutOffFromStructure = 15,

    /// <summary>
    /// Applied after an action phase while the force has no chain access to its spawn or an ally spawn.
    /// </summary>
    CutOffFromSpawn = 16,

    /// <summary>
    /// Applied after an action phase when the force newly gains a contiguous route to its spawn or an ally spawn.
    /// </summary>
    ReunitedWithSpawn = 17,

    /// <summary>
    /// Applied after an action phase when the force newly gains chain access to a structure type or tag.
    /// </summary>
    ReunitedWithStructure = 18,

    /// <summary>Applied after a special action such as teleport succeeds.</summary>
    SpecialActionSucceeded = 19,

    /// <summary>Applied after a special action such as teleport fails or is interrupted.</summary>
    SpecialActionFailed = 20,
}

/// <summary>
/// When a configured force status is cleared back to Normal.
/// </summary>
public enum ForceStatusClearTrigger
{
    /// <summary>Cleared after the force Holds.</summary>
    Hold = 0,

    /// <summary>Cleared after the force Moves or Splits.</summary>
    AfterMove = 1,

    /// <summary>Cleared after the force fights a resolved battle.</summary>
    AfterBattle = 2,

    /// <summary>Cleared after the force Moves, Splits, or fights a resolved battle.</summary>
    AfterMoveOrBattle = 3,

    /// <summary>Cleared after the force wins a resolved battle.</summary>
    BattleWon = 4,

    /// <summary>Cleared after the force loses a resolved battle or is forced to retreat.</summary>
    BattleLostOrRetreat = 5,

    /// <summary>Legacy Hold-while-not-water trigger. Mapped to Hold anywhere.</summary>
    HoldWhileNotWater = 6,

    /// <summary>Legacy Hold-at-settlement trigger. Mapped to Hold on Capital City, City, Supply Depot, and Town.</summary>
    HoldAtSettlement = 7,

    /// <summary>Cleared after consecutive matching action phases.</summary>
    ConsecutiveActions = 8,

    /// <summary>Cleared after the force surrenders. Occurrences count consecutive matching action phases at that location.</summary>
    Surrender = 9,

    /// <summary>Cleared after a successful Build.</summary>
    Build = 10,

    /// <summary>Cleared after a successful Pillage that does not destroy the structure.</summary>
    Pillage = 11,

    /// <summary>Cleared after a successful Repair.</summary>
    Repair = 12,

    /// <summary>Cleared after a successful destroy (second Pillage or DestroyImmediately).</summary>
    Destroy = 13,

    /// <summary>
    /// Cleared after occupying a territory with another force that currently has this status.
    /// Passing through during a multi-territory Move does not count.
    /// </summary>
    OccupyingWithThisStatus = 14,

    /// <summary>
    /// Cleared after occupying a territory with another force that currently has a chosen status.
    /// Passing through during a multi-territory Move does not count.
    /// </summary>
    OccupyingWithSpecifiedStatus = 15,

    /// <summary>
    /// Cleared after a resolved battle when this force or an opposing participant achieved a chosen
    /// standard battle-result question.
    /// </summary>
    StandardBattleResultQuestion = 16,

    /// <summary>
    /// Cleared after an action phase while the force has no chain access to a structure type or tag.
    /// </summary>
    CutOffFromStructure = 17,

    /// <summary>
    /// Cleared after an action phase while the force has no chain access to its spawn or an ally spawn.
    /// </summary>
    CutOffFromSpawn = 18,

    /// <summary>
    /// Cleared after an action phase when the force newly gains a contiguous route to its spawn or an ally spawn.
    /// </summary>
    ReunitedWithSpawn = 19,

    /// <summary>
    /// Cleared after an action phase when the force newly gains chain access to a structure type or tag.
    /// </summary>
    ReunitedWithStructure = 20,
}

/// <summary>
/// Classifies force-status triggers that need extra catalog data or map-access evaluation.
/// </summary>
public static class ForceStatusTriggerKinds
{
    /// <summary>Returns whether the trigger is a chosen standard battle-result question.</summary>
    public static bool RequiresQuestion(ForceStatusEnableTrigger trigger)
    {
        return trigger == ForceStatusEnableTrigger.StandardBattleResultQuestion;
    }

    /// <summary>Returns whether the trigger is a chosen standard battle-result question.</summary>
    public static bool RequiresQuestion(ForceStatusClearTrigger trigger)
    {
        return trigger == ForceStatusClearTrigger.StandardBattleResultQuestion;
    }

    /// <summary>Returns whether the trigger is spawn-chain cut-off or reunion.</summary>
    public static bool IsSpawnAccess(ForceStatusEnableTrigger trigger)
    {
        return trigger is ForceStatusEnableTrigger.CutOffFromSpawn or ForceStatusEnableTrigger.ReunitedWithSpawn;
    }

    /// <summary>Returns whether the trigger is spawn-chain cut-off or reunion.</summary>
    public static bool IsSpawnAccess(ForceStatusClearTrigger trigger)
    {
        return trigger is ForceStatusClearTrigger.CutOffFromSpawn or ForceStatusClearTrigger.ReunitedWithSpawn;
    }

    /// <summary>Returns whether the trigger is structure-chain cut-off or reunion.</summary>
    public static bool IsStructureAccess(ForceStatusEnableTrigger trigger)
    {
        return trigger is ForceStatusEnableTrigger.CutOffFromStructure or ForceStatusEnableTrigger.ReunitedWithStructure;
    }

    /// <summary>Returns whether the trigger is structure-chain cut-off or reunion.</summary>
    public static bool IsStructureAccess(ForceStatusClearTrigger trigger)
    {
        return trigger is ForceStatusClearTrigger.CutOffFromStructure or ForceStatusClearTrigger.ReunitedWithStructure;
    }

    /// <summary>Returns whether the trigger reads current or previous territory-chain access.</summary>
    public static bool IsTerritoryAccess(ForceStatusEnableTrigger trigger)
    {
        return IsSpawnAccess(trigger) || IsStructureAccess(trigger);
    }

    /// <summary>Returns whether the trigger reads current or previous territory-chain access.</summary>
    public static bool IsTerritoryAccess(ForceStatusClearTrigger trigger)
    {
        return IsSpawnAccess(trigger) || IsStructureAccess(trigger);
    }
}
