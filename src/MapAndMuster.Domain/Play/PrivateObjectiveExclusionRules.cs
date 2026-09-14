using MapAndMuster.Domain.Campaigns;
using MapAndMuster.Domain.Common;

namespace MapAndMuster.Domain.Play;

/// <summary>
/// Eligibility and unique-distribution checks for private-objective exclude lists.
/// </summary>
public static class PrivateObjectiveExclusionRules
{
    /// <summary>
    /// Whether <paramref name="holderId"/> may receive <paramref name="type"/> given faction and
    /// ally-group exclusions.
    /// </summary>
    public static bool IsEligible(
        PrivateObjectiveTypePlayRules type,
        PrivateObjectiveHolderKind holderKind,
        Guid holderId,
        IReadOnlyDictionary<Guid, Guid>? factionByPlayer,
        IReadOnlyDictionary<Guid, Guid?>? allyGroupByFaction)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (!type.Allows(holderKind))
        {
            return false;
        }

        Guid? factionId = holderKind switch
        {
            PrivateObjectiveHolderKind.Faction => holderId,
            PrivateObjectiveHolderKind.Player or PrivateObjectiveHolderKind.Traitor
                => factionByPlayer is not null && factionByPlayer.TryGetValue(holderId, out var faction)
                    ? faction
                    : null,
            _ => null,
        };
        Guid? allyGroupId = holderKind == PrivateObjectiveHolderKind.AllyGroup
            ? holderId
            : factionId is { } owningFaction
                && allyGroupByFaction is not null
                && allyGroupByFaction.TryGetValue(owningFaction, out var group)
                    ? group
                    : null;
        if (factionId is { } excludedFaction && type.ExcludedFactionIds.Contains(excludedFaction))
        {
            return false;
        }

        return allyGroupId is not { } excludedGroup || !type.ExcludedAllyGroupIds.Contains(excludedGroup);
    }

    /// <summary>
    /// How many holders of <paramref name="holderKind"/> can currently receive <paramref name="type"/>.
    /// Lower counts are more restrictive.
    /// </summary>
    public static int EligibleHolderCount(
        PrivateObjectiveTypePlayRules type,
        PrivateObjectiveHolderKind holderKind,
        IReadOnlyList<Guid> holderIds,
        IReadOnlyDictionary<Guid, Guid>? factionByPlayer,
        IReadOnlyDictionary<Guid, Guid?>? allyGroupByFaction)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(holderIds);
        return holderIds.Count(id => IsEligible(type, holderKind, id, factionByPlayer, allyGroupByFaction));
    }

    /// <summary>
    /// Adds setup errors when a private objective cannot reach at least one faction, or when unique
    /// assignment of every player-or-faction objective onto factions is impossible before the
    /// catalog is larger than the faction count.
    /// </summary>
    public static void ValidateDistribution(
        IReadOnlyList<PrivateObjectiveTypeSetup> types,
        IReadOnlyList<FactionSetup> factions,
        IReadOnlyList<AllyGroupSetup> allyGroups,
        List<DomainError> errors)
    {
        ArgumentNullException.ThrowIfNull(types);
        ArgumentNullException.ThrowIfNull(factions);
        ArgumentNullException.ThrowIfNull(allyGroups);
        ArgumentNullException.ThrowIfNull(errors);
        if (types.Count == 0)
        {
            return;
        }

        var allyIdByName = allyGroups
            .GroupBy(static group => group.Name, StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.First().Id, StringComparer.Ordinal);
        var factionIds = factions.Select(static faction => faction.Id).ToArray();
        var allyGroupIds = allyGroups.Select(static group => group.Id).ToArray();
        var eligibleFactionsByType = new List<Guid[]>();
        for (var index = 0; index < types.Count; index++)
        {
            var type = types[index];
            var allowsFactionOrPlayer = type.Allows(PrivateObjectiveHolderKind.Player)
                || type.Allows(PrivateObjectiveHolderKind.Faction);
            var allowsAlly = type.Allows(PrivateObjectiveHolderKind.AllyGroup);
            var eligibleFactions = factionIds
                .Where(id => IsFactionEligible(type, id, factions, allyIdByName))
                .ToArray();
            if (allowsFactionOrPlayer && factions.Count > 0 && eligibleFactions.Length == 0)
            {
                errors.Add(new DomainError(
                    $"privateObjectiveTypes[{index}].excludedFactionIds.invalid",
                    $"Private objective {index + 1} must remain receivable by at least one faction.",
                    $"privateObjectiveTypes[{index}].excludedFactionIds"));
            }

            if (allowsAlly && allyGroups.Count > 0
                && allyGroupIds.All(id => type.ExcludedAllyGroupIds.Contains(id)))
            {
                errors.Add(new DomainError(
                    $"privateObjectiveTypes[{index}].excludedAllyGroupIds.invalid",
                    $"Private objective {index + 1} must remain receivable by at least one ally group.",
                    $"privateObjectiveTypes[{index}].excludedAllyGroupIds"));
            }

            if (allowsFactionOrPlayer)
            {
                eligibleFactionsByType.Add(eligibleFactions);
            }
        }

        if (factions.Count == 0 || eligibleFactionsByType.Count == 0)
        {
            return;
        }

        var uniqueRequired = Math.Min(eligibleFactionsByType.Count, factions.Count);
        if (uniqueRequired == 0 || MaximumMatching(eligibleFactionsByType, factionIds) >= uniqueRequired)
        {
            return;
        }

        errors.Add(new DomainError(
            "privateObjectiveTypes.distribution.invalid",
            "Private objectives must still be distributable uniquely to factions before the catalog is larger than the faction count.",
            "privateObjectiveTypes"));
    }

    private static bool IsFactionEligible(
        PrivateObjectiveTypeSetup type,
        Guid factionId,
        IReadOnlyList<FactionSetup> factions,
        Dictionary<string, Guid> allyIdByName)
    {
        if (type.ExcludedFactionIds.Contains(factionId))
        {
            return false;
        }

        var faction = factions.First(item => item.Id == factionId);
        if (faction.AllyGroupName is null || !allyIdByName.TryGetValue(faction.AllyGroupName, out var allyId))
        {
            return true;
        }

        return !type.ExcludedAllyGroupIds.Contains(allyId);
    }

    private static int MaximumMatching(List<Guid[]> eligibleFactions, Guid[] factionIds)
    {
        var factionIndex = factionIds.Select(static (id, index) => (id, index)).ToDictionary(static item => item.id, static item => item.index);
        var pairToObjective = new int[factionIds.Length];
        Array.Fill(pairToObjective, -1);
        var matches = 0;
        for (var objective = 0; objective < eligibleFactions.Count; objective++)
        {
            var seen = new bool[factionIds.Length];
            if (TryMatch(objective, eligibleFactions, factionIndex, pairToObjective, seen))
            {
                matches++;
            }
        }

        return matches;
    }

    private static bool TryMatch(
        int objective,
        List<Guid[]> eligibleFactions,
        Dictionary<Guid, int> factionIndex,
        int[] pairToObjective,
        bool[] seen)
    {
        foreach (var factionId in eligibleFactions[objective])
        {
            var index = factionIndex[factionId];
            if (seen[index])
            {
                continue;
            }

            seen[index] = true;
            if (pairToObjective[index] < 0
                || TryMatch(pairToObjective[index], eligibleFactions, factionIndex, pairToObjective, seen))
            {
                pairToObjective[index] = objective;
                return true;
            }
        }

        return false;
    }
}
