import { Component, effect, input, output, signal, untracked } from '@angular/core';

import { IconComponent } from '../icon/icon.component';
import {
  cloneTerritoryDirectoryFilter,
  createDefaultTerritoryDirectoryFilter,
  revealedItemKeys,
  type TerritoryDirectoryFilterContext,
  type TerritoryDirectoryFilterState,
  type TerritoryFilterTriState,
} from './territory-directory-filter';

type FilterIdListKey =
  | 'ownerFactionIds'
  | 'factionTagIds'
  | 'allyGroupIds'
  | 'terrainTypeIds'
  | 'terrainTagIds'
  | 'structureTypeIds'
  | 'structureTagIds'
  | 'revealedItemKeys';

type FilterTriKey = 'neutral' | 'spawn' | 'pillaged' | 'hasStructure' | 'occupied' | 'adjacent';

@Component({
  selector: 'app-territory-directory-filter',
  imports: [IconComponent],
  templateUrl: './territory-directory-filter.component.html',
  styleUrl: './territory-directory-filter.component.css',
})
export class TerritoryDirectoryFilterComponent {
  readonly context = input.required<TerritoryDirectoryFilterContext>();
  readonly applied = output<TerritoryDirectoryFilterState>();

  protected readonly draft = signal<TerritoryDirectoryFilterState>(
    createDefaultTerritoryDirectoryFilter({
      factions: [],
      terrainTypes: [],
      structures: [],
      allyGroups: [],
      factionTags: [],
      terrainTags: [],
      structureTags: [],
      forces: [],
      items: [],
      adjacencies: [],
    }),
  );
  protected readonly triStates: { value: TerritoryFilterTriState; label: string }[] = [
    { value: 'any', label: 'Any' },
    { value: 'yes', label: 'Yes' },
    { value: 'no', label: 'No' },
  ];
  private draftTouched = false;

  constructor() {
    effect(() => {
      const context = this.context();
      untracked(() => {
        if (!this.draftTouched) {
          this.draft.set(createDefaultTerritoryDirectoryFilter(context));
        }
      });
    });
  }

  protected itemKeys(): string[] {
    return revealedItemKeys(this.context());
  }

  protected isSelected(key: FilterIdListKey, id: string): boolean {
    return this.draft()[key].includes(id);
  }

  protected toggle(key: FilterIdListKey, id: string): void {
    this.draftTouched = true;
    this.draft.update((filter) => {
      const current = filter[key];
      const next = current.includes(id) ? current.filter((item) => item !== id) : [...current, id];
      return { ...filter, [key]: next };
    });
  }

  protected triValue(key: FilterTriKey): TerritoryFilterTriState {
    return this.draft()[key];
  }

  protected setTri(key: FilterTriKey, event: Event): void {
    const target = event.target;
    if (!(target instanceof HTMLSelectElement)) {
      return;
    }

    const value = target.value;
    if (value !== 'any' && value !== 'yes' && value !== 'no') {
      return;
    }

    this.draftTouched = true;
    this.draft.update((filter) => ({ ...filter, [key]: value }));
  }

  protected applyFilter(): void {
    this.applied.emit(cloneTerritoryDirectoryFilter(this.draft()));
  }

  protected clearFilter(): void {
    this.draftTouched = false;
    const next = createDefaultTerritoryDirectoryFilter(this.context());
    this.draft.set(next);
    this.applied.emit(cloneTerritoryDirectoryFilter(next));
  }
}
