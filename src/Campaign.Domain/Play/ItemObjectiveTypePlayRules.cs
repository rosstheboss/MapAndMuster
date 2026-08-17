using Campaign.Domain.Campaigns;

namespace Campaign.Domain.Play;

/// <summary>
/// Catalog facts used to seed and resolve item objectives.
/// </summary>
public sealed class ItemObjectiveTypePlayRules
{
    /// <summary>
    /// Initializes item-objective play rules.
    /// </summary>
    public ItemObjectiveTypePlayRules(
        Guid id,
        string name,
        bool isHiddenUntilFound,
        ItemObjectivePlacementKind placement,
        bool allowOnSpawn,
        string? flavorText = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Id = id;
        Name = name;
        IsHiddenUntilFound = isHiddenUntilFound;
        Placement = placement;
        AllowOnSpawn = allowOnSpawn;
        FlavorText = flavorText;
    }

    /// <summary>Gets the catalog type identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the item name.</summary>
    public string Name { get; }

    /// <summary>Gets whether the item starts hidden.</summary>
    public bool IsHiddenUntilFound { get; }

    /// <summary>Gets how the item is placed at launch.</summary>
    public ItemObjectivePlacementKind Placement { get; }

    /// <summary>Gets whether the item may occupy a spawn territory.</summary>
    public bool AllowOnSpawn { get; }

    /// <summary>Gets optional flavor text snapshotted onto spawned instances.</summary>
    public string? FlavorText { get; }
}

/// <summary>
/// A manager-assigned launch placement for a Placed item objective.
/// </summary>
public sealed class ItemObjectiveMapPlacement
{
    /// <summary>
    /// Initializes a map placement.
    /// </summary>
    public ItemObjectiveMapPlacement(Guid typeId, Guid territoryId)
    {
        TypeId = typeId;
        TerritoryId = territoryId;
    }

    /// <summary>Gets the catalog type.</summary>
    public Guid TypeId { get; }

    /// <summary>Gets the assigned territory.</summary>
    public Guid TerritoryId { get; }
}
