namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// Catalog facts used to validate force-status location filters.
/// </summary>
internal sealed class ForceStatusLocationCatalog
{
    private readonly Dictionary<Guid, IReadOnlyList<Guid>> _tagsByType;

    public ForceStatusLocationCatalog(
        Guid waterTagId,
        IReadOnlyList<TerrainTypeSetup> terrainTypes,
        IReadOnlyList<StructureTypeSetup> structureTypes,
        IReadOnlyList<CatalogTag> terrainTags,
        IReadOnlyList<CatalogTag> structureTags)
    {
        WaterTagId = waterTagId;
        TerrainTypeIds = terrainTypes.Select(static type => type.Id).ToHashSet();
        StructureTypeIds = structureTypes.Select(static type => type.Id).ToHashSet();
        TerrainTagIds = terrainTags.Select(static tag => tag.Id).ToHashSet();
        StructureTagIds = structureTags.Select(static tag => tag.Id).ToHashSet();
        StructureIdsByName = structureTypes.ToDictionary(
            static type => type.Name,
            static type => type.Id,
            StringComparer.OrdinalIgnoreCase);
        _tagsByType = [];
        foreach (var type in terrainTypes)
        {
            _tagsByType[type.Id] = type.TagIds;
        }

        foreach (var type in structureTypes)
        {
            _tagsByType[type.Id] = type.TagIds;
        }
    }

    public Guid WaterTagId { get; }

    public HashSet<Guid> TerrainTypeIds { get; }

    public HashSet<Guid> StructureTypeIds { get; }

    public HashSet<Guid> TerrainTagIds { get; }

    public HashSet<Guid> StructureTagIds { get; }

    public Dictionary<string, Guid> StructureIdsByName { get; }

    public IReadOnlyList<Guid> TagsForType(Guid typeId)
    {
        return _tagsByType.GetValueOrDefault(typeId) ?? [];
    }
}
