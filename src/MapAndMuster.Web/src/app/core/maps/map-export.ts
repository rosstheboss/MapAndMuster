import type { CampaignFaction, CampaignStructureType } from '../campaigns/campaign.models';
import { fitSquareInPolygon, interiorAnchor, MARKER_MAX_PX, OVERLAY_FILL_OPACITY, STROKE_SCREEN_PX } from './geometry';
import type { FittedSquare, MapPoint } from './geometry';
import type { MapTerritory } from './map-graph.models';

const SPAWN_STRIPE_SCREEN_PX = 5;
const STRUCTURE_ICON_COLOR = '#1c1917';
const PILLAGED_ICON_COLOR = '#7c2d12';
const PILLAGED_PIN_FILL = 'rgba(254, 243, 226, 0.94)';
const FLAG_IMAGE_FILL = 'rgba(255, 255, 255, 0.92)';
const DEFAULT_SPAWN_STRIPE = '#78716c';
const MARKER_CONTENT_SCALE = 0.8;
const FLAG_MAX_PX = MARKER_MAX_PX * 2;
const STRUCTURE_MAX_PX = MARKER_MAX_PX * 3;

export interface MapExportDecorations {
  factions: readonly CampaignFaction[];
  structures: readonly CampaignStructureType[];
  structureImageUrl?: (structureTypeId: string, pillaged?: boolean) => string | null;
  flagImageUrl?: (factionId: string) => string | null;
}

export type MapExportImageLoader = (url: string) => Promise<CanvasImageSource | null>;

export function colorWithAlpha(color: string, alpha: number): string {
  const hex = color.trim().replace('#', '');
  if (hex.length === 6 && /^[0-9a-fA-F]{6}$/.test(hex)) {
    const red = Number.parseInt(hex.slice(0, 2), 16);
    const green = Number.parseInt(hex.slice(2, 4), 16);
    const blue = Number.parseInt(hex.slice(4, 6), 16);
    return `rgba(${red}, ${green}, ${blue}, ${alpha})`;
  }

  return color;
}

export function mapDownloadFilename(campaignName: string): string {
  const slug =
    campaignName
      .trim()
      .replace(/[^\w]+/g, '-')
      .replace(/^-+|-+$/g, '')
      .toLowerCase() || 'campaign';
  return `${slug}-map.png`;
}

export function drawTerritoryOverlay(
  ctx: CanvasRenderingContext2D,
  width: number,
  height: number,
  territories: readonly MapTerritory[],
  decorations?: MapExportDecorations,
): void {
  ctx.lineJoin = 'round';
  ctx.lineCap = 'round';
  ctx.lineWidth = Math.max(STROKE_SCREEN_PX, width / 1920);
  ctx.strokeStyle = 'rgba(28, 25, 23, 0.75)';
  const factions = decorations?.factions ?? [];

  for (const territory of territories) {
    if (territory.polygon.length < 3) {
      continue;
    }

    tracePolygon(ctx, territory.polygon, width, height);
    if (territory.spawnFactionId) {
      const spawnFaction = factions.find((faction) => faction.id === territory.spawnFactionId) ?? null;
      const stripeColor = territory.overlayColor ?? spawnFaction?.color ?? DEFAULT_SPAWN_STRIPE;
      ctx.save();
      ctx.clip();
      fillSpawnStripes(ctx, width, height, stripeColor);
      ctx.restore();
    } else if (territory.overlayColor) {
      ctx.fillStyle = colorWithAlpha(territory.overlayColor, OVERLAY_FILL_OPACITY);
      ctx.fill();
    }

    ctx.stroke();
  }
}

export async function drawMapDecorations(
  ctx: CanvasRenderingContext2D,
  width: number,
  height: number,
  territories: readonly MapTerritory[],
  decorations: MapExportDecorations,
  loadImage: MapExportImageLoader = loadOptionalImage,
): Promise<void> {
  const flagMaxWidth = FLAG_MAX_PX / Math.max(width, 1);
  const flagMaxHeight = FLAG_MAX_PX / Math.max(height, 1);
  const structureMaxWidth = STRUCTURE_MAX_PX / Math.max(width, 1);
  const structureMaxHeight = STRUCTURE_MAX_PX / Math.max(height, 1);
  const images = new Map<string, CanvasImageSource>();
  const urls = collectDecorationImageUrls(territories, decorations);
  await Promise.all(
    urls.map(async (url) => {
      const image = await loadImage(url);
      if (image) {
        images.set(url, image);
      }
    }),
  );

  for (const territory of territories) {
    if (territory.polygon.length < 3) {
      continue;
    }

    const center = interiorAnchor(territory.polygon);
    const structure = decorations.structures.find((item) => item.id === territory.structureTypeId) ?? null;
    const destroyed = territory.structureCondition === 'Destroyed';
    const pillaged = territory.structureCondition === 'Pillaged';
    const owner = decorations.factions.find((faction) => faction.id === territory.ownerFactionId) ?? null;
    const structureFit =
      structure && !destroyed
        ? fitSquareInPolygon(territory.polygon, center, structureMaxWidth, structureMaxHeight)
        : null;
    const flagPreferred = structureFit ? { x: structureFit.x + structureFit.width * 0.7, y: structureFit.y } : center;
    const flagFit = owner
      ? fitSquareInPolygon(
          territory.polygon,
          flagPreferred,
          flagMaxWidth,
          flagMaxHeight,
          structureFit ? [structureFit] : null,
        )
      : null;

    if (owner && flagFit) {
      const flagUrl = owner.hasFlagImage ? (decorations.flagImageUrl?.(owner.id) ?? null) : null;
      const flagImage = flagUrl ? (images.get(flagUrl) ?? null) : null;
      drawFactionFlag(ctx, width, height, flagFit, owner.color, flagImage);
    }

    if (structure && structureFit) {
      const imageUrl = structureImageSourceUrl(structure, pillaged, decorations);
      const customImage = imageUrl ? (images.get(imageUrl) ?? null) : null;
      const symbolUrl =
        !customImage && structure.builtinSymbol ? structureSymbolDataUrl(structure.builtinSymbol, pillaged) : null;
      const symbolImage = symbolUrl ? (images.get(symbolUrl) ?? null) : null;
      drawStructurePin(ctx, width, height, structureFit, pillaged, customImage ?? symbolImage);
    }
  }
}

export async function rasterizeMapPng(
  imageUrl: string,
  territories: readonly MapTerritory[],
  decorations?: MapExportDecorations,
): Promise<Blob> {
  const image = await loadHtmlImage(imageUrl);
  const canvas = document.createElement('canvas');
  canvas.width = Math.max(image.naturalWidth, 1);
  canvas.height = Math.max(image.naturalHeight, 1);
  const ctx = canvas.getContext('2d');
  if (!ctx) {
    throw new Error('Unable to draw the map image.');
  }

  ctx.drawImage(image, 0, 0);
  drawTerritoryOverlay(ctx, canvas.width, canvas.height, territories, decorations);
  if (decorations) {
    await drawMapDecorations(ctx, canvas.width, canvas.height, territories, decorations);
  }

  return canvasToPngBlob(canvas);
}

export function downloadBlob(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob);
  try {
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = filename;
    anchor.rel = 'noopener';
    document.body.append(anchor);
    anchor.click();
    anchor.remove();
  } finally {
    URL.revokeObjectURL(url);
  }
}

export function structureSymbolDataUrl(name: string, pillaged: boolean): string {
  const color = pillaged ? PILLAGED_ICON_COLOR : STRUCTURE_ICON_COLOR;
  const paths = structureSymbolPaths(name, pillaged)
    .map((d) => `<path d="${d}" />`)
    .join('');
  const svg =
    `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="${color}" ` +
    `stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round">${paths}</svg>`;
  return `data:image/svg+xml;charset=utf-8,${encodeURIComponent(svg)}`;
}

function collectDecorationImageUrls(territories: readonly MapTerritory[], decorations: MapExportDecorations): string[] {
  const urls = new Set<string>();
  for (const territory of territories) {
    const owner = decorations.factions.find((faction) => faction.id === territory.ownerFactionId) ?? null;
    if (owner?.hasFlagImage) {
      const flagUrl = decorations.flagImageUrl?.(owner.id) ?? null;
      if (flagUrl) {
        urls.add(flagUrl);
      }
    }

    const structure = decorations.structures.find((item) => item.id === territory.structureTypeId) ?? null;
    if (!structure || territory.structureCondition === 'Destroyed') {
      continue;
    }

    const pillaged = territory.structureCondition === 'Pillaged';
    const imageUrl = structureImageSourceUrl(structure, pillaged, decorations);
    if (imageUrl) {
      urls.add(imageUrl);
      continue;
    }

    if (structure.builtinSymbol) {
      urls.add(structureSymbolDataUrl(structure.builtinSymbol, pillaged));
    }
  }

  return [...urls];
}

function structureImageSourceUrl(
  structure: CampaignStructureType,
  pillaged: boolean,
  decorations: MapExportDecorations,
): string | null {
  if (pillaged) {
    return structure.hasPillagedImage ? (decorations.structureImageUrl?.(structure.id, true) ?? null) : null;
  }

  return structure.hasImage ? (decorations.structureImageUrl?.(structure.id, false) ?? null) : null;
}

function drawFactionFlag(
  ctx: CanvasRenderingContext2D,
  width: number,
  height: number,
  fit: FittedSquare,
  color: string,
  image: CanvasImageSource | null,
): void {
  const box = markerBox(fit, width, height);
  if (image) {
    ctx.save();
    roundedRect(ctx, box.left, box.top, box.width, box.height, 2.4);
    ctx.fillStyle = FLAG_IMAGE_FILL;
    ctx.fill();
    ctx.clip();
    drawContainedImage(ctx, image, box.left, box.top, box.width, box.height, MARKER_CONTENT_SCALE);
    ctx.restore();
    return;
  }

  ctx.save();
  ctx.beginPath();
  ctx.moveTo(box.left, box.top);
  ctx.lineTo(box.left + box.width, box.top + box.height * 0.2);
  ctx.lineTo(box.left + box.width * 0.7, box.top + box.height * 0.5);
  ctx.lineTo(box.left + box.width, box.top + box.height * 0.8);
  ctx.lineTo(box.left, box.top + box.height);
  ctx.closePath();
  ctx.fillStyle = color;
  ctx.fill();
  ctx.restore();
}

function drawStructurePin(
  ctx: CanvasRenderingContext2D,
  width: number,
  height: number,
  fit: FittedSquare,
  pillaged: boolean,
  image: CanvasImageSource | null,
): void {
  const box = markerBox(fit, width, height);
  if (pillaged) {
    ctx.save();
    ctx.fillStyle = PILLAGED_PIN_FILL;
    ctx.fillRect(box.left, box.top, box.width, box.height);
    ctx.restore();
  }

  if (!image) {
    return;
  }

  drawContainedImage(ctx, image, box.left, box.top, box.width, box.height, MARKER_CONTENT_SCALE);
}

function markerBox(
  fit: FittedSquare,
  width: number,
  height: number,
): { left: number; top: number; width: number; height: number } {
  const pinWidth = fit.width * width;
  const pinHeight = fit.height * height;
  return {
    left: fit.x * width - pinWidth / 2,
    top: fit.y * height - pinHeight / 2,
    width: pinWidth,
    height: pinHeight,
  };
}

function drawContainedImage(
  ctx: CanvasRenderingContext2D,
  image: CanvasImageSource,
  left: number,
  top: number,
  width: number,
  height: number,
  scale: number,
): void {
  const innerWidth = width * scale;
  const innerHeight = height * scale;
  const innerLeft = left + (width - innerWidth) / 2;
  const innerTop = top + (height - innerHeight) / 2;
  const size = sourceSize(image);
  if (size.width <= 0 || size.height <= 0) {
    ctx.drawImage(image, innerLeft, innerTop, innerWidth, innerHeight);
    return;
  }

  const fit = Math.min(innerWidth / size.width, innerHeight / size.height);
  const drawWidth = size.width * fit;
  const drawHeight = size.height * fit;
  ctx.drawImage(
    image,
    innerLeft + (innerWidth - drawWidth) / 2,
    innerTop + (innerHeight - drawHeight) / 2,
    drawWidth,
    drawHeight,
  );
}

function sourceSize(image: CanvasImageSource): { width: number; height: number } {
  if ('naturalWidth' in image && typeof image.naturalWidth === 'number' && image.naturalWidth > 0) {
    return { width: image.naturalWidth, height: image.naturalHeight };
  }

  if ('width' in image && typeof image.width === 'number') {
    return { width: image.width, height: typeof image.height === 'number' ? image.height : image.width };
  }

  return { width: 0, height: 0 };
}

function roundedRect(
  ctx: CanvasRenderingContext2D,
  x: number,
  y: number,
  width: number,
  height: number,
  radius: number,
): void {
  ctx.beginPath();
  if (typeof ctx.roundRect === 'function') {
    ctx.roundRect(x, y, width, height, radius);
    return;
  }

  ctx.rect(x, y, width, height);
}

function fillSpawnStripes(ctx: CanvasRenderingContext2D, width: number, height: number, color: string): void {
  const stripe = SPAWN_STRIPE_SCREEN_PX / Math.max(width, 1);
  const period = stripe * 2;
  ctx.save();
  ctx.scale(width, height);
  ctx.rotate(Math.PI / 4);
  ctx.fillStyle = colorWithAlpha(color, OVERLAY_FILL_OPACITY);
  const extent = 4;
  for (let x = -extent; x < extent; x += period) {
    ctx.fillRect(x, -extent, stripe, extent * 2);
  }

  ctx.restore();
}

function tracePolygon(
  ctx: CanvasRenderingContext2D,
  polygon: readonly MapPoint[],
  width: number,
  height: number,
): void {
  ctx.beginPath();
  for (const [index, point] of polygon.entries()) {
    const x = point.x * width;
    const y = point.y * height;
    if (index === 0) {
      ctx.moveTo(x, y);
    } else {
      ctx.lineTo(x, y);
    }
  }

  ctx.closePath();
}

function loadHtmlImage(url: string): Promise<HTMLImageElement> {
  return new Promise((resolve, reject) => {
    const image = new Image();
    image.onload = () => resolve(image);
    image.onerror = () => reject(new Error('Unable to load the map image.'));
    image.src = url;
  });
}

async function loadOptionalImage(url: string): Promise<CanvasImageSource | null> {
  try {
    return await loadHtmlImage(url);
  } catch {
    return null;
  }
}

function canvasToPngBlob(canvas: HTMLCanvasElement): Promise<Blob> {
  return new Promise((resolve, reject) => {
    canvas.toBlob((blob) => {
      if (blob) {
        resolve(blob);
        return;
      }

      reject(new Error('Unable to create the map PNG.'));
    }, 'image/png');
  });
}

function structureSymbolPaths(name: string, pillaged: boolean): readonly string[] {
  if (pillaged) {
    switch (name) {
      case 'Town':
        return [
          'M4 20V12l4-2 2 1v9H4z',
          'M12 20v-7l3-2 5 3v6h-8z',
          'M7 20v-3h2M15 20v-2h2',
          'M6 9l3-4 2 3',
          'M4 7l16 12',
        ];
      case 'City':
        return ['M3 20V10h5v10H3z', 'M8 20V7h5l3 4v9H8z', 'M16 20v-6h5v6h-5z', 'M10 11h2M18 16h1', 'M5 6l14 13'];
      case 'CapitalCity':
        return [
          'M4 20V11l4-2 4 2v9H4z',
          'M12 20V9l3-2 5 3v10h-8z',
          'M11 5l1.2 2.2h2.4L13 8.6l.7 2.2L11 9.5 8.9 10.8l.7-2.2-1.6-1.4h2.4L11 5z',
          'M5 8l14 11',
        ];
      case 'Fortification':
        return ['M4 20V11l2-2h2v2h2V9h3l5 4v7H4z', 'M4 11h10', 'M6 7l13 12'];
      case 'Castle':
        return ['M4 20V12l2-2h2v2h2V8h3l5 4v8H4z', 'M10 20v-4h3v4', 'M5 8l14 11'];
      case 'SupplyDepot':
        return ['M5 11h14l-1 9H7L5 11z', 'M9 11V8h6', 'M8 15h5M8 18h3', 'M4 7l16 12'];
      default:
        return [];
    }
  }

  switch (name) {
    case 'Town':
      return ['M4 20V11l4-3 4 3v9H4z', 'M12 20V12l4-3 4 3v8h-8z', 'M7 20v-4h2v4M15 20v-3h2v3'];
    case 'City':
      return ['M3 20V8h5v12H3z', 'M8 20V4h8v16H8z', 'M16 20v-9h5v9h-5z', 'M10 8h2M10 12h2M18 14h1'];
    case 'CapitalCity':
      return [
        'M4 20V9l4-3 4 3v11H4z',
        'M12 20V7l4-3 4 3v13h-8z',
        'M12 4l1.2 2.4H16l-2 1.6.8 2.5L12 9.3 9.2 10.5l.8-2.5-2-1.6h2.8L12 4z',
      ];
    case 'Fortification':
      return ['M4 20V9l2-2h2v2h2V7h4v2h2V7h2l2 2v11H4z', 'M4 9h16'];
    case 'Castle':
      return ['M4 20V10l2-2h2v2h2V6h4v4h2V8h2l2 2v10H4z', 'M10 20v-5h4v5'];
    case 'SupplyDepot':
      return ['M5 10h14l-1 10H6L5 10z', 'M8 10V7h8v3', 'M9 14h6M9 17h4'];
    default:
      return [];
  }
}
