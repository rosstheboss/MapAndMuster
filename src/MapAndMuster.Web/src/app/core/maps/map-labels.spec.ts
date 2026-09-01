import { overlayNameLabel } from './map-labels';
import type { MapTerritory } from './map-graph.models';

function territory(overrides: Partial<MapTerritory> = {}): MapTerritory {
  return {
    id: 't1',
    displayNumber: 12,
    name: null,
    description: null,
    polygon: [
      { x: 0.1, y: 0.1 },
      { x: 0.3, y: 0.1 },
      { x: 0.3, y: 0.3 },
      { x: 0.1, y: 0.3 },
    ],
    terrainTypeId: 'plains',
    structureTypeId: null,
    structureCondition: 'Operational',
    overlayColor: null,
    ownerFactionId: null,
    spawnFactionId: null,
    ...overrides,
  };
}

describe('overlayNameLabel', () => {
  it('always draws the full name, even when the polygon is too small for the text', () => {
    expect(overlayNameLabel(territory({ name: 'Coastal Highlands' }), { width: 40 }, 0.2)).toBe('Coastal Highlands');
  });

  it('hides a display number when the polygon is too small', () => {
    expect(overlayNameLabel(territory({ name: null, displayNumber: 4 }), { width: 40 }, 0.2)).toBeNull();
  });

  it('draws a display number when the unnamed territory is wide enough', () => {
    expect(overlayNameLabel(territory({ name: '  ', displayNumber: 4 }), { width: 1000 }, 1)).toBe('4');
  });
});
