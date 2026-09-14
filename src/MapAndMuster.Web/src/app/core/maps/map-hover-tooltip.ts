/** Gap from the cursor to the tooltip's near corner. */
export const MAP_HOVER_TOOLTIP_OFFSET_X = 12;
export const MAP_HOVER_TOOLTIP_OFFSET_Y = 16;
/** Minimum space kept between the tooltip and the map viewport edge. */
export const MAP_HOVER_TOOLTIP_MARGIN = 8;

export interface MapHoverTooltipPlacement {
  x: number;
  y: number;
}

/**
 * Places a map hover tooltip relative to the pointer inside the map viewport.
 * Prefers the lower-right of the cursor, then flips on the edges that would overflow:
 * right → lower-left, bottom → upper-right, both → upper-left.
 */
export function placeMapHoverTooltip(
  cursorX: number,
  cursorY: number,
  tooltipWidth: number,
  tooltipHeight: number,
  viewportWidth: number,
  viewportHeight: number,
  offsetX = MAP_HOVER_TOOLTIP_OFFSET_X,
  offsetY = MAP_HOVER_TOOLTIP_OFFSET_Y,
  margin = MAP_HOVER_TOOLTIP_MARGIN,
): MapHoverTooltipPlacement {
  const preferX = cursorX + offsetX;
  const preferY = cursorY + offsetY;
  const hitsRight = preferX + tooltipWidth > viewportWidth - margin;
  const hitsBottom = preferY + tooltipHeight > viewportHeight - margin;
  let x = hitsRight ? cursorX - offsetX - tooltipWidth : preferX;
  let y = hitsBottom ? cursorY - offsetY - tooltipHeight : preferY;
  const maxX = Math.max(margin, viewportWidth - tooltipWidth - margin);
  const maxY = Math.max(margin, viewportHeight - tooltipHeight - margin);
  x = Math.min(Math.max(x, margin), maxX);
  y = Math.min(Math.max(y, margin), maxY);
  return { x, y };
}
