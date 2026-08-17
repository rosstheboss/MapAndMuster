namespace Campaign.Domain.Campaigns;

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

    /// <summary>Gets whether this terrain is a water feature. Defaults to false.</summary>
    public bool? IsWaterFeature { get; init; }
}
