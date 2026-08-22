namespace MapAndMuster.Domain.Play;

/// <summary>
/// Calculates the longest path through a player's currently owned territories.
/// Allied territories are excluded.
/// </summary>
public static class TerritoryChainRules
{
    /// <summary>
    /// Returns the number of territories in the longest simple path among <paramref name="ownedTerritoryIds"/>.
    /// </summary>
    public static int LongestOwnedChain(
        IReadOnlyCollection<Guid> ownedTerritoryIds,
        IReadOnlyList<CampaignPointAdjacency> adjacencies)
    {
        ArgumentNullException.ThrowIfNull(ownedTerritoryIds);
        ArgumentNullException.ThrowIfNull(adjacencies);
        if (ownedTerritoryIds.Count == 0)
        {
            return 0;
        }

        var owned = ownedTerritoryIds as HashSet<Guid> ?? [.. ownedTerritoryIds];
        var neighbors = new Dictionary<Guid, List<Guid>>();
        foreach (var id in owned)
        {
            neighbors[id] = [];
        }

        foreach (var edge in adjacencies)
        {
            if (!owned.Contains(edge.TerritoryAId) || !owned.Contains(edge.TerritoryBId))
            {
                continue;
            }

            neighbors[edge.TerritoryAId].Add(edge.TerritoryBId);
            neighbors[edge.TerritoryBId].Add(edge.TerritoryAId);
        }

        var best = 1;
        var visited = new HashSet<Guid>();
        foreach (var start in owned)
        {
            visited.Clear();
            best = Math.Max(best, Walk(start, neighbors, visited));
        }

        return best;
    }

    private static int Walk(Guid current, Dictionary<Guid, List<Guid>> neighbors, HashSet<Guid> visited)
    {
        visited.Add(current);
        var best = 1;
        foreach (var next in neighbors[current])
        {
            if (visited.Contains(next))
            {
                continue;
            }

            best = Math.Max(best, 1 + Walk(next, neighbors, visited));
        }

        visited.Remove(current);
        return best;
    }
}

/// <summary>
/// An undirected adjacency used for territory-chain scoring.
/// </summary>
/// <param name="TerritoryAId">One territory.</param>
/// <param name="TerritoryBId">The other territory.</param>
public readonly record struct CampaignPointAdjacency(Guid TerritoryAId, Guid TerritoryBId);
