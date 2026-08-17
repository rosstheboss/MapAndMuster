import { STRUCTURE_TYPES } from '../maps/structures';
import type { DefaultStructureType } from './catalog-defaults';
import { defaultStructureCatalog } from './catalog-defaults';

export interface StructurePreset {
  id: string;
  name: string;
  structureTypes: readonly DefaultStructureType[];
}

export const STANDARD_STRUCTURES_PRESET_ID = 'standard-structures';

export const STRUCTURE_PRESETS: readonly StructurePreset[] = [
  {
    id: STANDARD_STRUCTURES_PRESET_ID,
    name: 'Standard structures',
    structureTypes: STRUCTURE_TYPES.map((entry) => ({
      name: entry.label,
      builtinSymbol: entry.id,
      isBuildable: entry.isBuildable,
      isPillageable: entry.isPillageable,
      isDestructible: entry.isDestructible,
    })),
  },
];

export function structureTypesFromPreset(presetId: string): DefaultStructureType[] | null {
  const preset = STRUCTURE_PRESETS.find((entry) => entry.id === presetId);
  if (!preset) {
    return null;
  }

  return preset.structureTypes.map((entry) => ({
    name: entry.name,
    builtinSymbol: entry.builtinSymbol,
    isBuildable: entry.isBuildable,
    isPillageable: entry.isPillageable,
    isDestructible: entry.isDestructible,
  }));
}

export function standardStructureTypes(): DefaultStructureType[] {
  return defaultStructureCatalog();
}
