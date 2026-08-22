using Campaign.Domain.Play;

namespace Campaign.Domain.Maps;

/// <summary>
/// A polygonal map region with required terrain, optional structure, overlay color, ownership, and spawn metadata.
/// </summary>
public sealed class Territory
{
    /// <summary>
    /// Initializes a validated territory.
    /// </summary>
    /// <param name="id">The territory identifier.</param>
    /// <param name="displayNumber">The unique display number used when no name is set.</param>
    /// <param name="name">The optional unique name.</param>
    /// <param name="description">The optional description.</param>
    /// <param name="polygon">The vertices in normalized map coordinates, without a duplicated closing point.</param>
    /// <param name="terrainTypeId">The required terrain type.</param>
    /// <param name="structureTypeId">The optional structure.</param>
    /// <param name="overlayColor">The optional #RRGGBB overlay color.</param>
    /// <param name="ownerFactionId">The owning faction, or null when Neutral.</param>
    /// <param name="spawnFactionId">The spawn-location faction, if any.</param>
    /// <param name="structureCondition">The structure condition when a structure is present.</param>
    /// <param name="ownerSubfaction">The owning required subfaction, when ownership is subfaction-specific.</param>
    /// <param name="spawnSubfaction">The spawn required subfaction, when spawn is subfaction-specific.</param>
    public Territory(
        Guid id,
        int displayNumber,
        string? name,
        string? description,
        IReadOnlyList<MapPoint> polygon,
        Guid terrainTypeId,
        Guid? structureTypeId,
        string? overlayColor,
        Guid? ownerFactionId,
        Guid? spawnFactionId,
        StructureCondition structureCondition = StructureCondition.Operational,
        string? ownerSubfaction = null,
        string? spawnSubfaction = null)
    {
        ArgumentNullException.ThrowIfNull(polygon);
        Id = id;
        DisplayNumber = displayNumber;
        Name = name;
        Description = description;
        Polygon = polygon;
        TerrainTypeId = terrainTypeId;
        StructureTypeId = structureTypeId;
        OverlayColor = overlayColor;
        OwnerFactionId = ownerFactionId;
        SpawnFactionId = spawnFactionId;
        StructureCondition = structureTypeId is null ? StructureCondition.Operational : structureCondition;
        OwnerSubfaction = ownerFactionId is null ? null : ownerSubfaction;
        SpawnSubfaction = spawnFactionId is null ? null : spawnSubfaction;
    }

    /// <summary>Gets the territory identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the unique display number used when no name is set.</summary>
    public int DisplayNumber { get; }

    /// <summary>Gets the optional unique name.</summary>
    public string? Name { get; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; }

    /// <summary>Gets the polygon vertices.</summary>
    public IReadOnlyList<MapPoint> Polygon { get; }

    /// <summary>Gets the required terrain type.</summary>
    public Guid TerrainTypeId { get; }

    /// <summary>Gets the optional structure.</summary>
    public Guid? StructureTypeId { get; }

    /// <summary>Gets the optional overlay color.</summary>
    public string? OverlayColor { get; }

    /// <summary>Gets the owning faction, or null when the territory is Neutral.</summary>
    public Guid? OwnerFactionId { get; }

    /// <summary>Gets the spawn-location faction, if any.</summary>
    public Guid? SpawnFactionId { get; }

    /// <summary>Gets the owning required subfaction, when ownership is subfaction-specific.</summary>
    public string? OwnerSubfaction { get; }

    /// <summary>Gets the spawn required subfaction, when spawn is subfaction-specific.</summary>
    public string? SpawnSubfaction { get; }

    /// <summary>Gets the structure condition when a structure is present.</summary>
    public StructureCondition StructureCondition { get; }

    /// <summary>
    /// Gets the name when present; otherwise the display number as a decimal string.
    /// </summary>
    public string DisplayLabel => Name ?? DisplayNumber.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
