export const TERRAIN_TYPES = [
  { id: 'Beach', label: 'Beach', overlayColor: '#E8C36A' },
  { id: 'Desert', label: 'Desert', overlayColor: '#D4A017' },
  { id: 'Highlands', label: 'Highlands', overlayColor: '#6B8E4E' },
  { id: 'Lake', label: 'Lake', overlayColor: '#5BA3C9' },
  { id: 'Mountain', label: 'Mountain', overlayColor: '#8A8680' },
  { id: 'Plains', label: 'Plains', overlayColor: '#7CB342' },
  { id: 'Riverlands', label: 'Riverlands', overlayColor: '#2E8B7A' },
  { id: 'Sea', label: 'Sea', overlayColor: '#1E5F8A' },
  { id: 'Swamp', label: 'Swamp', overlayColor: '#5C6B3A' },
] as const;

export type TerrainId = (typeof TERRAIN_TYPES)[number]['id'];

export function terrainLabel(id: string | null | undefined): string {
  return TERRAIN_TYPES.find((entry) => entry.id === id)?.label ?? 'None';
}

export function terrainOverlayColor(id: string | null | undefined): string | null {
  return TERRAIN_TYPES.find((entry) => entry.id === id)?.overlayColor ?? null;
}
