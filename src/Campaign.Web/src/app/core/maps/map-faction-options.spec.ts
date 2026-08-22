import { describe, expect, it } from 'vitest';

import type { CampaignFaction } from '../campaigns/campaign.models';
import {
  mapFactionOptionLabel,
  mapFactionOptions,
  mapFactionOptionValue,
  parseMapFactionOptionValue,
} from './map-faction-options';

const daemons: CampaignFaction = {
  id: 'daemons',
  name: 'Daemons of Chaos',
  color: '#AD1457',
  subfactions: ['Tzeentch', 'Khorne', 'Nurgle', 'Slaanesh'],
  allyGroupName: null,
  requiresSubfaction: true,
  hasFlagImage: false,
};

const north: CampaignFaction = {
  id: 'north',
  name: 'North',
  color: '#2563EB',
  subfactions: ['Riders'],
  allyGroupName: null,
  requiresSubfaction: false,
  hasFlagImage: false,
};

const skaven: CampaignFaction = {
  id: 'skaven',
  name: 'Skaven',
  color: '#78716C',
  subfactions: [],
  allyGroupName: null,
  requiresSubfaction: false,
  hasFlagImage: false,
  specialRuleIds: ['underground'],
};

describe('map faction options', () => {
  it('lists required subfactions as parent-subfaction labels and keeps optional subfactions on the parent', () => {
    const options = mapFactionOptions({ factions: [daemons, north] });
    expect(options.map((option) => option.label)).toEqual([
      'Daemons of Chaos - Khorne',
      'Daemons of Chaos - Nurgle',
      'Daemons of Chaos - Slaanesh',
      'Daemons of Chaos - Tzeentch',
      'North',
    ]);
    expect(options.some((option) => option.label === 'Daemons of Chaos')).toBe(false);
  });

  it('disables spawn for factions whose special rules include UndergroundNetwork', () => {
    const options = mapFactionOptions({
      factions: [skaven, north],
      specialRules: [{ id: 'underground', name: 'The Underground Network', text: '', effectKey: 'UndergroundNetwork' }],
    });
    expect(options.find((option) => option.factionId === 'skaven')?.spawnDisabled).toBe(true);
    expect(options.find((option) => option.factionId === 'north')?.spawnDisabled).toBe(false);
  });

  it('round-trips option values and labels', () => {
    expect(parseMapFactionOptionValue(mapFactionOptionValue('daemons', 'Khorne'))).toEqual({
      factionId: 'daemons',
      subfaction: 'Khorne',
    });
    expect(mapFactionOptionLabel([daemons], 'daemons', 'Khorne')).toBe('Daemons of Chaos - Khorne');
    expect(mapFactionOptionLabel([daemons], null, null)).toBe('Neutral');
  });
});
