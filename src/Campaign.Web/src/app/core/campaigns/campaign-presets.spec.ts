import { CAMPAIGN_PRESETS, HUNT_IN_ESTALIA_CAMPAIGN_PRESET_ID, campaignFromPreset } from './campaign-presets';
import { WARHAMMER_OLD_WORLD_PRESET_ID } from './faction-presets';
import { STANDARD_STRUCTURES_PRESET_ID } from './structure-presets';
import { STANDARD_TERRAIN_PRESET_ID } from './terrain-presets';

describe('campaign presets', () => {
  it('composes The Hunt in Estalia from the current catalog presets', () => {
    const copy = campaignFromPreset(HUNT_IN_ESTALIA_CAMPAIGN_PRESET_ID);
    expect(CAMPAIGN_PRESETS).toHaveLength(1);
    expect(CAMPAIGN_PRESETS[0]?.name).toBe('The Hunt in Estalia');
    expect(CAMPAIGN_PRESETS[0]?.factionPresetId).toBe(WARHAMMER_OLD_WORLD_PRESET_ID);
    expect(CAMPAIGN_PRESETS[0]?.terrainPresetId).toBe(STANDARD_TERRAIN_PRESET_ID);
    expect(CAMPAIGN_PRESETS[0]?.structurePresetId).toBe(STANDARD_STRUCTURES_PRESET_ID);
    expect(copy).not.toBeNull();
    expect(copy!.name).toBe('The Hunt in Estalia');
    expect(copy!.factions.some((faction) => faction.name === 'Daemons of Chaos')).toBe(true);
    expect(copy!.terrainTypes.some((entry) => entry.name === 'Highlands')).toBe(true);
    expect(copy!.structureTypes.some((entry) => entry.name === 'Town')).toBe(true);
    expect(copy!.itemObjectives).toEqual([]);
    expect(campaignFromPreset('unknown')).toBeNull();
  });
});
