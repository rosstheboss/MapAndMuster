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
  canPlay: boolean;
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
  factionId: string | null;
  subfaction: string | null;
  canPlay: boolean;
  canChooseFaction: boolean;
  canChat: boolean;
  canInspectPrivateChat?: boolean;
  participants?: CampaignParticipant[];
  mentionableMembers: CampaignLogMember[];
  chatChannels?: ChatChannel[];
  log: PlayLogEntry[];
  terrainTypes: CampaignTerrainType[];
  structureTypes: CampaignStructureType[];
  itemObjectiveTypes?: CampaignItemObjectiveType[];
  publicObjectiveTypes?: CampaignPublicObjectiveType[];
  pointsPerBattleWon?: number;
  pointsPerBattleDraw?: number;
  useDifferentialBattleScoring?: boolean;
  differentialMultiplier?: number;
  differentialMinimum?: number;
  differentialMaximum?: number;
  allowNegativeDifferential?: boolean;
  mostTerritoriesCampaignPoints?: number;
  longestTerritoryChainCampaignPoints?: number;
  mostBattlesWonCampaignPoints?: number;
  standings?: CampaignPointStanding[];
  publicObjectiveLeaderboards?: PublicObjectiveLeaderboard[];
  brokenAllyFactionIds?: string[];
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
  color?: string;
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
  campaignPoints?: number;
}

export interface CampaignStructureType {
  id: string;
  name: string;
  builtinSymbol: string | null;
  hasImage: boolean;
  hasPillagedImage: boolean;
  isBuildable: boolean;
  isPillageable: boolean;
  isDestructible: boolean;
  missions: CampaignMission[];
  campaignPoints?: number;
}

export interface CampaignItemObjectiveType {
  id: string;
  name: string;
  isHiddenUntilFound: boolean;
  placement: ItemObjectivePlacement;
  allowOnSpawn: boolean;
  builtinSymbol?: string;
  color?: string;
  hasImage?: boolean;
  campaignPoints?: number;
}

export interface CampaignPublicObjectiveType {
  id: string;
  name: string;
  description?: string | null;
  campaignPoints: number;
}

export interface CampaignPointStanding {
  userId: string;
  username: string;
  displayName: string;
  factionId?: string | null;
  factionName?: string | null;
  factionColor?: string | null;
  hasFlagImage?: boolean;
  allyGroupName?: string | null;
  territoryAndStructurePoints: number;
  battlesWonPoints: number;
  publicObjectivePoints: number;
  otherPoints: number;
  total: number;
  heldItems?: HeldItemObjective[];
}

export interface PublicObjectiveLeaderboard {
  kind: string;
  awardPoints: number;
  leaders: PublicObjectiveLeader[];
}

export interface PublicObjectiveLeader {
  userId: string;
  username: string;
  displayName: string;
  rank: number;
  metric: number;
  tieBreakMetric: number;
  awardsPoints: boolean;
}

export interface HeldItemObjective {
  typeId: string;
  name: string;
  builtinSymbol?: string | null;
  color?: string;
  hasImage?: boolean;
}

export type ItemObjectivePlacement = 'Random' | 'Placed';

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
  itemObjectiveTypes: SaveItemObjectiveTypePayload[];
  publicObjectiveTypes?: SavePublicObjectiveTypePayload[];
  pointsPerBattleWon?: number;
  pointsPerBattleDraw?: number;
  useDifferentialBattleScoring?: boolean;
  differentialMultiplier?: number;
  differentialMinimum?: number;
  differentialMaximum?: number;
  allowNegativeDifferential?: boolean;
  mostTerritoriesCampaignPoints?: number;
  longestTerritoryChainCampaignPoints?: number;
  mostBattlesWonCampaignPoints?: number;
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
  color?: string;
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
  campaignPoints?: number;
}

export interface SaveStructureTypePayload {
  id?: string;
  name: string;
  builtinSymbol?: string | null;
  clearImage?: boolean;
  clearPillagedImage?: boolean;
  isBuildable: boolean;
  isPillageable: boolean;
  isDestructible: boolean;
  missions: SaveMissionPayload[];
  campaignPoints?: number;
}

export interface SaveItemObjectiveTypePayload {
  id?: string;
  name: string;
  isHiddenUntilFound: boolean;
  placement: ItemObjectivePlacement;
  allowOnSpawn: boolean;
  builtinSymbol?: string | null;
  color?: string | null;
  clearImage?: boolean;
  campaignPoints?: number;
}

export interface SavePublicObjectiveTypePayload {
  id?: string;
  name: string;
  description?: string | null;
  campaignPoints?: number;
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
  itemObjectivePlacements?: ItemObjectivePlacementPayload[];
}

export interface MapTerritoryPayload {
  id: string;
  displayNumber: number;
  name: string | null;
  description: string | null;
  polygon: MapPointPayload[];
  terrainTypeId: string;
  structureTypeId: string | null;
  structureCondition?: string | null;
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

export interface ItemObjectivePlacementPayload {
  typeId: string;
  territoryId: string;
}

export interface SaveMapGraphPayload {
  revision: number;
  territories: MapTerritoryPayload[];
  adjacencies: MapAdjacencyPayload[];
  itemObjectivePlacements?: ItemObjectivePlacementPayload[];
}

export interface CampaignPlayDetail {
  id: string;
  name: string;
  revision: number;
  canManage: boolean;
  canDebug: boolean;
  isDebugActive: boolean;
  debugActorUserId: string | null;
  isParticipant: boolean;
  canChat: boolean;
  canInspectPrivateChat?: boolean;
  mentionableMembers: CampaignLogMember[];
  chatChannels?: ChatChannel[];
  status: string;
  currentRound: number | null;
  currentPhaseNumber: number | null;
  currentPhaseKind: string | null;
  currentPhaseLabel: string | null;
  currentPhaseStartsUtc: string | null;
  currentPhaseEndsUtc: string | null;
  currentWindowId: string | null;
  hasMap: boolean;
  factionId: string | null;
  canChooseFaction: boolean;
  isCommitted: boolean;
  roundCount: number;
  minRoundCount: number;
  remainingWindows: PlayWindow[];
  factions: CampaignFaction[];
  structureTypes: CampaignStructureType[];
  itemObjectives?: PlayItemObjective[];
  brokenAllyFactionIds?: string[];
  standings?: CampaignPointStanding[];
  publicObjectiveLeaderboards?: PublicObjectiveLeaderboard[];
  pointsPerBattleWon?: number;
  pointsPerBattleDraw?: number;
  useDifferentialBattleScoring?: boolean;
  forces: PlayForce[];
  myDrafts: PlayDraft[];
  orders: PlayOrder[];
  debugDrafts: PlayDraft[];
  commitments: PlayCommitment[];
  battles: PlayBattle[];
  log: PlayLogEntry[];
  playersMissingFaction: string[];
}

export interface PlayWindow {
  id: string;
  roundNumber: number;
  phaseNumber: number;
  kind: string;
  label: string;
  endsUtc: string;
}

export interface PlayForce {
  id: string;
  controllerUserId: string;
  controllerUsername: string | null;
  factionId: string;
  territoryId: string;
  isMine: boolean;
  inBattle: boolean;
  moveTargets: string[];
  availableActions: string[];
}

export interface PlayItemObjective {
  id: string;
  typeId: string;
  name: string;
  territoryId: string | null;
  possessorForceId: string | null;
  isRevealed: boolean;
  builtinSymbol?: string;
  color?: string;
  hasImage?: boolean;
}

export interface PlayDraft {
  forceId: string;
  kind: string;
  targetTerritoryId: string | null;
  structureTypeId: string | null;
}

export interface PlayOrder {
  forceId: string;
  kind: string;
  targetTerritoryId: string | null;
  isRevealed: boolean;
}

export interface PlayCommitment {
  userId: string;
  username: string | null;
  isCommitted: boolean;
}

export interface PlayBattle {
  id: string;
  territoryId: string;
  status: string;
  participantForceIds: string[];
  isMine: boolean;
  mySubmission: PlayBattleSubmission | null;
  opponentSubmission: PlayBattleSubmission | null;
  winnerForceId: string | null;
  isDraw: boolean;
  winnerScore?: number | null;
  loserScore?: number | null;
  needsRetreat: boolean;
  retreatTargets: string[];
}

export interface PlayBattleSubmission {
  submitterUserId: string;
  winnerForceId: string | null;
  isDraw: boolean;
  winnerScore?: number | null;
  loserScore?: number | null;
}

export interface PlayLogEntry {
  id: string;
  occurredUtc: string;
  kind: string;
  originator: string;
  originatorUsername?: string | null;
  summary: string;
  territoryId: string | null;
  forceId: string | null;
  battleId: string | null;
  isSystemAdjustment: boolean;
  channelKind?: string;
  channelLabel?: string | null;
  isPrivate?: boolean;
}

export interface ChatChannel {
  kind: string;
  targetId: string | null;
  label: string;
}

export interface CampaignChatSend {
  message: string;
  channelKind: string;
  targetId: string | null;
}

export interface CampaignLogMember {
  userId: string;
  username: string;
  displayName: string;
}

export interface CampaignParticipant {
  userId: string;
  username: string;
  displayName: string;
  isPlayer: boolean;
  isGameMaster: boolean;
  isAdministrator: boolean;
  factionName?: string | null;
  subfaction?: string | null;
  factionId?: string | null;
  factionColor?: string | null;
  hasFlagImage?: boolean;
  allyGroupName?: string | null;
}

export interface SaveOrderDraftPayload {
  revision: number;
  forceId: string;
  kind: string;
  targetTerritoryId?: string | null;
  structureTypeId?: string | null;
}

export interface PlayRevisionPayload {
  revision: number;
}

export interface SetPublicObjectiveAwardPayload {
  revision: number;
  objectiveId: string;
  playerUserId: string;
  awarded: boolean;
}

export interface PostCampaignChatPayload {
  revision: number;
  message: string;
  channelKind: string;
  targetId: string | null;
}

export interface SubmitBattleResultPayload {
  revision: number;
  battleId: string;
  winnerForceId?: string | null;
  isDraw: boolean;
  winnerScore?: number | null;
  loserScore?: number | null;
}

export interface BattleActionPayload {
  revision: number;
  battleId: string;
}

export interface SubmitRetreatPayload {
  revision: number;
  battleId: string;
  targetTerritoryId: string;
}

export interface ExtendCampaignSchedulePayload {
  revision: number;
  roundCount: number;
  extensions: { windowId: string; durationAmount: number; durationUnit: string }[];
}

export interface ChooseFactionPayload {
  revision: number;
  factionId: string;
  subfaction?: string | null;
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
