export interface CampaignListItem {
  id: string;
  name: string;
  playerSlotCount: number;
  occupiedPlayerSlots: number;
  isPrivate: boolean;
  canManage: boolean;
  isParticipant: boolean;
}

export interface CampaignDetail {
  id: string;
  name: string;
  description: string | null;
  playerSlotCount: number;
  occupiedPlayerSlots: number;
  isPrivate: boolean;
  creatorIsParticipant: boolean;
  hasMap: boolean;
  canManage: boolean;
  isParticipant: boolean;
  revision: number;
  createdUtc: string;
  updatedUtc: string;
  factions: CampaignFaction[];
  allyGroups: CampaignAllyGroup[];
  links: CampaignLink[];
}

export interface CampaignFaction {
  id: string;
  name: string;
  subfactions: string[];
  allyGroupName: string | null;
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

export interface SaveCampaignPayload {
  name: string;
  description: string | null;
  playerCount: number;
  isPrivate: boolean;
  joinPassword: string | null;
  creatorIsParticipant: boolean;
  factions: SaveFactionPayload[];
  allyGroups: SaveAllyGroupPayload[];
  links: SaveLinkPayload[];
  revision?: number;
}

export interface SaveFactionPayload {
  name: string;
  subfactions: string[];
  allyGroupName: string | null;
}

export interface SaveAllyGroupPayload {
  name: string;
}

export interface SaveLinkPayload {
  label: string;
  url: string;
}
