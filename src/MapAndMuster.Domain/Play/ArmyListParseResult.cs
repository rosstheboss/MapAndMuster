namespace MapAndMuster.Domain.Play;

/// <summary>
/// Outcome of attempting to read supply amounts from pasted army-list text.
/// </summary>
public sealed class ArmyListParseResult
{
    private ArmyListParseResult(bool parsed, int armyPoints, IReadOnlyList<ArmyListSupplyCategory> categories)
    {
        Parsed = parsed;
        ArmyPoints = armyPoints;
        Categories = categories;
    }

    /// <summary>Gets whether the text matched the selected builder and yielded usable amounts.</summary>
    public bool Parsed { get; }

    /// <summary>Gets the army size in points read from the list header.</summary>
    public int ArmyPoints { get; }

    /// <summary>Gets per-category unit counts and default supply amounts.</summary>
    public IReadOnlyList<ArmyListSupplyCategory> Categories { get; }

    /// <summary>Gets supply-costing units summed from special, rare, and similar categories.</summary>
    public int SupplyCostingUnitCount =>
        Categories.Where(static category => category.CostsSupply).Sum(static category => category.SupplyPoints);

    /// <summary>A failed parse. The player must enter supply amounts by hand.</summary>
    public static ArmyListParseResult Failed { get; } = new(false, 0, []);

    /// <summary>
    /// Creates a successful parse.
    /// </summary>
    public static ArmyListParseResult Success(int armyPoints, IReadOnlyList<ArmyListSupplyCategory> categories)
    {
        ArgumentNullException.ThrowIfNull(categories);
        ArgumentOutOfRangeException.ThrowIfNegative(armyPoints);
        return new ArmyListParseResult(true, armyPoints, categories);
    }
}
