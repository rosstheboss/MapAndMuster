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
    {
      id: 'north',
      name: 'North',
      color: '#2563EB',
      subfactions: [],
      allyGroupName: null,
      requiresSubfaction: false,
      hasFlagImage: false,
    },
    {
      id: 'south',
      name: 'South',
      color: '#DC2626',
      subfactions: [],
      allyGroupName: null,
      requiresSubfaction: false,
      hasFlagImage: false,
    },
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
    expect(compiled.textContent).toContain('Download map');
    expect(compiled.textContent).toContain('100%');
    expect(compiled.textContent).toContain('Fit to panel');
    expect(compiled.textContent).toContain('Zoom in');
    expect(compiled.textContent).toContain('800%');
    const zoomInput = compiled.querySelector<HTMLInputElement>('input[aria-label="Zoom percent"]');
    expect(zoomInput?.value).toBe('100');
    const zoomIn = [...compiled.querySelectorAll('button')].find((button) => button.textContent.includes('Zoom in'));
    zoomIn?.click();
    fixture.detectChanges();
    expect(compiled.querySelector<HTMLInputElement>('input[aria-label="Zoom percent"]')?.value).toBe('110');
    const hundred = [...compiled.querySelectorAll('button')].find((button) => button.textContent.trim() === '100%');
    hundred?.click();
    fixture.detectChanges();
    expect(compiled.querySelector<HTMLInputElement>('input[aria-label="Zoom percent"]')?.value).toBe('100');
    const fit = [...compiled.querySelectorAll('button')].find((button) => button.textContent.includes('Fit to panel'));
    expect(fit).toBeTruthy();
    fit?.click();
    fixture.detectChanges();
    expect(TERRAIN_TYPES.map((entry) => entry.label)).toEqual([
      'Beach',
      'Cave',
      'Desert',
      'Forest',
      'Highlands',
      'Jungle',
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

  it('keeps a clicked territory selected in select mode until empty map is clicked', async () => {
    const fixture = TestBed.createComponent(MapEditorPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaignId}`).flush(campaign);
    http.expectOne(`/api/campaigns/${campaignId}/map/graph`).flush({
      ...emptyGraph,
      territories: [
        {
          id: 't1',
          displayNumber: 1,
          name: 'Northmarch',
          description: null,
          polygon: [
            { x: 0.1, y: 0.1 },
            { x: 0.4, y: 0.1 },
            { x: 0.4, y: 0.4 },
            { x: 0.1, y: 0.4 },
          ],
          terrainTypeId: 'plains',
          structureTypeId: null,
          overlayColor: null,
          ownerFactionId: null,
          spawnFactionId: null,
        },
      ],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      onToolChange: (tool: string) => void;
      onTerritorySelect: (event: { id: string; additive: boolean }) => void;
      onBackground: () => void;
      selectedIds: () => string[];
    };
    page.onToolChange('select');
    page.onTerritorySelect({ id: 't1', additive: false });
    fixture.detectChanges();
    expect(page.selectedIds()).toEqual(['t1']);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Delete territory');
    page.onBackground();
    fixture.detectChanges();
    expect(page.selectedIds()).toEqual([]);
    http.verify();
  });

  it('clears the selection when empty map is clicked outside select mode', async () => {
    const fixture = TestBed.createComponent(MapEditorPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaignId}`).flush(campaign);
    http.expectOne(`/api/campaigns/${campaignId}/map/graph`).flush({
      ...emptyGraph,
      territories: [
        {
          id: 't1',
          displayNumber: 1,
          name: 'Northmarch',
          description: null,
          polygon: [
            { x: 0.1, y: 0.1 },
            { x: 0.4, y: 0.1 },
            { x: 0.4, y: 0.4 },
            { x: 0.1, y: 0.4 },
          ],
          terrainTypeId: 'plains',
          structureTypeId: null,
          overlayColor: null,
          ownerFactionId: null,
          spawnFactionId: null,
        },
      ],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      onToolChange: (tool: string) => void;
      onTerritorySelect: (event: { id: string; additive: boolean }) => void;
      onBackground: () => void;
      requestDownload: () => Promise<void>;
      colorRandom: () => void;
      selectedIds: () => string[];
      confirmingDownload: () => boolean;
    };
    page.onToolChange('draw');
    page.onTerritorySelect({ id: 't1', additive: false });
    fixture.detectChanges();
    expect(page.selectedIds()).toEqual(['t1']);
    page.onBackground();
    fixture.detectChanges();
    expect(page.selectedIds()).toEqual([]);

    page.colorRandom();
    await page.requestDownload();
    fixture.detectChanges();
    expect(page.confirmingDownload()).toBe(true);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Save map before downloading?');
    http.verify();
  });

  it('ctrl-selects, moves, and deletes territories together', async () => {
    const fixture = TestBed.createComponent(MapEditorPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaignId}`).flush(campaign);
    http.expectOne(`/api/campaigns/${campaignId}/map/graph`).flush({
      ...emptyGraph,
      territories: [
        {
          id: 't1',
          displayNumber: 1,
          name: 'Northmarch',
          description: null,
          polygon: [
            { x: 0.1, y: 0.1 },
            { x: 0.3, y: 0.1 },
            { x: 0.3, y: 0.3 },
            { x: 0.1, y: 0.3 },
          ],
          terrainTypeId: 'plains',
          structureTypeId: null,
          overlayColor: null,
          ownerFactionId: null,
          spawnFactionId: null,
        },
        {
          id: 't2',
          displayNumber: 2,
          name: 'Southmarch',
          description: null,
          polygon: [
            { x: 0.3, y: 0.1 },
            { x: 0.5, y: 0.1 },
            { x: 0.5, y: 0.3 },
            { x: 0.3, y: 0.3 },
          ],
          terrainTypeId: 'plains',
          structureTypeId: null,
          overlayColor: null,
          ownerFactionId: null,
          spawnFactionId: null,
        },
        {
          id: 't3',
          displayNumber: 3,
          name: 'Eastmarch',
          description: null,
          polygon: [
            { x: 0.58, y: 0.12 },
            { x: 0.73, y: 0.12 },
            { x: 0.73, y: 0.27 },
            { x: 0.58, y: 0.27 },
          ],
          terrainTypeId: 'plains',
          structureTypeId: null,
          overlayColor: null,
          ownerFactionId: null,
          spawnFactionId: null,
        },
      ],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      onToolChange: (tool: string) => void;
      onTerritorySelect: (event: { id: string; additive: boolean }) => void;
      onTerritoryMove: (event: { origin: { x: number; y: number }; current: { x: number; y: number } }) => void;
      onTerritoryMoveEnd: () => void;
      onKeydown: (event: KeyboardEvent) => void;
      deleteSelectedTerritories: () => void;
      deleteLabel: () => string;
      selectedIds: () => string[];
      graph: () => { territories: { id: string; polygon: { x: number; y: number }[] }[] };
    };
    const compiled = fixture.nativeElement as HTMLElement;

    page.onToolChange('select');
    page.onTerritorySelect({ id: 't1', additive: false });
    page.onTerritorySelect({ id: 't2', additive: true });
    fixture.detectChanges();
    expect(page.selectedIds()).toEqual(['t1', 't2']);
    expect(page.deleteLabel()).toBe('Delete territories');
    expect(compiled.textContent).toContain('Delete territories');

    page.onTerritoryMove({ origin: { x: 0.2, y: 0.2 }, current: { x: 0.3, y: 0.2 } });
    expect(page.graph().territories.find((territory) => territory.id === 't1')?.polygon[0]?.x).toBeCloseTo(0.1, 5);

    page.onTerritoryMove({ origin: { x: 0.2, y: 0.2 }, current: { x: 0.24, y: 0.2 } });
    page.onTerritoryMoveEnd();
    expect(page.graph().territories.find((territory) => territory.id === 't1')?.polygon[0]?.x).toBeCloseTo(0.14, 5);
    expect(page.graph().territories.find((territory) => territory.id === 't2')?.polygon[1]?.x).toBeCloseTo(0.54, 5);

    page.onKeydown(new KeyboardEvent('keydown', { key: 'Delete' }));
    fixture.detectChanges();
    expect(page.graph().territories.map((territory) => territory.id)).toEqual(['t3']);
    expect(page.selectedIds()).toEqual([]);
    http.verify();
  });
});
