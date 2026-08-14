namespace Campaign.Domain.Maps;

/// <summary>
/// Public labels for structure types, in alphabetical order.
/// </summary>
public static class StructureCatalog
{
    /// <summary>
    /// Gets every structure type with its display label, sorted alphabetically by label.
    /// </summary>
    public static IReadOnlyList<(StructureType Type, string Label)> All { get; } =
    [
        (StructureType.CapitalCity, "Capital City"),
        (StructureType.Castle, "Castle"),
        (StructureType.City, "City"),
        (StructureType.Fortification, "Fortification"),
        (StructureType.SupplyDepot, "Supply Depot"),
        (StructureType.Town, "Town"),
    ];

    /// <summary>
    /// Returns the display label for a structure type.
    /// </summary>
    /// <param name="type">The structure type.</param>
    /// <returns>The label.</returns>
    public static string LabelFor(StructureType type)
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
    /// Parses a structure type from an enum name or display label.
    /// </summary>
    /// <param name="value">The raw value.</param>
    /// <param name="type">The parsed type.</param>
    /// <returns><see langword="true"/> when the value is known.</returns>
    public static bool TryParse(string? value, out StructureType type)
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
