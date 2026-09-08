using MapAndMuster.Domain.Campaigns;

namespace MapAndMuster.Domain.Play;

/// <summary>
/// A player-scoped Backstab: the traitor (not their whole faction) is treated as an enemy by
/// the betrayed faction, or by one required/daemon subfaction when that scope applies.
/// </summary>
/// <param name="TraitorUserId">The player who resolved Backstab.</param>
/// <param name="BetrayedFactionId">The victim faction.</param>
/// <param name="BetrayedSubfaction">
/// Set when the victim faction requires a subfaction or has DividedWeStand; otherwise null so
/// every subfaction of that faction treats this traitor as an enemy.
/// </param>
/// <param name="BetrayedUserId">The co-located victim player, or null for empty allied land.</param>
public sealed record AllyBetrayal(
    Guid TraitorUserId,
    Guid BetrayedFactionId,
    string? BetrayedSubfaction,
    Guid? BetrayedUserId);

/// <summary>
/// Player-scoped alliance breaks from Backstab. Legacy <see cref="CampaignPlayState.BrokenAllyFactionIds"/>
/// and <see cref="CampaignPlayState.BrokenAllySubfactions"/> remain for saved campaigns.
/// </summary>
public static class AllyBetrayalRules
{
    /// <summary>
    /// Returns the stored subfaction key for a victim, or null when the whole faction is betrayed.
    /// Empty-land Backstab has no subfaction, so it always records a whole-faction betrayal.
    /// </summary>
    public static string? ScopeKey(Guid factionId, string? subfaction, SpecialRuleContext rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        if (string.IsNullOrWhiteSpace(subfaction))
        {
            return null;
        }

        if (rules.FactionRequiresSubfaction(factionId)
            || rules.Has(factionId, null, SpecialRuleEffectKeys.DividedWeStand)
            || rules.Has(factionId, subfaction, SpecialRuleEffectKeys.DividedWeStand))
        {
            return subfaction.Trim();
        }

        return null;
    }

    /// <summary>Returns whether two forces are hostile because one player betrayed the other.</summary>
    public static bool AreHostile(
        CampaignForce left,
        CampaignForce right,
        IReadOnlyList<AllyBetrayal> betrayals)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        ArgumentNullException.ThrowIfNull(betrayals);
        if (left.ControllerUserId == right.ControllerUserId)
        {
            return false;
        }

        foreach (var betrayal in betrayals)
        {
            if (betrayal.TraitorUserId == left.ControllerUserId && MatchesVictim(betrayal, right))
            {
                return true;
            }

            if (betrayal.TraitorUserId == right.ControllerUserId && MatchesVictim(betrayal, left))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns whether this player already broke the alliance with that faction (whole faction)
    /// or, when <paramref name="subfaction"/> is set, with that scoped subfaction.
    /// Unknown subfaction (empty land) matches only a whole-faction betrayal.
    /// </summary>
    public static bool PlayerBetrayedFaction(
        Guid traitorUserId,
        Guid factionId,
        string? subfaction,
        IReadOnlyList<AllyBetrayal> betrayals)
    {
        ArgumentNullException.ThrowIfNull(betrayals);
        foreach (var betrayal in betrayals)
        {
            if (betrayal.TraitorUserId != traitorUserId || betrayal.BetrayedFactionId != factionId)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(betrayal.BetrayedSubfaction))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(subfaction)
                && string.Equals(betrayal.BetrayedSubfaction, subfaction, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns whether this traitor/faction/subfaction relationship is already recorded.</summary>
    public static bool HasRelationship(
        IReadOnlyList<AllyBetrayal> betrayals,
        Guid traitorUserId,
        Guid betrayedFactionId,
        string? betrayedSubfaction)
    {
        ArgumentNullException.ThrowIfNull(betrayals);
        return betrayals.Any(item =>
            item.TraitorUserId == traitorUserId
            && item.BetrayedFactionId == betrayedFactionId
            && SameSubfaction(item.BetrayedSubfaction, betrayedSubfaction));
    }

    /// <summary>Distinct traitor relationships used to grant one Traitor private objective each.</summary>
    public static int DistinctRelationshipCount(IReadOnlyList<AllyBetrayal> betrayals, Guid traitorUserId)
    {
        ArgumentNullException.ThrowIfNull(betrayals);
        return betrayals
            .Where(item => item.TraitorUserId == traitorUserId)
            .Select(static item => (
                item.BetrayedFactionId,
                Subfaction: NormalizeSubfaction(item.BetrayedSubfaction)))
            .Distinct()
            .Count();
    }

    /// <summary>Returns whether two betrayal rows name the same victim player or empty-land faction.</summary>
    public static bool SameRow(AllyBetrayal left, AllyBetrayal right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.TraitorUserId == right.TraitorUserId
            && left.BetrayedFactionId == right.BetrayedFactionId
            && left.BetrayedUserId == right.BetrayedUserId
            && SameSubfaction(left.BetrayedSubfaction, right.BetrayedSubfaction);
    }

    internal static bool MatchesVictim(AllyBetrayal betrayal, CampaignForce force)
    {
        if (force.ControllerUserId == betrayal.TraitorUserId || force.FactionId != betrayal.BetrayedFactionId)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(betrayal.BetrayedSubfaction))
        {
            return true;
        }

        return string.Equals(force.Subfaction, betrayal.BetrayedSubfaction, StringComparison.OrdinalIgnoreCase);
    }

    private static bool SameSubfaction(string? left, string? right)
    {
        return string.Equals(NormalizeSubfaction(left), NormalizeSubfaction(right), StringComparison.Ordinal);
    }

    private static string NormalizeSubfaction(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }
}
