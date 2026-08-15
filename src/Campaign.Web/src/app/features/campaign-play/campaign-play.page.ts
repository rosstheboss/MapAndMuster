import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { AuthService, readApiError } from '../../core/auth/auth.service';
import { CampaignService } from '../../core/campaigns/campaign.service';
import type { CampaignPlayDetail, PlayBattle, PlayForce } from '../../core/campaigns/campaign.models';
import { DURATION_UNITS, statusLabel } from '../../core/campaigns/campaign-schedule';
import { FORM_SAVE_SUCCESS_MESSAGE } from '../../core/forms/form-messages';
import { FormSubmitOverlayService } from '../../core/forms/form-submit-overlay.service';
import { adjacentTerritoryIds } from '../../core/maps/adjacency';
import type { MapGraph, MapTerritory } from '../../core/maps/map-graph.models';
import { territoryLabel } from '../../core/maps/map-graph.models';
import { CampaignMapViewComponent } from '../../shared/campaign-map-view/campaign-map-view.component';
import { PhaseCountdownComponent } from '../../shared/phase-countdown/phase-countdown.component';
import { InstantDatePipe } from '../../shared/time/instant-date.pipe';

const DRAFT_KINDS = ['Hold', 'Move', 'Build', 'Pillage', 'Repair', 'Split', 'Backstab'] as const;

@Component({
  selector: 'app-campaign-play-page',
  imports: [FormsModule, RouterLink, CampaignMapViewComponent, PhaseCountdownComponent, InstantDatePipe],
  templateUrl: './campaign-play.page.html',
  styleUrl: './campaign-play.page.css',
})
export class CampaignPlayPage {
  private readonly campaignsApi = inject(CampaignService);
  private readonly overlay = inject(FormSubmitOverlayService);
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);

  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly successMessage = signal<string | null>(null);
  protected readonly play = signal<CampaignPlayDetail | null>(null);
  protected readonly graph = signal<MapGraph>({ territories: [], adjacencies: [] });
  protected readonly hoveredTerritoryId = signal<string | null>(null);
  protected readonly selectedIds = signal<string[]>([]);
  protected readonly draftKinds = DRAFT_KINDS;
  protected readonly durationUnits = DURATION_UNITS;
  protected readonly factionChoice = signal('');
  protected readonly subfactionChoice = signal('');
  protected readonly roundCount = signal(3);
  protected readonly extensionAmount = signal(1);
  protected readonly extensionUnit = signal('Hours');
  protected readonly extensionWindowId = signal('');
  protected readonly drafts = signal<
    Record<string, { kind: string; targetTerritoryId: string; structureTypeId: string }>
  >({});
  protected readonly battleWinner = signal<Record<string, string>>({});
  protected readonly retreatTarget = signal<Record<string, string>>({});
  protected readonly showingLog = signal(false);

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
    const play = this.play();
    return play?.hasMap ? this.campaignsApi.mapUrl(play.id, play.revision) : null;
  });

  protected readonly myForces = computed(() => this.play()?.forces.filter((force) => force.isMine) ?? []);
  protected readonly isActionPhase = computed(() => this.play()?.currentPhaseKind === 'Action');
  protected readonly isBattlePhase = computed(() => this.play()?.currentPhaseKind === 'Battle');
  protected readonly selectedFaction = computed(
    () => this.play()?.factions.find((faction) => faction.id === this.factionChoice()) ?? null,
  );

  protected hoveredTerritory = computed(() => {
    const id = this.hoveredTerritoryId() ?? this.selectedIds().at(-1) ?? null;
    return this.graph().territories.find((territory) => territory.id === id) ?? null;
  });

  protected readonly focusTerritoryIds = computed(() => {
    const ids = [...this.selectedIds()];
    const hover = this.hoveredTerritoryId();
    if (hover) {
      ids.push(hover);
    }

    return [...new Set(ids)];
  });

  protected readonly adjacentTerritoryIds = computed(() =>
    adjacentTerritoryIds(this.graph().adjacencies, this.focusTerritoryIds()),
  );

  protected asNumber(value: string | number): number {
    return Number(value);
  }

  protected timeZoneId(): string | null {
    return this.auth.currentUser()?.timeZoneId ?? null;
  }

  protected statusText(status: string): string {
    return statusLabel(status);
  }

  protected labelFor(territory: MapTerritory): string {
    return territoryLabel(territory);
  }

  protected factionName(id: string | null | undefined): string {
    if (!id) {
      return 'Neutral';
    }

    return this.play()?.factions.find((faction) => faction.id === id)?.name ?? 'Unknown faction';
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

  protected draftFor(forceId: string): { kind: string; targetTerritoryId: string; structureTypeId: string } {
    return this.drafts()[forceId] ?? { kind: 'Hold', targetTerritoryId: '', structureTypeId: '' };
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

  protected onDraftKind(forceId: string, kind: string): void {
    const current = this.draftFor(forceId);
    this.drafts.update((drafts) => ({ ...drafts, [forceId]: { ...current, kind } }));
  }

  protected onDraftTarget(forceId: string, targetTerritoryId: string): void {
    const current = this.draftFor(forceId);
    this.drafts.update((drafts) => ({ ...drafts, [forceId]: { ...current, targetTerritoryId } }));
  }

  protected onDraftStructure(forceId: string, structureTypeId: string): void {
    const current = this.draftFor(forceId);
    this.drafts.update((drafts) => ({ ...drafts, [forceId]: { ...current, structureTypeId } }));
  }

  protected flagImageUrl = (factionId: string): string | null => {
    const play = this.play();
    const faction = play?.factions.find((item) => item.id === factionId);
    if (!play || !faction?.hasFlagImage) {
      return null;
    }

    return this.campaignsApi.flagImageUrl(play.id, factionId, play.revision);
  };

  protected structureImageUrl = (structureTypeId: string): string | null => {
    const play = this.play();
    const structure = play?.structureTypes.find((item) => item.id === structureTypeId);
    if (!play || !structure?.hasImage) {
      return null;
    }

    return this.campaignsApi.structureImageUrl(play.id, structureTypeId, play.revision);
  };

  protected onTerritorySelect(event: { id: string; additive: boolean }): void {
    if (event.additive) {
      this.selectedIds.update((current) =>
        current.includes(event.id) ? current.filter((id) => id !== event.id) : [...current, event.id],
      );
      return;
    }

    this.selectedIds.set([event.id]);
    this.hoveredTerritoryId.set(event.id);
  }

  protected async chooseFaction(): Promise<void> {
    const play = this.play();
    const factionId = this.factionChoice();
    if (!play || !factionId) {
      return;
    }

    await this.runPlay(() =>
      this.campaignsApi.chooseFaction(play.id, {
        revision: play.revision,
        factionId,
        subfaction: this.subfactionChoice() || null,
      }),
    );
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
    this.play.set(play);
    this.roundCount.set(play.roundCount);
    this.extensionWindowId.set(play.remainingWindows[0]?.id ?? '');
    const drafts: Record<string, { kind: string; targetTerritoryId: string; structureTypeId: string }> = {};
    for (const force of play.forces.filter((item) => item.isMine)) {
      const saved = play.myDrafts.find((draft) => draft.forceId === force.id);
      drafts[force.id] = {
        kind: saved?.kind ?? 'Hold',
        targetTerritoryId: saved?.targetTerritoryId ?? '',
        structureTypeId: saved?.structureTypeId ?? '',
      };
    }

    this.drafts.set(drafts);
  }

  private async load(id: string): Promise<void> {
    try {
      const [play, graph] = await Promise.all([this.campaignsApi.getPlay(id), this.campaignsApi.getMapGraph(id)]);
      this.applyPlay(play);
      this.graph.set({
        territories: graph.territories.map((territory) => ({
          id: territory.id,
          displayNumber: territory.displayNumber,
          name: territory.name,
          description: territory.description,
          polygon: territory.polygon.map((point) => ({ x: point.x, y: point.y })),
          terrainTypeId: territory.terrainTypeId,
          structureTypeId: territory.structureTypeId,
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
    } catch (error: unknown) {
      this.error.set(readApiError(error, 'Unable to load this campaign.'));
    } finally {
      this.loading.set(false);
    }
  }
}
