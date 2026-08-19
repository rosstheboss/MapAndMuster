import { Component, computed, DestroyRef, inject, signal, viewChild, type ElementRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { AuthService, readApiError } from '../../core/auth/auth.service';
import { CampaignService } from '../../core/campaigns/campaign.service';
import type {
  CampaignChatSend,
  CampaignDetail,
  CampaignFaction,
  CampaignMission,
  CampaignParticipant,
  CampaignPlayDetail,
  CampaignSpecialRule,
  BattleParticipantReport,
  ArmyListSupplyCategory,
  MapGraphDetail,
  PlayBattle,
  PlayBattleForceSupply,
  PlayForce,
  PlayItemObjective,
  PublicObjectiveLeader,
  UserSearchHit,
} from '../../core/campaigns/campaign.models';
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
import { missionsForTerritory, structureTypeById, terrainTypeById } from '../../core/campaigns/campaign.models';
import { CAMPAIGN_LOG_POLL_MS, mergeCampaignLog, type CampaignLogSync } from '../../core/campaigns/campaign-log';
import {
  actionNumberAt,
  DURATION_UNITS,
  formatDuration,
  formatPhaseEndTimestamp,
  formatPhaseLabel,
  statusLabel,
} from '../../core/campaigns/campaign-schedule';
import { FORM_SAVE_SUCCESS_MESSAGE } from '../../core/forms/form-messages';
import { FormSubmitOverlayService } from '../../core/forms/form-submit-overlay.service';
import { formatLocation } from '../../core/location/location';
import { adjacentTerritoryIds } from '../../core/maps/adjacency';
import { downloadBlob, mapDownloadFilename, rasterizeMapPng } from '../../core/maps/map-export';
import { serializeMapSvg, svgDownloadFilename } from '../../core/maps/map-svg';
import type { MapGraph, MapTerritory } from '../../core/maps/map-graph.models';
import { normalizeStructureCondition, territoryLabel } from '../../core/maps/map-graph.models';
import { InstantDatePipe } from '../../shared/time/instant-date.pipe';
import { CampaignLogComponent } from '../../shared/campaign-log/campaign-log.component';
import {
  CampaignMapViewComponent,
  type MapForceMarker,
  type MapHeldItem,
  type MapItemMarker,
} from '../../shared/campaign-map-view/campaign-map-view.component';
import { MapSymbolComponent } from '../../shared/map-symbol/map-symbol.component';
import { PhaseCountdownComponent } from '../../shared/phase-countdown/phase-countdown.component';

const CAMPAIGN_SECTIONS = [
  'log',
  'participants',
  'faction',
  'missingFaction',
  'map',
  'itemObjectives',
  'privateObjectives',
  'orders',
  'battles',
  'debug',
  'schedule',
  'details',
  'round',
  'factions',
  'allies',
  'links',
  'standings',
  'delete',
] as const;

type CampaignSection = (typeof CAMPAIGN_SECTIONS)[number];

interface OrderDraft {
  kind: string;
  targetTerritoryId: string;
  structureTypeId: string;
}

interface MapActionFlow {
  step: 'menu' | 'pick-target' | 'pick-structure' | 'confirm';
  forceId: string;
  originId: string;
  kind: string;
  targetTerritoryId: string;
  structureTypeId: string;
  menuX: number;
  menuY: number;
}

function openSections(): Record<CampaignSection, boolean> {
  return Object.fromEntries(CAMPAIGN_SECTIONS.map((id) => [id, true])) as Record<CampaignSection, boolean>;
}

@Component({
  selector: 'app-campaign-detail-page',
  imports: [
    FormsModule,
    RouterLink,
    InstantDatePipe,
    CampaignLogComponent,
    CampaignMapViewComponent,
    MapSymbolComponent,
    PhaseCountdownComponent,
  ],
  templateUrl: './campaign-detail.page.html',
  styleUrl: './campaign-detail.page.css',
})
export class CampaignDetailPage {
  private readonly campaignsApi = inject(CampaignService);
  private readonly overlay = inject(FormSubmitOverlayService);
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly viewPrefs = inject(CampaignViewPrefsService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly mapBoard = viewChild<ElementRef<HTMLElement>>('mapBoard');

  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly successMessage = signal<string | null>(null);
  protected readonly campaign = signal<CampaignDetail | null>(null);
  protected readonly play = signal<CampaignPlayDetail | null>(null);
  protected readonly graph = signal<MapGraph>({ territories: [], adjacencies: [] });
  protected readonly hoveredTerritoryId = signal<string | null>(null);
  protected readonly selectedIds = signal<string[]>([]);
  protected readonly confirmingDelete = signal(false);
  protected readonly deleting = signal(false);
  protected readonly downloading = signal(false);
  protected readonly chatBusy = signal(false);
  protected readonly chatError = signal<string | null>(null);
  protected readonly openSections = signal(openSections());
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
  private lastChatScrollTop = 0;
  protected readonly durationUnits = DURATION_UNITS;
  protected readonly factionChoice = signal('');
  protected readonly subfactionChoice = signal('');
  protected readonly memberQuery = signal('');
  protected readonly memberHits = signal<UserSearchHit[]>([]);
  protected readonly staffFactionId = signal<Partial<Record<string, string>>>({});
  protected readonly staffSubfaction = signal<Partial<Record<string, string>>>({});
  protected readonly kickUserId = signal<string | null>(null);
  protected readonly roundCount = signal(3);
  protected readonly extensionAmount = signal(1);
  protected readonly extensionUnit = signal('Hours');
  protected readonly extensionWindowId = signal('');
  protected readonly drafts = signal<Record<string, OrderDraft>>({});
  protected readonly debugDrafts = signal<Record<string, OrderDraft>>({});
  protected readonly mapAction = signal<MapActionFlow | null>(null);
  protected readonly battleWinner = signal<Record<string, string>>({});
  protected readonly battleScores = signal<
    Partial<Record<string, { winnerScore: number | null; loserScore: number | null }>>
  >({});
  private readonly battleReports = signal<Record<string, BattleParticipantReport[]>>({});
  private readonly armyListParseMessages = signal<Record<string, string>>({});
  private readonly armyListParseTimers = new Map<string, ReturnType<typeof setTimeout>>();
  protected readonly retreatTarget = signal<Record<string, string>>({});
  private readonly mapRevision = signal(0);
  private logPollStarted = false;

  constructor() {
    const id = this.route.snapshot.paramMap.get('id');
    this.destroyRef.onDestroy(() => {
      this.persistViewPrefs();
      for (const timer of this.armyListParseTimers.values()) {
        globalThis.clearTimeout(timer);
      }
    });
    if (id) {
      void this.load(id);
    } else {
      this.error.set('The campaign was not found.');
      this.loading.set(false);
    }
  }

  protected readonly mapSrc = computed(() => {
    const campaign = this.campaign();
    if (!campaign?.hasMap) {
      return null;
    }

    return this.campaignsApi.mapUrl(campaign.id, this.mapRevision());
  });

  protected hoveredTerritory = computed(() => {
    const id = this.hoveredTerritoryId() ?? this.selectedIds().at(-1) ?? null;
    return this.graph().territories.find((territory) => territory.id === id) ?? null;
  });
  protected readonly adjacentTerritoryIds = computed(() => {
    const flow = this.mapAction();
    if (flow?.step === 'pick-target') {
      const force = this.myForces().find((item) => item.id === flow.forceId);
      return force?.moveTargets ?? [];
    }

    return adjacentTerritoryIds(this.graph().adjacencies, this.selectedIds());
  });
  protected readonly selectedFaction = computed(
    () => this.campaign()?.factions.find((faction) => faction.id === this.factionChoice()) ?? null,
  );
  protected readonly chosenFaction = computed(() => {
    const campaign = this.campaign();
    if (!campaign?.factionId) {
      return null;
    }

    return campaign.factions.find((faction) => faction.id === campaign.factionId) ?? null;
  });
  protected readonly myForces = computed(() => this.play()?.forces.filter((force) => force.isMine) ?? []);
  protected readonly orderableForces = computed(() =>
    this.myForces().filter((force) => force.availableActions.length > 0),
  );
  protected readonly canCommitActions = computed(() => {
    const play = this.play();
    const forces = this.orderableForces();
    if (!play || play.isCommitted || forces.length === 0) {
      return false;
    }

    return forces.every((force) => play.myDrafts.some((draft) => draft.forceId === force.id));
  });
  protected readonly buildableStructures = computed(() =>
    (this.play()?.structureTypes ?? this.campaign()?.structureTypes ?? []).filter((type) => type.isBuildable),
  );
  protected readonly isActionPhase = computed(() => this.play()?.currentPhaseKind === 'Action');
  protected readonly isBattlePhase = computed(() => this.play()?.currentPhaseKind === 'Battle');
  protected readonly hasOpenBattles = computed(() => (this.play()?.battles.length ?? 0) > 0);
  protected readonly canDebug = computed(() => this.play()?.canDebug === true);
  protected readonly isDebugActive = computed(() => this.play()?.isDebugActive === true);
  protected readonly showSpawnLocation = computed(() => {
    const status = this.campaign()?.status;
    return status === 'Scheduled' || status === 'InProgress';
  });
  protected readonly spawnLocationName = computed(() => {
    const factionId = this.factionChoice() || this.campaign()?.factionId;
    if (!factionId) {
      return null;
    }

    const territory = this.graph().territories.find((item) => item.spawnFactionId === factionId);
    return territory ? territoryLabel(territory) : null;
  });
  protected readonly mapForces = computed((): MapForceMarker[] => {
    const play = this.play();
    const campaign = this.campaign();
    if (!play) {
      return [];
    }

    const items = play.itemObjectives ?? [];
    return play.forces.map((force) => ({
      id: force.id,
      territoryId: force.territoryId,
      factionId: force.factionId,
      isMine: force.isMine,
      inBattle: force.inBattle,
      label: `${this.forceLabel(force)} in ${this.territoryName(force.territoryId)}`,
      heldItems: items
        .filter((item) => item.possessorForceId === force.id)
        .map((item) => this.toHeldMapItem(item, campaign)),
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
  protected readonly leaderboards = computed(
    () => this.play()?.publicObjectiveLeaderboards ?? this.campaign()?.publicObjectiveLeaderboards ?? [],
  );
  protected readonly awardableObjectives = computed(() =>
    (this.campaign()?.publicObjectiveTypes ?? []).filter((objective) => objective.campaignPoints > 0),
  );
  protected readonly unclaimedPrivateCounts = computed(
    () => this.play()?.privateObjectiveUnclaimedCounts ?? this.campaign()?.privateObjectiveUnclaimedCounts ?? [],
  );
  protected readonly visiblePrivateObjectives = computed(
    () => this.play()?.privateObjectives ?? this.campaign()?.privateObjectives ?? [],
  );
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

  protected canStaffMembers(): boolean {
    const campaign = this.campaign();
    const user = this.auth.currentUser();
    return Boolean(campaign && (campaign.canManage || user?.isAdministrator));
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

  protected async addMember(hit: UserSearchHit): Promise<void> {
    const campaign = this.campaign();
    if (!campaign) {
      return;
    }

    this.error.set(null);
    this.successMessage.set(null);
    try {
      await this.overlay.run(() => this.campaignsApi.addMember(campaign.id, hit.userId, campaign.revision));
      this.memberQuery.set('');
      this.memberHits.set([]);
      await this.load(campaign.id);
      this.successMessage.set(FORM_SAVE_SUCCESS_MESSAGE);
    } catch (error: unknown) {
      this.error.set(readApiError(error, 'Unable to add that player.'));
    }
  }

  protected async kickMember(participant: CampaignParticipant): Promise<void> {
    const campaign = this.campaign();
    if (!campaign) {
      return;
    }

    if (this.kickUserId() !== participant.userId) {
      this.kickUserId.set(participant.userId);
      return;
    }

    this.error.set(null);
    this.successMessage.set(null);
    try {
      await this.overlay.run(() => this.campaignsApi.kickMember(campaign.id, participant.userId, campaign.revision));
      this.kickUserId.set(null);
      await this.load(campaign.id);
      this.successMessage.set(FORM_SAVE_SUCCESS_MESSAGE);
    } catch (error: unknown) {
      this.error.set(readApiError(error, 'Unable to remove that player.'));
      this.kickUserId.set(null);
    }
  }

  protected staffFactionValue(participant: CampaignParticipant): string {
    return this.staffFactionId()[participant.userId] ?? participant.factionId ?? '';
  }

  protected staffSubfactionValue(participant: CampaignParticipant): string {
    return this.staffSubfaction()[participant.userId] ?? participant.subfaction ?? '';
  }

  protected onStaffFaction(participant: CampaignParticipant, factionId: string): void {
    this.staffFactionId.update((current) => ({ ...current, [participant.userId]: factionId }));
    this.staffSubfaction.update((current) => ({ ...current, [participant.userId]: '' }));
  }

  protected onStaffSubfaction(participant: CampaignParticipant, subfaction: string): void {
    this.staffSubfaction.update((current) => ({ ...current, [participant.userId]: subfaction }));
  }

  protected staffFaction(participant: CampaignParticipant): CampaignFaction | null {
    const id = this.staffFactionValue(participant);
    return this.campaign()?.factions.find((faction) => faction.id === id) ?? null;
  }

  protected async assignFaction(participant: CampaignParticipant): Promise<void> {
    const campaign = this.campaign();
    const factionId = this.staffFactionValue(participant);
    if (!campaign || !factionId) {
      return;
    }

    this.error.set(null);
    this.successMessage.set(null);
    try {
      await this.overlay.run(() =>
        this.campaignsApi.assignFaction(campaign.id, {
          revision: this.play()?.revision ?? campaign.revision,
          userId: participant.userId,
          factionId,
          subfaction: this.staffSubfactionValue(participant) || null,
        }),
      );
      await this.load(campaign.id);
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
    const campaign = this.campaign();
    if (!campaign) {
      return;
    }

    this.chatError.set(null);
    this.chatBusy.set(true);
    try {
      const next = await this.campaignsApi.postChat(campaign.id, {
        revision: campaign.revision,
        message: payload.message,
        channelKind: payload.channelKind,
        targetId: payload.targetId,
      });
      this.applyLog(next, true);
    } catch (error: unknown) {
      this.chatError.set(readApiError(error, 'Unable to send that chat message.'));
    } finally {
      this.chatBusy.set(false);
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
    const factionId = this.factionChoice();
    if (!campaign || !factionId) {
      return;
    }

    this.error.set(null);
    this.successMessage.set(null);
    try {
      await this.overlay.run(() =>
        this.campaignsApi.chooseFaction(campaign.id, {
          revision: campaign.revision,
          factionId,
          subfaction: this.subfactionChoice() || null,
        }),
      );
      this.campaign.update((current) =>
        current
          ? {
              ...current,
              factionId,
              subfaction: this.subfactionChoice() || null,
            }
          : current,
      );
      await this.load(campaign.id);
      this.successMessage.set(FORM_SAVE_SUCCESS_MESSAGE);
    } catch (error: unknown) {
      this.error.set(readApiError(error, 'Unable to save your faction.'));
    }
  }

  protected onTerritorySelect(event: { id: string; additive: boolean; clientX?: number; clientY?: number }): void {
    if (this.handleMapActionSelect(event)) {
      return;
    }

    if (event.additive) {
      this.selectedIds.update((current) =>
        current.includes(event.id) ? current.filter((id) => id !== event.id) : [...current, event.id],
      );
      return;
    }

    this.selectedIds.set([event.id]);
    this.hoveredTerritoryId.set(event.id);
  }

  protected onMapBackgroundSelect(): void {
    this.cancelMapAction();
    this.selectedIds.set([]);
  }

  protected factionName(id: string | null | undefined): string {
    if (!id) {
      return 'Neutral';
    }

    return this.campaign()?.factions.find((faction) => faction.id === id)?.name ?? 'Unknown faction';
  }

  protected adjacentLabels(territory: MapTerritory): string {
    const names = this.graph()
      .adjacencies.filter((edge) => edge.territoryAId === territory.id || edge.territoryBId === territory.id)
      .map((edge) => {
        const otherId = edge.territoryAId === territory.id ? edge.territoryBId : edge.territoryAId;
        const other = this.graph().territories.find((item) => item.id === otherId);
        return other ? territoryLabel(other) : otherId;
      })
      .sort((left, right) => left.localeCompare(right));
    return names.length > 0 ? names.join(', ') : 'None';
  }

  protected labelFor(territory: MapTerritory): string {
    return territoryLabel(territory);
  }

  protected forceLabelById(forceId: string): string {
    const force = this.play()?.forces.find((item) => item.id === forceId);
    return force ? this.forceLabel(force) : forceId;
  }

  protected forceLabel(force: PlayForce): string {
    const name = force.controllerUsername ?? 'Player';
    const status = force.statusName?.trim();
    const base = `${name} · ${this.factionName(force.factionId)}`;
    return status ? `${base} · ${status}` : base;
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
    return terrainTypeById(this.campaign(), id)?.isWaterFeature === true;
  }

  protected specialRulesFor(ids: readonly string[] | undefined): CampaignSpecialRule[] {
    if (!ids || ids.length === 0) {
      return [];
    }

    const catalog = this.play()?.specialRules ?? this.campaign()?.specialRules ?? [];
    return ids.flatMap((id) => catalog.find((rule) => rule.id === id) ?? []);
  }

  protected itemSpecialRules(item: PlayItemObjective): CampaignSpecialRule[] {
    const type = this.campaign()?.itemObjectiveTypes?.find((entry) => entry.id === item.typeId);
    return this.specialRulesFor(type?.specialRuleIds);
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
        ? this.campaignsApi.structureImageUrl(campaign.id, structureTypeId, campaign.revision, true)
        : null;
    }

    if (!structure.hasImage) {
      return null;
    }

    return this.campaignsApi.structureImageUrl(campaign.id, structureTypeId, campaign.revision);
  };

  protected itemObjectiveImageUrl = (typeId: string): string | null => {
    const campaign = this.campaign();
    const type = campaign?.itemObjectiveTypes?.find((item) => item.id === typeId);
    if (!campaign || !type?.hasImage) {
      return null;
    }

    return this.campaignsApi.itemObjectiveImageUrl(campaign.id, typeId, campaign.revision);
  };

  protected standingItemImageUrl(item: { typeId: string; hasImage?: boolean }): string | null {
    const campaign = this.campaign();
    if (!campaign || !item.hasImage) {
      return null;
    }

    return this.campaignsApi.itemObjectiveImageUrl(campaign.id, item.typeId, campaign.revision);
  }

  protected flagImageUrl = (factionId: string): string | null => {
    const campaign = this.campaign();
    const faction = campaign?.factions.find((item) => item.id === factionId);
    if (!campaign || !faction?.hasFlagImage) {
      return null;
    }

    return this.campaignsApi.flagImageUrl(campaign.id, factionId, campaign.revision);
  };

  protected missionFileUrl(mission: CampaignMission): string | null {
    const campaign = this.campaign();
    if (!campaign || !mission.hasFile) {
      return null;
    }

    return this.campaignsApi.missionFileUrl(campaign.id, mission.id);
  }

  protected draftKindsFor(force: PlayForce): readonly string[] {
    return force.availableActions;
  }

  protected debugKindsFor(force: PlayForce): readonly string[] {
    return force.availableActions.length > 0
      ? force.availableActions
      : ['Hold', 'Move', 'Build', 'Pillage', 'Repair', 'Split', 'Backstab'];
  }

  protected draftFor(forceId: string): OrderDraft {
    return this.drafts()[forceId] ?? { kind: 'Hold', targetTerritoryId: '', structureTypeId: '' };
  }

  protected debugDraftFor(forceId: string): OrderDraft {
    return this.debugDrafts()[forceId] ?? { kind: 'Hold', targetTerritoryId: '', structureTypeId: '' };
  }

  protected savedDraft(forceId: string): { kind: string; targetTerritoryId: string | null } | null {
    return this.play()?.myDrafts.find((draft) => draft.forceId === forceId) ?? null;
  }

  protected onDraftKind(forceId: string, kind: string): void {
    const current = this.draftFor(forceId);
    this.drafts.update((drafts) => ({
      ...drafts,
      [forceId]: {
        kind,
        targetTerritoryId: kind === 'Move' || kind === 'Split' ? current.targetTerritoryId : '',
        structureTypeId: kind === 'Build' ? current.structureTypeId : '',
      },
    }));
  }

  protected onDraftTarget(forceId: string, targetTerritoryId: string): void {
    const current = this.draftFor(forceId);
    this.drafts.update((drafts) => ({ ...drafts, [forceId]: { ...current, targetTerritoryId } }));
  }

  protected onDraftStructure(forceId: string, structureTypeId: string): void {
    const current = this.draftFor(forceId);
    this.drafts.update((drafts) => ({ ...drafts, [forceId]: { ...current, structureTypeId } }));
  }

  protected onDebugDraftKind(forceId: string, kind: string): void {
    const current = this.debugDraftFor(forceId);
    this.debugDrafts.update((drafts) => ({
      ...drafts,
      [forceId]: {
        kind,
        targetTerritoryId: kind === 'Move' || kind === 'Split' ? current.targetTerritoryId : '',
        structureTypeId: kind === 'Build' ? current.structureTypeId : '',
      },
    }));
  }

  protected onDebugDraftTarget(forceId: string, targetTerritoryId: string): void {
    const current = this.debugDraftFor(forceId);
    this.debugDrafts.update((drafts) => ({ ...drafts, [forceId]: { ...current, targetTerritoryId } }));
  }

  protected onDebugDraftStructure(forceId: string, structureTypeId: string): void {
    const current = this.debugDraftFor(forceId);
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
    return (
      (battle.isMine || battle.canStaffConfirm === true) &&
      (battle.status === 'Pending' || battle.status === 'AwaitingResults' || battle.status === 'Disputed')
    );
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
    this.patchReport(battleId, forceId, { [field]: Number.isFinite(parsed) ? Math.max(0, parsed) : 0 });
  }

  protected battleReportFlag(
    battleId: string,
    forceId: string,
    field: 'killedEnemyGeneral' | 'destroyedEnemySupplyLine',
  ): boolean {
    return this.reportFor(battleId, forceId)[field];
  }

  protected onBattleReportFlag(
    battleId: string,
    forceId: string,
    field: 'killedEnemyGeneral' | 'destroyedEnemySupplyLine',
    value: boolean,
  ): void {
    this.patchReport(battleId, forceId, { [field]: value });
  }

  protected battleQuestionBoolean(battleId: string, forceId: string, questionId: string): boolean {
    return this.answerFor(battleId, forceId, questionId).booleanValue === true;
  }

  protected onBattleQuestionBoolean(battleId: string, forceId: string, questionId: string, value: boolean): void {
    this.patchAnswer(battleId, forceId, questionId, { booleanValue: value, battlePointsValue: null });
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
    const units = this.reportFor(battle.id, forceId).supplyCostingUnitCount;
    const supply = battle.forceSupplies?.find((item) => item.forceId === forceId);
    const allowance = supply?.forceAllowancePoints ?? 0;
    const recurring = Math.min(units, allowance);
    const temporary = Math.max(0, units - allowance);
    if (temporary === 0) {
      return `Spends ${recurring} from territory and round supply.`;
    }

    return `Spends ${recurring} from territory and round supply, then ${temporary} temporary.`;
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
    this.patchReport(battleId, forceId, { armyListText: value });
    this.scheduleArmyListParse(battleId, forceId);
  }

  protected armyListGameSystem(battleId: string, forceId: string): string {
    return this.reportFor(battleId, forceId).armyListGameSystem ?? 'WarhammerTheOldWorld';
  }

  protected onArmyListGameSystem(battleId: string, forceId: string, value: string): void {
    this.patchReport(battleId, forceId, { armyListGameSystem: value });
    this.scheduleArmyListParse(battleId, forceId);
  }

  protected armyListBuilder(battleId: string, forceId: string): string {
    return this.reportFor(battleId, forceId).armyListBuilder ?? 'Other';
  }

  protected onArmyListBuilder(battleId: string, forceId: string, value: string): void {
    this.patchReport(battleId, forceId, { armyListBuilder: value });
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
    this.patchReport(battleId, forceId, { supplyCategories: categories, supplyCostingUnitCount });
  }

  protected opponentArmyList(battle: PlayBattle, forceId: string): string | null {
    const text = battle.opponentSubmission?.reports?.find((report) => report.forceId === forceId)?.armyListText?.trim();
    return text ?? null;
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
      this.patchReport(battleId, forceId, {
        armyPoints: result.armyPoints,
        supplyCostingUnitCount: result.supplyCostingUnitCount,
        supplyCategories: result.categories,
      });
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
    if (existing) {
      return existing;
    }

    return {
      forceId,
      victoryPoints: 0,
      armyPoints: 0,
      differentialBattlePoints: 0,
      bonusBattlePoints: 0,
      supplyCostingUnitCount: 0,
      armyListText: '',
      armyListGameSystem: 'WarhammerTheOldWorld',
      armyListBuilder: 'Other',
      supplyCategories: [],
      killedEnemyGeneral: false,
      destroyedEnemySupplyLine: false,
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

  private patchReport(battleId: string, forceId: string, patch: Partial<BattleParticipantReport>): void {
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

  protected leaderboardTitle(kind: string): string {
    switch (kind) {
      case 'MostTerritories':
        return 'Most territories';
      case 'LongestTerritoryChain':
        return 'Longest territory chain';
      case 'MostBattlesWon':
        return 'Most battles won';
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

    return `${leader.metric} territories`;
  }

  protected onRetreatTarget(battleId: string, targetTerritoryId: string): void {
    this.retreatTarget.update((current) => ({ ...current, [battleId]: targetTerritoryId }));
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

    await this.runPlay(() =>
      this.campaignsApi.submitBattleResult(play.id, {
        revision: play.revision,
        battleId: battle.id,
        winnerForceId: null,
        isDraw: false,
        reports: this.reportsFor(battle),
      }),
    );
  }

  protected async acceptBattle(battle: PlayBattle): Promise<void> {
    const play = this.play();
    if (!play) {
      return;
    }

    await this.runPlay(() => this.campaignsApi.acceptBattleResult(play.id, battle.id, play.revision));
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

  protected async submitRetreat(battle: PlayBattle): Promise<void> {
    const play = this.play();
    const targetTerritoryId = this.retreatTarget()[battle.id];
    if (!play || !targetTerritoryId) {
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

  protected async submitSurrender(battle: PlayBattle): Promise<void> {
    const play = this.play();
    const targetTerritoryId = this.retreatTarget()[battle.id];
    if (!play || !targetTerritoryId) {
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

  protected async applyDebugCorrection(force: PlayForce): Promise<void> {
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
      const blob = await rasterizeMapPng(imageUrl, this.graph().territories);
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

    const blob = new Blob([serializeMapSvg(this.graph())], { type: 'image/svg+xml' });
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

    return `Round ${round} - ${label}`;
  }

  protected phaseEndTimestamp(endsUtc: string): string {
    return formatPhaseEndTimestamp(endsUtc, this.timeZoneId());
  }

  protected onPhaseExpired(): void {
    void this.refreshBoard();
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
    return force?.availableActions ?? [];
  }

  protected mapActionPrompt(): string | null {
    const flow = this.mapAction();
    if (flow?.step === 'pick-target') {
      return `Select a destination for ${flow.kind}.`;
    }

    if (flow?.step === 'pick-structure') {
      return 'Select a structure to build.';
    }

    return null;
  }

  protected confirmActionSummary(): string {
    const flow = this.mapAction();
    if (!flow) {
      return '';
    }

    const origin = this.territoryName(flow.originId);
    if (flow.kind === 'Move' || flow.kind === 'Split') {
      return `${flow.kind} from ${origin} to ${this.territoryName(flow.targetTerritoryId)}?`;
    }

    if (flow.kind === 'Build') {
      const name = this.buildableStructures().find((type) => type.id === flow.structureTypeId)?.name ?? 'a structure';
      return `Build ${name} in ${origin}?`;
    }

    return `${flow.kind} in ${origin}?`;
  }

  protected onMapActionKind(kind: string): void {
    const flow = this.mapAction();
    if (!flow) {
      return;
    }

    if (kind === 'Move' || kind === 'Split') {
      this.mapAction.set({ ...flow, step: 'pick-target', kind, targetTerritoryId: '', structureTypeId: '' });
      this.selectedIds.set([flow.originId]);
      return;
    }

    if (kind === 'Build') {
      this.mapAction.set({ ...flow, step: 'pick-structure', kind, targetTerritoryId: '', structureTypeId: '' });
      return;
    }

    this.mapAction.set({ ...flow, step: 'confirm', kind, targetTerritoryId: '', structureTypeId: '' });
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

    this.drafts.update((drafts) => ({
      ...drafts,
      [force.id]: {
        kind: flow.kind,
        targetTerritoryId: flow.targetTerritoryId,
        structureTypeId: flow.structureTypeId,
      },
    }));
    this.cancelMapAction();
    await this.saveDraft(force);
  }

  private handleMapActionSelect(event: { id: string; additive: boolean; clientX?: number; clientY?: number }): boolean {
    const play = this.play();
    if (!this.isActionPhase() || !play?.isParticipant || play.canChooseFaction || play.isCommitted) {
      return false;
    }

    const flow = this.mapAction();
    if (flow?.step === 'pick-target') {
      const force = this.myForces().find((item) => item.id === flow.forceId);
      if (force?.moveTargets.includes(event.id) && event.id !== flow.originId) {
        this.mapAction.set({ ...flow, step: 'confirm', targetTerritoryId: event.id });
        this.selectedIds.set([flow.originId, event.id]);
        return true;
      }

      this.cancelMapAction();
    } else if (flow) {
      this.cancelMapAction();
    }

    const occupying = this.myForces().find((item) => item.territoryId === event.id && item.availableActions.length > 0);
    if (!occupying) {
      return false;
    }

    const { x, y } = this.menuPosition(event.clientX ?? 0, event.clientY ?? 0);
    this.selectedIds.set([event.id]);
    this.hoveredTerritoryId.set(event.id);
    this.mapAction.set({
      step: 'menu',
      forceId: occupying.id,
      originId: event.id,
      kind: '',
      targetTerritoryId: '',
      structureTypeId: '',
      menuX: x,
      menuY: y,
    });
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
      return draft.targetTerritoryId.length > 0;
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
      (saved.structureTypeId ?? '') === draft.structureTypeId
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
      this.error.set(readApiError(error, 'Unable to save play changes.'));
    }
  }

  private applyPlay(play: CampaignPlayDetail): void {
    const previousRevision = this.play()?.revision ?? this.campaign()?.revision ?? 0;
    this.play.set(play);
    this.cancelMapAction();
    this.roundCount.set(play.roundCount);
    this.extensionWindowId.set(play.remainingWindows[0]?.id ?? '');
    const drafts: Record<string, OrderDraft> = {};
    for (const force of play.forces.filter((item) => item.isMine)) {
      const saved = play.myDrafts.find((draft) => draft.forceId === force.id);
      drafts[force.id] = {
        kind: saved?.kind ?? 'Hold',
        targetTerritoryId: saved?.targetTerritoryId ?? '',
        structureTypeId: saved?.structureTypeId ?? '',
      };
    }

    this.drafts.set(drafts);
    const debugDrafts: Record<string, OrderDraft> = {};
    for (const force of play.forces) {
      const saved = play.debugDrafts.find((draft) => draft.forceId === force.id);
      debugDrafts[force.id] = {
        kind: saved?.kind ?? 'Hold',
        targetTerritoryId: saved?.targetTerritoryId ?? '',
        structureTypeId: saved?.structureTypeId ?? '',
      };
    }

    this.debugDrafts.set(debugDrafts);
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
            specialRules: play.specialRules ?? current.specialRules,
          }
        : current,
    );
    if (play.revision !== previousRevision) {
      this.mapRevision.set(play.revision);
      void this.reloadGraph(play.id);
    }
  }

  private applyLog(snapshot: CampaignLogSync, force = false): void {
    this.campaign.update((current) =>
      current && (force || snapshot.revision > current.revision) ? mergeCampaignLog(current, snapshot) : current,
    );
    this.play.update((current) =>
      current && (force || snapshot.revision > current.revision) ? mergeCampaignLog(current, snapshot) : current,
    );
  }

  private startPolling(): void {
    if (this.logPollStarted) {
      return;
    }

    this.logPollStarted = true;
    const timer = globalThis.setInterval(() => void this.refreshBoard(), CAMPAIGN_LOG_POLL_MS);
    this.destroyRef.onDestroy(() => globalThis.clearInterval(timer));
  }

  protected async pullLog(): Promise<void> {
    await this.refreshBoard();
  }

  private shouldLoadPlay(campaign: CampaignDetail): boolean {
    return campaign.status !== 'Scheduled' || Date.parse(campaign.startsUtc) <= Date.now();
  }

  private async refreshBoard(): Promise<void> {
    const campaign = this.campaign();
    if (!campaign || this.chatBusy() || globalThis.document.visibilityState === 'hidden') {
      return;
    }

    try {
      if (this.shouldLoadPlay(campaign)) {
        const play = await this.campaignsApi.getPlay(campaign.id);
        this.applyPlay(play);
        return;
      }

      const next = await this.campaignsApi.get(campaign.id);
      this.applyLog(next);
      if (this.shouldLoadPlay(next)) {
        this.campaign.set(next);
        const play = await this.campaignsApi.getPlay(next.id);
        this.applyPlay(play);
      }
    } catch {
      // Keep the visible board; the next poll retries.
    }
  }

  private async reloadGraph(id: string): Promise<void> {
    try {
      const graph = await this.campaignsApi.getMapGraph(id);
      this.applyGraph(graph);
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
        spawnFactionId: territory.spawnFactionId,
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
      this.factionChoice.set(campaign.factionId ?? '');
      this.subfactionChoice.set(campaign.subfaction ?? '');
      this.mapRevision.set(campaign.revision);
      this.startPolling();
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

  private restoreViewPrefs(campaignId: string): void {
    const stored = readStoredPrefs(campaignId);
    const prefs = stored ?? defaultCampaignViewPrefs();
    this.highlightMode.set(prefs.highlightMode);
    this.standingsSort.set(prefs.standingsSort);
    this.chatChannelKey.set(prefs.chatChannelKey);
    this.lastChatScrollTop = prefs.chatScrollTop;
    this.restoreChatScroll.set(stored ? stored.chatScrollTop : null);
    if (stored?.sections) {
      const next = openSections();
      for (const id of CAMPAIGN_SECTIONS) {
        if (typeof stored.sections[id] === 'boolean') {
          next[id] = stored.sections[id]!;
        }
      }

      this.openSections.set(next);
    }

    this.prefsHydrated = true;
  }

  private persistViewPrefs(): void {
    const campaign = this.campaign();
    if (!campaign || !this.prefsHydrated) {
      return;
    }

    this.viewPrefs.write(campaign.id, {
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

    return this.campaignsApi.itemObjectiveImageUrl(campaign.id, item.typeId, campaign.revision);
  }
}
