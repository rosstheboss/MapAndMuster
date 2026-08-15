export interface CampaignListItem {
  id: string;
  name: string;
  description: string | null;
  playerSlotCount: number;
  occupiedPlayerSlots: number;
  isPrivate: boolean;
  isPubliclyViewable: boolean;
  canManage: boolean;
  isParticipant: boolean;
  canView: boolean;
  canJoin: boolean;
  canLeave: boolean;
  city: string | null;
  region: string | null;
  country: string | null;
  status: string;
  startsUtc: string;
  endsUtc: string;
  currentRound: number | null;
  currentPhaseLabel: string | null;
  currentPhaseEndsUtc: string | null;
}

export interface CampaignDetail {
  id: string;
  name: string;
  description: string | null;
  playerSlotCount: number;
  occupiedPlayerSlots: number;
  isPrivate: boolean;
  isPubliclyViewable: boolean;
  creatorIsParticipant: boolean;
  city: string | null;
  region: string | null;
  country: string | null;
  hasMap: boolean;
  canManage: boolean;
  isParticipant: boolean;
  revision: number;
  createdUtc: string;
  updatedUtc: string;
  factions: CampaignFaction[];
  allyGroups: CampaignAllyGroup[];
  links: CampaignLink[];
  timeZoneId: string;
  startsAtLocal: string;
  startsUtc: string;
  endsUtc: string;
  roundCount: number;
  roundLengthAmount: number;
  roundLengthUnit: string;
  phases: RoundPhase[];
  status: string;
  currentRound: number | null;
  currentPhaseNumber: number | null;
  currentPhaseKind: string | null;
  currentPhaseStartsUtc: string | null;
  currentPhaseEndsUtc: string | null;
  terrainTypes: CampaignTerrainType[];
  structureTypes: CampaignStructureType[];
}

export interface RoundPhase {
  kind: string;
  durationAmount: number;
  durationUnit: string;
}

export interface CampaignFaction {
  id: string;
  name: string;
  color: string;
  subfactions: string[];
  allyGroupName: string | null;
  requiresSubfaction: boolean;
  hasFlagImage: boolean;
}

export interface CampaignAllyGroup {
  id: string;
  name: string;
}

export interface CampaignLink {
  id: string;
  label: string;
  url: string;
}

export interface CampaignMission {
  id: string;
  name: string;
  url: string | null;
  hasFile: boolean;
  fileName: string | null;
}

export interface CampaignTerrainType {
  id: string;
  name: string;
  color: string;
  missions: CampaignMission[];
}

export interface CampaignStructureType {
  id: string;
  name: string;
  builtinSymbol: string | null;
  hasImage: boolean;
  missions: CampaignMission[];
}

export interface SaveCampaignPayload {
  name: string;
  description: string | null;
  playerCount: number;
  isPrivate: boolean;
  isPubliclyViewable: boolean;
  joinPassword: string | null;
  creatorIsParticipant: boolean;
  city: string | null;
  region: string | null;
  country: string | null;
  factions: SaveFactionPayload[];
  allyGroups: SaveAllyGroupPayload[];
  links: SaveLinkPayload[];
  revision?: number;
  timeZoneId: string;
  startsAtLocal: string;
  roundCount: number;
  roundLengthAmount: number;
  roundLengthUnit: string;
  phases: SaveRoundPhasePayload[];
  terrainTypes: SaveTerrainTypePayload[];
  structureTypes: SaveStructureTypePayload[];
}

export interface SaveRoundPhasePayload {
  kind: string;
  durationAmount: number;
  durationUnit: string;
}

export interface SaveFactionPayload {
  id?: string;
  name: string;
  color: string;
  subfactions: string[];
  allyGroupName: string | null;
  requiresSubfaction: boolean;
  clearFlagImage?: boolean;
}

export interface SaveAllyGroupPayload {
  name: string;
}

export interface SaveLinkPayload {
  label: string;
  url: string;
}

export interface SaveTerrainTypePayload {
  id?: string;
  name: string;
  color: string;
  missions: SaveMissionPayload[];
}

export interface SaveStructureTypePayload {
  id?: string;
  name: string;
  builtinSymbol?: string | null;
  clearImage?: boolean;
  missions: SaveMissionPayload[];
}

export interface SaveMissionPayload {
  id?: string;
  name: string;
  url?: string | null;
  clearFile?: boolean;
}

export interface MapGraphDetail {
  campaignId: string;
  revision: number;
  canManage: boolean;
  territories: MapTerritoryPayload[];
  adjacencies: MapAdjacencyPayload[];
}

export interface MapTerritoryPayload {
  id: string;
  displayNumber: number;
  name: string | null;
  description: string | null;
  polygon: MapPointPayload[];
  terrainTypeId: string;
  structureTypeId: string | null;
  overlayColor: string | null;
  ownerFactionId: string | null;
  spawnFactionId: string | null;
}

export interface MapPointPayload {
  x: number;
  y: number;
}

export interface MapAdjacencyPayload {
  id: string;
  territoryAId: string;
  territoryBId: string;
  origin: string;
  markerX: number;
  markerY: number;
}

export interface SaveMapGraphPayload {
  revision: number;
  territories: MapTerritoryPayload[];
  adjacencies: MapAdjacencyPayload[];
}

export function terrainTypeById(
  campaign: CampaignDetail | null | undefined,
  id: string | null | undefined,
): CampaignTerrainType | null {
  if (!campaign || !id) {
    return null;
  }

  return campaign.terrainTypes.find((type) => type.id === id) ?? null;
}

export function structureTypeById(
  campaign: CampaignDetail | null | undefined,
  id: string | null | undefined,
): CampaignStructureType | null {
  if (!campaign || !id) {
    return null;
  }

  return campaign.structureTypes.find((type) => type.id === id) ?? null;
}

export function missionsForTerritory(
  campaign: CampaignDetail | null | undefined,
  terrainTypeId: string | null | undefined,
  structureTypeId: string | null | undefined,
): CampaignMission[] {
  const structure = structureTypeById(campaign, structureTypeId);
  if (structure && structure.missions.length > 0) {
    return structure.missions;
  }

  return terrainTypeById(campaign, terrainTypeId)?.missions ?? [];
}
