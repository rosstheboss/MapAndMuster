import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import type { CampaignFaction, CampaignStructureType } from '../../core/campaigns/campaign.models';
import type { MapTerritory } from '../../core/maps/map-graph.models';
import { TerritoryListItemComponent, territoryListItemMarks, type TerritoryListItemMarks } from './territory-list-item';

const png =
  'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==';

const north: CampaignFaction = {
  id: 'north',
  name: 'North',
  color: '#2563EB',
  subfactions: [],
  allyGroupName: null,
  requiresSubfaction: false,
  hasFlagImage: false,
};

const town: CampaignStructureType = {
  id: 'town',
  name: 'Town',
  builtinSymbol: 'Town',
  hasImage: false,
  hasPillagedImage: false,
  isBuildable: false,
  isPillageable: true,
  isDestructible: true,
  missions: [],
};

const plains = { id: 'plains', name: 'Plains' };

function territory(overrides: Partial<MapTerritory> = {}): MapTerritory {
  return {
    id: 't1',
    displayNumber: 1,
    name: 'Coast',
    description: null,
    polygon: [
      { x: 0.1, y: 0.1 },
      { x: 0.4, y: 0.1 },
      { x: 0.4, y: 0.4 },
      { x: 0.1, y: 0.4 },
    ],
    terrainTypeId: 'plains',
    structureTypeId: null,
    structureCondition: 'Operational',
    overlayColor: null,
    ownerFactionId: null,
    spawnFactionId: null,
    ...overrides,
  };
}

function marksFor(
  overrides: Partial<MapTerritory> = {},
  extra?: Partial<Parameters<typeof territoryListItemMarks>[1]>,
): TerritoryListItemMarks {
  return territoryListItemMarks(territory(overrides), {
    factions: [north],
    terrainTypes: [plains],
    structures: [town],
    flagImageUrl: () => png,
    structureImageUrl: () => null,
    ...extra,
  });
}

describe('territoryListItemMarks', () => {
  it('uses terrain by default and omits structure and owner when absent', () => {
    expect(marksFor()).toEqual({
      label: 'Coast',
      terrainSymbol: 'Plains',
      structureSymbol: null,
      structureImageUrl: null,
      structurePillaged: false,
      ownerLogoSrc: null,
      ownerColor: null,
      ownerTint: false,
      ownerName: null,
    });
  });

  it('adds a structure symbol beside terrain and an owner color flag', () => {
    expect(
      marksFor({
        structureTypeId: 'town',
        ownerFactionId: 'north',
      }),
    ).toEqual({
      label: 'Coast',
      terrainSymbol: 'Plains',
      structureSymbol: 'Town',
      structureImageUrl: null,
      structurePillaged: false,
      ownerLogoSrc: null,
      ownerColor: '#2563EB',
      ownerTint: false,
      ownerName: 'North',
    });
  });

  it('omits a destroyed structure and tints an uploaded owner logo', () => {
    const owner: CampaignFaction = { ...north, hasFlagImage: true, tintFlagImage: true };
    const marks = marksFor(
      { structureTypeId: 'town', structureCondition: 'Destroyed', ownerFactionId: 'north' },
      { factions: [owner] },
    );
    expect(marks.structureSymbol).toBeNull();
    expect(marks.ownerLogoSrc).toBe(png);
    expect(marks.ownerTint).toBe(true);
    expect(marks.ownerColor).toBe('#2563EB');
  });
});

describe('TerritoryListItemComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TerritoryListItemComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
  });

  it('renders owner, structure, terrain, then name inside a bordered row', () => {
    const fixture = TestBed.createComponent(TerritoryListItemComponent);
    fixture.componentRef.setInput(
      'marks',
      marksFor({
        structureTypeId: 'town',
        ownerFactionId: 'north',
      }),
    );
    fixture.detectChanges();

    const button = (fixture.nativeElement as HTMLElement).querySelector('button')!;
    expect(button.classList.contains('territory-list-item')).toBe(true);
    expect(button.classList.contains('list-button')).toBe(true);
    const children = [...button.children].map((node) => node.tagName.toLowerCase());
    expect(children).toEqual(['span', 'app-map-symbol', 'app-map-symbol', 'span']);
    expect(button.querySelector('.owner-flag')).toBeTruthy();
    expect(button.querySelectorAll('app-map-symbol')).toHaveLength(2);
    expect(button.querySelector('.item-label')?.textContent.trim()).toBe('Coast');
  });

  it('exposes the territory hover tooltip on the row', () => {
    const fixture = TestBed.createComponent(TerritoryListItemComponent);
    fixture.componentRef.setInput('marks', marksFor());
    fixture.componentRef.setInput('tooltip', 'Coast\nOwner: Neutral');
    fixture.detectChanges();

    const button = (fixture.nativeElement as HTMLElement).querySelector('button')!;
    expect(button.getAttribute('title')).toBe('Coast\nOwner: Neutral');
  });
});
