/**
 * Keys into the `assetTags` table the API sends with a campaign, preset, or play snapshot.
 *
 * These must match `CampaignAssetTagMap` on the server. A tag identifies the stored file rather
 * than the campaign revision, so an asset URL built from one stays valid — and cacheable — until
 * the file itself is replaced.
 */
export type CampaignAssetTags = Record<string, string>;

/** Key for the campaign map image. */
export const MAP_ASSET_KEY = 'map';

/** Key for a structure logo. */
export function structureAssetKey(structureTypeId: string, pillaged = false): string {
  return `${pillaged ? 'structure-pillaged' : 'structure'}:${structureTypeId}`;
}

/** Key for an item-objective logo. */
export function itemAssetKey(itemObjectiveTypeId: string): string {
  return `item:${itemObjectiveTypeId}`;
}

/** Key for a force-status chit or token image. */
export function forceStatusTokenAssetKey(forceStatusId: string): string {
  return `force-status-token:${forceStatusId}`;
}

/** Key for a faction or subfaction flag. */
export function factionAssetKey(factionId: string, subfaction?: string | null): string {
  const trimmed = subfaction?.trim();
  return trimmed ? `faction:${factionId}:${trimmed.toLowerCase()}` : `faction:${factionId}`;
}

/**
 * Builds the cache-busting query for an asset URL.
 *
 * Returns an empty string when no tag is known, which leaves the URL unversioned rather than
 * pinning it to a wrong tag.
 */
export function assetTagQuery(tags: CampaignAssetTags | undefined, key: string): string {
  const tag = tags?.[key];
  return tag ? `?t=${encodeURIComponent(tag)}` : '';
}
