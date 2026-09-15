import type { ForceStatusCondition } from './force-status-presets';

export type { ForceStatusCondition };

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
  currentPhaseKind: string | null;
  currentPhaseEndsUtc: string | null;
  canPlay: boolean;
  canChooseFaction: boolean;
  isCommitted: boolean;
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
  /** Opaque cache tags for stored files, keyed by the helpers in `campaign-asset-tags`. */
  assetTags: Record<string, string>;
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
  standardBattleResultQuestions?: StandardBattleResultQuestion[];
  missions?: CampaignMission[];
  terrainTags?: CatalogTag[];
  structureTags?: CatalogTag[];
  factionTags?: CatalogTag[];
  missionTags?: CatalogTag[];
  forceStatuses?: CampaignForceStatus[];
  privateObjectiveTypes?: CampaignPrivateObjectiveType[];
  privateObjectives?: PrivateObjectiveAssignment[];
  privateObjectiveUnclaimedCounts?: PrivateObjectiveUnclaimedCount[];
  rivalObjectives?: RivalObjectiveAssignment[];
  rivalObjectivesEnabled?: boolean;
  rivalObjectiveCampaignPoints?: number;
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
  mostStructurePointsCampaignPoints?: number;
  pointsPerTerritoryCampaignPoints?: number;
  alliedRelicControlCampaignPoints?: number;
  mostTerritoriesTerrainTagId?: string | null;
  longestTerritoryChainTerrainTagId?: string | null;
  mostStructurePointsStructureTagId?: string | null;
  pointsPerTerritoryTerrainTagId?: string | null;
  splitForceSupplyPenaltyPercent?: number;
  splitForceSupplyPenaltyIsPercent?: boolean;
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
  allyGroupId?: string | null;
  requiresSubfaction: boolean;
  hasFlagImage: boolean;
  tintFlagImage?: boolean;
  specialRuleIds?: string[];
  subfactionSpecialRules?: SubfactionSpecialRules[];
  tagIds?: string[];
  subfactionTags?: SubfactionTags[];
  subfactionAppearances?: SubfactionAppearance[];
  forceMovementSpeed?: number;
  subfactionMovementSpeeds?: SubfactionMovementSpeed[];
}

export interface SubfactionMovementSpeed {
  name: string;
  speed: number;
}

export type SubfactionFlagSource = 'inherit' | 'color' | 'image';

export interface SubfactionAppearance {
  name: string;
  color: string | null;
  flagSource: SubfactionFlagSource;
  hasFlagImage: boolean;
  tintFlagImage?: boolean;
}

export interface SubfactionSpecialRules {
  name: string;
  specialRuleIds: string[];
}

export interface SubfactionTags {
  name: string;
  tagIds: string[];
}

export interface CatalogTag {
  id: string;
  name: string;
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
  statusChanges?: MissionStatusChange[];
  isAttackerDefender?: boolean;
  hasArmyPointsAdvantage?: boolean;
  armyPointsAdvantageSide?: string;
  armyPointsAdvantageIsPercent?: boolean;
  armyPointsAdvantageAmount?: number;
  hasSupplyPointsAdvantage?: boolean;
  supplyPointsAdvantageSide?: string;
  supplyPointsAdvantageAmount?: number;
  tagIds?: string[];
}

export interface MissionResultQuestion {
  id: string;
  prompt: string;
  kind: string;
  battlePoints: number;
  campaignPoints: number;
  standardQuestionId?: string | null;
}

export interface MissionStatusChange {
  id: string;
  outcome: string;
  whenCurrentStatus?: string | null;
  setStatus?: string | null;
  leaveUnchanged?: boolean;
}

export interface StandardBattleResultQuestion {
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
  tagIds?: string[];
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
  tagIds?: string[];
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
  effects?: ItemObjectiveEffect[];
}

export const ITEM_OBJECTIVE_EFFECT_KINDS = [
  { id: 'PushDefeatedOpponentToSpawn', label: 'Push a defeated opponent to spawn' },
  { id: 'AddMovementSpeed', label: 'Add force movement speed' },
  { id: 'ModifySupply', label: 'Add or subtract supply (minimum 1)' },
  { id: 'TeleportToRandomEmptyNonSpawn', label: 'Teleport Randomly' },
  { id: 'TeleportToChosenNonSpawnOncePerRound', label: 'Teleport to Specific Territory' },
  { id: 'InflictStatusWhileHeld', label: 'Inflict a status while holding this item' },
  { id: 'ImmuneToStatuses', label: 'Immune to statuses' },
  { id: 'InflictStatusOnSharedTerritory', label: 'Inflict statuses on forces sharing the territory' },
  { id: 'NullifyAdjacentItemObjectives', label: 'Nullify adjacent item objectives' },
  { id: 'ModifyArmyPoints', label: 'Add army points (amount or percent)' },
  { id: 'OverrideAlliances', label: 'Override alliances while held' },
  { id: 'Custom', label: 'Custom battle reminder' },
] as const;

export interface ItemObjectiveEffect {
  id: string;
  kind: string;
  amount?: number;
  amountIsPercent?: boolean;
  statusTypeIds?: string[];
  immuneToAllStatuses?: boolean;
  suspendCurrentAllyGroup?: boolean;
  forcedAllyGroupName?: string | null;
  alliedFactions?: ItemObjectiveAllianceTarget[];
  customText?: string | null;
  successStatusTypeId?: string | null;
  failureStatusTypeId?: string | null;
}

export interface ItemObjectiveAllianceTarget {
  factionId: string;
  subfaction?: string | null;
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
  setForceStatusName?: string | null;
}

export interface CampaignSpecialRule {
  id: string;
  name: string;
  text: string;
  effectKey?: string | null;
}

export interface CampaignForceStatus {
  id: string;
  name: string;
  effects: string;
  enableTrigger?: string;
  clearTrigger?: string;
  enableOccurrences?: number;
  clearOccurrences?: number;
  enableConditions?: ForceStatusCondition[];
  clearConditions?: ForceStatusCondition[];
  priority?: number;
  cancelsStatusIds?: string[];
  immuneFactionIds?: string[];
  immuneSubfactions?: ForceStatusImmuneSubfaction[];
  hasTokenImage?: boolean;
}

export interface ForceStatusImmuneSubfaction {
  factionId: string;
  subfaction: string;
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
  matchesAnyStructureType?: boolean;
  itemObjectiveTypeId?: string | null;
  matchesAnyItemObjective?: boolean;
  targetKind?: string | null;
  targetSelection?: string | null;
  targetId?: string | null;
  forceStatusTypeIds?: string[];
  statusMatchKind?: string | null;
  prerequisiteForceStatusTypeId?: string | null;
  prerequisiteWasLost?: boolean;
  structureTagId?: string | null;
  terrainTagId?: string | null;
  excludedFactionIds?: string[];
  excludedAllyGroupIds?: string[];
}

export interface PrivateObjectiveAssignment {
  id: string;
  typeId: string;
  holderKind: string;
  holderId: string;
  holderSubfaction?: string | null;
  status: string;
  scoringKind: string;
  name?: string | null;
  description?: string | null;
  campaignPoints?: number | null;
  currentCount?: number | null;
  requiredCount?: number | null;
  canClaim?: boolean;
  canModerate?: boolean;
}

export interface PrivateObjectiveUnclaimedCount {
  holderKind: string;
  holderId: string;
  holderName: string;
  count: number;
}

export interface RivalObjectiveAssignment {
  id: string;
  holderUserId: string;
  status: string;
  rivalUserId?: string | null;
  rivalDisplayName?: string | null;
  rivalFactionName?: string | null;
  rivalSubfaction?: string | null;
  campaignPoints?: number | null;
}

export interface CampaignPointStanding {
  userId: string;
  username: string;
  displayName: string;
  factionId?: string | null;
  factionName?: string | null;
  factionColor?: string | null;
  hasFlagImage?: boolean;
  tintFlagImage?: boolean;
  allyGroupName?: string | null;
  territoryAndStructurePoints: number;
  battlesWonPoints: number;
  publicObjectivePoints: number;
  privateObjectivePoints?: number;
  otherPoints: number;
  total: number;
  territoryAndStructureSources?: CampaignPointSource[];
  battleSources?: CampaignPointSource[];
  publicObjectiveSources?: CampaignPointSource[];
  privateObjectiveSources?: CampaignPointSource[];
  otherSources?: CampaignPointSource[];
  heldItems?: HeldItemObjective[];
}

export interface CampaignPointSource {
  label: string;
  points: number;
}

export interface PublicObjectiveLeaderboard {
  kind: string;
  title?: string;
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
  tiedPlayerCount?: number;
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
  standardBattleResultQuestions?: SaveStandardBattleResultQuestionPayload[];
  missions?: SaveMissionPayload[];
  terrainTags?: CatalogTag[];
  structureTags?: CatalogTag[];
  factionTags?: CatalogTag[];
  missionTags?: CatalogTag[];
  forceStatuses?: SaveForceStatusPayload[];
  privateObjectiveTypes?: SavePrivateObjectiveTypePayload[];
  rivalObjectivesEnabled?: boolean;
  rivalObjectiveCampaignPoints?: number;
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
  mostStructurePointsCampaignPoints?: number;
  pointsPerTerritoryCampaignPoints?: number;
  alliedRelicControlCampaignPoints?: number;
  mostTerritoriesTerrainTagId?: string | null;
  longestTerritoryChainTerrainTagId?: string | null;
  mostStructurePointsStructureTagId?: string | null;
  pointsPerTerritoryTerrainTagId?: string | null;
  splitForceSupplyPenaltyPercent?: number;
  splitForceSupplyPenaltyIsPercent?: boolean;
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
  allyGroupId?: string | null;
  requiresSubfaction: boolean;
  clearFlagImage?: boolean;
  tintFlagImage?: boolean;
  specialRuleIds?: string[];
  subfactionSpecialRules?: SubfactionSpecialRules[];
  tagIds?: string[];
  subfactionTags?: SubfactionTags[];
  subfactionAppearances?: SaveSubfactionAppearancePayload[];
  forceMovementSpeed?: number;
  subfactionMovementSpeeds?: SubfactionMovementSpeed[];
}

export interface SaveSubfactionAppearancePayload {
  name: string;
  color?: string | null;
  flagSource?: SubfactionFlagSource;
  clearFlagImage?: boolean;
  tintFlagImage?: boolean;
}

export interface SaveAllyGroupPayload {
  id?: string;
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
  tagIds?: string[];
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
  tagIds?: string[];
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
  effects?: SaveItemObjectiveEffectPayload[];
}

export interface SaveItemObjectiveEffectPayload {
  id?: string;
  kind: string;
  amount?: number;
  amountIsPercent?: boolean;
  statusTypeIds?: string[];
  immuneToAllStatuses?: boolean;
  suspendCurrentAllyGroup?: boolean;
  forcedAllyGroupName?: string | null;
  alliedFactions?: ItemObjectiveAllianceTarget[];
  customText?: string | null;
  successStatusTypeId?: string | null;
  failureStatusTypeId?: string | null;
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
  setForceStatusName?: string | null;
}

export interface SaveSpecialRulePayload {
  id?: string;
  name: string;
  text?: string | null;
  effectKey?: string | null;
}

export interface SaveStandardBattleResultQuestionPayload {
  id?: string;
  prompt: string;
  kind?: string;
  battlePoints?: number;
  campaignPoints?: number;
}

export interface SaveForceStatusPayload {
  id?: string;
  name: string;
  effects?: string | null;
  enableConditions: ForceStatusCondition[];
  clearConditions: ForceStatusCondition[];
  enableTrigger?: string;
  clearTrigger?: string;
  enableOccurrences?: number;
  clearOccurrences?: number;
  priority: number;
  cancelsStatusIds?: string[];
  immuneFactionIds?: string[];
  immuneSubfactions?: ForceStatusImmuneSubfaction[];
  clearTokenImage?: boolean;
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
  matchesAnyStructureType?: boolean;
  itemObjectiveTypeId?: string | null;
  matchesAnyItemObjective?: boolean;
  targetKind?: string | null;
  targetSelection?: string | null;
  targetId?: string | null;
  forceStatusTypeIds?: string[];
  statusMatchKind?: string | null;
  prerequisiteForceStatusTypeId?: string | null;
  prerequisiteWasLost?: boolean;
  structureTagId?: string | null;
  terrainTagId?: string | null;
  excludedFactionIds?: string[];
  excludedAllyGroupIds?: string[];
}

export interface SaveMissionPayload {
  id?: string;
  name: string;
  url?: string | null;
  clearFile?: boolean;
  resultQuestions?: SaveMissionResultQuestionPayload[];
  statusChanges?: SaveMissionStatusChangePayload[];
  isAttackerDefender?: boolean;
  hasArmyPointsAdvantage?: boolean;
  armyPointsAdvantageSide?: string;
  armyPointsAdvantageIsPercent?: boolean;
  armyPointsAdvantageAmount?: number;
  hasSupplyPointsAdvantage?: boolean;
  supplyPointsAdvantageSide?: string;
  supplyPointsAdvantageAmount?: number;
  tagIds?: string[];
}

export interface SaveMissionResultQuestionPayload {
  id?: string;
  prompt: string;
  kind?: string;
  battlePoints?: number;
  campaignPoints?: number;
  standardQuestionId?: string | null;
}

export interface SaveMissionStatusChangePayload {
  id?: string;
  outcome: string;
  whenCurrentStatus?: string | null;
  setStatus?: string | null;
  leaveUnchanged?: boolean;
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
  ownerSubfaction?: string | null;
  spawnFactionId: string | null;
  spawnSubfaction?: string | null;
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
  /** Opaque cache tags for stored files, keyed by the helpers in `campaign-asset-tags`. */
  assetTags: Record<string, string>;
  factionId: string | null;
  canChooseFaction: boolean;
  isCommitted: boolean;
  viewerSupply?: PlayerSupplyView | null;
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
  rivalObjectives?: RivalObjectiveAssignment[];
  specialRules?: CampaignSpecialRule[];
  forceStatuses?: CampaignForceStatus[];
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
  mapTerritories?: PlayMapTerritory[];
}

export interface PlayMapTerritory {
  id: string;
  ownerFactionId: string | null;
  ownerSubfaction?: string | null;
  structureTypeId?: string | null;
  structureCondition?: string | null;
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
  moveHops?: PlayMoveHop[];
  availableActions: string[];
  statusName?: string | null;
  statusEffects?: string | null;
  subfaction?: string | null;
  canMoveTwoTerritories?: boolean;
  movementSpeed?: number;
  canDestroyImmediately?: boolean;
  canUseExtraBlackPowder?: boolean;
  canUseMagicalSupply?: boolean;
  hiddenRelicNearby?: boolean;
  battleReminders?: string[];
  supply?: PlayerSupplyView | null;
  canChooseTeleportDestination?: boolean;
  teleportTargets?: string[];
  isRandomTeleportLocked?: boolean;
  droppableItemObjectiveIds?: string[];
}

export interface PlayMoveHop {
  viaTerritoryId: string;
  targetTerritoryId: string;
  intermediateTerritoryIds?: string[];
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
  viaTerritoryId?: string | null;
  viaPath?: string[] | null;
  destroyImmediately?: boolean;
  droppedItemObjectiveIds?: string[];
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
  needsResult?: boolean;
  needsRetreat?: boolean;
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
  armyLists?: PlayBattleArmyList[];
  winnerForceId: string | null;
  isDraw: boolean;
  winnerScore?: number | null;
  loserScore?: number | null;
  needsRetreat: boolean;
  awaitingRetreat?: boolean;
  isRetreatCommitted?: boolean;
  isSurrenderCommitted?: boolean;
  retreatDraftTargetId?: string | null;
  retreatTargets?: string[];
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
  contributions?: SupplyContribution[];
}

export interface SupplyContribution {
  kind: string;
  territoryId?: string | null;
  label: string;
  points: number;
  isAllied?: boolean;
}

export interface PlayerSupplyView {
  currentSupplyPoints: number;
  temporarySupplyPoints: number;
  mapSupplyPoints: number;
  roundFreeSupplyPoints: number;
  splitPenaltyPoints: number;
  forceAllowancePoints: number;
  contributions?: SupplyContribution[];
}

export interface PlayBattleSubmission {
  submitterUserId: string;
  winnerForceId: string | null;
  isDraw: boolean;
  winnerScore?: number | null;
  loserScore?: number | null;
  reports?: BattleParticipantReport[];
  submittedUtc?: string | null;
}

export interface PlayBattleArmyList {
  forceId: string;
  submitterUserId: string;
  submittedUtc: string;
  armyPoints: number;
  supplyCostingUnitCount: number;
  armyListText?: string | null;
  armyListGameSystem?: string | null;
  armyListBuilder?: string;
  supplyCategories?: ArmyListSupplyCategory[];
}

export interface BattleParticipantReport {
  forceId: string;
  victoryPoints: number;
  armyPoints: number;
  differentialBattlePoints: number;
  bonusBattlePoints: number;
  supplyCostingUnitCount: number;
  usedExtraBlackPowder?: boolean;
  magicalSupplyRerolls?: number;
  armyListText?: string | null;
  armyListGameSystem?: string | null;
  armyListBuilder?: string | null;
  supplyCategories?: ArmyListSupplyCategory[];
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
  tintFlagImage?: boolean;
  allyGroupName?: string | null;
  currentSupplyPoints?: number | null;
  temporarySupplyPoints?: number | null;
  mapSupplyPoints?: number | null;
  roundFreeSupplyPoints?: number | null;
  maxArmyPoints?: number | null;
  freeCharacterCount?: number | null;
  splitPenaltyPoints?: number | null;
  contributions?: SupplyContribution[];
  traitorVictims?: TraitorVictim[];
}

export interface TraitorVictim {
  userId?: string | null;
  username?: string | null;
  displayName?: string | null;
  factionName: string;
  subfaction?: string | null;
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
  viaTerritoryId?: string | null;
  viaPath?: string[] | null;
  destroyImmediately?: boolean;
  droppedItemObjectiveIds?: string[] | null;
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
  reports?: BattleParticipantReport[];
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

export interface SetForceStatusesPayload {
  revision: number;
  forceIds?: string[];
  statusName?: string | null;
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
