namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// Terrain and structure types and tags a faction prefers when a choice must be made,
/// including random spawn placement.
/// </summary>
public sealed class FactionPreference
{
    /// <summary>An empty preference list.</summary>
    public static FactionPreference None { get; } = new([], [], [], []);

    /// <summary>
    /// Initializes a preference list. Identifiers are stored distinct and in input order.
    /// </summary>
    public FactionPreference(
        IReadOnlyList<Guid>? terrainTypeIds,
        IReadOnlyList<Guid>? terrainTagIds,
        IReadOnlyList<Guid>? structureTypeIds,
        IReadOnlyList<Guid>? structureTagIds)
    {
        TerrainTypeIds = Distinct(terrainTypeIds);
        TerrainTagIds = Distinct(terrainTagIds);
        StructureTypeIds = Distinct(structureTypeIds);
        StructureTagIds = Distinct(structureTagIds);
    }

    /// <summary>Gets preferred terrain types.</summary>
    public IReadOnlyList<Guid> TerrainTypeIds { get; }

    /// <summary>Gets preferred terrain tags.</summary>
    public IReadOnlyList<Guid> TerrainTagIds { get; }

    /// <summary>Gets preferred structure types.</summary>
    public IReadOnlyList<Guid> StructureTypeIds { get; }

    /// <summary>Gets preferred structure tags.</summary>
    public IReadOnlyList<Guid> StructureTagIds { get; }

    /// <summary>Gets how many distinct preferences are listed.</summary>
    public int Count =>
        TerrainTypeIds.Count + TerrainTagIds.Count + StructureTypeIds.Count + StructureTagIds.Count;

    private static IReadOnlyList<Guid> Distinct(IReadOnlyList<Guid>? ids)
    {
        if (ids is null || ids.Count == 0)
        {
            return [];
        }

        return [.. ids.Where(static id => id != Guid.Empty).Distinct()];
    }
}
