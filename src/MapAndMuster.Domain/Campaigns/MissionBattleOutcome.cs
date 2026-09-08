namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// Which battle result a mission status-change condition matches.
/// </summary>
public enum MissionBattleOutcome
{
    /// <summary>The force won a fought battle.</summary>
    Win = 0,

    /// <summary>The force lost a fought battle.</summary>
    Lose = 1,
}
