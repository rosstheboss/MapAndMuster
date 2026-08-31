import {
  Component,
  computed,
  DestroyRef,
  HostListener,
  inject,
  input,
  output,
  signal,
  viewChild,
  type ElementRef,
} from '@angular/core';

import type { CampaignAllyGroup, CampaignFaction, CampaignStructureType } from '../../core/campaigns/campaign.models';
import { resolveFactionAppearance } from '../../core/campaigns/faction-appearance';
import type { MapHighlightMode } from '../../core/campaigns/campaign-view-prefs.service';
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
  interiorAnchor,
  MARKER_MAX_PX,
  MAX_ZOOM,
  MIN_ZOOM,
  normalizedFromPixels,
  pointOnPolygonBoundary,
  polygonIntersectsRect,
  polygonPointsAttribute,
  rectanglesOverlap,
  SNAP_RING_SCREEN_PX,
  STROKE_FULL_HIGHLIGHT_SCREEN_PX,
  STROKE_HALF_HIGHLIGHT_SCREEN_PX,
  STROKE_SCREEN_PX,
  VERTEX_SCREEN_PX,
  ZOOM_STEP,
} from '../../core/maps/geometry';
import type { FittedSquare, MapPoint } from '../../core/maps/geometry';
import { territoryLabel, type MapAdjacency, type MapTerritory } from '../../core/maps/map-graph.models';
import { IconComponent } from '../icon/icon.component';
import { MapSymbolComponent } from '../map-symbol/map-symbol.component';
import { isAdditiveModifier } from '../../core/maps/pointer';

const HOVER_LIFT_SCREEN_PX = 6;
const SPAWN_STRIPE_SCREEN_PX = 5;

/** Wait before applying or clearing a territory hover so border jitter does not flicker. */
export const TERRITORY_HOVER_INTENT_MS = 200;
/** Ease-in-out duration for lifting a hovered territory and returning it. */
export const TERRITORY_HOVER_MOTION_MS = 200;

export type MovePlacement = 'valid' | 'invalid' | null;

export interface MapForceMarker {
  id: string;
  territoryId: string;
  factionId: string;
  subfaction?: string | null;
  isMine: boolean;
  inBattle: boolean;
  label: string;
  heldItems?: readonly MapHeldItem[];
}

export interface MapHeldItem {
  name: string;
  builtinSymbol: string;
  color: string;
  imageUrl: string | null;
}

export interface MapItemMarker {
  id: string;
  territoryId: string;
  name: string;
  carried: boolean;
  hidden: boolean;
  builtinSymbol?: string;
  color?: string;
  imageUrl?: string | null;
}

@Component({
  selector: 'app-campaign-map-view',
  imports: [IconComponent, MapSymbolComponent],
  templateUrl: './campaign-map-view.component.html',
  styleUrl: './campaign-map-view.component.css',
  host: {
    '[class.is-fullscreen]': 'fullscreen()',
  },
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
  readonly showOverlay = input(true);
  readonly showAdjacencies = input(false);
  readonly layerToggles = input(false);
  readonly showTerritoryDirectory = input(true);
  readonly showConnectionsToggle = input(true);
  readonly adjacenciesInteractive = input(false);
  readonly focusSelectedTerritories = input(false);
  readonly showOverlayChange = output<boolean>();
  readonly showConnectionsChange = output<boolean>();
  readonly interactive = input(true);
  readonly moveTerritories = input(false);
  readonly movePlacement = input<MovePlacement>(null);
  readonly marqueeSelect = input(false);
  readonly factions = input<readonly CampaignFaction[]>([]);
  readonly structures = input<readonly CampaignStructureType[]>([]);
  readonly structureImageUrl = input<(structureTypeId: string, pillaged?: boolean) => string | null>(() => null);
  readonly flagImageUrl = input<(factionId: string, subfaction?: string | null) => string | null>(() => null);
  readonly forces = input<readonly MapForceMarker[]>([]);
  readonly items = input<readonly MapItemMarker[]>([]);
  readonly colorMode = input<MapHighlightMode>('configured');
  readonly allyGroups = input<readonly CampaignAllyGroup[]>([]);
  readonly brokenAllyFactionIds = input<readonly string[]>([]);
  readonly itemImageUrl = input<(typeId: string) => string | null>(() => null);
  readonly emphasizedForceIds = input<readonly string[]>([]);

  readonly mapPoint = output<MapPoint>();
  readonly mapHover = output<MapPoint>();
  readonly territoryHover = output<string | null>();
  readonly adjacencyHover = output<string | null>();
  readonly territorySelect = output<{ id: string; additive: boolean; clientX: number; clientY: number }>();
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
  protected readonly fullscreen = signal(false);
  protected readonly showNames = signal(false);
  protected readonly imageReady = signal(false);
  protected readonly marqueeBox = signal<{ left: number; top: number; width: number; height: number } | null>(null);
  private movingTerritory = false;
  private moveOrigin: MapPoint | null = null;
  private hasFittedImage = false;
  private marqueeOrigin: { clientX: number; clientY: number; point: MapPoint; additive: boolean } | null = null;
  private panOrigin = { x: 0, y: 0, panX: 0, panY: 0 };
  private readonly pointers = new Map<number, { x: number; y: number }>();
  private pinch: { distance: number; zoom: number; imageX: number; imageY: number } | null = null;
  private pinchCooldown = false;
  private hoverIntentTimer: ReturnType<typeof setTimeout> | null = null;
  private hoverIntentId: string | null | undefined = undefined;
  private readonly hoverMotionIds = signal<ReadonlySet<string>>(new Set());
  private readonly hoverMotionTimers = new Map<string, ReturnType<typeof setTimeout>>();
  private resizeObserver: ResizeObserver | null = null;
  private observedDestroy = false;
  private destroyed = false;

  constructor() {
    this.destroyRef.onDestroy(() => {
      this.destroyed = true;
      this.clearHoverIntentTimer();
      this.clearHoverMotion();
      this.releaseFullscreen();
    });
  }

  protected readonly overlayTerritories = computed(() => {
    const image = this.imageSize();
    const scale = Math.max(this.currentScale(), Number.EPSILON);
    const maxWidth = MARKER_MAX_PX / (image.width * scale);
    const maxHeight = MARKER_MAX_PX / (image.height * scale);
    return this.territories().map((territory) => {
      const center = interiorAnchor(territory.polygon);
      const structure = this.structures().find((item) => item.id === territory.structureTypeId) ?? null;
      const destroyed = territory.structureCondition === 'Destroyed';
      const pillaged = territory.structureCondition === 'Pillaged';
      const owner = this.factions().find((faction) => faction.id === territory.ownerFactionId) ?? null;
      const appearance = resolveFactionAppearance(owner, territory.ownerSubfaction);
      const selected = this.isSelected(territory.id);
      const structureFit =
        structure && !destroyed ? fitSquareInPolygon(territory.polygon, center, maxWidth, maxHeight) : null;
      const flagPreferred = structureFit ? { x: structureFit.x + structureFit.width * 0.7, y: structureFit.y } : center;
      const flagFit = owner
        ? fitSquareInPolygon(
            territory.polygon,
            flagPreferred,
            maxWidth,
            maxHeight,
            structureFit ? [structureFit] : null,
          )
        : null;
      const logos: FittedSquare[] = [];
      if (structureFit) {
        logos.push(structureFit);
      }

      if (flagFit) {
        logos.push(flagFit);
      }

      const avoided: FittedSquare[] = [...logos];
      const present = this.forces().filter((force) => force.territoryId === territory.id);
      const forcePins = present.map((force, index) => {
        const preferred = {
          x: center.x + (index - (present.length - 1) / 2) * maxWidth * 0.6,
          y: center.y + maxHeight * 0.38,
        };
        const fit = this.fitForcePin(territory.polygon, preferred, maxWidth, maxHeight, logos, avoided);
        avoided.push(fit);
        const forceOwner = this.factions().find((faction) => faction.id === force.factionId) ?? null;
        const forceAppearance = resolveFactionAppearance(forceOwner, force.subfaction);
        return {
          force,
          fit,
          color: forceAppearance.color,
          emphasized: this.emphasizedForceIds().includes(force.id),
        };
      });
      const presentItems = this.items().filter((item) => item.territoryId === territory.id && !item.carried);
      const itemPins = presentItems.map((item, index) => {
        const preferred = {
          x: center.x + (index - (presentItems.length - 1) / 2) * maxWidth * 0.55,
          y: center.y - maxHeight * 0.38,
        };
        const fit = fitSquareInPolygon(territory.polygon, preferred, maxWidth * 0.7, maxHeight * 0.7, avoided);
        avoided.push(fit);
        return { item, fit };
      });
      const fill = this.territoryFill(territory, owner, appearance.color);
      const spawnFaction = this.factions().find((faction) => faction.id === territory.spawnFactionId) ?? null;
      const spawnColor = spawnFaction
        ? resolveFactionAppearance(spawnFaction, territory.spawnSubfaction).color
        : '#78716c';
      const isSpawn = !!territory.spawnFactionId;
      const stripeColor = fill === 'transparent' ? spawnColor : fill;
      const hovered = territory.id === this.hoveredTerritoryId();
      const lifted = hovered && !selected && !this.marqueeBox();
      const hoverLift = -this.screenToMap(HOVER_LIFT_SCREEN_PX);
      const glowSource = isSpawn ? stripeColor : fill;
      return {
        territory,
        points: polygonPointsAttribute(territory.polygon),
        center,
        structureFit,
        flag:
          owner && flagFit
            ? {
                ...flagFit,
                color: appearance.color,
                image: appearance.hasFlagImage ? this.flagImageUrl()(owner.id, territory.ownerSubfaction) : null,
                tint: appearance.tint,
              }
            : null,
        forces: forcePins,
        items: itemPins,
        structure,
        structureImage: structure
          ? pillaged && structure.hasPillagedImage
            ? this.structureImageUrl()(structure.id, true)
            : !pillaged && structure.hasImage
              ? this.structureImageUrl()(structure.id, false)
              : null
          : null,
        selected,
        halfHighlighted: !selected && this.isHalfHighlighted(territory.id),
        dimmed:
          this.focusSelectedTerritories() &&
          this.selectedTerritoryIds().length > 0 &&
          !selected &&
          !this.isHalfHighlighted(territory.id),
        moveValid: selected && this.movePlacement() === 'valid',
        moveInvalid: selected && this.movePlacement() === 'invalid',
        isSpawn,
        fill: isSpawn ? `url(#${spawnStripePatternId(stripeColor)})` : fill,
        lifted,
        hoverLift,
        strokeWidth: this.screenToMap(
          selected
            ? STROKE_FULL_HIGHLIGHT_SCREEN_PX
            : this.isHalfHighlighted(territory.id)
              ? STROKE_HALF_HIGHLIGHT_SCREEN_PX
              : STROKE_SCREEN_PX,
        ),
        glowColor: glowSource === 'transparent' ? 'var(--color-glow)' : glowSource,
        accessibleName: territoryLabel(territory),
        mapLabel: overlayNameLabel(territory, image, scale),
      };
    });
  });

  protected readonly territoryDirectory = computed(() =>
    [...this.territories()]
      .sort(
        (left, right) =>
          left.displayNumber - right.displayNumber || territoryLabel(left).localeCompare(territoryLabel(right)),
      )
      .map((territory) => ({
        id: territory.id,
        label: territoryLabel(territory),
        selected: this.selectedTerritoryIds().includes(territory.id),
      })),
  );

  protected readonly spawnStripePatterns = computed(() => {
    const stripe = this.screenToMap(SPAWN_STRIPE_SCREEN_PX);
    const period = Math.max(stripe * 2, Number.EPSILON);
    const seen = new Map<string, { id: string; color: string }>();
    for (const territory of this.territories()) {
      if (!territory.spawnFactionId) {
        continue;
      }

      const owner = this.factions().find((faction) => faction.id === territory.ownerFactionId) ?? null;
      const appearance = resolveFactionAppearance(owner, territory.ownerSubfaction);
      const fill = this.territoryFill(territory, owner, appearance.color);
      const spawnFaction = this.factions().find((faction) => faction.id === territory.spawnFactionId) ?? null;
      const color =
        fill === 'transparent'
          ? spawnFaction
            ? resolveFactionAppearance(spawnFaction, territory.spawnSubfaction).color
            : '#78716c'
          : fill;
      if (!seen.has(color)) {
        seen.set(color, { id: spawnStripePatternId(color), color });
      }
    }

    return { stripe, period, patterns: [...seen.values()] };
  });

  protected readonly moveDropMarker = computed(() => {
    const placement = this.movePlacement();
    if (!placement) {
      return null;
    }

    const selected = this.territories().filter((territory) => this.isSelected(territory.id));
    if (selected.length === 0) {
      return null;
    }

    let x = 0;
    let y = 0;
    for (const territory of selected) {
      const center = centroid(territory.polygon);
      x += center.x;
      y += center.y;
    }

    return {
      x: x / selected.length,
      y: y / selected.length,
      size: MARKER_MAX_PX / Math.max(this.currentScale(), Number.EPSILON),
      valid: placement === 'valid',
    };
  });

  protected readonly overlayAdjacencies = computed(() => {
    if (!this.showOverlay() || !this.showAdjacencies()) {
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
      const strokeWidth = Math.min(this.screenToMap(STROKE_FULL_HIGHLIGHT_SCREEN_PX), 0.008);
      return [
        {
          edge,
          geometry: adjacencyArrowGeometry(ends.from, ends.to, this.screenToMap(ARROW_HEAD_SCREEN_PX)),
          hitWidth: Math.min(this.screenToMap(ARROW_HIT_SCREEN_PX), 0.05),
          strokeWidth,
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

  protected readonly mapScale = computed(() => Math.max(this.currentScale(), 0.01));

  protected readonly zoomPercent = computed(() => Math.round(this.currentScale() * 100));

  protected markerSize(fit: FittedSquare): { width: number; height: number } {
    const image = this.imageSize();
    return { width: fit.width * image.width, height: fit.height * image.height };
  }

  protected flagBackground(flag: { image: string | null; tint: boolean; color: string }): string {
    if (flag.image && flag.tint) {
      return flag.color;
    }

    return flag.image ? 'transparent' : flag.color;
  }

  protected maskUrl(src: string): string {
    return `url(${JSON.stringify(src)})`;
  }

  protected forceLabel(force: MapForceMarker): string {
    const held = force.heldItems?.map((item) => item.name).join(', ');
    return held ? `${force.label}. Holding ${held}` : force.label;
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
    this.imageReady.set(true);
    this.observeViewport();
    if (!this.hasFittedImage) {
      this.hasFittedImage = true;
      this.zoomToFit();
    }
  }

  protected onWheel(event: WheelEvent): void {
    event.preventDefault();
    this.clearHoverMotion();
    const factor = event.deltaY < 0 ? 1 : -1;
    this.nudgeZoom(factor, event.clientX, event.clientY);
  }

  protected onPointerMove(event: PointerEvent): void {
    this.updatePointer(event);
    if (this.applyPinch()) {
      return;
    }

    if (this.pinchCooldown) {
      return;
    }

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
    this.rememberPointer(event);
    if (this.pointers.size >= 2) {
      this.beginPinch();
      event.preventDefault();
      event.stopPropagation();
      return;
    }

    if (this.pinchCooldown) {
      event.preventDefault();
      return;
    }

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
    this.rememberPointer(event);
    if (this.pointers.size >= 2) {
      this.beginPinch();
      event.preventDefault();
      event.stopPropagation();
      return;
    }

    if (this.pinchCooldown) {
      event.preventDefault();
      return;
    }

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
      const additive = isAdditiveModifier(event);
      this.activateTerritory(territoryId, additive, event.clientX, event.clientY);
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
    this.forgetPointer(event);
    if (this.pinchCooldown) {
      this.panning.set(false);
      return;
    }

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
    if (this.hoveredTerritoryId() === id) {
      this.clearHoverIntentTimer();
      this.hoverIntentId = undefined;
      return;
    }

    this.scheduleTerritoryHover(id);
  }

  protected onTerritoryFocus(id: string): void {
    this.clearHoverIntentTimer();
    this.hoverIntentId = undefined;
    this.emitTerritoryHover(id);
  }

  protected onTerritoryKeydown(event: KeyboardEvent, id: string): void {
    if (event.key !== 'Enter' && event.key !== ' ') {
      return;
    }

    event.preventDefault();
    event.stopPropagation();
    this.activateTerritory(id, isAdditiveModifier(event), 0, 0);
  }

  protected onDirectorySelect(id: string, event: MouseEvent): void {
    this.activateTerritory(id, isAdditiveModifier(event), event.clientX, event.clientY);
    this.emitTerritoryHover(id);
  }

  protected onShowNamesChange(event: Event): void {
    const target = event.target;
    if (target instanceof HTMLInputElement) {
      this.showNames.set(target.checked);
    }
  }

  protected onTerritoryLeave(id: string): void {
    if (this.hoverIntentId === id) {
      this.clearHoverIntentTimer();
      this.hoverIntentId = undefined;
      return;
    }

    if (this.hoveredTerritoryId() === id) {
      this.scheduleTerritoryHover(null);
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
    this.clearHoverIntentTimer();
    this.hoverIntentId = undefined;
    if (!this.panning()) {
      this.emitTerritoryHover(null);
      this.adjacencyHover.emit(null);
    }
  }

  private scheduleTerritoryHover(id: string | null): void {
    if (this.hoverIntentId === id) {
      return;
    }

    this.clearHoverIntentTimer();
    this.hoverIntentId = id;
    this.hoverIntentTimer = setTimeout(() => {
      this.hoverIntentTimer = null;
      this.hoverIntentId = undefined;
      this.emitTerritoryHover(id);
    }, TERRITORY_HOVER_INTENT_MS);
  }

  private emitTerritoryHover(id: string | null): void {
    const previous = this.hoveredTerritoryId();
    if (previous && previous !== id) {
      this.playHoverMotion(previous);
    }

    if (id) {
      this.playHoverMotion(id);
    }

    this.territoryHover.emit(id);
  }

  protected isHoverMotion(id: string): boolean {
    return this.hoverMotionIds().has(id);
  }

  private playHoverMotion(id: string): void {
    const existing = this.hoverMotionTimers.get(id);
    if (existing !== undefined) {
      clearTimeout(existing);
    }

    if (!this.hoverMotionIds().has(id)) {
      const next = new Set(this.hoverMotionIds());
      next.add(id);
      this.hoverMotionIds.set(next);
    }

    this.hoverMotionTimers.set(
      id,
      setTimeout(() => {
        this.hoverMotionTimers.delete(id);
        const next = new Set(this.hoverMotionIds());
        next.delete(id);
        this.hoverMotionIds.set(next);
      }, TERRITORY_HOVER_MOTION_MS),
    );
  }

  private clearHoverMotion(): void {
    for (const timer of this.hoverMotionTimers.values()) {
      clearTimeout(timer);
    }

    this.hoverMotionTimers.clear();
    if (this.hoverMotionIds().size > 0) {
      this.hoverMotionIds.set(new Set());
    }
  }

  private clearHoverIntentTimer(): void {
    if (this.hoverIntentTimer !== null) {
      clearTimeout(this.hoverIntentTimer);
      this.hoverIntentTimer = null;
    }
  }

  protected zoomIn(): void {
    this.nudgeZoom(1);
  }

  protected zoomOut(): void {
    this.nudgeZoom(-1);
  }

  protected zoomToActualSize(): void {
    this.clearHoverMotion();
    this.fitToPanel.set(false);
    this.zoom.set(1);
    this.centerImage();
  }

  protected zoomToFit(): void {
    this.clearHoverMotion();
    this.fitToPanel.set(true);
    this.centerImage();
  }

  protected onZoomInput(event: Event): void {
    const value = Number((event.target as HTMLInputElement).value);
    if (Number.isFinite(value)) {
      this.clearHoverMotion();
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
    if (event.ctrlKey || event.metaKey || event.altKey) {
      return;
    }

    if (event.key === ' ' || event.code === 'Space') {
      if (event.target !== event.currentTarget) {
        return;
      }

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

    if (event.key === 'f' || event.key === 'F') {
      event.preventDefault();
      this.zoomToFit();
      return;
    }

    if (event.key === '1' || event.key === '0') {
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

  @HostListener('document:keydown', ['$event'])
  protected onDocumentKeydown(event: KeyboardEvent): void {
    if (event.ctrlKey || event.metaKey || event.altKey || this.isTypingTarget(event.target)) {
      return;
    }

    if (event.key === 'Escape' && this.fullscreen()) {
      event.preventDefault();
      this.setFullscreen(false);
      return;
    }

    if (event.key === 'm' || event.key === 'M') {
      event.preventDefault();
      this.toggleFullscreen();
    }
  }

  protected toggleFullscreen(): void {
    this.setFullscreen(!this.fullscreen());
  }

  protected onShowOverlayChange(event: Event): void {
    const target = event.target;
    if (target instanceof HTMLInputElement) {
      this.showOverlayChange.emit(target.checked);
    }
  }

  protected onShowConnectionsChange(event: Event): void {
    const target = event.target;
    if (target instanceof HTMLInputElement) {
      this.showConnectionsChange.emit(target.checked);
    }
  }

  private activateTerritory(id: string, additive: boolean, clientX: number, clientY: number): void {
    if (!this.interactive()) {
      return;
    }

    this.territorySelect.emit({ id, additive, clientX, clientY });
  }

  private isSelected(territoryId: string): boolean {
    return this.selectedTerritoryIds().includes(territoryId);
  }

  private fitForcePin(
    polygon: readonly MapPoint[],
    preferred: MapPoint,
    maxWidth: number,
    maxHeight: number,
    logos: readonly FittedSquare[],
    otherDots: readonly FittedSquare[],
  ): FittedSquare {
    const options = { minScale: 0.5, allowOverlapFallback: false } as const;
    const withAll = fitSquareInPolygon(polygon, preferred, maxWidth, maxHeight, [...logos, ...otherDots], options);
    if (!logos.some((logo) => rectanglesOverlap(withAll, logo))) {
      return withAll;
    }

    return fitSquareInPolygon(polygon, preferred, maxWidth, maxHeight, logos, options);
  }

  private territoryFill(territory: MapTerritory, owner: CampaignFaction | null, factionColor?: string): string {
    const mode = this.colorMode();
    if (mode === 'configured') {
      return territory.overlayColor ?? 'transparent';
    }

    if (!owner) {
      return 'transparent';
    }

    if (mode === 'alliance' && owner.allyGroupName && !this.brokenAllyFactionIds().includes(owner.id)) {
      const group = this.allyGroups().find((item) => item.name.toLowerCase() === owner.allyGroupName?.toLowerCase());
      if (group?.color) {
        return group.color;
      }
    }

    return factionColor ?? owner.color;
  }

  private isHalfHighlighted(territoryId: string): boolean {
    return territoryId === this.hoveredTerritoryId() || this.adjacentTerritoryIds().includes(territoryId);
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
      additive: isAdditiveModifier(event),
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

  private isTypingTarget(target: EventTarget | null): boolean {
    if (!(target instanceof HTMLElement)) {
      return false;
    }

    const tag = target.tagName;
    return tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT' || target.isContentEditable;
  }

  private setFullscreen(on: boolean): void {
    this.fullscreen.set(on);
    document.body.style.overflow = on ? 'hidden' : '';
    requestAnimationFrame(() => {
      requestAnimationFrame(() => {
        if (this.destroyed) {
          return;
        }

        this.observeViewport();
        this.repositionAfterViewportChange();
      });
    });
  }

  private releaseFullscreen(): void {
    if (!this.fullscreen()) {
      return;
    }

    this.fullscreen.set(false);
    document.body.style.overflow = '';
  }

  private rememberPointer(event: PointerEvent): void {
    this.pointers.set(event.pointerId, { x: event.clientX, y: event.clientY });
    try {
      (event.currentTarget as HTMLElement | SVGElement).setPointerCapture(event.pointerId);
    } catch {
      // Some test hosts do not support pointer capture; pinch still follows move events.
    }
  }

  private updatePointer(event: PointerEvent): void {
    if (!this.pointers.has(event.pointerId)) {
      return;
    }

    this.pointers.set(event.pointerId, { x: event.clientX, y: event.clientY });
  }

  private forgetPointer(event?: PointerEvent): void {
    if (event) {
      this.pointers.delete(event.pointerId);
    } else {
      this.pointers.clear();
    }

    if (this.pointers.size < 2) {
      this.pinch = null;
      if (this.panning() && !this.spaceHeld()) {
        this.panning.set(false);
      }
    }

    if (this.pointers.size === 0) {
      this.pinchCooldown = false;
    }
  }

  private beginPinch(): void {
    this.cancelTransientMapGestures();
    this.pinchCooldown = true;
    this.panning.set(true);
    const points = [...this.pointers.values()];
    const first = points[0];
    const second = points[1];
    if (!first || !second) {
      return;
    }

    const viewport = this.viewport()?.nativeElement.getBoundingClientRect();
    if (!viewport) {
      return;
    }

    const startScale = this.currentScale();
    this.fitToPanel.set(false);
    this.zoom.set(startScale);
    const midX = (first.x + second.x) / 2 - viewport.left;
    const midY = (first.y + second.y) / 2 - viewport.top;
    this.pinch = {
      distance: Math.max(Math.hypot(first.x - second.x, first.y - second.y), 1),
      zoom: startScale,
      imageX: (midX - this.panX()) / startScale,
      imageY: (midY - this.panY()) / startScale,
    };
  }

  private applyPinch(): boolean {
    if (this.pointers.size < 2) {
      return false;
    }

    if (!this.pinch) {
      this.beginPinch();
    }

    const pinch = this.pinch;
    const points = [...this.pointers.values()];
    const first = points[0];
    const second = points[1];
    if (!pinch || !first || !second) {
      return true;
    }

    const viewport = this.viewport()?.nativeElement.getBoundingClientRect();
    if (!viewport) {
      return true;
    }

    const distance = Math.max(Math.hypot(first.x - second.x, first.y - second.y), 1);
    const nextZoom = clampZoom((pinch.zoom * distance) / pinch.distance);
    this.zoom.set(nextZoom);
    const nextScale = this.currentScale();
    const midX = (first.x + second.x) / 2 - viewport.left;
    const midY = (first.y + second.y) / 2 - viewport.top;
    this.panX.set(midX - pinch.imageX * nextScale);
    this.panY.set(midY - pinch.imageY * nextScale);
    this.clampPan();
    return true;
  }

  private cancelTransientMapGestures(): void {
    if (this.movingTerritory) {
      this.movingTerritory = false;
      this.moveOrigin = null;
      this.territoryMoveEnd.emit();
    }

    this.marqueeOrigin = null;
    this.marqueeBox.set(null);
    this.panning.set(false);
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
    if (this.destroyed) {
      return;
    }

    const element = this.viewport()?.nativeElement;
    if (!element) {
      return;
    }

    this.syncViewportToElement();
    this.resizeObserver?.disconnect();
    this.resizeObserver = new ResizeObserver((entries) => {
      const box = entries[0].contentRect;
      this.viewportSize.set({ width: box.width || 1, height: box.height || 1 });
      this.repositionAfterViewportChange();
    });
    this.resizeObserver.observe(element);
    if (!this.observedDestroy) {
      this.observedDestroy = true;
      this.destroyRef.onDestroy(() => this.resizeObserver?.disconnect());
    }
  }

  private syncViewportToElement(): void {
    const element = this.viewport()?.nativeElement;
    if (!element) {
      return;
    }

    this.viewportSize.set({ width: element.clientWidth || 1, height: element.clientHeight || 1 });
  }

  private repositionAfterViewportChange(): void {
    if (this.fitToPanel()) {
      this.centerImage();
      return;
    }

    this.clampPan();
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
    this.clearHoverMotion();
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

function spawnStripePatternId(color: string): string {
  return `spawn-stripe-${color.replace(/[^a-zA-Z0-9]/g, '')}`;
}

const NAME_CHAR_PX = 10;

function overlayNameLabel(territory: MapTerritory, image: { width: number }, scale: number): string {
  const name = territory.name?.trim();
  if (!name || territory.polygon.length === 0) {
    return String(territory.displayNumber);
  }

  const xs = territory.polygon.map((point) => point.x);
  const widthPx = (Math.max(...xs) - Math.min(...xs)) * image.width * scale;
  if (name.length * NAME_CHAR_PX + 8 > widthPx) {
    return String(territory.displayNumber);
  }

  return name;
}
