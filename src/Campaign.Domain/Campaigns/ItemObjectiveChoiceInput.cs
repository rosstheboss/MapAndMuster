namespace Campaign.Domain.Campaigns;

/// <summary>
/// User-supplied holder choice on an item objective.
/// </summary>
public sealed class ItemObjectiveChoiceInput
{
    /// <summary>Gets the client-assigned identifier, when present.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets the choice name, such as Open.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the results. One result is used as-is; several pick one at random.</summary>
    public IReadOnlyList<ItemObjectiveChoiceResultInput>? Results { get; init; }
}

/// <summary>
/// User-supplied outcome of an item-objective choice.
/// </summary>
public sealed class ItemObjectiveChoiceResultInput
{
    /// <summary>Gets the client-assigned identifier, when present.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets replacement flavor text after the choice.</summary>
    public string? FlavorText { get; init; }

    /// <summary>Gets an optional state label after the choice.</summary>
    public string? NewStateKey { get; init; }

    /// <summary>Gets whether the item is destroyed and removed from the map.</summary>
    public bool DestroyItem { get; init; }

    /// <summary>Gets a replacement item-objective catalog type, when the original is destroyed or transformed.</summary>
    public Guid? ReplacementItemTypeId { get; init; }

    /// <summary>Gets a private-objective catalog type granted to the possessing player.</summary>
    public Guid? GrantedPrivateObjectiveTypeId { get; init; }
}
