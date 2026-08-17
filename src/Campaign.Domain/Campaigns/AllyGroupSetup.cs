namespace Campaign.Domain.Campaigns;

/// <summary>
/// A validated ally group that two or more factions may join.
/// </summary>
public sealed class AllyGroupSetup
{
    /// <summary>
    /// Initializes a validated ally group.
    /// </summary>
    /// <param name="name">The group name.</param>
    /// <param name="color">The unique #RRGGBB overlay color.</param>
    public AllyGroupSetup(string name, string color)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(color);
        Name = name;
        Color = color;
    }

    /// <summary>Gets the ally-group name.</summary>
    public string Name { get; }

    /// <summary>Gets the unique overlay color.</summary>
    public string Color { get; }
}
