import type { MapTerritory } from './map-graph.models';

const NAME_CHAR_PX = 10;

/** Drawn map label: full name always, or the display number only when it still fits. */
export function overlayNameLabel(territory: MapTerritory, image: { width: number }, scale: number): string | null {
  const name = territory.name?.trim();
  if (name) {
    return name;
  }

  const number = String(territory.displayNumber);
  if (territory.polygon.length === 0) {
    return number;
  }

  const xs = territory.polygon.map((point) => point.x);
  const widthPx = (Math.max(...xs) - Math.min(...xs)) * image.width * scale;
  if (number.length * NAME_CHAR_PX + 8 > widthPx) {
    return null;
  }

  return number;
}
