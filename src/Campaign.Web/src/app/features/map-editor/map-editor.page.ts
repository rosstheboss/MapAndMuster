import {
  afterRenderEffect,
  Component,
  computed,
  HostListener,
  inject,
  signal,
  viewChild,
  type ElementRef,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { readApiErrorMessages } from '../../core/auth/auth.service';
import { AuthService } from '../../core/auth/auth.service';
import { CampaignService } from '../../core/campaigns/campaign.service';
import type { CampaignDetail, CampaignMission, MapGraphDetail } from '../../core/campaigns/campaign.models';
import { missionsForTerritory, structureTypeById, terrainTypeById } from '../../core/campaigns/campaign.models';
import { FORM_SAVE_SUCCESS_MESSAGE } from '../../core/forms/form-messages';
import { isAdditiveModifier } from '../../core/maps/pointer';
import { FormSubmitOverlayService } from '../../core/forms/form-submit-overlay.service';
import {
  adjacencyMarker,
  adjacentTerritoryIds,
  findConnection,
  generateAdjacencies,
  orderedPair,
} from '../../core/maps/adjacency';
import {
  clampPoint,
  clampTranslation,
  CLOSE_POLYGON_SCREEN_PX,
  containsStrict,
  encloseAlongImageEdge,
  encloseAlongTouchedBorders,
  interiorsOverlap,
  isValidTerritoryPolygon,
  MIN_DRAW_SCREEN_PX,
  MIN_DRAW_STEP,
  SNAP_SCREEN_PX,
  snapToExistingGeometry,
  traceSharedBorder,
  translatePolygon,
  type MapPoint,
} from '../../core/maps/geometry';
import {
  readStoredOverlayColorMode,
  writeStoredOverlayColorMode,
  type OverlayColorMode,
} from '../../core/maps/map-editor-preferences';
import { downloadBlob, mapDownloadFilename, rasterizeMapPng } from '../../core/maps/map-export';
import { parseMapSvg, serializeMapSvg, svgDownloadFilename } from '../../core/maps/map-svg';
import {
  mapFactionOptionLabel,
  mapFactionOptions,
  mapFactionOptionValue,
  parseMapFactionOptionValue,
  spawnIdentity,
  type MapFactionOption,
} from '../../core/maps/map-faction-options';
import {
  cloneGraph,
  createId,
  nextDisplayNumber,
  normalizeStructureCondition,
  territoryLabel,
  type MapAdjacency,
  type MapGraph,
  type MapTerritory,
} from '../../core/maps/map-graph.models';
import { CampaignMapViewComponent } from '../../shared/campaign-map-view/campaign-map-view.component';
import { IconComponent } from '../../shared/icon/icon.component';
import { MapSymbolComponent } from '../../shared/map-symbol/map-symbol.component';
import { SaveCampaignPresetDialogComponent } from '../../shared/save-campaign-preset-dialog/save-campaign-preset-dialog.component';
import { InstantDatePipe } from '../../shared/time/instant-date.pipe';

export type MapEditorTool = 'draw' | 'erase' | 'select' | 'connect';
export type { OverlayColorMode };

@Component({
  selector: 'app-map-editor-page',
  imports: [
    FormsModule,
    RouterLink,
    CampaignMapViewComponent,
    IconComponent,
    MapSymbolComponent,
    InstantDatePipe,
    SaveCampaignPresetDialogComponent,
  ],
  templateUrl: './map-editor.page.html',
  styleUrl: './map-editor.page.css',
})
export class MapEditorPage {
  private readonly campaignsApi = inject(CampaignService);
  private readonly overlay = inject(FormSubmitOverlayService);
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

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
  protected readonly selectedAdjacencyId = signal<string | null>(null);
  protected readonly connectPendingId = signal<string | null>(null);
  protected readonly colorMode = signal<OverlayColorMode>('manual');
  protected readonly lastSavedAtUtc = signal<string | null>(null);
  protected readonly saveStatus = signal<'success' | 'failure' | null>(null);
  protected readonly showOverlay = signal(true);
  protected readonly showConnections = signal(true);
  protected readonly editorFieldsCollapsed = signal(false);
  protected readonly territoryListCollapsed = signal(true);
  protected readonly mapImageRevision = signal(0);
  protected readonly drawingActive = signal(false);
  protected readonly movePlacement = signal<'valid' | 'invalid' | null>(null);
  protected readonly confirmingDownload = signal(false);
  protected readonly downloading = signal(false);
  protected readonly pendingDownload = signal<'map' | 'svg' | null>(null);
  protected readonly savePresetOpen = signal(false);
  private readonly svgFileInput = viewChild<ElementRef<HTMLInputElement>>('svgFile');

  protected readonly mapTools: MapEditorTool[] = ['draw', 'erase', 'select', 'connect'];
  protected readonly colorModes: OverlayColorMode[] = ['random', 'terrain', 'manual'];

  private revision = 0;
  private undoStack: MapGraph[] = [];
  private redoStack: MapGraph[] = [];
  private drawingRedo: MapPoint[] = [];
  private strokeStartCount = 0;
  private readonly campaignId = this.route.snapshot.paramMap.get('id');
  private moveBaseline: MapGraph | null = null;
  private moveDidMove = false;
  private savedSnapshot = signal('');
  private savedGraph: MapGraph = { territories: [], adjacencies: [] };
  private readonly mapView = viewChild(CampaignMapViewComponent);
  private readonly territoryList = viewChild<ElementRef<HTMLUListElement>>('territoryList');
  private readonly historyVersion = signal(0);

  protected readonly canManage = computed(() => this.campaign()?.canManage === true);
  protected readonly isAdministrator = computed(() => this.auth.currentUser()?.isAdministrator === true);
  protected readonly canUndo = computed(() => {
    this.historyVersion();
    return this.drawing().length > 0 || this.undoStack.length > 0;
  });
  protected readonly canRedoHistory = computed(() => {
    this.historyVersion();
    return this.drawingRedo.length > 0 || this.redoStack.length > 0;
  });
  protected readonly mapSrc = computed(() => {
    const campaign = this.campaign();
    return campaign?.hasMap ? this.campaignsApi.mapUrl(campaign.id, this.mapImageRevision()) : null;
  });
  protected readonly selected = computed(() => {
    const id = this.selectedId();
    return this.graph().territories.find((territory) => territory.id === id) ?? null;
  });
  protected readonly selectedId = computed(() => this.selectedIds().at(-1) ?? null);
  protected readonly selectedTerritories = computed(() => {
    const byId = new Map(this.graph().territories.map((territory) => [territory.id, territory]));
    return this.selectedIds()
      .map((id) => byId.get(id))
      .filter((territory): territory is MapTerritory => !!territory);
  });
  protected readonly selectedAdjacency = computed(() => {
    const id = this.selectedAdjacencyId();
    return this.graph().adjacencies.find((edge) => edge.id === id) ?? null;
  });
  protected readonly hoveredConnection = computed(() => {
    const id = this.hoveredAdjacencyId();
    return this.graph().adjacencies.find((edge) => edge.id === id) ?? null;
  });
  protected readonly inspected = computed(() => {
    const hoverId = this.hoveredTerritoryId();
    return this.graph().territories.find((territory) => territory.id === hoverId) ?? this.selected();
  });
  protected readonly adjacentTerritoryIds = computed(() => {
    const edge = this.selectedAdjacency() ?? this.hoveredConnection();
    if (edge) {
      return [edge.territoryAId, edge.territoryBId];
    }

    return adjacentTerritoryIds(this.graph().adjacencies, this.selectedIds());
  });
  protected readonly canConnectSelected = computed(() => {
    const ids = this.selectedIds();
    return (
      this.canManage() &&
      ids.length === 2 &&
      ids[0] !== ids[1] &&
      !findConnection(this.graph().adjacencies, ids[0], ids[1])
    );
  });
  protected readonly sortedTerritories = computed(() =>
    [...this.graph().territories].sort((left, right) => territoryLabel(left).localeCompare(territoryLabel(right))),
  );
  private readonly topSelectedTerritoryId = computed(() => {
    const selected = new Set(this.selectedIds());
    if (selected.size === 0) {
      return null;
    }

    return this.sortedTerritories().find((territory) => selected.has(territory.id))?.id ?? null;
  });
  protected readonly factionOptions = computed(() => {
    const campaign = this.campaign();
    return campaign ? mapFactionOptions(campaign) : [];
  });
  protected readonly catalogTerrains = computed(() =>
    [...(this.campaign()?.terrainTypes ?? [])].sort((left, right) => left.name.localeCompare(right.name)),
  );
  protected readonly catalogStructures = computed(() =>
    [...(this.campaign()?.structureTypes ?? [])].sort((left, right) => left.name.localeCompare(right.name)),
  );
  protected readonly placedItemTypes = computed(() =>
    [...(this.campaign()?.itemObjectiveTypes ?? [])]
      .filter((type) => type.placement === 'Placed')
      .sort((left, right) => left.name.localeCompare(right.name)),
  );
  protected readonly hasUnsavedEdits = computed(
    () => this.drawing().length > 0 || JSON.stringify(this.graph()) !== this.savedSnapshot(),
  );
  protected readonly editorHeading = computed(() => {
    if (this.selectedAdjacency()) {
      return 'Connection';
    }

    if (this.selectedTerritories().length > 1) {
      return 'Selected territories';
    }

    return 'Territory';
  });
  protected readonly editorFieldsDirty = computed(() => {
    if (this.selectedAdjacency()) {
      return this.isAdjacencyFieldDirty('a') || this.isAdjacencyFieldDirty('b');
    }

    return this.selectedIds().some((id) => this.isTerritoryDirty(id));
  });
  protected readonly territoryListDirty = computed(() =>
    this.graph().territories.some((territory) => this.isTerritoryDirty(territory.id)),
  );

  protected toggleEditorFields(): void {
    this.editorFieldsCollapsed.update((collapsed) => !collapsed);
  }

  protected toggleTerritoryList(): void {
    this.territoryListCollapsed.update((collapsed) => !collapsed);
  }

  protected setShowOverlay(visible: boolean): void {
    this.showOverlay.set(visible);
  }

  protected setShowConnections(visible: boolean): void {
    this.showConnections.set(visible);
  }

  protected isAdditiveClick(event: MouseEvent): boolean {
    return isAdditiveModifier(event);
  }

  protected isTerritoryFieldDirty(
    key:
      | 'name'
      | 'description'
      | 'terrainTypeId'
      | 'structureTypeId'
      | 'structureCondition'
      | 'ownerFactionId'
      | 'ownerSubfaction'
      | 'spawnFactionId'
      | 'spawnSubfaction'
      | 'overlayColor',
  ): boolean {
    const current = this.selected();
    if (!current) {
      return false;
    }

    const saved = this.savedGraph.territories.find((item) => item.id === current.id);
    if (!saved) {
      return true;
    }

    return (current[key] ?? '') !== (saved[key] ?? '');
  }

  protected isItemPlacementDirty(typeId: string): boolean {
    const selectedId = this.selected()?.id;
    if (!selectedId) {
      return false;
    }

    const currentId = this.graph().itemObjectivePlacements?.find((item) => item.typeId === typeId)?.territoryId ?? null;
    const savedId =
      this.savedGraph.itemObjectivePlacements?.find((item) => item.typeId === typeId)?.territoryId ?? null;
    return (currentId === selectedId) !== (savedId === selectedId);
  }

  protected isTerritoryDirty(id: string): boolean {
    const current = this.graph().territories.find((item) => item.id === id);
    const saved = this.savedGraph.territories.find((item) => item.id === id);
    if (!current) {
      return false;
    }

    if (!saved) {
      return true;
    }

    if (JSON.stringify(current) !== JSON.stringify(saved)) {
      return true;
    }

    const currentPlacements = (this.graph().itemObjectivePlacements ?? []).filter((item) => item.territoryId === id);
    const savedPlacements = (this.savedGraph.itemObjectivePlacements ?? []).filter((item) => item.territoryId === id);
    return JSON.stringify(currentPlacements) !== JSON.stringify(savedPlacements);
  }

  protected isAdjacencyFieldDirty(end: 'a' | 'b'): boolean {
    const current = this.selectedAdjacency();
    if (!current) {
      return false;
    }

    const saved = this.savedGraph.adjacencies.find((item) => item.id === current.id);
    if (!saved) {
      return true;
    }

    return end === 'a' ? current.territoryAId !== saved.territoryAId : current.territoryBId !== saved.territoryBId;
  }

  constructor() {
    afterRenderEffect(() => {
      const id = this.topSelectedTerritoryId();
      if (this.territoryListCollapsed() || !id) {
        return;
      }

      const buttons = this.territoryList()?.nativeElement.querySelectorAll<HTMLButtonElement>('[data-territory-id]');
      const target = [...(buttons ?? [])].find((button) => button.dataset['territoryId'] === id);
      target?.scrollIntoView({ block: 'nearest', inline: 'nearest' });
    });

    if (this.campaignId) {
      const stored = readStoredOverlayColorMode(this.campaignId);
      if (stored) {
        this.colorMode.set(stored);
      }

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
        return 'Connect';
    }
  }

  protected onToolChange(tool: MapEditorTool): void {
    this.tool.set(tool);
    this.connectPendingId.set(null);
    if (tool !== 'select' && tool !== 'erase') {
      this.selectedAdjacencyId.set(null);
      this.hoveredAdjacencyId.set(null);
    }
    if (tool !== 'draw') {
      this.drawing.set([]);
      this.drawingActive.set(false);
    }
    if (tool === 'draw') {
      this.hoveredTerritoryId.set(null);
    }
    if (tool === 'connect' && this.canConnectSelected()) {
      this.connectSelectedTerritories();
    }
  }

  protected colorModeLabel(mode: OverlayColorMode): string {
    switch (mode) {
      case 'random':
        return 'Random Colors';
      case 'terrain':
        return 'Color By Terrain';
      case 'manual':
        return 'Manual Colors';
    }
  }

  protected onMapHover(point: MapPoint): void {
    if (this.tool() === 'draw' && this.canManage()) {
      this.snapTarget.set(
        snapToExistingGeometry(point, this.snapVertices(), this.polygons(), this.interactionDistance(SNAP_SCREEN_PX)),
      );
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

    const preview = moveSelection(
      this.moveBaseline,
      ids,
      event.current.x - event.origin.x,
      event.current.y - event.origin.y,
    );
    this.moveDidMove = Math.hypot(event.current.x - event.origin.x, event.current.y - event.origin.y) >= MIN_DRAW_STEP;
    this.movePlacement.set(preview.valid ? 'valid' : 'invalid');
    this.graph.set(preview.graph);
  }

  protected onTerritoryMoveEnd(): void {
    if (this.moveBaseline) {
      if (this.moveDidMove && this.movePlacement() === 'valid') {
        this.pushGraphUndo(this.moveBaseline);
      } else {
        this.graph.set(this.moveBaseline);
      }
    }

    this.moveBaseline = null;
    this.moveDidMove = false;
    this.movePlacement.set(null);
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
      ...graph,
      territories: graph.territories.filter((territory) => !remove.has(territory.id)),
      adjacencies: graph.adjacencies.filter((edge) => !remove.has(edge.territoryAId) && !remove.has(edge.territoryBId)),
      itemObjectivePlacements: (graph.itemObjectivePlacements ?? []).filter((item) => !remove.has(item.territoryId)),
    }));
    this.selectedIds.set([]);
  }

  protected deleteLabel(): string {
    return this.selectedIds().length > 1 ? 'Delete territories' : 'Delete territory';
  }

  protected onBackground(): void {
    this.selectedIds.set([]);
    this.selectedAdjacencyId.set(null);
    this.connectPendingId.set(null);
  }

  protected onTerritorySelect(event: { id: string; additive: boolean } | string): void {
    const id = typeof event === 'string' ? event : event.id;
    const additive = typeof event === 'string' ? false : event.additive;
    this.selectedAdjacencyId.set(null);
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

  protected onTerritoryMarquee(event: { ids: string[]; additive: boolean }): void {
    this.selectedAdjacencyId.set(null);
    this.connectPendingId.set(null);
    if (event.additive) {
      this.selectedIds.update((current) => [...new Set([...current, ...event.ids])]);
      return;
    }

    this.selectedIds.set(event.ids);
  }

  protected onAdjacencySelect(id: string): void {
    if (this.tool() !== 'select' && this.tool() !== 'erase') {
      return;
    }

    if (this.tool() === 'erase' && this.canManage()) {
      this.deleteAdjacency(id);
      return;
    }

    const edge = this.graph().adjacencies.find((item) => item.id === id);
    if (!edge) {
      return;
    }

    this.selectedAdjacencyId.set(id);
    this.selectedIds.set([]);
    this.connectPendingId.set(null);
  }

  protected connectSelectedTerritories(): void {
    if (!this.canManage()) {
      return;
    }

    const ids = this.selectedIds();
    if (ids.length !== 2 || ids[0] === ids[1]) {
      this.revealErrors(['Select two territories to create a connection.']);
      return;
    }

    this.addConnection(ids[0], ids[1]);
  }

  protected deleteSelectedAdjacency(): void {
    const id = this.selectedAdjacencyId();
    if (id) {
      this.deleteAdjacency(id);
    }
  }

  protected setAdjacencyEnd(end: 'a' | 'b', territoryId: string): void {
    const edge = this.selectedAdjacency();
    if (!edge || !this.canManage()) {
      return;
    }

    const otherId = end === 'a' ? edge.territoryBId : edge.territoryAId;
    if (!this.replaceConnection(edge.id, territoryId, otherId)) {
      return;
    }
  }

  protected onTerritoryHover(id: string | null): void {
    if (this.tool() === 'draw') {
      this.hoveredTerritoryId.set(null);
      return;
    }

    this.hoveredTerritoryId.set(id);
  }

  protected onAdjacencyHover(id: string | null): void {
    this.hoveredAdjacencyId.set(id);
    if (id) {
      this.hoveredTerritoryId.set(null);
    }
  }

  protected closePolygon(): void {
    if (!this.canManage()) {
      return;
    }

    const original = this.drawing();
    if (original.length < 2) {
      return;
    }

    if (this.tryCommitDrawing(original)) {
      return;
    }

    const enclosed =
      encloseAlongImageEdge(original, this.polygons()) ?? encloseAlongTouchedBorders(original, this.polygons());
    if (enclosed && this.tryCommitDrawing(enclosed)) {
      return;
    }

    this.revealCloseDrawingError(original);
  }

  protected cancelDrawing(): void {
    this.drawing.set([]);
    this.drawingActive.set(false);
    this.snapTarget.set(null);
    this.drawingRedo = [];
    this.markHistoryChanged();
  }

  protected undo(): void {
    if (!this.canManage()) {
      return;
    }

    const points = this.drawing();
    const last = points.at(-1);
    if (last) {
      this.drawingRedo.push(last);
      this.drawing.set(points.slice(0, -1));
      this.markHistoryChanged();
      return;
    }

    const previous = this.undoStack.pop();
    if (previous) {
      this.redoStack.push(cloneGraph(this.graph()));
      if (this.redoStack.length > 40) {
        this.redoStack.shift();
      }

      this.graph.set(previous);
      this.markHistoryChanged();
    }
  }

  protected redo(): void {
    if (!this.canManage()) {
      return;
    }

    const point = this.drawingRedo.pop();
    if (point) {
      this.drawing.update((points) => [...points, point]);
      this.markHistoryChanged();
      return;
    }

    const next = this.redoStack.pop();
    if (!next) {
      return;
    }

    this.undoStack.push(cloneGraph(this.graph()));
    if (this.undoStack.length > 40) {
      this.undoStack.shift();
    }

    this.graph.set(next);
    this.markHistoryChanged();
  }

  protected onConnectClick(): void {
    this.onToolChange('connect');
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
    this.selectedAdjacencyId.set(null);
    this.hoveredAdjacencyId.set(null);
  }

  protected setColorMode(mode: OverlayColorMode): void {
    if (!this.canManage()) {
      return;
    }

    this.applyColorMode(mode);
    if (mode === 'manual') {
      return;
    }

    this.pushUndo();
    this.graph.update((graph) => ({
      ...graph,
      territories: graph.territories.map((territory) => ({
        ...territory,
        overlayColor: this.overlayColorForNew(territory.terrainTypeId),
      })),
    }));
  }

  protected colorRandom(): void {
    this.setColorMode('random');
  }

  protected colorByTerrain(): void {
    this.setColorMode('terrain');
  }

  protected colorManual(): void {
    this.setColorMode('manual');
  }

  protected colorClear(): void {
    if (!this.canManage()) {
      return;
    }

    this.applyColorMode('manual');
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
    this.patchSelected((territory) => ({
      ...territory,
      terrainTypeId: value,
      overlayColor: this.colorMode() === 'terrain' ? this.overlayColorForNew(value) : territory.overlayColor,
    }));
  }

  protected setStructure(value: string): void {
    this.patchSelected((territory) => ({
      ...territory,
      structureTypeId: value || null,
      structureCondition: value ? territory.structureCondition : 'Operational',
    }));
  }

  protected setStructureCondition(value: string): void {
    this.patchSelected((territory) => ({
      ...territory,
      structureCondition: value === 'Pillaged' ? 'Pillaged' : 'Operational',
    }));
  }

  protected setOwner(value: string): void {
    if (this.selected()?.spawnFactionId) {
      return;
    }

    const parsed = parseMapFactionOptionValue(value);
    this.patchSelected((territory) => ({
      ...territory,
      ownerFactionId: parsed.factionId || null,
      ownerSubfaction: parsed.factionId ? parsed.subfaction : null,
    }));
  }

  protected setSpawn(value: string): void {
    const parsed = parseMapFactionOptionValue(value);
    this.patchSelected((territory) => ({
      ...territory,
      spawnFactionId: parsed.factionId || null,
      spawnSubfaction: parsed.factionId ? parsed.subfaction : null,
      ...(parsed.factionId ? { ownerFactionId: parsed.factionId, ownerSubfaction: parsed.subfaction } : {}),
    }));
  }

  protected setOverlayColor(value: string): void {
    this.applyColorMode('manual');
    this.patchSelected((territory) => ({ ...territory, overlayColor: value || null }));
  }

  protected itemPlacementTerritoryId(typeId: string): string | null {
    return this.graph().itemObjectivePlacements?.find((item) => item.typeId === typeId)?.territoryId ?? null;
  }

  protected isItemPlacedHere(typeId: string): boolean {
    const territoryId = this.selectedId();
    return !!territoryId && this.itemPlacementTerritoryId(typeId) === territoryId;
  }

  protected itemPlacementLabel(typeId: string): string {
    const territoryId = this.itemPlacementTerritoryId(typeId);
    const territory = this.graph().territories.find((item) => item.id === territoryId);
    return territory ? territoryLabel(territory) : 'Not placed';
  }

  protected setItemPlacedHere(typeId: string, placed: boolean): void {
    const territoryId = this.selectedId();
    if (!territoryId || !this.canManage()) {
      return;
    }

    this.pushUndo();
    this.graph.update((graph) => {
      const without = (graph.itemObjectivePlacements ?? []).filter((item) => item.typeId !== typeId);
      return {
        ...graph,
        itemObjectivePlacements: placed ? [...without, { typeId, territoryId }] : without,
      };
    });
  }

  protected factionName(id: string | null | undefined, subfaction?: string | null): string {
    return mapFactionOptionLabel(this.campaign()?.factions ?? [], id, subfaction);
  }

  protected ownerValue(territory: MapTerritory): string {
    return territory.ownerFactionId ? mapFactionOptionValue(territory.ownerFactionId, territory.ownerSubfaction) : '';
  }

  protected spawnValue(territory: MapTerritory): string {
    return territory.spawnFactionId ? mapFactionOptionValue(territory.spawnFactionId, territory.spawnSubfaction) : '';
  }

  protected ownerLocked(): boolean {
    return !!this.selected()?.spawnFactionId;
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

  protected spawnTaken(option: MapFactionOption): boolean {
    const selectedId = this.selectedId();
    return this.graph().territories.some(
      (territory) => territory.id !== selectedId && this.spawnValue(territory) === option.value,
    );
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

  protected labelForId(id: string): string {
    const territory = this.graph().territories.find((item) => item.id === id);
    return territory ? territoryLabel(territory) : 'Unknown territory';
  }

  @HostListener('document:pointerup')
  @HostListener('document:pointercancel')
  protected onPointerUp(): void {
    if (this.drawingActive()) {
      this.traceSharedBorderIfEligible();
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
    if (event.key === 'Enter' && this.drawing().length >= 2 && !typing) {
      event.preventDefault();
      this.closePolygon();
      return;
    }

    if (event.key === 'Escape') {
      this.cancelDrawing();
      this.connectPendingId.set(null);
      this.selectedAdjacencyId.set(null);
      return;
    }

    if ((event.key === 'Delete' || event.key === 'Backspace') && !typing && this.drawing().length === 0) {
      if (this.selectedAdjacencyId()) {
        event.preventDefault();
        this.deleteSelectedAdjacency();
        return;
      }

      if (this.selectedIds().length > 0) {
        event.preventDefault();
        this.deleteSelectedTerritories();
      }
      return;
    }

    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'z') {
      event.preventDefault();
      if (event.shiftKey) {
        this.redo();
      } else {
        this.undo();
      }

      return;
    }

    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'y') {
      event.preventDefault();
      this.redo();
    }
  }

  protected async save(): Promise<boolean> {
    const campaign = this.campaign();
    if (!campaign || !this.canManage()) {
      return false;
    }

    const names = this.graph()
      .territories.map((territory) => territory.name?.trim().toLowerCase())
      .filter((name): name is string => !!name);
    if (new Set(names).size !== names.length) {
      this.saveStatus.set('failure');
      this.revealErrors(['Territory names must be unique for the campaign.']);
      return false;
    }

    if (this.graph().territories.some((territory) => !territory.terrainTypeId)) {
      this.saveStatus.set('failure');
      this.revealErrors(['Every territory needs a terrain type.']);
      return false;
    }

    const spawnIds = this.graph()
      .territories.map((territory) => spawnIdentity(territory.spawnFactionId, territory.spawnSubfaction))
      .filter((id): id is string => !!id);
    if (new Set(spawnIds).size !== spawnIds.length) {
      this.saveStatus.set('failure');
      this.revealErrors(['Each faction or required subfaction can have only one spawn location.']);
      return false;
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
            structureCondition: territory.structureCondition,
            overlayColor: territory.overlayColor,
            ownerFactionId: territory.ownerFactionId,
            ownerSubfaction: territory.ownerSubfaction ?? null,
            spawnFactionId: territory.spawnFactionId,
            spawnSubfaction: territory.spawnSubfaction ?? null,
          })),
          adjacencies: this.graph().adjacencies.map((edge) => ({
            id: edge.id,
            territoryAId: edge.territoryAId,
            territoryBId: edge.territoryBId,
            origin: edge.origin,
            markerX: edge.marker.x,
            markerY: edge.marker.y,
          })),
          itemObjectivePlacements: this.graph().itemObjectivePlacements ?? [],
        }),
      );
      this.revision = saved.revision;
      const graph = fromApi(saved);
      this.graph.set(graph);
      this.rememberSaved(graph);
      this.campaign.update((current) => (current ? { ...current, revision: saved.revision } : current));
      this.lastSavedAtUtc.set(new Date().toISOString());
      this.successMessage.set(FORM_SAVE_SUCCESS_MESSAGE);
      this.saveStatus.set('success');
      return true;
    } catch (error: unknown) {
      this.saveStatus.set('failure');
      this.revealErrors(readApiErrorMessages(error, 'Unable to save the map.'));
      return false;
    } finally {
      this.saving.set(false);
    }
  }

  protected async requestDownload(kind: 'map' | 'svg' = 'map'): Promise<void> {
    if (kind === 'map' && !this.mapSrc()) {
      return;
    }

    if (this.hasUnsavedEdits()) {
      this.pendingDownload.set(kind);
      this.confirmingDownload.set(true);
      return;
    }

    await this.performDownload(kind, this.savedGraph);
  }

  protected cancelDownloadPrompt(): void {
    this.confirmingDownload.set(false);
    this.pendingDownload.set(null);
  }

  protected async downloadLastSaved(): Promise<void> {
    const kind = this.pendingDownload() ?? 'map';
    this.confirmingDownload.set(false);
    this.pendingDownload.set(null);
    await this.performDownload(kind, this.savedGraph);
  }

  protected async saveAndDownload(): Promise<void> {
    const kind = this.pendingDownload() ?? 'map';
    this.confirmingDownload.set(false);
    this.pendingDownload.set(null);
    const saved = await this.save();
    if (saved) {
      await this.performDownload(kind, this.savedGraph);
    }
  }

  protected requestUploadSvg(): void {
    this.svgFileInput()?.nativeElement.click();
  }

  protected openSavePresetDialog(): void {
    if (!this.isAdministrator() || !this.campaign()) {
      return;
    }

    this.savePresetOpen.set(true);
  }

  protected closeSavePresetDialog(): void {
    this.savePresetOpen.set(false);
  }

  protected async confirmSavePreset(name: string): Promise<void> {
    const campaign = this.campaign();
    if (!campaign || !this.isAdministrator()) {
      return;
    }

    if (name.length < 3) {
      this.revealErrors(['Preset name must be at least 3 characters.']);
      return;
    }

    this.saving.set(true);
    this.errorMessages.set([]);
    this.successMessage.set(null);
    try {
      const saved = await this.overlay.run(async () => {
        if (this.hasUnsavedEdits()) {
          const graphSaved = await this.save();
          if (!graphSaved) {
            return false;
          }
        }

        await this.campaignsApi.saveAsPreset(campaign.id, name);
        return true;
      });
      if (!saved) {
        return;
      }

      this.closeSavePresetDialog();
      this.successMessage.set(FORM_SAVE_SUCCESS_MESSAGE);
    } catch (error: unknown) {
      this.revealErrors(readApiErrorMessages(error, 'Unable to save the campaign preset.'));
    } finally {
      this.saving.set(false);
    }
  }

  protected async onSvgFile(event: Event): Promise<void> {
    const input = event.target;
    if (!(input instanceof HTMLInputElement) || !input.files?.[0]) {
      return;
    }

    const campaign = this.campaign();
    const defaultTerrainTypeId = campaign?.terrainTypes[0]?.id;
    if (!campaign?.canManage || !defaultTerrainTypeId) {
      this.revealErrors(['Upload a campaign with terrain types before importing SVG overlay data.']);
      input.value = '';
      return;
    }

    try {
      const text = await input.files[0].text();
      const parsed = parseMapSvg(text, { defaultTerrainTypeId });
      if (parsed.graph.territories.length === 0) {
        this.revealErrors(
          parsed.errors.length > 0 ? parsed.errors : ['The SVG file did not contain any valid territories.'],
        );
        return;
      }

      this.pushUndo();
      this.graph.set({
        ...parsed.graph,
        territories: parsed.graph.territories.map((territory) => ({
          ...territory,
          overlayColor:
            this.colorMode() === 'manual' ? territory.overlayColor : this.overlayColorForNew(territory.terrainTypeId),
        })),
      });
      this.selectedIds.set([]);
      this.successMessage.set(
        parsed.errors.length > 0
          ? `Imported ${parsed.graph.territories.length} territories. ${parsed.errors[0]}`
          : `Imported ${parsed.graph.territories.length} territories from the SVG file.`,
      );
      this.errorMessages.set(parsed.errors.length > 0 ? parsed.errors : []);
    } catch {
      this.revealErrors(['Unable to read the SVG file.']);
    } finally {
      input.value = '';
    }
  }

  private async load(id: string): Promise<void> {
    try {
      const [campaign, graph] = await Promise.all([this.campaignsApi.get(id), this.campaignsApi.getMapGraph(id)]);
      if (campaign.status !== 'Scheduled') {
        await this.router.navigate(['/campaigns', id, 'play']);
        return;
      }

      this.campaign.set(campaign);
      this.mapImageRevision.set(campaign.revision);
      this.revision = graph.revision;
      const loaded = fromApi(graph);
      this.graph.set(loaded);
      this.rememberSaved(loaded);
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
    const snapDistance = this.interactionDistance(SNAP_SCREEN_PX);
    const snap = snapToExistingGeometry(point, this.snapVertices(), this.polygons(), snapDistance) ?? clampPoint(point);
    const current = this.drawing();
    const first = current.at(0);
    if (
      current.length >= 3 &&
      first &&
      distanceClose(snap, first, this.interactionDistance(CLOSE_POLYGON_SCREEN_PX)) &&
      options?.force
    ) {
      this.closePolygon();
      return;
    }

    const last = current.at(-1);
    if (last && !options?.force && !farEnough(last, snap, this.interactionDistance(MIN_DRAW_SCREEN_PX))) {
      return;
    }

    if (last && distanceClose(last, snap, this.interactionDistance(MIN_DRAW_SCREEN_PX))) {
      return;
    }

    this.drawingRedo = [];
    this.drawing.set([...current, snap]);
    this.markHistoryChanged();
  }

  private eraseAt(point: MapPoint): void {
    if (this.drawing().length > 0) {
      this.undo();
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
      ...graph,
      territories: graph.territories.filter((territory) => territory.id !== id),
      adjacencies: graph.adjacencies.filter((edge) => edge.territoryAId !== id && edge.territoryBId !== id),
      itemObjectivePlacements: (graph.itemObjectivePlacements ?? []).filter((item) => item.territoryId !== id),
    }));
    if (this.selectedIds().includes(id)) {
      this.selectedIds.update((current) => current.filter((item) => item !== id));
    }
    const selectedEdge = this.selectedAdjacency();
    if (selectedEdge && (selectedEdge.territoryAId === id || selectedEdge.territoryBId === id)) {
      this.selectedAdjacencyId.set(null);
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

    this.addConnection(pending, id);
  }

  private addConnection(leftId: string, rightId: string): boolean {
    if (leftId === rightId) {
      this.revealErrors(['A connection is always between two different territories.']);
      return false;
    }

    if (findConnection(this.graph().adjacencies, leftId, rightId)) {
      this.revealErrors(['Those territories already have a connection.']);
      return false;
    }

    const left = this.graph().territories.find((territory) => territory.id === leftId);
    const right = this.graph().territories.find((territory) => territory.id === rightId);
    if (!left || !right) {
      return false;
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
    this.selectedAdjacencyId.set(edge.id);
    this.selectedIds.set([]);
    this.connectPendingId.set(null);
    return true;
  }

  private replaceConnection(edgeId: string, leftId: string, rightId: string): boolean {
    if (leftId === rightId) {
      this.revealErrors(['A connection is always between two different territories.']);
      return false;
    }

    const existing = findConnection(this.graph().adjacencies, leftId, rightId);
    if (existing && existing.id !== edgeId) {
      this.revealErrors(['Those territories already have a connection.']);
      return false;
    }

    const left = this.graph().territories.find((territory) => territory.id === leftId);
    const right = this.graph().territories.find((territory) => territory.id === rightId);
    if (!left || !right) {
      return false;
    }

    this.pushUndo();
    const [a, b] = orderedPair(left.id, right.id);
    this.graph.update((graph) => ({
      ...graph,
      adjacencies: graph.adjacencies.map((item) =>
        item.id === edgeId
          ? { ...item, territoryAId: a, territoryBId: b, origin: 'Manual', marker: adjacencyMarker(left, right) }
          : item,
      ),
    }));
    return true;
  }

  private deleteAdjacency(id: string): void {
    this.pushUndo();
    this.graph.update((graph) => ({
      ...graph,
      adjacencies: graph.adjacencies.filter((edge) => edge.id !== id),
    }));
    if (this.selectedAdjacencyId() === id) {
      this.selectedAdjacencyId.set(null);
    }
    if (this.hoveredAdjacencyId() === id) {
      this.hoveredAdjacencyId.set(null);
    }
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

  private tryCommitDrawing(points: readonly MapPoint[]): boolean {
    if (points.length < 3 || !isValidTerritoryPolygon(points)) {
      return false;
    }

    if (this.polygons().some((polygon) => interiorsOverlap(points, polygon))) {
      return false;
    }

    const defaultTerrain = this.catalogTerrains().at(0);
    if (!defaultTerrain) {
      this.revealErrors(['Add at least one terrain type in campaign setup before drawing territories.']);
      return true;
    }

    this.pushUndo();
    const territory: MapTerritory = {
      id: createId(),
      displayNumber: nextDisplayNumber(this.graph().territories),
      name: null,
      description: null,
      polygon: points.map((point) => ({ ...point })),
      terrainTypeId: defaultTerrain.id,
      structureTypeId: null,
      structureCondition: 'Operational',
      overlayColor: this.overlayColorForNew(defaultTerrain.id),
      ownerFactionId: null,
      ownerSubfaction: null,
      spawnFactionId: null,
      spawnSubfaction: null,
    };
    this.graph.update((graph) => ({ ...graph, territories: [...graph.territories, territory] }));
    this.drawing.set([]);
    this.drawingActive.set(false);
    this.selectedIds.set([territory.id]);
    this.tool.set('select');
    this.successMessage.set(null);
    this.errorMessages.set([]);
    return true;
  }

  private revealCloseDrawingError(points: readonly MapPoint[]): void {
    if (points.length < 3) {
      this.revealErrors(['That shape could not be closed along a border or the map edge.']);
      return;
    }

    if (!isValidTerritoryPolygon(points)) {
      this.revealErrors(['That shape must stay on the map, stay closed, and not cross itself.']);
      return;
    }

    if (this.polygons().some((polygon) => interiorsOverlap(points, polygon))) {
      this.revealErrors(['Territories cannot overlap. They may share a border.']);
      return;
    }

    this.revealErrors(['That shape could not be closed along a border or the map edge.']);
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
    this.pushGraphUndo(cloneGraph(this.graph()));
  }

  private pushGraphUndo(previous: MapGraph): void {
    this.undoStack.push(previous);
    if (this.undoStack.length > 40) {
      this.undoStack.shift();
    }

    this.redoStack = [];
    this.drawingRedo = [];
    this.markHistoryChanged();
  }

  private markHistoryChanged(): void {
    this.historyVersion.update((value) => value + 1);
  }

  private rememberSaved(graph: MapGraph): void {
    this.savedGraph = cloneGraph(graph);
    this.savedSnapshot.set(JSON.stringify(graph));
  }

  private interactionDistance(pixels: number): number {
    return this.mapView()?.screenToMap(pixels) ?? pixels / 1000;
  }

  private async performDownload(kind: 'map' | 'svg', graph: MapGraph): Promise<void> {
    if (kind === 'svg') {
      this.downloadSvg(graph);
      return;
    }

    await this.downloadGraph(graph);
  }

  private downloadSvg(graph: MapGraph): void {
    const campaign = this.campaign();
    if (!campaign) {
      return;
    }

    const blob = new Blob([serializeMapSvg(graph)], { type: 'image/svg+xml' });
    downloadBlob(blob, svgDownloadFilename(campaign.name));
  }

  private async downloadGraph(graph: MapGraph): Promise<void> {
    const campaign = this.campaign();
    const imageUrl = this.mapSrc();
    if (!campaign || !imageUrl) {
      return;
    }

    this.downloading.set(true);
    this.errorMessages.set([]);
    try {
      const blob = await rasterizeMapPng(imageUrl, graph.territories, {
        factions: campaign.factions,
        structures: campaign.structureTypes,
        structureImageUrl: this.structureImageUrl,
        flagImageUrl: this.flagImageUrl,
      });
      downloadBlob(blob, mapDownloadFilename(campaign.name));
    } catch (error: unknown) {
      this.revealErrors(readApiErrorMessages(error, 'Unable to download the map.'));
    } finally {
      this.downloading.set(false);
    }
  }

  protected discardUnsavedChanges(): void {
    if (!this.canManage() || !this.hasUnsavedEdits()) {
      return;
    }

    this.graph.set(cloneGraph(this.savedGraph));
    this.drawing.set([]);
    this.drawingActive.set(false);
    this.snapTarget.set(null);
    this.undoStack = [];
    this.redoStack = [];
    this.drawingRedo = [];
    this.markHistoryChanged();
    const remaining = new Set(this.savedGraph.territories.map((territory) => territory.id));
    this.selectedIds.update((current) => current.filter((id) => remaining.has(id)));
    const selectedEdgeId = this.selectedAdjacencyId();
    if (selectedEdgeId && !this.savedGraph.adjacencies.some((edge) => edge.id === selectedEdgeId)) {
      this.selectedAdjacencyId.set(null);
    }

    this.successMessage.set(null);
    this.errorMessages.set([]);
    this.saveStatus.set(null);
  }

  private applyColorMode(mode: OverlayColorMode): void {
    this.colorMode.set(mode);
    if (this.campaignId) {
      writeStoredOverlayColorMode(this.campaignId, mode);
    }
  }

  private overlayColorForNew(terrainTypeId: string): string | null {
    switch (this.colorMode()) {
      case 'random':
        return randomOverlayColor();
      case 'terrain':
        return terrainTypeById(this.campaign(), terrainTypeId)?.color ?? null;
      case 'manual':
        return null;
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
      structureCondition: normalizeStructureCondition(territory.structureTypeId, territory.structureCondition),
      overlayColor: territory.overlayColor,
      ownerFactionId: territory.ownerFactionId,
      ownerSubfaction: territory.ownerSubfaction ?? null,
      spawnFactionId: territory.spawnFactionId,
      spawnSubfaction: territory.spawnSubfaction ?? null,
    })),
    adjacencies: detail.adjacencies.map((edge) => ({
      id: edge.id,
      territoryAId: edge.territoryAId,
      territoryBId: edge.territoryBId,
      origin: edge.origin === 'Generated' ? 'Generated' : 'Manual',
      marker: { x: edge.markerX, y: edge.markerY },
    })),
    itemObjectivePlacements: (detail.itemObjectivePlacements ?? []).map((item) => ({
      typeId: item.typeId,
      territoryId: item.territoryId,
    })),
  };
}

function moveSelection(
  baseline: MapGraph,
  ids: readonly string[],
  dx: number,
  dy: number,
): { graph: MapGraph; valid: boolean } {
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
  let valid = true;
  for (const territory of moved) {
    if (!isValidTerritoryPolygon(territory.polygon)) {
      valid = false;
      break;
    }

    if (others.some((other) => interiorsOverlap(territory.polygon, other.polygon))) {
      valid = false;
      break;
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

  return { graph: { territories, adjacencies, itemObjectivePlacements: baseline.itemObjectivePlacements }, valid };
}

function distanceClose(left: MapPoint, right: MapPoint, threshold: number): boolean {
  const dx = left.x - right.x;
  const dy = left.y - right.y;
  return dx * dx + dy * dy <= threshold * threshold;
}

function farEnough(left: MapPoint, right: MapPoint, threshold: number): boolean {
  const dx = left.x - right.x;
  const dy = left.y - right.y;
  return dx * dx + dy * dy >= threshold * threshold;
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
