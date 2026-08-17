namespace Campaign.Domain.Campaigns;

/// <summary>
/// Built-in item-objective logos. Managers may recolor these or replace them with a 50×50 upload.
/// </summary>
public static class ItemObjectiveCatalog
{
    /// <summary>Default color used when setup omits a logo color.</summary>
    public const string DefaultColor = "#C45C26";

    /// <summary>
    /// Gets every built-in logo key and label, in alphabetical order.
    /// </summary>
    public static IReadOnlyList<(ItemObjectiveSymbol Symbol, string Label)> All { get; } =
    [
        (ItemObjectiveSymbol.Banner, "Banner"),
        (ItemObjectiveSymbol.Chalice, "Chalice"),
        (ItemObjectiveSymbol.Crown, "Crown"),
        (ItemObjectiveSymbol.Gem, "Gem"),
        (ItemObjectiveSymbol.Horn, "Horn"),
        (ItemObjectiveSymbol.Orb, "Orb"),
        (ItemObjectiveSymbol.Ring, "Ring"),
        (ItemObjectiveSymbol.Shield, "Shield"),
        (ItemObjectiveSymbol.Sword, "Sword"),
        (ItemObjectiveSymbol.Tome, "Tome"),
    ];

    /// <summary>
    /// Parses a built-in logo key or label.
    /// </summary>
    /// <param name="value">The raw value.</param>
    /// <param name="symbol">The parsed symbol.</param>
    /// <returns><see langword="true"/> when the value is known.</returns>
    public static bool TryParse(string? value, out ItemObjectiveSymbol symbol)
    {
        if (Enum.TryParse(value, ignoreCase: true, out symbol) && Enum.IsDefined(symbol))
        {
            return true;
        }

        foreach (var entry in All)
        {
            if (string.Equals(entry.Label, value, StringComparison.OrdinalIgnoreCase))
            {
                symbol = entry.Symbol;
                return true;
            }
        }

        symbol = default;
        return false;
    }

    /// <summary>
    /// Returns the canonical built-in logo key.
    /// </summary>
    /// <param name="value">The supplied key or label.</param>
    /// <returns>The canonical key, or <see langword="null"/>.</returns>
    public static string? CanonicalSymbol(string? value)
    {
        return TryParse(value, out var symbol) ? symbol.ToString() : null;
    }
}

/// <summary>
/// Built-in item-objective logo keys.
/// </summary>
public enum ItemObjectiveSymbol
{
    /// <summary>A crown.</summary>
    Crown = 0,

    /// <summary>A sword.</summary>
    Sword = 1,

    /// <summary>A shield.</summary>
    Shield = 2,

    /// <summary>A chalice.</summary>
    Chalice = 3,

    /// <summary>A gem.</summary>
    Gem = 4,

    /// <summary>A banner.</summary>
    Banner = 5,

    /// <summary>A ring.</summary>
    Ring = 6,

    /// <summary>An orb.</summary>
    Orb = 7,

    /// <summary>A horn.</summary>
    Horn = 8,

    /// <summary>A tome.</summary>
    Tome = 9,
}
