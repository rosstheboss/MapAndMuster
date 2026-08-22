namespace MapAndMuster.Domain.Maps;

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

    /// <summary>
    /// Default play flags for a built-in structure type.
    /// Town, Capital City, City, and Castle are not buildable. Capital City is not pillageable.
    /// Capital City, City, and Castle are not destructible.
    /// </summary>
    /// <param name="type">The built-in structure type.</param>
    /// <returns>Buildable, pillageable, and destructible flags.</returns>
    public static (bool IsBuildable, bool IsPillageable, bool IsDestructible) DefaultFlags(StructureType type)
    {
        return type switch
        {
            StructureType.CapitalCity => (false, false, false),
            StructureType.Castle => (false, true, false),
            StructureType.City => (false, true, false),
            StructureType.Fortification => (true, true, true),
            StructureType.SupplyDepot => (true, true, true),
            StructureType.Town => (false, true, true),
            _ => (true, true, true),
        };
    }

    /// <summary>
    /// Default play flags for a structure name or built-in logo key. Unknown custom structures
    /// default to buildable, pillageable, and destructible.
    /// </summary>
    /// <param name="name">The structure name.</param>
    /// <param name="builtinSymbol">The built-in logo key.</param>
    /// <returns>Buildable, pillageable, and destructible flags.</returns>
    public static (bool IsBuildable, bool IsPillageable, bool IsDestructible) DefaultFlags(
        string? name,
        string? builtinSymbol)
    {
        if (TryParse(builtinSymbol, out var type) || TryParse(name, out type))
        {
            return DefaultFlags(type);
        }

        return (true, true, true);
    }
}
