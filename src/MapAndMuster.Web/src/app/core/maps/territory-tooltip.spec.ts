import type { MapTerritory } from './map-graph.models';
import { territoryHoverTooltip } from './territory-tooltip';

const territory: MapTerritory = {
  id: 't1',
  displayNumber: 1,
  name: 'Coast',
  description: null,
  polygon: [],
  terrainTypeId: 'plains',
  structureTypeId: 'town',
  structureCondition: 'Pillaged',
  overlayColor: null,
  ownerFactionId: null,
  spawnFactionId: null,
};

const catalogs = {
  factions: [{ id: 'north', name: 'North' }],
  terrainTypes: [{ id: 'plains', name: 'Plains' }],
  structures: [{ id: 'town', name: 'Town' }],
};

describe('territoryHoverTooltip', () => {
  it('lists name, Neutral owner, pillaged structure, terrain, and no forces', () => {
    expect(territoryHoverTooltip(territory, catalogs)).toBe(
      ['Coast', 'Owner: Neutral', 'Town (pillaged)', 'Terrain: Plains', 'Forces: None'].join('\n'),
    );
  });

  it('names an owner and an intact structure, and omits a destroyed structure', () => {
    expect(
      territoryHoverTooltip({ ...territory, ownerFactionId: 'north', structureCondition: 'Operational' }, catalogs),
    ).toContain('Owner: North');
    expect(
      territoryHoverTooltip({ ...territory, ownerFactionId: 'north', structureCondition: 'Operational' }, catalogs),
    ).toContain('\nTown\n');
    expect(territoryHoverTooltip({ ...territory, structureCondition: 'Destroyed' }, catalogs)).not.toContain('Town');
  });

  it('lists forces, an open battle, and a retreating loser still on the territory', () => {
    const text = territoryHoverTooltip(territory, {
      ...catalogs,
      forces: [
        { id: 'winner', territoryId: 't1', name: 'Ada · North', inBattle: false },
        { id: 'loser', territoryId: 't1', name: 'Bob · South', inBattle: true },
      ],
      battles: [
        {
          territoryId: 't1',
          status: 'Finalized',
          participantForceIds: ['winner', 'loser'],
          winnerForceId: 'winner',
          isDraw: false,
        },
      ],
    });
    expect(text).toContain('Forces: Ada · North, Bob · South');
    expect(text).toContain('Retreating: Bob · South');
    expect(text).not.toMatch(/(^|\n)Battle(\n|$)/);
  });

  it('marks an open battle without listing a retreat', () => {
    const text = territoryHoverTooltip(territory, {
      ...catalogs,
      forces: [
        { id: 'a', territoryId: 't1', name: 'Ada · North', inBattle: true },
        { id: 'b', territoryId: 't1', name: 'Bob · South', inBattle: true },
      ],
      battles: [
        {
          territoryId: 't1',
          status: 'AwaitingResults',
          participantForceIds: ['a', 'b'],
          winnerForceId: null,
          isDraw: false,
        },
      ],
    });
    expect(text).toMatch(/(^|\n)Battle(\n|$)/);
    expect(text).not.toContain('Retreating:');
  });
});
