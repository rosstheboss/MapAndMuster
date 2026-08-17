namespace Campaign.Domain.Maps;

/// <summary>
/// Public labels and default overlay colors for terrain types, in alphabetical order.
/// </summary>
public static class TerrainCatalog
{
    /// <summary>
    /// Gets every terrain type with its display label and default overlay color, sorted alphabetically.
    /// </summary>
    public static IReadOnlyList<(TerrainType Type, string Label, string OverlayColor)> All { get; } =
    [
        (TerrainType.Beach, "Beach", "#E8C36A"),
        (TerrainType.Cave, "Cave", "#6B4F3A"),
        (TerrainType.Desert, "Desert", "#D4A017"),
        (TerrainType.Forest, "Forest", "#2E7D32"),
        (TerrainType.Highlands, "Highlands", "#C45C26"),
        (TerrainType.Jungle, "Jungle", "#0B8F4A"),
        (TerrainType.Lake, "Lake", "#5BA3C9"),
        (TerrainType.Mountain, "Mountain", "#8A8680"),
        (TerrainType.Plains, "Plains", "#7CB342"),
        (TerrainType.Riverlands, "Riverlands", "#2E8B7A"),
        (TerrainType.Sea, "Sea", "#1E5F8A"),
        (TerrainType.Swamp, "Swamp", "#5C6B3A"),
    ];

    /// <summary>
    /// Returns the display label for a terrain type.
    /// </summary>
    /// <param name="type">The terrain type.</param>
    /// <returns>The label.</returns>
    public static string LabelFor(TerrainType type)
    {
        foreach (var entry in All)
        {
            if (entry.Type == type)
            {
                return entry.Label;
            }
        }

        return type.ToString();
    }

    /// <summary>
    /// Returns the default overlay color for a terrain type.
    /// </summary>
    /// <param name="type">The terrain type.</param>
    /// <returns>The hex color.</returns>
    public static string OverlayColorFor(TerrainType type)
    {
        foreach (var entry in All)
        {
            if (entry.Type == type)
            {
                return entry.OverlayColor;
            }
        }

        return "#888888";
    }

    /// <summary>
    /// Parses a terrain type from an enum name or display label.
    /// </summary>
    /// <param name="value">The raw value.</param>
    /// <param name="type">The parsed type.</param>
    /// <returns><see langword="true"/> when the value is known.</returns>
    public static bool TryParse(string? value, out TerrainType type)
    {
        if (Enum.TryParse(value, ignoreCase: true, out type) && Enum.IsDefined(type))
        {
            return true;
        }

        foreach (var entry in All)
        {
            if (string.Equals(entry.Label, value, StringComparison.OrdinalIgnoreCase))
            {
                type = entry.Type;
                return true;
            }
        }

        type = default;
        return false;
    }
}
