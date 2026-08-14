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

    /// <summary>Gets nested missions. Missions are optional for structures.</summary>
    public IReadOnlyList<MissionInput>? Missions { get; init; }
}
