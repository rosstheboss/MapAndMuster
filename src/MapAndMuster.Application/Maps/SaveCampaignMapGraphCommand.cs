using MapAndMuster.Domain.Maps;

namespace MapAndMuster.Application.Maps;

/// <summary>
/// Command to replace the overlay territory graph for a campaign map.
/// </summary>
public sealed class SaveCampaignMapGraphCommand
{
    /// <summary>Gets the authenticated user.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets the campaign identifier.</summary>
    public required Guid CampaignId { get; init; }

    /// <summary>Gets the last observed campaign revision.</summary>
    public required int ExpectedRevision { get; init; }

    /// <summary>Gets the territories.</summary>
    public required IReadOnlyList<TerritoryInput> Territories { get; init; }

    /// <summary>Gets the adjacencies.</summary>
    public required IReadOnlyList<AdjacencyInput> Adjacencies { get; init; }

    /// <summary>Gets manager-assigned item objective placements.</summary>
    public IReadOnlyList<ItemObjectivePlacementInput>? ItemObjectivePlacements { get; init; }
}

/// <summary>
/// A manager-assigned launch location for a Placed item objective.
/// </summary>
public sealed class ItemObjectivePlacementInput
{
    /// <summary>Gets the item objective type.</summary>
    public required Guid TypeId { get; init; }

    /// <summary>Gets the territory.</summary>
    public required Guid TerritoryId { get; init; }
}
