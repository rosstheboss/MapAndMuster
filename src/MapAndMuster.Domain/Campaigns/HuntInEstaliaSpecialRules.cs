namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// Documented Hunt in Estalia faction and subfaction special-rule names.
/// Catalog rows are matched by these names when a stored faction has no assigned identifiers.
/// </summary>
public static class HuntInEstaliaSpecialRules
{
    private static readonly Dictionary<string, string[]> FactionRuleNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Beastmen Brayherds"] = ["Expert Ambushers"],
        ["Kingdom of Bretonnia"] = ["Safe in Water"],
        ["Chaos Dwarfs"] = ["Slavers"],
        ["Daemons of Chaos"] = ["Divided We Stand"],
        ["Dark Elves"] = ["Treacherous"],
        ["Dwarfen Mountain Holds"] = ["It Is Going In The Book!", "Rulers of Stone"],
        ["Empire of Man"] = ["Prepared for Battle"],
        ["Grand Cathay"] = ["The Art of War"],
        ["High Elf Realms"] = ["Determined"],
        ["Lizardmen"] = ["Conduits of Power", "Spawning Pools"],
        ["Ogre Kingdoms"] = ["For Hire", "Tough Guts"],
        ["Orc & Goblin Tribes"] = ["The Green Tide"],
        ["Renegade Crowns"] = ["Defenders of the Homeland", "The Great City of Magritta"],
        ["Skaven"] = ["The Underground Network"],
        ["Tomb Kings of Khemri"] = ["Called by the Relic", "Undead"],
        ["Vampire Counts"] = ["Fresh Corpses", "Undead"],
        ["Warriors of Chaos"] = ["Northern Raiders"],
        ["Wood Elf Realms"] = ["Navigators of the Forests", "Healed by Nature"],
    };

    private static readonly Dictionary<string, Dictionary<string, string[]>> SubfactionRuleNames = new(
        StringComparer.OrdinalIgnoreCase)
    {
        ["Daemons of Chaos"] = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Khorne"] = ["Only Blood Satisfies!"],
            ["Nurgle"] = ["Bringers of the Plague"],
            ["Slaanesh"] = ["Alluring"],
            ["Tzeentch"] = ["Magical Supply"],
        },
    };

    /// <summary>Returns documented special-rule names for a Hunt in Estalia faction.</summary>
    public static IReadOnlyList<string> RuleNamesForFaction(string factionName)
    {
        if (string.IsNullOrWhiteSpace(factionName))
        {
            return [];
        }

        return FactionRuleNames.TryGetValue(factionName.Trim(), out var names) ? names : [];
    }

    /// <summary>Returns documented special-rule names for a Hunt in Estalia subfaction.</summary>
    public static IReadOnlyList<string> RuleNamesForSubfaction(string factionName, string subfactionName)
    {
        if (string.IsNullOrWhiteSpace(factionName) || string.IsNullOrWhiteSpace(subfactionName))
        {
            return [];
        }

        if (!SubfactionRuleNames.TryGetValue(factionName.Trim(), out var bySubfaction))
        {
            return [];
        }

        return bySubfaction.TryGetValue(subfactionName.Trim(), out var names) ? names : [];
    }
}
