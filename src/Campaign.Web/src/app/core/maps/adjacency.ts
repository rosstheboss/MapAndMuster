import { centroid, sharedBorderMidpoint } from './geometry';
import { createId, type MapAdjacency, type MapTerritory } from './map-graph.models';

export function orderedPair(left: string, right: string): [string, string] {
  return left <= right ? [left, right] : [right, left];
}

export function connects(edge: MapAdjacency, left: string, right: string): boolean {
  return (
    (edge.territoryAId === left && edge.territoryBId === right) ||
    (edge.territoryAId === right && edge.territoryBId === left)
  );
}

export function generateAdjacencies(
  territories: readonly MapTerritory[],
  existing: readonly MapAdjacency[],
): MapAdjacency[] {
  const manual = existing.filter((edge) => edge.origin === 'Manual');
  const manualPairs = new Set(manual.map((edge) => orderedPair(edge.territoryAId, edge.territoryBId).join(':')));
  const generated: MapAdjacency[] = [];

  for (let i = 0; i < territories.length; i += 1) {
    for (let j = i + 1; j < territories.length; j += 1) {
      const left = territories.at(i);
      const right = territories.at(j);
      if (!left || !right) {
        continue;
      }

      const pair = orderedPair(left.id, right.id).join(':');
      if (manualPairs.has(pair)) {
        continue;
      }

      const midpoint = sharedBorderMidpoint(left.polygon, right.polygon);
      if (!midpoint) {
        continue;
      }

      generated.push({
        id: createId(),
        territoryAId: left.id,
        territoryBId: right.id,
        origin: 'Generated',
        marker: midpoint,
      });
    }
  }

  return [...manual, ...generated];
}

export function adjacencyMarker(left: MapTerritory, right: MapTerritory): { x: number; y: number } {
  return (
    sharedBorderMidpoint(left.polygon, right.polygon) ?? midpointOf(centroid(left.polygon), centroid(right.polygon))
  );
}

function midpointOf(left: { x: number; y: number }, right: { x: number; y: number }): { x: number; y: number } {
  return { x: (left.x + right.x) / 2, y: (left.y + right.y) / 2 };
}
