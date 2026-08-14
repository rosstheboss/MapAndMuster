import {
  FACTION_PRESETS,
  WARHAMMER_OLD_WORLD_PRESET_ID,
  factionsFromPreset,
  sortedPresetFactions,
} from './faction-presets';

describe('faction presets', () => {
  it('sorts factions and nested subfactions alphabetically without mutating the source', () => {
    const source = [
      { name: 'Zebra Host', subfactions: ['Gamma', 'Alpha'] },
      { name: 'Amber League', subfactions: [] },
    ];
    const sorted = sortedPresetFactions(source);

    expect(sorted.map((faction) => faction.name)).toEqual(['Amber League', 'Zebra Host']);
    expect(sorted[1]?.subfactions).toEqual(['Alpha', 'Gamma']);
    expect(source[0]?.name).toBe('Zebra Host');
    expect(source[0]?.subfactions).toEqual(['Gamma', 'Alpha']);
  });

  it('copies the Old World catalog in alphabetical order', () => {
    const factions = factionsFromPreset(WARHAMMER_OLD_WORLD_PRESET_ID);
    expect(FACTION_PRESETS[0]?.name).toBe('Warhammer: The Old World');
    expect(factions).not.toBeNull();
    expect(factions!.map((faction) => faction.name)).toEqual([
      'Beastmen Brayherds',
      'Chaos Dwarfs',
      'Daemons of Chaos',
      'Dark Elves',
      'Dwarfen Mountain Holds',
      'Empire of Man',
      'Grand Cathay',
      'High Elf Realms',
      'Kingdom of Bretonnia',
      'Lizardmen',
      'Ogre Kingdoms',
      'Orc & Goblin Tribes',
      'Skaven',
      'Tomb Kings of Khemri',
      'Vampire Counts',
      'Warriors of Chaos',
      'Wood Elf Realms',
    ]);
    expect(factions!.find((faction) => faction.name === 'Beastmen Brayherds')?.subfactions).toEqual([
      'Minotaur Blood Herd',
      'Wild Herd',
    ]);
    expect(factions!.find((faction) => faction.name === 'Warriors of Chaos')?.subfactions).toEqual([
      'Heralds of Darkness',
      'Hordes of Chaos',
      'Wolves of the Sea',
    ]);
    expect(factions!.find((faction) => faction.name === 'Wood Elf Realms')?.subfactions).toEqual([
      'Host of Talsyn',
      "Orion's Wild Hunt",
    ]);
  });

  it('does not change the catalog when a copied list is edited', () => {
    const factions = factionsFromPreset(WARHAMMER_OLD_WORLD_PRESET_ID);
    expect(factions).not.toBeNull();
    if (!factions) {
      return;
    }

    const first = factions[0];
    expect(first).toBeDefined();
    first.name = 'Renamed';

    const original = FACTION_PRESETS[0]?.factions.find((faction) => faction.name === 'Beastmen Brayherds');
    expect(original?.name).toBe('Beastmen Brayherds');
    expect(first.subfactions).not.toBe(original?.subfactions);
    expect(factionsFromPreset('unknown')).toBeNull();
  });
});
