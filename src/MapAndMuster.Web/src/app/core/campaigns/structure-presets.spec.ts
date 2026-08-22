import { STRUCTURE_TYPES } from '../maps/structures';
import { STANDARD_STRUCTURES_PRESET_ID, STRUCTURE_PRESETS, structureTypesFromPreset } from './structure-presets';

describe('structure presets', () => {
  it('copies the current standard structure catalog without sharing object identity', () => {
    const types = structureTypesFromPreset(STANDARD_STRUCTURES_PRESET_ID);
    expect(STRUCTURE_PRESETS).toHaveLength(1);
    expect(STRUCTURE_PRESETS[0]?.name).toBe('Standard structures');
    expect(types).not.toBeNull();
    expect(types!.map((entry) => entry.name)).toEqual(STRUCTURE_TYPES.map((entry) => entry.label));

    const first = types![0];
    first.name = 'Renamed';
    expect(STRUCTURE_PRESETS[0]?.structureTypes[0]?.name).toBe(STRUCTURE_TYPES[0].label);
    expect(structureTypesFromPreset('unknown')).toBeNull();
  });
});
