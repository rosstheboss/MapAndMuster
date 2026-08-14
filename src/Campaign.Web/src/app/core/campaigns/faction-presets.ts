export interface FactionPresetFaction {
  name: string;
  subfactions: readonly string[];
}

export interface FactionPreset {
  id: string;
  name: string;
  factions: readonly FactionPresetFaction[];
}

export const WARHAMMER_OLD_WORLD_PRESET_ID = 'warhammer-the-old-world';

const WARHAMMER_OLD_WORLD_FACTIONS: readonly FactionPresetFaction[] = [
  { name: 'Beastmen Brayherds', subfactions: ['Wild Herd', 'Minotaur Blood Herd'] },
  { name: 'Dark Elves', subfactions: [] },
  { name: 'Chaos Dwarfs', subfactions: [] },
  { name: 'Daemons of Chaos', subfactions: [] },
  {
    name: 'Dwarfen Mountain Holds',
    subfactions: ['Slayer Host', 'Expeditionary Force', 'Royal Clan'],
  },
  { name: 'Grand Cathay', subfactions: ['Warriors of Wind & Field', 'Jade Fleet'] },
  { name: 'Empire of Man', subfactions: ['Knightly Orders', 'City-state of Nuln'] },
  { name: 'High Elf Realms', subfactions: ['Sea Guard Garrison', 'Chracian Warhost'] },
  { name: 'Lizardmen', subfactions: [] },
  { name: 'Kingdom of Bretonnia', subfactions: ['Errantry Crusade', 'Bretonnian Exiles'] },
  { name: 'Ogre Kingdoms', subfactions: [] },
  { name: 'Orc & Goblin Tribes', subfactions: ['Troll Horde', 'Nomadic Waaagh!'] },
  { name: 'Tomb Kings of Khemri', subfactions: ['Nehekharan Royal Host', 'Mortuary Cult'] },
  { name: 'Skaven', subfactions: [] },
  {
    name: 'Warriors of Chaos',
    subfactions: ['Wolves of the Sea', 'Hordes of Chaos', 'Heralds of Darkness'],
  },
  { name: 'Vampire Counts', subfactions: [] },
  { name: 'Wood Elf Realms', subfactions: ["Orion's Wild Hunt", 'Host of Talsyn'] },
];

export const FACTION_PRESETS: readonly FactionPreset[] = [
  {
    id: WARHAMMER_OLD_WORLD_PRESET_ID,
    name: 'Warhammer: The Old World',
    factions: WARHAMMER_OLD_WORLD_FACTIONS,
  },
];

export function compareNames(left: string, right: string): number {
  return left.localeCompare(right, 'en', { sensitivity: 'base' });
}

export function sortedPresetFactions(factions: readonly FactionPresetFaction[]): FactionPresetFaction[] {
  return factions
    .map((faction) => ({
      name: faction.name,
      subfactions: [...faction.subfactions].sort(compareNames),
    }))
    .sort((left, right) => compareNames(left.name, right.name));
}

export function factionsFromPreset(presetId: string): FactionPresetFaction[] | null {
  const preset = FACTION_PRESETS.find((entry) => entry.id === presetId);
  if (!preset) {
    return null;
  }

  return sortedPresetFactions(preset.factions);
}
