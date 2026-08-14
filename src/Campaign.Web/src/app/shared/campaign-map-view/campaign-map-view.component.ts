import { Component, computed, input, output } from '@angular/core';

import type { CampaignFaction, CampaignStructureType } from '../../core/campaigns/campaign.models';
import { centroid, clampPoint, polygonPointsAttribute } from '../../core/maps/geometry';
import type { MapPoint } from '../../core/maps/geometry';
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
  readonly selectedTerritoryId = input<string | null>(null);
  readonly hoveredTerritoryId = input<string | null>(null);
  readonly hoveredAdjacencyId = input<string | null>(null);
  readonly highlightedTerritoryIds = input<readonly string[]>([]);
  readonly showAdjacencies = input(false);
  readonly interactive = input(true);
  readonly factions = input<readonly CampaignFaction[]>([]);
  readonly structures = input<readonly CampaignStructureType[]>([]);
  readonly structureImageUrl = input<(structureTypeId: string) => string | null>(() => null);

  readonly mapPoint = output<MapPoint>();
  readonly mapHover = output<MapPoint>();
  readonly territoryHover = output<string | null>();
  readonly adjacencyHover = output<string | null>();
  readonly territorySelect = output<string>();
  readonly adjacencySelect = output<string>();
  readonly backgroundSelect = output<void>();

  protected readonly overlayTerritories = computed(() =>
    this.territories().map((territory) => {
      const center = centroid(territory.polygon);
      const structure = this.structures().find((item) => item.id === territory.structureTypeId) ?? null;
      const owner = this.factions().find((faction) => faction.id === territory.ownerFactionId) ?? null;
      const hasStructure = !!territory.structureTypeId;
      return {
        territory,
        points: polygonPointsAttribute(territory.polygon),
        center,
        flag: owner
          ? {
              x: hasStructure ? center.x - 0.035 : center.x,
              y: hasStructure ? center.y - 0.035 : center.y,
              color: owner.color,
            }
          : null,
        structure,
        structureImage: structure?.hasImage ? this.structureImageUrl()(structure.id) : null,
        highlighted: this.isHighlighted(territory.id),
        glowColor: territory.overlayColor ?? 'var(--color-glow)',
      };
    }),
  );

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
          highlighted: edge.id === this.hoveredAdjacencyId() || this.isHighlighted(edge.territoryAId),
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

  protected onPointerMove(event: PointerEvent): void {
    const point = this.pointFromEvent(event);
    if (!point) {
      return;
    }

    this.mapHover.emit(point);
  }

  protected onPointerDown(event: PointerEvent): void {
    if (!this.interactive() || event.button !== 0) {
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

    if (kind === 'territory' && id) {
      this.territorySelect.emit(id);
    } else {
      this.backgroundSelect.emit();
    }

    this.mapPoint.emit(point);
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
    this.territoryHover.emit(null);
    this.adjacencyHover.emit(null);
  }

  private isHighlighted(territoryId: string): boolean {
    return (
      territoryId === this.selectedTerritoryId() ||
      territoryId === this.hoveredTerritoryId() ||
      this.highlightedTerritoryIds().includes(territoryId)
    );
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
