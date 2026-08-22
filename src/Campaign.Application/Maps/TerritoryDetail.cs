namespace Campaign.Application.Maps;

/// <summary>
/// A territory in a map-graph response.
/// </summary>
public sealed class TerritoryDetail
{
    /// <summary>Gets the territory identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the unique display number used when no name is set.</summary>
    public required int DisplayNumber { get; init; }

    /// <summary>Gets the optional unique name.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the polygon vertices in normalized map coordinates.</summary>
    public required IReadOnlyList<MapPointDetail> Polygon { get; init; }

    /// <summary>Gets the campaign terrain type identifier.</summary>
    public required Guid TerrainTypeId { get; init; }

    /// <summary>Gets the optional campaign structure type identifier.</summary>
    public Guid? StructureTypeId { get; init; }

    /// <summary>Gets the structure condition name when a structure is present.</summary>
    public string? StructureCondition { get; init; }

    /// <summary>Gets the optional overlay color as #RRGGBB.</summary>
    public string? OverlayColor { get; init; }

    /// <summary>Gets the owning faction, or null when the territory is Neutral.</summary>
    public Guid? OwnerFactionId { get; init; }

    /// <summary>Gets the owning required subfaction, when ownership is subfaction-specific.</summary>
    public string? OwnerSubfaction { get; init; }

    /// <summary>Gets the spawn-location faction, if any.</summary>
    public Guid? SpawnFactionId { get; init; }

    /// <summary>Gets the spawn required subfaction, when spawn is subfaction-specific.</summary>
    public string? SpawnSubfaction { get; init; }
}
