namespace Campaign.Domain.Campaigns;

/// <summary>
/// A validated item-objective catalog entry.
/// </summary>
public sealed class ItemObjectiveTypeSetup
{
    /// <summary>
    /// Initializes a validated item objective type.
    /// </summary>
    /// <param name="id">The type identifier.</param>
    /// <param name="name">The item name.</param>
    /// <param name="isHiddenUntilFound">Whether the item stays hidden until found or staff-revealed.</param>
    /// <param name="placement">How the item is placed at launch.</param>
    /// <param name="allowOnSpawn">Whether the item may occupy a spawn territory.</param>
    public ItemObjectiveTypeSetup(
        Guid id,
        string name,
        bool isHiddenUntilFound,
        ItemObjectivePlacementKind placement,
        bool allowOnSpawn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Id = id;
        Name = name;
        IsHiddenUntilFound = isHiddenUntilFound;
        Placement = placement;
        AllowOnSpawn = allowOnSpawn;
    }

    /// <summary>Gets the type identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the item name.</summary>
    public string Name { get; }

    /// <summary>Gets whether the item stays hidden until found or staff-revealed.</summary>
    public bool IsHiddenUntilFound { get; }

    /// <summary>Gets how the item is placed at launch.</summary>
    public ItemObjectivePlacementKind Placement { get; }

    /// <summary>Gets whether the item may occupy a spawn territory.</summary>
    public bool AllowOnSpawn { get; }
}
