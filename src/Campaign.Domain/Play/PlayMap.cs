namespace Campaign.Domain.Play;

/// <summary>
/// Territory facts needed to validate and resolve campaign actions.
/// </summary>
public sealed class PlayTerritory
{
    /// <summary>
    /// Initializes a play territory snapshot.
    /// </summary>
    public PlayTerritory(
        Guid id,
        int displayNumber,
        Guid? ownerFactionId,
        Guid? spawnFactionId,
        Guid? structureTypeId,
        string? structureName,
        StructureCondition structureCondition,
        bool isPillageable = true,
        bool isDestructible = true,
        bool isWaterFeature = false,
        Guid? terrainTypeId = null)
    {
        Id = id;
        DisplayNumber = displayNumber;
        OwnerFactionId = ownerFactionId;
        SpawnFactionId = spawnFactionId;
        StructureTypeId = structureTypeId;
        StructureName = structureName;
        StructureCondition = structureCondition;
        IsPillageable = isPillageable;
        IsDestructible = isDestructible;
        IsWaterFeature = isWaterFeature;
        TerrainTypeId = terrainTypeId;
    }

    /// <summary>Gets the territory identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the display number used for deterministic fallbacks.</summary>
    public int DisplayNumber { get; }

    /// <summary>Gets the controlling faction, or null when neutral.</summary>
    public Guid? OwnerFactionId { get; }

    /// <summary>Gets the spawn faction, if this territory is a spawn.</summary>
    public Guid? SpawnFactionId { get; }

    /// <summary>Gets the structure type, if any.</summary>
    public Guid? StructureTypeId { get; }

    /// <summary>Gets the structure display name, if any.</summary>
    public string? StructureName { get; }

    /// <summary>Gets the structure condition.</summary>
    public StructureCondition StructureCondition { get; }

    /// <summary>Gets whether this territory is a spawn location.</summary>
    public bool IsSpawn => SpawnFactionId.HasValue;

    /// <summary>Gets whether the occupying structure may be pillaged.</summary>
    public bool IsPillageable { get; }

    /// <summary>Gets whether a second Pillage may destroy and remove the occupying structure.</summary>
    public bool IsDestructible { get; }

    /// <summary>Gets whether this territory's terrain is a water feature.</summary>
    public bool IsWaterFeature { get; }

    /// <summary>Gets the terrain type, when known.</summary>
    public Guid? TerrainTypeId { get; }

    /// <summary>
    /// Returns a copy with updated ownership and structure fields.
    /// </summary>
    public PlayTerritory With(
        Guid? ownerFactionId = null,
        Guid? structureTypeId = null,
        string? structureName = null,
        StructureCondition? structureCondition = null,
        bool clearStructure = false,
        bool? isPillageable = null,
        bool? isDestructible = null)
    {
        return new PlayTerritory(
            Id,
            DisplayNumber,
            ownerFactionId ?? OwnerFactionId,
            SpawnFactionId,
            clearStructure ? null : structureTypeId ?? StructureTypeId,
            clearStructure ? null : structureName ?? StructureName,
            clearStructure ? StructureCondition.Operational : structureCondition ?? StructureCondition,
            !clearStructure && (isPillageable ?? IsPillageable),
            !clearStructure && (isDestructible ?? IsDestructible),
            IsWaterFeature,
            TerrainTypeId);
    }
}

/// <summary>
/// Map graph facts used during play without overlay geometry.
/// </summary>
public sealed class PlayMap
{
    private readonly Dictionary<Guid, PlayTerritory> _territories;
    private readonly Dictionary<Guid, HashSet<Guid>> _adjacent;

    /// <summary>
    /// Initializes a play map.
    /// </summary>
    /// <param name="territories">The territories.</param>
    /// <param name="adjacencies">Undirected adjacency pairs.</param>
    /// <param name="structureTypes">Catalog flags used to validate Build and structure effects.</param>
    public PlayMap(
        IReadOnlyList<PlayTerritory> territories,
        IReadOnlyList<(Guid A, Guid B)> adjacencies,
        IReadOnlyList<StructureTypePlayRules>? structureTypes = null)
    {
        ArgumentNullException.ThrowIfNull(territories);
        ArgumentNullException.ThrowIfNull(adjacencies);
        Territories = territories;
        StructureTypes = structureTypes ?? [];
        _territories = territories.ToDictionary(static territory => territory.Id);
        _adjacent = [];
        foreach (var (left, right) in adjacencies)
        {
            AddEdge(left, right);
            AddEdge(right, left);
        }
    }

    /// <summary>Gets the territories.</summary>
    public IReadOnlyList<PlayTerritory> Territories { get; }

    /// <summary>Gets catalog flags for structure types in this campaign.</summary>
    public IReadOnlyList<StructureTypePlayRules> StructureTypes { get; }

    /// <summary>
    /// Returns a territory by identifier.
    /// </summary>
    public PlayTerritory? Territory(Guid id)
    {
        return _territories.GetValueOrDefault(id);
    }

    /// <summary>
    /// Whether two territories share an adjacency edge.
    /// </summary>
    public bool AreAdjacent(Guid originId, Guid destinationId)
    {
        return _adjacent.TryGetValue(originId, out var neighbors) && neighbors.Contains(destinationId);
    }

    /// <summary>
    /// Adjacent territory identifiers, sorted by display number for deterministic fallbacks.
    /// </summary>
    public IReadOnlyList<Guid> Neighbors(Guid territoryId)
    {
        if (!_adjacent.TryGetValue(territoryId, out var neighbors))
        {
            return [];
        }

        return
        [
            .. neighbors
                .Select(id => _territories.GetValueOrDefault(id))
                .OfType<PlayTerritory>()
                .OrderBy(static territory => territory.DisplayNumber)
                .Select(static territory => territory.Id),
        ];
    }

    /// <summary>
    /// Spawn territory for a faction, if the map has one.
    /// </summary>
    public PlayTerritory? SpawnFor(Guid factionId)
    {
        return Territories.FirstOrDefault(territory => territory.SpawnFactionId == factionId);
    }

    /// <summary>
    /// Returns a copy with replaced territories.
    /// </summary>
    public PlayMap WithTerritories(IReadOnlyList<PlayTerritory> territories)
    {
        var edges = _adjacent
            .SelectMany(pair => pair.Value.Where(other => other.CompareTo(pair.Key) > 0).Select(other => (pair.Key, other)))
            .ToArray();
        return new PlayMap(territories, edges, StructureTypes);
    }

    /// <summary>
    /// Catalog rules for a structure type, when present.
    /// </summary>
    public StructureTypePlayRules? StructureRules(Guid structureTypeId)
    {
        return StructureTypes.FirstOrDefault(item => item.Id == structureTypeId);
    }

    /// <summary>Gets whether any catalog structure may be built.</summary>
    public bool HasBuildableStructure => StructureTypes.Any(static item => item.IsBuildable);

    /// <summary>
    /// Replaces one territory.
    /// </summary>
    public PlayMap Replace(PlayTerritory territory)
    {
        ArgumentNullException.ThrowIfNull(territory);
        var next = Territories.Select(item => item.Id == territory.Id ? territory : item).ToArray();
        return WithTerritories(next);
    }

    private void AddEdge(Guid from, Guid to)
    {
        if (!_adjacent.TryGetValue(from, out var set))
        {
            set = [];
            _adjacent[from] = set;
        }

        set.Add(to);
    }
}
