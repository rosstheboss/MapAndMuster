namespace Campaign.Domain.Campaigns;

/// <summary>
/// How a mission result question is answered on a battle report.
/// </summary>
public enum MissionResultQuestionKind
{
    /// <summary>The reporter answers true or false.</summary>
    Boolean = 0,

    /// <summary>The reporter enters a battle-point amount.</summary>
    BattlePoints = 1,
}
