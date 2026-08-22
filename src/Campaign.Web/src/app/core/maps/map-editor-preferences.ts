export type OverlayColorMode = 'random' | 'terrain' | 'manual';

export const OVERLAY_COLOR_MODE_STORAGE_PREFIX = 'map-editor-color-mode:';

export function readStoredOverlayColorMode(campaignId: string): OverlayColorMode | null {
  try {
    const value = localStorage.getItem(OVERLAY_COLOR_MODE_STORAGE_PREFIX + campaignId);
    if (value === 'random' || value === 'terrain' || value === 'manual') {
      return value;
    }
  } catch {
    return null;
  }

  return null;
}

export function writeStoredOverlayColorMode(campaignId: string, mode: OverlayColorMode): void {
  try {
    localStorage.setItem(OVERLAY_COLOR_MODE_STORAGE_PREFIX + campaignId, mode);
  } catch {
    // Private mode or quota errors should not block the editor.
  }
}
