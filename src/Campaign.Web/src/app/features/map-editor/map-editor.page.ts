import { Component, computed, HostListener, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { readApiErrorMessages } from '../../core/auth/auth.service';
import { CampaignService } from '../../core/campaigns/campaign.service';
import type { CampaignDetail, CampaignMission, MapGraphDetail } from '../../core/campaigns/campaign.models';
import { missionsForTerritory, structureTypeById, terrainTypeById } from '../../core/campaigns/campaign.models';
import { FORM_SAVE_SUCCESS_MESSAGE } from '../../core/forms/form-messages';
import { FormSubmitOverlayService } from '../../core/forms/form-submit-overlay.service';
import {
  adjacencyMarker,
  adjacentTerritoryIds,
  connects,
  generateAdjacencies,
  orderedPair,
} from '../../core/maps/adjacency';
import {
  clampPoint,
  clampTranslation,
  containsStrict,
  encloseAlongImageEdge,
  encloseAlongTouchedBorders,
  interiorsOverlap,
  isValidTerritoryPolygon,
  MIN_DRAW_STEP,
  snapToExistingGeometry,
  traceSharedBorder,
  translatePolygon,
  type MapPoint,
} from '../../core/maps/geometry';
import {
  cloneGraph,
  createId,
  nextDisplayNumber,
  territoryLabel,
  type MapAdjacency,
  type MapGraph,
  type MapTerritory,
} from '../../core/maps/map-graph.models';
import { CampaignMapViewComponent } from '../../shared/campaign-map-view/campaign-map-view.component';
import { MapSymbolComponent } from '../../shared/map-symbol/map-symbol.component';

export type MapEditorTool = 'draw' | 'erase' | 'select' | 'connect';

@Component({
  selector: 'app-map-editor-page',
  imports: [FormsModule, RouterLink, CampaignMapViewComponent, MapSymbolComponent],
  templateUrl: './map-editor.page.html',
  styleUrl: './map-editor.page.css',
})
export class MapEditorPage {
  private readonly campaignsApi = inject(CampaignService);
  private readonly overlay = inject(FormSubmitOverlayService);
  private readonly route = inject(ActivatedRoute);

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly errorMessages = signal<string[]>([]);
  protected readonly successMessage = signal<string | null>(null);
  protected readonly campaign = signal<CampaignDetail | null>(null);
  protected readonly graph = signal<MapGraph>({ territories: [], adjacencies: [] });
  protected readonly drawing = signal<MapPoint[]>([]);
  protected readonly snapTarget = signal<MapPoint | null>(null);
  protected readonly tool = signal<MapEditorTool>('draw');
  protected readonly selectedIds = signal<string[]>([]);
  protected readonly hoveredTerritoryId = signal<string | null>(null);
  protected readonly hoveredAdjacencyId = signal<string | null>(null);
  protected readonly connectPendingId = signal<string | null>(null);
  protected readonly drawingActive = signal(false);

  protected readonly tools: MapEditorTool[] = ['draw', 'erase', 'select', 'connect'];

  private revision = 0;
  private undoStack: MapGraph[] = [];
  private strokeStartCount = 0;
  private readonly campaignId = this.route.snapshot.paramMap.get('id');
  private moveBaseline: MapGraph | null = null;
  private moveDidMove = false;

  protected readonly canManage = computed(() => this.campaign()?.canManage === true);
  protected readonly mapSrc = computed(() => {
    const campaign = this.campaign();
    return campaign?.hasMap ? this.campaignsApi.mapUrl(campaign.id, campaign.revision) : null;
  });
  protected readonly selected = computed(() => {
    const id = this.selectedId();
    return this.graph().territories.find((territory) => territory.id === id) ?? null;
  });
  protected readonly selectedId = computed(() => this.selectedIds().at(-1) ?? null);
  protected readonly inspected = computed(() => {
    const hoveredAdj = this.hoveredAdjacencyId();
    if (hoveredAdj) {
      const edge = this.graph().adjacencies.find((item) => item.id === hoveredAdj);
      if (edge) {
        return this.graph().territories.find((territory) => territory.id === edge.territoryAId) ?? this.selected();
      }
    }

    const hoverId = this.hoveredTerritoryId();
    return this.graph().territories.find((territory) => territory.id === hoverId) ?? this.selected();
  });
  protected readonly highlightedTerritoryIds = computed(() => {
    const ids = [...this.selectedIds()];
    const hover = this.hoveredTerritoryId();
    if (hover) {
      ids.push(hover);
    }

    const edge = this.graph().adjacencies.find((item) => item.id === this.hoveredAdjacencyId());
    if (edge) {
      ids.push(edge.territoryAId, edge.territoryBId);
    }

    return [...new Set(ids)];
  });
  protected readonly adjacentTerritoryIds = computed(() =>
    adjacentTerritoryIds(this.graph().adjacencies, this.highlightedTerritoryIds()),
  );
  protected readonly sortedTerritories = computed(() =>
    [...this.graph().territories].sort((left, right) => territoryLabel(left).localeCompare(territoryLabel(right))),
  );
  protected readonly sortedFactions = computed(() =>
    [...(this.campaign()?.factions ?? [])].sort((left, right) => left.name.localeCompare(right.name)),
  );
  protected readonly catalogTerrains = computed(() =>
    [...(this.campaign()?.terrainTypes ?? [])].sort((left, right) => left.name.localeCompare(right.name)),
  );
  protected readonly catalogStructures = computed(() =>
    [...(this.campaign()?.structureTypes ?? [])].sort((left, right) => left.name.localeCompare(right.name)),
  );

  constructor() {
    if (this.campaignId) {
      void this.load(this.campaignId);
    } else {
      this.errorMessages.set(['The campaign was not found.']);
      this.loading.set(false);
    }
  }

  protected toolLabel(tool: MapEditorTool): string {
    switch (tool) {
      case 'draw':
        return 'Draw';
      case 'erase':
        return 'Erase';
      case 'select':
        return 'Select';
      case 'connect':
        return 'Add or delete arrows';
    }
  }

  protected onToolChange(tool: MapEditorTool): void {
    this.tool.set(tool);
    this.connectPendingId.set(null);
    if (tool !== 'draw') {
      this.drawing.set([]);
      this.drawingActive.set(false);
    }
  }

  protected onMapHover(point: MapPoint): void {
    if (this.tool() === 'draw' && this.canManage()) {
      this.snapTarget.set(snapToExistingGeometry(point, this.snapVertices(), this.polygons()));
      if (this.drawingActive()) {
        this.appendDrawPoint(point);
      }
    } else {
      this.snapTarget.set(null);
    }
  }

  protected onMapPoint(point: MapPoint): void {
    if (!this.canManage()) {
      return;
    }

    if (this.tool() === 'draw') {
      if (!this.drawingActive()) {
        this.strokeStartCount = this.drawing().length;
      }

      this.drawingActive.set(true);
      this.appendDrawPoint(point, { force: true });
      return;
    }

    if (this.tool() === 'erase') {
      this.eraseAt(point);
    }
  }

  protected onTerritoryMove(event: { origin: MapPoint; current: MapPoint }): void {
    if (this.tool() !== 'select' || !this.canManage()) {
      return;
    }

    const ids = this.selectedIds();
    if (ids.length === 0) {
      return;
    }

    if (!this.moveBaseline) {
      this.moveBaseline = cloneGraph(this.graph());
      this.moveDidMove = false;
    }

    const moved = moveSelection(
      this.moveBaseline,
      ids,
      event.current.x - event.origin.x,
      event.current.y - event.origin.y,
    );
    if (!moved) {
      return;
    }

    this.moveDidMove = Math.hypot(event.current.x - event.origin.x, event.current.y - event.origin.y) >= MIN_DRAW_STEP;
    this.graph.set(moved);
  }

  protected onTerritoryMoveEnd(): void {
    if (this.moveBaseline) {
      if (this.moveDidMove) {
        this.undoStack.push(this.moveBaseline);
        if (this.undoStack.length > 40) {
          this.undoStack.shift();
        }
      } else {
        this.graph.set(this.moveBaseline);
      }
    }

    this.moveBaseline = null;
    this.moveDidMove = false;
  }

  protected deleteSelectedTerritories(): void {
    if (!this.canManage()) {
      return;
    }

    const ids = this.selectedIds();
    if (ids.length === 0) {
      return;
    }

    this.pushUndo();
    const remove = new Set(ids);
    this.graph.update((graph) => ({
      territories: graph.territories.filter((territory) => !remove.has(territory.id)),
      adjacencies: graph.adjacencies.filter((edge) => !remove.has(edge.territoryAId) && !remove.has(edge.territoryBId)),
    }));
    this.selectedIds.set([]);
  }

  protected deleteLabel(): string {
    return this.selectedIds().length > 1 ? 'Delete territories' : 'Delete territory';
  }

  protected onBackground(): void {
    if (this.tool() === 'select') {
      this.selectedIds.set([]);
    }
  }

  protected onTerritorySelect(event: { id: string; additive: boolean } | string): void {
    const id = typeof event === 'string' ? event : event.id;
    const additive = typeof event === 'string' ? false : event.additive;
    if (this.tool() === 'connect' && this.canManage()) {
      this.handleConnectTerritory(id);
      return;
    }

    if (this.tool() === 'erase' && this.canManage()) {
      this.deleteTerritory(id);
      return;
    }

    if (this.tool() !== 'select' && this.tool() !== 'connect') {
      this.selectedIds.set([id]);
      return;
    }

    if (additive) {
      this.selectedIds.update((current) =>
        current.includes(id) ? current.filter((item) => item !== id) : [...current, id],
      );
      return;
    }

    this.selectedIds.set(this.selectedIds().includes(id) ? this.selectedIds() : [id]);
  }

  protected onAdjacencySelect(id: string): void {
    if (this.tool() === 'connect' && this.canManage()) {
      this.pushUndo();
      this.graph.update((graph) => ({
        ...graph,
        adjacencies: graph.adjacencies.filter((edge) => edge.id !== id),
      }));
      this.hoveredAdjacencyId.set(null);
      return;
    }

    const edge = this.graph().adjacencies.find((item) => item.id === id);
    if (edge) {
      this.selectedIds.set([edge.territoryAId]);
    }
  }

  protected onTerritoryHover(id: string | null): void {
    this.hoveredTerritoryId.set(id);
  }

  protected onAdjacencyHover(id: string | null): void {
    this.hoveredAdjacencyId.set(id);
  }

  protected closePolygon(): void {
    const points = this.drawing();
    if (!this.canManage() || points.length < 3) {
      return;
    }

    if (!isValidTerritoryPolygon(points)) {
      this.revealErrors(['That shape must stay on the map, stay closed, and not cross itself.']);
      return;
    }

    if (this.graph().territories.some((territory) => interiorsOverlap(points, territory.polygon))) {
      this.revealErrors(['Territories cannot overlap. They may share a border.']);
      return;
    }

    this.pushUndo();
    const defaultTerrain = this.catalogTerrains().at(0);
    if (!defaultTerrain) {
      this.revealErrors(['Add at least one terrain type in campaign setup before drawing territories.']);
      return;
    }

    const territory: MapTerritory = {
      id: createId(),
      displayNumber: nextDisplayNumber(this.graph().territories),
      name: null,
      description: null,
      polygon: points.map((point) => ({ ...point })),
      terrainTypeId: defaultTerrain.id,
      structureTypeId: null,
      overlayColor: null,
      ownerFactionId: null,
      spawnFactionId: null,
    };
    this.graph.update((graph) => ({ ...graph, territories: [...graph.territories, territory] }));
    this.drawing.set([]);
    this.drawingActive.set(false);
    this.selectedIds.set([territory.id]);
    this.tool.set('select');
    this.successMessage.set(null);
    this.errorMessages.set([]);
  }

  protected cancelDrawing(): void {
    this.drawing.set([]);
    this.drawingActive.set(false);
    this.snapTarget.set(null);
  }

  protected undo(): void {
    if (!this.canManage()) {
      return;
    }

    if (this.drawing().length > 0) {
      this.drawing.update((points) => points.slice(0, -1));
      return;
    }

    const previous = this.undoStack.pop();
    if (previous) {
      this.graph.set(previous);
    }
  }

  protected generateConnections(): void {
    if (!this.canManage()) {
      return;
    }

    this.pushUndo();
    this.graph.update((graph) => ({
      ...graph,
      adjacencies: generateAdjacencies(graph.territories, graph.adjacencies),
    }));
  }

  protected clearConnections(): void {
    if (!this.canManage() || this.graph().adjacencies.length === 0) {
      return;
    }

    this.pushUndo();
    this.graph.update((graph) => ({ ...graph, adjacencies: [] }));
  }

  protected colorRandom(): void {
    if (!this.canManage()) {
      return;
    }

    this.pushUndo();
    this.graph.update((graph) => ({
      ...graph,
      territories: graph.territories.map((territory) => ({ ...territory, overlayColor: randomOverlayColor() })),
    }));
  }

  protected colorByTerrain(): void {
    if (!this.canManage()) {
      return;
    }

    this.pushUndo();
    this.graph.update((graph) => ({
      ...graph,
      territories: graph.territories.map((territory) => ({
        ...territory,
        overlayColor: terrainTypeById(this.campaign(), territory.terrainTypeId)?.color ?? null,
      })),
    }));
  }

  protected colorClear(): void {
    if (!this.canManage()) {
      return;
    }

    this.pushUndo();
    this.graph.update((graph) => ({
      ...graph,
      territories: graph.territories.map((territory) => ({ ...territory, overlayColor: null })),
    }));
  }

  protected setName(value: string): void {
    this.patchSelected((territory) => ({ ...territory, name: value.trim() || null }));
  }

  protected setDescription(value: string): void {
    this.patchSelected((territory) => ({ ...territory, description: value.trim() || null }));
  }

  protected setTerrain(value: string): void {
    this.patchSelected((territory) => ({ ...territory, terrainTypeId: value }));
  }

  protected setStructure(value: string): void {
    this.patchSelected((territory) => ({ ...territory, structureTypeId: value || null }));
  }

  protected setOwner(value: string): void {
    this.patchSelected((territory) => ({ ...territory, ownerFactionId: value || null }));
  }

  protected setSpawn(value: string): void {
    this.patchSelected((territory) => ({ ...territory, spawnFactionId: value || null }));
  }

  protected setOverlayColor(value: string): void {
    this.patchSelected((territory) => ({ ...territory, overlayColor: value || null }));
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

  protected spawnTaken(factionId: string): boolean {
    const selectedId = this.selectedId();
    return this.graph().territories.some(
      (territory) => territory.spawnFactionId === factionId && territory.id !== selectedId,
    );
  }

  protected structureImageUrl = (structureTypeId: string): string | null => {
    const campaign = this.campaign();
    const structure = structureTypeById(campaign, structureTypeId);
    if (!campaign || !structure?.hasImage) {
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

  protected inspectedMissions(): CampaignMission[] {
    const territory = this.inspected();
    return missionsForTerritory(this.campaign(), territory?.terrainTypeId, territory?.structureTypeId);
  }

  protected terrainName(id: string | null): string {
    return terrainTypeById(this.campaign(), id)?.name ?? 'None';
  }

  protected structureName(id: string | null): string {
    return structureTypeById(this.campaign(), id)?.name ?? 'None';
  }

  protected terrainSymbol(id: string | null | undefined): string | null {
    return terrainTypeById(this.campaign(), id)?.name ?? null;
  }

  protected structureSymbol(id: string | null | undefined): string | null {
    return structureTypeById(this.campaign(), id)?.builtinSymbol ?? null;
  }

  protected labelFor(territory: MapTerritory): string {
    return territoryLabel(territory);
  }

  @HostListener('document:pointerup')
  @HostListener('document:pointercancel')
  protected onPointerUp(): void {
    if (this.drawingActive()) {
      if (!this.encloseImageEdgeIfEligible() && !this.encloseTouchedBordersIfEligible()) {
        this.traceSharedBorderIfEligible();
      }
    }

    this.drawingActive.set(false);
  }

  @HostListener('document:keydown', ['$event'])
  protected onKeydown(event: KeyboardEvent): void {
    if (!this.canManage()) {
      return;
    }

    const target = event.target as HTMLElement | null;
    const typing = target?.tagName === 'INPUT' || target?.tagName === 'TEXTAREA' || target?.tagName === 'SELECT';
    if (event.key === 'Enter' && this.drawing().length >= 3 && !typing) {
      event.preventDefault();
      this.closePolygon();
      return;
    }

    if (event.key === 'Escape') {
      this.cancelDrawing();
      this.connectPendingId.set(null);
      return;
    }

    if (
      (event.key === 'Delete' || event.key === 'Backspace') &&
      !typing &&
      this.drawing().length === 0 &&
      this.selectedIds().length > 0
    ) {
      event.preventDefault();
      this.deleteSelectedTerritories();
      return;
    }

    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'z') {
      event.preventDefault();
      this.undo();
    }
  }

  protected async save(): Promise<void> {
    const campaign = this.campaign();
    if (!campaign || !this.canManage()) {
      return;
    }

    const names = this.graph()
      .territories.map((territory) => territory.name?.trim().toLowerCase())
      .filter((name): name is string => !!name);
    if (new Set(names).size !== names.length) {
      this.revealErrors(['Territory names must be unique for the campaign.']);
      return;
    }

    if (this.graph().territories.some((territory) => !territory.terrainTypeId)) {
      this.revealErrors(['Every territory needs a terrain type.']);
      return;
    }

    const spawnIds = this.graph()
      .territories.map((territory) => territory.spawnFactionId)
      .filter((id): id is string => !!id);
    if (new Set(spawnIds).size !== spawnIds.length) {
      this.revealErrors(['Each faction can have only one spawn location.']);
      return;
    }

    this.saving.set(true);
    this.errorMessages.set([]);
    this.successMessage.set(null);
    try {
      const saved = await this.overlay.run(() =>
        this.campaignsApi.saveMapGraph(campaign.id, {
          revision: this.revision,
          territories: this.graph().territories.map((territory) => ({
            id: territory.id,
            displayNumber: territory.displayNumber,
            name: territory.name,
            description: territory.description,
            polygon: territory.polygon,
            terrainTypeId: territory.terrainTypeId,
            structureTypeId: territory.structureTypeId,
            overlayColor: territory.overlayColor,
            ownerFactionId: territory.ownerFactionId,
            spawnFactionId: territory.spawnFactionId,
          })),
          adjacencies: this.graph().adjacencies.map((edge) => ({
            id: edge.id,
            territoryAId: edge.territoryAId,
            territoryBId: edge.territoryBId,
            origin: edge.origin,
            markerX: edge.marker.x,
            markerY: edge.marker.y,
          })),
        }),
      );
      this.revision = saved.revision;
      this.graph.set(fromApi(saved));
      this.campaign.update((current) => (current ? { ...current, revision: saved.revision } : current));
      this.successMessage.set(FORM_SAVE_SUCCESS_MESSAGE);
    } catch (error: unknown) {
      this.revealErrors(readApiErrorMessages(error, 'Unable to save the map.'));
    } finally {
      this.saving.set(false);
    }
  }

  private async load(id: string): Promise<void> {
    try {
      const [campaign, graph] = await Promise.all([this.campaignsApi.get(id), this.campaignsApi.getMapGraph(id)]);
      this.campaign.set(campaign);
      this.revision = graph.revision;
      this.graph.set(fromApi(graph));
      if (!campaign.canManage) {
        this.tool.set('select');
      }
    } catch (error: unknown) {
      this.revealErrors(readApiErrorMessages(error, 'Unable to load this campaign map.'));
    } finally {
      this.loading.set(false);
    }
  }

  private appendDrawPoint(point: MapPoint, options?: { force?: boolean }): void {
    const snap = snapToExistingGeometry(point, this.snapVertices(), this.polygons()) ?? clampPoint(point);
    const current = this.drawing();
    const first = current.at(0);
    if (current.length >= 3 && first && distanceClose(snap, first) && options?.force) {
      this.closePolygon();
      return;
    }

    const last = current.at(-1);
    if (last && !options?.force && !farEnough(last, snap)) {
      return;
    }

    if (last && distanceClose(last, snap)) {
      return;
    }

    this.drawing.set([...current, snap]);
  }

  private eraseAt(point: MapPoint): void {
    if (this.drawing().length > 0) {
      this.drawing.update((points) => points.slice(0, -1));
      return;
    }

    const hit = [...this.graph().territories].reverse().find((territory) => containsStrict(territory.polygon, point));
    if (hit) {
      this.deleteTerritory(hit.id);
    }
  }

  private deleteTerritory(id: string): void {
    this.pushUndo();
    this.graph.update((graph) => ({
      territories: graph.territories.filter((territory) => territory.id !== id),
      adjacencies: graph.adjacencies.filter((edge) => edge.territoryAId !== id && edge.territoryBId !== id),
    }));
    if (this.selectedIds().includes(id)) {
      this.selectedIds.update((current) => current.filter((item) => item !== id));
    }
  }

  private handleConnectTerritory(id: string): void {
    const pending = this.connectPendingId();
    if (!pending) {
      this.connectPendingId.set(id);
      this.selectedIds.set([id]);
      return;
    }

    if (pending === id) {
      this.connectPendingId.set(null);
      return;
    }

    if (this.graph().adjacencies.some((edge) => connects(edge, pending, id))) {
      this.revealErrors(['Those territories already have an adjacency arrow.']);
      this.connectPendingId.set(null);
      return;
    }

    const left = this.graph().territories.find((territory) => territory.id === pending);
    const right = this.graph().territories.find((territory) => territory.id === id);
    if (!left || !right) {
      this.connectPendingId.set(null);
      return;
    }

    this.pushUndo();
    const [a, b] = orderedPair(left.id, right.id);
    const edge: MapAdjacency = {
      id: createId(),
      territoryAId: a,
      territoryBId: b,
      origin: 'Manual',
      marker: adjacencyMarker(left, right),
    };
    this.graph.update((graph) => ({ ...graph, adjacencies: [...graph.adjacencies, edge] }));
    this.connectPendingId.set(null);
  }

  private patchSelected(mutate: (territory: MapTerritory) => MapTerritory): void {
    const id = this.selectedId();
    if (!id || !this.canManage()) {
      return;
    }

    this.graph.update((graph) => ({
      ...graph,
      territories: graph.territories.map((territory) => (territory.id === id ? mutate(territory) : territory)),
    }));
  }

  private snapVertices(): MapPoint[] {
    return [...this.graph().territories.flatMap((territory) => territory.polygon), ...this.drawing()];
  }

  private polygons(): MapPoint[][] {
    return this.graph().territories.map((territory) => territory.polygon);
  }

  private encloseImageEdgeIfEligible(): boolean {
    const enclosed = encloseAlongImageEdge(this.drawing(), this.polygons());
    if (!enclosed) {
      return false;
    }

    this.drawing.set(enclosed);
    this.closePolygon();
    return true;
  }

  private encloseTouchedBordersIfEligible(): boolean {
    const enclosed = encloseAlongTouchedBorders(this.drawing(), this.polygons());
    if (!enclosed) {
      return false;
    }

    this.drawing.set(enclosed);
    this.closePolygon();
    return true;
  }

  private traceSharedBorderIfEligible(): void {
    const points = this.drawing();
    if (points.length < 2) {
      return;
    }

    const added = points.length - this.strokeStartCount;
    const fromIndex = added <= 1 ? Math.max(0, points.length - 2) : this.strokeStartCount;
    const start = points.at(fromIndex);
    const end = points.at(-1);
    if (!start || !end) {
      return;
    }

    const traced = traceSharedBorder(start, end, this.polygons());
    if (!traced || traced.length < 2) {
      return;
    }

    this.drawing.set([...points.slice(0, fromIndex), ...traced]);
  }

  private pushUndo(): void {
    this.undoStack.push(cloneGraph(this.graph()));
    if (this.undoStack.length > 40) {
      this.undoStack.shift();
    }
  }

  private revealErrors(messages: readonly string[]): void {
    this.successMessage.set(null);
    this.errorMessages.set([...messages]);
  }
}

function fromApi(detail: MapGraphDetail): MapGraph {
  return {
    territories: detail.territories.map((territory) => ({
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
    adjacencies: detail.adjacencies.map((edge) => ({
      id: edge.id,
      territoryAId: edge.territoryAId,
      territoryBId: edge.territoryBId,
      origin: edge.origin === 'Generated' ? 'Generated' : 'Manual',
      marker: { x: edge.markerX, y: edge.markerY },
    })),
  };
}

function moveSelection(baseline: MapGraph, ids: readonly string[], dx: number, dy: number): MapGraph | null {
  const selected = new Set(ids);
  const selectedPolygons = baseline.territories
    .filter((territory) => selected.has(territory.id))
    .map((territory) => territory.polygon);
  const delta = clampTranslation(selectedPolygons, dx, dy);
  const territories = baseline.territories.map((territory) =>
    selected.has(territory.id)
      ? { ...territory, polygon: translatePolygon(territory.polygon, delta.x, delta.y) }
      : territory,
  );
  const moved = territories.filter((territory) => selected.has(territory.id));
  const others = territories.filter((territory) => !selected.has(territory.id));
  for (const territory of moved) {
    if (!isValidTerritoryPolygon(territory.polygon)) {
      return null;
    }

    if (others.some((other) => interiorsOverlap(territory.polygon, other.polygon))) {
      return null;
    }
  }

  const adjacencies = baseline.adjacencies.map((edge) => {
    if (!selected.has(edge.territoryAId) && !selected.has(edge.territoryBId)) {
      return edge;
    }

    const left = territories.find((territory) => territory.id === edge.territoryAId);
    const right = territories.find((territory) => territory.id === edge.territoryBId);
    if (!left || !right) {
      return edge;
    }

    return { ...edge, marker: adjacencyMarker(left, right) };
  });

  return { territories, adjacencies };
}

function distanceClose(left: MapPoint, right: MapPoint): boolean {
  const dx = left.x - right.x;
  const dy = left.y - right.y;
  return dx * dx + dy * dy <= 0.018 * 0.018;
}

function farEnough(left: MapPoint, right: MapPoint): boolean {
  const dx = left.x - right.x;
  const dy = left.y - right.y;
  return dx * dx + dy * dy >= MIN_DRAW_STEP * MIN_DRAW_STEP;
}

function randomOverlayColor(): string {
  const hue = Math.floor(Math.random() * 360);
  const saturation = 0.52;
  const lightness = 0.42;
  const channel = (offset: number): number => {
    const value = (offset + hue / 30) % 12;
    const chroma = saturation * Math.min(lightness, 1 - lightness);
    const mixed = lightness - chroma * Math.max(-1, Math.min(value - 3, 9 - value, 1));
    return Math.round(mixed * 255);
  };

  const red = channel(0);
  const green = channel(8);
  const blue = channel(4);
  return `#${red.toString(16).padStart(2, '0')}${green.toString(16).padStart(2, '0')}${blue.toString(16).padStart(2, '0')}`.toUpperCase();
}
