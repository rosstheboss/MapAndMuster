export const TERRAIN_TYPES = [
  { id: 'Beach', label: 'Beach', overlayColor: '#E8C36A', isWaterFeature: true },
  { id: 'Cave', label: 'Cave', overlayColor: '#6B4F3A', isWaterFeature: false },
  { id: 'Desert', label: 'Desert', overlayColor: '#D4A017', isWaterFeature: false },
  { id: 'Forest', label: 'Forest', overlayColor: '#2E7D32', isWaterFeature: false },
  { id: 'Highlands', label: 'Highlands', overlayColor: '#C45C26', isWaterFeature: false },
  { id: 'Jungle', label: 'Jungle', overlayColor: '#0B8F4A', isWaterFeature: false },
  { id: 'Lake', label: 'Lake', overlayColor: '#5BA3C9', isWaterFeature: true },
  { id: 'Mountain', label: 'Mountain', overlayColor: '#8A8680', isWaterFeature: false },
  { id: 'Plains', label: 'Plains', overlayColor: '#7CB342', isWaterFeature: false },
  { id: 'Riverlands', label: 'Riverlands', overlayColor: '#2E8B7A', isWaterFeature: true },
  { id: 'Sea', label: 'Sea', overlayColor: '#1E5F8A', isWaterFeature: true },
  { id: 'Swamp', label: 'Swamp', overlayColor: '#5C6B3A', isWaterFeature: true },
] as const;

export type TerrainId = (typeof TERRAIN_TYPES)[number]['id'];

export function terrainLabel(id: string | null | undefined): string {
  return TERRAIN_TYPES.find((entry) => entry.id === id)?.label ?? 'None';
}

export function terrainOverlayColor(id: string | null | undefined): string | null {
  return TERRAIN_TYPES.find((entry) => entry.id === id)?.overlayColor ?? null;
}
