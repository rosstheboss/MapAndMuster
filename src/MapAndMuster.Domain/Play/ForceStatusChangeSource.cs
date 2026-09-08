namespace MapAndMuster.Domain.Play;

/// <summary>
/// Why a force's named status changed. Recorded on status facts and the public game log.
/// </summary>
public enum ForceStatusChangeSource
{
    /// <summary>A configured catalog enable or clear trigger.</summary>
    Catalog = 0,

    /// <summary>Three consecutive actions occupying water-feature territories.</summary>
    ConsecutiveWater = 1,

    /// <summary>A fought battle lost on a water-feature territory.</summary>
    WaterBattleDefeat = 2,

    /// <summary>Surrender after two consecutive water-feature actions, before fighting.</summary>
    WaterSurrender = 3,

    /// <summary>Sharing a territory with another faction that has Diseased this phase.</summary>
    Contagion = 4,

    /// <summary>Same-player split forces rejoined while one was Diseased.</summary>
    Rejoin = 5,

    /// <summary>A named special-rule effect, such as Bringers of the Plague.</summary>
    SpecialRule = 6,

    /// <summary>A mission win/lose status-change condition.</summary>
    Mission = 7,

    /// <summary>A resolved item-objective choice result.</summary>
    ItemObjective = 8,

    /// <summary>A manager or administrator assigned the status.</summary>
    Staff = 9,

    /// <summary>Hold on a Capital City, City, Supply Depot, or Town while Diseased.</summary>
    SettlementHold = 10,
}
