namespace Campaign.Domain.Play;

/// <summary>
/// Calculated supply shown for a player: map holdings, round bonus, split penalty, and remaining temporary points.
/// </summary>
/// <param name="UserId">The player.</param>
/// <param name="MapSupplyPoints">Supply from connected territories and operational structures.</param>
/// <param name="RoundFreeSupplyPoints">Free supply granted this round.</param>
/// <param name="SplitPenaltyPoints">Supply subtracted because the player currently has split forces.</param>
/// <param name="TemporarySupplyPoints">Unspent player-pool temporary supply from pillage and destroy.</param>
/// <param name="CurrentSupplyPoints">Maximum one force can spend if assigned the entire remaining temporary pool.</param>
/// <param name="MaxArmyPoints">Configured maximum army points for the current round.</param>
/// <param name="FreeCharacterCount">Free characters whose base cost does not count against supply this round.</param>
/// <param name="IsSplit">Whether the player currently has more than one force.</param>
public sealed record PlayerSupplySnapshot(
    Guid UserId,
    int MapSupplyPoints,
    int RoundFreeSupplyPoints,
    int SplitPenaltyPoints,
    int TemporarySupplyPoints,
    int CurrentSupplyPoints,
    int MaxArmyPoints,
    int FreeCharacterCount,
    bool IsSplit)
{
    /// <summary>
    /// Map supply after the split-force penalty (minimum 1 when split and map supply is positive), plus round free supply.
    /// Temporary points are not included.
    /// </summary>
    public int ForceAllowancePoints => Math.Max(0, MapSupplyPoints + RoundFreeSupplyPoints - SplitPenaltyPoints);
}
