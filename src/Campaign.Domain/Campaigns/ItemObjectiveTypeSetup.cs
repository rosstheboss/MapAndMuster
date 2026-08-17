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
    /// <param name="builtinSymbol">The built-in logo key used until a custom image is uploaded.</param>
    /// <param name="color">The #RRGGBB logo color used with a built-in symbol.</param>
    /// <param name="clearImage">Whether an existing uploaded logo should be removed.</param>
    /// <param name="campaignPoints">Campaign points awarded while a force currently holds this item.</param>
    /// <param name="flavorText">Optional flavor or lore text shown to the holder.</param>
    /// <param name="choices">Holder choices configured for this item.</param>
    /// <param name="specialRuleIds">Special rules assigned to this item.</param>
    public ItemObjectiveTypeSetup(
        Guid id,
        string name,
        bool isHiddenUntilFound,
        ItemObjectivePlacementKind placement,
        bool allowOnSpawn,
        string? builtinSymbol = null,
        string? color = null,
        bool clearImage = false,
        int campaignPoints = 0,
        string? flavorText = null,
        IReadOnlyList<ItemObjectiveChoiceSetup>? choices = null,
        IReadOnlyList<Guid>? specialRuleIds = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(campaignPoints);
        Id = id;
        Name = name;
        IsHiddenUntilFound = isHiddenUntilFound;
        Placement = placement;
        AllowOnSpawn = allowOnSpawn;
        BuiltinSymbol = builtinSymbol ?? nameof(ItemObjectiveSymbol.Crown);
        Color = color ?? ItemObjectiveCatalog.DefaultColor;
        ClearImage = clearImage;
        CampaignPoints = campaignPoints;
        FlavorText = flavorText;
        Choices = choices ?? [];
        SpecialRuleIds = specialRuleIds ?? [];
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

    /// <summary>Gets the built-in logo key.</summary>
    public string BuiltinSymbol { get; }

    /// <summary>Gets the logo color.</summary>
    public string Color { get; }

    /// <summary>Gets whether an existing uploaded logo should be removed.</summary>
    public bool ClearImage { get; }

    /// <summary>Gets campaign points awarded while a force currently holds this item.</summary>
    public int CampaignPoints { get; }

    /// <summary>Gets optional flavor or lore text shown to the holder.</summary>
    public string? FlavorText { get; }

    /// <summary>Gets holder choices configured for this item.</summary>
    public IReadOnlyList<ItemObjectiveChoiceSetup> Choices { get; }

    /// <summary>Gets special rules assigned to this item.</summary>
    public IReadOnlyList<Guid> SpecialRuleIds { get; }
}
