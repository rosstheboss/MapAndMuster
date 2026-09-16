using MapAndMuster.Domain.Campaigns;

namespace MapAndMuster.Application.Campaigns;

/// <summary>
/// Fills empty Hunt in Estalia faction special-rule assignments from catalog names.
/// Explicit stored identifiers win; stale identifiers that match nothing fall back to the documented names.
/// </summary>
internal static class HuntInEstaliaSpecialRuleBinder
{
    public static IReadOnlyList<Guid> FactionRuleIds(StoredFaction faction, IReadOnlyList<StoredSpecialRule> catalog)
    {
        ArgumentNullException.ThrowIfNull(faction);
        ArgumentNullException.ThrowIfNull(catalog);
        return Resolve(faction.SpecialRuleIds, HuntInEstaliaSpecialRules.RuleNamesForFaction(faction.Name), catalog);
    }

    public static IReadOnlyList<SubfactionSpecialRulesDetail> SubfactionRuleAssignments(
        StoredFaction faction,
        IReadOnlyList<StoredSpecialRule> catalog)
    {
        ArgumentNullException.ThrowIfNull(faction);
        ArgumentNullException.ThrowIfNull(catalog);
        var known = faction.SubfactionSpecialRules
            .ToDictionary(static item => item.Name, StringComparer.OrdinalIgnoreCase);
        IEnumerable<string> names = faction.Subfactions.Count > 0
            ? faction.Subfactions
            : known.Keys;
        return
        [
            .. names.Select(name =>
            {
                known.TryGetValue(name, out var stored);
                var ids = Resolve(
                    stored?.SpecialRuleIds ?? [],
                    HuntInEstaliaSpecialRules.RuleNamesForSubfaction(faction.Name, name),
                    catalog);
                return new SubfactionSpecialRulesDetail
                {
                    Name = stored?.Name ?? name,
                    SpecialRuleIds = ids,
                };
            })
            .Where(item => item.SpecialRuleIds.Count > 0 || known.ContainsKey(item.Name)),
        ];
    }

    private static IReadOnlyList<Guid> Resolve(
        IReadOnlyList<Guid> stored,
        IReadOnlyList<string> documentedNames,
        IReadOnlyList<StoredSpecialRule> catalog)
    {
        var knownIds = catalog.Select(static rule => rule.Id).ToHashSet();
        var fromStore = stored.Where(knownIds.Contains).Distinct().ToArray();
        if (fromStore.Length > 0)
        {
            return fromStore;
        }

        if (documentedNames.Count == 0)
        {
            return stored;
        }

        var byName = catalog
            .GroupBy(static rule => rule.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static group => group.Key, static group => group.First().Id, StringComparer.OrdinalIgnoreCase);
        return
        [
            .. documentedNames
                .Select(name => byName.TryGetValue(name, out var id) ? id : (Guid?)null)
                .OfType<Guid>(),
        ];
    }
}
