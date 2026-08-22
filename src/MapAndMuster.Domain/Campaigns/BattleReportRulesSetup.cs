namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// Campaign-wide questions always asked on a battle report, plus campaign points they award.
/// </summary>
public sealed class BattleReportRulesSetup
{
    /// <summary>
    /// Initializes battle-report questions that apply to every mission.
    /// </summary>
    public BattleReportRulesSetup(
        bool alwaysAskGeneralKill,
        bool alwaysAskSupplyLineDestroyed,
        int generalKillCampaignPoints,
        int supplyLineDestroyedCampaignPoints)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(generalKillCampaignPoints);
        ArgumentOutOfRangeException.ThrowIfNegative(supplyLineDestroyedCampaignPoints);
        AlwaysAskGeneralKill = alwaysAskGeneralKill;
        AlwaysAskSupplyLineDestroyed = alwaysAskSupplyLineDestroyed;
        GeneralKillCampaignPoints = generalKillCampaignPoints;
        SupplyLineDestroyedCampaignPoints = supplyLineDestroyedCampaignPoints;
    }

    /// <summary>Gets the Hunt in Estalia defaults: ask both questions for 1 campaign point each.</summary>
    public static BattleReportRulesSetup Default { get; } = new(
        HuntInEstaliaDefaults.AlwaysAskGeneralKill,
        HuntInEstaliaDefaults.AlwaysAskSupplyLineDestroyed,
        HuntInEstaliaDefaults.GeneralKillCampaignPoints,
        HuntInEstaliaDefaults.SupplyLineDestroyedCampaignPoints);

    /// <summary>Gets whether every battle report asks if the enemy general was slain.</summary>
    public bool AlwaysAskGeneralKill { get; }

    /// <summary>Gets whether every battle report asks if the enemy supply line was destroyed.</summary>
    public bool AlwaysAskSupplyLineDestroyed { get; }

    /// <summary>Gets campaign points awarded for a slain enemy general.</summary>
    public int GeneralKillCampaignPoints { get; }

    /// <summary>Gets campaign points awarded for destroying the enemy supply line.</summary>
    public int SupplyLineDestroyedCampaignPoints { get; }
}
