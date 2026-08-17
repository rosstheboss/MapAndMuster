import { TERRAIN_TYPES } from '../maps/terrain';
import type { DefaultTerrainType } from './catalog-defaults';
import { defaultTerrainCatalog } from './catalog-defaults';

export interface TerrainPreset {
  id: string;
  name: string;
  terrainTypes: readonly DefaultTerrainType[];
}

export const STANDARD_TERRAIN_PRESET_ID = 'standard-terrain';

export const TERRAIN_PRESETS: readonly TerrainPreset[] = [
  {
    id: STANDARD_TERRAIN_PRESET_ID,
    name: 'Standard terrain',
    terrainTypes: TERRAIN_TYPES.map((entry) => ({
      name: entry.label,
      color: entry.overlayColor,
      isWaterFeature: entry.isWaterFeature,
    })),
  },
];

export function terrainTypesFromPreset(presetId: string): DefaultTerrainType[] | null {
  const preset = TERRAIN_PRESETS.find((entry) => entry.id === presetId);
  if (!preset) {
    return null;
  }

  return preset.terrainTypes.map((entry) => ({
    name: entry.name,
    color: entry.color,
    isWaterFeature: entry.isWaterFeature === true,
  }));
}

export function standardTerrainTypes(): DefaultTerrainType[] {
  return defaultTerrainCatalog();
}
