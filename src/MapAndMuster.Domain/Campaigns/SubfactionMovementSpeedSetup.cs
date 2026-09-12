namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// Movement-speed override for one named subfaction.
/// </summary>
public sealed class SubfactionMovementSpeedSetup
{
    /// <summary>
    /// Initializes a subfaction movement-speed override.
    /// </summary>
    /// <param name="name">The subfaction name.</param>
    /// <param name="speed">How many adjacent territories the subfaction may Move in one action.</param>
    public SubfactionMovementSpeedSetup(string name, int speed)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(speed, ForceMovementSpeeds.Min);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(speed, ForceMovementSpeeds.Max);
        Name = name.Trim();
        Speed = speed;
    }

    /// <summary>Gets the subfaction name.</summary>
    public string Name { get; }

    /// <summary>Gets the movement speed for this subfaction.</summary>
    public int Speed { get; }
}

/// <summary>
/// User-supplied movement-speed override for one subfaction.
/// </summary>
public sealed class SubfactionMovementSpeedInput
{
    /// <summary>Gets the subfaction name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the movement speed. Omitted means inherit the faction speed.</summary>
    public int? Speed { get; init; }
}
