namespace Campaign.Domain.Campaigns;

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

    /// <summary>Applied when the force occupies a water-feature territory after resolution.</summary>
    OccupyingWater = 4,
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

    /// <summary>Cleared after the force Holds while not occupying a water-feature territory.</summary>
    HoldWhileNotWater = 6,
}
