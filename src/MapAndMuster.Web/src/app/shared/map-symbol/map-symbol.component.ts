import { Component, input } from '@angular/core';

@Component({
  selector: 'app-map-symbol',
  templateUrl: './map-symbol.component.html',
  styleUrl: './map-symbol.component.css',
})
export class MapSymbolComponent {
  readonly kind = input.required<'terrain' | 'structure' | 'item'>();
  readonly name = input.required<string>();
  readonly label = input<string | undefined>(undefined);
  readonly pillaged = input(false);
}
