namespace MapAndMuster.Domain.Play;

/// <summary>
/// Stops enemy forces that would cross while moving more than one territory.
/// </summary>
public static class MovementCrossingRules
{
    /// <summary>One force's planned steps after it leaves its origin.</summary>
    /// <param name="ForceId">The force.</param>
    /// <param name="OriginId">The territory it leaves. Entering only this origin does not lock.</param>
    /// <param name="Steps">Territories entered, in order, excluding the origin.</param>
    public sealed record Mover(Guid ForceId, Guid OriginId, IReadOnlyList<Guid> Steps);

    /// <summary>
    /// Returns the territory where each crossing force must stop. Forces that do not cross are omitted.
    /// </summary>
    public static IReadOnlyDictionary<Guid, Guid> StopTerritories(
        IReadOnlyList<Mover> movers,
        Func<Guid, Guid, bool> areEnemies)
    {
        ArgumentNullException.ThrowIfNull(movers);
        ArgumentNullException.ThrowIfNull(areEnemies);
        var active = movers.Where(static mover => mover.Steps.Count > 0).ToList();
        var position = active.ToDictionary(static mover => mover.ForceId, static mover => mover.OriginId);
        var stopped = new Dictionary<Guid, Guid>();
        var maxSteps = active.Count == 0 ? 0 : active.Max(static mover => mover.Steps.Count);
        for (var step = 0; step < maxSteps; step++)
        {
            var next = new Dictionary<Guid, Guid>();
            foreach (var mover in active)
            {
                if (stopped.ContainsKey(mover.ForceId))
                {
                    continue;
                }

                next[mover.ForceId] = step < mover.Steps.Count ? mover.Steps[step] : position[mover.ForceId];
            }

            var candidates = new List<Guid>();
            var ids = next.Keys.ToArray();
            for (var left = 0; left < ids.Length; left++)
            {
                for (var right = left + 1; right < ids.Length; right++)
                {
                    var a = ids[left];
                    var b = ids[right];
                    if (!areEnemies(a, b))
                    {
                        continue;
                    }

                    if (next[a] == next[b])
                    {
                        candidates.Add(next[a]);
                        continue;
                    }

                    var moverA = active.Single(item => item.ForceId == a);
                    var moverB = active.Single(item => item.ForceId == b);
                    if (next[a] == position[b] && position[b] != moverB.OriginId)
                    {
                        candidates.Add(position[b]);
                    }

                    if (next[b] == position[a] && position[a] != moverA.OriginId)
                    {
                        candidates.Add(position[a]);
                    }
                }
            }

            if (candidates.Count > 0)
            {
                var chosen = candidates
                    .Distinct()
                    .OrderBy(territory => ArrivalStep(active, position, territory, step))
                    .ThenBy(static territory => territory)
                    .First();
                foreach (var id in ids)
                {
                    if (candidates.Count > 0 && (next[id] == chosen || position[id] == chosen))
                    {
                        stopped[id] = chosen;
                    }
                }
            }

            foreach (var pair in next)
            {
                if (!stopped.ContainsKey(pair.Key))
                {
                    position[pair.Key] = pair.Value;
                }
            }
        }

        return stopped;
    }

    private static int ArrivalStep(
        IReadOnlyList<Mover> movers,
        Dictionary<Guid, Guid> position,
        Guid territoryId,
        int step)
    {
        foreach (var mover in movers)
        {
            if (position[mover.ForceId] != territoryId)
            {
                continue;
            }

            for (var index = 0; index < mover.Steps.Count; index++)
            {
                if (mover.Steps[index] == territoryId)
                {
                    return index;
                }
            }

            return step;
        }

        return step;
    }
}
