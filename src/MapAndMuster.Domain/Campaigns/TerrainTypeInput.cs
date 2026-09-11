namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// User-supplied terrain type for campaign setup.
/// </summary>
public sealed class TerrainTypeInput
{
    /// <summary>Gets the client-assigned identifier, when present.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets the terrain type name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the unique overlay color as #RRGGBB.</summary>
    public required string Color { get; init; }

    /// <summary>Gets nested missions. At least one is required.</summary>
    public IReadOnlyList<MissionInput>? Missions { get; init; }

    /// <summary>
    /// Gets whether this terrain is a water feature. Accepted only when loading older catalogs;
    /// new saves assign the Water terrain tag instead.
    /// </summary>
    public bool? IsWaterFeature { get; init; }

    /// <summary>Gets supply points granted by a controlled territory of this terrain. Defaults to 1.</summary>
    public int? SupplyPoints { get; init; }

    /// <summary>Gets terrain-catalog tag identifiers assigned to this type.</summary>
    public IReadOnlyList<Guid>? TagIds { get; init; }
}
