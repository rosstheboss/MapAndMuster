namespace Campaign.Domain.Campaigns;

/// <summary>
/// Which battle role receives a mission's army-point or supply-point advantage.
/// </summary>
public enum MissionAdvantageSide
{
    /// <summary>The attacking force and its allies.</summary>
    Attacker = 0,

    /// <summary>The defending force and its allies.</summary>
    Defender = 1,
}
