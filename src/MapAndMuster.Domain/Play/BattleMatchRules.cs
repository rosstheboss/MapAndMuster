namespace MapAndMuster.Domain.Play;

/// <summary>
/// Chooses which forces play a tabletop game when more than two opposing sides share a territory.
/// </summary>
public static class BattleMatchRules
{
    /// <summary>
    /// Groups participating forces into opposing sides using ally groups.
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<CampaignForce>> Sides(
        IReadOnlyList<CampaignForce> forces,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlyCollection<Guid> brokenAllyFactionIds)
    {
        ArgumentNullException.ThrowIfNull(forces);
        ArgumentNullException.ThrowIfNull(factionAllyGroups);
        ArgumentNullException.ThrowIfNull(brokenAllyFactionIds);
        var sides = new List<List<CampaignForce>>();
        foreach (var force in forces.OrderBy(static item => item.Id))
        {
            var matched = sides.FirstOrDefault(side =>
                side.All(member => !ActionResolution.AreEnemies(
                    member.FactionId,
                    force.FactionId,
                    factionAllyGroups,
                    brokenAllyFactionIds)));
            if (matched is null)
            {
                sides.Add([force]);
            }
            else
            {
                matched.Add(force);
            }
        }

        return sides;
    }

    /// <summary>
    /// Returns the two strongest remaining forces from different sides for the next tabletop game.
    /// When only two sides remain, every remaining fighting force plays in one game.
    /// </summary>
    public static IReadOnlyList<Guid> NextActiveForceIds(
        IReadOnlyList<CampaignForce> fighting,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlyCollection<Guid> brokenAllyFactionIds,
        Func<CampaignForce, CombatantStrengthRules.Strength> strengthOf,
        Func<int, int> pickIndex)
    {
        ArgumentNullException.ThrowIfNull(fighting);
        var sides = Sides(fighting, factionAllyGroups, brokenAllyFactionIds);
        if (sides.Count <= 1)
        {
            return [.. fighting.Select(static force => force.Id)];
        }

        if (sides.Count == 2)
        {
            return [.. fighting.Select(static force => force.Id)];
        }

        var ranked = CombatantStrengthRules.Rank(fighting, strengthOf, pickIndex);
        if (ranked.Count < 2)
        {
            return [.. ranked.Select(static force => force.Id)];
        }

        var first = ranked[0];
        var second = ranked.First(force =>
            ActionResolution.AreEnemies(first.FactionId, force.FactionId, factionAllyGroups, brokenAllyFactionIds));
        return [first.Id, second.Id];
    }
}
