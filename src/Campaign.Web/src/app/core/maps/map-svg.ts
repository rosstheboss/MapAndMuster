import { interiorsOverlap, isValidTerritoryPolygon, type MapPoint } from './geometry';
import { createId, nextDisplayNumber, type MapAdjacency, type MapGraph, type MapTerritory } from './map-graph.models';

export function svgDownloadFilename(campaignName: string): string {
  const slug =
    campaignName
      .trim()
      .replace(/[^\w]+/g, '-')
      .replace(/^-+|-+$/g, '')
      .toLowerCase() || 'campaign';
  return `${slug}-overlay.svg`;
}

export function serializeMapSvg(graph: MapGraph): string {
  const territories = graph.territories
    .map((territory) => {
      const points = territory.polygon.map((point) => `${formatCoord(point.x)},${formatCoord(point.y)}`).join(' ');
      return `    <polygon${attr('data-territory-id', territory.id)}${attr(
        'data-display-number',
        String(territory.displayNumber),
      )}${attr('data-name', territory.name)}${attr('data-description', territory.description)}${attr(
        'data-terrain-type-id',
        territory.terrainTypeId,
      )}${attr('data-structure-type-id', territory.structureTypeId)}${attr(
        'data-overlay-color',
        territory.overlayColor,
      )}${attr('data-owner-faction-id', territory.ownerFactionId)}${attr(
        'data-spawn-faction-id',
        territory.spawnFactionId,
      )} points="${points}" fill="${escapeAttr(territory.overlayColor ?? '#000000')}" fill-opacity="${
        territory.overlayColor ? '0.32' : '0'
      }" stroke="#1c1917" stroke-opacity="0.75" stroke-width="0.002" />`;
    })
    .join('\n');
  const adjacencies = graph.adjacencies
    .map((edge) => {
      const left = graph.territories.find((territory) => territory.id === edge.territoryAId);
      const right = graph.territories.find((territory) => territory.id === edge.territoryBId);
      const start = left?.polygon[0] ?? { x: edge.marker.x, y: edge.marker.y };
      const end = right?.polygon[0] ?? start;
      return `    <line${attr('data-adjacency-id', edge.id)}${attr('data-territory-a-id', edge.territoryAId)}${attr(
        'data-territory-b-id',
        edge.territoryBId,
      )}${attr('data-origin', edge.origin)}${attr('data-marker-x', formatCoord(edge.marker.x))}${attr(
        'data-marker-y',
        formatCoord(edge.marker.y),
      )} x1="${formatCoord(start.x)}" y1="${formatCoord(start.y)}" x2="${formatCoord(end.x)}" y2="${formatCoord(
        end.y,
      )}" />`;
    })
    .join('\n');

  return `<?xml version="1.0" encoding="UTF-8"?>
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1 1">
  <g data-map-overlay="territories">
${territories}
  </g>
  <g data-map-overlay="adjacencies">
${adjacencies}
  </g>
</svg>
`;
}

export function parseMapSvg(
  svgText: string,
  options: { defaultTerrainTypeId: string },
): { graph: MapGraph; errors: string[] } {
  const errors: string[] = [];
  const document = new DOMParser().parseFromString(svgText, 'image/svg+xml');
  if (document.querySelector('parsererror')) {
    return { graph: { territories: [], adjacencies: [] }, errors: ['The SVG file could not be parsed.'] };
  }

  const svg = document.querySelector('svg');
  if (!svg) {
    return { graph: { territories: [], adjacencies: [] }, errors: ['The file does not contain SVG overlay data.'] };
  }

  const viewBox = readViewBox(svg);
  const nativePolygons = [...svg.querySelectorAll('polygon[data-territory-id]')];
  if (nativePolygons.length > 0) {
    return parseNativeOverlay(svg, viewBox, errors);
  }

  return parseGenericShapes(svg, viewBox, options.defaultTerrainTypeId, errors);
}

function parseNativeOverlay(
  svg: SVGSVGElement,
  viewBox: ViewBox,
  errors: string[],
): { graph: MapGraph; errors: string[] } {
  const territories: MapTerritory[] = [];
  for (const polygon of svg.querySelectorAll('polygon[data-territory-id]')) {
    const id = polygon.getAttribute('data-territory-id')?.trim();
    const points = normalizePoints(readPoints(polygon.getAttribute('points')), viewBox);
    if (!id || !isValidTerritoryPolygon(points)) {
      errors.push('An exported territory could not be restored.');
      continue;
    }

    territories.push({
      id,
      displayNumber:
        Number.parseInt(polygon.getAttribute('data-display-number') ?? '0', 10) || nextDisplayNumber(territories),
      name: emptyToNull(polygon.getAttribute('data-name')),
      description: emptyToNull(polygon.getAttribute('data-description')),
      polygon: points,
      terrainTypeId: polygon.getAttribute('data-terrain-type-id') ?? '',
      structureTypeId: emptyToNull(polygon.getAttribute('data-structure-type-id')),
      overlayColor: emptyToNull(polygon.getAttribute('data-overlay-color')),
      ownerFactionId: emptyToNull(polygon.getAttribute('data-owner-faction-id')),
      spawnFactionId: emptyToNull(polygon.getAttribute('data-spawn-faction-id')),
    });
  }

  const adjacencies: MapAdjacency[] = [];
  for (const line of svg.querySelectorAll('line[data-adjacency-id]')) {
    const id = line.getAttribute('data-adjacency-id')?.trim();
    const territoryAId = line.getAttribute('data-territory-a-id')?.trim();
    const territoryBId = line.getAttribute('data-territory-b-id')?.trim();
    if (!id || !territoryAId || !territoryBId) {
      continue;
    }

    const markerX = Number.parseFloat(line.getAttribute('data-marker-x') ?? '0.5');
    const markerY = Number.parseFloat(line.getAttribute('data-marker-y') ?? '0.5');
    adjacencies.push({
      id,
      territoryAId,
      territoryBId,
      origin: line.getAttribute('data-origin') === 'Manual' ? 'Manual' : 'Generated',
      marker: { x: Number.isFinite(markerX) ? markerX : 0.5, y: Number.isFinite(markerY) ? markerY : 0.5 },
    });
  }

  if (territories.length === 0) {
    errors.push('The SVG file did not contain any valid territories.');
  }

  return { graph: { territories, adjacencies }, errors };
}

function parseGenericShapes(
  svg: SVGSVGElement,
  viewBox: ViewBox,
  defaultTerrainTypeId: string,
  errors: string[],
): { graph: MapGraph; errors: string[] } {
  const territories: MapTerritory[] = [];
  const shapes = [...svg.querySelectorAll('polygon, polyline, rect, path')];
  for (const shape of shapes) {
    const raw = shapePoints(shape);
    const polygon = normalizePoints(raw, viewBox);
    if (!isValidTerritoryPolygon(polygon)) {
      continue;
    }

    if (territories.some((existing) => interiorsOverlap(existing.polygon, polygon))) {
      continue;
    }

    territories.push({
      id: createId(),
      displayNumber: nextDisplayNumber(territories),
      name: emptyToNull(shape.getAttribute('id') ?? shape.getAttribute('data-name')),
      description: null,
      polygon,
      terrainTypeId: defaultTerrainTypeId,
      structureTypeId: null,
      overlayColor: null,
      ownerFactionId: null,
      spawnFactionId: null,
    });
  }

  if (territories.length === 0) {
    errors.push('The SVG file did not contain any valid territory shapes.');
  }

  return { graph: { territories, adjacencies: [] }, errors };
}

function shapePoints(shape: Element): MapPoint[] {
  if (shape.tagName.toLowerCase() === 'rect') {
    const x = Number.parseFloat(shape.getAttribute('x') ?? '0');
    const y = Number.parseFloat(shape.getAttribute('y') ?? '0');
    const width = Number.parseFloat(shape.getAttribute('width') ?? '0');
    const height = Number.parseFloat(shape.getAttribute('height') ?? '0');
    if (![x, y, width, height].every(Number.isFinite) || width <= 0 || height <= 0) {
      return [];
    }

    return [
      { x, y },
      { x: x + width, y },
      { x: x + width, y: y + height },
      { x, y: y + height },
    ];
  }

  if (shape.tagName.toLowerCase() === 'path') {
    return pathToPolygon(shape as SVGPathElement);
  }

  return readPoints(shape.getAttribute('points'));
}

function pathToPolygon(path: SVGPathElement): MapPoint[] {
  try {
    const length = path.getTotalLength();
    if (!Number.isFinite(length) || length <= 0) {
      return parsePathCommands(path.getAttribute('d') ?? '');
    }

    const steps = Math.max(8, Math.min(48, Math.round(length * 24)));
    const points: MapPoint[] = [];
    for (let index = 0; index < steps; index += 1) {
      const point = path.getPointAtLength((length * index) / steps);
      points.push({ x: point.x, y: point.y });
    }

    return points;
  } catch {
    return parsePathCommands(path.getAttribute('d') ?? '');
  }
}

function parsePathCommands(data: string): MapPoint[] {
  const tokens = data.match(/[MmLlHhVvZz]|-?\d*\.?\d+(?:e[-+]?\d+)?/g) ?? [];
  const points: MapPoint[] = [];
  let command = 'L';
  let current: MapPoint = { x: 0, y: 0 };
  let start: MapPoint = { x: 0, y: 0 };
  let index = 0;
  const nextNumber = (): number | null => {
    if (index >= tokens.length) {
      return null;
    }

    const token = tokens[index];
    if (/[A-Za-z]/.test(token)) {
      return null;
    }

    index += 1;
    const value = Number.parseFloat(token);
    return Number.isFinite(value) ? value : null;
  };

  while (index < tokens.length) {
    const token = tokens[index];
    if (/[A-Za-z]/.test(token)) {
      command = token;
      index += 1;
      if (command === 'Z' || command === 'z') {
        current = { ...start };
        continue;
      }
    }

    if (command === 'M' || command === 'm' || command === 'L' || command === 'l') {
      const x = nextNumber();
      const y = nextNumber();
      if (x === null || y === null) {
        break;
      }

      current = command === 'm' || command === 'l' ? { x: current.x + x, y: current.y + y } : { x, y };
      if (command === 'M' || command === 'm') {
        start = current;
        command = command === 'M' ? 'L' : 'l';
      }

      points.push(current);
      continue;
    }

    if (command === 'H' || command === 'h') {
      const x = nextNumber();
      if (x === null) {
        break;
      }

      current = { x: command === 'h' ? current.x + x : x, y: current.y };
      points.push(current);
      continue;
    }

    if (command === 'V' || command === 'v') {
      const y = nextNumber();
      if (y === null) {
        break;
      }

      current = { x: current.x, y: command === 'v' ? current.y + y : y };
      points.push(current);
      continue;
    }

    nextNumber();
  }

  return points;
}

interface ViewBox {
  x: number;
  y: number;
  width: number;
  height: number;
}

function readViewBox(svg: SVGSVGElement): ViewBox {
  const raw = svg
    .getAttribute('viewBox')
    ?.trim()
    .split(/[\s,]+/)
    .map((part) => Number.parseFloat(part));
  if (raw?.length === 4 && raw.every((value) => Number.isFinite(value)) && raw[2] > 0 && raw[3] > 0) {
    return { x: raw[0], y: raw[1], width: raw[2], height: raw[3] };
  }

  const width = Number.parseFloat(svg.getAttribute('width') ?? '1');
  const height = Number.parseFloat(svg.getAttribute('height') ?? '1');
  return {
    x: 0,
    y: 0,
    width: Number.isFinite(width) && width > 0 ? width : 1,
    height: Number.isFinite(height) && height > 0 ? height : 1,
  };
}

function readPoints(value: string | null): MapPoint[] {
  if (!value) {
    return [];
  }

  const numbers = value
    .trim()
    .split(/[\s,]+/)
    .map((part) => Number.parseFloat(part));
  const points: MapPoint[] = [];
  for (let index = 0; index + 1 < numbers.length; index += 2) {
    const x = numbers[index];
    const y = numbers[index + 1];
    if (Number.isFinite(x) && Number.isFinite(y)) {
      points.push({ x, y });
    }
  }

  return points;
}

function normalizePoints(points: readonly MapPoint[], viewBox: ViewBox): MapPoint[] {
  return points.map((point) => ({
    x: (point.x - viewBox.x) / viewBox.width,
    y: (point.y - viewBox.y) / viewBox.height,
  }));
}

function attr(name: string, value: string | null | undefined): string {
  if (value === null || value === undefined || value === '') {
    return '';
  }

  return ` ${name}="${escapeAttr(value)}"`;
}

function escapeAttr(value: string): string {
  return value.replaceAll('&', '&amp;').replaceAll('"', '&quot;').replaceAll('<', '&lt;').replaceAll('>', '&gt;');
}

function emptyToNull(value: string | null | undefined): string | null {
  const trimmed = value?.trim();
  if (!trimmed) {
    return null;
  }

  return trimmed;
}

function formatCoord(value: number): string {
  return value.toFixed(6).replace(/\.?0+$/, '');
}
