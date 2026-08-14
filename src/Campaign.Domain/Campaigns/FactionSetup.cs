namespace Campaign.Domain.Campaigns;

/// <summary>
/// A validated faction in a campaign setup, including optional subfactions and ally membership.
/// </summary>
public sealed class FactionSetup
{
    /// <summary>
    /// Initializes a validated faction.
    /// </summary>
    /// <param name="id">The faction identifier.</param>
    /// <param name="name">The faction name.</param>
    /// <param name="color">The unique faction color.</param>
    /// <param name="subfactions">The subfaction names.</param>
    /// <param name="allyGroupName">The ally group this faction joins, if any.</param>
    /// <param name="requiresSubfaction">Whether a player who chooses this faction must pick a subfaction.</param>
    /// <param name="clearFlagImage">Whether an existing uploaded flag image should be removed.</param>
    public FactionSetup(
        Guid id,
        string name,
        string color,
        IReadOnlyList<string> subfactions,
        string? allyGroupName,
        bool requiresSubfaction,
        bool clearFlagImage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(color);
        ArgumentNullException.ThrowIfNull(subfactions);
        Id = id;
        Name = name;
        Color = color;
        Subfactions = subfactions;
        AllyGroupName = allyGroupName;
        RequiresSubfaction = requiresSubfaction;
        ClearFlagImage = clearFlagImage;
    }

    /// <summary>Gets the faction identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the faction name.</summary>
    public string Name { get; }

    /// <summary>Gets the unique faction color.</summary>
    public string Color { get; }

    /// <summary>Gets the subfaction names.</summary>
    public IReadOnlyList<string> Subfactions { get; }

    /// <summary>Gets the ally-group name this faction joins, if any.</summary>
    public string? AllyGroupName { get; }

    /// <summary>Gets whether a player who chooses this faction must pick a subfaction.</summary>
    public bool RequiresSubfaction { get; }

    /// <summary>Gets whether an existing uploaded flag image should be removed.</summary>
    public bool ClearFlagImage { get; }
}
