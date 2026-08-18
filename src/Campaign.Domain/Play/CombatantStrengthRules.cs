namespace Campaign.Domain.Play;

/// <summary>
/// Ranks forces for retreat collisions and multi-side battle pairing.
/// </summary>
public static class CombatantStrengthRules
{
    /// <summary>
    /// Strength used to decide who keeps a collided retreat destination or who plays first.
    /// </summary>
    /// <param name="CampaignPoints">Current campaign-point total.</param>
    /// <param name="TerritoryCount">Territories currently owned by the force's faction.</param>
    /// <param name="StructureCount">Non-destroyed structures currently controlled by the force's faction.</param>
    /// <param name="SupplyPoints">Force allowance plus remaining player temporary supply.</param>
    public sealed record Strength(int CampaignPoints, int TerritoryCount, int StructureCount, int SupplyPoints);

    /// <summary>
    /// Returns a comparison where higher strength is greater. Callers break remaining ties with randomness.
    /// </summary>
    public static int Compare(Strength left, Strength right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        var points = left.CampaignPoints.CompareTo(right.CampaignPoints);
        if (points != 0)
        {
            return points;
        }

        var territories = left.TerritoryCount.CompareTo(right.TerritoryCount);
        if (territories != 0)
        {
            return territories;
        }

        var structures = left.StructureCount.CompareTo(right.StructureCount);
        if (structures != 0)
        {
            return structures;
        }

        return left.SupplyPoints.CompareTo(right.SupplyPoints);
    }

    /// <summary>
    /// Orders items strongest-first. Equal strength is shuffled with <paramref name="pickIndex"/>.
    /// </summary>
    public static IReadOnlyList<T> Rank<T>(
        IReadOnlyList<T> items,
        Func<T, Strength> strengthOf,
        Func<int, int> pickIndex)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(strengthOf);
        ArgumentNullException.ThrowIfNull(pickIndex);
        var remaining = items.ToList();
        var ranked = new List<T>(remaining.Count);
        while (remaining.Count > 0)
        {
            var strongest = remaining[0];
            var strongestScore = strengthOf(strongest);
            var ties = new List<int> { 0 };
            for (var index = 1; index < remaining.Count; index++)
            {
                var compared = Compare(strengthOf(remaining[index]), strongestScore);
                if (compared > 0)
                {
                    strongest = remaining[index];
                    strongestScore = strengthOf(strongest);
                    ties = [index];
                }
                else if (compared == 0)
                {
                    ties.Add(index);
                }
            }

            var chosenIndex = ties[Math.Clamp(pickIndex(ties.Count), 0, ties.Count - 1)];
            ranked.Add(remaining[chosenIndex]);
            remaining.RemoveAt(chosenIndex);
        }

        return ranked;
    }

    /// <summary>
    /// Counts owned territories and non-destroyed structures for a faction.
    /// </summary>
    public static (int Territories, int Structures) Holdings(PlayMap map, Guid factionId)
    {
        ArgumentNullException.ThrowIfNull(map);
        var territories = 0;
        var structures = 0;
        foreach (var territory in map.Territories)
        {
            if (territory.OwnerFactionId != factionId)
            {
                continue;
            }

            territories++;
            if (territory.StructureTypeId is not null && territory.StructureCondition != StructureCondition.Destroyed)
            {
                structures++;
            }
        }

        return (territories, structures);
    }
}
