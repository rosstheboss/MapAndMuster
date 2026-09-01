import { MAP_VIEW_ZOOM_STORAGE_PREFIX, readStoredMapViewZoom, writeStoredMapViewZoom } from './map-view-preferences';

const campaignId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

describe('map-view-preferences', () => {
  beforeEach(() => {
    localStorage.removeItem(MAP_VIEW_ZOOM_STORAGE_PREFIX + campaignId);
  });

  it('returns null when no zoom is stored', () => {
    expect(readStoredMapViewZoom(campaignId)).toBeNull();
  });

  it('round-trips a stored zoom for a campaign', () => {
    writeStoredMapViewZoom(campaignId, { fit: false, zoom: 2 });
    expect(readStoredMapViewZoom(campaignId)).toEqual({ fit: false, zoom: 2 });
    expect(readStoredMapViewZoom('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb')).toBeNull();
  });

  it('ignores unknown stored values', () => {
    localStorage.setItem(MAP_VIEW_ZOOM_STORAGE_PREFIX + campaignId, '{"zoom":2}');
    expect(readStoredMapViewZoom(campaignId)).toBeNull();
  });
});
