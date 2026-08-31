import {
  CAMPAIGN_PRESETS,
  HUNT_IN_ESTALIA_CAMPAIGN_PRESET_ID,
  campaignFromPreset,
  campaignPresetApplyOptions,
  campaignPresetSaveNames,
} from './campaign-presets';
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
    expect(copy!.specialRules.length).toBeGreaterThan(0);
    expect(copy!.specialRules.some((rule) => rule.name === 'Crusaders')).toBe(true);
    expect(copy!.specialRules.find((rule) => rule.name === 'Crusaders')?.description).toContain(
      'two adjacent territories',
    );
    expect(copy!.forceStatuses.map((status) => status.name)).toEqual([
      'Diseased',
      'Shaken',
      'Confident',
      'Exhausted',
      'Well Rested',
    ]);
    expect(copy!.factions.find((faction) => faction.name === 'Beastmen Brayherds')?.specialRuleNames).toEqual([
      'Expert Ambushers',
    ]);
    expect(copy!.factions.find((faction) => faction.name === 'Daemons of Chaos')?.subfactionSpecialRules).toEqual({
      Khorne: ['Only Blood Satisfies!'],
      Nurgle: ['Bringers of the Plague'],
      Slaanesh: ['Alluring'],
      Tzeentch: ['Magical Supply'],
    });
    expect(copy!.factions.find((faction) => faction.name === 'Daemons of Chaos')?.subfactionAppearances).toEqual([
      { name: 'Khorne', color: '#B91C1C', flagSource: 'color' },
      { name: 'Nurgle', color: '#3F6212', flagSource: 'color' },
      { name: 'Slaanesh', color: '#F472B6', flagSource: 'color' },
      { name: 'Tzeentch', color: '#0E7490', flagSource: 'color' },
    ]);
    expect(copy!.factions.some((faction) => faction.name === 'Renegade Crowns')).toBe(true);
    expect(copy!.terrainTypes.find((entry) => entry.name === 'Sea')?.isWaterFeature).toBe(true);
    expect(campaignFromPreset('unknown')).toBeNull();
  });

  it('includes The Hunt in Estalia when listing names for save autocomplete', () => {
    expect(campaignPresetSaveNames([])).toEqual(['The Hunt in Estalia']);
    expect(campaignPresetSaveNames(['Frontier War', 'the hunt in estalia'])).toEqual([
      'Frontier War',
      'the hunt in estalia',
    ]);
    expect(campaignPresetSaveNames(['  The Hunt   in Estalia  '])).toEqual(['The Hunt in Estalia']);
  });

  it('replaces the catalog Hunt entry when a saved preset uses the same name', () => {
    expect(campaignPresetApplyOptions([])).toEqual([
      { id: HUNT_IN_ESTALIA_CAMPAIGN_PRESET_ID, name: 'The Hunt in Estalia' },
    ]);
    expect(
      campaignPresetApplyOptions([{ id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', name: '  The Hunt   in Estalia  ' }]),
    ).toEqual([{ id: 'saved:bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', name: 'The Hunt in Estalia' }]);
    expect(campaignPresetApplyOptions([{ id: 'cccccccc-cccc-cccc-cccc-cccccccccccc', name: 'Frontier War' }])).toEqual([
      { id: 'saved:cccccccc-cccc-cccc-cccc-cccccccccccc', name: 'Frontier War' },
      { id: HUNT_IN_ESTALIA_CAMPAIGN_PRESET_ID, name: 'The Hunt in Estalia' },
    ]);
  });
});
