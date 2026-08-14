namespace Campaign.Domain.Campaigns;

/// <summary>
/// A validated faction in a campaign setup, including optional subfactions and ally membership.
/// </summary>
public sealed class FactionSetup
{
    /// <summary>
    /// Initializes a validated faction.
    /// </summary>
    /// <param name="name">The faction name.</param>
    /// <param name="subfactions">The subfaction names.</param>
    /// <param name="allyGroupName">The ally group this faction joins, if any.</param>
    public FactionSetup(string name, IReadOnlyList<string> subfactions, string? allyGroupName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(subfactions);
        Name = name;
        Subfactions = subfactions;
        AllyGroupName = allyGroupName;
    }

    /// <summary>Gets the faction name.</summary>
    public string Name { get; }

    /// <summary>Gets the subfaction names.</summary>
    public IReadOnlyList<string> Subfactions { get; }

    /// <summary>Gets the ally-group name this faction joins, if any.</summary>
    public string? AllyGroupName { get; }
}
