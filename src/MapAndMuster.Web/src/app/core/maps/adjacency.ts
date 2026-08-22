import { centroid, closestPolygonPoints, GEOMETRY_EPSILON, sharedBorderMidpoint, type MapPoint } from './geometry';
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

export function findConnection(
  adjacencies: readonly MapAdjacency[],
  left: string,
  right: string,
): MapAdjacency | undefined {
  return adjacencies.find((edge) => connects(edge, left, right));
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

export function adjacencyArrowEndpoints(
  left: readonly MapPoint[],
  right: readonly MapPoint[],
  inset = 0,
): { from: MapPoint; to: MapPoint } {
  const contacts = closestPolygonPoints(left, right);
  const leftCenter = centroid(left);
  const rightCenter = centroid(right);
  let dx = contacts.to.x - contacts.from.x;
  let dy = contacts.to.y - contacts.from.y;
  let span = Math.hypot(dx, dy);
  if (span < GEOMETRY_EPSILON) {
    dx = rightCenter.x - leftCenter.x;
    dy = rightCenter.y - leftCenter.y;
    span = Math.hypot(dx, dy) || 1;
  }

  const ux = dx / span;
  const uy = dy / span;
  const leftRoom = Math.hypot(contacts.from.x - leftCenter.x, contacts.from.y - leftCenter.y) * 0.85;
  const rightRoom = Math.hypot(contacts.to.x - rightCenter.x, contacts.to.y - rightCenter.y) * 0.85;
  const leftInset = Math.max(0, Math.min(inset, leftRoom));
  const rightInset = Math.max(0, Math.min(inset, rightRoom));
  return {
    from: { x: contacts.from.x - ux * leftInset, y: contacts.from.y - uy * leftInset },
    to: { x: contacts.to.x + ux * rightInset, y: contacts.to.y + uy * rightInset },
  };
}

export function adjacencyArrowGeometry(
  from: MapPoint,
  to: MapPoint,
  headLength: number,
): { x1: number; y1: number; x2: number; y2: number; headA: string; headB: string } {
  const dx = to.x - from.x;
  const dy = to.y - from.y;
  const length = Math.hypot(dx, dy) || 1;
  const ux = dx / length;
  const uy = dy / length;
  const head = Math.min(headLength, length * 0.35);
  const px = -uy;
  const py = ux;
  const headA = `${from.x},${from.y} ${from.x + ux * head + px * head * 0.55},${from.y + uy * head + py * head * 0.55} ${from.x + ux * head - px * head * 0.55},${from.y + uy * head - py * head * 0.55}`;
  const headB = `${to.x},${to.y} ${to.x - ux * head + px * head * 0.55},${to.y - uy * head + py * head * 0.55} ${to.x - ux * head - px * head * 0.55},${to.y - uy * head - py * head * 0.55}`;
  return { x1: from.x, y1: from.y, x2: to.x, y2: to.y, headA, headB };
}

function midpointOf(left: { x: number; y: number }, right: { x: number; y: number }): { x: number; y: number } {
  return { x: (left.x + right.x) / 2, y: (left.y + right.y) / 2 };
}
