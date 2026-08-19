import { specialRuleNamesForFaction, specialRuleNamesForSubfaction } from './special-rule-presets';

export interface FactionPresetFaction {
  name: string;
  color: string;
  subfactions: readonly string[];
  requiresSubfaction: boolean;
  specialRuleNames?: readonly string[];
  subfactionSpecialRules?: Readonly<Record<string, readonly string[]>>;
}

export interface FactionPreset {
  id: string;
  name: string;
  factions: readonly FactionPresetFaction[];
}

export const WARHAMMER_OLD_WORLD_PRESET_ID = 'warhammer-the-old-world';

export const FACTION_COLOR_PALETTE: readonly string[] = [
  '#2563EB',
  '#DC2626',
  '#16A34A',
  '#CA8A04',
  '#7C3AED',
  '#EA580C',
  '#0891B2',
  '#BE185D',
  '#4B5563',
  '#65A30D',
  '#C026D3',
  '#0F766E',
  '#1D4ED8',
  '#B45309',
  '#15803D',
  '#6D28D9',
  '#9F1239',
  '#0369A1',
  '#A16207',
  '#334155',
];

const WARHAMMER_OLD_WORLD_FACTIONS: readonly FactionPresetFaction[] = [
  {
    name: 'Beastmen Brayherds',
    color: '#5D4037',
    subfactions: ['Wild Herd', 'Minotaur Blood Herd'],
    requiresSubfaction: false,
  },
  { name: 'Dark Elves', color: '#4A148C', subfactions: [], requiresSubfaction: false },
  { name: 'Chaos Dwarfs', color: '#B45309', subfactions: [], requiresSubfaction: false },
  {
    name: 'Daemons of Chaos',
    color: '#AD1457',
    subfactions: ['Khorne', 'Nurgle', 'Slaanesh', 'Tzeentch'],
    requiresSubfaction: true,
  },
  {
    name: 'Dwarfen Mountain Holds',
    color: '#F59E0B',
    subfactions: ['Slayer Host', 'Expeditionary Force', 'Royal Clan'],
    requiresSubfaction: false,
  },
  {
    name: 'Grand Cathay',
    color: '#DC2626',
    subfactions: ['Warriors of Wind & Field', 'Jade Fleet'],
    requiresSubfaction: false,
  },
  {
    name: 'Empire of Man',
    color: '#F5D000',
    subfactions: ['Knightly Orders', 'City-state of Nuln'],
    requiresSubfaction: false,
  },
  {
    name: 'High Elf Realms',
    color: '#93C5FD',
    subfactions: ['Sea Guard Garrison', 'Chracian Warhost'],
    requiresSubfaction: false,
  },
  { name: 'Lizardmen', color: '#14B8A6', subfactions: [], requiresSubfaction: false },
  {
    name: 'Kingdom of Bretonnia',
    color: '#1E40AF',
    subfactions: ['Errantry Crusade', 'Bretonnian Exiles'],
    requiresSubfaction: false,
  },
  { name: 'Ogre Kingdoms', color: '#D6A05A', subfactions: [], requiresSubfaction: false },
  {
    name: 'Orc & Goblin Tribes',
    color: '#4D7C0F',
    subfactions: ['Troll Horde', 'Nomadic Waaagh!'],
    requiresSubfaction: false,
  },
  { name: 'Renegade Crowns', color: '#C2410C', subfactions: [], requiresSubfaction: false },
  {
    name: 'Tomb Kings of Khemri',
    color: '#EAB308',
    subfactions: ['Nehekharan Royal Host', 'Mortuary Cult'],
    requiresSubfaction: false,
  },
  { name: 'Skaven', color: '#78716C', subfactions: [], requiresSubfaction: false },
  {
    name: 'Warriors of Chaos',
    color: '#111827',
    subfactions: ['Wolves of the Sea', 'Hordes of Chaos', 'Heralds of Darkness'],
    requiresSubfaction: false,
  },
  { name: 'Vampire Counts', color: '#7F1D1D', subfactions: [], requiresSubfaction: false },
  {
    name: 'Wood Elf Realms',
    color: '#166534',
    subfactions: ["Orion's Wild Hunt", 'Host of Talsyn'],
    requiresSubfaction: false,
  },
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
      color: faction.color,
      requiresSubfaction: faction.requiresSubfaction,
      subfactions: [...faction.subfactions].sort(compareNames),
      specialRuleNames: [...(faction.specialRuleNames ?? specialRuleNamesForFaction(faction.name))],
      subfactionSpecialRules: Object.fromEntries(
        [...faction.subfactions].map((name) => [
          name,
          [...(faction.subfactionSpecialRules?.[name] ?? specialRuleNamesForSubfaction(faction.name, name))],
        ]),
      ),
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

export function nextUnusedFactionColor(used: readonly string[]): string {
  const taken = new Set(used.map((color) => color.toUpperCase()));
  const unused = FACTION_COLOR_PALETTE.find((color) => !taken.has(color.toUpperCase()));
  return unused ?? '#2563EB';
}
