using MapAndMuster.Application.Campaigns;

namespace MapAndMuster.Backend.UnitTests.Campaigns;

public sealed class TestCampaignRosterTests
{
    [Fact]
    public void CoversEveryOldWorldFactionAndNamedSubfaction()
    {
        var factions = OldWorldFactions();
        var slots = TestCampaignRoster.Slots(factions);

        Assert.Equal(43, slots.Count);
        Assert.Contains(slots, slot => slot.FactionName == "Dark Elves" && slot.Subfaction is null);
        Assert.Contains(slots, slot => slot.FactionName == "Beastmen Brayherds" && slot.Subfaction is null);
        Assert.Contains(slots, slot => slot.FactionName == "Beastmen Brayherds" && slot.Subfaction == "Wild Herd");
        Assert.Contains(slots, slot => slot.FactionName == "Daemons of Chaos" && slot.Subfaction == "Khorne");
        Assert.DoesNotContain(slots, slot => slot.FactionName == "Daemons of Chaos" && slot.Subfaction is null);
        Assert.True(slots.Count <= MapAndMuster.Application.Identity.TestAccountCatalog.Count);
    }

    [Fact]
    public void OrdersParentThenSubfactionsAlphabeticallyByFaction()
    {
        var slots = TestCampaignRoster.Slots(
        [
            Faction("Empire of Man", false, "Knightly Orders", "City-state of Nuln"),
            Faction("Dark Elves", false),
            Faction("Daemons of Chaos", true, "Tzeentch", "Khorne"),
        ]);

        Assert.Equal(
            [
                ("Daemons of Chaos", "Khorne"),
                ("Daemons of Chaos", "Tzeentch"),
                ("Dark Elves", null),
                ("Empire of Man", null),
                ("Empire of Man", "City-state of Nuln"),
                ("Empire of Man", "Knightly Orders"),
            ],
            slots.Select(slot => (slot.FactionName, slot.Subfaction)).ToArray());
    }

    private static IReadOnlyList<StoredFaction> OldWorldFactions()
    {
        return
        [
            Faction("Beastmen Brayherds", false, "Wild Herd", "Minotaur Blood Herd"),
            Faction("Dark Elves", false),
            Faction("Chaos Dwarfs", false),
            Faction("Daemons of Chaos", true, "Khorne", "Nurgle", "Slaanesh", "Tzeentch"),
            Faction("Dwarfen Mountain Holds", false, "Slayer Host", "Expeditionary Force", "Royal Clan"),
            Faction("Grand Cathay", false, "Warriors of Wind & Field", "Jade Fleet"),
            Faction("Empire of Man", false, "Knightly Orders", "City-state of Nuln"),
            Faction("High Elf Realms", false, "Sea Guard Garrison", "Chracian Warhost"),
            Faction("Lizardmen", false),
            Faction("Kingdom of Bretonnia", false, "Errantry Crusade", "Bretonnian Exiles"),
            Faction("Ogre Kingdoms", false),
            Faction("Orc & Goblin Tribes", false, "Troll Horde", "Nomadic Waaagh!"),
            Faction("Renegade Crowns", false),
            Faction("Tomb Kings of Khemri", false, "Nehekharan Royal Host", "Mortuary Cult"),
            Faction("Skaven", false),
            Faction("Warriors of Chaos", false, "Wolves of the Sea", "Hordes of Chaos", "Heralds of Darkness"),
            Faction("Vampire Counts", false),
            Faction("Wood Elf Realms", false, "Orion's Wild Hunt", "Host of Talsyn"),
        ];
    }

    private static StoredFaction Faction(string name, bool requiresSubfaction, params string[] subfactions)
    {
        return new StoredFaction
        {
            Id = Guid.NewGuid(),
            Name = name,
            Color = "#2563EB",
            Subfactions = subfactions,
            RequiresSubfaction = requiresSubfaction,
        };
    }
}
