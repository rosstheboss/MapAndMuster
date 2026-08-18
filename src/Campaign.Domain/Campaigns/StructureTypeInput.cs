namespace Campaign.Domain.Campaigns;

/// <summary>
/// User-supplied structure type for campaign setup.
/// </summary>
public sealed class StructureTypeInput
{
    /// <summary>Gets the client-assigned identifier, when present.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets the structure name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the built-in logo key used until a custom image is uploaded.</summary>
    public string? BuiltinSymbol { get; init; }

    /// <summary>Gets whether an existing uploaded logo should be removed.</summary>
    public bool ClearImage { get; init; }

    /// <summary>Gets whether an existing uploaded pillaged logo should be removed.</summary>
    public bool ClearPillagedImage { get; init; }

    /// <summary>Gets whether players may Build this structure. Omitted values use catalog defaults.</summary>
    public bool? IsBuildable { get; init; }

    /// <summary>Gets whether players may Pillage this structure. Omitted values use catalog defaults.</summary>
    public bool? IsPillageable { get; init; }

    /// <summary>Gets whether a second Pillage may destroy and remove this structure. Omitted values use catalog defaults.</summary>
    public bool? IsDestructible { get; init; }

    /// <summary>Gets nested missions. Missions are optional for structures.</summary>
    public IReadOnlyList<MissionInput>? Missions { get; init; }

    /// <summary>Gets campaign points awarded for currently controlling this structure. Defaults to 0.</summary>
    public int? CampaignPoints { get; init; }

    /// <summary>Gets ongoing map supply while this structure is operational. Defaults to 1.</summary>
    public int? SupplyPoints { get; init; }

    /// <summary>Gets temporary supply awarded when this structure is pillaged. Defaults to 1.</summary>
    public int? PillageSupplyPoints { get; init; }

    /// <summary>Gets temporary supply awarded when this structure is destroyed. Defaults to 1.</summary>
    public int? DestroySupplyPoints { get; init; }
}
