namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// Bounds and default for how many adjacent territories a force may Move in one action.
/// </summary>
public static class ForceMovementSpeeds
{
    /// <summary>Default movement speed when a faction does not set one.</summary>
    public const int Default = 1;

    /// <summary>Minimum configurable movement speed.</summary>
    public const int Min = 1;

    /// <summary>Maximum configurable movement speed, including item bonuses.</summary>
    public const int Max = 10;
}
