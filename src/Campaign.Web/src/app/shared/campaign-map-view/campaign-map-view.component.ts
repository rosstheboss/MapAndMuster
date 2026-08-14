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
import {
  centroid,
  clampPoint,
  containsStrict,
  fitSquareInPolygon,
  MARKER_MAX_PX,
  MAX_ZOOM,
  MIN_ZOOM,
  pointOnPolygonBoundary,
  polygonPointsAttribute,
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
  readonly adjacentTerritoryIds = input<readonly string[]>([]);
  readonly showAdjacencies = input(false);
  readonly interactive = input(true);
  readonly pointerPan = input(false);
  readonly moveTerritories = input(false);
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
  private panning = false;
  private movingTerritory = false;
  private moveOrigin: MapPoint | null = null;
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

      const from = centroid(left.polygon);
      const to = centroid(right.polygon);
      return [
        {
          edge,
          geometry: doubleArrow(edge.marker, from, to),
          highlighted: edge.id === this.hoveredAdjacencyId() || this.isSelected(edge.territoryAId),
          transform: this.hoverScale(edge.marker, edge.id === this.hoveredAdjacencyId()),
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

  protected markerSize(fit: FittedSquare): { width: number; height: number } {
    const image = this.imageSize();
    return { width: fit.width * image.width, height: fit.height * image.height };
  }

  protected onImageLoad(event: Event): void {
    const image = event.target as HTMLImageElement;
    this.imageSize.set({ width: image.naturalWidth || 1, height: image.naturalHeight || 1 });
    this.observeViewport();
    this.zoomToFit();
  }

  protected onWheel(event: WheelEvent): void {
    event.preventDefault();
    const factor = event.deltaY < 0 ? 1 : -1;
    this.nudgeZoom(factor, event.clientX, event.clientY);
  }

  protected onPointerMove(event: PointerEvent): void {
    if (this.panning) {
      this.panX.set(this.panOrigin.panX + event.clientX - this.panOrigin.x);
      this.panY.set(this.panOrigin.panY + event.clientY - this.panOrigin.y);
      this.clampPan();
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

  protected onPointerDown(event: PointerEvent): void {
    if (event.button === 1 || this.spaceHeld()) {
      this.beginPan(event);
      return;
    }

    if (!this.interactive()) {
      if (this.pointerPan() && event.button === 0 && this.canDragPan()) {
        this.beginPan(event);
      }

      return;
    }

    if (event.button !== 0) {
      return;
    }

    const point = this.pointFromEvent(event);
    if (!point) {
      return;
    }

    const target = event.target as SVGElement | HTMLElement;
    const kind = target.dataset['kind'];
    const id = target.dataset['id'];
    if (kind === 'adjacency' && id) {
      this.adjacencySelect.emit(id);
      event.preventDefault();
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

    this.backgroundSelect.emit();
    this.mapPoint.emit(point);
    if (this.pointerPan() && this.canDragPan()) {
      this.beginPan(event);
    }
  }

  protected onPointerUp(): void {
    if (this.movingTerritory) {
      this.movingTerritory = false;
      this.moveOrigin = null;
      this.territoryMoveEnd.emit();
    }

    this.panning = false;
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
    this.adjacencyHover.emit(id);
  }

  protected onAdjacencyLeave(id: string): void {
    if (this.hoveredAdjacencyId() === id) {
      this.adjacencyHover.emit(null);
    }
  }

  protected clearHovers(): void {
    if (!this.panning) {
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

  private hoverScale(marker: MapPoint, highlighted: boolean): string | null {
    if (!highlighted) {
      return null;
    }

    return `translate(${marker.x} ${marker.y}) scale(1.5) translate(${-marker.x} ${-marker.y})`;
  }

  private pointFromEvent(event: PointerEvent): MapPoint | null {
    const svg = (event.currentTarget as SVGElement).closest('svg');
    if (!svg) {
      return null;
    }

    const rect = svg.getBoundingClientRect();
    if (rect.width === 0 || rect.height === 0) {
      return null;
    }

    return clampPoint({
      x: (event.clientX - rect.left) / rect.width,
      y: (event.clientY - rect.top) / rect.height,
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

  private beginPan(event: PointerEvent): void {
    this.panning = true;
    this.panOrigin = { x: event.clientX, y: event.clientY, panX: this.panX(), panY: this.panY() };
    (event.currentTarget as HTMLElement | SVGElement).setPointerCapture(event.pointerId);
    event.preventDefault();
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

function doubleArrow(
  marker: MapPoint,
  from: MapPoint,
  to: MapPoint,
): { x1: number; y1: number; x2: number; y2: number; headA: string; headB: string } {
  const dx = to.x - from.x;
  const dy = to.y - from.y;
  const length = Math.hypot(dx, dy) || 1;
  const ux = dx / length;
  const uy = dy / length;
  const half = 0.028;
  const head = 0.012;
  const x1 = marker.x - ux * half;
  const y1 = marker.y - uy * half;
  const x2 = marker.x + ux * half;
  const y2 = marker.y + uy * half;
  const px = -uy;
  const py = ux;
  const headA = `${x1},${y1} ${x1 + ux * head + px * head * 0.55},${y1 + uy * head + py * head * 0.55} ${x1 + ux * head - px * head * 0.55},${y1 + uy * head - py * head * 0.55}`;
  const headB = `${x2},${y2} ${x2 - ux * head + px * head * 0.55},${y2 - uy * head + py * head * 0.55} ${x2 - ux * head - px * head * 0.55},${y2 - uy * head - py * head * 0.55}`;
  return { x1, y1, x2, y2, headA, headB };
}
