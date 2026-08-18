import { WARHAMMER_OLD_WORLD_PRESET_ID, factionsFromPreset } from './faction-presets';
import {
  HUNT_IN_ESTALIA_ITEM_OBJECTIVES_PRESET_ID,
  itemObjectivesFromPreset,
  type ItemObjectivePresetItem,
} from './item-objective-presets';
import { specialRulesFromOldWorldPreset, type SpecialRulePreset } from './special-rule-presets';
import { STANDARD_STRUCTURES_PRESET_ID, structureTypesFromPreset } from './structure-presets';
import { STANDARD_TERRAIN_PRESET_ID, terrainTypesFromPreset } from './terrain-presets';
import {
  STANDARD_FORCE_STATUSES_PRESET_ID,
  forceStatusesFromStandardPreset,
  type ForceStatusPreset,
} from './force-status-presets';
import type { DefaultStructureType } from './catalog-defaults';
import type { DefaultTerrainType } from './catalog-defaults';
import type { FactionPresetFaction } from './faction-presets';

export interface CampaignPresetCopy {
  name: string;
  factions: FactionPresetFaction[];
  terrainTypes: DefaultTerrainType[];
  structureTypes: DefaultStructureType[];
  itemObjectives: ItemObjectivePresetItem[];
  specialRules: SpecialRulePreset[];
  forceStatuses: ForceStatusPreset[];
}

export interface CampaignPreset {
  id: string;
  name: string;
  factionPresetId: string;
  terrainPresetId: string;
  structurePresetId: string;
  itemObjectivePresetId: string;
  forceStatusPresetId: string;
}

export const HUNT_IN_ESTALIA_CAMPAIGN_PRESET_ID = 'the-hunt-in-estalia';

export const CAMPAIGN_PRESETS: readonly CampaignPreset[] = [
  {
    id: HUNT_IN_ESTALIA_CAMPAIGN_PRESET_ID,
    name: 'The Hunt in Estalia',
    factionPresetId: WARHAMMER_OLD_WORLD_PRESET_ID,
    terrainPresetId: STANDARD_TERRAIN_PRESET_ID,
    structurePresetId: STANDARD_STRUCTURES_PRESET_ID,
    itemObjectivePresetId: HUNT_IN_ESTALIA_ITEM_OBJECTIVES_PRESET_ID,
    forceStatusPresetId: STANDARD_FORCE_STATUSES_PRESET_ID,
  },
];

export function campaignFromPreset(presetId: string): CampaignPresetCopy | null {
  const preset = CAMPAIGN_PRESETS.find((entry) => entry.id === presetId);
  if (!preset) {
    return null;
  }

  const factions = factionsFromPreset(preset.factionPresetId);
  const terrainTypes = terrainTypesFromPreset(preset.terrainPresetId);
  const structureTypes = structureTypesFromPreset(preset.structurePresetId);
  const itemObjectives = itemObjectivesFromPreset(preset.itemObjectivePresetId);
  if (!factions || !terrainTypes || !structureTypes || !itemObjectives) {
    return null;
  }

  return {
    name: preset.name,
    factions,
    terrainTypes,
    structureTypes,
    itemObjectives,
    specialRules: specialRulesFromOldWorldPreset(),
    forceStatuses: forceStatusesFromStandardPreset(),
  };
}
