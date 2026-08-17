import {
  adjacentTerritoryIds,
  adjacencyArrowEndpoints,
  adjacencyArrowGeometry,
  findConnection,
  generateAdjacencies,
} from './adjacency';
import {
  clampTranslation,
  encloseAlongImageEdge,
  encloseAlongTouchedBorders,
  findSnapTarget,
  fitSquareInPolygon,
  interiorsOverlap,
  isValidTerritoryPolygon,
  normalizedFromPixels,
  polygonIntersectsRect,
  resolveTerritoryTranslation,
  segmentsCross,
  sharedBorderMidpoint,
  traceSharedBorder,
  translatePolygon,
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

  it('allows extra vertices along a shared border even when they sit slightly inside', () => {
    const existing = square(0.1, 0.1, 0.3);
    const neighbor = [
      { x: 0.4, y: 0.1 },
      { x: 0.399, y: 0.2 },
      { x: 0.4, y: 0.3 },
      { x: 0.4, y: 0.4 },
      { x: 0.7, y: 0.4 },
      { x: 0.7, y: 0.1 },
    ];
    expect(interiorsOverlap(existing, neighbor)).toBe(false);
  });

  it('rejects a new border that overhangs into a territory', () => {
    const existing = square(0.1, 0.1, 0.3);
    const overhang = [
      { x: 0.4, y: 0.1 },
      { x: 0.25, y: 0.25 },
      { x: 0.4, y: 0.4 },
      { x: 0.7, y: 0.4 },
      { x: 0.7, y: 0.1 },
    ];
    expect(interiorsOverlap(existing, overhang)).toBe(true);
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

  it('converts screen pixels to normalized map units using zoom', () => {
    expect(normalizedFromPixels(8, 4000, 1)).toBeCloseTo(0.002, 6);
    expect(normalizedFromPixels(8, 4000, 4)).toBeCloseTo(0.0005, 6);
  });

  it('uses a tighter snap radius when one is supplied', () => {
    expect(
      findSnapTarget(
        { x: 0.21, y: 0.2 },
        [
          { x: 0.2, y: 0.2 },
          { x: 0.8, y: 0.8 },
        ],
        0.005,
      ),
    ).toBeNull();
    expect(
      findSnapTarget(
        { x: 0.202, y: 0.2 },
        [
          { x: 0.2, y: 0.2 },
          { x: 0.8, y: 0.8 },
        ],
        0.005,
      ),
    ).toEqual({ x: 0.2, y: 0.2 });
  });

  it('stretches an adjacency arrow so heads and a short shaft overhang both territories', () => {
    const left = square(0.1, 0.1, 0.2);
    const right = square(0.7, 0.1, 0.2);
    const ends = adjacencyArrowEndpoints(left, right, 0.05);
    expect(ends.from.x).toBeLessThan(0.3);
    expect(ends.from.x).toBeGreaterThan(0.1);
    expect(ends.to.x).toBeGreaterThan(0.7);
    expect(ends.to.x).toBeLessThan(0.9);
    const geometry = adjacencyArrowGeometry(ends.from, ends.to, 0.02);
    expect(geometry.x1).toBe(ends.from.x);
    expect(geometry.x2).toBe(ends.to.x);
  });

  it('keeps adjacent connection arrows from crossing', () => {
    const a = square(0.1, 0.1, 0.3);
    const b = square(0.4, 0.1, 0.3);
    const c = square(0.1, 0.4, 0.3);
    const ab = adjacencyArrowEndpoints(a, b, 0.04);
    const ac = adjacencyArrowEndpoints(a, c, 0.04);
    expect(segmentsCross(ab.from, ab.to, ac.from, ac.to)).toBe(false);
  });

  it('detects a territory that intersects a selection box', () => {
    expect(polygonIntersectsRect(square(0.1, 0.1, 0.2), 0.25, 0.15, 0.4, 0.3)).toBe(true);
    expect(polygonIntersectsRect(square(0.1, 0.1, 0.2), 0.5, 0.5, 0.7, 0.7)).toBe(false);
  });

  it('traces an unobstructed shared border between two endpoints', () => {
    const existing = square(0.1, 0.1, 0.3);
    const traced = traceSharedBorder({ x: 0.4, y: 0.12 }, { x: 0.4, y: 0.38 }, [existing]);
    expect(traced).toEqual([
      { x: 0.4, y: 0.12 },
      { x: 0.4, y: 0.38 },
    ]);
  });

  it('inserts existing vertices when endpoints sit on the same territory outline', () => {
    const existing = square(0.1, 0.1, 0.3);
    const traced = traceSharedBorder({ x: 0.2, y: 0.1 }, { x: 0.4, y: 0.2 }, [existing]);
    expect(traced?.[0]).toEqual({ x: 0.2, y: 0.1 });
    expect(traced?.at(-1)).toEqual({ x: 0.4, y: 0.2 });
    expect(traced?.some((point) => point.x === 0.4 && point.y === 0.1)).toBe(true);
  });

  it('does not trace a border already shared with another territory', () => {
    const leftPoly = square(0.1, 0.1, 0.3);
    const rightPoly = square(0.4, 0.1, 0.3);
    expect(traceSharedBorder({ x: 0.4, y: 0.15 }, { x: 0.4, y: 0.35 }, [leftPoly, rightPoly])).toBeNull();
  });

  it('encloses a pocket by walking the touched territory border', () => {
    const existing = square(0.1, 0.4, 0.3);
    const enclosed = encloseAlongTouchedBorders(
      [
        { x: 0.1, y: 0.4 },
        { x: 0.25, y: 0.2 },
        { x: 0.4, y: 0.4 },
      ],
      [existing],
    );
    expect(enclosed).toBeTruthy();
    expect(isValidTerritoryPolygon(enclosed ?? [])).toBe(true);
    expect(interiorsOverlap(enclosed ?? [], existing)).toBe(false);
  });

  it('encloses a pocket when the drawn line has extra vertices along a shared border', () => {
    const existing = square(0.1, 0.4, 0.3);
    const enclosed = encloseAlongTouchedBorders(
      [
        { x: 0.1, y: 0.4 },
        { x: 0.2, y: 0.4 },
        { x: 0.25, y: 0.2 },
        { x: 0.4, y: 0.4 },
      ],
      [existing],
    );
    expect(enclosed).toBeTruthy();
    expect(isValidTerritoryPolygon(enclosed ?? [])).toBe(true);
    expect(interiorsOverlap(enclosed ?? [], existing)).toBe(false);
  });

  it('encloses a pocket against two adjacent territories', () => {
    const leftPoly = square(0.1, 0.2, 0.3);
    const rightPoly = square(0.4, 0.2, 0.3);
    const enclosed = encloseAlongTouchedBorders(
      [
        { x: 0.2, y: 0.2 },
        { x: 0.4, y: 0.05 },
        { x: 0.55, y: 0.2 },
      ],
      [leftPoly, rightPoly],
    );
    expect(enclosed).toBeTruthy();
    expect(interiorsOverlap(enclosed ?? [], leftPoly)).toBe(false);
    expect(interiorsOverlap(enclosed ?? [], rightPoly)).toBe(false);
  });

  it('fits a 50px-class marker at the center when the territory is large enough', () => {
    const fitted = fitSquareInPolygon(square(0.1, 0.1, 0.4), { x: 0.3, y: 0.3 }, 0.05, 0.05);
    expect(fitted.x).toBeCloseTo(0.3, 3);
    expect(fitted.y).toBeCloseTo(0.3, 3);
    expect(fitted.width).toBeCloseTo(0.05, 3);
  });

  it('places a later marker away from occupied squares', () => {
    const occupied = { x: 0.3, y: 0.3, width: 0.08, height: 0.08 };
    const preferred = { x: 0.36, y: 0.3 };
    const polygon = square(0.1, 0.1, 0.4);
    const overlapping = fitSquareInPolygon(polygon, preferred, 0.08, 0.08);
    const fitted = fitSquareInPolygon(polygon, preferred, 0.08, 0.08, [occupied]);
    const overlapsOccupied = (marker: { x: number; y: number; width: number; height: number }): boolean =>
      Math.abs(marker.x - occupied.x) < (marker.width + occupied.width) / 2 &&
      Math.abs(marker.y - occupied.y) < (marker.height + occupied.height) / 2;
    expect(overlapsOccupied(overlapping)).toBe(true);
    expect(overlapsOccupied(fitted)).toBe(false);
  });

  it('encloses a drawn line along the map image edge', () => {
    const enclosed = encloseAlongImageEdge(
      [
        { x: 0, y: 0.3 },
        { x: 0.2, y: 0.5 },
        { x: 0, y: 0.7 },
      ],
      [],
    );
    expect(enclosed).toBeTruthy();
    expect(isValidTerritoryPolygon(enclosed ?? [])).toBe(true);
    expect(enclosed?.some((point) => point.x === 0 && point.y === 0)).toBe(false);
    expect(enclosed?.some((point) => point.x === 1)).toBe(false);
  });

  it('does not enclose along the image edge unless both endpoints sit on it', () => {
    expect(
      encloseAlongImageEdge(
        [
          { x: 0, y: 0.3 },
          { x: 0.2, y: 0.5 },
          { x: 0.4, y: 0.4 },
        ],
        [],
      ),
    ).toBeNull();
  });

  it('clamps a group translation so every vertex stays on the map', () => {
    expect(clampTranslation([square(0.1, 0.1, 0.3)], -1, 0)).toEqual({ x: -0.1, y: 0 });
    expect(clampTranslation([square(0.1, 0.1, 0.3)], 1, 1)).toEqual({ x: 0.6, y: 0.6 });
    expect(translatePolygon(square(0.1, 0.1, 0.3), 0.05, 0)[0]?.x).toBeCloseTo(0.15, 5);
    expect(translatePolygon(square(0.1, 0.1, 0.3), 0.05, 0)[0]?.y).toBeCloseTo(0.1, 5);
  });

  it('snaps a move back onto a touching border instead of overlapping interiors', () => {
    const moving = [square(0.1, 0.1, 0.3)];
    const neighbor = [square(0.4, 0.1, 0.3)];
    const nearShared = resolveTerritoryTranslation(moving, neighbor, 0.01, 0);
    expect(nearShared?.x).toBeCloseTo(0.01, 5);
    expect(interiorsOverlap(translatePolygon(moving[0] ?? [], nearShared?.x ?? 1, 0), neighbor[0] ?? [])).toBe(false);
    const blocked = resolveTerritoryTranslation(moving, neighbor, 0.2, 0);
    expect(blocked?.x ?? 1).toBeLessThan(0.05);
    expect(interiorsOverlap(translatePolygon(moving[0] ?? [], blocked?.x ?? 1, 0), neighbor[0] ?? [])).toBe(false);
    const along = resolveTerritoryTranslation(moving, neighbor, 0, 0.04);
    expect(along?.x).toBeCloseTo(0, 5);
    expect(along?.y).toBeCloseTo(0.04, 5);
    const away = resolveTerritoryTranslation(moving, neighbor, -0.1, 0);
    expect(away?.x).toBeCloseTo(-0.1, 5);
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

  it('finds a connection regardless of territory order', () => {
    const edge: MapAdjacency = {
      id: 'ab',
      territoryAId: 'a',
      territoryBId: 'b',
      origin: 'Manual',
      marker: { x: 0.4, y: 0.25 },
    };
    expect(findConnection([edge], 'b', 'a')?.id).toBe('ab');
    expect(findConnection([edge], 'a', 'c')).toBeUndefined();
  });

  it('lists neighboring territories and omits the selected ones', () => {
    const territories: MapTerritory[] = [
      territory('a', 1, square(0.1, 0.1, 0.3)),
      territory('b', 2, square(0.4, 0.1, 0.3)),
      territory('c', 3, square(0.7, 0.1, 0.3)),
    ];
    const edges = generateAdjacencies(territories, []);
    expect(adjacentTerritoryIds(edges, ['a']).sort()).toEqual(['b']);
    expect(adjacentTerritoryIds(edges, ['a', 'b']).sort()).toEqual(['c']);
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
    structureCondition: 'Operational',
    overlayColor: null,
    ownerFactionId: null,
    spawnFactionId: null,
  };
}
