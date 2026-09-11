namespace MapAndMuster.Domain.Campaigns;

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
    /// <param name="supplyPoints">Supply points granted by a controlled territory of this terrain.</param>
    /// <param name="tagIds">Terrain-catalog tags assigned to this type.</param>
    public TerrainTypeSetup(
        Guid id,
        string name,
        string color,
        IReadOnlyList<MissionSetup> missions,
        int supplyPoints = HuntInEstaliaDefaults.SupplyPoints,
        IReadOnlyList<Guid>? tagIds = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(color);
        ArgumentNullException.ThrowIfNull(missions);
        ArgumentOutOfRangeException.ThrowIfNegative(supplyPoints);
        Id = id;
        Name = name;
        Color = color;
        Missions = missions;
        SupplyPoints = supplyPoints;
        TagIds = DistinctIds(tagIds);
    }

    /// <summary>Gets the terrain type identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the terrain type name.</summary>
    public string Name { get; }

    /// <summary>Gets the unique overlay color.</summary>
    public string Color { get; }

    /// <summary>Gets the missions.</summary>
    public IReadOnlyList<MissionSetup> Missions { get; }

    /// <summary>Gets supply points granted by a controlled territory of this terrain.</summary>
    public int SupplyPoints { get; }

    /// <summary>Gets terrain-catalog tags assigned to this type.</summary>
    public IReadOnlyList<Guid> TagIds { get; }

    private static IReadOnlyList<Guid> DistinctIds(IReadOnlyList<Guid>? ids)
    {
        if (ids is null || ids.Count == 0)
        {
            return [];
        }

        return [.. ids.Where(static id => id != Guid.Empty).Distinct()];
    }
}
