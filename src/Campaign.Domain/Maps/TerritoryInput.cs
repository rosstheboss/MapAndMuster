namespace Campaign.Domain.Maps;

/// <summary>
/// Unvalidated territory fields from a map-editor save.
/// </summary>
public sealed class TerritoryInput
{
    /// <summary>Gets the client-assigned identifier, when present.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets the display number used when no name is set.</summary>
    public int DisplayNumber { get; init; }

    /// <summary>Gets the optional unique name.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the polygon vertices in normalized map coordinates.</summary>
    public IReadOnlyList<MapPointInput>? Polygon { get; init; }

    /// <summary>Gets the campaign terrain type identifier.</summary>
    public Guid? TerrainTypeId { get; init; }

    /// <summary>Gets the optional campaign structure type identifier.</summary>
    public Guid? StructureTypeId { get; init; }

    /// <summary>Gets the optional overlay color as #RRGGBB.</summary>
    public string? OverlayColor { get; init; }

    /// <summary>Gets the owning faction, or null when the territory is neutral.</summary>
    public Guid? OwnerFactionId { get; init; }

    /// <summary>Gets the spawn-location faction, when this territory is a spawn.</summary>
    public Guid? SpawnFactionId { get; init; }
}
