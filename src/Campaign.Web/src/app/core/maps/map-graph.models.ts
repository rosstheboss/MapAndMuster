import type { MapPoint } from './geometry';

export type AdjacencyOrigin = 'Generated' | 'Manual';

export interface MapTerritory {
  id: string;
  displayNumber: number;
  name: string | null;
  description: string | null;
  polygon: MapPoint[];
  terrainTypeId: string;
  structureTypeId: string | null;
  overlayColor: string | null;
  ownerFactionId: string | null;
  spawnFactionId: string | null;
}

export interface MapAdjacency {
  id: string;
  territoryAId: string;
  territoryBId: string;
  origin: AdjacencyOrigin;
  marker: MapPoint;
}

export interface MapGraph {
  territories: MapTerritory[];
  adjacencies: MapAdjacency[];
}

export function territoryLabel(territory: MapTerritory): string {
  const name = territory.name?.trim();
  if (name) {
    return name;
  }

  return String(territory.displayNumber);
}

export function nextDisplayNumber(territories: readonly MapTerritory[]): number {
  return territories.reduce((max, territory) => Math.max(max, territory.displayNumber), 0) + 1;
}

export function createId(): string {
  return crypto.randomUUID();
}

export function cloneGraph(graph: MapGraph): MapGraph {
  return {
    territories: graph.territories.map((territory) => ({
      ...territory,
      polygon: territory.polygon.map((point) => ({ ...point })),
    })),
    adjacencies: graph.adjacencies.map((edge) => ({
      ...edge,
      marker: { ...edge.marker },
    })),
  };
}
