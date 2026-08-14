import { generateAdjacencies } from './adjacency';
import {
  findSnapTarget,
  interiorsOverlap,
  isValidTerritoryPolygon,
  sharedBorderMidpoint,
  type MapPoint,
} from './geometry';
import type { MapAdjacency, MapTerritory } from './map-graph.models';

describe('map geometry', () => {
  const left = square(0.1, 0.1, 0.3);
  const right = square(0.4, 0.1, 0.3);

  it('allows a shared border and supplies a midpoint', () => {
    expect(interiorsOverlap(left, right)).toBe(false);
    const midpoint = sharedBorderMidpoint(left, right);
    expect(midpoint?.x).toBeCloseTo(0.4, 3);
    expect(midpoint?.y).toBeCloseTo(0.25, 3);
  });

  it('rejects overlapping interiors', () => {
    expect(interiorsOverlap(square(0.1, 0.1, 0.4), square(0.3, 0.1, 0.4))).toBe(true);
  });

  it('rejects self-intersecting polygons', () => {
    expect(
      isValidTerritoryPolygon([
        { x: 0.2, y: 0.2 },
        { x: 0.6, y: 0.6 },
        { x: 0.6, y: 0.2 },
        { x: 0.2, y: 0.6 },
      ]),
    ).toBe(false);
  });

  it('snaps the cursor to a nearby vertex', () => {
    expect(
      findSnapTarget({ x: 0.201, y: 0.199 }, [
        { x: 0.2, y: 0.2 },
        { x: 0.8, y: 0.8 },
      ]),
    ).toEqual({
      x: 0.2,
      y: 0.2,
    });
  });
});

describe('adjacency generation', () => {
  it('keeps manual arrows and skips those pairs on regenerate', () => {
    const territories: MapTerritory[] = [
      territory('a', 1, square(0.1, 0.1, 0.3)),
      territory('b', 2, square(0.4, 0.1, 0.3)),
      territory('c', 3, square(0.7, 0.1, 0.3)),
    ];
    const existing: MapAdjacency[] = [
      {
        id: 'manual',
        territoryAId: 'a',
        territoryBId: 'b',
        origin: 'Manual',
        marker: { x: 0.4, y: 0.9 },
      },
      {
        id: 'stale',
        territoryAId: 'b',
        territoryBId: 'c',
        origin: 'Generated',
        marker: { x: 0.1, y: 0.1 },
      },
    ];

    const generated = generateAdjacencies(territories, existing);
    expect(generated.filter((edge) => edge.origin === 'Manual')).toHaveLength(1);
    expect(generated.find((edge) => edge.origin === 'Manual')?.marker.y).toBe(0.9);
    expect(generated.some((edge) => edge.id === 'stale')).toBe(false);
    expect(generated.some((edge) => edge.origin === 'Generated' && edge.territoryAId === 'b')).toBe(true);
    expect(generated.some((edge) => edge.origin === 'Generated' && edge.territoryAId === 'a')).toBe(false);
  });
});

function square(x: number, y: number, size: number): MapPoint[] {
  return [
    { x, y },
    { x: x + size, y },
    { x: x + size, y: y + size },
    { x, y: y + size },
  ];
}

function territory(id: string, displayNumber: number, polygon: MapPoint[]): MapTerritory {
  return {
    id,
    displayNumber,
    name: null,
    description: null,
    polygon,
    terrainTypeId: 'plains',
    structureTypeId: null,
    overlayColor: null,
    ownerFactionId: null,
    spawnFactionId: null,
  };
}
