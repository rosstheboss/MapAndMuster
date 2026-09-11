namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// How a force-status condition is limited to a place on the map.
/// </summary>
public enum ConditionLocationKind
{
    /// <summary>Matches anywhere.</summary>
    Any = 0,

    /// <summary>Matches one terrain type.</summary>
    TerrainType = 1,

    /// <summary>Matches terrain that has a terrain-catalog tag.</summary>
    TerrainTag = 2,

    /// <summary>Matches one structure type.</summary>
    StructureType = 3,

    /// <summary>Matches a structure that has a structure-catalog tag.</summary>
    StructureTag = 4,
}

/// <summary>
/// Optional place a force-status condition must match.
/// </summary>
public sealed class ConditionLocation
{
    /// <summary>A location that matches anywhere.</summary>
    public static ConditionLocation Any { get; } = new(ConditionLocationKind.Any, null, null);

    /// <summary>
    /// Initializes a location filter.
    /// </summary>
    public ConditionLocation(ConditionLocationKind kind, Guid? typeId, Guid? tagId)
    {
        Kind = kind;
        TypeId = typeId is Guid id && id != Guid.Empty ? id : null;
        TagId = tagId is Guid tag && tag != Guid.Empty ? tag : null;
    }

    /// <summary>Gets how this location is limited.</summary>
    public ConditionLocationKind Kind { get; }

    /// <summary>Gets the terrain or structure type when <see cref="Kind"/> is a type filter.</summary>
    public Guid? TypeId { get; }

    /// <summary>Gets the terrain or structure tag when <see cref="Kind"/> is a tag filter.</summary>
    public Guid? TagId { get; }

    /// <summary>
    /// Returns a fingerprint used to detect duplicate conditions.
    /// </summary>
    public string Fingerprint()
    {
        return Kind switch
        {
            ConditionLocationKind.Any => "any",
            ConditionLocationKind.TerrainType => $"terrainType:{TypeId:D}",
            ConditionLocationKind.TerrainTag => $"terrainTag:{TagId:D}",
            ConditionLocationKind.StructureType => $"structureType:{TypeId:D}",
            ConditionLocationKind.StructureTag => $"structureTag:{TagId:D}",
            _ => Kind.ToString(),
        };
    }

    /// <summary>
    /// Returns whether this location is strictly broader than <paramref name="other"/> given catalog tag assignments.
    /// </summary>
    public bool StrictlySubsumes(ConditionLocation other, Func<Guid, IReadOnlyList<Guid>> tagsForType)
    {
        ArgumentNullException.ThrowIfNull(other);
        ArgumentNullException.ThrowIfNull(tagsForType);
        if (Kind == ConditionLocationKind.Any && other.Kind != ConditionLocationKind.Any)
        {
            return true;
        }

        if (Kind == ConditionLocationKind.TerrainTag
            && other.Kind == ConditionLocationKind.TerrainType
            && TagId is { } terrainTag
            && other.TypeId is { } terrainType
            && tagsForType(terrainType).Contains(terrainTag))
        {
            return true;
        }

        return Kind == ConditionLocationKind.StructureTag
            && other.Kind == ConditionLocationKind.StructureType
            && TagId is { } structureTag
            && other.TypeId is { } structureType
            && tagsForType(structureType).Contains(structureTag);
    }

    /// <summary>
    /// Returns whether the force's current or battle place matches this filter.
    /// Destroyed structures do not match structure filters.
    /// </summary>
    public bool Matches(
        Guid? terrainTypeId,
        IReadOnlyList<Guid> terrainTagIds,
        Guid? structureTypeId,
        IReadOnlyList<Guid> structureTagIds)
    {
        ArgumentNullException.ThrowIfNull(terrainTagIds);
        ArgumentNullException.ThrowIfNull(structureTagIds);
        return Kind switch
        {
            ConditionLocationKind.Any => true,
            ConditionLocationKind.TerrainType => TypeId is { } terrain && terrainTypeId == terrain,
            ConditionLocationKind.TerrainTag => TagId is { } terrainTag && terrainTagIds.Contains(terrainTag),
            ConditionLocationKind.StructureType => TypeId is { } structure && structureTypeId == structure,
            ConditionLocationKind.StructureTag => TagId is { } structureTag && structureTagIds.Contains(structureTag),
            _ => false,
        };
    }
}
