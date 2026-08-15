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

export function adjacentTerritoryIds(adjacencies: readonly MapAdjacency[], selectedIds: readonly string[]): string[] {
  const selected = new Set(selectedIds.filter((id) => id.length > 0));
  const adjacent = new Set<string>();
  for (const edge of adjacencies) {
    if (selected.has(edge.territoryAId) && !selected.has(edge.territoryBId)) {
      adjacent.add(edge.territoryBId);
    } else if (selected.has(edge.territoryBId) && !selected.has(edge.territoryAId)) {
      adjacent.add(edge.territoryAId);
    }
  }

  return [...adjacent];
}

export function adjacencyArrowGeometry(
  marker: { x: number; y: number },
  from: { x: number; y: number },
  to: { x: number; y: number },
  halfLength: number,
): { x1: number; y1: number; x2: number; y2: number; headA: string; headB: string } {
  const dx = to.x - from.x;
  const dy = to.y - from.y;
  const length = Math.hypot(dx, dy) || 1;
  const ux = dx / length;
  const uy = dy / length;
  const head = halfLength * (12 / 28);
  const x1 = marker.x - ux * halfLength;
  const y1 = marker.y - uy * halfLength;
  const x2 = marker.x + ux * halfLength;
  const y2 = marker.y + uy * halfLength;
  const px = -uy;
  const py = ux;
  const headA = `${x1},${y1} ${x1 + ux * head + px * head * 0.55},${y1 + uy * head + py * head * 0.55} ${x1 + ux * head - px * head * 0.55},${y1 + uy * head - py * head * 0.55}`;
  const headB = `${x2},${y2} ${x2 - ux * head + px * head * 0.55},${y2 - uy * head + py * head * 0.55} ${x2 - ux * head - px * head * 0.55},${y2 - uy * head - py * head * 0.55}`;
  return { x1, y1, x2, y2, headA, headB };
}

function midpointOf(left: { x: number; y: number }, right: { x: number; y: number }): { x: number; y: number } {
  return { x: (left.x + right.x) / 2, y: (left.y + right.y) / 2 };
}
