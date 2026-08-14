using Campaign.Domain.Maps;

namespace Campaign.Application.Maps;

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
}
