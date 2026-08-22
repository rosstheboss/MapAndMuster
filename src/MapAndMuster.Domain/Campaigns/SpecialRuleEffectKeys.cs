namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// Stable keys for special-rule policies that the engine can enforce or calculate.
/// User-created rules omit a key and stay display-only.
/// </summary>
public static class SpecialRuleEffectKeys
{
    /// <summary>Beastmen Ambushers gain +1 to Ambushing rolls. Display-only tabletop reminder.</summary>
    public const string ExpertAmbushers = "ExpertAmbushers";

    /// <summary>Two-territory Move with interception, no claim on the first hop, and restricted rejoins.</summary>
    public const string Crusaders = "Crusaders";

    /// <summary>No dangerous-terrain rolls on water features for named Bretonnian troops. Battle reminder.</summary>
    public const string SafeInWater = "SafeInWater";

    /// <summary>Captured unpillaged towns and cities grant an extra supply point each.</summary>
    public const string Slavers = "Slavers";

    /// <summary>Daemon gods are required subfactions treated as allied factions that can backstab.</summary>
    public const string DividedWeStand = "DividedWeStand";

    /// <summary>Pillage may destroy immediately and may target allied structures.</summary>
    public const string OnlyBloodSatisfies = "OnlyBloodSatisfies";

    /// <summary>Never Diseased or Well Rested; a win can Disease the opponent.</summary>
    public const string BringersOfThePlague = "BringersOfThePlague";

    /// <summary>Command-phase seduction. Display-only tabletop reminder.</summary>
    public const string Alluring = "Alluring";

    /// <summary>Unused army-list supply becomes one casting or dispelling reroll per leftover point this battle, declared on the result.</summary>
    public const string MagicalSupply = "MagicalSupply";

    /// <summary>Once-per-game Fly/Ethereal. Display-only tabletop reminder.</summary>
    public const string Treacherous = "Treacherous";

    /// <summary>Once-per-battle Hatred. Display-only tabletop reminder.</summary>
    public const string ItIsGoingInTheBook = "ItIsGoingInTheBook";

    /// <summary>Mountain/cave cover and cliff flee. Display-only tabletop reminder.</summary>
    public const string RulersOfStone = "RulersOfStone";

    /// <summary>Spend one supply point for Extra Black Powder, declared on the battle result.</summary>
    public const string PreparedForBattle = "PreparedForBattle";

    /// <summary>Retreat may enter any territory and may capture it.</summary>
    public const string ArtOfWar = "ArtOfWar";

    /// <summary>Choose who goes first on a Beach or next to a spawn. Display-only tabletop reminder.</summary>
    public const string Determined = "Determined";

    /// <summary>Hidden-relic adjacency notice and move-to-adjacent-to-relic after it is found.</summary>
    public const string ConduitsOfPower = "ConduitsOfPower";

    /// <summary>Owned water features count as supply depots and fortifications, with extra built-structure supply.</summary>
    public const string SpawningPools = "SpawningPools";

    /// <summary>Ogre non-character units may be mercenaries in other armies.</summary>
    public const string ForHire = "ForHire";

    /// <summary>Never Diseased; one random unit gains Frenzy. Disease immunity is enforced.</summary>
    public const string ToughGuts = "ToughGuts";

    /// <summary>Cannot build supply depots; empty controlled land counts as a depot.</summary>
    public const string GreenTide = "GreenTide";

    /// <summary>Neutral towns and cities count as supply depots regardless of location.</summary>
    public const string DefendersOfTheHomeland = "DefendersOfTheHomeland";

    /// <summary>Spawn at the Capital City, which grants city supply.</summary>
    public const string GreatCityOfMagritta = "GreatCityOfMagritta";

    /// <summary>No spawn; randomize into a town or city and skip spawn battles.</summary>
    public const string UndergroundNetwork = "UndergroundNetwork";

    /// <summary>Must Move toward a revealed relic until it is captured or battle is forced.</summary>
    public const string CalledByTheRelic = "CalledByTheRelic";

    /// <summary>+2 to one casting or dispelling roll while holding a relic. Display-only tabletop reminder.</summary>
    public const string RelicOfAPastAge = "RelicOfAPastAge";

    /// <summary>Never Shaken, Diseased, Well Rested, or Confident.</summary>
    public const string Undead = "Undead";

    /// <summary>+D3 Arise wounds in towns, castles, or cities. Battle reminder when those structures apply.</summary>
    public const string FreshCorpses = "FreshCorpses";

    /// <summary>Pillage awards two temporary supply points rather than one.</summary>
    public const string NorthernRaiders = "NorthernRaiders";

    /// <summary>Non-Tree Spirit units ignore burning-woods dangerous terrain. Display-only.</summary>
    public const string NavigatorsOfTheForests = "NavigatorsOfTheForests";

    /// <summary>Tree Spirits may regain wounds in woods. Display-only tabletop reminder.</summary>
    public const string HealedByNature = "HealedByNature";

    /// <summary>Every key the engine recognizes.</summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ExpertAmbushers,
        Crusaders,
        SafeInWater,
        Slavers,
        DividedWeStand,
        OnlyBloodSatisfies,
        BringersOfThePlague,
        Alluring,
        MagicalSupply,
        Treacherous,
        ItIsGoingInTheBook,
        RulersOfStone,
        PreparedForBattle,
        ArtOfWar,
        Determined,
        ConduitsOfPower,
        SpawningPools,
        ForHire,
        ToughGuts,
        GreenTide,
        DefendersOfTheHomeland,
        GreatCityOfMagritta,
        UndergroundNetwork,
        CalledByTheRelic,
        RelicOfAPastAge,
        Undead,
        FreshCorpses,
        NorthernRaiders,
        NavigatorsOfTheForests,
        HealedByNature,
    };

    /// <summary>Returns whether <paramref name="key"/> is a known mechanical effect.</summary>
    public static bool IsKnown(string? key)
    {
        return !string.IsNullOrWhiteSpace(key) && All.Contains(key.Trim());
    }
}
