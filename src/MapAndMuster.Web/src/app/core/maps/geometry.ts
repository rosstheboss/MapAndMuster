export interface MapPoint {
  x: number;
  y: number;
}

export const GEOMETRY_EPSILON = 1e-6;
export const MIN_SHARED_BORDER_LENGTH = 0.008;
export const BORDER_TRACE_TOLERANCE = 0.002;
export const SNAP_DISTANCE = 0.018;
export const MIN_DRAW_STEP = 0.008;
export const MARKER_MAX_PX = 50;
export const MIN_ZOOM = 0.1;
export const MAX_ZOOM = 8;
export const ZOOM_STEP = 0.1;
export const SNAP_SCREEN_PX = 10;
export const MIN_DRAW_SCREEN_PX = 2;
export const CLOSE_POLYGON_SCREEN_PX = 12;
export const STROKE_SCREEN_PX = 2.5;
export const STROKE_FULL_HIGHLIGHT_SCREEN_PX = STROKE_SCREEN_PX * 2;
export const STROKE_HALF_HIGHLIGHT_SCREEN_PX = STROKE_SCREEN_PX * 1.5;
export const DRAWING_STROKE_SCREEN_PX = 1.75;
export const VERTEX_SCREEN_PX = 3.25;
export const SNAP_RING_SCREEN_PX = 6;
export const ARROW_HEAD_SCREEN_PX = 10;
export const ARROW_OVERHANG_LINE_SCREEN_PX = 10;
export const ARROW_HIT_SCREEN_PX = 16;
export const ARROW_HOVER_SCALE = 1.5;
export const OVERLAY_FILL_OPACITY = 0.32;

export function normalizedFromPixels(pixels: number, imageWidth: number, scale: number): number {
  return pixels / Math.max(imageWidth * scale, Number.EPSILON);
}

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

export function findSnapTarget(
  cursor: MapPoint,
  vertices: readonly MapPoint[],
  snapDistance = SNAP_DISTANCE,
): MapPoint | null {
  let best: MapPoint | null = null;
  let bestDistance = snapDistance * snapDistance;
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

export interface AxisAlignedBounds {
  minX: number;
  minY: number;
  maxX: number;
  maxY: number;
}

export function unionPolygonBounds(polygons: readonly (readonly MapPoint[])[]): AxisAlignedBounds | null {
  let minX = Number.POSITIVE_INFINITY;
  let minY = Number.POSITIVE_INFINITY;
  let maxX = Number.NEGATIVE_INFINITY;
  let maxY = Number.NEGATIVE_INFINITY;
  for (const polygon of polygons) {
    for (const point of polygon) {
      minX = Math.min(minX, point.x);
      minY = Math.min(minY, point.y);
      maxX = Math.max(maxX, point.x);
      maxY = Math.max(maxY, point.y);
    }
  }

  if (!Number.isFinite(minX)) {
    return null;
  }

  return { minX, minY, maxX, maxY };
}

/** A point inside the polygon, used so markers do not sit in a hole or on a neighbor. */
export function interiorAnchor(polygon: readonly MapPoint[]): MapPoint {
  if (polygon.length === 0) {
    return { x: 0.5, y: 0.5 };
  }

  const areaCenter = areaCentroid(polygon);
  if (containsStrict(polygon, areaCenter)) {
    return areaCenter;
  }

  const vertexCenter = centroid(polygon);
  if (containsStrict(polygon, vertexCenter)) {
    return vertexCenter;
  }

  let minX = Number.POSITIVE_INFINITY;
  let minY = Number.POSITIVE_INFINITY;
  let maxX = Number.NEGATIVE_INFINITY;
  let maxY = Number.NEGATIVE_INFINITY;
  for (const point of polygon) {
    minX = Math.min(minX, point.x);
    minY = Math.min(minY, point.y);
    maxX = Math.max(maxX, point.x);
    maxY = Math.max(maxY, point.y);
  }

  let best = vertexCenter;
  let bestScore = Number.NEGATIVE_INFINITY;
  const consider = (candidate: MapPoint): void => {
    if (!containsStrict(polygon, candidate)) {
      return;
    }

    const score = distanceToBoundary(polygon, candidate);
    if (score > bestScore) {
      best = candidate;
      bestScore = score;
    }
  };

  const steps = 20;
  const spanX = maxX - minX;
  const spanY = maxY - minY;
  for (let i = 1; i < steps; i += 1) {
    for (let j = 1; j < steps; j += 1) {
      consider({
        x: minX + (spanX * i) / steps,
        y: minY + (spanY * j) / steps,
      });
    }
  }

  const count = polygon.length;
  for (let i = 0; i < count; i += 1) {
    const a = polygon.at(i);
    const b = polygon.at((i + 1) % count);
    if (!a || !b) {
      continue;
    }

    const mid = { x: (a.x + b.x) / 2, y: (a.y + b.y) / 2 };
    const dx = b.x - a.x;
    const dy = b.y - a.y;
    const length = Math.hypot(dx, dy);
    if (length < GEOMETRY_EPSILON) {
      continue;
    }

    const inset = Math.min(0.008, length / 4);
    consider({ x: mid.x - (dy / length) * inset, y: mid.y + (dx / length) * inset });
    consider({ x: mid.x + (dy / length) * inset, y: mid.y - (dx / length) * inset });
  }

  return bestScore > Number.NEGATIVE_INFINITY ? best : vertexCenter;
}

function areaCentroid(polygon: readonly MapPoint[]): MapPoint {
  let x = 0;
  let y = 0;
  let twiceArea = 0;
  const count = polygon.length;
  for (let i = 0; i < count; i += 1) {
    const a = polygon.at(i);
    const b = polygon.at((i + 1) % count);
    if (!a || !b) {
      continue;
    }

    const cross = a.x * b.y - b.x * a.y;
    twiceArea += cross;
    x += (a.x + b.x) * cross;
    y += (a.y + b.y) * cross;
  }

  if (Math.abs(twiceArea) < GEOMETRY_EPSILON) {
    return centroid(polygon);
  }

  return { x: x / (3 * twiceArea), y: y / (3 * twiceArea) };
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

/** Returns true when interiors cover each other. Shared and near-shared traced borders are allowed. */
export function interiorsOverlap(left: readonly MapPoint[], right: readonly MapPoint[]): boolean {
  if (left.length < 3 || right.length < 3) {
    return false;
  }

  if (hasProperEdgeCrossing(left, right)) {
    return true;
  }

  if (hasVertexDeepInside(left, right) || hasVertexDeepInside(right, left)) {
    return true;
  }

  return hasSharedInteriorSample(left, right) || hasSharedInteriorSample(right, left);
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

export function pointOnPolygonBoundary(polygon: readonly MapPoint[], point: MapPoint): boolean {
  return isOnBoundary(polygon, point);
}

export function segmentIntersection(
  a1: MapPoint,
  a2: MapPoint,
  b1: MapPoint,
  b2: MapPoint,
): { t: number; u: number; point: MapPoint } | null {
  const dx1 = a2.x - a1.x;
  const dy1 = a2.y - a1.y;
  const dx2 = b2.x - b1.x;
  const dy2 = b2.y - b1.y;
  const denom = dx1 * dy2 - dy1 * dx2;
  if (Math.abs(denom) < GEOMETRY_EPSILON) {
    return null;
  }

  const t = ((b1.x - a1.x) * dy2 - (b1.y - a1.y) * dx2) / denom;
  const u = ((b1.x - a1.x) * dy1 - (b1.y - a1.y) * dx1) / denom;
  if (t < -GEOMETRY_EPSILON || t > 1 + GEOMETRY_EPSILON || u < -GEOMETRY_EPSILON || u > 1 + GEOMETRY_EPSILON) {
    return null;
  }

  return { t, u, point: { x: a1.x + t * dx1, y: a1.y + t * dy1 } };
}

export function segmentPolygonHits(
  polygon: readonly MapPoint[],
  start: MapPoint,
  end: MapPoint,
): { t: number; point: MapPoint }[] {
  const hits: { t: number; point: MapPoint }[] = [];
  const count = polygon.length;
  for (let i = 0; i < count; i += 1) {
    const a = polygon.at(i);
    const b = polygon.at((i + 1) % count);
    if (!a || !b) {
      continue;
    }

    const hit = segmentIntersection(start, end, a, b);
    if (hit) {
      hits.push({ t: hit.t, point: hit.point });
    }
  }

  return hits;
}

export function polygonIntersectsRect(
  polygon: readonly MapPoint[],
  left: number,
  top: number,
  right: number,
  bottom: number,
): boolean {
  const minX = Math.min(left, right);
  const maxX = Math.max(left, right);
  const minY = Math.min(top, bottom);
  const maxY = Math.max(top, bottom);
  for (const point of polygon) {
    if (point.x >= minX && point.x <= maxX && point.y >= minY && point.y <= maxY) {
      return true;
    }
  }

  const corners: MapPoint[] = [
    { x: minX, y: minY },
    { x: maxX, y: minY },
    { x: maxX, y: maxY },
    { x: minX, y: maxY },
  ];
  for (const corner of corners) {
    if (containsStrict(polygon, corner) || isOnBoundary(polygon, corner)) {
      return true;
    }
  }

  const rectEdges: [MapPoint, MapPoint][] = [
    [corners[0], corners[1]],
    [corners[1], corners[2]],
    [corners[2], corners[3]],
    [corners[3], corners[0]],
  ];
  const count = polygon.length;
  for (let i = 0; i < count; i += 1) {
    const a = polygon.at(i);
    const b = polygon.at((i + 1) % count);
    if (!a || !b) {
      continue;
    }

    for (const [start, end] of rectEdges) {
      if (segmentsProperlyIntersect(a, b, start, end) || collinearOverlap(a, b, start, end)) {
        return true;
      }
    }
  }

  return false;
}

export function snapToExistingGeometry(
  cursor: MapPoint,
  vertices: readonly MapPoint[],
  polygons: readonly (readonly MapPoint[])[],
  snapDistance = SNAP_DISTANCE,
): MapPoint | null {
  const vertex = findSnapTarget(cursor, vertices, snapDistance);
  if (vertex) {
    return vertex;
  }

  return snapToNearestEdge(cursor, polygons, snapDistance);
}

export const MAP_FRAME: readonly MapPoint[] = [
  { x: 0, y: 0 },
  { x: 1, y: 0 },
  { x: 1, y: 1 },
  { x: 0, y: 1 },
];

export function isOnImageEdge(point: MapPoint): boolean {
  return (
    point.x <= SNAP_DISTANCE || point.x >= 1 - SNAP_DISTANCE || point.y <= SNAP_DISTANCE || point.y >= 1 - SNAP_DISTANCE
  );
}

export function snapToImageEdge(point: MapPoint): MapPoint {
  return {
    x: point.x <= SNAP_DISTANCE ? 0 : point.x >= 1 - SNAP_DISTANCE ? 1 : point.x,
    y: point.y <= SNAP_DISTANCE ? 0 : point.y >= 1 - SNAP_DISTANCE ? 1 : point.y,
  };
}

export function clampTranslation(polygons: readonly (readonly MapPoint[])[], dx: number, dy: number): MapPoint {
  let minX = Number.POSITIVE_INFINITY;
  let maxX = Number.NEGATIVE_INFINITY;
  let minY = Number.POSITIVE_INFINITY;
  let maxY = Number.NEGATIVE_INFINITY;
  for (const polygon of polygons) {
    for (const point of polygon) {
      minX = Math.min(minX, point.x);
      maxX = Math.max(maxX, point.x);
      minY = Math.min(minY, point.y);
      maxY = Math.max(maxY, point.y);
    }
  }

  if (!Number.isFinite(minX)) {
    return { x: 0, y: 0 };
  }

  return {
    x: Math.min(1 - maxX, Math.max(-minX, dx)),
    y: Math.min(1 - maxY, Math.max(-minY, dy)),
  };
}

export function translatePolygon(polygon: readonly MapPoint[], dx: number, dy: number): MapPoint[] {
  return polygon.map((point) => ({ x: point.x + dx, y: point.y + dy }));
}

export function closestPolygonPoints(
  left: readonly MapPoint[],
  right: readonly MapPoint[],
): { from: MapPoint; to: MapPoint } {
  const shared = sharedBorderMidpoint(left, right);
  if (shared) {
    return { from: shared, to: shared };
  }

  let best = Number.POSITIVE_INFINITY;
  let from = centroid(left);
  let to = centroid(right);
  const consider = (a: MapPoint, b: MapPoint): void => {
    const distance = distanceSquared(a, b);
    if (distance < best) {
      best = distance;
      from = a;
      to = b;
    }
  };

  for (const vertex of left) {
    for (const point of closestPointsOnEdges(right, vertex)) {
      consider(vertex, point);
    }
  }

  for (const vertex of right) {
    for (const point of closestPointsOnEdges(left, vertex)) {
      consider(point, vertex);
    }
  }

  for (const leftVertex of left) {
    for (const rightVertex of right) {
      consider(leftVertex, rightVertex);
    }
  }

  return { from, to };
}

export function resolveTerritoryTranslation(
  selectedPolygons: readonly (readonly MapPoint[])[],
  otherPolygons: readonly (readonly MapPoint[])[],
  dx: number,
  dy: number,
  snapDistance = SNAP_DISTANCE,
): MapPoint | null {
  const clamped = clampTranslation(selectedPolygons, dx, dy);
  const candidates: MapPoint[] = [];
  const aligned = alignTranslationToNeighborEdges(selectedPolygons, otherPolygons, clamped, snapDistance);
  if (aligned) {
    candidates.push(aligned);
  }

  candidates.push(clamped);
  for (const candidate of candidates) {
    const next = clampTranslation(selectedPolygons, candidate.x, candidate.y);
    if (translationFits(selectedPolygons, otherPolygons, next)) {
      return next;
    }
  }

  let lo = 0;
  let hi = 1;
  let best: MapPoint | null = translationFits(selectedPolygons, otherPolygons, { x: 0, y: 0 }) ? { x: 0, y: 0 } : null;
  for (let step = 0; step < 18; step += 1) {
    const mid = (lo + hi) / 2;
    const trial = clampTranslation(selectedPolygons, clamped.x * mid, clamped.y * mid);
    if (translationFits(selectedPolygons, otherPolygons, trial)) {
      best = trial;
      lo = mid;
    } else {
      hi = mid;
    }
  }

  return best;
}

export function segmentsCross(a1: MapPoint, a2: MapPoint, b1: MapPoint, b2: MapPoint): boolean {
  return segmentsProperlyIntersect(a1, a2, b1, b2);
}

export function snapToNearestEdge(
  cursor: MapPoint,
  polygons: readonly (readonly MapPoint[])[],
  snapDistance = SNAP_DISTANCE,
): MapPoint | null {
  let best: MapPoint | null = null;
  let bestDistance = snapDistance * snapDistance;
  for (const polygon of [...polygons, MAP_FRAME]) {
    const count = polygon.length;
    for (let i = 0; i < count; i += 1) {
      const a = polygon.at(i);
      const b = polygon.at((i + 1) % count);
      if (!a || !b) {
        continue;
      }

      const projected = projectPointOnSegment(a, b, cursor, snapDistance);
      if (!projected) {
        continue;
      }

      const distance = distanceSquared(cursor, projected);
      if (distance <= bestDistance) {
        best = projected;
        bestDistance = distance;
      }
    }
  }

  return best;
}

export function traceSharedBorder(
  start: MapPoint,
  end: MapPoint,
  polygons: readonly (readonly MapPoint[])[],
): MapPoint[] | null {
  if (distanceSquared(start, end) <= GEOMETRY_EPSILON * GEOMETRY_EPSILON) {
    return null;
  }

  let best: MapPoint[] | null = null;
  let bestLength = Number.POSITIVE_INFINITY;
  for (let index = 0; index < polygons.length; index += 1) {
    const polygon = polygons.at(index);
    if (!polygon || !isOnBoundary(polygon, start) || !isOnBoundary(polygon, end)) {
      continue;
    }

    for (const path of boundaryPaths(polygon, start, end)) {
      if (path.length < 2) {
        continue;
      }

      const length = polylineLength(path);
      if (length < MIN_SHARED_BORDER_LENGTH || length >= bestLength) {
        continue;
      }

      if (pathBlockedByOtherPolygons(path, polygons, index)) {
        continue;
      }

      best = path;
      bestLength = length;
    }
  }

  return best;
}

export function encloseAlongTouchedBorders(
  drawn: readonly MapPoint[],
  polygons: readonly (readonly MapPoint[])[],
): MapPoint[] | null {
  const unique = distinctVertices(drawn);
  if (unique.length < 2) {
    return null;
  }

  const start = unique[0];
  const end = unique.at(-1);
  if (!end || distanceSquared(start, end) <= GEOMETRY_EPSILON * GEOMETRY_EPSILON) {
    return null;
  }

  const touched = polygons.filter((polygon) => isOnBoundary(polygon, start) || isOnBoundary(polygon, end));
  if (touched.length === 0) {
    return null;
  }

  const graph = buildUnsharedBoundaryGraph(touched, polygons, start, end);
  const walks = simpleBoundaryPaths(graph, end, start, 12);
  let best: MapPoint[] | null = null;
  let bestArea = Number.POSITIVE_INFINITY;
  for (const walk of walks) {
    if (walk.length < 2) {
      continue;
    }

    const polygon = distinctVertices([...unique, ...walk.slice(1, -1)]);
    if (!isValidTerritoryPolygon(polygon)) {
      continue;
    }

    if (polygons.some((existing) => interiorsOverlap(polygon, existing))) {
      continue;
    }

    if (polygons.some((existing) => territoryContainedBy(existing, polygon))) {
      continue;
    }

    const area = polygonArea(polygon);
    if (area < bestArea) {
      best = polygon;
      bestArea = area;
    }
  }

  return best;
}

export function encloseAlongImageEdge(
  drawn: readonly MapPoint[],
  polygons: readonly (readonly MapPoint[])[],
): MapPoint[] | null {
  const unique = distinctVertices(drawn);
  if (unique.length < 2) {
    return null;
  }

  const rawStart = unique[0];
  const rawEnd = unique.at(-1);
  if (!rawEnd || !isOnImageEdge(rawStart) || !isOnImageEdge(rawEnd)) {
    return null;
  }

  const start = snapToImageEdge(rawStart);
  const end = snapToImageEdge(rawEnd);
  if (distanceSquared(start, end) <= GEOMETRY_EPSILON * GEOMETRY_EPSILON) {
    return null;
  }

  const drawnWithEdges = [start, ...unique.slice(1, -1), end];
  let best: MapPoint[] | null = null;
  let bestArea = Number.POSITIVE_INFINITY;
  for (const walk of boundaryPaths(MAP_FRAME, end, start)) {
    if (walk.length < 2) {
      continue;
    }

    const polygon = distinctVertices([...drawnWithEdges, ...walk.slice(1, -1)]);
    if (!isValidTerritoryPolygon(polygon)) {
      continue;
    }

    if (polygons.some((existing) => interiorsOverlap(polygon, existing))) {
      continue;
    }

    if (polygons.some((existing) => territoryContainedBy(existing, polygon))) {
      continue;
    }

    const area = polygonArea(polygon);
    if (area < bestArea) {
      best = polygon;
      bestArea = area;
    }
  }

  return best;
}

export interface FittedSquare {
  x: number;
  y: number;
  width: number;
  height: number;
}

export interface FitSquareOptions {
  minScale?: number;
  allowOverlapFallback?: boolean;
}

export function rectanglesOverlap(left: FittedSquare, right: FittedSquare): boolean {
  return (
    Math.abs(left.x - right.x) < (left.width + right.width) / 2 &&
    Math.abs(left.y - right.y) < (left.height + right.height) / 2
  );
}

export function fitSquareInPolygon(
  polygon: readonly MapPoint[],
  preferred: MapPoint,
  maxWidth: number,
  maxHeight: number,
  avoid: readonly FittedSquare[] | null = null,
  options?: FitSquareOptions,
): FittedSquare {
  const minScale = Math.min(1, Math.max(options?.minScale ?? 0.2, 0.05));
  const allowOverlapFallback = options?.allowOverlapFallback ?? true;
  const minWidth = Math.max(maxWidth * minScale, 0.004);
  const minHeight = Math.max(maxHeight * minScale, 0.004);
  const searchFrom = containsInclusive(polygon, preferred) ? preferred : interiorAnchor(polygon);
  const avoided = avoid ?? [];
  const fitted = tryFitSquareInPolygon(polygon, searchFrom, maxWidth, maxHeight, avoided, minScale);
  if (fitted) {
    return fitted;
  }

  if (allowOverlapFallback && avoided.length > 0) {
    const overlapping = tryFitSquareInPolygon(polygon, searchFrom, maxWidth, maxHeight, [], minScale);
    if (overlapping) {
      return overlapping;
    }
  }

  return {
    x: searchFrom.x,
    y: searchFrom.y,
    width: minWidth,
    height: minHeight,
  };
}

export function tryFitSquareInPolygon(
  polygon: readonly MapPoint[],
  origin: MapPoint,
  maxWidth: number,
  maxHeight: number,
  avoid: readonly FittedSquare[] | null = null,
  minScale = 0.2,
): FittedSquare | null {
  const clampedScale = Math.min(1, Math.max(minScale, 0.05));
  return searchFittedSquare(
    polygon,
    origin,
    maxWidth,
    maxHeight,
    Math.max(maxWidth * clampedScale, 0.004),
    Math.max(maxHeight * clampedScale, 0.004),
    avoid ?? [],
    clampedScale,
  );
}

function searchFittedSquare(
  polygon: readonly MapPoint[],
  origin: MapPoint,
  maxWidth: number,
  maxHeight: number,
  minWidth: number,
  minHeight: number,
  avoided: readonly FittedSquare[],
  minScale = 0.2,
): FittedSquare | null {
  let best: FittedSquare | null = null;
  let bestScore = Number.NEGATIVE_INFINITY;
  const sizes = [1, 0.85, 0.7, 0.55, 0.5, 0.4, 0.28, 0.2]
    .filter((factor) => factor + 1e-9 >= minScale)
    .sort((left, right) => right - left);
  for (const factor of sizes) {
    const width = Math.max(minWidth, maxWidth * factor);
    const height = Math.max(minHeight, maxHeight * factor);
    for (const offset of markerOffsets()) {
      const candidate: FittedSquare = {
        x: origin.x + offset.x * width,
        y: origin.y + offset.y * height,
        width,
        height,
      };
      if (!squareFitsPolygon(polygon, candidate, avoided)) {
        continue;
      }

      const score = factor * 10 - Math.hypot(candidate.x - origin.x, candidate.y - origin.y);
      if (score > bestScore) {
        best = candidate;
        bestScore = score;
      }
    }

    if (best && factor >= 0.85) {
      return best;
    }
  }

  return best;
}

function territoryContainedBy(inner: readonly MapPoint[], outer: readonly MapPoint[]): boolean {
  if (inner.length === 0) {
    return false;
  }

  if (containsStrict(outer, centroid(inner))) {
    return true;
  }

  return inner.some((vertex) => containsStrict(outer, vertex));
}

function markerOffsets(): MapPoint[] {
  return [
    { x: 0, y: 0 },
    { x: 0.6, y: 0 },
    { x: -0.6, y: 0 },
    { x: 0, y: 0.6 },
    { x: 0, y: -0.6 },
    { x: 0.8, y: 0.35 },
    { x: -0.8, y: 0.35 },
    { x: 0.8, y: -0.35 },
    { x: -0.8, y: -0.35 },
    { x: 0.35, y: 0.8 },
    { x: -0.35, y: 0.8 },
    { x: 0.35, y: -0.8 },
    { x: -0.35, y: -0.8 },
    { x: 1.1, y: 0 },
    { x: -1.1, y: 0 },
    { x: 0, y: 1.1 },
    { x: 0, y: -1.1 },
    { x: 1.4, y: 0 },
    { x: -1.4, y: 0 },
    { x: 0, y: 1.4 },
    { x: 0, y: -1.4 },
    { x: 1.8, y: 0.55 },
    { x: -1.8, y: 0.55 },
    { x: 1.8, y: -0.55 },
    { x: -1.8, y: -0.55 },
    { x: 0.55, y: 1.8 },
    { x: -0.55, y: 1.8 },
    { x: 0.55, y: -1.8 },
    { x: -0.55, y: -1.8 },
  ];
}

function squareFitsPolygon(
  polygon: readonly MapPoint[],
  square: FittedSquare,
  avoid: readonly FittedSquare[],
): boolean {
  if (avoid.some((item) => rectanglesOverlap(square, item))) {
    return false;
  }

  const hw = square.width / 2;
  const hh = square.height / 2;
  const samples: MapPoint[] = [
    { x: square.x, y: square.y },
    { x: square.x - hw, y: square.y - hh },
    { x: square.x + hw, y: square.y - hh },
    { x: square.x + hw, y: square.y + hh },
    { x: square.x - hw, y: square.y + hh },
    { x: square.x, y: square.y - hh },
    { x: square.x, y: square.y + hh },
    { x: square.x - hw, y: square.y },
    { x: square.x + hw, y: square.y },
  ];
  return samples.every((point) => containsInclusive(polygon, point));
}

function containsInclusive(polygon: readonly MapPoint[], point: MapPoint): boolean {
  return containsStrict(polygon, point) || isOnBoundary(polygon, point);
}

function buildUnsharedBoundaryGraph(
  touched: readonly (readonly MapPoint[])[],
  allPolygons: readonly (readonly MapPoint[])[],
  start: MapPoint,
  end: MapPoint,
): Map<string, MapPoint[]> {
  const adj = new Map<string, MapPoint[]>();
  for (let owner = 0; owner < allPolygons.length; owner += 1) {
    const polygon = allPolygons.at(owner);
    if (!polygon || !touched.includes(polygon)) {
      continue;
    }

    const count = polygon.length;
    for (let i = 0; i < count; i += 1) {
      const a = polygon.at(i);
      const b = polygon.at((i + 1) % count);
      if (!a || !b) {
        continue;
      }

      if (pathBlockedByOtherPolygons([a, b], allPolygons, owner)) {
        continue;
      }

      const splits = [a, b];
      if (pointOnSegment(a, b, start)) {
        splits.push(start);
      }

      if (pointOnSegment(a, b, end)) {
        splits.push(end);
      }

      for (const other of touched) {
        for (const vertex of other) {
          if (pointOnSegment(a, b, vertex)) {
            splits.push(vertex);
          }
        }
      }

      const ordered = sortAlongSegment(a, b, splits);
      for (let j = 1; j < ordered.length; j += 1) {
        addUndirectedEdge(adj, ordered[j - 1], ordered[j]);
      }
    }
  }

  return adj;
}

function sortAlongSegment(a: MapPoint, b: MapPoint, points: readonly MapPoint[]): MapPoint[] {
  const dx = b.x - a.x;
  const dy = b.y - a.y;
  const unique: MapPoint[] = [];
  for (const point of points) {
    if (!unique.some((existing) => distanceSquared(existing, point) <= GEOMETRY_EPSILON * GEOMETRY_EPSILON)) {
      unique.push(point);
    }
  }

  return unique.sort((left, right) => {
    const leftT = (left.x - a.x) * dx + (left.y - a.y) * dy;
    const rightT = (right.x - a.x) * dx + (right.y - a.y) * dy;
    return leftT - rightT;
  });
}

function addUndirectedEdge(adj: Map<string, MapPoint[]>, left: MapPoint, right: MapPoint): void {
  if (distanceSquared(left, right) <= GEOMETRY_EPSILON * GEOMETRY_EPSILON) {
    return;
  }

  const leftKey = pointKey(left);
  const rightKey = pointKey(right);
  const leftNeighbors = adj.get(leftKey) ?? [];
  const rightNeighbors = adj.get(rightKey) ?? [];
  if (!leftNeighbors.some((point) => pointKey(point) === rightKey)) {
    leftNeighbors.push(right);
    adj.set(leftKey, leftNeighbors);
  }

  if (!rightNeighbors.some((point) => pointKey(point) === leftKey)) {
    rightNeighbors.push(left);
    adj.set(rightKey, rightNeighbors);
  }
}

function simpleBoundaryPaths(adj: Map<string, MapPoint[]>, from: MapPoint, to: MapPoint, limit: number): MapPoint[][] {
  const results: MapPoint[][] = [];
  const goal = pointKey(to);
  const visited = new Set<string>([pointKey(from)]);
  const path: MapPoint[] = [from];

  const visit = (): void => {
    if (results.length >= limit) {
      return;
    }

    const current = path.at(-1);
    if (!current) {
      return;
    }

    if (path.length > 1 && pointKey(current) === goal) {
      results.push([...path]);
      return;
    }

    if (path.length > 48) {
      return;
    }

    for (const next of adj.get(pointKey(current)) ?? []) {
      const key = pointKey(next);
      if (key === goal && path.length > 1) {
        results.push([...path, next]);
        if (results.length >= limit) {
          return;
        }

        continue;
      }

      if (visited.has(key)) {
        continue;
      }

      visited.add(key);
      path.push(next);
      visit();
      path.pop();
      visited.delete(key);
      if (results.length >= limit) {
        return;
      }
    }
  };

  visit();
  return results;
}

function pointKey(point: MapPoint): string {
  return `${point.x.toFixed(6)},${point.y.toFixed(6)}`;
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
      if (b1 && b2 && segmentsCrossThroughInterior(a1, a2, b1, b2)) {
        return true;
      }
    }
  }

  return false;
}

function hasVertexDeepInside(vertices: readonly MapPoint[], polygon: readonly MapPoint[]): boolean {
  return vertices.some((vertex) => containsDeepInterior(polygon, vertex));
}

function hasSharedInteriorSample(source: readonly MapPoint[], other: readonly MapPoint[]): boolean {
  const center = centroid(source);
  if (isSharedInteriorPoint(source, other, center)) {
    return true;
  }

  const count = source.length;
  for (let i = 0; i < count; i += 1) {
    const vertex = source.at(i);
    const next = source.at((i + 1) % count);
    if (!vertex) {
      continue;
    }

    if (isSharedInteriorPoint(source, other, { x: (vertex.x + center.x) / 2, y: (vertex.y + center.y) / 2 })) {
      return true;
    }

    if (!next) {
      continue;
    }

    const inward = insetToward(
      { x: (vertex.x + next.x) / 2, y: (vertex.y + next.y) / 2 },
      center,
      BORDER_TRACE_TOLERANCE * 2,
    );
    if (inward && isSharedInteriorPoint(source, other, inward)) {
      return true;
    }
  }

  return false;
}

function insetToward(from: MapPoint, toward: MapPoint, distance: number): MapPoint | null {
  const dx = toward.x - from.x;
  const dy = toward.y - from.y;
  const length = Math.hypot(dx, dy);
  if (length < GEOMETRY_EPSILON) {
    return null;
  }

  return { x: from.x + (dx / length) * distance, y: from.y + (dy / length) * distance };
}

function isSharedInteriorPoint(source: readonly MapPoint[], other: readonly MapPoint[], sample: MapPoint): boolean {
  return containsDeepInterior(source, sample) && containsDeepInterior(other, sample);
}

function containsDeepInterior(polygon: readonly MapPoint[], point: MapPoint, margin = BORDER_TRACE_TOLERANCE): boolean {
  if (!containsStrict(polygon, point)) {
    return false;
  }

  return distanceToBoundary(polygon, point) > margin;
}

function distanceToBoundary(polygon: readonly MapPoint[], point: MapPoint): number {
  let best = Number.POSITIVE_INFINITY;
  const count = polygon.length;
  for (let i = 0; i < count; i += 1) {
    const a = polygon.at(i);
    const b = polygon.at((i + 1) % count);
    if (!a || !b) {
      continue;
    }

    const closest = closestPointOnSegment(a, b, point);
    const distance = Math.hypot(point.x - closest.x, point.y - closest.y);
    if (distance < best) {
      best = distance;
    }
  }

  return best;
}

function projectPointOnSegment(a: MapPoint, b: MapPoint, p: MapPoint, snapDistance = SNAP_DISTANCE): MapPoint | null {
  const dx = b.x - a.x;
  const dy = b.y - a.y;
  const lengthSquared = dx * dx + dy * dy;
  if (lengthSquared < GEOMETRY_EPSILON * GEOMETRY_EPSILON) {
    return distanceSquared(a, p) <= snapDistance * snapDistance ? a : null;
  }

  const t = Math.max(0, Math.min(1, ((p.x - a.x) * dx + (p.y - a.y) * dy) / lengthSquared));
  return { x: a.x + dx * t, y: a.y + dy * t };
}

function boundaryPaths(polygon: readonly MapPoint[], start: MapPoint, end: MapPoint): MapPoint[][] {
  const startEdge = edgeIndexContaining(polygon, start);
  const endEdge = edgeIndexContaining(polygon, end);
  if (startEdge < 0 || endEdge < 0) {
    return [];
  }

  if (startEdge === endEdge) {
    return [[start, end], walkBoundary(polygon, start, end, startEdge, endEdge, 1)];
  }

  return [
    walkBoundary(polygon, start, end, startEdge, endEdge, 1),
    walkBoundary(polygon, start, end, startEdge, endEdge, -1),
  ];
}

function edgeIndexContaining(polygon: readonly MapPoint[], point: MapPoint): number {
  const count = polygon.length;
  for (let i = 0; i < count; i += 1) {
    const a = polygon.at(i);
    const b = polygon.at((i + 1) % count);
    if (a && b && pointOnSegment(a, b, point)) {
      return i;
    }
  }

  return -1;
}

function walkBoundary(
  polygon: readonly MapPoint[],
  start: MapPoint,
  end: MapPoint,
  startEdge: number,
  endEdge: number,
  direction: 1 | -1,
): MapPoint[] {
  const count = polygon.length;
  const path: MapPoint[] = [start];
  let edge = startEdge;
  let guard = 0;
  while (guard < count + 2) {
    guard += 1;
    if (edge === endEdge && path.length > 1) {
      path.push(end);
      return path;
    }

    const nextVertexIndex = direction === 1 ? (edge + 1) % count : edge;
    const nextVertex = polygon.at(nextVertexIndex);
    if (nextVertex) {
      path.push(nextVertex);
    }

    edge = (edge + direction + count) % count;
    if (edge === endEdge) {
      path.push(end);
      return path;
    }
  }

  return [];
}

function polylineLength(path: readonly MapPoint[]): number {
  let length = 0;
  for (let i = 1; i < path.length; i += 1) {
    const previous = path.at(i - 1);
    const current = path.at(i);
    if (previous && current) {
      length += Math.sqrt(distanceSquared(previous, current));
    }
  }

  return length;
}

function pathBlockedByOtherPolygons(
  path: readonly MapPoint[],
  polygons: readonly (readonly MapPoint[])[],
  ownerIndex: number,
): boolean {
  for (let index = 0; index < polygons.length; index += 1) {
    if (index === ownerIndex) {
      continue;
    }

    const other = polygons.at(index);
    if (!other) {
      continue;
    }

    for (const vertex of other) {
      if (pointStrictlyOnPolyline(path, vertex)) {
        return true;
      }
    }

    const otherCount = other.length;
    for (let i = 0; i < otherCount; i += 1) {
      const a = other.at(i);
      const b = other.at((i + 1) % otherCount);
      if (!a || !b) {
        continue;
      }

      for (let j = 1; j < path.length; j += 1) {
        const c = path.at(j - 1);
        const d = path.at(j);
        if (!c || !d) {
          continue;
        }

        const overlap = collinearOverlap(c, d, a, b);
        if (overlap && Math.sqrt(distanceSquared(overlap.start, overlap.end)) >= MIN_SHARED_BORDER_LENGTH) {
          return true;
        }
      }
    }
  }

  return false;
}

function pointStrictlyOnPolyline(path: readonly MapPoint[], point: MapPoint): boolean {
  const first = path.at(0);
  const last = path.at(-1);
  if (first && distanceSquared(first, point) <= GEOMETRY_EPSILON * GEOMETRY_EPSILON) {
    return false;
  }

  if (last && distanceSquared(last, point) <= GEOMETRY_EPSILON * GEOMETRY_EPSILON) {
    return false;
  }

  for (let i = 1; i < path.length; i += 1) {
    const a = path.at(i - 1);
    const b = path.at(i);
    if (a && b && pointOnSegment(a, b, point)) {
      return true;
    }
  }

  return false;
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

function segmentsCrossThroughInterior(a1: MapPoint, a2: MapPoint, b1: MapPoint, b2: MapPoint): boolean {
  if (!segmentsProperlyIntersect(a1, a2, b1, b2)) {
    return false;
  }

  const hit = segmentIntersection(a1, a2, b1, b2);
  if (!hit) {
    return false;
  }

  const toleranceSquared = BORDER_TRACE_TOLERANCE * BORDER_TRACE_TOLERANCE;
  if (
    distanceSquared(hit.point, a1) <= toleranceSquared ||
    distanceSquared(hit.point, a2) <= toleranceSquared ||
    distanceSquared(hit.point, b1) <= toleranceSquared ||
    distanceSquared(hit.point, b2) <= toleranceSquared
  ) {
    return false;
  }

  if (
    pointDistanceToSegment(a1, a2, b1) <= BORDER_TRACE_TOLERANCE &&
    pointDistanceToSegment(a1, a2, b2) <= BORDER_TRACE_TOLERANCE
  ) {
    return false;
  }

  return (
    pointDistanceToSegment(b1, b2, a1) > BORDER_TRACE_TOLERANCE ||
    pointDistanceToSegment(b1, b2, a2) > BORDER_TRACE_TOLERANCE
  );
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

function closestPointsOnEdges(polygon: readonly MapPoint[], point: MapPoint): MapPoint[] {
  const points: MapPoint[] = [];
  const count = polygon.length;
  for (let i = 0; i < count; i += 1) {
    const a = polygon.at(i);
    const b = polygon.at((i + 1) % count);
    if (a && b) {
      points.push(closestPointOnSegment(a, b, point));
    }
  }

  return points;
}

function closestPointOnSegment(a: MapPoint, b: MapPoint, point: MapPoint): MapPoint {
  const dx = b.x - a.x;
  const dy = b.y - a.y;
  const lengthSquared = dx * dx + dy * dy;
  if (lengthSquared < GEOMETRY_EPSILON * GEOMETRY_EPSILON) {
    return a;
  }

  const t = Math.max(0, Math.min(1, ((point.x - a.x) * dx + (point.y - a.y) * dy) / lengthSquared));
  return { x: a.x + dx * t, y: a.y + dy * t };
}

function pointDistanceToSegment(a: MapPoint, b: MapPoint, point: MapPoint): number {
  const closest = closestPointOnSegment(a, b, point);
  return Math.hypot(point.x - closest.x, point.y - closest.y);
}

function translationFits(
  selectedPolygons: readonly (readonly MapPoint[])[],
  otherPolygons: readonly (readonly MapPoint[])[],
  delta: MapPoint,
): boolean {
  const moved = selectedPolygons.map((polygon) => translatePolygon(polygon, delta.x, delta.y));
  if (moved.some((polygon) => !isValidTerritoryPolygon(polygon))) {
    return false;
  }

  return !moved.some((polygon) => otherPolygons.some((other) => interiorsOverlap(polygon, other)));
}

function alignTranslationToNeighborEdges(
  selectedPolygons: readonly (readonly MapPoint[])[],
  otherPolygons: readonly (readonly MapPoint[])[],
  delta: MapPoint,
  snapDistance: number,
): MapPoint | null {
  let bestDistance = snapDistance;
  let best: MapPoint | null = null;
  for (const polygon of selectedPolygons) {
    const count = polygon.length;
    for (let i = 0; i < count; i += 1) {
      const a1 = polygon.at(i);
      const a2 = polygon.at((i + 1) % count);
      if (!a1 || !a2) {
        continue;
      }

      const movedA = { x: a1.x + delta.x, y: a1.y + delta.y };
      for (const other of otherPolygons) {
        const otherCount = other.length;
        for (let j = 0; j < otherCount; j += 1) {
          const b1 = other.at(j);
          const b2 = other.at((j + 1) % otherCount);
          if (!b1 || !b2) {
            continue;
          }

          const edgeDx = b2.x - b1.x;
          const edgeDy = b2.y - b1.y;
          const edgeLength = Math.hypot(edgeDx, edgeDy);
          if (edgeLength < GEOMETRY_EPSILON) {
            continue;
          }

          const nx = -edgeDy / edgeLength;
          const ny = edgeDx / edgeLength;
          const gap = (movedA.x - b1.x) * nx + (movedA.y - b1.y) * ny;
          const distance = Math.abs(gap);
          if (distance > bestDistance) {
            continue;
          }

          const aligned = { x: delta.x - nx * gap, y: delta.y - ny * gap };
          const trialA = { x: a1.x + aligned.x, y: a1.y + aligned.y };
          const trialB = { x: a2.x + aligned.x, y: a2.y + aligned.y };
          if (!collinearOverlap(trialA, trialB, b1, b2)) {
            continue;
          }

          bestDistance = distance;
          best = aligned;
        }
      }
    }
  }

  return best;
}
