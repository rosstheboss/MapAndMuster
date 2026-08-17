namespace Campaign.Domain.Play;

/// <summary>
/// One spawned item objective on the campaign map or carried by a force.
/// </summary>
public sealed class CampaignItemObjective
{
    /// <summary>
    /// Initializes an item objective instance.
    /// </summary>
    public CampaignItemObjective(
        Guid id,
        Guid typeId,
        string name,
        Guid? territoryId,
        Guid? possessorForceId,
        bool isRevealed,
        Guid originalTerritoryId,
        bool wasHiddenUntilFound)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Id = id;
        TypeId = typeId;
        Name = name;
        TerritoryId = territoryId;
        PossessorForceId = possessorForceId;
        IsRevealed = isRevealed;
        OriginalTerritoryId = originalTerritoryId;
        WasHiddenUntilFound = wasHiddenUntilFound;
    }

    /// <summary>Gets the instance identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the catalog type.</summary>
    public Guid TypeId { get; }

    /// <summary>Gets the item name snapshotted at launch.</summary>
    public string Name { get; }

    /// <summary>Gets the territory when the item is on the ground.</summary>
    public Guid? TerritoryId { get; }

    /// <summary>Gets the carrying force when the item is possessed.</summary>
    public Guid? PossessorForceId { get; }

    /// <summary>Gets whether players may see this item.</summary>
    public bool IsRevealed { get; }

    /// <summary>Gets the territory where the item first appeared.</summary>
    public Guid OriginalTerritoryId { get; }

    /// <summary>Gets whether the item started hidden until found.</summary>
    public bool WasHiddenUntilFound { get; }

    /// <summary>
    /// Returns a copy with updated location, possessor, or reveal state.
    /// </summary>
    public CampaignItemObjective With(
        Guid? territoryId = null,
        Guid? possessorForceId = null,
        bool? isRevealed = null,
        bool clearTerritory = false,
        bool clearPossessor = false)
    {
        return new CampaignItemObjective(
            Id,
            TypeId,
            Name,
            clearTerritory ? null : territoryId ?? TerritoryId,
            clearPossessor ? null : possessorForceId ?? PossessorForceId,
            isRevealed ?? IsRevealed,
            OriginalTerritoryId,
            WasHiddenUntilFound);
    }
}
