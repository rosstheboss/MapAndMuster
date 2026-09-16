import { NgTemplateOutlet } from '@angular/common';
import {
  afterNextRender,
  Component,
  computed,
  DestroyRef,
  inject,
  Injector,
  signal,
  viewChild,
  type ElementRef,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { AuthService, isConcurrencyConflict, readApiError } from '../../core/auth/auth.service';
import {
  latestDelinquencyEntryForUser,
  mergeCampaignLog,
  type CampaignLogExportRequest,
  type CampaignLogSync,
} from '../../core/campaigns/campaign-log';
import { UpdateStreamService, type UpdateStreamSubscription } from '../../core/campaigns/update-stream.service';
import { ClockService } from '../../core/time/clock.service';
import { MAP_EDIT_CLOSED_MESSAGE, MAP_EDIT_CLOSED_QUERY } from '../../core/campaigns/campaign-notices';
import {
  actionKindLabel,
  actionNumberAt,
  battleStatusLabel,
  DURATION_UNITS,
  forceStatusLabel,
  forceStatusClearLabel,
  forceStatusEnableLabel,
  formatCountdown,
  formatDuration,
  formatPhaseEndTimestamp,
  formatPhaseLabel,
  statusLabel,
} from '../../core/campaigns/campaign-schedule';
import {
  CampaignViewPrefsService,
  DEFAULT_STANDINGS_SORT,
  defaultCampaignViewPrefs,
  nextStandingsSort,
  readStoredPrefs,
  sortStandings,
  type MapHighlightMode,
  type StandingsSort,
  type StandingsSortColumn,
} from '../../core/campaigns/campaign-view-prefs.service';
import {
  ITEM_OBJECTIVE_EFFECT_KINDS,
  missionsForTerritory,
  structureTypeById,
  terrainTypeById,
  type ArmyListSupplyCategory,
  type BattleParticipantReport,
  type CampaignChatSend,
  type CampaignDetail,
  type CampaignFaction,
  type CampaignForceStatus,
  type CampaignMission,
  type CampaignParticipant,
  type CampaignPlayDetail,
  type CampaignPointSource,
  type CampaignPointStanding,
  type CampaignSpecialRule,
  type MapGraphDetail,
  type MissionResultQuestion,
  type ParticipantDelinquency,
  type PlayBattle,
  type PlayBattleForceSupply,
  type PlayBattleSubmission,
  type PlayDraft,
  type PlayerSupplyView,
  type PlayForce,
  type PlayItemObjective,
  type PlayMoveHop,
  type PrivateObjectiveAssignment,
  type PublicObjectiveLeader,
  type PublicObjectiveLeaderboard,
  type RivalObjectiveAssignment,
  type TraitorVictim,
  type UserSearchHit,
} from '../../core/campaigns/campaign.models';
import { CampaignService } from '../../core/campaigns/campaign.service';
import { findSubfactionAppearance, resolveFactionAppearance } from '../../core/campaigns/faction-appearance';
import { compareNames } from '../../core/campaigns/faction-presets';
import { specialRuleNamesForFaction, specialRuleNamesForSubfaction } from '../../core/campaigns/special-rule-presets';
import { hidesForceStatusLocation, isWaterTagName } from '../../core/campaigns/force-status-presets';
import { FORM_SAVE_SUCCESS_MESSAGE } from '../../core/forms/form-messages';
import { FormSubmitOverlayService } from '../../core/forms/form-submit-overlay.service';
import { formatLocation } from '../../core/location/location';
import { adjacentTerritoryIds, findConnection } from '../../core/maps/adjacency';
import { downloadBlob, mapDownloadFilename, rasterizeMapPng } from '../../core/maps/map-export';
import {
  mapFactionOptionValue,
  parseMapFactionOptionValue,
  playerFactionOptions,
} from '../../core/maps/map-faction-options';
import type { MapGraph, MapTerritory } from '../../core/maps/map-graph.models';
import { normalizeStructureCondition, territoryLabel } from '../../core/maps/map-graph.models';
import { mapSvgCatalogFrom, serializeMapSvg, svgDownloadFilename } from '../../core/maps/map-svg';
import { BackToTopComponent } from '../../shared/back-to-top/back-to-top.component';
import { CampaignLogComponent } from '../../shared/campaign-log/campaign-log.component';
import {
  CampaignMapViewComponent,
  type MapForceAction,
  type MapForceMarker,
  type MapHeldItem,
  type MapItemMarker,
} from '../../shared/campaign-map-view/campaign-map-view.component';
import { ConfirmButtonComponent } from '../../shared/confirm-button/confirm-button.component';
import { AppDialogComponent } from '../../shared/dialog/dialog.component';
import { FactionLogoComponent } from '../../shared/faction-logo/faction-logo.component';
import { MapSymbolComponent } from '../../shared/map-symbol/map-symbol.component';
import { PhaseCountdownComponent } from '../../shared/phase-countdown/phase-countdown.component';
import { UpdateStreamStatusComponent } from '../../shared/update-stream-status/update-stream-status.component';
import { InstantDatePipe } from '../../shared/time/instant-date.pipe';
import { TraitorMarkComponent } from '../../shared/traitor-mark/traitor-mark.component';

const CAMPAIGN_SECTIONS = [
  'log',
  'participants',
  'faction',
  'itemObjectives',
  'missingFaction',
  'map',
  'privateObjectives',
  'orders',
  'battles',
  'debug',
  'ringer',
  'forceStatus',
  'schedule',
  'details',
  'factions',
  'forceStatusCatalog',
  'allies',
  'links',
  'standings',
  'end',
  'delete',
  'manage',
] as const;

type CampaignSection = (typeof CAMPAIGN_SECTIONS)[number];

interface AllyGroupPlayer {
  userId: string;
  displayName: string;
  factionId: string;
  factionName: string;
  subfaction: string | null;
  traitorVictims: TraitorVictim[];
}

interface FactionRosterPlayer {
  userId: string;
  displayName: string;
}

interface FactionRosterSubfaction {
  name: string;
  specialRules: CampaignSpecialRule[];
  players: FactionRosterPlayer[];
}

interface FactionRoster {
  faction: CampaignFaction;
  specialRules: CampaignSpecialRule[];
  players: FactionRosterPlayer[];
  subfactions: FactionRosterSubfaction[];
}

interface FactionSpawnPlace {
  territoryId: string;
  label: string;
  prefix: string;
  subfactionLabel: string;
  suffix: string;
}

interface CommitmentRosterEntry {
  userId: string;
  username: string | null;
  isCommitted: boolean;
  needsResult: boolean;
  needsRetreat: boolean;
  dutyLabel: string;
  faction: CampaignFaction | null;
  factionName: string | null;
  subfaction: string | null;
  locations: { territoryId: string; label: string }[];
}

interface OrderDraft {
  kind: string;
  targetTerritoryId: string;
  structureTypeId: string;
  viaTerritoryId: string;
  viaPath: string[];
  destroyImmediately: boolean;
  droppedItemObjectiveIds: string[];
}

interface MapActionFlow {
  step: 'menu' | 'pick-target' | 'pick-via' | 'pick-structure' | 'confirm';
  forceId: string;
  originId: string;
  kind: string;
  targetTerritoryId: string;
  viaTerritoryId: string;
  viaPath: string[];
  viaCandidates: string[];
  structureTypeId: string;
  droppedItemObjectiveIds: string[];
  menuX: number;
  menuY: number;
}

function battleDutyLabel(needsResult: boolean, needsRetreat: boolean): string | null {
  if (needsResult && needsRetreat) {
    return 'Needs result and retreat';
  }

  if (needsResult) {
    return 'Needs result';
  }

  if (needsRetreat) {
    return 'Needs retreat';
  }

  return null;
}

function emptyOrderDraft(overrides?: Partial<OrderDraft>): OrderDraft {
  return {
    kind: 'Hold',
    targetTerritoryId: '',
    structureTypeId: '',
    viaTerritoryId: '',
    viaPath: [],
    destroyImmediately: false,
    droppedItemObjectiveIds: [],
    ...overrides,
  };
}

const RUNNING_OPEN_SECTIONS: readonly CampaignSection[] = [
  'faction',
  'orders',
  'battles',
  'log',
  'standings',
  'debug',
  'ringer',
  'schedule',
  'end',
];

function defaultOpenSections(status: string): Record<CampaignSection, boolean> {
  if (status !== 'InProgress') {
    return openSections();
  }

  return Object.fromEntries(CAMPAIGN_SECTIONS.map((id) => [id, RUNNING_OPEN_SECTIONS.includes(id)])) as Record<
    CampaignSection,
    boolean
  >;
}

function openSections(): Record<CampaignSection, boolean> {
  return Object.fromEntries(CAMPAIGN_SECTIONS.map((id) => [id, id !== 'forceStatusCatalog'])) as Record<
    CampaignSection,
    boolean
  >;
}

@Component({
  selector: 'app-campaign-detail-page',
  imports: [
    FormsModule,
    NgTemplateOutlet,
    RouterLink,
    InstantDatePipe,
    BackToTopComponent,
    CampaignLogComponent,
    CampaignMapViewComponent,
    ConfirmButtonComponent,
    AppDialogComponent,
    MapSymbolComponent,
    FactionLogoComponent,
    TraitorMarkComponent,
    PhaseCountdownComponent,
    UpdateStreamStatusComponent,
  ],
  templateUrl: './campaign-detail.page.html',
  styleUrl: './campaign-detail.page.css',
})
export class CampaignDetailPage {
  private readonly campaignsApi = inject(CampaignService);
  private readonly updates = inject(UpdateStreamService);
  private readonly clock = inject(ClockService);
  private readonly overlay = inject(FormSubmitOverlayService);
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly viewPrefs = inject(CampaignViewPrefsService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);
  private readonly mapBoard = viewChild<ElementRef<HTMLElement>>('mapBoard');
  private readonly mapSection = viewChild<ElementRef<HTMLElement>>('mapSection');
  private readonly ordersSection = viewChild<ElementRef<HTMLElement>>('ordersSection');
  private readonly commitmentsBlock = viewChild<ElementRef<HTMLElement>>('commitmentsBlock');
  private readonly battlesBlock = viewChild<ElementRef<HTMLElement>>('battlesBlock');

  protected readonly loading = signal(true);
  protected readonly chatLoading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly pageNotice = signal<string | null>(null);
  protected readonly chatLoadError = signal<string | null>(null);
  protected readonly successMessage = signal<string | null>(null);
  protected readonly campaign = signal<CampaignDetail | null>(null);
  protected readonly logBoard = signal<CampaignLogSync | null>(null);
  protected readonly play = signal<CampaignPlayDetail | null>(null);
  protected readonly graph = signal<MapGraph>({ territories: [], adjacencies: [] });
  protected readonly hoveredTerritoryId = signal<string | null>(null);
  protected readonly selectedIds = signal<string[]>([]);
  protected readonly confirmingEnd = signal(false);
  protected readonly confirmingDelete = signal(false);
  protected readonly confirmingCommit = signal(false);
  protected readonly confirmingRetreatBattleId = signal<string | null>(null);
  protected readonly ending = signal(false);
  protected readonly deleting = signal(false);
  protected readonly nowMs = this.clock.nowMs;
  protected readonly downloading = signal(false);
  protected readonly downloadingLog = signal(false);
  protected readonly chatBusy = signal(false);
  protected readonly chatError = signal<string | null>(null);
  protected readonly openSections = signal(openSections());
  protected readonly openCatalogStatusIds = signal<ReadonlySet<string>>(new Set());
  protected readonly commitmentsRosterOpen = signal(false);
  protected readonly highlightMode = signal<MapHighlightMode>('configured');
  protected readonly standingsSort = signal<StandingsSort>({ ...DEFAULT_STANDINGS_SORT });
  protected readonly chatChannelKey = signal(defaultCampaignViewPrefs().chatChannelKey);
  protected readonly restoreChatScroll = signal<number | null>(null);
  protected readonly awardObjectiveId = signal('');
  protected readonly awardPlayerUserId = signal('');
  protected readonly grantHolderKind = signal('Player');
  protected readonly grantHolderId = signal('');
  protected readonly grantTypeId = signal('');
  private prefsHydrated = false;
  private storedSectionPrefs: Record<string, boolean> | null = null;
  private lastChatScrollTop = 0;
  private readonly campaignId = this.route.snapshot.paramMap.get('id');
  protected readonly durationUnits = DURATION_UNITS;
  protected readonly factionChoice = signal('');
  protected readonly memberQuery = signal('');
  protected readonly memberHits = signal<UserSearchHit[]>([]);
  protected readonly staffFactionId = signal<Partial<Record<string, string>>>({});
  protected readonly roundCount = signal(3);
  protected readonly extensionAmount = signal(1);
  protected readonly extensionUnit = signal('Hours');
  protected readonly extensionWindowId = signal('');
  protected readonly ringerForceId = signal('');
  protected readonly ringerFactionId = signal('');
  protected readonly ringerMissionId = signal('');
  protected readonly ringerPlayerIsDefender = signal(false);
  protected readonly forceStatusForceId = signal('');
  protected readonly forceStatusName = signal('Normal');
  protected readonly forceStatusAll = signal(false);
  protected readonly drafts = signal<Record<string, OrderDraft>>({});
  protected readonly debugDrafts = signal<Record<string, OrderDraft>>({});
  private readonly dirtyDraftForceIds = signal<ReadonlySet<string>>(new Set());
  private readonly dirtyDebugDraftForceIds = signal<ReadonlySet<string>>(new Set());
  protected readonly mapAction = signal<MapActionFlow | null>(null);
  protected readonly mapFocus = signal<{ kind: 'player' | 'faction' | 'ally'; id: string } | null>(null);
  protected readonly logScrollEntryId = signal<string | null>(null);
  protected readonly battleWinner = signal<Record<string, string>>({});
  protected readonly battleScores = signal<
    Partial<Record<string, { winnerScore: number | null; loserScore: number | null }>>
  >({});
  private readonly battleReports = signal<Record<string, BattleParticipantReport[]>>({});
  private readonly dirtyBattleResultIds = signal<ReadonlySet<string>>(new Set());
  private readonly dirtyArmyListKeys = signal<ReadonlySet<string>>(new Set());
  private readonly appliedResultKeys = signal<Record<string, string>>({});
  private readonly armyListParseMessages = signal<Record<string, string>>({});
  private readonly armyListParseTimers = new Map<string, ReturnType<typeof setTimeout>>();
  protected readonly retreatTarget = signal<Record<string, string>>({});
  private readonly expandedBattles = signal<Record<string, boolean>>({});
  private readonly expandedBattlePanels = signal<Record<string, boolean>>({});
  protected readonly updateStream = signal<UpdateStreamSubscription | null>(null);

  constructor() {
    const id = this.campaignId;
    const releaseClock = this.clock.subscribe();
    this.destroyRef.onDestroy(() => {
      releaseClock();
      this.persistViewPrefs();
      for (const timer of this.armyListParseTimers.values()) {
        globalThis.clearTimeout(timer);
      }
    });
    if (id) {
      this.applyMapEditNotice();
      void this.load(id);
      void this.loadChat(id);
    } else {
      this.error.set('The campaign was not found.');
      this.loading.set(false);
      this.chatLoading.set(false);
    }
  }

  protected readonly mapSrc = computed(() => {
    const campaign = this.campaign();
    if (!campaign?.hasMap) {
      return null;
    }

    return this.campaignsApi.mapUrl(campaign.id, campaign.assetTags);
  });

  protected hoveredTerritory = computed(() => {
    const id = this.hoveredTerritoryId() ?? this.selectedIds().at(-1) ?? null;
    return this.graph().territories.find((territory) => territory.id === id) ?? null;
  });
  protected readonly adjacentTerritoryIds = computed(() => {
    const flow = this.mapAction();
    if (flow?.step === 'pick-target') {
      const force = this.myForces().find((item) => item.id === flow.forceId);
      return force ? this.destinationTargets(force, flow.kind) : [];
    }

    if (flow?.step === 'pick-via') {
      return flow.viaCandidates;
    }

    if (!flow && this.pendingRetreatTargets().length > 0) {
      return this.pendingRetreatTargets();
    }

    return adjacentTerritoryIds(this.graph().adjacencies, this.selectedIds());
  });
  protected readonly factionAssignmentOptions = computed(() => playerFactionOptions(this.campaign()?.factions ?? []));
  protected readonly chosenFaction = computed(() => {
    const campaign = this.campaign();
    if (!campaign?.factionId) {
      return null;
    }

    return campaign.factions.find((faction) => faction.id === campaign.factionId) ?? null;
  });
  protected readonly myForces = computed(() => this.play()?.forces.filter((force) => force.isMine) ?? []);
  protected readonly relicNearbyNotices = computed(() =>
    this.myForces()
      .filter((force) => force.hiddenRelicNearby)
      .map((force) => ({
        forceId: force.id,
        territoryName: this.territoryName(force.territoryId),
        glowColor: this.forceAccent(force),
      })),
  );
  protected readonly anyForceHasChainSupply = computed(() => this.myForces().some((force) => !!force.supply));
  protected readonly orderableForces = computed(() =>
    this.myForces().filter(
      (force) => !force.inBattle && force.isRandomTeleportLocked !== true && force.availableActions.length > 0,
    ),
  );
  protected readonly canCommitActions = computed(() => {
    const play = this.play();
    const forces = this.orderableForces();
    if (!play || play.isCommitted) {
      return false;
    }

    if (forces.length === 0) {
      return this.myForces().some((force) => !force.inBattle && force.isRandomTeleportLocked === true);
    }

    return forces.every((force) => play.myDrafts.some((draft) => draft.forceId === force.id));
  });
  protected readonly isFinalRequiredCommitment = computed(() => {
    const play = this.play();
    const viewerId = this.auth.currentUser()?.id;
    if (!play || play.isCommitted || !viewerId) {
      return false;
    }

    const others = play.commitments.filter((item) => item.userId !== viewerId);
    return others.every((item) => item.isCommitted);
  });
  protected readonly canUncommitActions = computed(() => {
    const play = this.play();
    return (
      !!play?.isCommitted &&
      play.currentPhaseKind === 'Action' &&
      !!play.currentWindowId &&
      this.orderableForces().length > 0
    );
  });
  protected readonly canUncommit = computed(() => this.canUncommitActions());
  protected readonly commitmentSummary = computed(() => {
    const play = this.play();
    if (!play) {
      return null;
    }

    const total = play.commitments.length;
    const committed = play.commitments.filter((item) => item.isCommitted).length;
    const waiting = play.commitments
      .filter((item) => !item.isCommitted)
      .map((item) => {
        const name = item.username ?? item.userId;
        const duties = battleDutyLabel(item.needsResult === true, item.needsRetreat === true);
        return play.currentPhaseKind === 'Battle' && duties ? `${name} (${duties.toLowerCase()})` : name;
      });
    const finishedNoun = play.currentPhaseKind === 'Battle' ? 'finished results and retreats' : 'committed';
    return {
      committed,
      total,
      waiting,
      text:
        waiting.length === 0
          ? `${committed} of ${total} players ${finishedNoun}.`
          : play.currentPhaseKind === 'Battle'
            ? `${committed} of ${total} players finished. Waiting on ${waiting.join(', ')}.`
            : `${committed} of ${total} players committed. Waiting on ${waiting.join(', ')}.`,
    };
  });
  protected readonly commitmentEntries = computed((): CommitmentRosterEntry[] => {
    const play = this.play();
    const campaign = this.campaign();
    if (!play) {
      return [];
    }

    return play.commitments
      .map((item) => {
        const participant = campaign?.participants?.find((member) => member.userId === item.userId);
        const forces = play.forces.filter((force) => force.controllerUserId === item.userId);
        const force = forces.find((entry) => firstNonEmpty(entry.subfaction) !== null) ?? forces.at(0);
        const faction =
          (participant ? resolveParticipantFaction(participant, campaign?.factions ?? []) : null) ??
          (force ? (campaign?.factions.find((entry) => entry.id === force.factionId) ?? null) : null);
        const factionName = firstNonEmpty(
          participant?.factionName,
          faction?.name,
          namedFaction(force ? this.factionName(force.factionId) : null),
        );
        const subfaction = firstNonEmpty(participant?.subfaction, force?.subfaction);
        const locations: CommitmentRosterEntry['locations'] = [];
        const seen = new Set<string>();
        for (const owned of forces) {
          if (seen.has(owned.territoryId)) {
            continue;
          }

          seen.add(owned.territoryId);
          locations.push({ territoryId: owned.territoryId, label: this.territoryName(owned.territoryId) });
        }

        const needsResult = item.needsResult === true;
        const needsRetreat = item.needsRetreat === true;
        return {
          userId: item.userId,
          username: item.username,
          isCommitted: item.isCommitted,
          needsResult,
          needsRetreat,
          dutyLabel: item.isCommitted ? 'Committed' : (battleDutyLabel(needsResult, needsRetreat) ?? 'Drafting'),
          faction,
          factionName,
          subfaction,
          locations,
        };
      })
      .sort((left, right) => compareNames(left.username ?? left.userId, right.username ?? right.userId));
  });
  protected readonly pendingBattleDuties = computed(() => this.commitmentEntries().filter((item) => !item.isCommitted));
  protected readonly viewerCommitChip = computed(() => {
    const play = this.play();
    if (!play?.isParticipant || play.canChooseFaction) {
      return null;
    }

    const viewerId = this.auth.currentUser()?.id;
    const self = viewerId ? play.commitments.find((item) => item.userId === viewerId) : undefined;
    if (self) {
      return self.isCommitted ? 'Committed' : 'Not committed';
    }

    if (play.currentPhaseKind === 'Battle') {
      return null;
    }

    const mine = this.myForces();
    if (mine.length > 0 && mine.every((force) => force.inBattle)) {
      return 'Committed';
    }

    return play.isCommitted ? 'Committed' : 'Not committed';
  });
  protected readonly countdownUrgency = computed(() => {
    const endsUtc = this.play()?.currentPhaseEndsUtc ?? this.campaign()?.currentPhaseEndsUtc;
    if (!endsUtc) {
      return null;
    }

    const remaining = Date.parse(endsUtc) - this.nowMs();
    if (!Number.isFinite(remaining) || remaining <= 0) {
      return 'danger';
    }

    if (remaining <= 2 * 60 * 60 * 1000) {
      return 'danger';
    }

    if (remaining <= 24 * 60 * 60 * 1000) {
      return 'dirty';
    }

    return null;
  });
  protected readonly announcedCountdown = computed(() => {
    const endsUtc = this.play()?.currentPhaseEndsUtc ?? this.campaign()?.currentPhaseEndsUtc;
    return endsUtc ? formatCountdown(endsUtc, this.nowMs()) : '';
  });
  protected readonly battleStatusLabel = battleStatusLabel;

  protected battleDisplayStatus(battle: PlayBattle): string {
    if (battle.awaitingRetreat) {
      return 'Awaiting Retreat Order.';
    }

    return battleStatusLabel(battle.status);
  }

  protected readonly buildableStructures = computed(() =>
    (this.play()?.structureTypes ?? this.campaign()?.structureTypes ?? []).filter((type) => type.isBuildable),
  );
  protected readonly isActionPhase = computed(() => this.play()?.currentPhaseKind === 'Action');
  protected readonly showMapCommit = computed(() => {
    const play = this.play();
    if (!play?.isParticipant || play.canChooseFaction) {
      return false;
    }

    if (this.isActionPhase() && (!play.isCommitted || this.canUncommitActions())) {
      return true;
    }

    return this.canUncommitRetreat() || this.hasMapBattleCommit();
  });
  protected readonly mapCommitNoun = computed(() => {
    if (this.canUncommitActions()) {
      return 'Actions';
    }

    const battles = this.play()?.battles ?? [];
    if (battles.some((battle) => battle.canSurrender === true || battle.isSurrenderCommitted === true)) {
      return 'Surrender';
    }

    if (
      battles.some(
        (battle) =>
          battle.needsRetreat === true || battle.isRetreatCommitted === true || battle.isSurrenderCommitted === true,
      )
    ) {
      return 'Retreat';
    }

    return 'Actions';
  });
  protected readonly mapCanCommit = computed(
    () => this.canCommitActions() || this.canCommitSurrender() || this.canCommitAnyRetreat(),
  );
  protected readonly mapShowUncommit = computed(() => this.canUncommitActions() || this.canUncommitRetreat());
  protected readonly mapCommitClosesPhase = computed(() => {
    if (this.canUncommitActions() || this.canUncommitRetreat()) {
      return false;
    }

    const surrender = this.pendingSurrenderBattle();
    if (surrender) {
      return this.isFinalRequiredBattleCommitment(surrender);
    }

    const retreat = this.pendingRetreatBattle();
    if (retreat) {
      return this.isFinalRequiredBattleCommitment(retreat);
    }

    return this.isFinalRequiredCommitment();
  });
  protected readonly isBattlePhase = computed(() => this.play()?.currentPhaseKind === 'Battle');
  protected readonly hasOpenBattles = computed(() => (this.play()?.battles.length ?? 0) > 0);
  protected readonly canDebug = computed(() => this.play()?.canDebug === true);
  protected readonly isDebugActive = computed(() => this.play()?.isDebugActive === true);
  protected readonly showSpawnLocation = computed(() => {
    const status = this.campaign()?.status;
    return status === 'Scheduled' || status === 'InProgress';
  });
  protected readonly spawnLocation = computed(() => {
    const campaign = this.campaign();
    const parsed = parseMapFactionOptionValue(this.factionChoice());
    const factionId = parsed.factionId || campaign?.factionId;
    const subfaction = parsed.factionId ? parsed.subfaction : (campaign?.subfaction ?? null);
    if (!factionId) {
      return null;
    }

    const assigned = this.graph().territories.filter((item) => item.spawnFactionId === factionId);
    const wanted = subfaction?.trim().toLowerCase() ?? '';
    const match = wanted
      ? (assigned.find((item) => namedSpawnSubfaction(item)?.toLowerCase() === wanted) ??
        assigned.find((item) => namedSpawnSubfaction(item) === null))
      : (assigned.find((item) => namedSpawnSubfaction(item) === null) ?? assigned[0]);
    return match ? { id: match.id, label: territoryLabel(match) } : null;
  });
  protected readonly focusedForceIds = computed(() => {
    const focus = this.mapFocus();
    return focus ? this.forcesForFocus(focus).map((force) => force.id) : [];
  });
  protected readonly mapForces = computed((): MapForceMarker[] => {
    const play = this.play();
    const campaign = this.campaign();
    if (!play) {
      return [];
    }

    const items = play.itemObjectives ?? [];
    return play.forces.map((force) => {
      const participant = campaign?.participants?.find((member) => member.userId === force.controllerUserId);
      return {
        id: force.id,
        territoryId: force.territoryId,
        factionId: force.factionId,
        subfaction: force.subfaction ?? participant?.subfaction ?? null,
        isMine: force.isMine,
        inBattle: force.inBattle,
        name: this.forceLabel(force),
        label: `${this.forceLabel(force)} in ${this.territoryName(force.territoryId)}`,
        heldItems: items
          .filter((item) => item.possessorForceId === force.id)
          .map((item) => this.toHeldMapItem(item, campaign)),
        action: this.ownForceMapAction(force),
        ...this.forceOrderRouteFields(force),
        moveTargets: force.moveTargets,
        hiddenRelicNearby: !!force.hiddenRelicNearby,
        isTeleporting: this.isForceTeleporting(force),
      };
    });
  });
  protected readonly mapBattles = computed(() => {
    return (this.play()?.battles ?? []).map((battle) => ({
      territoryId: battle.territoryId,
      status: battle.status,
      participantForceIds: battle.participantForceIds,
      winnerForceId: battle.winnerForceId,
      isDraw: battle.isDraw,
      isNoContest: battle.isNoContest,
    }));
  });
  protected readonly mapItems = computed((): MapItemMarker[] => {
    const play = this.play();
    const campaign = this.campaign();
    if (!play) {
      return [];
    }

    return (play.itemObjectives ?? []).flatMap((item) => {
      if (item.possessorForceId || !item.territoryId) {
        return [];
      }

      return [
        {
          id: item.id,
          territoryId: item.territoryId,
          name: item.name,
          carried: false,
          hidden: !item.isRevealed,
          builtinSymbol: item.builtinSymbol ?? 'Crown',
          color: item.color ?? '#C45C26',
          imageUrl: this.itemObjectiveImageSrc(item, campaign),
        },
      ];
    });
  });
  protected readonly visibleItemObjectives = computed(() => this.play()?.itemObjectives ?? []);
  protected readonly hiddenItemCount = computed(
    () => (this.play()?.itemObjectives ?? []).filter((item) => !item.isRevealed).length,
  );
  protected readonly sortedStandings = computed(() =>
    sortStandings(this.play()?.standings ?? this.campaign()?.standings ?? [], this.standingsSort()),
  );
  protected readonly showCampaignPoints = computed(() => {
    const status = this.play()?.status ?? this.campaign()?.status;
    return (status ?? 'Scheduled') !== 'Scheduled';
  });
  protected readonly listedAllyGroups = computed(() => {
    const campaign = this.campaign();
    if (!campaign) {
      return [];
    }

    const brokenAllyFactionIds = new Set(this.play()?.brokenAllyFactionIds ?? campaign.brokenAllyFactionIds ?? []);

    return [...campaign.allyGroups]
      .sort((left, right) => compareNames(left.name, right.name))
      .map((group) => {
        const factions = campaign.factions.filter((faction) => factionBelongsToAllyGroup(faction, group));
        const players = allyGroupPlayers(campaign, factions, brokenAllyFactionIds);
        return {
          group,
          members: allyGroupMemberFactions(factions),
          players,
        };
      });
  });
  protected readonly listedFactions = computed(() => {
    const campaign = this.campaign();
    if (!campaign) {
      return [];
    }

    const playFactions = this.play()?.factions ?? [];
    const catalog = mergeSpecialRuleCatalogs(this.play()?.specialRules, campaign.specialRules);
    return campaign.factions.map((faction) =>
      factionRoster(
        campaign,
        factionWithPlayRules(
          faction,
          playFactions.find((item) => item.id === faction.id),
        ),
        (ids) => this.specialRulesFor(ids),
        catalog,
      ),
    );
  });
  protected readonly forceStatusCatalog = computed(() => {
    const campaign = this.campaign();
    if (!campaign) {
      return [];
    }

    const statuses = [...(campaign.forceStatuses ?? [])].sort(
      (left, right) => (left.priority ?? 0) - (right.priority ?? 0),
    );
    const names = new Map(statuses.map((status) => [status.id, status.name] as const));
    const factions = new Map(campaign.factions.map((faction) => [faction.id, faction] as const));
    const forces = this.play()?.forces ?? [];
    const rosters = this.listedFactions();
    return statuses.map((status) => ({
      status,
      enableConditions:
        status.enableConditions && status.enableConditions.length > 0
          ? status.enableConditions
          : status.enableTrigger
            ? [{ trigger: status.enableTrigger, occurrences: status.enableOccurrences ?? 1 }]
            : [],
      clearConditions:
        status.clearConditions && status.clearConditions.length > 0
          ? status.clearConditions
          : status.clearTrigger
            ? [{ trigger: status.clearTrigger, occurrences: status.clearOccurrences ?? 1 }]
            : [],
      cancelNames: (status.cancelsStatusIds ?? []).map((id) => names.get(id)).filter((name): name is string => !!name),
      immuneFactions: (status.immuneFactionIds ?? []).flatMap((id) => {
        const faction = factions.get(id);
        return faction ? [{ id, name: faction.name }] : [];
      }),
      immuneSubfactions: (status.immuneSubfactions ?? []).flatMap((item) => {
        const faction = factions.get(item.factionId);
        return faction ? [{ label: `${faction.name} — ${item.subfaction}` }] : [];
      }),
      forces: forces
        .filter((force) => compareNames(force.statusName ?? '', status.name) === 0)
        .map((force) => {
          const roster = rosters.find((entry) => entry.faction.id === force.factionId);
          const players = [
            firstNonEmpty(force.controllerUsername) ?? 'Player',
            ...(roster?.players.map((player) => player.displayName) ?? []),
            ...(roster?.subfactions.flatMap((subfaction) => subfaction.players.map((player) => player.displayName)) ??
              []),
          ].filter((name, index, listed) => listed.findIndex((item) => compareNames(item, name) === 0) === index);
          return {
            force,
            factionName: this.factionName(force.factionId),
            players,
          };
        }),
    }));
  });
  protected readonly chosenSpecialRules = computed(() => {
    const parsed = parseMapFactionOptionValue(this.factionChoice());
    const campaign = this.campaign();
    const factionId = parsed.factionId || campaign?.factionId;
    const subfaction = parsed.factionId ? parsed.subfaction : (campaign?.subfaction ?? null);
    if (!campaign || !factionId) {
      return [];
    }

    const catalogFaction = campaign.factions.find((item) => item.id === factionId);
    if (!catalogFaction) {
      return [];
    }

    const faction = factionWithPlayRules(
      catalogFaction,
      this.play()?.factions.find((item) => item.id === catalogFaction.id),
    );
    const catalog = mergeSpecialRuleCatalogs(this.play()?.specialRules, campaign.specialRules);
    return displaySpecialRulesFor(faction, catalog, (ids) => this.specialRulesFor(ids), subfaction);
  });
  protected readonly pendingRetreatTargets = computed(() => {
    const battles = this.play()?.battles ?? [];
    const targets = new Set<string>();
    for (const battle of battles) {
      if (!battle.needsRetreat) {
        continue;
      }

      for (const target of battle.retreatTargets ?? []) {
        targets.add(target);
      }
    }

    return [...targets];
  });
  protected readonly leaderboards = computed(
    () => this.play()?.publicObjectiveLeaderboards ?? this.campaign()?.publicObjectiveLeaderboards ?? [],
  );
  protected readonly awardableObjectives = computed(() =>
    (this.campaign()?.publicObjectiveTypes ?? []).filter((objective) => objective.campaignPoints > 0),
  );
  protected readonly visiblePrivateObjectives = computed(
    () => this.play()?.privateObjectives ?? this.campaign()?.privateObjectives ?? [],
  );
  protected readonly myPrivateObjectives = computed(() => {
    const campaign = this.campaign();
    const userId = this.auth.currentUser()?.id;
    if (!campaign || !userId) {
      return [];
    }

    return this.visiblePrivateObjectives().filter((assignment) => isOwnPrivateAssignment(assignment, campaign, userId));
  });
  protected readonly myUnclaimedPrivateObjectives = computed(() =>
    this.myPrivateObjectives().filter(
      (assignment) => assignment.status === 'Assigned' || assignment.status === 'Claimed',
    ),
  );
  protected readonly myActiveRivalObjectives = computed(() =>
    this.myRivalObjectives().filter((assignment) => assignment.status === 'Assigned'),
  );
  protected readonly othersClaimedPrivateObjectives = computed(() => {
    const campaign = this.campaign();
    const userId = this.auth.currentUser()?.id;
    if (!campaign || !userId) {
      return [];
    }

    return this.visiblePrivateObjectives()
      .filter((assignment) => {
        if (isOwnPrivateAssignment(assignment, campaign, userId)) {
          return false;
        }

        return assignment.status === 'Revealed' || (assignment.status === 'Claimed' && assignment.canModerate === true);
      })
      .sort((left, right) =>
        compareNames(privateObjectiveFactionName(left, campaign), privateObjectiveFactionName(right, campaign)),
      );
  });
  protected privateObjectiveHolderCaption(assignment: PrivateObjectiveAssignment): string {
    const campaign = this.campaign();
    return campaign ? privateObjectiveHolderLabel(assignment, campaign) : '';
  }
  protected isViewerPrivateAssignment(assignment: PrivateObjectiveAssignment): boolean {
    const campaign = this.campaign();
    const userId = this.auth.currentUser()?.id;
    return !!campaign && !!userId && isOwnPrivateAssignment(assignment, campaign, userId);
  }
  protected readonly visibleRivalObjectives = computed(
    () => this.play()?.rivalObjectives ?? this.campaign()?.rivalObjectives ?? [],
  );
  protected readonly myRivalObjectives = computed(() => {
    const userId = this.auth.currentUser()?.id;
    if (!userId) {
      return [];
    }

    return this.visibleRivalObjectives().filter((assignment) => assignment.holderUserId === userId);
  });
  protected readonly othersRevealedRivals = computed(() => {
    const userId = this.auth.currentUser()?.id;
    return this.visibleRivalObjectives().filter(
      (assignment) => assignment.status === 'Revealed' && assignment.holderUserId !== userId,
    );
  });
  protected rivalHolderCaption(assignment: RivalObjectiveAssignment): string {
    const campaign = this.campaign();
    return (
      campaign?.participants?.find((participant) => participant.userId === assignment.holderUserId)?.displayName ??
      'A player'
    );
  }
  protected rivalTargetCaption(assignment: RivalObjectiveAssignment): string | null {
    const identity = this.rivalIdentity(assignment);
    if (!identity) {
      return null;
    }

    const { name, faction, subfaction } = identity;
    if (faction && subfaction) {
      return `${name} (${faction} — ${subfaction})`;
    }

    if (faction) {
      return `${name} (${faction})`;
    }

    return name;
  }
  private rivalIdentity(
    assignment: RivalObjectiveAssignment,
  ): { name: string; faction: string | null; subfaction: string | null } | null {
    const userId = assignment.rivalUserId?.trim();
    const participant = userId ? this.campaign()?.participants?.find((item) => item.userId === userId) : undefined;
    const name = firstNonEmpty(assignment.rivalDisplayName, participant?.displayName, participant?.username);
    if (!name) {
      return null;
    }

    const force = rivalForce(this.play(), userId);
    const standing = userId
      ? (this.play()?.standings ?? this.campaign()?.standings ?? []).find((row) => row.userId === userId)
      : undefined;
    return {
      name,
      faction: firstNonEmpty(
        assignment.rivalFactionName,
        participant?.factionName,
        namedFaction(force ? this.factionName(force.factionId) : null),
        standing?.factionName,
      ),
      subfaction: firstNonEmpty(assignment.rivalSubfaction, participant?.subfaction, force?.subfaction),
    };
  }
  protected readonly grantHolders = computed(() => {
    const campaign = this.campaign();
    if (!campaign) {
      return [];
    }

    const kind = this.grantHolderKind();
    if (kind === 'Faction') {
      return campaign.factions.map((faction) => ({ id: faction.id, name: faction.name }));
    }

    if (kind === 'AllyGroup') {
      return campaign.allyGroups.map((group) => ({ id: group.id, name: group.name }));
    }

    return (campaign.participants ?? [])
      .filter((participant) => participant.isPlayer)
      .map((participant) => ({ id: participant.userId, name: participant.displayName }));
  });
  protected readonly grantablePrivateTypes = computed(() =>
    (this.campaign()?.privateObjectiveTypes ?? []).filter((type) => !!type.name),
  );
  protected readonly usesDifferentialScoring = computed(
    () => this.play()?.useDifferentialBattleScoring ?? this.campaign()?.useDifferentialBattleScoring ?? true,
  );

  protected isOpen(id: CampaignSection): boolean {
    return this.openSections()[id] !== false;
  }

  protected toggleSection(id: CampaignSection): void {
    this.openSections.update((current) => ({ ...current, [id]: !current[id] }));
    this.persistViewPrefs();
  }

  protected setSection(id: CampaignSection, open: boolean): void {
    this.openSections.update((current) => ({ ...current, [id]: open }));
    this.persistViewPrefs();
  }

  protected isCatalogStatusOpen(statusId: string): boolean {
    return this.openCatalogStatusIds().has(statusId);
  }

  protected toggleCatalogStatus(statusId: string): void {
    this.openCatalogStatusIds.update((current) => {
      const next = new Set(current);
      if (next.has(statusId)) {
        next.delete(statusId);
      } else {
        next.add(statusId);
      }

      return next;
    });
  }

  protected forceStatusConditionLabel(
    condition: { trigger: string; occurrences?: number },
    side: 'enable' | 'clear',
  ): string {
    const trigger =
      side === 'enable' ? forceStatusEnableLabel(condition.trigger) : forceStatusClearLabel(condition.trigger);
    const times = condition.occurrences && condition.occurrences > 1 ? ` ×${condition.occurrences}` : '';
    return `${trigger}${times}`;
  }

  protected forceStatusConditionDetail(condition: {
    trigger: string;
    locationKind?: string;
    locationTypeId?: string | null;
    locationTagId?: string | null;
    requiredStatusId?: string | null;
    requiredQuestionId?: string | null;
  }): string | null {
    const parts: string[] = [];
    if (!hidesForceStatusLocation(condition.trigger)) {
      const location = this.forceStatusLocationCaption(condition);
      if (location) {
        parts.push(location);
      }
    }

    if (condition.requiredStatusId) {
      const name = (this.campaign()?.forceStatuses ?? []).find(
        (status) => status.id === condition.requiredStatusId,
      )?.name;
      if (name) {
        parts.push(`occupying ${name}`);
      }
    }

    if (condition.requiredQuestionId) {
      const prompt = (this.campaign()?.standardBattleResultQuestions ?? []).find(
        (question) => question.id === condition.requiredQuestionId,
      )?.prompt;
      if (prompt) {
        parts.push(prompt);
      }
    }

    return parts.length > 0 ? parts.join(' · ') : null;
  }

  protected forceStatusTokenSrc(status: CampaignForceStatus): string | null {
    const campaign = this.campaign();
    if (!campaign || !status.hasTokenImage) {
      return null;
    }

    return this.campaignsApi.forceStatusTokenUrl(campaign.id, status.id, campaign.assetTags);
  }

  private forceStatusLocationCaption(condition: {
    locationKind?: string;
    locationTypeId?: string | null;
    locationTagId?: string | null;
  }): string | null {
    const campaign = this.campaign();
    if (!campaign) {
      return null;
    }

    if (condition.locationKind === 'TerrainType') {
      const name = campaign.terrainTypes.find((item) => item.id === condition.locationTypeId)?.name;
      return name ? `terrain ${name}` : 'a terrain type';
    }

    if (condition.locationKind === 'StructureType') {
      const name = campaign.structureTypes.find((item) => item.id === condition.locationTypeId)?.name;
      return name ? `structure ${name}` : 'a structure type';
    }

    if (condition.locationKind === 'TerrainTag') {
      const name = campaign.terrainTags?.find((item) => item.id === condition.locationTagId)?.name;
      return name ? `terrain tag ${name}` : 'a terrain tag';
    }

    if (condition.locationKind === 'StructureTag') {
      const name = campaign.structureTags?.find((item) => item.id === condition.locationTagId)?.name;
      return name ? `structure tag ${name}` : 'a structure tag';
    }

    return null;
  }

  protected delinquencyEntryId(userId: string): string | null {
    return latestDelinquencyEntryForUser(this.logBoard()?.log ?? [], this.play()?.forces ?? [], userId)?.id ?? null;
  }

  protected openDelinquencyLog(entryId: string): void {
    this.setSection('log', true);
    this.logScrollEntryId.set(entryId);
  }

  private applyMapEditNotice(): void {
    const notice = this.route.snapshot.queryParamMap.get('notice');
    if (notice !== MAP_EDIT_CLOSED_QUERY) {
      return;
    }

    this.pageNotice.set(MAP_EDIT_CLOSED_MESSAGE);
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { notice: null },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  protected profileFrom(): { from: string } {
    return { from: this.router.url };
  }

  protected participantRoles(participant: CampaignParticipant): string {
    const roles: string[] = [];
    if (participant.isGameMaster) {
      roles.push('Manager');
    }

    if (participant.isPlayer) {
      roles.push('Player');
    }

    if (participant.isAdministrator) {
      roles.push('Admin');
    }

    return roles.join(', ');
  }

  protected delinquencyPhaseLabel(offence: ParticipantDelinquency): string {
    const kind = offence.phaseKind === 'Battle' ? 'Battle' : 'Action';
    return `Round ${offence.roundNumber}, ${kind} ${offence.kindOrdinal}`;
  }

  protected canStaffMembers(): boolean {
    const campaign = this.campaign();
    const user = this.auth.currentUser();
    return Boolean(campaign && (campaign.canManage || user?.isAdministrator));
  }

  protected ringerTargetForces(): PlayForce[] {
    const play = this.play();
    if (play?.currentPhaseKind !== 'Battle') {
      return [];
    }

    const spawnIds = new Set(
      this.graph()
        .territories.filter((territory) => Boolean(territory.spawnFactionId))
        .map((territory) => territory.id),
    );
    return play.forces.filter((force) => !force.isMine && !force.inBattle && !spawnIds.has(force.territoryId));
  }

  protected async searchMembers(): Promise<void> {
    const campaign = this.campaign();
    const query = this.memberQuery().trim();
    if (!campaign || query.length < 2) {
      this.memberHits.set([]);
      return;
    }

    try {
      this.memberHits.set(await this.campaignsApi.searchUsers(campaign.id, query));
    } catch (error: unknown) {
      this.error.set(readApiError(error, 'Unable to search users.'));
    }
  }

  protected async addMember(
    hit: UserSearchHit,
    options: { isGameMaster?: boolean; isPlayer?: boolean } = {},
  ): Promise<void> {
    const campaign = this.campaign();
    if (!campaign) {
      return;
    }

    this.error.set(null);
    this.successMessage.set(null);
    try {
      await this.overlay.run(() => this.campaignsApi.addMember(campaign.id, hit.userId, campaign.revision, options));
      this.memberQuery.set('');
      this.memberHits.set([]);
      await this.load(campaign.id);
      this.successMessage.set(FORM_SAVE_SUCCESS_MESSAGE);
    } catch (error: unknown) {
      this.error.set(readApiError(error, 'Unable to add that member.'));
    }
  }

  protected async promoteMember(participant: CampaignParticipant): Promise<void> {
    const campaign = this.campaign();
    if (!campaign) {
      return;
    }

    this.error.set(null);
    this.successMessage.set(null);
    try {
      await this.overlay.run(() =>
        this.campaignsApi.addMember(campaign.id, participant.userId, campaign.revision, {
          isGameMaster: true,
          isPlayer: true,
        }),
      );
      await this.load(campaign.id);
      this.successMessage.set(FORM_SAVE_SUCCESS_MESSAGE);
    } catch (error: unknown) {
      this.error.set(readApiError(error, 'Unable to make that player a campaign manager.'));
    }
  }

  protected async kickMember(participant: CampaignParticipant): Promise<void> {
    const campaign = this.campaign();
    if (!campaign) {
      return;
    }

    this.error.set(null);
    this.successMessage.set(null);
    try {
      await this.overlay.run(() => this.campaignsApi.kickMember(campaign.id, participant.userId, campaign.revision));
      await this.load(campaign.id);
      this.successMessage.set(FORM_SAVE_SUCCESS_MESSAGE);
    } catch (error: unknown) {
      this.error.set(readApiError(error, 'Unable to remove that player.'));
    }
  }

  protected staffFactionValue(participant: CampaignParticipant): string {
    const pending = this.staffFactionId()[participant.userId];
    if (pending !== undefined) {
      return pending;
    }

    if (!participant.factionId) {
      return '';
    }

    return mapFactionOptionValue(participant.factionId, participant.subfaction);
  }

  protected onStaffFaction(participant: CampaignParticipant, value: string): Promise<void> {
    this.staffFactionId.update((current) => ({ ...current, [participant.userId]: value }));
    const saved = participant.factionId ? mapFactionOptionValue(participant.factionId, participant.subfaction) : '';
    if (value && value !== saved) {
      return this.assignFaction(participant);
    }

    return Promise.resolve();
  }

  protected async assignFaction(participant: CampaignParticipant): Promise<void> {
    const campaign = this.campaign();
    const parsed = parseMapFactionOptionValue(this.staffFactionValue(participant));
    if (!campaign || !parsed.factionId) {
      return;
    }

    this.error.set(null);
    this.successMessage.set(null);
    try {
      const play = await this.overlay.run(() =>
        this.campaignsApi.assignFaction(campaign.id, {
          revision: this.play()?.revision ?? campaign.revision,
          userId: participant.userId,
          factionId: parsed.factionId,
          subfaction: parsed.subfaction,
        }),
      );
      const detail = await this.campaignsApi.get(campaign.id);
      this.campaign.set(detail);
      this.staffFactionId.update((current) => {
        const next = { ...current };
        delete next[participant.userId];
        return next;
      });
      if (this.shouldLoadPlay(detail)) {
        this.applyPlay(play, { preserveLocalWork: true });
      }
      this.successMessage.set(FORM_SAVE_SUCCESS_MESSAGE);
    } catch (error: unknown) {
      this.error.set(readApiError(error, 'Unable to assign that faction.'));
    }
  }

  protected expandAllSections(): void {
    this.openSections.set(openSections());
    this.persistViewPrefs();
  }

  protected collapseAllSections(): void {
    this.openSections.set(
      Object.fromEntries(CAMPAIGN_SECTIONS.map((id) => [id, false])) as Record<CampaignSection, boolean>,
    );
    this.persistViewPrefs();
  }

  protected setHighlightMode(mode: string): void {
    if (mode !== 'configured' && mode !== 'faction' && mode !== 'alliance') {
      return;
    }

    this.highlightMode.set(mode);
    this.persistViewPrefs();
  }

  protected sortBy(column: StandingsSortColumn): void {
    this.standingsSort.set(nextStandingsSort(this.standingsSort(), column));
    this.persistViewPrefs();
  }

  protected sortDirection(column: StandingsSortColumn): 'ascending' | 'descending' | 'none' {
    const sort = this.standingsSort();
    if (sort.column !== column) {
      return 'none';
    }

    return sort.direction === 'asc' ? 'ascending' : 'descending';
  }

  protected isStandingViewer(userId: string): boolean {
    return this.auth.currentUser()?.id === userId;
  }

  protected traitorVictims(userId: string): TraitorVictim[] {
    return this.campaign()?.participants?.find((participant) => participant.userId === userId)?.traitorVictims ?? [];
  }

  protected onChatChannelChange(key: string): void {
    this.chatChannelKey.set(key);
    this.persistViewPrefs();
  }

  protected onChatScrollChange(scrollTop: number): void {
    this.lastChatScrollTop = scrollTop;
    this.persistViewPrefs();
  }

  protected asNumber(value: string | number): number {
    return Number(value);
  }

  protected async postChat(payload: CampaignChatSend): Promise<void> {
    const campaignId = this.campaign()?.id ?? this.campaignId;
    const revision = this.campaign()?.revision ?? this.logBoard()?.revision;
    if (!campaignId || typeof revision !== 'number') {
      return;
    }

    this.chatError.set(null);
    this.chatBusy.set(true);
    try {
      const next = await this.campaignsApi.postChat(campaignId, {
        revision,
        message: payload.message,
        channelKind: payload.channelKind,
        targetId: payload.targetId,
      });
      this.applyLogSnapshot(next, true);
    } catch (error: unknown) {
      this.chatError.set(readApiError(error, 'Unable to send that chat message.'));
    } finally {
      this.chatBusy.set(false);
    }
  }

  protected async downloadLog(request: CampaignLogExportRequest): Promise<void> {
    const campaignId = this.campaign()?.id ?? this.campaignId;
    if (!campaignId) {
      return;
    }

    this.downloadingLog.set(true);
    this.error.set(null);
    try {
      const file = await this.campaignsApi.exportLog(campaignId, request);
      downloadBlob(file.blob, file.filename);
    } catch (error: unknown) {
      this.error.set(readApiError(error, 'Unable to download the campaign log.'));
    } finally {
      this.downloadingLog.set(false);
    }
  }

  protected async setPublicObjectiveAward(awarded: boolean): Promise<void> {
    const campaign = this.campaign();
    const objectiveId = this.awardObjectiveId();
    const playerUserId = this.awardPlayerUserId();
    if (!campaign || !objectiveId || !playerUserId) {
      return;
    }

    await this.runPlay(() =>
      this.campaignsApi.setPublicObjectiveAward(campaign.id, {
        revision: campaign.revision,
        objectiveId,
        playerUserId,
        awarded,
      }),
    );
  }

  protected async grantPrivateObjective(): Promise<void> {
    const campaign = this.campaign();
    const holderId = this.grantHolderId();
    if (!campaign || !holderId) {
      return;
    }

    await this.runPlay(() =>
      this.campaignsApi.grantPrivateObjective(campaign.id, {
        revision: campaign.revision,
        holderKind: this.grantHolderKind(),
        holderId,
        typeId: this.grantTypeId() || null,
      }),
    );
  }

  protected async claimPrivateObjective(assignmentId: string): Promise<void> {
    const campaign = this.campaign();
    if (!campaign || !assignmentId) {
      return;
    }

    await this.runPlay(() =>
      this.campaignsApi.claimPrivateObjective(campaign.id, {
        revision: campaign.revision,
        assignmentId,
      }),
    );
  }

  protected async moderatePrivateObjective(assignmentId: string, approved: boolean): Promise<void> {
    const campaign = this.campaign();
    if (!campaign || !assignmentId) {
      return;
    }

    await this.runPlay(() =>
      this.campaignsApi.moderatePrivateObjective(campaign.id, {
        revision: campaign.revision,
        assignmentId,
        approved,
      }),
    );
  }

  protected async resolveItemObjectiveChoice(itemId: string, choiceId: string): Promise<void> {
    const campaign = this.campaign();
    if (!campaign || !itemId || !choiceId) {
      return;
    }

    await this.runPlay(() =>
      this.campaignsApi.resolveItemObjectiveChoice(campaign.id, {
        revision: campaign.revision,
        itemId,
        choiceId,
      }),
    );
  }

  protected onGrantHolderKind(kind: string): void {
    this.grantHolderKind.set(kind);
    this.grantHolderId.set(this.grantHolders()[0]?.id ?? '');
  }

  protected async chooseFaction(): Promise<void> {
    const campaign = this.campaign();
    const parsed = parseMapFactionOptionValue(this.factionChoice());
    if (!campaign || !parsed.factionId) {
      return;
    }

    this.error.set(null);
    this.successMessage.set(null);
    try {
      await this.overlay.run(() =>
        this.campaignsApi.chooseFaction(campaign.id, {
          revision: campaign.revision,
          factionId: parsed.factionId,
          subfaction: parsed.subfaction,
        }),
      );
      this.campaign.update((current) =>
        current
          ? {
              ...current,
              factionId: parsed.factionId,
              subfaction: parsed.subfaction,
            }
          : current,
      );
      await this.load(campaign.id);
      this.successMessage.set(FORM_SAVE_SUCCESS_MESSAGE);
    } catch (error: unknown) {
      this.error.set(readApiError(error, 'Unable to save your faction.'));
    }
  }

  protected onTerritorySelect(event: {
    id: string;
    additive: boolean;
    clientX?: number;
    clientY?: number;
    source?: 'cycle';
  }): void {
    if (event.source === 'cycle') {
      this.cancelMapAction();
      this.mapFocus.set(null);
      this.selectedIds.set([event.id]);
      this.hoveredTerritoryId.set(event.id);
      return;
    }

    const handled = this.handleMapActionSelect(event);
    this.mapFocus.set(null);
    if (handled) {
      return;
    }

    this.assignRetreatFromMap(event.id);
    if (event.additive) {
      this.selectedIds.update((current) =>
        current.includes(event.id) ? current.filter((id) => id !== event.id) : [...current, event.id],
      );
      return;
    }

    this.selectedIds.set([event.id]);
    this.hoveredTerritoryId.set(event.id);
  }

  protected onForceSelect(event: { id: string; territoryId: string; clientX: number; clientY: number }): void {
    const play = this.play();
    const force = this.myForces().find((item) => item.id === event.id);
    if (!play?.isParticipant || play.canChooseFaction || !force) {
      return;
    }

    this.mapFocus.set(null);
    this.selectedIds.set([event.territoryId]);
    this.hoveredTerritoryId.set(event.territoryId);
    if (this.mapAction()?.step === 'pick-target' || this.mapAction()?.step === 'pick-via') {
      return;
    }

    const battle = this.battleForForce(force);
    if (force.inBattle) {
      const kinds: string[] = [];
      if (battle?.canSurrender) {
        kinds.push('Surrender');
      }

      if (battle?.needsRetreat) {
        kinds.push('Retreat');
      }

      if (kinds.length === 1 && kinds[0] === 'Retreat') {
        this.mapAction.set({
          step: 'pick-target',
          forceId: force.id,
          originId: event.territoryId,
          kind: 'Retreat',
          targetTerritoryId: '',
          viaTerritoryId: '',
          viaPath: [],
          viaCandidates: [],
          structureTypeId: '',
          droppedItemObjectiveIds: [],
          menuX: 0,
          menuY: 0,
        });
        return;
      }

      if (kinds.length > 0) {
        this.openForceMapMenu(force, event.clientX, event.clientY);
      }

      return;
    }

    if (!this.isActionPhase() || play.isCommitted || force.availableActions.length === 0) {
      return;
    }

    if (force.isRandomTeleportLocked === true) {
      return;
    }

    this.openForceMapMenu(force, event.clientX, event.clientY);
  }

  private openForceMapMenu(force: PlayForce, clientX: number, clientY: number): void {
    const { x, y } = this.menuPosition(clientX, clientY);
    this.selectedIds.set([force.territoryId]);
    this.hoveredTerritoryId.set(force.territoryId);
    this.mapAction.set({
      step: 'menu',
      forceId: force.id,
      originId: force.territoryId,
      kind: '',
      targetTerritoryId: '',
      viaTerritoryId: '',
      viaPath: [],
      viaCandidates: [],
      structureTypeId: '',
      droppedItemObjectiveIds: [],
      menuX: x,
      menuY: y,
    });
  }

  protected onMapBackgroundSelect(): void {
    this.cancelMapAction();
    this.selectedIds.set([]);
    this.mapFocus.set(null);
  }

  protected isMapFocused(kind: 'player' | 'faction' | 'ally', id: string): boolean {
    const focus = this.mapFocus();
    return focus?.kind === kind && focus.id === id;
  }

  protected focusOnMap(kind: 'player' | 'faction' | 'ally', id: string): void {
    if (this.mapAction()) {
      return;
    }

    const current = this.mapFocus();
    if (current?.kind === kind && current.id === id) {
      this.mapFocus.set(null);
      this.selectedIds.set([]);
      return;
    }

    this.mapFocus.set({ kind, id });
    this.selectedIds.set(this.territoriesForFocus({ kind, id }));
    this.setSection('map', true);
    this.scrollMapIntoView();
  }

  protected allyGroupIdByName(name: string | null | undefined): string | null {
    if (!name) {
      return null;
    }

    return this.campaign()?.allyGroups.find((group) => group.name === name)?.id ?? null;
  }

  private territoriesForFocus(focus: { kind: 'player' | 'faction' | 'ally'; id: string }): string[] {
    const factionIds = this.factionIdsForFocus(focus);
    const forceTerritoryIds = this.forcesForFocus(focus).map((force) => force.territoryId);
    const ownedIds = this.graph()
      .territories.filter((territory) => territory.ownerFactionId && factionIds.includes(territory.ownerFactionId))
      .map((territory) => territory.id);
    return [...new Set([...ownedIds, ...forceTerritoryIds])];
  }

  private forcesForFocus(focus: { kind: 'player' | 'faction' | 'ally'; id: string }): PlayForce[] {
    const forces = this.play()?.forces ?? [];
    if (focus.kind === 'player') {
      return forces.filter((force) => force.controllerUserId === focus.id);
    }

    const factionIds = this.factionIdsForFocus(focus);
    return forces.filter((force) => factionIds.includes(force.factionId));
  }

  private factionIdsForFocus(focus: { kind: 'player' | 'faction' | 'ally'; id: string }): string[] {
    const campaign = this.campaign();
    if (!campaign) {
      return [];
    }

    if (focus.kind === 'faction') {
      return [focus.id];
    }

    if (focus.kind === 'ally') {
      const group = campaign.allyGroups.find((item) => item.id === focus.id);
      return campaign.factions
        .filter((faction) => faction.allyGroupId === focus.id || (!!group && faction.allyGroupName === group.name))
        .map((faction) => faction.id);
    }

    const participant = campaign.participants?.find((item) => item.userId === focus.id);
    if (participant?.factionId) {
      return [participant.factionId];
    }

    if (participant?.factionName) {
      const faction = campaign.factions.find((item) => item.name === participant.factionName);
      return faction ? [faction.id] : [];
    }

    const standingFactionId = (this.play()?.standings ?? campaign.standings ?? []).find(
      (row) => row.userId === focus.id,
    )?.factionId;
    return standingFactionId ? [standingFactionId] : [];
  }

  protected factionName(id: string | null | undefined): string {
    if (!id) {
      return 'Neutral';
    }

    return this.campaign()?.factions.find((faction) => faction.id === id)?.name ?? 'Unknown faction';
  }

  protected spawnPlacesFor(faction: CampaignFaction): FactionSpawnPlace[] {
    return factionSpawnPlaces(faction, this.graph().territories);
  }

  protected adjacentTerritories(territory: MapTerritory): { id: string; label: string }[] {
    return this.graph()
      .adjacencies.filter((edge) => edge.territoryAId === territory.id || edge.territoryBId === territory.id)
      .map((edge) => {
        const otherId = edge.territoryAId === territory.id ? edge.territoryBId : edge.territoryAId;
        const other = this.graph().territories.find((item) => item.id === otherId);
        return { id: otherId, label: other ? territoryLabel(other) : otherId };
      })
      .sort((left, right) => left.label.localeCompare(right.label));
  }

  protected selectTerritoryOnMap(territoryId: string): void {
    if (this.mapAction() || !this.graph().territories.some((territory) => territory.id === territoryId)) {
      return;
    }

    this.mapFocus.set(null);
    this.selectedIds.set([territoryId]);
    this.hoveredTerritoryId.set(territoryId);
    this.assignRetreatFromMap(territoryId);
    this.setSection('map', true);
    this.scrollMapIntoView();
  }

  private scrollMapIntoView(): void {
    afterNextRender(
      () => {
        this.scrollElementIntoView(this.mapSection()?.nativeElement);
      },
      { injector: this.injector },
    );
  }

  private scrollElementIntoView(element: HTMLElement | null | undefined): void {
    if (!element || typeof element.scrollIntoView !== 'function') {
      return;
    }

    element.scrollIntoView({
      behavior: 'smooth',
      block: 'start',
      inline: 'nearest',
    });
  }

  protected labelFor(territory: MapTerritory): string {
    return territoryLabel(territory);
  }

  protected forceLabelById(forceId: string): string {
    const force = this.play()?.forces.find((item) => item.id === forceId);
    return force ? this.forceLabel(force) : forceId;
  }

  protected supplyFor(battle: PlayBattle, forceId: string | null | undefined): PlayBattleForceSupply | undefined {
    if (!forceId) {
      return undefined;
    }

    return battle.forceSupplies?.find((supply) => supply.forceId === forceId);
  }

  protected viewerSupplyView(): PlayerSupplyView | null {
    const playSupply = this.play()?.viewerSupply;
    if (playSupply) {
      return playSupply;
    }

    const userId = this.auth.currentUser()?.id;
    const participant = this.campaign()?.participants?.find((item) => item.userId === userId);
    if (!participant || typeof participant.currentSupplyPoints !== 'number') {
      return null;
    }

    return {
      currentSupplyPoints: participant.currentSupplyPoints,
      temporarySupplyPoints: participant.temporarySupplyPoints ?? 0,
      mapSupplyPoints: participant.mapSupplyPoints ?? 0,
      roundFreeSupplyPoints: participant.roundFreeSupplyPoints ?? 0,
      splitPenaltyPoints: participant.splitPenaltyPoints ?? 0,
      forceAllowancePoints:
        (participant.mapSupplyPoints ?? 0) +
        (participant.roundFreeSupplyPoints ?? 0) -
        (participant.splitPenaltyPoints ?? 0),
      contributions: participant.contributions ?? [],
    };
  }

  protected chainSupplyView(supply: PlayerSupplyView): PlayerSupplyView {
    return {
      ...supply,
      temporarySupplyPoints: 0,
      currentSupplyPoints: supply.forceAllowancePoints,
      contributions: (supply.contributions ?? []).filter((row) => row.kind !== 'Temporary'),
    };
  }

  protected spendableSupplyView(): PlayerSupplyView | null {
    const supply = this.viewerSupplyView();
    if (!supply) {
      return null;
    }

    const points = supply.temporarySupplyPoints;
    const contributions = (supply.contributions ?? []).filter((row) => row.kind === 'Temporary');
    return {
      currentSupplyPoints: points,
      temporarySupplyPoints: 0,
      mapSupplyPoints: 0,
      roundFreeSupplyPoints: 0,
      splitPenaltyPoints: 0,
      forceAllowancePoints: 0,
      contributions:
        contributions.length > 0
          ? contributions
          : points === 0
            ? []
            : [{ kind: 'Temporary', label: 'Temporary supply', points, isAllied: false }],
    };
  }

  protected supplyTooltip(supply: PlayerSupplyView | PlayBattleForceSupply): string {
    const rows = this.supplyRows(supply);
    if (rows.length === 0) {
      return 'No supply sources.';
    }

    return rows.map((row) => `${row.label}: ${this.formatSupplyPoints(row.points)}`).join('\n');
  }

  protected supplyRows(supply: PlayerSupplyView | PlayBattleForceSupply): { label: string; points: number }[] {
    if (supply.contributions && supply.contributions.length > 0) {
      return supply.contributions.map((row) => ({ label: row.label, points: row.points }));
    }

    const rows: { label: string; points: number }[] = [];
    if ((supply.mapSupplyPoints ?? 0) !== 0) {
      rows.push({ label: 'Map holdings', points: supply.mapSupplyPoints ?? 0 });
    }

    if ((supply.roundFreeSupplyPoints ?? 0) !== 0) {
      rows.push({ label: 'Round free supply', points: supply.roundFreeSupplyPoints ?? 0 });
    }

    if ((supply.splitPenaltyPoints ?? 0) !== 0) {
      rows.push({ label: 'Split-force penalty', points: -(supply.splitPenaltyPoints ?? 0) });
    }

    if (supply.temporarySupplyPoints !== 0) {
      rows.push({ label: 'Temporary supply', points: supply.temporarySupplyPoints });
    }

    return rows;
  }

  protected formatSupplyPoints(points: number): string {
    return points > 0 ? `+${points}` : `${points}`;
  }

  protected battleTerritory(battle: PlayBattle): MapTerritory | null {
    return this.graph().territories.find((territory) => territory.id === battle.territoryId) ?? null;
  }

  protected battleMatchup(battle: PlayBattle): string {
    return battle.participantForceIds.map((forceId) => this.forceLabelById(forceId)).join(' vs ');
  }

  protected battleMissionKind(battle: PlayBattle): string {
    return battle.mission?.isAttackerDefender === true ? 'Attacker/defender mission' : 'Pitched battle';
  }

  protected battleCombatants(
    battle: PlayBattle,
  ): { forceId: string; role: 'Attacker' | 'Defender' | 'Combatant'; supply?: PlayBattleForceSupply }[] {
    const attackerDefender = battle.mission?.isAttackerDefender === true;
    return this.reportingForceIds(battle).map((forceId) => {
      let role: 'Attacker' | 'Defender' | 'Combatant' = 'Combatant';
      if (attackerDefender && battle.attackerForceId === forceId) {
        role = 'Attacker';
      } else if (attackerDefender && battle.defenderForceId === forceId) {
        role = 'Defender';
      }

      return { forceId, role, supply: this.supplyFor(battle, forceId) };
    });
  }

  protected missionHasAttachment(mission: CampaignMission): boolean {
    return !!mission.url || !!this.missionFileUrl(mission);
  }

  protected forceLabel(force: PlayForce): string {
    const name = force.controllerUsername ?? 'Player';
    const status = forceStatusLabel(force.statusName);
    const base = `${name} · ${this.factionName(force.factionId)}`;
    return status === 'Normal' ? base : `${base} · ${status}`;
  }

  protected forceAccent(force: PlayForce): string {
    return this.campaign()?.factions.find((faction) => faction.id === force.factionId)?.color ?? 'var(--color-accent)';
  }

  protected goToOrders(): void {
    this.setSection('orders', true);
    afterNextRender(
      () => {
        this.scrollElementIntoView(this.ordersSection()?.nativeElement);
      },
      { injector: this.injector },
    );
  }

  protected toggleCommitmentsRoster(): void {
    this.commitmentsRosterOpen.update((open) => !open);
  }

  protected goToFactionListing(factionId: string, event?: Event): void {
    event?.preventDefault();
    this.setSection('factions', true);
    this.scrollListingIntoView(`campaign-faction-${factionId}`);
  }

  protected goToAllyGroupListing(groupId: string, event?: Event): void {
    event?.preventDefault();
    this.setSection('allies', true);
    this.scrollListingIntoView(`campaign-ally-${groupId}`);
  }

  private scrollListingIntoView(elementId: string): void {
    afterNextRender(
      () => {
        const element = document.getElementById(elementId);
        if (!element) {
          return;
        }

        const toolbar = document.querySelector('.setup-toolbar');
        const offset = toolbar instanceof HTMLElement ? toolbar.getBoundingClientRect().height + 8 : 88;
        const top = window.scrollY + element.getBoundingClientRect().top - offset;
        window.scrollTo({ top: Math.max(0, top), behavior: 'smooth' });
      },
      { injector: this.injector },
    );
  }

  protected listJoin(index: number, total: number): string {
    if (index === 0 || total <= 1) {
      return '';
    }

    if (total === 2) {
      return ' and ';
    }

    return index === total - 1 ? ', and ' : ', ';
  }

  protected catalogFaction(id?: string | null, name?: string | null): CampaignFaction | null {
    const campaign = this.campaign();
    if (!campaign) {
      return null;
    }

    if (id) {
      const byId = campaign.factions.find((faction) => faction.id === id);
      if (byId) {
        return byId;
      }
    }

    const trimmed = name?.trim();
    if (!trimmed) {
      return null;
    }

    return campaign.factions.find((faction) => faction.name === trimmed) ?? null;
  }

  protected goToCommitments(): void {
    if (this.isBattlePhase() || this.hasOpenBattles()) {
      this.setSection('battles', true);
      afterNextRender(
        () => {
          this.scrollElementIntoView(this.battlesBlock()?.nativeElement);
        },
        { injector: this.injector },
      );
      return;
    }

    this.setSection('orders', true);
    this.commitmentsRosterOpen.set(true);
    afterNextRender(
      () => {
        this.scrollElementIntoView(this.commitmentsBlock()?.nativeElement);
      },
      { injector: this.injector },
    );
  }

  protected requestCommit(): void {
    this.confirmingCommit.set(true);
  }

  protected onMapCommit(): void {
    if (this.canUncommitActions()) {
      void this.uncommit();
      return;
    }

    if (this.canUncommitRetreat() && !this.canCommitSurrender() && !this.canCommitAnyRetreat()) {
      void this.uncommitReadyRetreats();
      return;
    }

    const surrender = this.pendingSurrenderBattle();
    if (surrender) {
      if (this.isFinalRequiredBattleCommitment(surrender)) {
        this.requestRetreatCommit(surrender);
        return;
      }

      void this.submitSurrender(surrender);
      return;
    }

    const retreat = this.pendingRetreatBattle();
    if (retreat) {
      if (this.isFinalRequiredBattleCommitment(retreat)) {
        this.requestRetreatCommit(retreat);
        return;
      }

      void this.commitRetreat(retreat);
      return;
    }

    if (!this.canCommitActions()) {
      return;
    }

    if (this.isFinalRequiredCommitment()) {
      this.requestCommit();
      return;
    }

    void this.commit();
  }

  protected cancelCommit(): void {
    this.confirmingCommit.set(false);
  }

  protected async confirmCommit(): Promise<void> {
    this.confirmingCommit.set(false);
    await this.commit();
  }

  protected territoryName(id: string | null | undefined): string {
    if (!id) {
      return 'None';
    }

    const territory = this.graph().territories.find((item) => item.id === id);
    return territory ? territoryLabel(territory) : id;
  }

  protected forcesInTerritory(territoryId: string): string[] {
    return (this.play()?.forces.filter((force) => force.territoryId === territoryId) ?? []).map((force) =>
      this.forceLabel(force),
    );
  }

  protected itemsInTerritory(territoryId: string): PlayItemObjective[] {
    const play = this.play();
    if (!play) {
      return [];
    }

    const forceIds = new Set(play.forces.filter((force) => force.territoryId === territoryId).map((force) => force.id));
    return (play.itemObjectives ?? []).filter(
      (item) => item.territoryId === territoryId || (!!item.possessorForceId && forceIds.has(item.possessorForceId)),
    );
  }

  protected itemLocation(item: PlayItemObjective): string {
    if (item.possessorForceId) {
      return `Carried by ${this.forceLabelById(item.possessorForceId)}`;
    }

    if (item.territoryId) {
      return this.territoryName(item.territoryId);
    }

    return 'Unknown';
  }

  protected itemSummary(item: PlayItemObjective): string {
    const hidden = item.isRevealed ? '' : ' (hidden)';
    const carried = item.possessorForceId ? ' (carried)' : '';
    return `${item.name}${hidden}${carried}`;
  }

  protected structureConditionLabel(condition: string | null | undefined): string {
    if (condition === 'Pillaged') {
      return 'pillaged';
    }

    if (condition === 'Destroyed') {
      return 'destroyed';
    }

    return 'operational';
  }

  protected terrainName(id: string | null): string {
    return terrainTypeById(this.campaign(), id)?.name ?? 'None';
  }

  protected isWaterFeature(id: string | null | undefined): boolean {
    const campaign = this.campaign();
    const type = terrainTypeById(campaign, id);
    if (!campaign || !type) {
      return false;
    }

    const waterIds = new Set(
      (campaign.terrainTags ?? []).filter((tag) => isWaterTagName(tag.name)).map((tag) => tag.id),
    );
    return (type.tagIds ?? []).some((tagId) => waterIds.has(tagId));
  }

  protected specialRulesFor(ids: readonly string[] | undefined): CampaignSpecialRule[] {
    if (!ids || ids.length === 0) {
      return [];
    }

    const catalog = mergeSpecialRuleCatalogs(this.play()?.specialRules, this.campaign()?.specialRules);
    const byId = new Map(catalog.map((rule) => [rule.id.toLowerCase(), rule] as const));
    return ids.flatMap((id) => {
      const rule = byId.get(id.toLowerCase());
      return rule ? [rule] : [];
    });
  }

  protected itemSpecialRules(item: PlayItemObjective): CampaignSpecialRule[] {
    const type = this.campaign()?.itemObjectiveTypes?.find((entry) => entry.id === item.typeId);
    return this.specialRulesFor(type?.specialRuleIds);
  }

  protected heldItemsForForce(force: PlayForce): PlayItemObjective[] {
    return (this.play()?.itemObjectives ?? []).filter((item) => item.possessorForceId === force.id);
  }

  protected itemEffectLabels(item: PlayItemObjective): string[] {
    const type = this.campaign()?.itemObjectiveTypes?.find((entry) => entry.id === item.typeId);
    return (type?.effects ?? []).map((effect) => {
      if (effect.kind === 'Custom' && effect.customText?.trim()) {
        return effect.customText.trim();
      }

      const kind = ITEM_OBJECTIVE_EFFECT_KINDS.find((entry) => entry.id === effect.kind);
      let label = kind?.label ?? effect.kind;
      if (effect.amount) {
        label += ` (${effect.amount}${effect.amountIsPercent ? '%' : ''})`;
      }

      return label;
    });
  }

  protected itemPowerLabels(item: PlayItemObjective): string[] {
    const rules = this.itemSpecialRules(item).map((rule) => (rule.text ? `${rule.name} — ${rule.text}` : rule.name));
    return [...this.itemEffectLabels(item), ...rules];
  }

  protected canResolveItemChoice(item: PlayItemObjective): boolean {
    return !item.isDestroyed && !item.resolvedChoiceId && (item.choices?.length ?? 0) > 0;
  }

  protected structureName(id: string | null | undefined): string {
    if (!id) {
      return 'None';
    }

    return (
      this.play()?.structureTypes.find((type) => type.id === id)?.name ??
      structureTypeById(this.campaign(), id)?.name ??
      'Unknown structure'
    );
  }

  protected terrainSymbol(id: string | null | undefined): string | null {
    return terrainTypeById(this.campaign(), id)?.name ?? null;
  }

  protected structureSymbol(id: string | null | undefined): string | null {
    return structureTypeById(this.campaign(), id)?.builtinSymbol ?? null;
  }

  protected inspectedMissions(): CampaignMission[] {
    const territory = this.hoveredTerritory();
    return missionsForTerritory(this.campaign(), territory?.terrainTypeId, territory?.structureTypeId);
  }

  protected structureAt(territoryId: string): { name: string; condition: string } | null {
    const territory = this.graph().territories.find((item) => item.id === territoryId);
    if (!territory?.structureTypeId) {
      return null;
    }

    return {
      name: this.structureName(territory.structureTypeId),
      condition: this.structureConditionLabel(territory.structureCondition),
    };
  }

  protected structureImageUrl = (structureTypeId: string, pillaged = false): string | null => {
    const campaign = this.campaign();
    const structure = structureTypeById(campaign, structureTypeId);
    if (!campaign || !structure) {
      return null;
    }

    if (pillaged) {
      return structure.hasPillagedImage
        ? this.campaignsApi.structureImageUrl(campaign.id, structureTypeId, campaign.assetTags, true)
        : null;
    }

    if (!structure.hasImage) {
      return null;
    }

    return this.campaignsApi.structureImageUrl(campaign.id, structureTypeId, campaign.assetTags);
  };

  protected itemObjectiveImageUrl = (typeId: string): string | null => {
    const campaign = this.campaign();
    const type = campaign?.itemObjectiveTypes?.find((item) => item.id === typeId);
    if (!campaign || !type?.hasImage) {
      return null;
    }

    return this.campaignsApi.itemObjectiveImageUrl(campaign.id, typeId, campaign.assetTags);
  };

  protected standingItemImageUrl(item: { typeId: string; hasImage?: boolean }): string | null {
    const campaign = this.campaign();
    if (!campaign || !item.hasImage) {
      return null;
    }

    return this.campaignsApi.itemObjectiveImageUrl(campaign.id, item.typeId, campaign.assetTags);
  }

  protected flagImageUrl = (factionId: string, subfaction?: string | null): string | null => {
    const campaign = this.campaign();
    const faction = campaign?.factions.find((item) => item.id === factionId);
    if (!campaign || !faction) {
      return null;
    }

    const appearance = resolveFactionAppearance(faction, subfaction);
    if (!appearance.hasFlagImage) {
      return null;
    }

    const source = findSubfactionAppearance(faction, subfaction)?.flagSource ?? 'inherit';
    const scopedSubfaction = source === 'image' ? subfaction : null;
    return this.campaignsApi.flagImageUrl(campaign.id, factionId, campaign.assetTags, scopedSubfaction);
  };

  protected standingSubfaction(userId: string): string | null {
    return this.campaign()?.participants?.find((participant) => participant.userId === userId)?.subfaction ?? null;
  }

  protected standingFlagUrl(row: { userId: string; factionId?: string | null }): string | null {
    if (!row.factionId) {
      return null;
    }

    return this.flagImageUrl(row.factionId, this.standingSubfaction(row.userId));
  }

  protected standingFlagTint(row: { userId: string; factionId?: string | null; tintFlagImage?: boolean }): boolean {
    const faction = this.campaign()?.factions.find((item) => item.id === row.factionId);
    return resolveFactionAppearance(faction, this.standingSubfaction(row.userId)).tint;
  }

  protected standingFlagColor(row: {
    userId: string;
    factionId?: string | null;
    factionColor?: string | null;
  }): string {
    const faction = this.campaign()?.factions.find((item) => item.id === row.factionId);
    return resolveFactionAppearance(faction, this.standingSubfaction(row.userId)).color;
  }

  protected standingColumnTooltip(sources: CampaignPointSource[] | undefined, total: number): string {
    const lines = (sources ?? []).map((source) => `${source.label}: ${source.points}`);
    lines.push(`Total: ${total}`);
    return lines.join('\n');
  }

  protected standingTotalTooltip(row: CampaignPointStanding): string {
    return this.standingColumnTooltip(
      [
        ...(row.territoryAndStructureSources ?? []),
        ...(row.battleSources ?? []),
        ...(row.publicObjectiveSources ?? []),
        ...(row.privateObjectiveSources ?? []),
        ...(row.otherSources ?? []),
      ],
      row.total,
    );
  }

  protected participantFlagUrl(participant: CampaignParticipant): string | null {
    const campaign = this.campaign();
    const faction = resolveParticipantFaction(participant, campaign?.factions ?? []);
    if (!campaign || !faction) {
      return null;
    }

    return this.flagImageUrl(faction.id, participant.subfaction);
  }

  protected participantFlagTint(participant: CampaignParticipant): boolean {
    const faction = resolveParticipantFaction(participant, this.campaign()?.factions ?? []);
    return resolveFactionAppearance(faction, participant.subfaction).tint;
  }

  protected participantFlagColor(participant: CampaignParticipant): string {
    const faction = resolveParticipantFaction(participant, this.campaign()?.factions ?? []);
    return resolveFactionAppearance(faction, participant.subfaction).color;
  }

  protected missionFileUrl(mission: CampaignMission): string | null {
    const campaign = this.campaign();
    if (!campaign || !mission.hasFile) {
      return null;
    }

    return this.campaignsApi.missionFileUrl(campaign.id, mission.id);
  }

  protected draftKindsFor(force: PlayForce): readonly string[] {
    return force.inBattle ? [] : force.availableActions;
  }

  protected actionKindLabel(kind: string): string {
    return actionKindLabel(kind);
  }

  protected isChosenTeleportKind(kind: string): boolean {
    return kind === 'TeleportToSpecificTerritory' || kind === 'Teleport';
  }

  protected isTeleportKind(kind: string): boolean {
    return kind === 'TeleportRandomly' || kind === 'TeleportToSpecificTerritory' || kind === 'Teleport';
  }

  protected isForceTeleporting(force: PlayForce): boolean {
    if (force.isTeleporting === true || force.isRandomTeleportLocked === true) {
      return true;
    }

    const action = this.ownForceMapAction(force);
    return !!action && action.status === 'committed' && this.isTeleportKind(action.kind);
  }

  protected droppableItemsFor(force: PlayForce): PlayItemObjective[] {
    const ids = new Set(force.droppableItemObjectiveIds ?? []);
    return this.heldItemsForForce(force).filter((item) => ids.has(item.id));
  }

  protected isItemDropSelected(forceId: string, itemId: string, debug = false): boolean {
    const draft = debug ? this.debugDraftFor(forceId) : this.draftFor(forceId);
    return draft.droppedItemObjectiveIds.includes(itemId);
  }

  protected onDraftDropItem(forceId: string, itemId: string, selected: boolean, debug = false): void {
    const current = debug ? this.debugDraftFor(forceId) : this.draftFor(forceId);
    const dropped = new Set(current.droppedItemObjectiveIds);
    if (selected) {
      dropped.add(itemId);
    } else {
      dropped.delete(itemId);
    }

    const next = { ...current, droppedItemObjectiveIds: [...dropped] };
    if (debug) {
      this.markDebugDraftDirty(forceId);
      this.debugDrafts.update((drafts) => ({ ...drafts, [forceId]: next }));
      return;
    }

    this.markDraftDirty(forceId);
    this.drafts.update((drafts) => ({ ...drafts, [forceId]: next }));
  }

  protected isMapItemDropSelected(itemId: string): boolean {
    return this.mapAction()?.droppedItemObjectiveIds.includes(itemId) === true;
  }

  protected onMapDropItem(itemId: string, selected: boolean): void {
    const flow = this.mapAction();
    if (!flow) {
      return;
    }

    const dropped = new Set(flow.droppedItemObjectiveIds);
    if (selected) {
      dropped.add(itemId);
    } else {
      dropped.delete(itemId);
    }

    this.mapAction.set({ ...flow, droppedItemObjectiveIds: [...dropped] });
  }

  protected mapConfirmForce(): PlayForce | null {
    const flow = this.mapAction();
    return this.myForces().find((item) => item.id === flow?.forceId) ?? null;
  }

  protected privateObjectiveProgress(assignment: PrivateObjectiveAssignment): string | null {
    if (
      assignment.scoringKind !== 'Automatic' ||
      assignment.currentCount === null ||
      assignment.currentCount === undefined ||
      assignment.requiredCount === null ||
      assignment.requiredCount === undefined
    ) {
      return null;
    }

    return `(${assignment.currentCount}/${assignment.requiredCount})`;
  }

  protected privateObjectiveSummary(assignment: PrivateObjectiveAssignment): string | null {
    const progress = this.privateObjectiveProgress(assignment);
    if (assignment.description && progress) {
      return `${assignment.description} ${progress}`;
    }

    return assignment.description ?? progress;
  }

  protected battleLockCaption(force: PlayForce): string {
    const place = this.territoryName(force.territoryId);
    const battle = this.play()?.battles.find((item) => item.participantForceIds.includes(force.id));
    const opponents = this.battleOpponentNames(force, battle);
    const withWhom = opponents.length > 0 ? ` with ${joinDisplayNames(opponents)}` : '';
    return `Locked in battle at ${place}${withWhom}. This force cannot perform an action until the battle is resolved. Surrender from the map.`;
  }

  private battleOpponentNames(force: PlayForce, battle: PlayBattle | undefined): string[] {
    const play = this.play();
    if (!play || !battle) {
      return [];
    }

    const names = play.forces
      .filter((item) => battle.participantForceIds.includes(item.id) && item.id !== force.id)
      .map((item) => `${item.controllerUsername ?? 'Player'} · ${this.factionName(item.factionId)}`);
    if (names.length > 0) {
      return names;
    }

    if (battle.isRinger) {
      const faction = battle.ringerFactionId ? this.factionName(battle.ringerFactionId) : 'a ringer';
      return [faction === 'Unknown faction' ? 'a ringer' : faction];
    }

    return [];
  }

  protected debugKindsFor(force: PlayForce): readonly string[] {
    return force.availableActions;
  }

  protected draftFor(forceId: string): OrderDraft {
    return this.drafts()[forceId] ?? emptyOrderDraft();
  }

  protected debugDraftFor(forceId: string): OrderDraft {
    return this.debugDrafts()[forceId] ?? emptyOrderDraft();
  }

  protected savedDraft(forceId: string): { kind: string; targetTerritoryId: string | null } | null {
    return this.play()?.myDrafts.find((draft) => draft.forceId === forceId) ?? null;
  }

  private ownForceMapAction(force: PlayForce): MapForceAction | null {
    if (!force.isMine) {
      return null;
    }

    const play = this.play();
    if (!play) {
      return null;
    }

    const draft = play.myDrafts.find((item) => item.forceId === force.id);
    const order = play.orders.find((item) => item.forceId === force.id);
    if (play.isCommitted) {
      const kind = order?.kind ?? draft?.kind;
      if (!kind) {
        return this.ownForceRetreatAction(force, play);
      }

      return {
        kind,
        status: 'committed',
        detail: this.forceActionDetail(kind, order?.targetTerritoryId ?? draft?.targetTerritoryId ?? null),
      };
    }

    if (!draft) {
      const retreat = this.ownForceRetreatAction(force, play);
      return retreat;
    }

    return {
      kind: draft.kind,
      status: 'draft',
      detail: this.forceActionDetail(draft.kind, draft.targetTerritoryId),
    };
  }

  private forceActionDetail(kind: string, targetTerritoryId: string | null): string | null {
    if (!targetTerritoryId || (kind !== 'Move' && kind !== 'Split' && kind !== 'Surrender' && kind !== 'Retreat')) {
      return null;
    }

    return `to ${this.territoryName(targetTerritoryId)}`;
  }

  private forceOrderRouteFields(force: PlayForce): { routeSteps?: readonly string[]; routeLabel?: string } {
    const route = this.forceOrderRoute(force);
    if (!route) {
      return {};
    }

    return { routeSteps: route.steps, routeLabel: route.label };
  }

  private forceOrderRoute(force: PlayForce): { steps: readonly string[]; label: string } | null {
    const play = this.play();
    if (!play) {
      return null;
    }

    const retreat = this.forceRetreatRoute(force, play);
    if (retreat) {
      return retreat;
    }

    const flow = this.mapAction();
    if (
      flow?.forceId === force.id &&
      flow.step === 'confirm' &&
      (flow.kind === 'Move' || flow.kind === 'Split') &&
      flow.targetTerritoryId
    ) {
      return this.routeFromSteps(
        flow.kind,
        force.territoryId,
        flow.targetTerritoryId,
        flow.viaTerritoryId,
        flow.viaPath,
      );
    }

    const canSeeOthers = play.isDebugActive === true;
    if (!force.isMine && !canSeeOthers) {
      return null;
    }

    const draft = force.isMine ? this.draftFor(force.id) : this.debugDraftFor(force.id);
    if ((draft.kind !== 'Move' && draft.kind !== 'Split') || !draft.targetTerritoryId) {
      return null;
    }

    return this.routeFromSteps(
      draft.kind,
      force.territoryId,
      draft.targetTerritoryId,
      draft.viaTerritoryId,
      draft.viaPath,
    );
  }

  private forceRetreatRoute(
    force: PlayForce,
    play: CampaignPlayDetail,
  ): { steps: readonly string[]; label: string } | null {
    if (!force.isMine && play.isDebugActive !== true) {
      return null;
    }

    for (const battle of play.battles) {
      if (!battle.participantForceIds.includes(force.id)) {
        continue;
      }

      const dest = this.retreatTarget()[battle.id];
      if (!dest) {
        continue;
      }

      return this.routeFromSteps('Retreat', force.territoryId, dest, '', []);
    }

    return null;
  }

  private routeFromSteps(
    kind: string,
    originId: string,
    targetId: string,
    viaTerritoryId: string,
    viaPath: readonly string[],
  ): { steps: readonly string[]; label: string } | null {
    const steps = [originId];
    for (const id of [viaTerritoryId, ...viaPath, targetId]) {
      if (id.length > 0 && id !== steps.at(-1)) {
        steps.push(id);
      }
    }

    if (steps.length < 2) {
      return null;
    }

    return { steps, label: this.describeOrderRoute(kind, steps) };
  }

  private describeOrderRoute(kind: string, steps: readonly string[]): string {
    const origin = this.territoryName(steps[0] ?? '');
    const destination = this.territoryName(steps.at(-1) ?? '');
    const hops = steps.slice(1, -1);
    if (hops.length === 0) {
      return `${kind} from ${origin} to ${destination}`;
    }

    const through = hops.map((id) => this.territoryName(id)).join(' and ');
    return `${kind} from ${origin} through ${through} to ${destination}`;
  }

  protected onDraftKind(forceId: string, kind: string): void {
    const current = this.draftFor(forceId);
    this.markDraftDirty(forceId);
    this.drafts.update((drafts) => ({
      ...drafts,
      [forceId]: emptyOrderDraft({
        kind,
        targetTerritoryId: this.needsDraftDestinationKind(kind) ? current.targetTerritoryId : '',
        structureTypeId: kind === 'Build' ? current.structureTypeId : '',
        viaTerritoryId: kind === 'Move' || kind === 'Split' ? current.viaTerritoryId : '',
        viaPath: kind === 'Move' || kind === 'Split' ? current.viaPath : [],
        destroyImmediately: kind === 'Pillage' ? current.destroyImmediately : false,
        droppedItemObjectiveIds: kind === 'Move' ? current.droppedItemObjectiveIds : [],
      }),
    }));
    if (this.needsDraftDestinationKind(kind) && current.targetTerritoryId) {
      this.applyDestinationRoute(forceId, current.targetTerritoryId, false);
    }
  }

  protected onDraftTarget(forceId: string, targetTerritoryId: string): void {
    this.applyDestinationRoute(forceId, targetTerritoryId, false);
  }

  protected onDraftVia(forceId: string, viaTerritoryId: string): void {
    this.applyViaSelection(forceId, viaTerritoryId, false);
  }

  protected viaTargets(force: PlayForce): string[] {
    return [...new Set((force.moveHops ?? []).map((hop) => hop.viaTerritoryId))];
  }

  protected needsDraftDestination(force: PlayForce, kind: string): boolean {
    return kind === 'Move' || kind === 'Split' || this.canChooseTeleport(force, kind);
  }

  protected canChooseTeleport(force: PlayForce, kind = 'Teleport'): boolean {
    return (
      kind === 'TeleportToSpecificTerritory' || (kind === 'Teleport' && force.canChooseTeleportDestination === true)
    );
  }

  protected destinationTargets(force: PlayForce, kind: string): string[] {
    if (kind === 'Surrender' || kind === 'Retreat') {
      return this.battleForForce(force)?.retreatTargets ?? [];
    }

    if (this.canChooseTeleport(force, kind)) {
      return force.teleportTargets ?? [];
    }

    return force.moveTargets;
  }

  private needsDraftDestinationKind(kind: string): boolean {
    return (
      kind === 'Move' ||
      kind === 'Split' ||
      kind === 'Teleport' ||
      kind === 'TeleportToSpecificTerritory' ||
      kind === 'Surrender' ||
      kind === 'Retreat'
    );
  }

  protected destinationsForVia(force: PlayForce, viaTerritoryId: string): string[] {
    if (!viaTerritoryId) {
      return force.moveTargets;
    }

    const hopTargets = (force.moveHops ?? [])
      .filter((hop) => hop.viaTerritoryId === viaTerritoryId)
      .map((hop) => hop.targetTerritoryId);
    return [...new Set([...force.moveTargets, ...hopTargets])];
  }

  protected viasForDestination(force: PlayForce, targetTerritoryId: string): string[] {
    if (!targetTerritoryId || this.isDirectMove(force, targetTerritoryId)) {
      return [];
    }

    return [
      ...new Set(
        (force.moveHops ?? [])
          .filter((hop) => hop.targetTerritoryId === targetTerritoryId)
          .map((hop) => hop.viaTerritoryId),
      ),
    ];
  }

  protected onDraftStructure(forceId: string, structureTypeId: string): void {
    const current = this.draftFor(forceId);
    this.markDraftDirty(forceId);
    this.drafts.update((drafts) => ({ ...drafts, [forceId]: { ...current, structureTypeId } }));
  }

  protected onDebugDraftKind(forceId: string, kind: string): void {
    const current = this.debugDraftFor(forceId);
    this.markDebugDraftDirty(forceId);
    this.debugDrafts.update((drafts) => ({
      ...drafts,
      [forceId]: emptyOrderDraft({
        kind,
        targetTerritoryId: this.needsDraftDestinationKind(kind) ? current.targetTerritoryId : '',
        structureTypeId: kind === 'Build' ? current.structureTypeId : '',
        viaTerritoryId: kind === 'Move' || kind === 'Split' ? current.viaTerritoryId : '',
        viaPath: kind === 'Move' || kind === 'Split' ? current.viaPath : [],
        destroyImmediately: kind === 'Pillage' ? current.destroyImmediately : false,
        droppedItemObjectiveIds: kind === 'Move' ? current.droppedItemObjectiveIds : [],
      }),
    }));
    if (this.needsDraftDestinationKind(kind) && current.targetTerritoryId) {
      this.applyDestinationRoute(forceId, current.targetTerritoryId, true);
    }
  }

  protected onDebugDraftTarget(forceId: string, targetTerritoryId: string): void {
    this.applyDestinationRoute(forceId, targetTerritoryId, true);
  }

  protected onDebugDraftVia(forceId: string, viaTerritoryId: string): void {
    this.applyViaSelection(forceId, viaTerritoryId, true);
  }

  protected onDebugDraftStructure(forceId: string, structureTypeId: string): void {
    const current = this.debugDraftFor(forceId);
    this.markDebugDraftDirty(forceId);
    this.debugDrafts.update((drafts) => ({ ...drafts, [forceId]: { ...current, structureTypeId } }));
  }

  protected battleWinnerId(battleId: string): string {
    return this.battleWinner()[battleId] || '';
  }

  protected retreatTargetId(battleId: string): string {
    return this.retreatTarget()[battleId] || '';
  }

  protected onBattleWinner(battleId: string, winnerForceId: string): void {
    this.battleWinner.update((current) => ({ ...current, [battleId]: winnerForceId }));
    this.markBattleResultDirty(battleId);
  }

  protected battleWinnerScore(battleId: string): number | null {
    const scores = this.battleScores()[battleId];
    return scores ? scores.winnerScore : null;
  }

  protected battleLoserScore(battleId: string): number | null {
    const scores = this.battleScores()[battleId];
    return scores ? scores.loserScore : null;
  }

  protected onBattleWinnerScore(battleId: string, value: string | number | null): void {
    this.patchBattleScore(battleId, 'winnerScore', value);
  }

  protected onBattleLoserScore(battleId: string, value: string | number | null): void {
    this.patchBattleScore(battleId, 'loserScore', value);
  }

  protected canReportBattle(battle: PlayBattle): boolean {
    const staff = this.canStaffMembers();
    return (
      (battle.isMine || staff) &&
      (battle.status === 'Pending' || battle.status === 'AwaitingResults' || battle.status === 'Disputed')
    );
  }

  protected canEditArmyList(battle: PlayBattle, forceId: string): boolean {
    return this.canReportBattle(battle) && (this.canStaffMembers() || this.playForce(forceId)?.isMine === true);
  }

  protected isBattleExpanded(battleId: string): boolean {
    return this.expandedBattles()[battleId] !== false;
  }

  protected toggleBattle(battleId: string): void {
    this.expandedBattles.update((current) => ({ ...current, [battleId]: !this.isBattleExpanded(battleId) }));
  }

  protected isBattlePanelOpen(battleId: string, panel: string): boolean {
    return this.expandedBattlePanels()[`${battleId}:${panel}`] !== false;
  }

  protected toggleBattlePanel(battleId: string, panel: string): void {
    const key = `${battleId}:${panel}`;
    this.expandedBattlePanels.update((current) => ({ ...current, [key]: !this.isBattlePanelOpen(battleId, panel) }));
  }

  protected canCommitRetreat(battle: PlayBattle): boolean {
    return battle.needsRetreat === true && !battle.isRetreatCommitted && !!this.retreatTarget()[battle.id];
  }

  protected canCommitSurrenderFor(battle: PlayBattle): boolean {
    return battle.canSurrender === true && !battle.isRetreatCommitted && !!this.retreatTarget()[battle.id];
  }

  protected canCommitSurrender(): boolean {
    return this.pendingSurrenderBattle() !== null;
  }

  protected canCommitAnyRetreat(): boolean {
    return this.pendingRetreatBattle() !== null;
  }

  protected canUncommitRetreat(): boolean {
    return (this.play()?.battles ?? []).some((battle) => this.canUncommitBattleRetreat(battle));
  }

  protected canUncommitBattleRetreat(battle: PlayBattle): boolean {
    if (battle.isSurrenderCommitted === true) {
      return true;
    }

    return battle.isRetreatCommitted === true && (battle.retreatTargets?.length ?? 0) > 1;
  }

  private hasMapBattleCommit(): boolean {
    return (this.play()?.battles ?? []).some(
      (battle) =>
        battle.canSurrender === true ||
        battle.needsRetreat === true ||
        battle.isRetreatCommitted === true ||
        battle.isSurrenderCommitted === true,
    );
  }

  private pendingSurrenderBattle(): PlayBattle | null {
    return (this.play()?.battles ?? []).find((battle) => this.canCommitSurrenderFor(battle)) ?? null;
  }

  private pendingRetreatBattle(): PlayBattle | null {
    return (this.play()?.battles ?? []).find((battle) => this.canCommitRetreat(battle)) ?? null;
  }

  private battleForForce(force: PlayForce): PlayBattle | undefined {
    return this.play()?.battles.find((battle) => battle.participantForceIds.includes(force.id));
  }

  private ownForceRetreatAction(force: PlayForce, play: CampaignPlayDetail): MapForceAction | null {
    const battle = play.battles.find((item) => item.participantForceIds.includes(force.id));
    if (!battle) {
      return null;
    }

    const chosen = this.retreatTarget()[battle.id] ?? '';
    const dest = chosen.length > 0 ? chosen : (battle.retreatDraftTargetId ?? null);
    if (battle.isRetreatCommitted || battle.isSurrenderCommitted) {
      return {
        kind: battle.isSurrenderCommitted ? 'Surrender' : 'Retreat',
        status: 'committed',
        detail: dest ? `to ${this.territoryName(dest)}` : null,
      };
    }

    if (battle.canSurrender && dest) {
      return { kind: 'Surrender', status: 'draft', detail: `to ${this.territoryName(dest)}` };
    }

    if (battle.needsRetreat && dest) {
      return { kind: 'Retreat', status: 'draft', detail: `to ${this.territoryName(dest)}` };
    }

    return null;
  }

  protected isFinalRequiredRetreatCommitment(battle: PlayBattle): boolean {
    return this.isFinalRequiredBattleCommitment(battle);
  }

  protected isFinalRequiredBattleCommitment(battle: PlayBattle): boolean {
    const play = this.play();
    const viewerId = this.auth.currentUser()?.id ?? play?.forces.find((force) => force.isMine)?.controllerUserId;
    const awaiting = battle.needsRetreat || battle.canSurrender;
    if (!play || !viewerId || play.currentPhaseKind !== 'Battle' || !awaiting || battle.isRetreatCommitted) {
      return false;
    }

    const others = play.commitments.filter((item) => item.userId !== viewerId);
    if (!others.every((item) => item.isCommitted)) {
      return false;
    }

    return !play.battles.some((other) => {
      if (other.id === battle.id) {
        return false;
      }

      if (other.needsRetreat) {
        return true;
      }

      return this.canReportBattle(other) && !other.mySubmission;
    });
  }

  protected confirmingRetreatBattle(): PlayBattle | null {
    const battleId = this.confirmingRetreatBattleId();
    return battleId ? (this.play()?.battles.find((item) => item.id === battleId) ?? null) : null;
  }

  protected confirmingBattleCommitTitle(): string {
    const battle = this.confirmingRetreatBattle();
    if (battle?.canSurrender && !battle.needsRetreat) {
      return 'Commit surrender and close the phase?';
    }

    return 'Commit retreat and close the phase?';
  }

  protected requestRetreatCommit(battle: PlayBattle): void {
    if (this.canCommitSurrenderFor(battle) || this.canCommitRetreat(battle)) {
      this.confirmingRetreatBattleId.set(battle.id);
      return;
    }

    this.error.set(
      battle.canSurrender
        ? 'Choose a surrender destination before committing.'
        : 'Choose a retreat destination before committing.',
    );
  }

  protected cancelRetreatCommit(): void {
    this.confirmingRetreatBattleId.set(null);
  }

  protected async confirmRetreatCommit(): Promise<void> {
    const battle = this.confirmingRetreatBattle();
    this.confirmingRetreatBattleId.set(null);
    if (!battle) {
      return;
    }

    if (this.canCommitSurrenderFor(battle)) {
      await this.submitSurrender(battle);
      return;
    }

    await this.commitRetreat(battle);
  }

  private async uncommitReadyRetreats(): Promise<void> {
    const play = this.play();
    if (!play) {
      return;
    }

    const committed = play.battles.filter((battle) => battle.isRetreatCommitted);
    for (const battle of committed) {
      await this.uncommitRetreat(battle);
    }
  }

  protected battleResultCaption(battle: PlayBattle): string | null {
    if (battle.isNoContest) {
      return 'No contest';
    }

    if (battle.isDraw) {
      return 'Draw';
    }

    if (battle.winnerForceId) {
      return `Winner: ${this.forceLabelById(battle.winnerForceId)}`;
    }

    return null;
  }

  protected retreatDestinations(battle: PlayBattle): { id: string; name: string }[] {
    const seen = new Set<string>();
    const destinations: { id: string; name: string }[] = [];
    for (const id of battle.retreatTargets ?? []) {
      if (!id || seen.has(id)) {
        continue;
      }

      seen.add(id);
      destinations.push({ id, name: this.territoryName(id) });
    }

    return destinations;
  }

  protected questionLabel(question: MissionResultQuestion): string {
    return `${question.prompt} (${question.battlePoints} BP)`;
  }

  protected battleReportTotal(battle: PlayBattle, forceId: string): number {
    const report = this.reportFor(battle.id, forceId);
    let questionPoints = 0;
    for (const question of battle.resultQuestions ?? []) {
      if (question.kind === 'Boolean') {
        if (this.battleQuestionBoolean(battle.id, forceId, question.id)) {
          questionPoints += question.battlePoints;
        }
      } else {
        questionPoints += this.battleQuestionPoints(battle.id, forceId, question.id);
      }
    }

    return report.differentialBattlePoints + report.bonusBattlePoints + questionPoints;
  }

  protected battleReportValue(
    battleId: string,
    forceId: string,
    field: 'victoryPoints' | 'armyPoints' | 'differentialBattlePoints' | 'bonusBattlePoints' | 'supplyCostingUnitCount',
  ): number {
    return this.reportFor(battleId, forceId)[field];
  }

  protected onBattleReportNumber(
    battleId: string,
    forceId: string,
    field: 'victoryPoints' | 'armyPoints' | 'differentialBattlePoints' | 'bonusBattlePoints' | 'supplyCostingUnitCount',
    value: string | number | null,
  ): void {
    const parsed = typeof value === 'number' ? value : Number(value);
    this.patchReport(
      battleId,
      forceId,
      { [field]: Number.isFinite(parsed) ? Math.max(0, parsed) : 0 },
      field === 'armyPoints' || field === 'supplyCostingUnitCount' ? 'army' : 'result',
    );
  }

  protected battleReportFlag(battleId: string, forceId: string, field: 'usedExtraBlackPowder'): boolean {
    return this.reportFor(battleId, forceId)[field] === true;
  }

  protected onBattleReportFlag(battleId: string, forceId: string, field: 'usedExtraBlackPowder', value: boolean): void {
    this.patchReport(battleId, forceId, { [field]: value }, 'result');
  }

  protected battleQuestionBoolean(battleId: string, forceId: string, questionId: string): boolean {
    return this.answerFor(battleId, forceId, questionId).booleanValue === true;
  }

  protected onBattleQuestionBoolean(battle: PlayBattle, forceId: string, questionId: string, value: boolean): void {
    const question = (battle.resultQuestions ?? []).find((item) => item.id === questionId);
    this.patchAnswer(battle.id, forceId, questionId, {
      booleanValue: value,
      battlePointsValue: value ? (question?.battlePoints ?? 0) : 0,
    });
  }

  protected battleQuestionPoints(battleId: string, forceId: string, questionId: string): number {
    return this.answerFor(battleId, forceId, questionId).battlePointsValue ?? 0;
  }

  protected onBattleQuestionPoints(
    battleId: string,
    forceId: string,
    questionId: string,
    value: string | number | null,
  ): void {
    const parsed = typeof value === 'number' ? value : Number(value);
    this.patchAnswer(battleId, forceId, questionId, {
      booleanValue: null,
      battlePointsValue: Number.isFinite(parsed) ? Math.max(0, parsed) : 0,
    });
  }

  protected reportingForceIds(battle: PlayBattle): string[] {
    return battle.reportingForceIds?.length ? battle.reportingForceIds : battle.participantForceIds;
  }

  protected supplySpendHint(battle: PlayBattle, forceId: string): string {
    const report = this.reportFor(battle.id, forceId);
    const units = report.supplyCostingUnitCount + (report.usedExtraBlackPowder === true ? 1 : 0);
    const supply = battle.forceSupplies?.find((item) => item.forceId === forceId);
    const allowance = supply?.forceAllowancePoints ?? 0;
    const recurring = Math.min(units, allowance);
    const temporary = Math.max(0, units - allowance);
    if (temporary === 0) {
      return `Spends ${recurring} from territory and round supply.`;
    }

    return `Spends ${recurring} from territory and round supply, then ${temporary} temporary.`;
  }

  protected canUseExtraBlackPowder(forceId: string): boolean {
    return this.playForce(forceId)?.canUseExtraBlackPowder === true;
  }

  protected canUseMagicalSupply(forceId: string): boolean {
    return this.playForce(forceId)?.canUseMagicalSupply === true;
  }

  protected magicalSupplyRerolls(battleId: string, forceId: string): number {
    return this.reportFor(battleId, forceId).magicalSupplyRerolls ?? 0;
  }

  protected onMagicalSupplyRerolls(battleId: string, forceId: string, value: string | number | null): void {
    const parsed = typeof value === 'number' ? value : Number(value);
    this.patchReport(battleId, forceId, {
      magicalSupplyRerolls: Number.isFinite(parsed) ? Math.max(0, parsed) : 0,
    });
  }

  protected opponentSpecialRuleUse(battle: PlayBattle, forceId: string): string | null {
    const report = battle.opponentSubmission?.reports?.find((item) => item.forceId === forceId);
    if (!report) {
      return null;
    }

    const parts: string[] = [];
    if (report.usedExtraBlackPowder === true) {
      parts.push('Used Extra Black Powder');
    }

    if ((report.magicalSupplyRerolls ?? 0) > 0) {
      parts.push(`Magical Supply rerolls: ${report.magicalSupplyRerolls}`);
    }

    return parts.length > 0 ? parts.join(' · ') : null;
  }

  private playForce(forceId: string): PlayForce | undefined {
    return this.play()?.forces.find((force) => force.id === forceId);
  }

  protected standardSupplyBreakdown(supply: PlayBattleForceSupply): string {
    const parts = [`map ${supply.mapSupplyPoints ?? 0}`];
    if ((supply.roundFreeSupplyPoints ?? 0) > 0) {
      parts.push(`round +${supply.roundFreeSupplyPoints}`);
    }
    if ((supply.splitPenaltyPoints ?? 0) > 0) {
      parts.push(`split −${supply.splitPenaltyPoints}`);
    }

    return parts.join(' · ');
  }

  protected armyListText(battleId: string, forceId: string): string {
    return this.reportFor(battleId, forceId).armyListText ?? '';
  }

  protected onArmyListText(battleId: string, forceId: string, value: string): void {
    this.patchReport(battleId, forceId, { armyListText: value }, 'army');
    this.scheduleArmyListParse(battleId, forceId);
  }

  protected armyListGameSystem(battleId: string, forceId: string): string {
    return this.reportFor(battleId, forceId).armyListGameSystem ?? 'WarhammerTheOldWorld';
  }

  protected onArmyListGameSystem(battleId: string, forceId: string, value: string): void {
    this.patchReport(battleId, forceId, { armyListGameSystem: value }, 'army');
    this.scheduleArmyListParse(battleId, forceId);
  }

  protected armyListBuilder(battleId: string, forceId: string): string {
    return this.reportFor(battleId, forceId).armyListBuilder ?? 'Other';
  }

  protected onArmyListBuilder(battleId: string, forceId: string, value: string): void {
    this.patchReport(battleId, forceId, { armyListBuilder: value }, 'army');
    this.setArmyListParseMessage(battleId, forceId, '');
    this.scheduleArmyListParse(battleId, forceId);
  }

  protected armyListParseMessage(battleId: string, forceId: string): string {
    return this.armyListParseMessages()[this.armyListKey(battleId, forceId)] ?? '';
  }

  protected armyListCategories(battleId: string, forceId: string): ArmyListSupplyCategory[] {
    return this.reportFor(battleId, forceId).supplyCategories ?? [];
  }

  protected onArmyListCategorySupply(
    battleId: string,
    forceId: string,
    name: string,
    value: string | number | null,
  ): void {
    const parsed = typeof value === 'number' ? value : Number(value);
    const supplyPoints = Number.isFinite(parsed) ? Math.max(0, parsed) : 0;
    const report = this.reportFor(battleId, forceId);
    const categories = (report.supplyCategories ?? []).map((category) =>
      category.name === name ? { ...category, supplyPoints } : category,
    );
    const supplyCostingUnitCount = categories
      .filter((category) => category.costsSupply)
      .reduce((sum, category) => sum + category.supplyPoints, 0);
    this.patchReport(battleId, forceId, { supplyCategories: categories, supplyCostingUnitCount }, 'army');
  }

  protected submittedArmyList(battle: PlayBattle, forceId: string): string | null {
    const text = battle.armyLists?.find((list) => list.forceId === forceId)?.armyListText?.trim();
    if (!text) {
      return null;
    }

    return text;
  }

  private scheduleArmyListParse(battleId: string, forceId: string): void {
    const key = this.armyListKey(battleId, forceId);
    const existing = this.armyListParseTimers.get(key);
    if (existing) {
      globalThis.clearTimeout(existing);
    }

    this.armyListParseTimers.set(
      key,
      globalThis.setTimeout(() => {
        this.armyListParseTimers.delete(key);
        void this.parseArmyList(battleId, forceId);
      }, 400),
    );
  }

  private async parseArmyList(battleId: string, forceId: string): Promise<void> {
    const play = this.play();
    const report = this.reportFor(battleId, forceId);
    const builder = report.armyListBuilder ?? 'Other';
    if (!play || builder === 'Other' || !(report.armyListText ?? '').trim()) {
      this.setArmyListParseMessage(battleId, forceId, '');
      return;
    }

    try {
      const result = await this.campaignsApi.parseArmyList(play.id, {
        gameSystem: report.armyListGameSystem ?? 'WarhammerTheOldWorld',
        builder,
        text: report.armyListText,
      });
      if (!result.parsed) {
        this.setArmyListParseMessage(battleId, forceId, result.message ?? '');
        return;
      }

      this.setArmyListParseMessage(battleId, forceId, '');
      this.patchReport(
        battleId,
        forceId,
        {
          armyPoints: result.armyPoints,
          supplyCostingUnitCount: result.supplyCostingUnitCount,
          supplyCategories: result.categories,
        },
        'army',
      );
    } catch {
      this.setArmyListParseMessage(
        battleId,
        forceId,
        'The list could not be parsed. Enter the supply points manually.',
      );
    }
  }

  private setArmyListParseMessage(battleId: string, forceId: string, message: string): void {
    const key = this.armyListKey(battleId, forceId);
    this.armyListParseMessages.update((current) => {
      const next = { ...current };
      if (message) {
        next[key] = message;
      } else {
        delete next[key];
      }

      return next;
    });
  }

  private armyListKey(battleId: string, forceId: string): string {
    return `${battleId}:${forceId}`;
  }

  private reportsFor(battle: PlayBattle): BattleParticipantReport[] {
    return this.reportingForceIds(battle).map((forceId) => this.reportFor(battle.id, forceId));
  }

  private reportFor(battleId: string, forceId: string): BattleParticipantReport {
    const existing = (this.battleReports()[battleId] ?? []).find((report) => report.forceId === forceId);
    return existing ?? this.emptyBattleReport(forceId);
  }

  private emptyBattleReport(forceId: string): BattleParticipantReport {
    return {
      forceId,
      victoryPoints: 0,
      armyPoints: 0,
      differentialBattlePoints: 0,
      bonusBattlePoints: 0,
      supplyCostingUnitCount: 0,
      usedExtraBlackPowder: false,
      magicalSupplyRerolls: 0,
      armyListText: '',
      armyListGameSystem: 'WarhammerTheOldWorld',
      armyListBuilder: 'Other',
      supplyCategories: [],
      answers: [],
    };
  }

  private answerFor(
    battleId: string,
    forceId: string,
    questionId: string,
  ): { booleanValue?: boolean | null; battlePointsValue?: number | null } {
    return (
      this.reportFor(battleId, forceId).answers.find((answer) => answer.questionId === questionId) ?? {
        booleanValue: false,
        battlePointsValue: 0,
      }
    );
  }

  private patchReport(
    battleId: string,
    forceId: string,
    patch: Partial<BattleParticipantReport>,
    kind: 'result' | 'army' = 'result',
  ): void {
    this.battleReports.update((current) => {
      const reports = [...(current[battleId] ?? [])];
      const index = reports.findIndex((report) => report.forceId === forceId);
      const next = { ...this.reportFor(battleId, forceId), ...patch };
      if (index >= 0) {
        reports[index] = next;
      } else {
        reports.push(next);
      }

      return { ...current, [battleId]: reports };
    });
    if (kind === 'army') {
      this.markArmyListDirty(battleId, forceId);
    } else {
      this.markBattleResultDirty(battleId);
    }
  }

  private patchAnswer(
    battleId: string,
    forceId: string,
    questionId: string,
    patch: { booleanValue?: boolean | null; battlePointsValue?: number | null },
  ): void {
    const report = this.reportFor(battleId, forceId);
    const answers = [...report.answers];
    const index = answers.findIndex((answer) => answer.questionId === questionId);
    const next = { questionId, ...this.answerFor(battleId, forceId, questionId), ...patch };
    if (index >= 0) {
      answers[index] = next;
    } else {
      answers.push(next);
    }

    this.patchReport(battleId, forceId, { answers });
  }

  private markBattleResultDirty(battleId: string): void {
    this.dirtyBattleResultIds.update((current) => new Set(current).add(battleId));
  }

  private markArmyListDirty(battleId: string, forceId: string): void {
    this.dirtyArmyListKeys.update((current) => new Set(current).add(this.armyListKey(battleId, forceId)));
  }

  private ownedArmyListReports(battle: PlayBattle): BattleParticipantReport[] {
    return this.reportingForceIds(battle)
      .filter((forceId) => this.canEditArmyList(battle, forceId))
      .map((forceId) => this.reportFor(battle.id, forceId));
  }

  private ownArmyPointsMissing(battle: PlayBattle, forceId?: string): boolean {
    const forceIds = forceId
      ? [forceId]
      : this.reportingForceIds(battle).filter((id) => this.canEditArmyList(battle, id));
    return forceIds.some((id) => {
      const report = this.reportFor(battle.id, id);
      const submitted = battle.armyLists?.find((item) => item.forceId === id);
      return report.armyPoints <= 0 && (!submitted || submitted.armyPoints <= 0);
    });
  }

  private latestResultSubmission(battle: PlayBattle): PlayBattleSubmission | null {
    const candidates = [battle.mySubmission, battle.opponentSubmission].filter(
      (item): item is PlayBattleSubmission => item !== null,
    );
    if (candidates.length === 0) {
      return null;
    }

    return [...candidates].sort(
      (left, right) => Date.parse(right.submittedUtc ?? '') - Date.parse(left.submittedUtc ?? ''),
    )[0];
  }

  protected leaderboardHeading(board: PublicObjectiveLeaderboard): string {
    const trimmed = board.title?.trim() ?? '';
    const title = trimmed.length > 0 ? trimmed : this.leaderboardTitle(board.kind);
    return `${title} (${board.awardPoints} CP)`;
  }

  protected leaderboardTitle(kind: string): string {
    switch (kind) {
      case 'MostTerritories':
        return 'Most territories';
      case 'LongestTerritoryChain':
        return 'Longest territory chain';
      case 'MostBattlesWon':
        return 'Most battles won';
      case 'MostStructurePoints':
        return 'Most structure points';
      case 'PointsPerTerritory':
        return 'Campaign points per territory';
      case 'NamedPublicObjective':
        return 'Public objective';
      default:
        return kind;
    }
  }

  protected leaderboardMetric(kind: string, leader: PublicObjectiveLeader): string {
    if (kind === 'MostBattlesWon') {
      return `${leader.metric} wins, ${leader.tieBreakMetric} draws`;
    }

    if (kind === 'LongestTerritoryChain') {
      return `${leader.metric} territories in chain`;
    }

    if (kind === 'MostStructurePoints') {
      return `${leader.metric} structure points`;
    }

    if (kind === 'NamedPublicObjective') {
      return 'Awarded';
    }

    return `${leader.metric} territories`;
  }

  protected onRetreatTarget(battleId: string, targetTerritoryId: string): void {
    this.retreatTarget.update((current) => ({ ...current, [battleId]: targetTerritoryId }));
  }

  private assignRetreatFromMap(territoryId: string): void {
    const play = this.play();
    if (!play) {
      return;
    }

    for (const battle of play.battles) {
      if (
        (!battle.needsRetreat && !battle.canSurrender) ||
        battle.isRetreatCommitted ||
        !(battle.retreatTargets ?? []).includes(territoryId)
      ) {
        continue;
      }

      this.onRetreatTarget(battle.id, territoryId);
    }
  }

  protected async saveDraft(force: PlayForce): Promise<void> {
    const play = this.play();
    if (!play) {
      return;
    }

    const draft = this.draftFor(force.id);
    await this.runPlay(() =>
      this.campaignsApi.saveDraft(play.id, {
        revision: play.revision,
        forceId: force.id,
        kind: draft.kind,
        targetTerritoryId: draft.targetTerritoryId || null,
        structureTypeId: draft.structureTypeId || null,
        viaTerritoryId: draft.viaTerritoryId || null,
        viaPath: draft.viaPath.length > 0 ? draft.viaPath : null,
        destroyImmediately: draft.destroyImmediately,
        droppedItemObjectiveIds: draft.kind === 'Move' ? draft.droppedItemObjectiveIds : [],
      }),
    );
  }

  protected async commit(): Promise<void> {
    const play = this.play();
    if (!play) {
      return;
    }

    await this.runPlay(() => this.campaignsApi.commitOrders(play.id, { revision: play.revision }));
  }

  protected async uncommit(): Promise<void> {
    const play = this.play();
    if (!play) {
      return;
    }

    await this.runPlay(() => this.campaignsApi.uncommitOrders(play.id, { revision: play.revision }));
  }

  protected async submitBattle(battle: PlayBattle): Promise<void> {
    const play = this.play();
    if (!play) {
      return;
    }

    if (this.ownArmyPointsMissing(battle)) {
      this.error.set('Enter the army points you used before submitting a result.');
      return;
    }

    const winnerChoice = this.battleWinner()[battle.id] ?? '';
    const isDraw = winnerChoice === 'draw';
    const winnerForceId =
      battle.isRinger && (winnerChoice === 'ringer' || winnerChoice === '')
        ? null
        : winnerChoice && winnerChoice !== 'draw'
          ? winnerChoice
          : null;
    await this.runPlay(() =>
      this.campaignsApi.submitBattleResult(play.id, {
        revision: play.revision,
        battleId: battle.id,
        winnerForceId,
        isDraw,
        reports: this.reportsFor(battle),
      }),
    );
  }

  protected async acceptBattle(battle: PlayBattle): Promise<void> {
    const play = this.play();
    if (!play) {
      return;
    }

    if (this.ownArmyPointsMissing(battle)) {
      this.error.set('Enter the army points you used before agreeing to a result.');
      return;
    }

    await this.runPlay(() =>
      this.campaignsApi.acceptBattleResult(play.id, battle.id, play.revision, this.ownedArmyListReports(battle)),
    );
  }

  protected async submitArmyList(battle: PlayBattle, forceId: string): Promise<void> {
    const play = this.play();
    if (!play || !this.canEditArmyList(battle, forceId)) {
      return;
    }

    if (this.ownArmyPointsMissing(battle, forceId)) {
      this.error.set('Enter the army points you used before submitting an army list.');
      return;
    }

    await this.runPlay(() =>
      this.campaignsApi.submitArmyList(play.id, {
        revision: play.revision,
        battleId: battle.id,
        reports: [this.reportFor(battle.id, forceId)],
      }),
    );
  }

  protected async resolveBattle(battle: PlayBattle): Promise<void> {
    const play = this.play();
    if (!play) {
      return;
    }

    await this.runPlay(() =>
      this.campaignsApi.resolveBattle(play.id, {
        revision: play.revision,
        battleId: battle.id,
        winnerForceId: null,
        isDraw: false,
        reports: this.reportsFor(battle),
      }),
    );
  }

  protected async commitRetreat(battle: PlayBattle): Promise<void> {
    const play = this.play();
    const targetTerritoryId = this.retreatTarget()[battle.id];
    if (!play) {
      return;
    }

    if (!targetTerritoryId) {
      this.error.set('Choose a retreat destination before committing.');
      return;
    }

    await this.runPlay(() =>
      this.campaignsApi.submitRetreat(play.id, {
        revision: play.revision,
        battleId: battle.id,
        targetTerritoryId,
      }),
    );
  }

  protected async uncommitRetreat(battle: PlayBattle): Promise<void> {
    const play = this.play();
    if (!play) {
      return;
    }

    await this.runPlay(() =>
      this.campaignsApi.uncommitRetreat(play.id, {
        revision: play.revision,
        battleId: battle.id,
      }),
    );
  }

  protected surrenderConfirmLabel(battle: PlayBattle): string {
    const territory = this.territoryName(battle.territoryId);
    const opponentForce = this.play()?.forces.find(
      (force) => battle.participantForceIds.includes(force.id) && !force.isMine,
    );
    const opponent = this.factionName(opponentForce?.factionId);
    if (opponentForce && opponent !== 'Unknown faction') {
      return `Surrender ${territory} to ${opponent}? You can uncommit while this window stays open.`;
    }

    return `Surrender ${territory}? You can uncommit while this window stays open.`;
  }

  protected async submitSurrender(battle: PlayBattle): Promise<void> {
    const play = this.play();
    const targetTerritoryId = this.retreatTarget()[battle.id];
    if (!play) {
      return;
    }

    if (!targetTerritoryId) {
      this.error.set('Choose a surrender destination before confirming.');
      return;
    }

    await this.runPlay(() =>
      this.campaignsApi.submitSurrender(play.id, {
        revision: play.revision,
        battleId: battle.id,
        targetTerritoryId,
      }),
    );
  }

  protected async extendSchedule(): Promise<void> {
    const play = this.play();
    if (!play) {
      return;
    }

    const windowId = this.extensionWindowId() || play.remainingWindows[0]?.id;
    const extensions = windowId
      ? [{ windowId, durationAmount: this.extensionAmount(), durationUnit: this.extensionUnit() }]
      : [];
    await this.runPlay(() =>
      this.campaignsApi.extendSchedule(play.id, {
        revision: play.revision,
        roundCount: this.roundCount(),
        extensions,
      }),
    );
  }

  protected async injectRingerBattle(): Promise<void> {
    const play = this.play();
    const campaign = this.campaign();
    if (!play || !campaign) {
      return;
    }

    const targetForceId = this.ringerForceId() || this.ringerTargetForces()[0]?.id;
    const ringerFactionId = this.ringerFactionId() || campaign.factions[0]?.id;
    if (!targetForceId || !ringerFactionId) {
      return;
    }

    await this.runPlay(() =>
      this.campaignsApi.injectRingerBattle(play.id, {
        revision: play.revision,
        targetForceId,
        ringerFactionId,
        missionId: this.ringerMissionId() || null,
        playerIsDefender: this.ringerPlayerIsDefender(),
      }),
    );
  }

  protected assignableForces(): PlayForce[] {
    return this.play()?.forces ?? [];
  }

  protected catalogStatusNames(): string[] {
    const statuses = this.play()?.forceStatuses ?? this.campaign()?.forceStatuses ?? [];
    return statuses.map((status) => status.name.trim()).filter((name) => name.length > 0);
  }

  protected async assignForceStatuses(): Promise<void> {
    const play = this.play();
    if (!play) {
      return;
    }

    const all = this.forceStatusAll();
    const forceId = this.forceStatusForceId() || this.assignableForces()[0]?.id;
    if (!all && !forceId) {
      return;
    }

    await this.runPlay(() =>
      this.campaignsApi.setForceStatuses(play.id, {
        revision: play.revision,
        forceIds: all ? [] : [forceId],
        statusName: this.forceStatusName() || 'Normal',
      }),
    );
  }

  protected async enterDebug(): Promise<void> {
    const play = this.play();
    if (!play) {
      return;
    }

    await this.runPlay(() => this.campaignsApi.enterDebug(play.id, { revision: play.revision }));
  }

  protected async exitDebug(): Promise<void> {
    const play = this.play();
    if (!play) {
      return;
    }

    await this.runPlay(() => this.campaignsApi.exitDebug(play.id, { revision: play.revision }));
  }

  protected async revealHiddenObjectives(): Promise<void> {
    const play = this.play();
    if (!play) {
      return;
    }

    await this.runPlay(() => this.campaignsApi.revealHiddenObjectives(play.id, { revision: play.revision }));
  }

  protected async applyDebugCorrection(force: PlayForce, reResolvePrevious = false): Promise<void> {
    const play = this.play();
    if (!play) {
      return;
    }

    const draft = this.debugDraftFor(force.id);
    await this.runPlay(() =>
      this.campaignsApi.debugCorrectOrder(play.id, {
        revision: play.revision,
        forceId: force.id,
        kind: draft.kind,
        targetTerritoryId: draft.targetTerritoryId || null,
        structureTypeId: draft.structureTypeId || null,
        viaTerritoryId: draft.viaTerritoryId || null,
        viaPath: draft.viaPath.length > 0 ? draft.viaPath : null,
        destroyImmediately: draft.destroyImmediately,
        droppedItemObjectiveIds: draft.kind === 'Move' ? draft.droppedItemObjectiveIds : [],
        reResolvePrevious,
      }),
    );
  }

  protected async downloadMap(): Promise<void> {
    const campaign = this.campaign();
    const imageUrl = this.mapSrc();
    if (!campaign || !imageUrl) {
      return;
    }

    this.downloading.set(true);
    this.error.set(null);
    try {
      const blob = await rasterizeMapPng(imageUrl, this.graph().territories, {
        factions: campaign.factions,
        structures: campaign.structureTypes,
        structureImageUrl: this.structureImageUrl,
        flagImageUrl: this.flagImageUrl,
      });
      downloadBlob(blob, mapDownloadFilename(campaign.name));
    } catch {
      this.error.set('Unable to download the map.');
    } finally {
      this.downloading.set(false);
    }
  }

  protected downloadSvg(): void {
    const campaign = this.campaign();
    if (!campaign) {
      return;
    }

    const blob = new Blob([serializeMapSvg(this.graph(), mapSvgCatalogFrom(campaign))], { type: 'image/svg+xml' });
    downloadBlob(blob, svgDownloadFilename(campaign.name));
  }

  protected timeZoneId(): string | null {
    return this.auth.currentUser()?.timeZoneId ?? null;
  }

  protected locationText(campaign: CampaignDetail): string | null {
    return formatLocation(campaign.city, campaign.region, campaign.country);
  }

  protected roleLabel(campaign: CampaignDetail): string {
    if (campaign.canManage && campaign.isParticipant) {
      return 'Manager and player';
    }

    if (campaign.canManage) {
      return 'Manager';
    }

    if (campaign.isParticipant) {
      return 'Player';
    }

    return 'Viewer';
  }

  protected statusText(status: string): string {
    return statusLabel(status);
  }

  protected roundLengthText(campaign: CampaignDetail): string {
    return formatDuration(campaign.roundLengthAmount, campaign.roundLengthUnit);
  }

  protected phaseText(campaign: CampaignDetail, index: number): string {
    const phase = campaign.phases.at(index);
    if (!phase) {
      return '';
    }

    return `${formatPhaseLabel(phase.kind, actionNumberAt(campaign.phases, index))} · ${formatDuration(phase.durationAmount, phase.durationUnit)}`;
  }

  protected currentPhaseHeading(campaign: CampaignDetail): string {
    const play = this.play();
    const round = play?.currentRound ?? campaign.currentRound;
    const label =
      play?.currentPhaseLabel ??
      (campaign.currentPhaseKind && campaign.currentPhaseNumber !== null
        ? formatPhaseLabel(campaign.currentPhaseKind, actionNumberAt(campaign.phases, campaign.currentPhaseNumber - 1))
        : null);
    if (round === null || !label) {
      return '';
    }

    return `Round ${round} · ${label}`;
  }

  protected phaseEndTimestamp(endsUtc: string): string {
    return formatPhaseEndTimestamp(endsUtc, this.timeZoneId(), this.auth.currentUser()?.dateTimeDisplayFormat);
  }

  protected onPhaseExpired(): void {
    void this.refreshBoard();
  }

  protected requestEnd(): void {
    this.confirmingEnd.set(true);
  }

  protected cancelEnd(): void {
    this.confirmingEnd.set(false);
  }

  protected async confirmEnd(): Promise<void> {
    const campaign = this.campaign();
    if (!campaign) {
      return;
    }

    this.ending.set(true);
    this.error.set(null);
    try {
      const revision = this.play()?.revision ?? campaign.revision;
      await this.campaignsApi.end(campaign.id, revision);
      this.confirmingEnd.set(false);
      this.ending.set(false);
      await this.router.navigateByUrl('/campaigns');
    } catch (error: unknown) {
      this.error.set(readApiError(error, 'Unable to end this campaign.'));
      this.confirmingEnd.set(false);
      this.ending.set(false);
    }
  }

  protected requestDelete(): void {
    this.confirmingDelete.set(true);
  }

  protected cancelDelete(): void {
    this.confirmingDelete.set(false);
  }

  protected async confirmDelete(): Promise<void> {
    const campaign = this.campaign();
    if (!campaign) {
      return;
    }

    this.deleting.set(true);
    this.error.set(null);
    try {
      await this.campaignsApi.delete(campaign.id);
      this.confirmingDelete.set(false);
      this.deleting.set(false);
      await this.router.navigateByUrl('/campaigns');
    } catch (error: unknown) {
      this.error.set(readApiError(error, 'Unable to delete this campaign.'));
      this.confirmingDelete.set(false);
      this.deleting.set(false);
    }
  }

  protected canSaveForceDraft(force: PlayForce): boolean {
    const play = this.play();
    const draft = this.draftFor(force.id);
    return (
      !!play && !play.isCommitted && this.isDraftReady(force, draft) && !this.draftMatchesSaved(play, force.id, draft)
    );
  }

  protected mapMenuActions(): readonly string[] {
    const flow = this.mapAction();
    const force = this.myForces().find((item) => item.id === flow?.forceId);
    if (!force) {
      return [];
    }

    if (force.inBattle) {
      const battle = this.battleForForce(force);
      const kinds: string[] = [];
      if (battle?.canSurrender) {
        kinds.push('Surrender');
      }

      if (battle?.needsRetreat) {
        kinds.push('Retreat');
      }

      return kinds;
    }

    if (force.isRandomTeleportLocked === true) {
      return [];
    }

    return force.availableActions;
  }

  protected mapActionPrompt(): string | null {
    const flow = this.mapAction();
    if (flow?.step === 'pick-target') {
      if (flow.kind === 'Move') {
        return 'Pick a territory to move to...';
      }

      if (flow.kind === 'Split') {
        return 'Pick a territory to split forces to...';
      }

      if (this.isChosenTeleportKind(flow.kind)) {
        return 'Pick a non-spawn territory to teleport to...';
      }

      if (flow.kind === 'Surrender') {
        return 'Pick a territory to surrender to...';
      }

      if (flow.kind === 'Retreat') {
        return 'Pick a territory to retreat to...';
      }

      return `Select a destination for ${flow.kind}.`;
    }

    if (flow?.step === 'pick-via') {
      return 'Select the territory to move through.';
    }

    if (flow?.step === 'pick-structure') {
      return 'Select a structure to build.';
    }

    if (!flow && this.pendingRetreatTargets().length > 0) {
      return 'Pick a territory to retreat to...';
    }

    return null;
  }

  protected mapPromptAvoidIds(): readonly string[] {
    return this.adjacentTerritoryIds();
  }

  protected confirmActionSummary(): string {
    const flow = this.mapAction();
    if (!flow) {
      return '';
    }

    const origin = this.territoryName(flow.originId);
    if (
      flow.kind === 'Move' ||
      flow.kind === 'Split' ||
      this.isChosenTeleportKind(flow.kind) ||
      flow.kind === 'Surrender' ||
      flow.kind === 'Retreat'
    ) {
      const destination = this.territoryName(flow.targetTerritoryId);
      const hops = [flow.viaTerritoryId, ...flow.viaPath].filter((id) => id.length > 0);
      if (hops.length === 0 || this.isChosenTeleportKind(flow.kind)) {
        return `${this.actionKindLabel(flow.kind)} from ${origin} to ${destination}?`;
      }

      const through = hops.map((id) => this.territoryName(id)).join(' and ');
      return `${this.actionKindLabel(flow.kind)} from ${origin} through ${through} to ${destination}?`;
    }

    if (flow.kind === 'Build') {
      const name = this.buildableStructures().find((type) => type.id === flow.structureTypeId)?.name ?? 'a structure';
      return `Build ${name} in ${origin}?`;
    }

    return `${this.actionKindLabel(flow.kind)} in ${origin}?`;
  }

  protected onMapActionKind(kind: string): void {
    const flow = this.mapAction();
    if (!flow) {
      return;
    }

    const force = this.myForces().find((item) => item.id === flow.forceId);
    if (
      kind === 'Move' ||
      kind === 'Split' ||
      kind === 'Surrender' ||
      kind === 'Retreat' ||
      (force && this.canChooseTeleport(force, kind))
    ) {
      this.mapAction.set({
        ...flow,
        step: 'pick-target',
        kind,
        targetTerritoryId: '',
        viaTerritoryId: '',
        viaPath: [],
        viaCandidates: [],
        structureTypeId: '',
      });
      this.selectedIds.set([flow.originId]);
      return;
    }

    if (kind === 'Build') {
      this.mapAction.set({
        ...flow,
        step: 'pick-structure',
        kind,
        targetTerritoryId: '',
        viaTerritoryId: '',
        viaPath: [],
        viaCandidates: [],
        structureTypeId: '',
      });
      return;
    }

    this.mapAction.set({
      ...flow,
      step: 'confirm',
      kind,
      targetTerritoryId: '',
      viaTerritoryId: '',
      viaPath: [],
      viaCandidates: [],
      structureTypeId: '',
    });
  }

  protected onMapStructurePicked(structureTypeId: string): void {
    const flow = this.mapAction();
    if (!flow) {
      return;
    }

    this.mapAction.set({ ...flow, step: 'confirm', kind: 'Build', structureTypeId, targetTerritoryId: '' });
  }

  protected cancelMapAction(): void {
    this.mapAction.set(null);
  }

  protected async confirmMapAction(): Promise<void> {
    const flow = this.mapAction();
    const force = this.myForces().find((item) => item.id === flow?.forceId);
    if (!flow || !force) {
      return;
    }

    this.markDraftDirty(force.id);
    this.drafts.update((drafts) => ({
      ...drafts,
      [force.id]: emptyOrderDraft({
        kind: flow.kind,
        targetTerritoryId: flow.targetTerritoryId,
        structureTypeId: flow.structureTypeId,
        viaTerritoryId: flow.viaTerritoryId,
        viaPath: flow.viaPath,
        droppedItemObjectiveIds: flow.kind === 'Move' ? flow.droppedItemObjectiveIds : [],
      }),
    }));
    this.cancelMapAction();
    await this.saveDraft(force);
  }

  private handleMapActionSelect(event: { id: string; additive: boolean; clientX?: number; clientY?: number }): boolean {
    const play = this.play();
    if (!play?.isParticipant || play.canChooseFaction) {
      return false;
    }

    const flow = this.mapAction();
    if (flow?.step === 'pick-via') {
      if (flow.viaCandidates.includes(event.id)) {
        this.applyMapVia(flow, event.id);
        return true;
      }

      this.cancelMapAction();
    } else if (flow?.step === 'pick-target') {
      const force = this.myForces().find((item) => item.id === flow.forceId);
      if (force && this.destinationTargets(force, flow.kind).includes(event.id) && event.id !== flow.originId) {
        this.applyMapDestination(flow, force, event.id);
        return true;
      }

      this.cancelMapAction();
    } else if (flow) {
      this.cancelMapAction();
    }

    if (!this.isActionPhase() || play.isCommitted) {
      return !!flow;
    }

    const occupying = this.myForces().find(
      (item) => item.territoryId === event.id && !item.inBattle && item.availableActions.length > 0,
    );
    if (!occupying) {
      return false;
    }

    this.openForceMapMenu(occupying, event.clientX ?? 0, event.clientY ?? 0);
    return true;
  }

  private menuPosition(clientX: number, clientY: number): { x: number; y: number } {
    const rect = this.mapBoard()?.nativeElement.getBoundingClientRect();
    if (!rect) {
      return { x: clientX, y: clientY };
    }

    return {
      x: Math.min(Math.max(clientX - rect.left + 12, 8), Math.max(rect.width - 180, 8)),
      y: Math.min(Math.max(clientY - rect.top, 8), Math.max(rect.height - 12, 8)),
    };
  }

  private isDraftReady(force: PlayForce, draft: OrderDraft | undefined): boolean {
    if (!draft || force.availableActions.length === 0 || !force.availableActions.includes(draft.kind)) {
      return false;
    }

    if (draft.kind === 'Move' || draft.kind === 'Split') {
      if (draft.targetTerritoryId.length === 0) {
        return false;
      }

      return !this.requiresViaChoice(force, draft.targetTerritoryId) || draft.viaTerritoryId.length > 0;
    }

    if (draft.kind === 'Teleport' || draft.kind === 'TeleportToSpecificTerritory') {
      return !this.canChooseTeleport(force, draft.kind) || draft.targetTerritoryId.length > 0;
    }

    if (draft.kind === 'Build') {
      return draft.structureTypeId.length > 0;
    }

    return true;
  }

  private draftMatchesSaved(play: CampaignPlayDetail, forceId: string, draft: OrderDraft): boolean {
    const saved = play.myDrafts.find((item) => item.forceId === forceId);
    return (
      saved?.kind === draft.kind &&
      (saved.targetTerritoryId ?? '') === draft.targetTerritoryId &&
      (saved.structureTypeId ?? '') === draft.structureTypeId &&
      (saved.viaTerritoryId ?? '') === draft.viaTerritoryId &&
      (saved.viaPath ?? []).join(',') === draft.viaPath.join(',') &&
      (saved.destroyImmediately === true) === draft.destroyImmediately &&
      (saved.droppedItemObjectiveIds ?? []).join(',') === draft.droppedItemObjectiveIds.join(',')
    );
  }

  private async runPlay(work: () => Promise<CampaignPlayDetail>): Promise<void> {
    this.error.set(null);
    this.successMessage.set(null);
    try {
      const next = await this.overlay.run(work);
      this.applyPlay(next);
      this.successMessage.set(FORM_SAVE_SUCCESS_MESSAGE);
    } catch (error: unknown) {
      if (isConcurrencyConflict(error)) {
        await this.refreshBoardAfterConflict();
        return;
      }

      this.error.set(readApiError(error, 'Unable to save play changes.'));
    }
  }

  private applyPlay(play: CampaignPlayDetail, options?: { preserveLocalWork?: boolean }): void {
    const previous = this.play();
    if (options?.preserveLocalWork && previous && play.revision <= previous.revision) {
      return;
    }

    const previousRevision = previous?.revision ?? this.campaign()?.revision ?? 0;
    const preserveLocalWork = options?.preserveLocalWork === true;
    this.play.set(play);
    if (!preserveLocalWork) {
      this.cancelMapAction();
    }

    this.roundCount.set(play.roundCount);
    this.extensionWindowId.set(play.remainingWindows[0]?.id ?? '');
    const drafts = this.draftsFromPlay(play);
    if (preserveLocalWork) {
      this.drafts.set(this.mergeDirtyDrafts(this.drafts(), drafts, this.dirtyDraftForceIds(), play.forces));
      this.dirtyDraftForceIds.set(this.retainedDirtyForceIds(this.dirtyDraftForceIds(), play.forces));
    } else {
      this.dirtyDraftForceIds.set(new Set());
      this.drafts.set(drafts);
    }

    const debugDrafts = this.debugDraftsFromPlay(play);
    if (preserveLocalWork) {
      this.debugDrafts.set(
        this.mergeDirtyDrafts(this.debugDrafts(), debugDrafts, this.dirtyDebugDraftForceIds(), play.forces),
      );
      this.dirtyDebugDraftForceIds.set(this.retainedDirtyForceIds(this.dirtyDebugDraftForceIds(), play.forces));
    } else {
      this.dirtyDebugDraftForceIds.set(new Set());
      this.debugDrafts.set(debugDrafts);
    }

    this.campaign.update((current) =>
      current
        ? {
            ...current,
            revision: play.revision,
            status: play.status,
            currentRound: play.currentRound,
            currentPhaseNumber: play.currentPhaseNumber,
            currentPhaseKind: play.currentPhaseKind,
            currentPhaseStartsUtc: play.currentPhaseStartsUtc,
            currentPhaseEndsUtc: play.currentPhaseEndsUtc,
            factionId: play.factionId ?? current.factionId,
            canChooseFaction: play.canChooseFaction,
            canChat: play.canChat,
            canInspectPrivateChat: play.canInspectPrivateChat,
            mentionableMembers: play.mentionableMembers,
            chatChannels: play.chatChannels,
            log: play.log,
            standings: play.standings ?? current.standings,
            publicObjectiveLeaderboards: play.publicObjectiveLeaderboards ?? current.publicObjectiveLeaderboards,
            brokenAllyFactionIds: play.brokenAllyFactionIds ?? current.brokenAllyFactionIds,
            privateObjectives: play.privateObjectives ?? current.privateObjectives,
            privateObjectiveUnclaimedCounts:
              play.privateObjectiveUnclaimedCounts ?? current.privateObjectiveUnclaimedCounts,
            specialRules: mergeSpecialRuleCatalogs(play.specialRules, current.specialRules),
            forceStatuses: play.forceStatuses ?? current.forceStatuses,
            factions: current.factions.map((faction) =>
              factionWithPlayRules(
                faction,
                play.factions.find((item) => item.id === faction.id),
              ),
            ),
          }
        : current,
    );
    this.applyLogSnapshot(play, true);
    if (play.currentPhaseKind && play.currentPhaseKind !== previous?.currentPhaseKind) {
      if (play.currentPhaseKind === 'Battle') {
        this.setSection('battles', true);
      }

      if (play.currentPhaseKind === 'Action') {
        this.setSection('orders', true);
      }

      this.setSection('log', true);
    }
    if (play.revision !== previousRevision) {
      void this.reloadGraph(play.id);
    }

    this.applyPlayMapOverlay(play);
    this.hydrateBattleForms(play, preserveLocalWork);
  }

  private hydrateBattleForms(play: CampaignPlayDetail, preserveLocalWork: boolean): void {
    const dirtyResults = new Set(preserveLocalWork ? this.dirtyBattleResultIds() : []);
    const dirtyArmy = new Set(preserveLocalWork ? this.dirtyArmyListKeys() : []);
    const applied = { ...this.appliedResultKeys() };
    const nextReports: Record<string, BattleParticipantReport[]> = preserveLocalWork ? { ...this.battleReports() } : {};
    const nextWinners = preserveLocalWork ? { ...this.battleWinner() } : {};
    const nextScores = preserveLocalWork ? { ...this.battleScores() } : {};

    for (const battle of play.battles) {
      const latest = this.latestResultSubmission(battle);
      const resultKey = latest ? `${latest.submitterUserId}:${latest.submittedUtc ?? ''}` : '';
      const applyResults = !preserveLocalWork || applied[battle.id] !== resultKey;
      const current = [...(nextReports[battle.id] ?? [])];

      for (const forceId of this.reportingForceIds(battle)) {
        let report = current.find((item) => item.forceId === forceId) ?? this.emptyBattleReport(forceId);
        if (applyResults && latest) {
          const submitted = latest.reports?.find((item) => item.forceId === forceId);
          if (submitted) {
            report = {
              ...report,
              victoryPoints: submitted.victoryPoints,
              differentialBattlePoints: submitted.differentialBattlePoints,
              bonusBattlePoints: submitted.bonusBattlePoints,
              usedExtraBlackPowder: submitted.usedExtraBlackPowder ?? false,
              magicalSupplyRerolls: submitted.magicalSupplyRerolls ?? 0,
              answers: submitted.answers,
            };
          }
        }

        const armyKey = this.armyListKey(battle.id, forceId);
        if (!preserveLocalWork || !dirtyArmy.has(armyKey)) {
          const list = battle.armyLists?.find((item) => item.forceId === forceId);
          if (list) {
            report = {
              ...report,
              armyPoints: list.armyPoints,
              supplyCostingUnitCount: list.supplyCostingUnitCount,
              armyListText: list.armyListText ?? '',
              armyListGameSystem: list.armyListGameSystem ?? 'WarhammerTheOldWorld',
              armyListBuilder: list.armyListBuilder ?? 'Other',
              supplyCategories: list.supplyCategories ?? [],
            };
          }
        }

        const index = current.findIndex((item) => item.forceId === forceId);
        if (index >= 0) {
          current[index] = report;
        } else {
          current.push(report);
        }
      }

      nextReports[battle.id] = current;
      if (applyResults && latest) {
        dirtyResults.delete(battle.id);
        nextWinners[battle.id] = latest.isDraw ? 'draw' : (latest.winnerForceId ?? '');
        nextScores[battle.id] = {
          winnerScore: latest.winnerScore ?? null,
          loserScore: latest.loserScore ?? null,
        };
      }

      applied[battle.id] = resultKey;
    }

    if (!preserveLocalWork) {
      dirtyResults.clear();
      dirtyArmy.clear();
    }

    this.battleReports.set(nextReports);
    this.battleWinner.set(nextWinners);
    this.battleScores.set(nextScores);
    this.dirtyBattleResultIds.set(dirtyResults);
    this.dirtyArmyListKeys.set(dirtyArmy);
    this.appliedResultKeys.set(applied);

    const nextRetreats = preserveLocalWork ? { ...this.retreatTarget() } : {};
    for (const battle of play.battles) {
      if (preserveLocalWork && nextRetreats[battle.id]) {
        continue;
      }

      if (battle.retreatDraftTargetId) {
        nextRetreats[battle.id] = battle.retreatDraftTargetId;
      }
    }

    this.retreatTarget.set(nextRetreats);
  }

  private applyPlayMapOverlay(play: CampaignPlayDetail): void {
    const overlays = play.mapTerritories;
    if (!overlays || overlays.length === 0) {
      return;
    }

    const byId = new Map(overlays.map((territory) => [territory.id, territory]));
    this.graph.update((graph) => ({
      ...graph,
      territories: graph.territories.map((territory) => {
        const overlay = byId.get(territory.id);
        if (!overlay) {
          return territory;
        }

        const structureTypeId = overlay.structureTypeId ?? territory.structureTypeId;
        const occupyingSubfaction =
          play.forces.find(
            (force) =>
              force.territoryId === overlay.id && force.factionId === overlay.ownerFactionId && force.subfaction,
          )?.subfaction ?? null;
        return {
          ...territory,
          ownerFactionId: overlay.ownerFactionId,
          ownerSubfaction: overlay.ownerFactionId
            ? (overlay.ownerSubfaction ??
              occupyingSubfaction ??
              (overlay.ownerFactionId === territory.ownerFactionId ? territory.ownerSubfaction : null))
            : null,
          structureTypeId,
          structureCondition: overlay.structureCondition
            ? normalizeStructureCondition(structureTypeId, overlay.structureCondition)
            : territory.structureCondition,
        };
      }),
    }));
  }

  private draftsFromPlay(play: CampaignPlayDetail): Record<string, OrderDraft> {
    const drafts: Record<string, OrderDraft> = {};
    for (const force of play.forces.filter((item) => item.isMine)) {
      drafts[force.id] = this.orderDraftFromSaved(play.myDrafts.find((draft) => draft.forceId === force.id));
    }

    return drafts;
  }

  private debugDraftsFromPlay(play: CampaignPlayDetail): Record<string, OrderDraft> {
    const drafts: Record<string, OrderDraft> = {};
    for (const force of play.forces) {
      drafts[force.id] = this.orderDraftFromSaved(play.debugDrafts.find((draft) => draft.forceId === force.id));
    }

    return drafts;
  }

  private orderDraftFromSaved(saved: PlayDraft | undefined): OrderDraft {
    return emptyOrderDraft({
      kind: saved?.kind ?? 'Hold',
      targetTerritoryId: saved?.targetTerritoryId ?? '',
      structureTypeId: saved?.structureTypeId ?? '',
      viaTerritoryId: saved?.viaTerritoryId ?? '',
      viaPath: [...(saved?.viaPath ?? [])],
      destroyImmediately: saved?.destroyImmediately === true,
      droppedItemObjectiveIds: [...(saved?.droppedItemObjectiveIds ?? [])],
    });
  }

  private applyDestinationRoute(forceId: string, targetTerritoryId: string, debug: boolean): void {
    const current = debug ? this.debugDraftFor(forceId) : this.draftFor(forceId);
    const force = this.play()?.forces.find((item) => item.id === forceId);
    const route = this.routeForDestination(force, targetTerritoryId);
    if (debug) {
      this.markDebugDraftDirty(forceId);
      this.debugDrafts.update((drafts) => ({
        ...drafts,
        [forceId]: { ...current, targetTerritoryId, viaTerritoryId: route.viaTerritoryId, viaPath: route.viaPath },
      }));
      return;
    }

    this.markDraftDirty(forceId);
    this.drafts.update((drafts) => ({
      ...drafts,
      [forceId]: { ...current, targetTerritoryId, viaTerritoryId: route.viaTerritoryId, viaPath: route.viaPath },
    }));
  }

  private applyViaSelection(forceId: string, viaTerritoryId: string, debug: boolean): void {
    const current = debug ? this.debugDraftFor(forceId) : this.draftFor(forceId);
    const force = this.play()?.forces.find((item) => item.id === forceId);
    const hop = (force?.moveHops ?? []).find(
      (item) => item.targetTerritoryId === current.targetTerritoryId && item.viaTerritoryId === viaTerritoryId,
    );
    const next = {
      ...current,
      viaTerritoryId,
      viaPath: hop?.intermediateTerritoryIds ? [...hop.intermediateTerritoryIds] : [],
    };
    if (debug) {
      this.markDebugDraftDirty(forceId);
      this.debugDrafts.update((drafts) => ({ ...drafts, [forceId]: next }));
      return;
    }

    this.markDraftDirty(forceId);
    this.drafts.update((drafts) => ({ ...drafts, [forceId]: next }));
  }

  private isDirectMove(force: PlayForce, destId: string): boolean {
    if (findConnection(this.graph().adjacencies, force.territoryId, destId)) {
      return true;
    }

    return this.hopsTo(force, destId).length === 0 && force.moveTargets.includes(destId);
  }

  private hopsTo(force: PlayForce, destId: string): PlayMoveHop[] {
    return (force.moveHops ?? []).filter((hop) => hop.targetTerritoryId === destId);
  }

  protected needsViaChoice(force: PlayForce, destId: string): boolean {
    return this.viasForDestination(force, destId).length > 0;
  }

  private requiresViaChoice(force: PlayForce, destId: string): boolean {
    return this.needsViaChoice(force, destId);
  }

  private routeForDestination(
    force: PlayForce | undefined,
    destId: string,
  ): { viaTerritoryId: string; viaPath: string[] } {
    if (!force || !destId || this.isDirectMove(force, destId)) {
      return { viaTerritoryId: '', viaPath: [] };
    }

    const hops = this.hopsTo(force, destId);
    if (hops.length === 1) {
      return {
        viaTerritoryId: hops[0]?.viaTerritoryId ?? '',
        viaPath: [...(hops[0]?.intermediateTerritoryIds ?? [])],
      };
    }

    const vias = [...new Set(hops.map((hop) => hop.viaTerritoryId))];
    if (vias.length === 1) {
      const matching = hops.filter((hop) => hop.viaTerritoryId === vias[0]);
      if (matching.length === 1) {
        return {
          viaTerritoryId: matching[0]?.viaTerritoryId ?? '',
          viaPath: [...(matching[0]?.intermediateTerritoryIds ?? [])],
        };
      }

      return { viaTerritoryId: vias[0] ?? '', viaPath: [] };
    }

    return { viaTerritoryId: '', viaPath: [] };
  }

  private applyMapDestination(flow: MapActionFlow, force: PlayForce, destId: string): void {
    if (flow.kind === 'Surrender' || flow.kind === 'Retreat') {
      const battle = this.battleForForce(force);
      if (battle) {
        this.onRetreatTarget(battle.id, destId);
      }

      if (flow.kind === 'Surrender' && this.isActionPhase() && !this.play()?.isCommitted) {
        this.markDraftDirty(force.id);
        this.drafts.update((drafts) => ({
          ...drafts,
          [force.id]: emptyOrderDraft({
            kind: 'Surrender',
            targetTerritoryId: destId,
          }),
        }));
        void this.saveDraft(force);
      }

      this.cancelMapAction();
      this.selectedIds.set([destId]);
      return;
    }

    if (this.isChosenTeleportKind(flow.kind) || this.isDirectMove(force, destId)) {
      this.confirmMapMove(flow, destId, '', []);
      return;
    }

    const hops = this.hopsTo(force, destId);
    if (hops.length === 0) {
      this.confirmMapMove(flow, destId, '', []);
      return;
    }

    this.resolveMapHops(flow, destId, hops, []);
  }

  private applyMapVia(flow: MapActionFlow, viaId: string): void {
    const force = this.myForces().find((item) => item.id === flow.forceId);
    if (!force) {
      this.cancelMapAction();
      return;
    }

    const chosen = flow.viaTerritoryId ? [...flow.viaPath, viaId] : [];
    const viaTerritoryId = flow.viaTerritoryId || viaId;
    const viaPath = flow.viaTerritoryId ? chosen : [];
    const remaining = this.hopsTo(force, flow.targetTerritoryId).filter((hop) => {
      if (hop.viaTerritoryId !== viaTerritoryId) {
        return false;
      }

      const intermediates = hop.intermediateTerritoryIds ?? [];
      return viaPath.every((id, index) => intermediates[index] === id);
    });
    this.resolveMapHops({ ...flow, viaTerritoryId, viaPath }, flow.targetTerritoryId, remaining, viaPath);
  }

  private nextHopCandidates(hops: PlayMoveHop[], viaTerritoryId: string, viaPath: readonly string[]): string[] {
    return [
      ...new Set(
        hops.map((hop) => {
          if (!viaTerritoryId) {
            return hop.viaTerritoryId;
          }

          return (hop.intermediateTerritoryIds ?? [])[viaPath.length] ?? '';
        }),
      ),
    ].filter((id) => id.length > 0);
  }

  private resolveMapHops(flow: MapActionFlow, destId: string, hops: PlayMoveHop[], viaPath: string[]): void {
    if (hops.length === 0) {
      this.confirmMapMove(flow, destId, flow.viaTerritoryId, viaPath);
      return;
    }

    const nextCandidates = this.nextHopCandidates(hops, flow.viaTerritoryId, viaPath);
    if (nextCandidates.length === 0) {
      const hop = hops[0];
      this.confirmMapMove(
        flow,
        destId,
        hop.viaTerritoryId,
        hop.intermediateTerritoryIds ? [...hop.intermediateTerritoryIds] : viaPath,
      );
      return;
    }

    this.mapAction.set({
      ...flow,
      step: 'pick-via',
      targetTerritoryId: destId,
      viaCandidates: nextCandidates,
    });
    this.selectedIds.set(
      [flow.originId, destId, flow.viaTerritoryId, ...viaPath, ...nextCandidates].filter((id) => id.length > 0),
    );
  }

  private confirmMapMove(flow: MapActionFlow, destId: string, viaTerritoryId: string, viaPath: string[]): void {
    this.mapAction.set({
      ...flow,
      step: 'confirm',
      targetTerritoryId: destId,
      viaTerritoryId,
      viaPath,
      viaCandidates: [],
    });
    this.selectedIds.set([flow.originId, destId, viaTerritoryId, ...viaPath].filter((id) => id.length > 0));
  }

  private mergeDirtyDrafts(
    local: Record<string, OrderDraft>,
    fromServer: Record<string, OrderDraft>,
    dirtyForceIds: ReadonlySet<string>,
    forces: readonly PlayForce[],
  ): Record<string, OrderDraft> {
    const known = new Set(forces.map((force) => force.id));
    const merged = { ...fromServer };
    for (const forceId of dirtyForceIds) {
      if (!known.has(forceId) || !Object.hasOwn(local, forceId)) {
        continue;
      }

      merged[forceId] = local[forceId];
    }

    return merged;
  }

  private retainedDirtyForceIds(dirtyForceIds: ReadonlySet<string>, forces: readonly PlayForce[]): ReadonlySet<string> {
    const known = new Set(forces.map((force) => force.id));
    return new Set([...dirtyForceIds].filter((forceId) => known.has(forceId)));
  }

  private markDraftDirty(forceId: string): void {
    this.dirtyDraftForceIds.update((current) => {
      const next = new Set(current);
      next.add(forceId);
      return next;
    });
  }

  private markDebugDraftDirty(forceId: string): void {
    this.dirtyDebugDraftForceIds.update((current) => {
      const next = new Set(current);
      next.add(forceId);
      return next;
    });
  }

  private applyLogSnapshot(snapshot: CampaignLogSync, force = false): void {
    this.logBoard.update((current) =>
      !current || force || snapshot.revision >= current.revision ? snapshot : current,
    );
    this.applyLog(snapshot, force);
  }

  private applyLog(snapshot: CampaignLogSync, force = false): void {
    this.campaign.update((current) =>
      current && (force || snapshot.revision > current.revision) ? mergeCampaignLog(current, snapshot) : current,
    );
    this.play.update((current) =>
      current && (force || snapshot.revision > current.revision) ? mergeCampaignLog(current, snapshot) : current,
    );
  }

  /**
   * Subscribes to pushed campaign updates instead of polling.
   *
   * Events carry only a revision, so a refetch happens exactly when the server has state this
   * page has not applied. A newer revision could be a chat message, an order resolution, or a
   * setup edit, and the two refreshes below cover all of them.
   */
  private startUpdateStream(): void {
    const campaignId = this.campaignId;
    if (this.updateStream() || !campaignId) {
      return;
    }

    const refresh = (): void => {
      void this.refreshChat();
      void this.refreshBoard();
    };

    this.updateStream.set(
      this.updates.watchCampaign(campaignId, {
        onUpdate: (revision) => {
          // Our own mutations already applied their response, so skip the echo.
          if (revision > this.appliedRevision()) {
            refresh();
          }
        },
        onFallbackPoll: refresh,
      }),
    );
    this.destroyRef.onDestroy(() => {
      this.updateStream()?.close();
      this.updateStream.set(null);
    });
  }

  /** The newest revision this page has already rendered. */
  private appliedRevision(): number {
    return Math.max(this.campaign()?.revision ?? 0, this.play()?.revision ?? 0);
  }

  protected async pullLog(): Promise<void> {
    await this.refreshChat();
  }

  private shouldLoadPlay(campaign: CampaignDetail): boolean {
    return campaign.status !== 'Scheduled' || Date.parse(campaign.startsUtc) <= Date.now();
  }

  private async refreshChat(): Promise<void> {
    const campaignId = this.campaignId;
    if (!campaignId || this.chatBusy() || globalThis.document.visibilityState === 'hidden') {
      return;
    }

    try {
      this.applyLogSnapshot(await this.campaignsApi.getLog(campaignId));
    } catch {
      // Keep the visible chat; the next poll retries.
    }
  }

  private async refreshBoard(): Promise<void> {
    const campaign = this.campaign();
    if (!campaign || this.chatBusy() || this.overlay.busy() || globalThis.document.visibilityState === 'hidden') {
      return;
    }

    try {
      if (this.shouldLoadPlay(campaign)) {
        const play = await this.campaignsApi.getPlay(campaign.id);
        if (play) {
          this.applyPlay(play, { preserveLocalWork: true });
        }
        return;
      }

      const next = await this.campaignsApi.get(campaign.id);
      this.campaign.set(next);
      if (this.shouldLoadPlay(next)) {
        const play = await this.campaignsApi.getPlay(next.id);
        if (play) {
          this.applyPlay(play, { preserveLocalWork: true });
        }
      }
    } catch {
      // Keep the visible board; the next poll retries.
    }
  }

  private async refreshBoardAfterConflict(): Promise<void> {
    const campaign = this.campaign();
    if (!campaign) {
      this.error.set('The campaign was changed by another request. Reload and try again.');
      return;
    }

    try {
      const play = await this.campaignsApi.getPlay(campaign.id);
      if (!play) {
        this.error.set('The campaign was changed by another request. Reload and try again.');
        return;
      }

      this.applyPlay(play);
      this.error.set(null);
    } catch {
      this.error.set('The campaign was changed by another request. Reload and try again.');
    }
  }

  private async reloadGraph(id: string): Promise<void> {
    try {
      const graph = await this.campaignsApi.getMapGraph(id);
      this.applyGraph(graph);
      const play = this.play();
      if (play) {
        this.applyPlayMapOverlay(play);
      }
    } catch {
      // Keep the visible overlay; the next refresh retries.
    }
  }

  private applyGraph(graph: MapGraphDetail): void {
    this.graph.set({
      territories: graph.territories.map((territory) => ({
        id: territory.id,
        displayNumber: territory.displayNumber,
        name: territory.name,
        description: territory.description,
        polygon: territory.polygon.map((point) => ({ x: point.x, y: point.y })),
        terrainTypeId: territory.terrainTypeId,
        structureTypeId: territory.structureTypeId,
        structureCondition: normalizeStructureCondition(territory.structureTypeId, territory.structureCondition),
        overlayColor: territory.overlayColor,
        ownerFactionId: territory.ownerFactionId,
        ownerSubfaction: territory.ownerSubfaction ?? null,
        spawnFactionId: territory.spawnFactionId,
        spawnSubfaction: territory.spawnSubfaction ?? null,
      })),
      adjacencies: graph.adjacencies.map((edge) => ({
        id: edge.id,
        territoryAId: edge.territoryAId,
        territoryBId: edge.territoryBId,
        origin: edge.origin === 'Generated' ? 'Generated' : 'Manual',
        marker: { x: edge.markerX, y: edge.markerY },
      })),
    });
  }

  private async load(id: string): Promise<void> {
    this.restoreViewPrefs(id);
    try {
      const playRequest = this.campaignsApi.getPlay(id).then(
        (play) => play,
        () => null,
      );
      const [campaign, graph, play] = await Promise.all([
        this.campaignsApi.get(id),
        this.campaignsApi.getMapGraph(id),
        playRequest,
      ]);
      this.campaign.set(campaign);
      this.factionChoice.set(campaign.factionId ? mapFactionOptionValue(campaign.factionId, campaign.subfaction) : '');
      this.applySectionPrefs(campaign.status);
      this.startUpdateStream();
      this.applyGraph(graph);
      if (play) {
        this.applyPlay(play);
      } else {
        this.play.set(null);
      }
      this.seedAwardDefaults();
    } catch (error: unknown) {
      this.error.set(readApiError(error, 'Unable to load this campaign.'));
    } finally {
      this.loading.set(false);
    }
  }

  private async loadChat(id: string): Promise<void> {
    this.chatLoading.set(true);
    this.chatLoadError.set(null);
    try {
      this.applyLogSnapshot(await this.campaignsApi.getLog(id), true);
      this.startUpdateStream();
    } catch (error: unknown) {
      this.chatLoadError.set(readApiError(error, 'Unable to load campaign chat.'));
    } finally {
      this.chatLoading.set(false);
    }
  }

  private restoreViewPrefs(campaignId: string): void {
    const stored = readStoredPrefs(campaignId);
    const prefs = stored ?? defaultCampaignViewPrefs();
    this.highlightMode.set(prefs.highlightMode);
    this.standingsSort.set(prefs.standingsSort);
    this.chatChannelKey.set(prefs.chatChannelKey);
    this.lastChatScrollTop = prefs.chatScrollTop;
    this.restoreChatScroll.set(stored ? stored.chatScrollTop : null);
    this.storedSectionPrefs = stored?.sections && typeof stored.sections === 'object' ? stored.sections : null;
    this.prefsHydrated = true;
  }

  private applySectionPrefs(status: string): void {
    const next = defaultOpenSections(status);
    if (this.storedSectionPrefs) {
      for (const id of CAMPAIGN_SECTIONS) {
        if (typeof this.storedSectionPrefs[id] === 'boolean') {
          next[id] = this.storedSectionPrefs[id]!;
        }
      }
    }

    this.openSections.set(next);
    this.commitmentsRosterOpen.set(false);
  }

  private persistViewPrefs(): void {
    const campaignId = this.campaign()?.id ?? this.campaignId;
    if (!campaignId || !this.prefsHydrated) {
      return;
    }

    this.viewPrefs.write(campaignId, {
      highlightMode: this.highlightMode(),
      sections: { ...this.openSections() },
      standingsSort: { ...this.standingsSort() },
      chatChannelKey: this.chatChannelKey(),
      chatScrollTop: this.lastChatScrollTop,
    });
  }

  private battleScorePair(battleId: string): { winnerScore: number | null; loserScore: number | null } {
    return this.battleScores()[battleId] ?? { winnerScore: null, loserScore: null };
  }

  private patchBattleScore(battleId: string, field: 'winnerScore' | 'loserScore', value: string | number | null): void {
    const parsed = value === null || value === '' ? null : Number(value);
    this.battleScores.update((current) => {
      const existing = current[battleId] ?? { winnerScore: null, loserScore: null };
      return {
        ...current,
        [battleId]: {
          ...existing,
          [field]: parsed === null || Number.isNaN(parsed) ? null : parsed,
        },
      };
    });
    this.markBattleResultDirty(battleId);
  }

  private seedAwardDefaults(): void {
    if (!this.awardObjectiveId()) {
      this.awardObjectiveId.set(this.awardableObjectives()[0]?.id ?? '');
    }

    if (!this.awardPlayerUserId()) {
      this.awardPlayerUserId.set(this.sortedStandings()[0]?.userId ?? '');
    }

    if (!this.grantHolderId()) {
      this.grantHolderId.set(this.grantHolders()[0]?.id ?? '');
    }
  }

  private toHeldMapItem(item: PlayItemObjective, campaign: CampaignDetail | null): MapHeldItem {
    return {
      name: item.name,
      builtinSymbol: item.builtinSymbol ?? 'Crown',
      color: item.color ?? '#C45C26',
      imageUrl: this.itemObjectiveImageSrc(item, campaign),
    };
  }

  private itemObjectiveImageSrc(
    item: { typeId: string; hasImage?: boolean },
    campaign: CampaignDetail | null,
  ): string | null {
    if (!campaign || !item.hasImage) {
      return null;
    }

    return this.campaignsApi.itemObjectiveImageUrl(campaign.id, item.typeId, campaign.assetTags);
  }
}

function namedSpawnSubfaction(territory: MapTerritory): string | null {
  const name = territory.spawnSubfaction?.trim();
  return name && name.length > 0 ? name : null;
}

function factionSpawnPlaces(faction: CampaignFaction, territories: readonly MapTerritory[]): FactionSpawnPlace[] {
  const assigned = territories.filter((territory) => territory.spawnFactionId === faction.id);
  const parent = assigned.find((territory) => namedSpawnSubfaction(territory) === null);
  const parentLabel = parent ? territoryLabel(parent) : null;
  const places: { territoryId: string; label: string; subfaction: string | null }[] = [];
  if (parent) {
    places.push({ territoryId: parent.id, label: territoryLabel(parent), subfaction: null });
  }

  for (const territory of assigned) {
    const subfaction = namedSpawnSubfaction(territory);
    if (!subfaction) {
      continue;
    }

    const label = territoryLabel(territory);
    if (parent && (territory.id === parent.id || (parentLabel !== null && label === parentLabel))) {
      continue;
    }

    places.push({ territoryId: territory.id, label, subfaction });
  }

  places.sort((left, right) => {
    if (left.subfaction === null && right.subfaction !== null) {
      return -1;
    }

    if (left.subfaction !== null && right.subfaction === null) {
      return 1;
    }

    return compareNames(left.subfaction ?? left.label, right.subfaction ?? right.label);
  });

  return places.map((place, index) => ({
    territoryId: place.territoryId,
    label: place.label,
    prefix: index === 0 ? ' (' : place.subfaction && places[0]?.subfaction === null ? '; ' : ', ',
    subfactionLabel: place.subfaction ? `${place.subfaction}: ` : '',
    suffix: index === places.length - 1 ? ')' : '',
  }));
}

function joinDisplayNames(names: string[]): string {
  if (names.length <= 1) {
    return names[0] ?? '';
  }

  if (names.length === 2) {
    return `${names[0]} and ${names[1]}`;
  }

  return `${names.slice(0, -1).join(', ')}, and ${names[names.length - 1]}`;
}

function factionBelongsToAllyGroup(faction: CampaignFaction, group: { id: string; name: string }): boolean {
  return faction.allyGroupId === group.id || faction.allyGroupName === group.name;
}

function isOwnPrivateAssignment(
  assignment: PrivateObjectiveAssignment,
  campaign: CampaignDetail,
  userId: string,
): boolean {
  if (assignment.holderKind === 'Player' || assignment.holderKind === 'Traitor') {
    return assignment.holderId === userId;
  }

  if (assignment.holderKind === 'Faction') {
    if (assignment.holderId !== campaign.factionId) {
      return false;
    }

    const scoped = assignment.holderSubfaction?.trim();
    if (!scoped) {
      return true;
    }

    const viewerSubfaction = campaign.subfaction?.trim();
    return !!viewerSubfaction && scoped.localeCompare(viewerSubfaction, undefined, { sensitivity: 'accent' }) === 0;
  }

  if (assignment.holderKind !== 'AllyGroup') {
    return false;
  }

  const faction = campaign.factions.find((item) => item.id === campaign.factionId);
  if (!faction) {
    return false;
  }

  const group = campaign.allyGroups.find((item) => item.id === assignment.holderId);
  return group ? factionBelongsToAllyGroup(faction, group) : faction.allyGroupId === assignment.holderId;
}

function privateObjectiveFactionName(assignment: PrivateObjectiveAssignment, campaign: CampaignDetail): string {
  if (assignment.holderKind === 'Faction') {
    return campaign.factions.find((faction) => faction.id === assignment.holderId)?.name ?? '';
  }

  if (assignment.holderKind === 'AllyGroup') {
    return campaign.allyGroups.find((group) => group.id === assignment.holderId)?.name ?? '';
  }

  const participant = campaign.participants?.find((item) => item.userId === assignment.holderId);
  return participant?.factionName ?? '';
}

function privateObjectiveHolderLabel(assignment: PrivateObjectiveAssignment, campaign: CampaignDetail): string {
  if (assignment.holderKind === 'Player' || assignment.holderKind === 'Traitor') {
    const participant = campaign.participants?.find((item) => item.userId === assignment.holderId);
    const name = participant?.displayName ?? participant?.username ?? 'Player';
    const faction = participant?.factionName?.trim();
    const labeled = faction ? `${name} · ${faction}` : name;
    return assignment.holderKind === 'Traitor' ? `${labeled} · Traitor` : labeled;
  }

  if (assignment.holderKind === 'Faction') {
    return campaign.factions.find((faction) => faction.id === assignment.holderId)?.name ?? 'Faction';
  }

  return campaign.allyGroups.find((group) => group.id === assignment.holderId)?.name ?? 'Ally group';
}

function allyGroupMemberFactions(
  factions: readonly CampaignFaction[],
): { faction: CampaignFaction; subfactions: string[] }[] {
  return [...factions]
    .sort((left, right) => compareNames(left.name, right.name))
    .map((faction) => ({
      faction,
      subfactions: [...faction.subfactions]
        .map((name) => name.trim())
        .filter((name) => name.length > 0)
        .sort(compareNames),
    }));
}

function allyGroupPlayers(
  campaign: CampaignDetail,
  groupFactions: readonly CampaignFaction[],
  brokenAllyFactionIds: ReadonlySet<string>,
): AllyGroupPlayer[] {
  const factionIds = new Set(groupFactions.map((faction) => faction.id));
  const factionNames = new Set(groupFactions.map((faction) => faction.name));

  return (campaign.participants ?? [])
    .filter((participant) => participant.isPlayer)
    .map((participant) => {
      const faction = resolveParticipantFaction(participant, campaign.factions);
      if (!faction || brokenAllyFactionIds.has(faction.id)) {
        return null;
      }

      if (!factionIds.has(faction.id) && !factionNames.has(faction.name)) {
        return null;
      }

      return {
        userId: participant.userId,
        displayName: participant.displayName,
        factionId: faction.id,
        factionName: firstNonEmpty(participant.factionName, faction.name) ?? faction.name,
        subfaction: firstNonEmpty(participant.subfaction),
        traitorVictims: participant.traitorVictims ?? [],
      };
    })
    .filter((player): player is AllyGroupPlayer => player !== null)
    .sort((left, right) => compareNames(left.displayName, right.displayName));
}

function resolveParticipantFaction(
  participant: CampaignParticipant,
  factions: readonly CampaignFaction[],
): CampaignFaction | null {
  if (participant.factionId) {
    const byId = factions.find((faction) => faction.id === participant.factionId);
    if (byId) {
      return byId;
    }
  }

  const name = participant.factionName?.trim();
  if (!name) {
    return null;
  }

  return factions.find((faction) => faction.name === name) ?? null;
}

function factionRoster(
  campaign: CampaignDetail,
  faction: CampaignFaction,
  rulesFor: (ids: readonly string[] | undefined) => CampaignSpecialRule[],
  catalog: readonly CampaignSpecialRule[],
): FactionRoster {
  const members = (campaign.participants ?? []).filter((participant) => {
    if (!participant.isPlayer) {
      return false;
    }

    return resolveParticipantFaction(participant, campaign.factions)?.id === faction.id;
  });
  const subfactionNames = [...faction.subfactions]
    .map((name) => name.trim())
    .filter((name) => name.length > 0)
    .sort(compareNames);
  return {
    faction,
    specialRules: resolvedSpecialRules(
      faction.specialRuleIds,
      specialRuleNamesForFaction(faction.name),
      catalog,
      rulesFor,
    ),
    players: rosterPlayers(members.filter((participant) => !participant.subfaction?.trim())),
    subfactions: subfactionNames.map((name) => {
      const assigned = faction.subfactionSpecialRules?.find(
        (item) => compareNames(item.name, name) === 0,
      )?.specialRuleIds;
      return {
        name,
        specialRules: resolvedSpecialRules(
          assigned,
          specialRuleNamesForSubfaction(faction.name, name),
          catalog,
          rulesFor,
        ),
        players: rosterPlayers(
          members.filter((participant) => compareNames(participant.subfaction?.trim() ?? '', name) === 0),
        ),
      };
    }),
  };
}

function rosterPlayers(participants: readonly CampaignParticipant[]): FactionRosterPlayer[] {
  return [...participants]
    .map((participant) => ({ userId: participant.userId, displayName: participant.displayName }))
    .sort((left, right) => compareNames(left.displayName, right.displayName));
}

function catalogRulesNamed(catalog: readonly CampaignSpecialRule[], names: readonly string[]): CampaignSpecialRule[] {
  const byName = new Map(catalog.map((rule) => [rule.name.trim().toLocaleLowerCase(), rule] as const));
  return names.flatMap((name) => {
    const rule = byName.get(name.trim().toLocaleLowerCase());
    return rule ? [rule] : [];
  });
}

function resolvedSpecialRules(
  ids: readonly string[] | undefined,
  names: readonly string[],
  catalog: readonly CampaignSpecialRule[],
  rulesFor: (ids: readonly string[] | undefined) => CampaignSpecialRule[],
): CampaignSpecialRule[] {
  const fromIds = rulesFor(ids);
  return fromIds.length > 0 ? fromIds : catalogRulesNamed(catalog, names);
}

function displaySpecialRulesFor(
  faction: CampaignFaction,
  catalog: readonly CampaignSpecialRule[],
  rulesFor: (ids: readonly string[] | undefined) => CampaignSpecialRule[],
  subfaction?: string | null,
): CampaignSpecialRule[] {
  const factionRules = resolvedSpecialRules(
    faction.specialRuleIds,
    specialRuleNamesForFaction(faction.name),
    catalog,
    rulesFor,
  );
  if (!subfaction) {
    return factionRules;
  }

  const assigned = faction.subfactionSpecialRules?.find(
    (item) => compareNames(item.name, subfaction) === 0,
  )?.specialRuleIds;
  const subRules = resolvedSpecialRules(
    assigned,
    specialRuleNamesForSubfaction(faction.name, subfaction),
    catalog,
    rulesFor,
  );
  const seen = new Set(factionRules.map((rule) => rule.id.toLowerCase()));
  return [...factionRules, ...subRules.filter((rule) => !seen.has(rule.id.toLowerCase()))];
}

function mergeSpecialRuleCatalogs(
  primary: readonly CampaignSpecialRule[] | undefined,
  fallback: readonly CampaignSpecialRule[] | undefined,
): CampaignSpecialRule[] {
  const byId = new Map<string, CampaignSpecialRule>();
  for (const rule of fallback ?? []) {
    byId.set(rule.id.toLowerCase(), rule);
  }

  for (const rule of primary ?? []) {
    byId.set(rule.id.toLowerCase(), rule);
  }

  return [...byId.values()];
}

function factionWithPlayRules(
  campaignFaction: CampaignFaction,
  playFaction: CampaignFaction | undefined,
): CampaignFaction {
  if (!playFaction) {
    return campaignFaction;
  }

  return {
    ...campaignFaction,
    specialRuleIds: playFaction.specialRuleIds?.length ? playFaction.specialRuleIds : campaignFaction.specialRuleIds,
    subfactionSpecialRules: playFaction.subfactionSpecialRules?.length
      ? playFaction.subfactionSpecialRules
      : campaignFaction.subfactionSpecialRules,
  };
}

function rivalForce(play: CampaignPlayDetail | null | undefined, userId: string | undefined): PlayForce | undefined {
  if (!userId) {
    return undefined;
  }

  const forces = (play?.forces ?? []).filter((force) => force.controllerUserId === userId);
  return forces.find((item) => firstNonEmpty(item.subfaction) !== null) ?? forces.at(0);
}

function namedFaction(name: string | null | undefined): string | null {
  const trimmed = firstNonEmpty(name);
  if (trimmed === 'Neutral' || trimmed === 'Unknown faction') {
    return null;
  }

  return trimmed;
}

function firstNonEmpty(...values: (string | null | undefined)[]): string | null {
  for (const value of values) {
    const trimmed = value?.trim();
    if (trimmed) {
      return trimmed;
    }
  }

  return null;
}
