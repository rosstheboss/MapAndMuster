namespace Campaign.Domain.Maps;

/// <summary>
/// Suggests adjacency edges from shared polygon borders without replacing user-created edges.
/// </summary>
public static class AdjacencyGenerator
{
    /// <summary>
    /// Builds generated adjacencies for pairs that share a border, skipping any pair that already has a manual edge.
    /// Existing generated edges for a pair are replaced so markers follow the current shared border.
    /// </summary>
    /// <param name="territories">The territories.</param>
    /// <param name="existing">The current adjacencies.</param>
    /// <returns>Manual edges plus newly generated edges.</returns>
    public static IReadOnlyList<TerritoryAdjacency> Generate(
        IReadOnlyList<Territory> territories,
        IReadOnlyList<TerritoryAdjacency> existing)
    {
        ArgumentNullException.ThrowIfNull(territories);
        ArgumentNullException.ThrowIfNull(existing);

        var manual = existing.Where(static edge => edge.Origin == AdjacencyOrigin.Manual).ToArray();
        var manualPairs = new HashSet<(Guid A, Guid B)>();
        foreach (var edge in manual)
        {
            manualPairs.Add((edge.TerritoryAId, edge.TerritoryBId));
        }

        var generated = new List<TerritoryAdjacency>();
        for (var i = 0; i < territories.Count; i++)
        {
            for (var j = i + 1; j < territories.Count; j++)
            {
                var left = territories[i];
                var right = territories[j];
                var pair = left.Id.CompareTo(right.Id) <= 0
                    ? (A: left.Id, B: right.Id)
                    : (A: right.Id, B: left.Id);
                if (manualPairs.Contains(pair))
                {
                    continue;
                }

                if (!PolygonGeometry.TrySharedBorder(left.Polygon, right.Polygon, out var midpoint))
                {
                    continue;
                }

                generated.Add(new TerritoryAdjacency(Guid.NewGuid(), left.Id, right.Id, AdjacencyOrigin.Generated, midpoint));
            }
        }

        return [.. manual, .. generated];
    }
}
