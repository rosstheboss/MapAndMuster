import { OVERLAY_FILL_OPACITY, STROKE_SCREEN_PX } from './geometry';
import type { MapTerritory } from './map-graph.models';

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
): void {
  ctx.lineJoin = 'round';
  ctx.lineCap = 'round';
  ctx.lineWidth = Math.max(STROKE_SCREEN_PX, width / 1920);
  ctx.strokeStyle = 'rgba(28, 25, 23, 0.75)';

  for (const territory of territories) {
    if (territory.polygon.length < 3) {
      continue;
    }

    ctx.beginPath();
    for (const [index, point] of territory.polygon.entries()) {
      const x = point.x * width;
      const y = point.y * height;
      if (index === 0) {
        ctx.moveTo(x, y);
      } else {
        ctx.lineTo(x, y);
      }
    }

    ctx.closePath();
    if (territory.overlayColor) {
      ctx.fillStyle = colorWithAlpha(territory.overlayColor, OVERLAY_FILL_OPACITY);
      ctx.fill();
    }

    ctx.stroke();
  }
}

export async function rasterizeMapPng(imageUrl: string, territories: readonly MapTerritory[]): Promise<Blob> {
  const image = await loadHtmlImage(imageUrl);
  const canvas = document.createElement('canvas');
  canvas.width = Math.max(image.naturalWidth, 1);
  canvas.height = Math.max(image.naturalHeight, 1);
  const ctx = canvas.getContext('2d');
  if (!ctx) {
    throw new Error('Unable to draw the map image.');
  }

  ctx.drawImage(image, 0, 0);
  drawTerritoryOverlay(ctx, canvas.width, canvas.height, territories);
  return canvasToPngBlob(canvas);
}

export function downloadBlob(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = filename;
  anchor.rel = 'noopener';
  document.body.append(anchor);
  anchor.click();
  anchor.remove();
  URL.revokeObjectURL(url);
}

function loadHtmlImage(url: string): Promise<HTMLImageElement> {
  return new Promise((resolve, reject) => {
    const image = new Image();
    image.onload = () => resolve(image);
    image.onerror = () => reject(new Error('Unable to load the map image.'));
    image.src = url;
  });
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
