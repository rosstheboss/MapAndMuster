namespace Campaign.Domain.Play;

/// <summary>
/// One army-composition category's unit count and declared supply spend.
/// </summary>
public sealed class ArmyListSupplyCategory
{
    /// <summary>
    /// Initializes a category row.
    /// </summary>
    public ArmyListSupplyCategory(string name, int unitCount, int supplyPoints, bool costsSupply)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(unitCount);
        ArgumentOutOfRangeException.ThrowIfNegative(supplyPoints);
        Name = name.Trim();
        UnitCount = unitCount;
        SupplyPoints = supplyPoints;
        CostsSupply = costsSupply;
    }

    /// <summary>Gets the category label, such as Special or Rare.</summary>
    public string Name { get; }

    /// <summary>Gets how many top-level units were counted in this category.</summary>
    public int UnitCount { get; }

    /// <summary>Gets declared supply points for this category. The player may edit this after a parse.</summary>
    public int SupplyPoints { get; }

    /// <summary>
    /// Gets whether units in this category spend supply by default (special, rare, and similar).
    /// </summary>
    public bool CostsSupply { get; }
}
