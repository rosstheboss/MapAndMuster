import type { CampaignFaction, CampaignStructureType, CampaignTerrainType } from '../../core/campaigns/campaign.models';
import type { MapTerritory } from '../../core/maps/map-graph.models';
import {
  applyTerritoryDirectoryFilter,
  createDefaultTerritoryDirectoryFilter,
  type TerritoryDirectoryFilterContext,
} from './territory-directory-filter';

const north: CampaignFaction = {
  id: 'north',
  name: 'North',
  color: '#111111',
  subfactions: [],
  allyGroupName: 'Coalition',
  allyGroupId: 'coalition',
  requiresSubfaction: false,
  hasFlagImage: false,
  tagIds: ['empire'],
};

const south: CampaignFaction = {
  ...north,
  id: 'south',
  name: 'South',
  allyGroupName: null,
  allyGroupId: null,
  tagIds: ['chaos'],
};

const plains: CampaignTerrainType = {
  id: 'plains',
  name: 'Plains',
  color: '#7CB342',
  missions: [],
  tagIds: ['open'],
};

const forest: CampaignTerrainType = { ...plains, id: 'forest', name: 'Forest', tagIds: ['wood'] };

const town: CampaignStructureType = {
  id: 'town',
  name: 'Town',
  builtinSymbol: 'Town',
  hasImage: false,
  hasPillagedImage: false,
  isBuildable: true,
  isPillageable: true,
  isDestructible: true,
  missions: [],
  tagIds: ['settlement'],
};

function hex(id: string, overrides: Partial<MapTerritory> = {}): MapTerritory {
  return {
    id,
    displayNumber: 1,
    name: id,
    description: null,
    polygon: [
      { x: 0, y: 0 },
      { x: 1, y: 0 },
      { x: 1, y: 1 },
      { x: 0, y: 1 },
    ],
    terrainTypeId: 'plains',
    structureTypeId: null,
    structureCondition: 'Operational',
    overlayColor: null,
    ownerFactionId: null,
    spawnFactionId: null,
    ...overrides,
  };
}

function context(overrides: Partial<TerritoryDirectoryFilterContext> = {}): TerritoryDirectoryFilterContext {
  return {
    factions: [north, south],
    terrainTypes: [plains, forest],
    structures: [town],
    allyGroups: [{ id: 'coalition', name: 'Coalition' }],
    factionTags: [
      { id: 'empire', name: 'Empire' },
      { id: 'chaos', name: 'Chaos' },
    ],
    terrainTags: [
      { id: 'open', name: 'Open' },
      { id: 'wood', name: 'Wood' },
    ],
    structureTags: [{ id: 'settlement', name: 'Settlement' }],
    forces: [],
    items: [],
    adjacencies: [],
    ...overrides,
  };
}

describe('applyTerritoryDirectoryFilter', () => {
  const coast = hex('coast', { name: 'Coast', ownerFactionId: 'north', terrainTypeId: 'plains' });
  const ridge = hex('ridge', { name: 'Ridge', ownerFactionId: 'south', terrainTypeId: 'forest' });
  const wilds = hex('wilds', { name: 'Wilds' });
  const townHex = hex('town', {
    name: 'Town',
    ownerFactionId: 'north',
    structureTypeId: 'town',
    structureCondition: 'Pillaged',
  });
  const spawn = hex('spawn', { name: 'Spawn', spawnFactionId: 'north' });

  it('returns every territory when the default filter is applied', () => {
    const territories = [coast, ridge, wilds, townHex, spawn];
    const ctx = context();
    expect(applyTerritoryDirectoryFilter(territories, createDefaultTerritoryDirectoryFilter(ctx), ctx).sort()).toEqual([
      'coast',
      'ridge',
      'spawn',
      'town',
      'wilds',
    ]);
  });

  it('filters by owner, occupancy, pillaged structure, and revealed items', () => {
    const territories = [coast, ridge, wilds, townHex];
    const ctx = context({
      forces: [{ territoryId: 'coast' }],
      items: [
        { territoryId: 'ridge', hidden: false, key: 'Crown' },
        { territoryId: 'wilds', hidden: false, key: 'Gem' },
      ],
    });
    const filter = createDefaultTerritoryDirectoryFilter(ctx);
    filter.ownerFactionIds = ['north'];
    expect(applyTerritoryDirectoryFilter(territories, filter, ctx).sort()).toEqual(['coast', 'town', 'wilds']);

    filter.neutral = 'no';
    expect(applyTerritoryDirectoryFilter(territories, filter, ctx).sort()).toEqual(['coast', 'town']);

    filter.occupied = 'yes';
    expect(applyTerritoryDirectoryFilter(territories, filter, ctx)).toEqual(['coast']);

    const itemFilter = createDefaultTerritoryDirectoryFilter(ctx);
    itemFilter.revealedItemKeys = ['Crown'];
    expect(applyTerritoryDirectoryFilter(territories, itemFilter, ctx)).toEqual(['ridge']);

    const pillage = createDefaultTerritoryDirectoryFilter(ctx);
    pillage.pillaged = 'yes';
    expect(applyTerritoryDirectoryFilter(territories, pillage, ctx)).toEqual(['town']);
  });

  it('keeps only neighbors of the matching set when adjacent is yes', () => {
    const territories = [coast, ridge, wilds];
    const ctx = context({
      adjacencies: [
        { id: 'a', territoryAId: 'coast', territoryBId: 'ridge', origin: 'Manual', marker: { x: 0, y: 0 } },
      ],
    });
    const filter = createDefaultTerritoryDirectoryFilter(ctx);
    filter.ownerFactionIds = ['north'];
    filter.neutral = 'no';
    filter.adjacent = 'yes';
    expect(applyTerritoryDirectoryFilter(territories, filter, ctx)).toEqual(['ridge']);
  });
});
