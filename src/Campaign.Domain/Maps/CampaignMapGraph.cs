namespace Campaign.Domain.Maps;

/// <summary>
/// Validated overlay territories and explicit adjacency edges for one campaign map.
/// Geometry may suggest adjacency; it never silently establishes it.
/// </summary>
public sealed class CampaignMapGraph
{
    /// <summary>
    /// Initializes a validated map graph.
    /// </summary>
    /// <param name="territories">The territories.</param>
    /// <param name="adjacencies">The explicit adjacencies.</param>
    public CampaignMapGraph(IReadOnlyList<Territory> territories, IReadOnlyList<TerritoryAdjacency> adjacencies)
    {
        ArgumentNullException.ThrowIfNull(territories);
        ArgumentNullException.ThrowIfNull(adjacencies);
        Territories = territories;
        Adjacencies = adjacencies;
    }

    /// <summary>Gets the territories.</summary>
    public IReadOnlyList<Territory> Territories { get; }

    /// <summary>Gets the explicit adjacencies.</summary>
    public IReadOnlyList<TerritoryAdjacency> Adjacencies { get; }

    /// <summary>
    /// Returns a graph whose generated adjacencies are rebuilt from shared borders, keeping every manual edge.
    /// </summary>
    /// <returns>The graph with refreshed generated connections.</returns>
    public CampaignMapGraph WithGeneratedAdjacencies()
    {
        return new CampaignMapGraph(Territories, AdjacencyGenerator.Generate(Territories, Adjacencies));
    }
}
