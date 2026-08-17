import { TERRAIN_TYPES } from '../maps/terrain';
import { STANDARD_TERRAIN_PRESET_ID, TERRAIN_PRESETS, terrainTypesFromPreset } from './terrain-presets';

describe('terrain presets', () => {
  it('copies the current standard terrain catalog without sharing object identity', () => {
    const types = terrainTypesFromPreset(STANDARD_TERRAIN_PRESET_ID);
    expect(TERRAIN_PRESETS).toHaveLength(1);
    expect(TERRAIN_PRESETS[0]?.name).toBe('Standard terrain');
    expect(types).not.toBeNull();
    expect(types!.map((entry) => entry.name)).toEqual(TERRAIN_TYPES.map((entry) => entry.label));
    expect(types!.find((entry) => entry.name === 'Highlands')?.color).toBe('#C45C26');

    const first = types![0];
    first.name = 'Renamed';
    expect(TERRAIN_PRESETS[0]?.terrainTypes[0]?.name).toBe(TERRAIN_TYPES[0].label);
    expect(terrainTypesFromPreset('unknown')).toBeNull();
  });
});
