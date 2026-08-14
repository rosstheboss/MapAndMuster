namespace Campaign.Domain.Campaigns;

/// <summary>
/// A validated campaign terrain type, including at least one mission.
/// </summary>
public sealed class TerrainTypeSetup
{
    /// <summary>
    /// Initializes a validated terrain type.
    /// </summary>
    /// <param name="id">The terrain type identifier.</param>
    /// <param name="name">The terrain type name.</param>
    /// <param name="color">The unique #RRGGBB overlay color.</param>
    /// <param name="missions">The missions for this terrain type.</param>
    public TerrainTypeSetup(Guid id, string name, string color, IReadOnlyList<MissionSetup> missions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(color);
        ArgumentNullException.ThrowIfNull(missions);
        Id = id;
        Name = name;
        Color = color;
        Missions = missions;
    }

    /// <summary>Gets the terrain type identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the terrain type name.</summary>
    public string Name { get; }

    /// <summary>Gets the unique overlay color.</summary>
    public string Color { get; }

    /// <summary>Gets the missions.</summary>
    public IReadOnlyList<MissionSetup> Missions { get; }
}
