import { Component, input } from '@angular/core';

import { MapSymbolComponent } from '../map-symbol/map-symbol.component';

export interface MapLegendFactionMark {
  id: string;
  name: string;
  color: string;
  image: string | null;
  tint: boolean;
}

export interface MapLegendItemMark {
  name: string;
  builtinSymbol: string;
  color: string;
  imageUrl: string | null;
}

export interface MapLegendStructureMark {
  id: string;
  name: string;
  builtinSymbol: string | null;
  hasImage: boolean;
  hasPillagedImage: boolean;
  isPillageable?: boolean;
  imageUrl?: string | null;
  pillagedImageUrl?: string | null;
}

@Component({
  selector: 'app-map-legend',
  imports: [MapSymbolComponent],
  templateUrl: './map-legend.component.html',
  styleUrl: './map-legend.component.css',
})
export class MapLegendComponent {
  readonly factions = input<readonly MapLegendFactionMark[]>([]);
  readonly structures = input<readonly MapLegendStructureMark[]>([]);
  readonly pillageStructure = input<MapLegendStructureMark | null>(null);
  readonly items = input<readonly MapLegendItemMark[]>([]);
  readonly showYourForce = input(false);
  readonly showForceInBattle = input(false);
  readonly showPlayGlows = input(false);
}
