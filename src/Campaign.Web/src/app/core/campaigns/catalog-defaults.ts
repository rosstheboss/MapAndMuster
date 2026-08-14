import { STRUCTURE_TYPES } from '../maps/structures';
import { TERRAIN_TYPES } from '../maps/terrain';

export interface DefaultTerrainType {
  name: string;
  color: string;
  missionName: string;
}

export interface DefaultStructureType {
  name: string;
  builtinSymbol: string;
}

export function defaultMissionName(terrainLabel: string): string {
  return `${terrainLabel.trim()} control`;
}

export function defaultTerrainCatalog(): DefaultTerrainType[] {
  return TERRAIN_TYPES.map((entry) => ({
    name: entry.label,
    color: entry.overlayColor,
    missionName: defaultMissionName(entry.label),
  }));
}

export function defaultStructureCatalog(): DefaultStructureType[] {
  return STRUCTURE_TYPES.map((entry) => ({
    name: entry.label,
    builtinSymbol: entry.id,
  }));
}
