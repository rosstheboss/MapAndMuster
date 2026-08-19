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
        int supplyCostingUnitCount = 0,
        string? armyListText = null,
        string? armyListGameSystem = null,
        ArmyListBuilder armyListBuilder = ArmyListBuilder.Other,
        IReadOnlyList<ArmyListSupplyCategory>? supplyCategories = null,
        bool usedExtraBlackPowder = false,
        int magicalSupplyRerolls = 0)
    {
        ArgumentNullException.ThrowIfNull(answers);
        ArgumentOutOfRangeException.ThrowIfNegative(victoryPoints);
        ArgumentOutOfRangeException.ThrowIfNegative(armyPoints);
        ArgumentOutOfRangeException.ThrowIfNegative(differentialBattlePoints);
        ArgumentOutOfRangeException.ThrowIfNegative(bonusBattlePoints);
        ArgumentOutOfRangeException.ThrowIfNegative(supplyCostingUnitCount);
        ArgumentOutOfRangeException.ThrowIfNegative(magicalSupplyRerolls);
        ForceId = forceId;
        VictoryPoints = victoryPoints;
        ArmyPoints = armyPoints;
        DifferentialBattlePoints = differentialBattlePoints;
        BonusBattlePoints = bonusBattlePoints;
        KilledEnemyGeneral = killedEnemyGeneral;
        DestroyedEnemySupplyLine = destroyedEnemySupplyLine;
        Answers = answers;
        SupplyCostingUnitCount = supplyCostingUnitCount;
        ArmyListText = armyListText;
        ArmyListGameSystem = armyListGameSystem;
        ArmyListBuilder = armyListBuilder;
        SupplyCategories = supplyCategories ?? [];
        UsedExtraBlackPowder = usedExtraBlackPowder;
        MagicalSupplyRerolls = magicalSupplyRerolls;
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

    /// <summary>Gets optional pasted army-list text for opponent and staff review.</summary>
    public string? ArmyListText { get; }

    /// <summary>Gets the game system selected when the list was pasted, when any.</summary>
    public string? ArmyListGameSystem { get; }

    /// <summary>Gets which army builder produced the pasted text, when parsing was attempted.</summary>
    public ArmyListBuilder ArmyListBuilder { get; }

    /// <summary>Gets optional per-category supply amounts filled from a parse or edited by the player.</summary>
    public IReadOnlyList<ArmyListSupplyCategory> SupplyCategories { get; }

    /// <summary>Gets whether Extra Black Powder was used this battle (Prepared for Battle).</summary>
    public bool UsedExtraBlackPowder { get; }

    /// <summary>Gets leftover composition supply used as Magical Supply rerolls this battle.</summary>
    public int MagicalSupplyRerolls { get; }

    /// <summary>Supply spent from army-list units plus Extra Black Powder, if used.</summary>
    public int SupplySpend => SupplyCostingUnitCount + (UsedExtraBlackPowder ? 1 : 0);

    /// <summary>
    /// Battle points used to decide the winner before campaign-point conversion.
    /// </summary>
    public int TotalBattlePoints =>
        DifferentialBattlePoints + BonusBattlePoints + Answers.Sum(static answer => answer.BattlePointsValue ?? 0);
}
