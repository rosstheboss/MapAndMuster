import {
  OVERLAY_COLOR_MODE_STORAGE_PREFIX,
  readStoredOverlayColorMode,
  writeStoredOverlayColorMode,
} from './map-editor-preferences';

const campaignId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

describe('map-editor-preferences', () => {
  beforeEach(() => {
    localStorage.removeItem(OVERLAY_COLOR_MODE_STORAGE_PREFIX + campaignId);
  });

  it('returns null when no overlay color mode is stored', () => {
    expect(readStoredOverlayColorMode(campaignId)).toBeNull();
  });

  it('round-trips a stored overlay color mode for a campaign', () => {
    writeStoredOverlayColorMode(campaignId, 'terrain');
    expect(readStoredOverlayColorMode(campaignId)).toBe('terrain');
    expect(readStoredOverlayColorMode('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb')).toBeNull();
  });

  it('ignores unknown stored values', () => {
    localStorage.setItem(OVERLAY_COLOR_MODE_STORAGE_PREFIX + campaignId, 'faction');
    expect(readStoredOverlayColorMode(campaignId)).toBeNull();
  });
});
