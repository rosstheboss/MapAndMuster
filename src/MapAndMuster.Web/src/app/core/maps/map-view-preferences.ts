export const MAP_VIEW_ZOOM_STORAGE_PREFIX = 'map-view-zoom:';

export interface MapViewZoom {
  fit: boolean;
  zoom: number;
}

export function readStoredMapViewZoom(campaignId: string): MapViewZoom | null {
  try {
    const raw = localStorage.getItem(MAP_VIEW_ZOOM_STORAGE_PREFIX + campaignId);
    if (!raw) {
      return null;
    }

    const parsed = JSON.parse(raw) as Partial<MapViewZoom>;
    if (typeof parsed.fit !== 'boolean') {
      return null;
    }

    const zoom = typeof parsed.zoom === 'number' && Number.isFinite(parsed.zoom) ? parsed.zoom : 1;
    return { fit: parsed.fit, zoom };
  } catch {
    return null;
  }
}

export function writeStoredMapViewZoom(campaignId: string, view: MapViewZoom): void {
  try {
    localStorage.setItem(MAP_VIEW_ZOOM_STORAGE_PREFIX + campaignId, JSON.stringify(view));
  } catch {
    // Private mode or quota errors should not block the map.
  }
}
