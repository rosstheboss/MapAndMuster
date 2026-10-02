using MapAndMuster.Domain.Campaigns;
using MapAndMuster.Domain.Maps;

namespace MapAndMuster.Domain.Play;

/// <summary>
/// Assigns players to general spawn locations when random spawn is enabled.
/// </summary>
public static class RandomSpawnRules
{
    /// <summary>A player waiting for a random spawn.</summary>
    public sealed record Player(Guid UserId, Guid FactionId, FactionPreference Preference);

    /// <summary>A general spawn and the tags and types present on it.</summary>
    public sealed record Spawn(
        Guid TerritoryId,
        Guid TerrainTypeId,
        IReadOnlySet<Guid> TerrainTagIds,
        Guid? StructureTypeId,
        IReadOnlySet<Guid> StructureTagIds);

    /// <summary>
    /// Places preference-heavy factions first, one faction per spawn until spawns run out.
    /// Players whose preferences are already taken join the final random pool.
    /// </summary>
    public static IReadOnlyDictionary<Guid, Guid> Assign(
        IReadOnlyList<Player> players,
        IReadOnlyList<Spawn> spawns,
        Func<int, int> pickIndex)
    {
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(spawns);
        ArgumentNullException.ThrowIfNull(pickIndex);
        var result = new Dictionary<Guid, Guid>();
        if (players.Count == 0 || spawns.Count == 0)
        {
            return result;
        }

        var open = spawns.ToList();
        var deferred = new List<Player>();
        var preferred = players
            .Where(static player => player.Preference.Count > 0)
            .OrderByDescending(static player => player.Preference.Count)
            .ThenBy(static player => player.UserId)
            .ToList();
        foreach (var player in preferred)
        {
            var best = Best(open, player.Preference, pickIndex);
            if (best is null || Score(best, player.Preference) == 0)
            {
                deferred.Add(player);
                continue;
            }

            result[player.UserId] = best.TerritoryId;
            Consume(open, best, spawns);
        }

        var last = players
            .Where(player => player.Preference.Count == 0)
            .Concat(deferred)
            .OrderBy(static player => player.UserId)
            .ToList();
        foreach (var player in last)
        {
            if (open.Count == 0)
            {
                open = spawns.ToList();
            }

            var index = Math.Clamp(pickIndex(open.Count), 0, open.Count - 1);
            var chosen = open[index];
            result[player.UserId] = chosen.TerritoryId;
            Consume(open, chosen, spawns);
        }

        return result;
    }

    /// <summary>
    /// Nearest general spawn by adjacency. Used when a force is sent back to a random spawn.
    /// </summary>
    public static Guid? Nearest(PlayMap map, Guid fromTerritoryId, IReadOnlyList<Guid> spawnIds)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(spawnIds);
        if (spawnIds.Count == 0)
        {
            return null;
        }

        if (spawnIds.Contains(fromTerritoryId))
        {
            return fromTerritoryId;
        }

        var targets = spawnIds.ToHashSet();
        var visited = new HashSet<Guid> { fromTerritoryId };
        var queue = new Queue<Guid>();
        queue.Enqueue(fromTerritoryId);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var neighbor in map.Neighbors(current))
            {
                if (!visited.Add(neighbor))
                {
                    continue;
                }

                if (targets.Contains(neighbor))
                {
                    return neighbor;
                }

                queue.Enqueue(neighbor);
            }
        }

        return spawnIds[0];
    }

    /// <summary>General spawn territories on a play map.</summary>
    public static IReadOnlyList<Guid> GeneralSpawnIds(PlayMap map)
    {
        ArgumentNullException.ThrowIfNull(map);
        return [.. map.Territories.Where(static territory => SpawnMarkers.IsGeneral(territory.SpawnFactionId)).Select(static territory => territory.Id)];
    }

    private static void Consume(List<Spawn> open, Spawn chosen, IReadOnlyList<Spawn> all)
    {
        _ = all;
        open.Remove(chosen);
    }

    private static Spawn? Best(List<Spawn> open, FactionPreference preference, Func<int, int> pickIndex)
    {
        if (open.Count == 0)
        {
            return null;
        }

        var bestScore = open.Max(spawn => Score(spawn, preference));
        var ties = open.Where(spawn => Score(spawn, preference) == bestScore).ToList();
        return ties[Math.Clamp(pickIndex(ties.Count), 0, ties.Count - 1)];
    }

    private static int Score(Spawn spawn, FactionPreference preference)
    {
        var score = 0;
        if (preference.TerrainTypeIds.Contains(spawn.TerrainTypeId))
        {
            score++;
        }

        score += preference.TerrainTagIds.Count(spawn.TerrainTagIds.Contains);
        if (spawn.StructureTypeId is { } structure && preference.StructureTypeIds.Contains(structure))
        {
            score++;
        }

        score += preference.StructureTagIds.Count(spawn.StructureTagIds.Contains);
        return score;
    }
}
