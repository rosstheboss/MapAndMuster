namespace Campaign.Domain.Maps;

/// <summary>
/// Unvalidated adjacency between two territories.
/// </summary>
public sealed class AdjacencyInput
{
    /// <summary>Gets the client-assigned identifier, when present.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets one territory identifier.</summary>
    public Guid TerritoryAId { get; init; }

    /// <summary>Gets the other territory identifier.</summary>
    public Guid TerritoryBId { get; init; }

    /// <summary>Gets Generated or Manual.</summary>
    public string? Origin { get; init; }

    /// <summary>Gets the editor arrow marker X coordinate.</summary>
    public double MarkerX { get; init; }

    /// <summary>Gets the editor arrow marker Y coordinate.</summary>
    public double MarkerY { get; init; }
}
