import { Component, computed, DestroyRef, inject, signal, viewChild, type ElementRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { AuthService, readApiError } from '../../core/auth/auth.service';
import { CampaignService } from '../../core/campaigns/campaign.service';
import type {
  CampaignDetail,
  CampaignMission,
  CampaignPlayDetail,
  MapGraphDetail,
  PlayBattle,
  PlayForce,
  PlayItemObjective,
} from '../../core/campaigns/campaign.models';
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
  type MapItemMarker,
} from '../../shared/campaign-map-view/campaign-map-view.component';
import { MapSymbolComponent } from '../../shared/map-symbol/map-symbol.component';
import { PhaseCountdownComponent } from '../../shared/phase-countdown/phase-countdown.component';

const CAMPAIGN_SECTIONS = [
  'log',
  'faction',
  'missingFaction',
  'map',
  'itemObjectives',
  'orders',
  'battles',
  'debug',
  'schedule',
  'details',
  'round',
  'factions',
  'allies',
  'links',
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
  protected readonly durationUnits = DURATION_UNITS;
  protected readonly factionChoice = signal('');
  protected readonly subfactionChoice = signal('');
  protected readonly roundCount = signal(3);
  protected readonly extensionAmount = signal(1);
  protected readonly extensionUnit = signal('Hours');
  protected readonly extensionWindowId = signal('');
  protected readonly drafts = signal<Record<string, OrderDraft>>({});
  protected readonly debugDrafts = signal<Record<string, OrderDraft>>({});
  protected readonly mapAction = signal<MapActionFlow | null>(null);
  protected readonly battleWinner = signal<Record<string, string>>({});
  protected readonly retreatTarget = signal<Record<string, string>>({});
  private readonly mapRevision = signal(0);
  private logPollStarted = false;

  constructor() {
    const id = this.route.snapshot.paramMap.get('id');
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
    if (!play) {
      return [];
    }

    return play.forces.map((force) => ({
      id: force.id,
      territoryId: force.territoryId,
      factionId: force.factionId,
      isMine: force.isMine,
      inBattle: force.inBattle,
      label: `${this.forceLabel(force)} in ${this.territoryName(force.territoryId)}`,
    }));
  });
  protected readonly mapItems = computed((): MapItemMarker[] => {
    const play = this.play();
    if (!play) {
      return [];
    }

    const forces = new Map(play.forces.map((force) => [force.id, force]));
    return (play.itemObjectives ?? []).flatMap((item) => {
      const territoryId = item.possessorForceId
        ? (forces.get(item.possessorForceId)?.territoryId ?? null)
        : item.territoryId;
      if (!territoryId) {
        return [];
      }

      return [
        {
          id: item.id,
          territoryId,
          name: item.name,
          carried: !!item.possessorForceId,
          hidden: !item.isRevealed,
        },
      ];
    });
  });
  protected readonly visibleItemObjectives = computed(() => this.play()?.itemObjectives ?? []);
  protected readonly hiddenItemCount = computed(
    () => (this.play()?.itemObjectives ?? []).filter((item) => !item.isRevealed).length,
  );

  protected isOpen(id: CampaignSection): boolean {
    return this.openSections()[id] !== false;
  }

  protected toggleSection(id: CampaignSection): void {
    this.openSections.update((current) => ({ ...current, [id]: !current[id] }));
  }

  protected setSection(id: CampaignSection, open: boolean): void {
    this.openSections.update((current) => ({ ...current, [id]: open }));
  }

  protected expandAllSections(): void {
    this.openSections.set(openSections());
  }

  protected collapseAllSections(): void {
    this.openSections.set(
      Object.fromEntries(CAMPAIGN_SECTIONS.map((id) => [id, false])) as Record<CampaignSection, boolean>,
    );
  }

  protected asNumber(value: string | number): number {
    return Number(value);
  }

  protected async postChat(message: string): Promise<void> {
    const campaign = this.campaign();
    if (!campaign) {
      return;
    }

    this.chatError.set(null);
    this.chatBusy.set(true);
    try {
      const next = await this.campaignsApi.postChat(campaign.id, { revision: campaign.revision, message });
      this.applyLog(next, true);
    } catch (error: unknown) {
      this.chatError.set(readApiError(error, 'Unable to send that chat message.'));
    } finally {
      this.chatBusy.set(false);
    }
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
    return `${name} · ${this.factionName(force.factionId)}`;
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

  protected async submitBattle(battle: PlayBattle, isDraw: boolean): Promise<void> {
    const play = this.play();
    if (!play) {
      return;
    }

    const winnerForceId = isDraw ? null : this.battleWinner()[battle.id] || null;
    await this.runPlay(() =>
      this.campaignsApi.submitBattleResult(play.id, {
        revision: play.revision,
        battleId: battle.id,
        winnerForceId,
        isDraw,
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

  protected async resolveBattle(battle: PlayBattle, isDraw: boolean): Promise<void> {
    const play = this.play();
    if (!play) {
      return;
    }

    await this.runPlay(() =>
      this.campaignsApi.resolveBattle(play.id, {
        revision: play.revision,
        battleId: battle.id,
        winnerForceId: isDraw ? null : this.battleWinner()[battle.id] || null,
        isDraw,
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
            mentionableMembers: play.mentionableMembers,
            log: play.log,
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
    } catch (error: unknown) {
      this.error.set(readApiError(error, 'Unable to load this campaign.'));
    } finally {
      this.loading.set(false);
    }
  }
}
