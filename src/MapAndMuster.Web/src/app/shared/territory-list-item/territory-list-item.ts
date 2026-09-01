import { Component, input, output } from '@angular/core';

import type { CampaignFaction, CampaignStructureType, CampaignTerrainType } from '../../core/campaigns/campaign.models';
import { resolveFactionAppearance } from '../../core/campaigns/faction-appearance';
import { territoryLabel, type MapTerritory } from '../../core/maps/map-graph.models';
import { FactionLogoComponent } from '../faction-logo/faction-logo.component';
import { MapSymbolComponent } from '../map-symbol/map-symbol.component';

export interface TerritoryListItemMarks {
  label: string;
  terrainSymbol: string | null;
  structureSymbol: string | null;
  structureImageUrl: string | null;
  structurePillaged: boolean;
  ownerLogoSrc: string | null;
  ownerColor: string | null;
  ownerTint: boolean;
  ownerName: string | null;
}

export function territoryListItemMarks(
  territory: MapTerritory,
  options: {
    factions: readonly CampaignFaction[];
    terrainTypes: readonly Pick<CampaignTerrainType, 'id' | 'name'>[];
    structures: readonly CampaignStructureType[];
    flagImageUrl: (factionId: string, subfaction?: string | null) => string | null;
    structureImageUrl: (structureTypeId: string, pillaged?: boolean) => string | null;
  },
): TerritoryListItemMarks {
  const owner = territory.ownerFactionId
    ? (options.factions.find((faction) => faction.id === territory.ownerFactionId) ?? null)
    : null;
  const appearance = territory.ownerFactionId ? resolveFactionAppearance(owner, territory.ownerSubfaction) : null;
  const terrain = options.terrainTypes.find((type) => type.id === territory.terrainTypeId) ?? null;
  const structure =
    territory.structureTypeId && territory.structureCondition !== 'Destroyed'
      ? (options.structures.find((type) => type.id === territory.structureTypeId) ?? null)
      : null;
  const pillaged = !!structure && territory.structureCondition === 'Pillaged';
  const structureImageUrl = structure
    ? pillaged && structure.hasPillagedImage
      ? options.structureImageUrl(structure.id, true)
      : !pillaged && structure.hasImage
        ? options.structureImageUrl(structure.id, false)
        : null
    : null;

  return {
    label: territoryLabel(territory),
    terrainSymbol: terrain?.name ?? null,
    structureSymbol: structure?.builtinSymbol ?? null,
    structureImageUrl,
    structurePillaged: pillaged,
    ownerLogoSrc:
      appearance?.hasFlagImage && territory.ownerFactionId
        ? options.flagImageUrl(territory.ownerFactionId, territory.ownerSubfaction)
        : null,
    ownerColor: appearance?.color ?? null,
    ownerTint: appearance?.tint === true,
    ownerName: owner?.name ?? null,
  };
}

@Component({
  selector: 'app-territory-list-item',
  imports: [FactionLogoComponent, MapSymbolComponent],
  templateUrl: './territory-list-item.html',
  styleUrl: './territory-list-item.css',
})
export class TerritoryListItemComponent {
  readonly marks = input.required<TerritoryListItemMarks>();
  readonly territoryId = input<string | null>(null);
  readonly selected = input(false);
  readonly dirty = input(false);
  readonly tooltip = input<string | null>(null);
  readonly activate = output<MouseEvent>();
}
