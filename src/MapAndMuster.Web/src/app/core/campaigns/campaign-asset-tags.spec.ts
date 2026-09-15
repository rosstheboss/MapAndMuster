import { describe, expect, it } from 'vitest';

import {
  MAP_ASSET_KEY,
  assetTagQuery,
  factionAssetKey,
  forceStatusTokenAssetKey,
  itemAssetKey,
  structureAssetKey,
} from './campaign-asset-tags';

describe('campaign asset tag keys', () => {
  it('distinguishes a structure logo from its pillaged variant', () => {
    expect(structureAssetKey('abc')).toBe('structure:abc');
    expect(structureAssetKey('abc', true)).toBe('structure-pillaged:abc');
  });

  it('keys an item objective by its type', () => {
    expect(itemAssetKey('relic-1')).toBe('item:relic-1');
  });

  it('keys a force-status token by its catalog id', () => {
    expect(forceStatusTokenAssetKey('status-1')).toBe('force-status-token:status-1');
  });

  it('lowercases the subfaction so the key matches the server', () => {
    expect(factionAssetKey('f1')).toBe('faction:f1');
    expect(factionAssetKey('f1', ' Iron Guard ')).toBe('faction:f1:iron guard');
  });

  it('treats an empty subfaction as the faction itself', () => {
    expect(factionAssetKey('f1', '   ')).toBe('faction:f1');
  });
});

describe('assetTagQuery', () => {
  it('emits the tag as a query parameter', () => {
    expect(assetTagQuery({ [MAP_ASSET_KEY]: 'abc123' }, MAP_ASSET_KEY)).toBe('?t=abc123');
  });

  it('emits nothing when the asset has no tag, rather than guessing one', () => {
    expect(assetTagQuery({}, MAP_ASSET_KEY)).toBe('');
    expect(assetTagQuery(undefined, MAP_ASSET_KEY)).toBe('');
  });
});
