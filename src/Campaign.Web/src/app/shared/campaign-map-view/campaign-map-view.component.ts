import {
  Component,
  computed,
  DestroyRef,
  inject,
  input,
  output,
  signal,
  viewChild,
  type ElementRef,
} from '@angular/core';

import type { CampaignFaction, CampaignStructureType } from '../../core/campaigns/campaign.models';
import { adjacencyArrowEndpoints, adjacencyArrowGeometry } from '../../core/maps/adjacency';
import {
  ARROW_HEAD_SCREEN_PX,
  ARROW_HIT_SCREEN_PX,
  ARROW_OVERHANG_LINE_SCREEN_PX,
  centroid,
  clampPoint,
  containsStrict,
  DRAWING_STROKE_SCREEN_PX,
  fitSquareInPolygon,
  MARKER_MAX_PX,
  MAX_ZOOM,
  MIN_ZOOM,
  normalizedFromPixels,
  pointOnPolygonBoundary,
  polygonIntersectsRect,
  polygonPointsAttribute,
  SNAP_RING_SCREEN_PX,
  STROKE_ADJACENT_SCREEN_PX,
  STROKE_SCREEN_PX,
  STROKE_SELECTED_SCREEN_PX,
  VERTEX_SCREEN_PX,
  ZOOM_STEP,
} from '../../core/maps/geometry';
import type { FittedSquare, MapPoint } from '../../core/maps/geometry';
import type { MapAdjacency, MapTerritory } from '../../core/maps/map-graph.models';
import { MapSymbolComponent } from '../map-symbol/map-symbol.component';

@Component({
  selector: 'app-campaign-map-view',
  imports: [MapSymbolComponent],
  templateUrl: './campaign-map-view.component.html',
  styleUrl: './campaign-map-view.component.css',
})
export class CampaignMapViewComponent {
  readonly imageUrl = input.required<string>();
  readonly territories = input<readonly MapTerritory[]>([]);
  readonly adjacencies = input<readonly MapAdjacency[]>([]);
  readonly drawingPoints = input<readonly MapPoint[]>([]);
  readonly snapTarget = input<MapPoint | null>(null);
  readonly selectedTerritoryIds = input<readonly string[]>([]);
  readonly hoveredTerritoryId = input<string | null>(null);
  readonly hoveredAdjacencyId = input<string | null>(null);
  readonly selectedAdjacencyId = input<string | null>(null);
  readonly adjacentTerritoryIds = input<readonly string[]>([]);
  readonly showAdjacencies = input(false);
  readonly adjacenciesInteractive = input(false);
  readonly interactive = input(true);
  readonly moveTerritories = input(false);
  readonly marqueeSelect = input(false);
  readonly factions = input<readonly CampaignFaction[]>([]);
  readonly structures = input<readonly CampaignStructureType[]>([]);
  readonly structureImageUrl = input<(structureTypeId: string) => string | null>(() => null);
  readonly flagImageUrl = input<(factionId: string) => string | null>(() => null);

  readonly mapPoint = output<MapPoint>();
  readonly mapHover = output<MapPoint>();
  readonly territoryHover = output<string | null>();
  readonly adjacencyHover = output<string | null>();
  readonly territorySelect = output<{ id: string; additive: boolean }>();
  readonly adjacencySelect = output<string>();
  readonly backgroundSelect = output<void>();
  readonly territoryMarquee = output<{ ids: string[]; additive: boolean }>();
  readonly territoryMove = output<{ origin: MapPoint; current: MapPoint }>();
  readonly territoryMoveEnd = output<void>();

  private readonly destroyRef = inject(DestroyRef);
  private readonly viewport = viewChild<ElementRef<HTMLElement>>('viewport');
  protected readonly zoom = signal(1);
  private readonly fitToPanel = signal(true);
  private readonly panX = signal(0);
  private readonly panY = signal(0);
  protected readonly imageSize = signal({ width: 1, height: 1 });
  private readonly viewportSize = signal({ width: 1, height: 1 });
  private readonly spaceHeld = signal(false);
  protected readonly panning = signal(false);
  protected readonly marqueeBox = signal<{ left: number; top: number; width: number; height: number } | null>(null);
  private movingTerritory = false;
  private moveOrigin: MapPoint | null = null;
  private hasFittedImage = false;
  private marqueeOrigin: { clientX: number; clientY: number; point: MapPoint; additive: boolean } | null = null;
  private panOrigin = { x: 0, y: 0, panX: 0, panY: 0 };
  private resizeObserver: ResizeObserver | null = null;
  private observedDestroy = false;

  protected readonly overlayTerritories = computed(() => {
    const image = this.imageSize();
    const scale = Math.max(this.currentScale(), Number.EPSILON);
    const maxWidth = MARKER_MAX_PX / (image.width * scale);
    const maxHeight = MARKER_MAX_PX / (image.height * scale);
    return this.territories().map((territory) => {
      const center = centroid(territory.polygon);
      const structure = this.structures().find((item) => item.id === territory.structureTypeId) ?? null;
      const owner = this.factions().find((faction) => faction.id === territory.ownerFactionId) ?? null;
      const selected = this.isSelected(territory.id);
      const structureFit = structure ? fitSquareInPolygon(territory.polygon, center, maxWidth, maxHeight) : null;
      const flagPreferred = structureFit ? { x: structureFit.x + structureFit.width * 0.7, y: structureFit.y } : center;
      const flagFit = owner
        ? fitSquareInPolygon(territory.polygon, flagPreferred, maxWidth, maxHeight, structureFit)
        : null;
      return {
        territory,
        points: polygonPointsAttribute(territory.polygon),
        center,
        structureFit,
        flag:
          owner && flagFit
            ? {
                ...flagFit,
                color: owner.color,
                image: owner.hasFlagImage ? this.flagImageUrl()(owner.id) : null,
              }
            : null,
        structure,
        structureImage: structure?.hasImage ? this.structureImageUrl()(structure.id) : null,
        selected,
        adjacent: !selected && this.isAdjacent(territory.id),
        strokeWidth: this.screenToMap(
          selected
            ? STROKE_SELECTED_SCREEN_PX
            : this.isAdjacent(territory.id)
              ? STROKE_ADJACENT_SCREEN_PX
              : STROKE_SCREEN_PX,
        ),
        glowColor: territory.overlayColor ?? 'var(--color-glow)',
      };
    });
  });

  protected readonly overlayAdjacencies = computed(() => {
    if (!this.showAdjacencies()) {
      return [];
    }

    const byId = new Map(this.territories().map((territory) => [territory.id, territory]));
    return this.adjacencies().flatMap((edge) => {
      const left = byId.get(edge.territoryAId);
      const right = byId.get(edge.territoryBId);
      if (!left || !right) {
        return [];
      }

      const inset = this.screenToMap(ARROW_HEAD_SCREEN_PX + ARROW_OVERHANG_LINE_SCREEN_PX);
      const ends = adjacencyArrowEndpoints(left.polygon, right.polygon, inset);
      const highlighted = edge.id === this.hoveredAdjacencyId() || edge.id === this.selectedAdjacencyId();
      return [
        {
          edge,
          geometry: adjacencyArrowGeometry(ends.from, ends.to, this.screenToMap(ARROW_HEAD_SCREEN_PX)),
          cx: (ends.from.x + ends.to.x) / 2,
          cy: (ends.from.y + ends.to.y) / 2,
          highlighted,
          hitWidth: Math.min(this.screenToMap(ARROW_HIT_SCREEN_PX), 0.05),
          strokeWidth: Math.min(this.screenToMap(STROKE_SELECTED_SCREEN_PX), 0.008),
        },
      ];
    });
  });

  protected readonly drawingPath = computed(() => {
    const points = this.drawingPoints();
    if (points.length === 0) {
      return '';
    }

    return points.map((point, index) => `${index === 0 ? 'M' : 'L'} ${point.x} ${point.y}`).join(' ');
  });

  protected readonly canvasTransform = computed(() => {
    const scale = this.currentScale();
    return `translate(${this.panX()}px, ${this.panY()}px) scale(${scale})`;
  });

  protected readonly zoomPercent = computed(() => Math.round(this.currentScale() * 100));

  protected adjacencyVisualTransform(item: { highlighted: boolean; cx: number; cy: number }): string | null {
    if (!item.highlighted) {
      return null;
    }

    return `translate(${item.cx} ${item.cy}) scale(1.5) translate(${-item.cx} ${-item.cy})`;
  }

  protected markerSize(fit: FittedSquare): { width: number; height: number } {
    const image = this.imageSize();
    return { width: fit.width * image.width, height: fit.height * image.height };
  }

  screenToMap(pixels: number): number {
    const width = this.imageSize().width;
    if (width <= 1) {
      return normalizedFromPixels(pixels, 1000, 1);
    }

    return normalizedFromPixels(pixels, width, this.currentScale());
  }

  protected drawingStrokeWidth(): number {
    return this.screenToMap(DRAWING_STROKE_SCREEN_PX);
  }

  protected vertexRadius(): number {
    return this.screenToMap(VERTEX_SCREEN_PX);
  }

  protected snapRadius(): number {
    return this.screenToMap(SNAP_RING_SCREEN_PX);
  }

  protected onImageLoad(event: Event): void {
    const image = event.target as HTMLImageElement;
    this.imageSize.set({ width: image.naturalWidth || 1, height: image.naturalHeight || 1 });
    this.observeViewport();
    if (!this.hasFittedImage) {
      this.hasFittedImage = true;
      this.zoomToFit();
    }
  }

  protected onWheel(event: WheelEvent): void {
    event.preventDefault();
    const factor = event.deltaY < 0 ? 1 : -1;
    this.nudgeZoom(factor, event.clientX, event.clientY);
  }

  protected onPointerMove(event: PointerEvent): void {
    if (this.applyPan(event)) {
      return;
    }

    if (this.marqueeOrigin) {
      this.updateMarquee(event);
      return;
    }

    const point = this.pointFromEvent(event);
    if (!point) {
      return;
    }

    if (this.movingTerritory && this.moveOrigin) {
      this.territoryMove.emit({ origin: this.moveOrigin, current: point });
      return;
    }

    this.mapHover.emit(point);
  }

  protected onViewportPointerDown(event: PointerEvent): void {
    if (this.shouldAlwaysPan(event)) {
      this.beginPan(event);
      return;
    }

    if (event.button === 0 && this.marqueeSelect() && event.target === event.currentTarget) {
      this.beginMarquee(event);
      return;
    }

    if (event.target === event.currentTarget && event.button === 0) {
      this.backgroundSelect.emit();
    }
  }

  protected onPointerDown(event: PointerEvent): void {
    if (this.shouldAlwaysPan(event)) {
      this.beginPan(event);
      event.stopPropagation();
      return;
    }

    if (!this.interactive()) {
      return;
    }

    if (event.button !== 0) {
      return;
    }

    const target = event.target as SVGElement | HTMLElement;
    const kind = target.dataset['kind'];
    const id = target.dataset['id'];
    if (kind === 'adjacency' && id && this.adjacenciesInteractive()) {
      this.adjacencySelect.emit(id);
      event.preventDefault();
      return;
    }

    const point = this.pointFromEvent(event);
    if (!point) {
      return;
    }

    const territoryId = kind === 'territory' && id ? id : this.territoryIdAt(point);
    if (territoryId) {
      const additive = event.ctrlKey || event.metaKey;
      this.territorySelect.emit({ id: territoryId, additive });
      this.mapPoint.emit(point);
      if (!additive && this.moveTerritories()) {
        this.movingTerritory = true;
        this.moveOrigin = point;
        (event.currentTarget as HTMLElement | SVGElement).setPointerCapture(event.pointerId);
      }

      return;
    }

    if (this.marqueeSelect()) {
      this.beginMarquee(event);
      return;
    }

    this.backgroundSelect.emit();
    this.mapPoint.emit(point);
  }

  protected onPointerUp(event?: PointerEvent): void {
    if (this.marqueeOrigin) {
      this.finishMarquee(event);
    }

    if (this.movingTerritory) {
      this.movingTerritory = false;
      this.moveOrigin = null;
      this.territoryMoveEnd.emit();
    }

    this.panning.set(false);
  }

  protected onContextMenu(event: MouseEvent): void {
    event.preventDefault();
  }

  protected onTerritoryEnter(id: string): void {
    this.territoryHover.emit(id);
  }

  protected onTerritoryLeave(id: string): void {
    if (this.hoveredTerritoryId() === id) {
      this.territoryHover.emit(null);
    }
  }

  protected onAdjacencyEnter(id: string): void {
    if (this.adjacenciesInteractive()) {
      this.adjacencyHover.emit(id);
    }
  }

  protected onAdjacencyLeave(id: string): void {
    if (this.hoveredAdjacencyId() === id) {
      this.adjacencyHover.emit(null);
    }
  }

  protected clearHovers(): void {
    if (!this.panning()) {
      this.territoryHover.emit(null);
      this.adjacencyHover.emit(null);
    }
  }

  protected zoomIn(): void {
    this.nudgeZoom(1);
  }

  protected zoomOut(): void {
    this.nudgeZoom(-1);
  }

  protected zoomToActualSize(): void {
    this.fitToPanel.set(false);
    this.zoom.set(1);
    this.centerImage();
  }

  protected zoomToFit(): void {
    this.fitToPanel.set(true);
    this.centerImage();
  }

  protected onZoomInput(event: Event): void {
    const value = Number((event.target as HTMLInputElement).value);
    if (Number.isFinite(value)) {
      this.fitToPanel.set(false);
      this.zoom.set(clampZoom(value / 100));
      if (this.zoom() === 1) {
        this.centerImage();
      } else {
        this.clampPan();
      }
    }
  }

  protected onViewportKeydown(event: KeyboardEvent): void {
    if (event.key === ' ' || event.code === 'Space') {
      event.preventDefault();
      this.spaceHeld.set(true);
      return;
    }

    if (event.key === '+' || event.key === '=') {
      event.preventDefault();
      this.zoomIn();
      return;
    }

    if (event.key === '-' || event.key === '_') {
      event.preventDefault();
      this.zoomOut();
      return;
    }

    if (event.key === '0') {
      event.preventDefault();
      this.zoomToActualSize();
      return;
    }

    const step = 28;
    if (event.key === 'ArrowLeft') {
      event.preventDefault();
      this.panX.update((value) => value + step);
      this.clampPan();
    } else if (event.key === 'ArrowRight') {
      event.preventDefault();
      this.panX.update((value) => value - step);
      this.clampPan();
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      this.panY.update((value) => value + step);
      this.clampPan();
    } else if (event.key === 'ArrowDown') {
      event.preventDefault();
      this.panY.update((value) => value - step);
      this.clampPan();
    }
  }

  protected onViewportKeyup(event: KeyboardEvent): void {
    if (event.key === ' ' || event.code === 'Space') {
      this.spaceHeld.set(false);
    }
  }

  private isSelected(territoryId: string): boolean {
    return this.selectedTerritoryIds().includes(territoryId) || territoryId === this.hoveredTerritoryId();
  }

  private isAdjacent(territoryId: string): boolean {
    return this.adjacentTerritoryIds().includes(territoryId);
  }

  private pointFromEvent(event: PointerEvent): MapPoint | null {
    const point = this.unclampedPointFromClient(event.clientX, event.clientY);
    return point ? clampPoint(point) : null;
  }

  private unclampedPointFromClient(clientX: number, clientY: number): MapPoint | null {
    const svg = this.viewport()?.nativeElement.querySelector('svg');
    if (!svg) {
      return null;
    }

    const rect = svg.getBoundingClientRect();
    if (rect.width === 0 || rect.height === 0) {
      return null;
    }

    return {
      x: (clientX - rect.left) / rect.width,
      y: (clientY - rect.top) / rect.height,
    };
  }

  private beginMarquee(event: PointerEvent): void {
    const point = this.unclampedPointFromClient(event.clientX, event.clientY);
    if (!point) {
      this.backgroundSelect.emit();
      return;
    }

    this.marqueeOrigin = {
      clientX: event.clientX,
      clientY: event.clientY,
      point,
      additive: event.ctrlKey || event.metaKey,
    };
    this.marqueeBox.set(null);
    try {
      (event.currentTarget as HTMLElement | SVGElement).setPointerCapture(event.pointerId);
    } catch {
      // Some test hosts do not support pointer capture; the box still follows move events.
    }
    event.preventDefault();
  }

  private updateMarquee(event: PointerEvent): void {
    const origin = this.marqueeOrigin;
    const viewport = this.viewport()?.nativeElement.getBoundingClientRect();
    if (!origin || !viewport) {
      return;
    }

    const width = Math.abs(event.clientX - origin.clientX);
    const height = Math.abs(event.clientY - origin.clientY);
    if (width < 4 && height < 4) {
      return;
    }

    this.marqueeBox.set({
      left: Math.min(origin.clientX, event.clientX) - viewport.left,
      top: Math.min(origin.clientY, event.clientY) - viewport.top,
      width,
      height,
    });
  }

  private finishMarquee(event?: PointerEvent): void {
    const origin = this.marqueeOrigin;
    const box = this.marqueeBox();
    this.marqueeOrigin = null;
    this.marqueeBox.set(null);
    if (!origin) {
      return;
    }

    if (!box || !event) {
      this.backgroundSelect.emit();
      return;
    }

    const end = this.unclampedPointFromClient(event.clientX, event.clientY) ?? origin.point;
    this.territoryMarquee.emit({
      ids: this.territories()
        .filter((territory) => polygonIntersectsRect(territory.polygon, origin.point.x, origin.point.y, end.x, end.y))
        .map((territory) => territory.id),
      additive: origin.additive,
    });
  }

  private territoryIdAt(point: MapPoint): string | null {
    for (const territory of [...this.territories()].reverse()) {
      if (containsStrict(territory.polygon, point) || pointOnPolygonBoundary(territory.polygon, point)) {
        return territory.id;
      }
    }

    return null;
  }

  private shouldAlwaysPan(event: PointerEvent): boolean {
    return event.button === 1 || event.button === 2 || this.spaceHeld();
  }

  private beginPan(event: PointerEvent): void {
    this.panning.set(true);
    this.panOrigin = { x: event.clientX, y: event.clientY, panX: this.panX(), panY: this.panY() };
    try {
      (event.currentTarget as HTMLElement | SVGElement).setPointerCapture(event.pointerId);
    } catch {
      // Some test hosts do not support pointer capture; pan still follows move events.
    }
    event.preventDefault();
  }

  private applyPan(event: PointerEvent): boolean {
    if (!this.panning()) {
      return false;
    }

    this.panX.set(this.panOrigin.panX + event.clientX - this.panOrigin.x);
    this.panY.set(this.panOrigin.panY + event.clientY - this.panOrigin.y);
    this.clampPan();
    return true;
  }

  private observeViewport(): void {
    const element = this.viewport()?.nativeElement;
    if (!element) {
      return;
    }

    this.viewportSize.set({ width: element.clientWidth || 1, height: element.clientHeight || 1 });
    this.resizeObserver?.disconnect();
    this.resizeObserver = new ResizeObserver((entries) => {
      const box = entries[0].contentRect;
      this.viewportSize.set({ width: box.width || 1, height: box.height || 1 });
      this.clampPan();
    });
    this.resizeObserver.observe(element);
    if (!this.observedDestroy) {
      this.observedDestroy = true;
      this.destroyRef.onDestroy(() => this.resizeObserver?.disconnect());
    }
  }

  private currentScale(): number {
    return this.fitToPanel() ? this.fitScale() : this.zoom();
  }

  private fitScale(): number {
    const image = this.imageSize();
    const viewport = this.viewportSize();
    return Math.min(viewport.width / image.width, viewport.height / image.height);
  }

  private canDragPan(): boolean {
    const image = this.imageSize();
    const viewport = this.viewportSize();
    const scale = this.currentScale();
    return image.width * scale > viewport.width || image.height * scale > viewport.height;
  }

  private nudgeZoom(direction: 1 | -1, clientX?: number, clientY?: number): void {
    const next = steppedZoom(this.currentScale(), direction);
    const viewport = this.viewport()?.nativeElement.getBoundingClientRect();
    if (!viewport) {
      this.fitToPanel.set(false);
      this.zoom.set(next);
      this.clampPan();
      return;
    }

    this.zoomToAt(clientX ?? viewport.left + viewport.width / 2, clientY ?? viewport.top + viewport.height / 2, next);
  }

  private zoomToAt(clientX: number, clientY: number, nextZoom: number): void {
    const viewport = this.viewport()?.nativeElement.getBoundingClientRect();
    if (!viewport) {
      return;
    }

    const oldScale = Math.max(this.currentScale(), Number.EPSILON);
    const nextScale = Math.max(nextZoom, Number.EPSILON);
    const cx = clientX - viewport.left;
    const cy = clientY - viewport.top;
    const imgX = (cx - this.panX()) / oldScale;
    const imgY = (cy - this.panY()) / oldScale;
    this.fitToPanel.set(false);
    this.zoom.set(nextZoom);
    this.panX.set(cx - imgX * nextScale);
    this.panY.set(cy - imgY * nextScale);
    this.clampPan();
  }

  private centerImage(): void {
    const image = this.imageSize();
    const viewport = this.viewportSize();
    const scale = this.currentScale();
    this.panX.set((viewport.width - image.width * scale) / 2);
    this.panY.set((viewport.height - image.height * scale) / 2);
  }

  private clampPan(): void {
    const image = this.imageSize();
    const viewport = this.viewportSize();
    const scale = this.currentScale();
    const scaledWidth = image.width * scale;
    const scaledHeight = image.height * scale;
    this.panX.set(clampAxis(this.panX(), viewport.width, scaledWidth));
    this.panY.set(clampAxis(this.panY(), viewport.height, scaledHeight));
  }
}

function clampZoom(value: number): number {
  return Math.min(MAX_ZOOM, Math.max(MIN_ZOOM, value));
}

function steppedZoom(current: number, direction: 1 | -1): number {
  const percent = Math.round(current * 100);
  const stepPercent = ZOOM_STEP * 100;
  if (direction > 0) {
    if (current >= MAX_ZOOM) {
      return current;
    }

    return clampZoom(Math.ceil((percent + 0.001) / stepPercent) * ZOOM_STEP);
  }

  if (current <= MIN_ZOOM) {
    return current;
  }

  return clampZoom(Math.floor((percent - 0.001) / stepPercent) * ZOOM_STEP);
}

function clampAxis(pan: number, viewport: number, scaled: number): number {
  if (scaled <= viewport) {
    return (viewport - scaled) / 2;
  }

  const min = viewport - scaled;
  if (pan > 0) {
    return 0;
  }

  return pan < min ? min : pan;
}
