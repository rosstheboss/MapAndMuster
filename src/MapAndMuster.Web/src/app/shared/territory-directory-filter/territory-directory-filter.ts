import type {
  CampaignAllyGroup,
  CampaignFaction,
  CampaignStructureType,
  CampaignTerrainType,
  CatalogTag,
} from '../../core/campaigns/campaign.models';
import type { MapAdjacency, MapTerritory } from '../../core/maps/map-graph.models';

export type TerritoryFilterTriState = 'any' | 'yes' | 'no';

export interface TerritoryDirectoryFilterState {
  ownerFactionIds: string[];
  factionTagIds: string[];
  allyGroupIds: string[];
  neutral: TerritoryFilterTriState;
  spawn: TerritoryFilterTriState;
  terrainTypeIds: string[];
  terrainTagIds: string[];
  structureTypeIds: string[];
  structureTagIds: string[];
  pillaged: TerritoryFilterTriState;
  hasStructure: TerritoryFilterTriState;
  occupied: TerritoryFilterTriState;
  revealedItemKeys: string[];
  adjacent: TerritoryFilterTriState;
}

export interface TerritoryDirectoryFilterForce {
  territoryId: string;
}

export interface TerritoryDirectoryFilterItem {
  territoryId: string;
  hidden?: boolean;
  key: string;
}

export interface TerritoryDirectoryFilterContext {
  factions: readonly CampaignFaction[];
  terrainTypes: readonly CampaignTerrainType[];
  structures: readonly CampaignStructureType[];
  allyGroups: readonly CampaignAllyGroup[];
  factionTags: readonly CatalogTag[];
  terrainTags: readonly CatalogTag[];
  structureTags: readonly CatalogTag[];
  forces: readonly TerritoryDirectoryFilterForce[];
  items: readonly TerritoryDirectoryFilterItem[];
  adjacencies: readonly MapAdjacency[];
}

export function cloneTerritoryDirectoryFilter(filter: TerritoryDirectoryFilterState): TerritoryDirectoryFilterState {
  return {
    ownerFactionIds: [...filter.ownerFactionIds],
    factionTagIds: [...filter.factionTagIds],
    allyGroupIds: [...filter.allyGroupIds],
    neutral: filter.neutral,
    spawn: filter.spawn,
    terrainTypeIds: [...filter.terrainTypeIds],
    terrainTagIds: [...filter.terrainTagIds],
    structureTypeIds: [...filter.structureTypeIds],
    structureTagIds: [...filter.structureTagIds],
    pillaged: filter.pillaged,
    hasStructure: filter.hasStructure,
    occupied: filter.occupied,
    revealedItemKeys: [...filter.revealedItemKeys],
    adjacent: filter.adjacent,
  };
}

export function createDefaultTerritoryDirectoryFilter(
  context: TerritoryDirectoryFilterContext,
): TerritoryDirectoryFilterState {
  return {
    ownerFactionIds: context.factions.map((faction) => faction.id),
    factionTagIds: context.factionTags.map((tag) => tag.id),
    allyGroupIds: context.allyGroups.map((group) => group.id),
    neutral: 'any',
    spawn: 'any',
    terrainTypeIds: context.terrainTypes.map((type) => type.id),
    terrainTagIds: context.terrainTags.map((tag) => tag.id),
    structureTypeIds: context.structures.map((type) => type.id),
    structureTagIds: context.structureTags.map((tag) => tag.id),
    pillaged: 'any',
    hasStructure: 'any',
    occupied: 'any',
    revealedItemKeys: revealedItemKeys(context),
    adjacent: 'any',
  };
}

export function applyTerritoryDirectoryFilter(
  territories: readonly MapTerritory[],
  filter: TerritoryDirectoryFilterState,
  context: TerritoryDirectoryFilterContext,
): string[] {
  const seed = territories.filter((territory) => matchesSeed(territory, filter, context));
  const seedIds = new Set(seed.map((territory) => territory.id));
  if (filter.adjacent === 'any') {
    return [...seedIds];
  }

  const neighbors = neighborIds(context.adjacencies);
  const allIds = territories.map((territory) => territory.id);
  if (seedIds.size === allIds.length) {
    return filter.adjacent === 'yes'
      ? allIds.filter((id) => (neighbors.get(id)?.size ?? 0) > 0)
      : allIds.filter((id) => (neighbors.get(id)?.size ?? 0) === 0);
  }

  if (filter.adjacent === 'yes') {
    return allIds.filter((id) => !seedIds.has(id) && hasNeighborIn(id, seedIds, neighbors));
  }

  return allIds.filter((id) => !seedIds.has(id) && !hasNeighborIn(id, seedIds, neighbors));
}

function matchesSeed(
  territory: MapTerritory,
  filter: TerritoryDirectoryFilterState,
  context: TerritoryDirectoryFilterContext,
): boolean {
  const owner = context.factions.find((faction) => faction.id === territory.ownerFactionId) ?? null;
  const isNeutral = !territory.ownerFactionId;
  if (!matchesTri(filter.neutral, isNeutral)) {
    return false;
  }

  if (!matchesTri(filter.spawn, !!territory.spawnFactionId)) {
    return false;
  }

  if (selectionIsActive(filter.ownerFactionIds, ids(context.factions)) && !isNeutral) {
    if (!territory.ownerFactionId || !filter.ownerFactionIds.includes(territory.ownerFactionId)) {
      return false;
    }
  }

  if (selectionIsActive(filter.factionTagIds, ids(context.factionTags))) {
    if (isNeutral || !intersects(owner?.tagIds, filter.factionTagIds)) {
      return false;
    }
  }

  if (selectionIsActive(filter.allyGroupIds, ids(context.allyGroups))) {
    const groupId = owner?.allyGroupId ?? null;
    if (!groupId || !filter.allyGroupIds.includes(groupId)) {
      return false;
    }
  }

  if (selectionIsActive(filter.terrainTypeIds, ids(context.terrainTypes))) {
    if (!filter.terrainTypeIds.includes(territory.terrainTypeId)) {
      return false;
    }
  }

  const terrain = context.terrainTypes.find((type) => type.id === territory.terrainTypeId) ?? null;
  if (selectionIsActive(filter.terrainTagIds, ids(context.terrainTags))) {
    if (!intersects(terrain?.tagIds, filter.terrainTagIds)) {
      return false;
    }
  }

  const hasStructure = !!territory.structureTypeId && territory.structureCondition !== 'Destroyed';
  if (!matchesTri(filter.hasStructure, hasStructure)) {
    return false;
  }

  const structure =
    hasStructure && territory.structureTypeId
      ? (context.structures.find((type) => type.id === territory.structureTypeId) ?? null)
      : null;
  if (selectionIsActive(filter.structureTypeIds, ids(context.structures)) && hasStructure) {
    if (!territory.structureTypeId || !filter.structureTypeIds.includes(territory.structureTypeId)) {
      return false;
    }
  }

  if (selectionIsActive(filter.structureTagIds, ids(context.structureTags)) && hasStructure) {
    if (!intersects(structure?.tagIds, filter.structureTagIds)) {
      return false;
    }
  }

  if (!matchesTri(filter.pillaged, territory.structureCondition === 'Pillaged')) {
    return false;
  }

  const occupied = context.forces.some((force) => force.territoryId === territory.id);
  if (!matchesTri(filter.occupied, occupied)) {
    return false;
  }

  const revealed = context.items.filter((item) => item.territoryId === territory.id && !item.hidden);
  const itemKeys = revealedItemKeys(context);
  if (selectionIsActive(filter.revealedItemKeys, itemKeys)) {
    if (!revealed.some((item) => filter.revealedItemKeys.includes(item.key))) {
      return false;
    }
  }

  return true;
}

function matchesTri(state: TerritoryFilterTriState, value: boolean): boolean {
  if (state === 'any') {
    return true;
  }

  return state === 'yes' ? value : !value;
}

function selectionIsActive(selected: readonly string[], all: readonly string[]): boolean {
  return all.length > 0 && selected.length > 0 && selected.length < all.length;
}

function ids(items: readonly { id: string }[]): string[] {
  return items.map((item) => item.id);
}

function intersects(left: readonly string[] | undefined, right: readonly string[]): boolean {
  return (left ?? []).some((id) => right.includes(id));
}

export function revealedItemKeys(context: TerritoryDirectoryFilterContext): string[] {
  return [...new Set(context.items.filter((item) => !item.hidden).map((item) => item.key))].sort((left, right) =>
    left.localeCompare(right),
  );
}

function neighborIds(adjacencies: readonly MapAdjacency[]): Map<string, Set<string>> {
  const neighbors = new Map<string, Set<string>>();
  const add = (from: string, to: string): void => {
    const set = neighbors.get(from) ?? new Set<string>();
    set.add(to);
    neighbors.set(from, set);
  };
  for (const edge of adjacencies) {
    add(edge.territoryAId, edge.territoryBId);
    add(edge.territoryBId, edge.territoryAId);
  }

  return neighbors;
}

function hasNeighborIn(id: string, seed: Set<string>, neighbors: Map<string, Set<string>>): boolean {
  const next = neighbors.get(id);
  if (!next) {
    return false;
  }

  for (const other of next) {
    if (seed.has(other)) {
      return true;
    }
  }

  return false;
}
