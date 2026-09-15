using MapAndMuster.Domain.Campaigns;

namespace MapAndMuster.Domain.Play;

/// <summary>
/// Snapshot of a force's spawn and structure access through its territory chain.
/// </summary>
public readonly struct TerritoryAccessFacts
{
    /// <summary>No spawn or structure access.</summary>
    public static TerritoryAccessFacts None { get; } = new(false);

    /// <summary>
    /// Initializes access facts for one force.
    /// </summary>
    public TerritoryAccessFacts(
        bool hasFriendlySpawnAccess,
        IReadOnlyList<Guid>? structureTypeIds = null,
        IReadOnlyList<Guid>? structureTagIds = null)
    {
        HasFriendlySpawnAccess = hasFriendlySpawnAccess;
        StructureTypeIds = structureTypeIds ?? [];
        StructureTagIds = structureTagIds ?? [];
    }

    /// <summary>
    /// Gets whether the force can reach its spawn or an allied faction's spawn along its chain.
    /// </summary>
    public bool HasFriendlySpawnAccess { get; }

    /// <summary>Gets structure types reachable along the chain, including special-rule virtual structures.</summary>
    public IReadOnlyList<Guid> StructureTypeIds { get; }

    /// <summary>Gets structure tags reachable along the chain, including special-rule virtual structures.</summary>
    public IReadOnlyList<Guid> StructureTagIds { get; }

    /// <summary>
    /// Returns whether the chain currently includes a structure matching <paramref name="location"/>.
    /// Any means any non-destroyed or virtual structure. Terrain filters never match.
    /// </summary>
    public bool HasStructureAccess(ConditionLocation location)
    {
        ArgumentNullException.ThrowIfNull(location);
        return location.Kind switch
        {
            ConditionLocationKind.Any => StructureTypeIds.Count > 0,
            ConditionLocationKind.StructureType => location.TypeId is { } type && StructureTypeIds.Contains(type),
            ConditionLocationKind.StructureTag => location.TagId is { } tag && StructureTagIds.Contains(tag),
            _ => false,
        };
    }

    /// <summary>
    /// Returns whether spawn access was gained compared with <paramref name="previous"/>.
    /// </summary>
    public bool GainedSpawnAccess(in TerritoryAccessFacts previous)
    {
        return HasFriendlySpawnAccess && !previous.HasFriendlySpawnAccess;
    }

    /// <summary>
    /// Returns whether structure access matching <paramref name="location"/> was gained compared with
    /// <paramref name="previous"/>.
    /// </summary>
    public bool GainedStructureAccess(in TerritoryAccessFacts previous, ConditionLocation location)
    {
        return HasStructureAccess(location) && !previous.HasStructureAccess(location);
    }
}
