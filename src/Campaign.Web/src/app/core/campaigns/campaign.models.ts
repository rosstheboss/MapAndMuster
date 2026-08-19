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
  specialRules?: CampaignSpecialRule[];
  missions?: CampaignMission[];
  forceStatuses?: CampaignForceStatus[];
  privateObjectiveTypes?: CampaignPrivateObjectiveType[];
  privateObjectives?: PrivateObjectiveAssignment[];
  privateObjectiveUnclaimedCounts?: PrivateObjectiveUnclaimedCount[];
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
  splitForceSupplyPenaltyPercent?: number;
  alwaysAskGeneralKill?: boolean;
  alwaysAskSupplyLineDestroyed?: boolean;
  generalKillCampaignPoints?: number;
  supplyLineDestroyedCampaignPoints?: number;
  roundEscalations?: RoundArmyEscalation[];
  standings?: CampaignPointStanding[];
  publicObjectiveLeaderboards?: PublicObjectiveLeaderboard[];
  brokenAllyFactionIds?: string[];
}

export interface RoundPhase {
  kind: string;
  durationAmount: number;
  durationUnit: string;
  endPhaseEarlyIfAble?: boolean;
}

export interface CampaignFaction {
  id: string;
  name: string;
  color: string;
  subfactions: string[];
  allyGroupName: string | null;
  requiresSubfaction: boolean;
  hasFlagImage: boolean;
  specialRuleIds?: string[];
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

export interface RoundArmyEscalation {
  roundNumber: number;
  maxArmyPoints: number;
  freeSupplyPoints: number;
  freeCharacterCount: number;
}

export interface CampaignMission {
  id: string;
  name: string;
  url: string | null;
  hasFile: boolean;
  fileName: string | null;
  resultQuestions?: MissionResultQuestion[];
  isAttackerDefender?: boolean;
  hasArmyPointsAdvantage?: boolean;
  armyPointsAdvantageSide?: string;
  armyPointsAdvantageIsPercent?: boolean;
  armyPointsAdvantageAmount?: number;
  hasSupplyPointsAdvantage?: boolean;
  supplyPointsAdvantageSide?: string;
  supplyPointsAdvantageAmount?: number;
}

export interface MissionResultQuestion {
  id: string;
  prompt: string;
  kind: string;
  battlePoints: number;
  campaignPoints: number;
}

export interface CampaignTerrainType {
  id: string;
  name: string;
  color: string;
  missions: CampaignMission[];
  campaignPoints?: number;
  isWaterFeature?: boolean;
  supplyPoints?: number;
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
  supplyPoints?: number;
  pillageSupplyPoints?: number;
  destroySupplyPoints?: number;
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
  flavorText?: string | null;
  choices?: ItemObjectiveChoice[];
  specialRuleIds?: string[];
}

export interface CampaignPublicObjectiveType {
  id: string;
  name: string;
  description?: string | null;
  campaignPoints: number;
}

export interface ItemObjectiveChoice {
  id: string;
  name: string;
  results?: ItemObjectiveChoiceResult[];
}

export interface ItemObjectiveChoiceResult {
  id: string;
  flavorText?: string | null;
  newStateKey?: string | null;
  destroyItem?: boolean;
  replacementItemTypeId?: string | null;
  grantedPrivateObjectiveTypeId?: string | null;
}

export interface CampaignSpecialRule {
  id: string;
  name: string;
  text: string;
}

export interface CampaignForceStatus {
  id: string;
  name: string;
  effects: string;
  enableTrigger: string;
  clearTrigger: string;
}

export interface CampaignPrivateObjectiveType {
  id: string;
  name?: string | null;
  description?: string | null;
  campaignPoints?: number | null;
  allowedHolderKinds?: string[];
  scoringKind: string;
  automaticKind?: string | null;
  requiredCount?: number;
  structureTypeId?: string | null;
  territoryIds?: string[];
}

export interface PrivateObjectiveAssignment {
  id: string;
  typeId: string;
  holderKind: string;
  holderId: string;
  status: string;
  scoringKind: string;
  name?: string | null;
  description?: string | null;
  campaignPoints?: number | null;
  canClaim?: boolean;
  canModerate?: boolean;
}

export interface PrivateObjectiveUnclaimedCount {
  holderKind: string;
  holderId: string;
  holderName: string;
  count: number;
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
  privateObjectivePoints?: number;
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

export interface CampaignPresetListItem {
  id: string;
  name: string;
  hasMap: boolean;
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
  itemObjectiveTypes: SaveItemObjectiveTypePayload[];
  publicObjectiveTypes?: SavePublicObjectiveTypePayload[];
  specialRules?: SaveSpecialRulePayload[];
  missions?: SaveMissionPayload[];
  forceStatuses?: SaveForceStatusPayload[];
  privateObjectiveTypes?: SavePrivateObjectiveTypePayload[];
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
  splitForceSupplyPenaltyPercent?: number;
  alwaysAskGeneralKill?: boolean;
  alwaysAskSupplyLineDestroyed?: boolean;
  generalKillCampaignPoints?: number;
  supplyLineDestroyedCampaignPoints?: number;
  roundEscalations?: RoundArmyEscalation[];
}

export interface SaveRoundPhasePayload {
  kind: string;
  durationAmount: number;
  durationUnit: string;
  endPhaseEarlyIfAble?: boolean;
}

export interface SaveFactionPayload {
  id?: string;
  name: string;
  color: string;
  subfactions: string[];
  allyGroupName: string | null;
  requiresSubfaction: boolean;
  clearFlagImage?: boolean;
  specialRuleIds?: string[];
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
  isWaterFeature?: boolean;
  supplyPoints?: number;
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
  supplyPoints?: number;
  pillageSupplyPoints?: number;
  destroySupplyPoints?: number;
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
  flavorText?: string | null;
  specialRuleIds?: string[];
  choices?: SaveItemObjectiveChoicePayload[];
}

export interface SavePublicObjectiveTypePayload {
  id?: string;
  name: string;
  description?: string | null;
  campaignPoints?: number;
}

export interface SaveItemObjectiveChoicePayload {
  id?: string;
  name: string;
  results?: SaveItemObjectiveChoiceResultPayload[];
}

export interface SaveItemObjectiveChoiceResultPayload {
  id?: string;
  flavorText?: string | null;
  newStateKey?: string | null;
  destroyItem?: boolean;
  replacementItemTypeId?: string | null;
  grantedPrivateObjectiveTypeId?: string | null;
}

export interface SaveSpecialRulePayload {
  id?: string;
  name: string;
  text?: string | null;
}

export interface SaveForceStatusPayload {
  id?: string;
  name: string;
  effects?: string | null;
  enableTrigger: string;
  clearTrigger: string;
}

export interface SavePrivateObjectiveTypePayload {
  id?: string;
  name: string;
  description?: string | null;
  campaignPoints?: number;
  allowedHolderKinds?: string[];
  scoringKind?: string;
  automaticKind?: string;
  requiredCount?: number;
  structureTypeId?: string | null;
  territoryIds?: string[];
}

export interface SaveMissionPayload {
  id?: string;
  name: string;
  url?: string | null;
  clearFile?: boolean;
  resultQuestions?: SaveMissionResultQuestionPayload[];
  isAttackerDefender?: boolean;
  hasArmyPointsAdvantage?: boolean;
  armyPointsAdvantageSide?: string;
  armyPointsAdvantageIsPercent?: boolean;
  armyPointsAdvantageAmount?: number;
  hasSupplyPointsAdvantage?: boolean;
  supplyPointsAdvantageSide?: string;
  supplyPointsAdvantageAmount?: number;
}

export interface SaveMissionResultQuestionPayload {
  id?: string;
  prompt: string;
  kind?: string;
  battlePoints?: number;
  campaignPoints?: number;
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
  privateObjectives?: PrivateObjectiveAssignment[];
  privateObjectiveUnclaimedCounts?: PrivateObjectiveUnclaimedCount[];
  specialRules?: CampaignSpecialRule[];
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
  statusName?: string | null;
  statusEffects?: string | null;
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
  flavorText?: string | null;
  stateKey?: string | null;
  isDestroyed?: boolean;
  resolvedChoiceId?: string | null;
  choices?: ItemObjectiveChoice[];
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
  activeForceIds?: string[];
  waitingForceIds?: string[];
  reportingForceIds?: string[];
  isNoContest?: boolean;
  isRinger?: boolean;
  ringerFactionId?: string | null;
  isMine: boolean;
  mySubmission: PlayBattleSubmission | null;
  opponentSubmission: PlayBattleSubmission | null;
  winnerForceId: string | null;
  isDraw: boolean;
  winnerScore?: number | null;
  loserScore?: number | null;
  needsRetreat: boolean;
  retreatTargets: string[];
  canSurrender?: boolean;
  resultQuestions?: MissionResultQuestion[];
  viewerSupplyPoints?: number | null;
  forceSupplies?: PlayBattleForceSupply[];
  canStaffConfirm?: boolean;
  mission?: CampaignMission | null;
  attackerForceId?: string | null;
  defenderForceId?: string | null;
}

export interface PlayBattleForceSupply {
  forceId: string;
  userId: string;
  forceAllowancePoints: number;
  currentSupplyPoints: number;
  temporarySupplyPoints: number;
  mapSupplyPoints?: number;
  roundFreeSupplyPoints?: number;
  splitPenaltyPoints?: number;
  roundMaxArmyPoints?: number;
  alliedArmyPoints?: number;
  freeCharacterCount?: number;
  isSplit?: boolean;
}

export interface PlayBattleSubmission {
  submitterUserId: string;
  winnerForceId: string | null;
  isDraw: boolean;
  winnerScore?: number | null;
  loserScore?: number | null;
  reports?: BattleParticipantReport[];
}

export interface BattleParticipantReport {
  forceId: string;
  victoryPoints: number;
  armyPoints: number;
  differentialBattlePoints: number;
  bonusBattlePoints: number;
  supplyCostingUnitCount: number;
  armyListText?: string | null;
  armyListGameSystem?: string | null;
  armyListBuilder?: string | null;
  supplyCategories?: ArmyListSupplyCategory[];
  killedEnemyGeneral: boolean;
  destroyedEnemySupplyLine: boolean;
  answers: BattleQuestionAnswer[];
}

export interface ArmyListSupplyCategory {
  name: string;
  unitCount: number;
  supplyPoints: number;
  costsSupply: boolean;
}

export interface ParseArmyListPayload {
  gameSystem?: string | null;
  builder?: string | null;
  text?: string | null;
}

export interface ParseArmyListResult {
  parsed: boolean;
  message?: string | null;
  armyPoints: number;
  supplyCostingUnitCount: number;
  categories: ArmyListSupplyCategory[];
}

export interface BattleQuestionAnswer {
  questionId: string;
  booleanValue?: boolean | null;
  battlePointsValue?: number | null;
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
  currentSupplyPoints?: number | null;
  temporarySupplyPoints?: number | null;
  mapSupplyPoints?: number | null;
  roundFreeSupplyPoints?: number | null;
  maxArmyPoints?: number | null;
  freeCharacterCount?: number | null;
}

export interface UserSearchHit {
  userId: string;
  username: string;
  displayName: string;
}

export interface SaveOrderDraftPayload {
  revision: number;
  forceId: string;
  kind: string;
  targetTerritoryId?: string | null;
  structureTypeId?: string | null;
  reResolvePrevious?: boolean;
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

export interface GrantPrivateObjectivePayload {
  revision: number;
  holderKind: string;
  holderId: string;
  typeId?: string | null;
}

export interface ClaimPrivateObjectivePayload {
  revision: number;
  assignmentId: string;
}

export interface ModeratePrivateObjectivePayload {
  revision: number;
  assignmentId: string;
  approved: boolean;
}

export interface ResolveItemObjectiveChoicePayload {
  revision: number;
  itemId: string;
  choiceId: string;
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
  reports?: BattleParticipantReport[];
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

export interface InjectRingerBattlePayload {
  revision: number;
  targetForceId: string;
  ringerFactionId: string;
  missionId?: string | null;
  playerIsDefender?: boolean;
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
