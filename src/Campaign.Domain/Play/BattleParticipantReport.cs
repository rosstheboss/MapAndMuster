namespace Campaign.Domain.Play;

/// <summary>
/// One force's structured battle report, including both sides when a player reports for both.
/// </summary>
public sealed class BattleParticipantReport
{
    /// <summary>
    /// Initializes a participant report.
    /// </summary>
    public BattleParticipantReport(
        Guid forceId,
        int victoryPoints,
        int armyPoints,
        int differentialBattlePoints,
        int bonusBattlePoints,
        bool killedEnemyGeneral,
        bool destroyedEnemySupplyLine,
        IReadOnlyList<BattleQuestionAnswer> answers,
        int supplyCostingUnitCount = 0)
    {
        ArgumentNullException.ThrowIfNull(answers);
        ArgumentOutOfRangeException.ThrowIfNegative(victoryPoints);
        ArgumentOutOfRangeException.ThrowIfNegative(armyPoints);
        ArgumentOutOfRangeException.ThrowIfNegative(differentialBattlePoints);
        ArgumentOutOfRangeException.ThrowIfNegative(bonusBattlePoints);
        ArgumentOutOfRangeException.ThrowIfNegative(supplyCostingUnitCount);
        ForceId = forceId;
        VictoryPoints = victoryPoints;
        ArmyPoints = armyPoints;
        DifferentialBattlePoints = differentialBattlePoints;
        BonusBattlePoints = bonusBattlePoints;
        KilledEnemyGeneral = killedEnemyGeneral;
        DestroyedEnemySupplyLine = destroyedEnemySupplyLine;
        Answers = answers;
        SupplyCostingUnitCount = supplyCostingUnitCount;
    }

    /// <summary>Gets the reported force.</summary>
    public Guid ForceId { get; }

    /// <summary>Gets tabletop victory points.</summary>
    public int VictoryPoints { get; }

    /// <summary>Gets the army size in points used in the battle.</summary>
    public int ArmyPoints { get; }

    /// <summary>Gets battle points converted from victory points by the reporter.</summary>
    public int DifferentialBattlePoints { get; }

    /// <summary>Gets bonus battle points from the mission.</summary>
    public int BonusBattlePoints { get; }

    /// <summary>Gets whether the reporter killed the opponent's general.</summary>
    public bool KilledEnemyGeneral { get; }

    /// <summary>Gets whether the reporter destroyed the enemy supply line.</summary>
    public bool DestroyedEnemySupplyLine { get; }

    /// <summary>Gets answers to the mission's extra questions.</summary>
    public IReadOnlyList<BattleQuestionAnswer> Answers { get; }

    /// <summary>Gets how many supply-costing units (special, rare, and similar) this force fielded.</summary>
    public int SupplyCostingUnitCount { get; }

    /// <summary>
    /// Battle points used to decide the winner before campaign-point conversion.
    /// </summary>
    public int TotalBattlePoints =>
        DifferentialBattlePoints + BonusBattlePoints + Answers.Sum(static answer => answer.BattlePointsValue ?? 0);
}
