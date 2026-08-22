namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// A validated campaign structure type, including an optional logo and optional missions.
/// </summary>
public sealed class StructureTypeSetup
{
    /// <summary>
    /// Initializes a validated structure type.
    /// </summary>
    /// <param name="id">The structure type identifier.</param>
    /// <param name="name">The structure name.</param>
    /// <param name="builtinSymbol">The built-in logo key used until a custom image is uploaded.</param>
    /// <param name="clearImage">Whether an existing uploaded logo should be removed.</param>
    /// <param name="clearPillagedImage">Whether an existing uploaded pillaged logo should be removed.</param>
    /// <param name="isBuildable">Whether players may Build this structure.</param>
    /// <param name="isPillageable">Whether players may Pillage this structure.</param>
    /// <param name="isDestructible">Whether a second Pillage may destroy and remove this structure.</param>
    /// <param name="missions">The optional missions.</param>
    /// <param name="campaignPoints">Campaign points awarded for currently controlling this structure.</param>
    /// <param name="supplyPoints">Ongoing map supply while this structure is operational.</param>
    /// <param name="pillageSupplyPoints">Temporary supply awarded when this structure is pillaged.</param>
    /// <param name="destroySupplyPoints">Temporary supply awarded when this structure is destroyed.</param>
    public StructureTypeSetup(
        Guid id,
        string name,
        string? builtinSymbol,
        bool clearImage,
        bool clearPillagedImage,
        bool isBuildable,
        bool isPillageable,
        bool isDestructible,
        IReadOnlyList<MissionSetup> missions,
        int campaignPoints = 0,
        int supplyPoints = HuntInEstaliaDefaults.SupplyPoints,
        int pillageSupplyPoints = HuntInEstaliaDefaults.SupplyPoints,
        int destroySupplyPoints = HuntInEstaliaDefaults.SupplyPoints)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(missions);
        ArgumentOutOfRangeException.ThrowIfNegative(campaignPoints);
        ArgumentOutOfRangeException.ThrowIfNegative(supplyPoints);
        ArgumentOutOfRangeException.ThrowIfNegative(pillageSupplyPoints);
        ArgumentOutOfRangeException.ThrowIfNegative(destroySupplyPoints);
        Id = id;
        Name = name;
        BuiltinSymbol = builtinSymbol;
        ClearImage = clearImage;
        ClearPillagedImage = clearPillagedImage;
        IsBuildable = isBuildable;
        IsPillageable = isPillageable;
        IsDestructible = isDestructible;
        Missions = missions;
        CampaignPoints = campaignPoints;
        SupplyPoints = supplyPoints;
        PillageSupplyPoints = pillageSupplyPoints;
        DestroySupplyPoints = destroySupplyPoints;
    }

    /// <summary>Gets the structure type identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the structure name.</summary>
    public string Name { get; }

    /// <summary>Gets the built-in logo key, when one is used.</summary>
    public string? BuiltinSymbol { get; }

    /// <summary>Gets whether an existing uploaded logo should be removed.</summary>
    public bool ClearImage { get; }

    /// <summary>Gets whether an existing uploaded pillaged logo should be removed.</summary>
    public bool ClearPillagedImage { get; }

    /// <summary>Gets whether players may Build this structure.</summary>
    public bool IsBuildable { get; }

    /// <summary>Gets whether players may Pillage this structure.</summary>
    public bool IsPillageable { get; }

    /// <summary>Gets whether a second Pillage may destroy and remove this structure.</summary>
    public bool IsDestructible { get; }

    /// <summary>Gets the optional missions.</summary>
    public IReadOnlyList<MissionSetup> Missions { get; }

    /// <summary>Gets campaign points awarded for currently controlling this structure when it is not destroyed.</summary>
    public int CampaignPoints { get; }

    /// <summary>Gets ongoing map supply while this structure is operational.</summary>
    public int SupplyPoints { get; }

    /// <summary>Gets temporary supply awarded when this structure is pillaged.</summary>
    public int PillageSupplyPoints { get; }

    /// <summary>Gets temporary supply awarded when this structure is destroyed.</summary>
    public int DestroySupplyPoints { get; }
}
