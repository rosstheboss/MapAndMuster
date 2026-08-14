export interface MapPoint {
  x: number;
  y: number;
}

export const GEOMETRY_EPSILON = 1e-6;
export const MIN_SHARED_BORDER_LENGTH = 0.008;
export const SNAP_DISTANCE = 0.018;
export const MIN_DRAW_STEP = 0.008;

export function clamp01(value: number): number {
  if (value < 0) {
    return 0;
  }

  return value > 1 ? 1 : value;
}

export function clampPoint(point: MapPoint): MapPoint {
  return { x: clamp01(point.x), y: clamp01(point.y) };
}

export function distanceSquared(left: MapPoint, right: MapPoint): number {
  const dx = left.x - right.x;
  const dy = left.y - right.y;
  return dx * dx + dy * dy;
}

export function findSnapTarget(cursor: MapPoint, vertices: readonly MapPoint[]): MapPoint | null {
  let best: MapPoint | null = null;
  let bestDistance = SNAP_DISTANCE * SNAP_DISTANCE;
  for (const vertex of vertices) {
    const distance = distanceSquared(cursor, vertex);
    if (distance <= bestDistance) {
      best = vertex;
      bestDistance = distance;
    }
  }

  return best;
}

export function centroid(polygon: readonly MapPoint[]): MapPoint {
  if (polygon.length === 0) {
    return { x: 0.5, y: 0.5 };
  }

  let x = 0;
  let y = 0;
  for (const point of polygon) {
    x += point.x;
    y += point.y;
  }

  return { x: x / polygon.length, y: y / polygon.length };
}

export function polygonArea(polygon: readonly MapPoint[]): number {
  let area = 0;
  const count = polygon.length;
  for (let i = 0; i < count; i += 1) {
    const a = polygon.at(i);
    const b = polygon.at((i + 1) % count);
    if (!a || !b) {
      continue;
    }

    area += a.x * b.y - b.x * a.y;
  }

  return Math.abs(area) / 2;
}

export function isValidTerritoryPolygon(polygon: readonly MapPoint[]): boolean {
  const unique = distinctVertices(polygon);
  if (unique.length < 3) {
    return false;
  }

  for (const point of unique) {
    if (point.x < 0 || point.x > 1 || point.y < 0 || point.y > 1) {
      return false;
    }
  }

  return polygonArea(unique) >= GEOMETRY_EPSILON && !selfIntersects(unique);
}

export function interiorsOverlap(left: readonly MapPoint[], right: readonly MapPoint[]): boolean {
  if (left.length < 3 || right.length < 3) {
    return false;
  }

  if (hasProperEdgeCrossing(left, right)) {
    return true;
  }

  if (hasVertexStrictlyInside(left, right) || hasVertexStrictlyInside(right, left)) {
    return true;
  }

  return hasInteriorSampleInside(left, right) || hasInteriorSampleInside(right, left);
}

export function sharedBorderMidpoint(left: readonly MapPoint[], right: readonly MapPoint[]): MapPoint | null {
  let bestLength = MIN_SHARED_BORDER_LENGTH;
  let midpoint: MapPoint | null = null;
  const leftCount = left.length;
  const rightCount = right.length;
  for (let i = 0; i < leftCount; i += 1) {
    const a1 = left.at(i);
    const a2 = left.at((i + 1) % leftCount);
    if (!a1 || !a2) {
      continue;
    }

    for (let j = 0; j < rightCount; j += 1) {
      const b1 = right.at(j);
      const b2 = right.at((j + 1) % rightCount);
      if (!b1 || !b2) {
        continue;
      }

      const overlap = collinearOverlap(a1, a2, b1, b2);
      if (!overlap) {
        continue;
      }

      const length = Math.sqrt(distanceSquared(overlap.start, overlap.end));
      if (length >= bestLength) {
        bestLength = length;
        midpoint = {
          x: (overlap.start.x + overlap.end.x) / 2,
          y: (overlap.start.y + overlap.end.y) / 2,
        };
      }
    }
  }

  return midpoint;
}

export function containsStrict(polygon: readonly MapPoint[], point: MapPoint): boolean {
  if (isOnBoundary(polygon, point)) {
    return false;
  }

  let inside = false;
  const count = polygon.length;
  let j = count - 1;
  for (let i = 0; i < count; j = i, i += 1) {
    const a = polygon.at(i);
    const b = polygon.at(j);
    if (!a || !b) {
      continue;
    }

    const intersect =
      a.y > point.y !== b.y > point.y && point.x < ((b.x - a.x) * (point.y - a.y)) / (b.y - a.y + Number.EPSILON) + a.x;
    if (intersect) {
      inside = !inside;
    }
  }

  return inside;
}

export function polygonPointsAttribute(polygon: readonly MapPoint[]): string {
  return polygon.map((point) => `${point.x},${point.y}`).join(' ');
}

function distinctVertices(polygon: readonly MapPoint[]): MapPoint[] {
  const points: MapPoint[] = [];
  for (const point of polygon) {
    const previous = points.at(-1);
    if (previous && distanceSquared(previous, point) <= GEOMETRY_EPSILON * GEOMETRY_EPSILON) {
      continue;
    }

    points.push(point);
  }

  const first = points.at(0);
  const last = points.at(-1);
  if (points.length > 1 && first && last && distanceSquared(first, last) <= GEOMETRY_EPSILON * GEOMETRY_EPSILON) {
    points.pop();
  }

  return points;
}

function selfIntersects(polygon: readonly MapPoint[]): boolean {
  const count = polygon.length;
  for (let i = 0; i < count; i += 1) {
    const a1 = polygon.at(i);
    const a2 = polygon.at((i + 1) % count);
    if (!a1 || !a2) {
      continue;
    }

    for (let j = i + 1; j < count; j += 1) {
      if (Math.abs(i - j) <= 1 || (i === 0 && j === count - 1)) {
        continue;
      }

      const b1 = polygon.at(j);
      const b2 = polygon.at((j + 1) % count);
      if (b1 && b2 && segmentsProperlyIntersect(a1, a2, b1, b2)) {
        return true;
      }
    }
  }

  return false;
}

function hasProperEdgeCrossing(left: readonly MapPoint[], right: readonly MapPoint[]): boolean {
  const leftCount = left.length;
  const rightCount = right.length;
  for (let i = 0; i < leftCount; i += 1) {
    const a1 = left.at(i);
    const a2 = left.at((i + 1) % leftCount);
    if (!a1 || !a2) {
      continue;
    }

    for (let j = 0; j < rightCount; j += 1) {
      const b1 = right.at(j);
      const b2 = right.at((j + 1) % rightCount);
      if (b1 && b2 && segmentsProperlyIntersect(a1, a2, b1, b2)) {
        return true;
      }
    }
  }

  return false;
}

function hasVertexStrictlyInside(vertices: readonly MapPoint[], polygon: readonly MapPoint[]): boolean {
  return vertices.some((vertex) => containsStrict(polygon, vertex));
}

function hasInteriorSampleInside(source: readonly MapPoint[], other: readonly MapPoint[]): boolean {
  const center = centroid(source);
  if (containsStrict(other, center)) {
    return true;
  }

  return source.some((vertex) => containsStrict(other, { x: (vertex.x + center.x) / 2, y: (vertex.y + center.y) / 2 }));
}

function isOnBoundary(polygon: readonly MapPoint[], point: MapPoint): boolean {
  const count = polygon.length;
  for (let i = 0; i < count; i += 1) {
    const a = polygon.at(i);
    const b = polygon.at((i + 1) % count);
    if (a && b && pointOnSegment(a, b, point)) {
      return true;
    }
  }

  return false;
}

function segmentsProperlyIntersect(a1: MapPoint, a2: MapPoint, b1: MapPoint, b2: MapPoint): boolean {
  const o1 = orientation(a1, a2, b1);
  const o2 = orientation(a1, a2, b2);
  const o3 = orientation(b1, b2, a1);
  const o4 = orientation(b1, b2, a2);
  return o1 * o2 < 0 && o3 * o4 < 0;
}

function orientation(a: MapPoint, b: MapPoint, c: MapPoint): number {
  return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
}

function pointOnSegment(a: MapPoint, b: MapPoint, p: MapPoint): boolean {
  if (Math.abs(orientation(a, b, p)) > GEOMETRY_EPSILON) {
    return false;
  }

  const minX = Math.min(a.x, b.x) - GEOMETRY_EPSILON;
  const maxX = Math.max(a.x, b.x) + GEOMETRY_EPSILON;
  const minY = Math.min(a.y, b.y) - GEOMETRY_EPSILON;
  const maxY = Math.max(a.y, b.y) + GEOMETRY_EPSILON;
  return p.x >= minX && p.x <= maxX && p.y >= minY && p.y <= maxY;
}

function collinearOverlap(
  a1: MapPoint,
  a2: MapPoint,
  b1: MapPoint,
  b2: MapPoint,
): { start: MapPoint; end: MapPoint } | null {
  if (
    Math.abs(orientation(a1, a2, b1)) > GEOMETRY_EPSILON * 10 ||
    Math.abs(orientation(a1, a2, b2)) > GEOMETRY_EPSILON * 10
  ) {
    return null;
  }

  const dx = a2.x - a1.x;
  const dy = a2.y - a1.y;
  const axisLength = Math.sqrt(dx * dx + dy * dy);
  if (axisLength < GEOMETRY_EPSILON) {
    return null;
  }

  const project = (point: MapPoint): number => ((point.x - a1.x) * dx + (point.y - a1.y) * dy) / axisLength;
  let bMin = project(b1);
  let bMax = project(b2);
  if (bMin > bMax) {
    const swap = bMin;
    bMin = bMax;
    bMax = swap;
  }

  const start = Math.max(0, bMin);
  const end = Math.min(axisLength, bMax);
  if (end - start < GEOMETRY_EPSILON) {
    return null;
  }

  const ux = dx / axisLength;
  const uy = dy / axisLength;
  return {
    start: { x: a1.x + ux * start, y: a1.y + uy * start },
    end: { x: a1.x + ux * end, y: a1.y + uy * end },
  };
}
