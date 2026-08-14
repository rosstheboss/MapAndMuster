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
    public AllyGroupSetup(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    /// <summary>Gets the ally-group name.</summary>
    public string Name { get; }
}
