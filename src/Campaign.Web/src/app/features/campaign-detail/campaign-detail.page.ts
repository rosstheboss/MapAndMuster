import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { AuthService, readApiError } from '../../core/auth/auth.service';
import { CampaignService } from '../../core/campaigns/campaign.service';
import type { CampaignDetail, CampaignMission } from '../../core/campaigns/campaign.models';
import { missionsForTerritory, structureTypeById, terrainTypeById } from '../../core/campaigns/campaign.models';
import { InstantDatePipe } from '../../shared/time/instant-date.pipe';
import { actionNumberAt, formatDuration, formatPhaseLabel, statusLabel } from '../../core/campaigns/campaign-schedule';
import { formatLocation } from '../../core/location/location';
import { adjacentTerritoryIds } from '../../core/maps/adjacency';
import { downloadBlob, mapDownloadFilename, rasterizeMapPng } from '../../core/maps/map-export';
import type { MapGraph, MapTerritory } from '../../core/maps/map-graph.models';
import { territoryLabel } from '../../core/maps/map-graph.models';
import { CampaignMapPreviewComponent } from '../../shared/campaign-map-preview/campaign-map-preview.component';
import { CampaignMapViewComponent } from '../../shared/campaign-map-view/campaign-map-view.component';
import { MapSymbolComponent } from '../../shared/map-symbol/map-symbol.component';

@Component({
  selector: 'app-campaign-detail-page',
  imports: [RouterLink, InstantDatePipe, CampaignMapViewComponent, CampaignMapPreviewComponent, MapSymbolComponent],
  templateUrl: './campaign-detail.page.html',
  styleUrl: './campaign-detail.page.css',
})
export class CampaignDetailPage {
  private readonly campaignsApi = inject(CampaignService);
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly campaign = signal<CampaignDetail | null>(null);
  protected readonly graph = signal<MapGraph>({ territories: [], adjacencies: [] });
  protected readonly hoveredTerritoryId = signal<string | null>(null);
  protected readonly selectedIds = signal<string[]>([]);
  protected readonly confirmingDelete = signal(false);
  protected readonly deleting = signal(false);
  protected readonly downloading = signal(false);

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

    return this.campaignsApi.mapUrl(campaign.id, campaign.revision);
  });

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

  protected inspectedMissions(): CampaignMission[] {
    const territory = this.hoveredTerritory();
    return missionsForTerritory(this.campaign(), territory?.terrainTypeId, territory?.structureTypeId);
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

  protected statusText(campaign: CampaignDetail): string {
    return statusLabel(campaign.status);
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

  protected currentPhaseText(campaign: CampaignDetail): string {
    if (campaign.currentRound === null || campaign.currentPhaseNumber === null || !campaign.currentPhaseKind) {
      return '';
    }

    const index = campaign.currentPhaseNumber - 1;
    return `Round ${campaign.currentRound} · ${formatPhaseLabel(campaign.currentPhaseKind, actionNumberAt(campaign.phases, index))}`;
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

  private async load(id: string): Promise<void> {
    try {
      const [campaign, graph] = await Promise.all([this.campaignsApi.get(id), this.campaignsApi.getMapGraph(id)]);
      this.campaign.set(campaign);
      if (campaign.hasMap) {
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
      }
    } catch (error: unknown) {
      this.error.set(readApiError(error, 'Unable to load this campaign.'));
    } finally {
      this.loading.set(false);
    }
  }
}
