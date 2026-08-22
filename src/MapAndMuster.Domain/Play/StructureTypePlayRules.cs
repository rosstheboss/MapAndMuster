using MapAndMuster.Domain.Campaigns;

namespace MapAndMuster.Domain.Play;

/// <summary>
/// Campaign catalog flags used to validate Build, Pillage, and destroy-on-pillage.
/// </summary>
public sealed class StructureTypePlayRules
{
    /// <summary>
    /// Initializes structure play rules.
    /// </summary>
    public StructureTypePlayRules(
        Guid id,
        string name,
        bool isBuildable,
        bool isPillageable,
        bool isDestructible,
        int supplyPoints = HuntInEstaliaDefaults.SupplyPoints,
        int pillageSupplyPoints = HuntInEstaliaDefaults.SupplyPoints,
        int destroySupplyPoints = HuntInEstaliaDefaults.SupplyPoints)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Id = id;
        Name = name;
        IsBuildable = isBuildable;
        IsPillageable = isPillageable;
        IsDestructible = isDestructible;
        SupplyPoints = supplyPoints;
        PillageSupplyPoints = pillageSupplyPoints;
        DestroySupplyPoints = destroySupplyPoints;
    }

    /// <summary>Gets the structure type identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the display name.</summary>
    public string Name { get; }

    /// <summary>Gets whether players may Build this structure.</summary>
    public bool IsBuildable { get; }

    /// <summary>Gets whether players may Pillage this structure.</summary>
    public bool IsPillageable { get; }

    /// <summary>Gets whether a second Pillage may destroy and remove this structure.</summary>
    public bool IsDestructible { get; }

    /// <summary>Gets ongoing map supply while this structure is operational.</summary>
    public int SupplyPoints { get; }

    /// <summary>Gets temporary supply awarded when this structure is pillaged.</summary>
    public int PillageSupplyPoints { get; }

    /// <summary>Gets temporary supply awarded when this structure is destroyed.</summary>
    public int DestroySupplyPoints { get; }
}
