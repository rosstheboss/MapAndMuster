namespace Campaign.Domain.Campaigns;

/// <summary>
/// A validated holder choice on an item objective.
/// </summary>
public sealed class ItemObjectiveChoiceSetup
{
    /// <summary>
    /// Initializes a validated choice.
    /// </summary>
    public ItemObjectiveChoiceSetup(Guid id, string name, IReadOnlyList<ItemObjectiveChoiceResultSetup> results)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(results);
        Id = id;
        Name = name;
        Results = results;
    }

    /// <summary>Gets the choice identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the choice name.</summary>
    public string Name { get; }

    /// <summary>Gets the configured results.</summary>
    public IReadOnlyList<ItemObjectiveChoiceResultSetup> Results { get; }
}

/// <summary>
/// A validated outcome of an item-objective choice.
/// </summary>
public sealed class ItemObjectiveChoiceResultSetup
{
    /// <summary>
    /// Initializes a validated result.
    /// </summary>
    public ItemObjectiveChoiceResultSetup(
        Guid id,
        string? flavorText,
        string? newStateKey,
        bool destroyItem,
        Guid? replacementItemTypeId,
        Guid? grantedPrivateObjectiveTypeId)
    {
        Id = id;
        FlavorText = flavorText;
        NewStateKey = newStateKey;
        DestroyItem = destroyItem;
        ReplacementItemTypeId = replacementItemTypeId;
        GrantedPrivateObjectiveTypeId = grantedPrivateObjectiveTypeId;
    }

    /// <summary>Gets the result identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets replacement flavor text after the choice.</summary>
    public string? FlavorText { get; }

    /// <summary>Gets an optional state label after the choice.</summary>
    public string? NewStateKey { get; }

    /// <summary>Gets whether the item is destroyed and removed from the map.</summary>
    public bool DestroyItem { get; }

    /// <summary>Gets a replacement item-objective catalog type.</summary>
    public Guid? ReplacementItemTypeId { get; }

    /// <summary>Gets a private-objective catalog type granted to the possessing player.</summary>
    public Guid? GrantedPrivateObjectiveTypeId { get; }
}
