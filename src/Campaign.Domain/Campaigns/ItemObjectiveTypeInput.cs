namespace Campaign.Domain.Campaigns;

/// <summary>
/// User-supplied hidden or public item objective for campaign setup.
/// </summary>
public sealed class ItemObjectiveTypeInput
{
    /// <summary>Gets the client-assigned identifier, when present.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets the item name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets whether the item stays hidden until found or staff-revealed. Defaults to true.</summary>
    public bool? IsHiddenUntilFound { get; init; }

    /// <summary>Gets Random or Placed. Defaults to Random.</summary>
    public string? Placement { get; init; }

    /// <summary>Gets whether the item may spawn on a faction spawn territory. Defaults to false.</summary>
    public bool? AllowOnSpawn { get; init; }

    /// <summary>Gets the built-in logo key. Defaults to Crown.</summary>
    public string? BuiltinSymbol { get; init; }

    /// <summary>Gets the logo color as #RRGGBB. Defaults to the catalog color.</summary>
    public string? Color { get; init; }

    /// <summary>Gets whether an existing uploaded logo should be removed.</summary>
    public bool ClearImage { get; init; }

    /// <summary>Gets campaign points awarded while a force currently holds this item. Defaults to 0.</summary>
    public int? CampaignPoints { get; init; }

    /// <summary>Gets optional flavor or lore text shown to the holder.</summary>
    public string? FlavorText { get; init; }

    /// <summary>Gets holder choices configured for this item.</summary>
    public IReadOnlyList<ItemObjectiveChoiceInput>? Choices { get; init; }

    /// <summary>Gets special-rule identifiers assigned to this item.</summary>
    public IReadOnlyList<Guid>? SpecialRuleIds { get; init; }
}
