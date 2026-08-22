namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// User-supplied private objective catalog entry for campaign setup.
/// </summary>
public sealed class PrivateObjectiveTypeInput
{
    /// <summary>Gets the client-assigned identifier, when present.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets the objective name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the optional secret description or criteria text.</summary>
    public string? Description { get; init; }

    /// <summary>Gets campaign points awarded when revealed or completed. Defaults to 0.</summary>
    public int? CampaignPoints { get; init; }

    /// <summary>Gets holder kinds this catalog entry may be assigned to. Empty means all three.</summary>
    public IReadOnlyList<string>? AllowedHolderKinds { get; init; }

    /// <summary>Gets Manual or Automatic. Defaults to Manual.</summary>
    public string? ScoringKind { get; init; }

    /// <summary>Gets the automatic criterion kind when scoring is Automatic.</summary>
    public string? AutomaticKind { get; init; }

    /// <summary>Gets how many matching facts complete an automatic objective. Defaults to 1.</summary>
    public int? RequiredCount { get; init; }

    /// <summary>Gets the structure type for structure-based automatic criteria.</summary>
    public Guid? StructureTypeId { get; init; }

    /// <summary>Gets named territories for ControlNamedTerritories.</summary>
    public IReadOnlyList<Guid>? TerritoryIds { get; init; }
}
