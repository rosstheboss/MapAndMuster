using MapAndMuster.Domain.Common;

namespace MapAndMuster.Domain.Maps;

/// <summary>
/// A faction or required subfaction that needs a spawn when placement is not random.
/// </summary>
public sealed record FactionSpawnCheck(
    Guid Id,
    string Name,
    bool RequiresSubfaction,
    IReadOnlyList<string> Subfactions,
    bool HasAlternatePlacement,
    IReadOnlySet<string> SubfactionsWithAlternatePlacement);

/// <summary>
/// A territory's spawn assignment.
/// </summary>
public readonly record struct TerritorySpawn(Guid? FactionId, string? Subfaction);

/// <summary>
/// Spawn placement rules shared by the map editor and campaign start.
/// </summary>
public static class SpawnAssignmentRules
{
    /// <summary>
    /// When random placement is off, every faction needs its own spawn or a neutral spawn to draw from.
    /// Factions placed by Magritta or the Underground Network are omitted.
    /// </summary>
    public static DomainError? RequireSpawnOrNeutral(
        bool randomSpawnLocations,
        IReadOnlyList<FactionSpawnCheck> factions,
        IReadOnlyList<TerritorySpawn> spawns)
    {
        ArgumentNullException.ThrowIfNull(factions);
        ArgumentNullException.ThrowIfNull(spawns);
        if (randomSpawnLocations || spawns.Any(static spawn => SpawnMarkers.IsGeneral(spawn.FactionId)))
        {
            return null;
        }

        var missing = new List<string>();
        foreach (var faction in factions.OrderBy(static item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (faction.HasAlternatePlacement)
            {
                continue;
            }

            if (faction.RequiresSubfaction)
            {
                foreach (var subfaction in faction.Subfactions
                    .Select(static name => name.Trim())
                    .Where(static name => name.Length > 0)
                    .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase))
                {
                    if (faction.SubfactionsWithAlternatePlacement.Contains(subfaction))
                    {
                        continue;
                    }

                    var assigned = spawns.Any(spawn =>
                        spawn.FactionId == faction.Id
                        && string.Equals(spawn.Subfaction, subfaction, StringComparison.OrdinalIgnoreCase));
                    if (!assigned)
                    {
                        missing.Add($"{faction.Name} - {subfaction}");
                    }
                }

                continue;
            }

            if (!spawns.Any(spawn => spawn.FactionId == faction.Id && !SpawnMarkers.IsGeneral(spawn.FactionId)))
            {
                missing.Add(faction.Name);
            }
        }

        if (missing.Count == 0)
        {
            return null;
        }

        var verb = missing.Count == 1 ? "has" : "have";
        return new DomainError(
            "territories.spawn.missing",
            $"No neutral spawn locations exist, and {string.Join(", ", missing)} {verb} no specific spawn location.",
            "territories");
    }
}
