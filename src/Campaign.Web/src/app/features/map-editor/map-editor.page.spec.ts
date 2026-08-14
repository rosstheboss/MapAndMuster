import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { TERRAIN_TYPES } from '../../core/maps/terrain';
import { STRUCTURE_TYPES } from '../../core/maps/structures';
import { MapEditorPage } from './map-editor.page';

const campaignId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

const campaign = {
  id: campaignId,
  name: 'Border War',
  description: null,
  playerSlotCount: 8,
  occupiedPlayerSlots: 1,
  isPrivate: false,
  creatorIsParticipant: true,
  hasMap: true,
  canManage: true,
  isParticipant: true,
  revision: 2,
  createdUtc: '2026-08-13T00:00:00+00:00',
  updatedUtc: '2026-08-13T00:00:00+00:00',
  factions: [
    { id: 'north', name: 'North', color: '#2563EB', subfactions: [], allyGroupName: null, requiresSubfaction: false },
    { id: 'south', name: 'South', color: '#DC2626', subfactions: [], allyGroupName: null, requiresSubfaction: false },
  ],
  allyGroups: [],
  links: [],
  terrainTypes: [
    {
      id: 'plains',
      name: 'Plains',
      color: '#7CB342',
      missions: [{ id: 'm1', name: 'Plains control', url: null, hasFile: false, fileName: null }],
    },
  ],
  structureTypes: [{ id: 'town', name: 'Town', builtinSymbol: 'Town', hasImage: false, missions: [] }],
  timeZoneId: 'UTC',
  startsAtLocal: '2099-01-05T12:00',
  startsUtc: '2099-01-05T12:00:00+00:00',
  endsUtc: '2099-03-02T12:00:00+00:00',
  roundCount: 8,
  roundLengthAmount: 1,
  roundLengthUnit: 'Weeks',
  phases: [],
  status: 'Scheduled',
  currentRound: null,
  currentPhaseNumber: null,
  currentPhaseKind: null,
  currentPhaseStartsUtc: null,
  currentPhaseEndsUtc: null,
};

const emptyGraph = {
  campaignId,
  revision: 2,
  canManage: true,
  territories: [],
  adjacencies: [],
};

describe('MapEditorPage', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MapEditorPage],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => campaignId } }, paramMap: of() },
        },
      ],
    }).compileComponents();
  });

  it('lists terrain and structures alphabetically and can generate connections', async () => {
    const fixture = TestBed.createComponent(MapEditorPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaignId}`).flush(campaign);
    http.expectOne(`/api/campaigns/${campaignId}/map/graph`).flush(emptyGraph);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Map editor');
    expect(compiled.textContent).toContain('Generate Connections');
    expect(compiled.textContent).toContain('Clear connections');
    expect(TERRAIN_TYPES.map((entry) => entry.label)).toEqual([
      'Beach',
      'Desert',
      'Highlands',
      'Lake',
      'Mountain',
      'Plains',
      'Riverlands',
      'Sea',
      'Swamp',
    ]);
    expect(STRUCTURE_TYPES.map((entry) => entry.label)).toEqual([
      'Capital City',
      'Castle',
      'City',
      'Fortification',
      'Supply Depot',
      'Town',
    ]);

    const generate = [...compiled.querySelectorAll('button')].find((button) =>
      button.textContent.includes('Generate Connections'),
    );
    expect(generate).toBeTruthy();
    generate?.click();
    fixture.detectChanges();
    http.verify();
  });
});
