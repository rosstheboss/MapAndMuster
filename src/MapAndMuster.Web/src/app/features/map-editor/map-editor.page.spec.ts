import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { AuthService } from '../../core/auth/auth.service';
import type { OwnProfile } from '../../core/auth/auth.models';
import type { MapPoint } from '../../core/maps/geometry';
import { OVERLAY_COLOR_MODE_STORAGE_PREFIX } from '../../core/maps/map-editor-preferences';
import type { MapTerritory } from '../../core/maps/map-graph.models';
import { serializeMapSvg } from '../../core/maps/map-svg';
import { STRUCTURE_TYPES } from '../../core/maps/structures';
import { TERRAIN_TYPES } from '../../core/maps/terrain';
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
  structureTypes: [
    {
      id: 'town',
      name: 'Town',
      builtinSymbol: 'Town',
      hasImage: false,
      hasPillagedImage: false,
      isBuildable: false,
      isPillageable: true,
      isDestructible: true,
      missions: [],
    },
  ],
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
  factionId: null,
  subfaction: null,
  canPlay: false,
  canChooseFaction: false,
  canChat: true,
  mentionableMembers: [],
  log: [],
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
    localStorage.removeItem(OVERLAY_COLOR_MODE_STORAGE_PREFIX + campaignId);
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
    expect(compiled.textContent).toContain('Auto Generate Connections');
    expect(compiled.textContent).not.toContain('Connect selected');
    expect(compiled.textContent).not.toContain('Cancel Drawing');
    expect(compiled.textContent).toContain('Clear Connections');
    expect(compiled.textContent).toContain('Undo');
    expect(compiled.textContent).toContain('Redo');
    expect(compiled.textContent).toContain('Manual Colors');
    expect(compiled.textContent).toContain('Clear Unsaved Changes');
    expect(compiled.textContent).toContain('Save Map');
    expect(compiled.textContent).toContain('Show Overlay');
    expect(compiled.textContent).toContain('Show Connections');
    expect(compiled.textContent).not.toContain('Draw on the overlay, not the image');
    expect(compiled.textContent).toContain('Download map');
    expect(compiled.textContent).toContain('Edit campaign');
    expect(compiled.textContent).not.toContain('Save as Preset');
    expect(compiled.textContent).toContain('100%');
    expect(compiled.textContent).toContain('Fit');
    expect(compiled.textContent).not.toContain('Fit to panel');
    expect(compiled.querySelector('button[aria-label="Zoom in"]')?.textContent).toContain('+');
    expect(compiled.querySelector('.map-viewport')?.getAttribute('aria-label')).toContain('800');
    const zoomInput = compiled.querySelector<HTMLInputElement>('input[aria-label="Zoom percent"]');
    expect(zoomInput?.value).toBe('100');
    const zoomIn = compiled.querySelector<HTMLButtonElement>('button[aria-label="Zoom in"]');
    zoomIn?.click();
    fixture.detectChanges();
    expect(compiled.querySelector<HTMLInputElement>('input[aria-label="Zoom percent"]')?.value).toBe('110');
    const hundred = [...compiled.querySelectorAll('button')].find((button) => button.textContent.trim() === '100%');
    hundred?.click();
    fixture.detectChanges();
    expect(compiled.querySelector<HTMLInputElement>('input[aria-label="Zoom percent"]')?.value).toBe('100');
    const fit = [...compiled.querySelectorAll('button')].find((button) => button.textContent.trim() === 'Fit');
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
      button.textContent.includes('Auto Generate Connections'),
    );
    expect(generate).toBeTruthy();
    expect(generate?.classList.contains('button-secondary')).toBe(true);
    expect(generate?.classList.contains('button')).toBe(false);
    const toolGroup = compiled.querySelector('[aria-label="Map tools"]');
    expect([...(toolGroup?.querySelectorAll('button') ?? [])].map((button) => button.textContent.trim())).toEqual([
      'Draw',
      'Erase',
      'Select',
      'Connect',
    ]);
    expect(toolGroup?.querySelector('[aria-checked="true"]')?.textContent.trim()).toBe('Draw');
    expect(toolGroup?.querySelector('.is-active')?.textContent.trim()).toBe('Draw');
    const colorGroup = compiled.querySelector('[aria-label="Overlay color mode"]');
    expect([...(colorGroup?.querySelectorAll('button') ?? [])].map((button) => button.textContent.trim())).toEqual([
      'Random Colors',
      'Color By Terrain',
      'Manual Colors',
    ]);
    expect(colorGroup?.querySelector('[aria-checked="true"]')?.textContent.trim()).toBe('Manual Colors');
    generate?.click();
    fixture.detectChanges();
    http.verify();
  });

  it('lets an administrator save the current map as a preset', async () => {
    const fixture = TestBed.createComponent(MapEditorPage);
    TestBed.inject(AuthService).currentUser.set(administratorProfile());
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaignId}`).flush(campaign);
    http.expectOne(`/api/campaigns/${campaignId}/map/graph`).flush(emptyGraph);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.page-header')?.textContent).toContain('Save as Preset');
    const savePreset = [...compiled.querySelectorAll('button')].find(
      (button) => button.textContent.trim() === 'Save as Preset',
    );
    savePreset?.click();
    fixture.detectChanges();
    http.expectOne('/api/campaign-presets').flush([]);
    await fixture.whenStable();
    fixture.detectChanges();

    expect(compiled.querySelector('#savePresetName')).toBeTruthy();
    http.verify();
  });

  it('redoes a graph change that was undone', async () => {
    const fixture = TestBed.createComponent(MapEditorPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaignId}`).flush(campaign);
    http.expectOne(`/api/campaigns/${campaignId}/map/graph`).flush({
      ...emptyGraph,
      territories: [namedSquare('t1', 1, 'Northmarch', 0.1), namedSquare('t2', 2, 'Southmarch', 0.3)],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      generateConnections: () => void;
      undo: () => void;
      redo: () => void;
      canRedoHistory: () => boolean;
      graph: () => { adjacencies: { id: string }[] };
    };
    page.generateConnections();
    expect(page.graph().adjacencies.length).toBeGreaterThan(0);
    page.undo();
    expect(page.graph().adjacencies).toHaveLength(0);
    expect(page.canRedoHistory()).toBe(true);
    page.redo();
    expect(page.graph().adjacencies.length).toBeGreaterThan(0);
    http.verify();
  });

  it('assigns a placed item objective to the selected territory', async () => {
    const fixture = TestBed.createComponent(MapEditorPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaignId}`).flush({
      ...campaign,
      itemObjectiveTypes: [
        {
          id: 'crown',
          name: 'Crown',
          isHiddenUntilFound: true,
          placement: 'Placed',
          allowOnSpawn: false,
        },
      ],
    });
    http.expectOne(`/api/campaigns/${campaignId}/map/graph`).flush({
      ...emptyGraph,
      territories: [namedSquare('t1', 1, 'Northmarch', 0.1)],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      selectedIds: { set: (ids: string[]) => void };
      setItemPlacedHere: (typeId: string, placed: boolean) => void;
      graph: () => { itemObjectivePlacements?: { typeId: string; territoryId: string }[] };
    };
    page.selectedIds.set(['t1']);
    page.setItemPlacedHere('crown', true);
    fixture.detectChanges();
    expect(page.graph().itemObjectivePlacements).toEqual([{ typeId: 'crown', territoryId: 't1' }]);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Place Crown here');
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
          structureCondition: 'Operational',
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

  it('lets a manager mark a placed structure as pillaged', async () => {
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
          structureTypeId: 'town',
          structureCondition: 'Pillaged',
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
      selected: () => { structureTypeId: string | null; structureCondition: string } | null;
    };
    page.onToolChange('select');
    page.onTerritorySelect({ id: 't1', additive: false });
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(page.selected()?.structureCondition).toBe('Pillaged');
    const condition = compiled.querySelector<HTMLSelectElement>('#territory-structure-condition');
    expect(condition).toBeTruthy();
    expect(condition?.value).toBe('Pillaged');
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
          structureCondition: 'Operational',
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
          structureCondition: 'Operational',
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
          structureCondition: 'Operational',
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
          structureCondition: 'Operational',
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
      movePlacement: () => 'valid' | 'invalid' | null;
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
    expect(page.graph().territories.find((territory) => territory.id === 't1')?.polygon[0]?.x).toBeCloseTo(0.2, 5);
    expect(page.movePlacement()).toBe('invalid');

    page.onTerritoryMove({ origin: { x: 0.2, y: 0.2 }, current: { x: 0.24, y: 0.2 } });
    expect(page.movePlacement()).toBe('valid');
    page.onTerritoryMoveEnd();
    expect(page.movePlacement()).toBeNull();
    expect(page.graph().territories.find((territory) => territory.id === 't1')?.polygon[0]?.x).toBeCloseTo(0.14, 5);
    expect(page.graph().territories.find((territory) => territory.id === 't2')?.polygon[1]?.x).toBeCloseTo(0.54, 5);

    page.onKeydown(new KeyboardEvent('keydown', { key: 'Delete' }));
    fixture.detectChanges();
    expect(page.graph().territories.map((territory) => territory.id)).toEqual(['t3']);
    expect(page.selectedIds()).toEqual([]);
    http.verify();
  });

  it('restores a moved group when it is dropped in an invalid place', async () => {
    const fixture = TestBed.createComponent(MapEditorPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaignId}`).flush(campaign);
    http.expectOne(`/api/campaigns/${campaignId}/map/graph`).flush({
      ...emptyGraph,
      territories: [
        namedSquare('t1', 1, 'Northmarch', 0.1),
        namedSquare('t2', 2, 'Southmarch', 0.4),
        namedSquare('t3', 3, 'Eastmarch', 0.7),
      ],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      onToolChange: (tool: string) => void;
      onTerritorySelect: (event: { id: string; additive: boolean }) => void;
      onTerritoryMove: (event: { origin: { x: number; y: number }; current: { x: number; y: number } }) => void;
      onTerritoryMoveEnd: () => void;
      graph: () => { territories: { id: string; polygon: { x: number; y: number }[] }[] };
    };

    page.onToolChange('select');
    page.onTerritorySelect({ id: 't1', additive: false });
    page.onTerritoryMove({ origin: { x: 0.2, y: 0.2 }, current: { x: 0.45, y: 0.2 } });
    expect(page.graph().territories.find((territory) => territory.id === 't1')?.polygon[0]?.x).toBeCloseTo(0.35, 5);
    page.onTerritoryMoveEnd();
    expect(page.graph().territories.find((territory) => territory.id === 't1')?.polygon[0]?.x).toBeCloseTo(0.1, 5);
    http.verify();
  });

  it('does not close a drawing when the pointer is released', async () => {
    const fixture = TestBed.createComponent(MapEditorPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaignId}`).flush(campaign);
    http.expectOne(`/api/campaigns/${campaignId}/map/graph`).flush(emptyGraph);
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      drawing: { (): MapPoint[]; set(points: MapPoint[]): void };
      drawingActive: { set(value: boolean): void };
      onPointerUp: () => void;
      graph: () => { territories: { id: string }[] };
    };
    page.drawing.set([
      { x: 0, y: 0.3 },
      { x: 0.2, y: 0.5 },
      { x: 0, y: 0.7 },
    ]);
    page.drawingActive.set(true);
    page.onPointerUp();
    expect(page.drawing()).toEqual([
      { x: 0, y: 0.3 },
      { x: 0.2, y: 0.5 },
      { x: 0, y: 0.7 },
    ]);
    expect(page.graph().territories).toHaveLength(0);
    http.verify();
  });

  it('closes along a touched border only when Close Territory is used', async () => {
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
            { x: 0.1, y: 0.4 },
            { x: 0.4, y: 0.4 },
            { x: 0.4, y: 0.7 },
            { x: 0.1, y: 0.7 },
          ],
          terrainTypeId: 'plains',
          structureTypeId: null,
          structureCondition: 'Operational',
          overlayColor: null,
          ownerFactionId: null,
          spawnFactionId: null,
        },
      ],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      drawing: { (): MapPoint[]; set(points: MapPoint[]): void };
      closePolygon: () => void;
      graph: () => { territories: { id: string }[] };
    };
    page.drawing.set([
      { x: 0.1, y: 0.4 },
      { x: 0.25, y: 0.2 },
      { x: 0.4, y: 0.4 },
    ]);
    page.closePolygon();
    expect(page.drawing()).toEqual([]);
    expect(page.graph().territories).toHaveLength(2);
    http.verify();
  });

  it('encloses along the map image edge when Close Territory is pressed', async () => {
    const fixture = TestBed.createComponent(MapEditorPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaignId}`).flush(campaign);
    http.expectOne(`/api/campaigns/${campaignId}/map/graph`).flush(emptyGraph);
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      drawing: { (): MapPoint[]; set(points: MapPoint[]): void };
      closePolygon: () => void;
      graph: () => { territories: { id: string }[] };
    };
    page.drawing.set([
      { x: 0, y: 0.3 },
      { x: 0, y: 0.7 },
    ]);
    page.closePolygon();
    expect(page.drawing()).toEqual([]);
    expect(page.graph().territories).toHaveLength(1);
    http.verify();
  });

  it('closes a neighbor that has extra vertices along a shared border', async () => {
    const fixture = TestBed.createComponent(MapEditorPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaignId}`).flush(campaign);
    http.expectOne(`/api/campaigns/${campaignId}/map/graph`).flush({
      ...emptyGraph,
      territories: [namedSquare('t1', 1, 'Northmarch', 0.1)],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      drawing: { set(points: MapPoint[]): void };
      closePolygon: () => void;
      graph: () => { territories: { id: string }[] };
      errorMessages: () => string[];
    };
    page.drawing.set([
      { x: 0.3, y: 0.1 },
      { x: 0.299, y: 0.2 },
      { x: 0.3, y: 0.3 },
      { x: 0.55, y: 0.3 },
      { x: 0.55, y: 0.1 },
    ]);
    page.closePolygon();
    expect(page.errorMessages()).toEqual([]);
    expect(page.graph().territories).toHaveLength(2);
    http.verify();
  });

  it('commits and can save a closed drawing that traces a shared border', async () => {
    const fixture = TestBed.createComponent(MapEditorPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaignId}`).flush(campaign);
    http.expectOne(`/api/campaigns/${campaignId}/map/graph`).flush({
      ...emptyGraph,
      territories: [namedSquare('t1', 1, 'Northmarch', 0.1)],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      drawing: { set(points: MapPoint[]): void };
      closePolygon: () => void;
      save: () => Promise<boolean>;
      graph: () => { territories: { id: string }[]; adjacencies: unknown[]; itemObjectivePlacements?: unknown[] };
      errorMessages: () => string[];
    };
    page.drawing.set([
      { x: 0.4, y: 0.1 },
      { x: 0.399, y: 0.2 },
      { x: 0.4, y: 0.4 },
      { x: 0.7, y: 0.4 },
      { x: 0.7, y: 0.1 },
    ]);
    page.closePolygon();
    expect(page.errorMessages()).toEqual([]);
    expect(page.graph().territories).toHaveLength(2);

    const saving = page.save();
    http.expectOne(`/api/campaigns/${campaignId}/map/graph`).flush({
      ...emptyGraph,
      revision: 3,
      territories: page.graph().territories,
      adjacencies: page.graph().adjacencies,
    });
    await expect(saving).resolves.toBe(true);
    expect(page.errorMessages()).toEqual([]);
    http.verify();
  });

  it('rejects a closed drawing that covers another territory interior', async () => {
    const fixture = TestBed.createComponent(MapEditorPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaignId}`).flush(campaign);
    http.expectOne(`/api/campaigns/${campaignId}/map/graph`).flush({
      ...emptyGraph,
      territories: [namedSquare('t1', 1, 'Northmarch', 0.1)],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      drawing: { set(points: MapPoint[]): void };
      closePolygon: () => void;
      graph: () => { territories: { id: string }[] };
      errorMessages: () => string[];
    };
    page.drawing.set([
      { x: 0.3, y: 0.1 },
      { x: 0.2, y: 0.2 },
      { x: 0.3, y: 0.3 },
      { x: 0.55, y: 0.3 },
      { x: 0.55, y: 0.1 },
    ]);
    page.closePolygon();
    expect(page.errorMessages()).toEqual(['Territories cannot overlap. They may share a border.']);
    expect(page.graph().territories).toHaveLength(1);
    http.verify();
  });

  it('applies terrain or random overlay colors from the active color mode', async () => {
    const fixture = TestBed.createComponent(MapEditorPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaignId}`).flush(campaign);
    http.expectOne(`/api/campaigns/${campaignId}/map/graph`).flush({
      ...emptyGraph,
      territories: [namedSquare('t1', 1, 'Northmarch', 0.1)],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      colorByTerrain: () => void;
      colorRandom: () => void;
      colorClear: () => void;
      colorMode: () => string;
      setTerrain: (value: string) => void;
      onTerritorySelect: (event: { id: string; additive: boolean }) => void;
      graph: () => { territories: { overlayColor: string | null }[] };
    };

    page.colorByTerrain();
    expect(page.colorMode()).toBe('terrain');
    expect(page.graph().territories[0]?.overlayColor).toBe('#7CB342');
    page.onTerritorySelect({ id: 't1', additive: false });
    page.setTerrain('plains');
    expect(page.graph().territories[0]?.overlayColor).toBe('#7CB342');

    page.colorRandom();
    expect(page.colorMode()).toBe('random');
    expect(page.graph().territories[0]?.overlayColor).toMatch(/^#[0-9A-F]{6}$/);

    page.colorClear();
    expect(page.colorMode()).toBe('manual');
    expect(page.graph().territories[0]?.overlayColor).toBeNull();
    http.verify();
  });

  it('keeps zoom after save and can discard unsaved overlay edits', async () => {
    const fixture = TestBed.createComponent(MapEditorPage);
    const http = TestBed.inject(HttpTestingController);
    const graph = {
      ...emptyGraph,
      territories: [namedSquare('t1', 1, 'Northmarch', 0.1)],
    };
    http.expectOne(`/api/campaigns/${campaignId}`).flush(campaign);
    http.expectOne(`/api/campaigns/${campaignId}/map/graph`).flush(graph);
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      colorRandom: () => void;
      discardUnsavedChanges: () => void;
      save: () => Promise<boolean>;
      hasUnsavedEdits: () => boolean;
      lastSavedAtUtc: () => string | null;
      mapSrc: () => string | null;
      graph: () => { territories: { overlayColor: string | null }[] };
    };
    const compiled = fixture.nativeElement as HTMLElement;
    const save = [...compiled.querySelectorAll('button')].find((button) => button.textContent.trim() === 'Save Map');
    const discard = [...compiled.querySelectorAll('button')].find((button) =>
      button.textContent.includes('Clear Unsaved Changes'),
    );
    expect(save?.disabled).toBe(true);
    expect(discard?.disabled).toBe(true);

    const zoomIn = compiled.querySelector<HTMLButtonElement>('button[aria-label="Zoom in"]');
    zoomIn?.click();
    fixture.detectChanges();
    expect(compiled.querySelector<HTMLInputElement>('input[aria-label="Zoom percent"]')?.value).toBe('110');

    page.colorRandom();
    fixture.detectChanges();
    expect(page.hasUnsavedEdits()).toBe(true);
    expect(save?.disabled).toBe(false);
    const mapSrc = page.mapSrc();

    const saving = page.save();
    http.expectOne(`/api/campaigns/${campaignId}/map/graph`).flush({ ...graph, revision: 3 });
    await saving;
    fixture.detectChanges();

    expect(page.hasUnsavedEdits()).toBe(false);
    expect(page.mapSrc()).toBe(mapSrc);
    expect(compiled.querySelector<HTMLInputElement>('input[aria-label="Zoom percent"]')?.value).toBe('110');
    expect(compiled.textContent).toContain('Successfully saved changes.');
    expect(compiled.textContent).toContain('Last saved');
    expect(page.lastSavedAtUtc()).toBeTruthy();
    expect(compiled.querySelector('.save-status.is-success')).toBeTruthy();
    const savedColor = page.graph().territories[0]?.overlayColor;

    page.colorRandom();
    fixture.detectChanges();
    expect(page.graph().territories[0]?.overlayColor).not.toBe(savedColor);
    page.discardUnsavedChanges();
    fixture.detectChanges();
    expect(page.graph().territories[0]?.overlayColor).toBe(savedColor);
    expect(compiled.querySelector('.save-status')).toBeNull();
    expect(compiled.querySelector<HTMLInputElement>('input[aria-label="Zoom percent"]')?.value).toBe('110');
    http.verify();
  });

  it('marks a changed territory field as dirty', async () => {
    const fixture = TestBed.createComponent(MapEditorPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaignId}`).flush(campaign);
    http.expectOne(`/api/campaigns/${campaignId}/map/graph`).flush({
      ...emptyGraph,
      territories: [namedSquare('t1', 1, 'Northmarch', 0.1)],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      onTerritorySelect: (event: { id: string; additive: boolean }) => void;
      setName: (value: string) => void;
      toggleTerritoryList: () => void;
      isTerritoryFieldDirty: (key: string) => boolean;
      isTerritoryDirty: (id: string) => boolean;
    };
    page.onTerritorySelect({ id: 't1', additive: false });
    page.toggleTerritoryList();
    fixture.detectChanges();
    expect(page.isTerritoryFieldDirty('name')).toBe(false);

    page.setName('Westmarch');
    fixture.detectChanges();
    expect(page.isTerritoryFieldDirty('name')).toBe(true);
    expect(page.isTerritoryDirty('t1')).toBe(true);
    const compiled = fixture.nativeElement as HTMLElement;
    const nameField = compiled.querySelector('#territory-name')?.closest('.field');
    expect(nameField?.classList.contains('is-dirty')).toBe(true);
    expect(compiled.querySelector('h1')?.classList.contains('has-dirty')).toBe(false);
    expect(compiled.querySelector('.list-button.is-dirty')).toBeTruthy();
    http.verify();
  });

  it('toggles a single active map tool in the grouped tools', async () => {
    const fixture = TestBed.createComponent(MapEditorPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaignId}`).flush(campaign);
    http.expectOne(`/api/campaigns/${campaignId}/map/graph`).flush(emptyGraph);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const select = [...compiled.querySelectorAll<HTMLButtonElement>('[aria-label="Map tools"] button')].find(
      (button) => button.textContent.trim() === 'Select',
    );
    expect(select).toBeTruthy();
    select?.click();
    fixture.detectChanges();

    const tools = [...compiled.querySelectorAll('[aria-label="Map tools"] button')];
    expect(
      tools.filter((button) => button.classList.contains('is-active')).map((button) => button.textContent.trim()),
    ).toEqual(['Select']);
    expect(
      tools
        .filter((button) => button.getAttribute('aria-checked') === 'true')
        .map((button) => button.textContent.trim()),
    ).toEqual(['Select']);
    http.verify();
  });

  it('does not hover-lift territories while Draw is active', async () => {
    const fixture = TestBed.createComponent(MapEditorPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaignId}`).flush(campaign);
    http.expectOne(`/api/campaigns/${campaignId}/map/graph`).flush({
      ...emptyGraph,
      territories: [namedSquare('t1', 1, 'Northmarch', 0.1)],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      onTerritoryHover: (id: string | null) => void;
      onToolChange: (tool: string) => void;
      hoveredTerritoryId: () => string | null;
    };
    const compiled = fixture.nativeElement as HTMLElement;

    page.onTerritoryHover('t1');
    fixture.detectChanges();
    expect(page.hoveredTerritoryId()).toBeNull();
    expect(compiled.querySelector('.territory[data-id="t1"]')?.classList.contains('is-half-highlighted')).toBe(false);

    page.onToolChange('select');
    page.onTerritoryHover('t1');
    fixture.detectChanges();
    expect(page.hoveredTerritoryId()).toBe('t1');
    expect(compiled.querySelector('.territory[data-id="t1"]')?.classList.contains('is-half-highlighted')).toBe(true);

    page.onToolChange('draw');
    fixture.detectChanges();
    expect(page.hoveredTerritoryId()).toBeNull();
    expect(compiled.querySelector('.territory[data-id="t1"]')?.classList.contains('is-half-highlighted')).toBe(false);
    http.verify();
  });

  it('does not show hover placeholder copy or swap the editor when hovering an unselected territory', async () => {
    const fixture = TestBed.createComponent(MapEditorPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaignId}`).flush(campaign);
    http.expectOne(`/api/campaigns/${campaignId}/map/graph`).flush({
      ...emptyGraph,
      territories: [namedSquare('t1', 1, 'Northmarch', 0.1)],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      onToolChange: (tool: string) => void;
      onTerritoryHover: (id: string | null) => void;
      onTerritorySelect: (event: { id: string; additive: boolean }) => void;
      onBackground: () => void;
    };
    const compiled = fixture.nativeElement as HTMLElement;
    const editorText = (): string => compiled.querySelector('.territory-editor')?.textContent ?? '';
    const body = compiled.querySelector('.territory-editor-body');
    page.onToolChange('select');
    fixture.detectChanges();

    expect(editorText()).not.toContain('Hover or select a territory to see its details.');
    expect(compiled.querySelector('#territory-name')).toBeNull();
    expect(body).toBeTruthy();

    page.onTerritoryHover('t1');
    fixture.detectChanges();
    expect(editorText()).not.toContain('Hover or select a territory to see its details.');
    expect(compiled.querySelector('#territory-name')).toBeNull();
    expect(compiled.querySelector('.territory-editor-body')).toBe(body);

    page.onTerritorySelect({ id: 't1', additive: false });
    fixture.detectChanges();
    expect(compiled.querySelector('#territory-name')).toBeTruthy();
    expect(compiled.querySelector('.territory-editor-body')).toBeTruthy();

    page.onBackground();
    fixture.detectChanges();
    expect(compiled.querySelector('#territory-name')).toBeNull();
    expect(compiled.querySelector('.territory-editor-body')).toBeTruthy();
    expect(editorText()).not.toContain('Hover or select a territory to see its details.');
    http.verify();
  });

  it('restores the last overlay color mode for the campaign without recoloring', async () => {
    const fixture = TestBed.createComponent(MapEditorPage);
    const http = TestBed.inject(HttpTestingController);
    const graph = {
      ...emptyGraph,
      territories: [namedSquare('t1', 1, 'Northmarch', 0.1)],
    };
    http.expectOne(`/api/campaigns/${campaignId}`).flush(campaign);
    http.expectOne(`/api/campaigns/${campaignId}/map/graph`).flush(graph);
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      colorByTerrain: () => void;
      colorMode: () => string;
    };
    page.colorByTerrain();
    expect(page.colorMode()).toBe('terrain');
    fixture.destroy();

    const next = TestBed.createComponent(MapEditorPage);
    http.expectOne(`/api/campaigns/${campaignId}`).flush(campaign);
    http.expectOne(`/api/campaigns/${campaignId}/map/graph`).flush(graph);
    await next.whenStable();
    next.detectChanges();

    const restored = next.componentInstance as unknown as {
      colorMode: () => string;
      hasUnsavedEdits: () => boolean;
      graph: () => { territories: { overlayColor: string | null }[] };
    };
    expect(restored.colorMode()).toBe('terrain');
    expect(restored.hasUnsavedEdits()).toBe(false);
    expect(restored.graph().territories[0]?.overlayColor).toBeNull();
    const colorGroup = (next.nativeElement as HTMLElement).querySelector('[aria-label="Overlay color mode"]');
    expect(colorGroup?.querySelector('[aria-checked="true"]')?.textContent.trim()).toBe('Color By Terrain');
    expect(colorGroup?.querySelector('.is-active')?.textContent.trim()).toBe('Color By Terrain');
    http.verify();
  });

  it('connects two selected territories and rejects a second connection for the same pair', async () => {
    const fixture = TestBed.createComponent(MapEditorPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaignId}`).flush(campaign);
    http.expectOne(`/api/campaigns/${campaignId}/map/graph`).flush({
      ...emptyGraph,
      territories: [namedSquare('t1', 1, 'Northmarch', 0.1), namedSquare('t2', 2, 'Southmarch', 0.4)],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      onToolChange: (tool: string) => void;
      onTerritorySelect: (event: { id: string; additive: boolean }) => void;
      onConnectClick: () => void;
      connectSelectedTerritories: () => void;
      graph: () => { adjacencies: { id: string; territoryAId: string; territoryBId: string }[] };
      selectedAdjacencyId: () => string | null;
      errorMessages: () => string[];
    };
    const compiled = fixture.nativeElement as HTMLElement;

    page.onToolChange('select');
    page.onTerritorySelect({ id: 't1', additive: false });
    page.onTerritorySelect({ id: 't2', additive: true });
    fixture.detectChanges();
    expect(compiled.querySelector('.territory-editor .section-toggle')?.textContent).toContain('Selected territories');
    expect(compiled.textContent).toContain('Northmarch');
    expect(compiled.textContent).toContain('Southmarch');
    page.onConnectClick();
    fixture.detectChanges();

    expect(page.graph().adjacencies).toHaveLength(1);
    expect(page.graph().adjacencies[0]).toMatchObject({ territoryAId: 't1', territoryBId: 't2' });
    expect(page.selectedAdjacencyId()).toBe(page.graph().adjacencies[0]?.id ?? null);
    expect(compiled.querySelector('.territory-editor .section-toggle')?.textContent).toContain('Connection');
    expect(compiled.textContent).toContain('Northmarch');
    expect(compiled.textContent).toContain('Southmarch');

    page.onToolChange('select');
    page.onTerritorySelect({ id: 't1', additive: false });
    page.onTerritorySelect({ id: 't2', additive: true });
    page.connectSelectedTerritories();
    fixture.detectChanges();
    expect(page.errorMessages()).toContain('Those territories already have a connection.');
    expect(page.graph().adjacencies).toHaveLength(1);
    http.verify();
  });

  it('shows and deletes a connection when two connected territories are selected', async () => {
    const fixture = TestBed.createComponent(MapEditorPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaignId}`).flush(campaign);
    http.expectOne(`/api/campaigns/${campaignId}/map/graph`).flush({
      ...emptyGraph,
      territories: [namedSquare('t1', 1, 'Northmarch', 0.1), namedSquare('t2', 2, 'Southmarch', 0.4)],
      adjacencies: [
        {
          id: 'ab',
          territoryAId: 't1',
          territoryBId: 't2',
          origin: 'Manual',
          marker: { x: 0.4, y: 0.2 },
        },
      ],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      onToolChange: (tool: string) => void;
      onTerritorySelect: (event: { id: string; additive: boolean }) => void;
      graph: () => { adjacencies: { id: string }[] };
    };
    const compiled = fixture.nativeElement as HTMLElement;

    page.onToolChange('select');
    page.onTerritorySelect({ id: 't1', additive: false });
    page.onTerritorySelect({ id: 't2', additive: true });
    fixture.detectChanges();

    expect(compiled.textContent).toContain('These territories are connected.');
    const deleteConnection = [...compiled.querySelectorAll<HTMLButtonElement>('button')].find(
      (button) => button.textContent?.trim() === 'Delete connection',
    );
    expect(deleteConnection).toBeTruthy();
    deleteConnection?.click();
    fixture.detectChanges();

    expect(page.graph().adjacencies).toHaveLength(0);
    expect(compiled.textContent).not.toContain('These territories are connected.');
    expect(compiled.textContent).toContain('Northmarch');
    expect(compiled.textContent).toContain('Southmarch');
    http.verify();
  });

  it('lists a selected territory connections and can remove one', async () => {
    const fixture = TestBed.createComponent(MapEditorPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaignId}`).flush(campaign);
    http.expectOne(`/api/campaigns/${campaignId}/map/graph`).flush({
      ...emptyGraph,
      territories: [
        namedSquare('t1', 1, 'Northmarch', 0.1),
        namedSquare('t2', 2, 'Southmarch', 0.4),
        namedSquare('t3', 3, 'Eastmarch', 0.7),
      ],
      adjacencies: [
        {
          id: 'ab',
          territoryAId: 't1',
          territoryBId: 't2',
          origin: 'Manual',
          marker: { x: 0.4, y: 0.2 },
        },
        {
          id: 'ac',
          territoryAId: 't1',
          territoryBId: 't3',
          origin: 'Manual',
          marker: { x: 0.5, y: 0.2 },
        },
      ],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      onToolChange: (tool: string) => void;
      onTerritorySelect: (event: { id: string; additive: boolean }) => void;
      graph: () => { adjacencies: { id: string }[] };
      selectedIds: () => string[];
    };
    const compiled = fixture.nativeElement as HTMLElement;

    page.onToolChange('select');
    page.onTerritorySelect({ id: 't1', additive: false });
    fixture.detectChanges();

    const connections = compiled.querySelector('.connection-list');
    expect(connections?.textContent).toContain('Eastmarch');
    expect(connections?.textContent).toContain('Southmarch');

    compiled.querySelector<HTMLButtonElement>('[aria-label="Remove connection to Southmarch"]')?.click();
    fixture.detectChanges();

    expect(page.graph().adjacencies.map((edge) => edge.id)).toEqual(['ac']);
    expect(compiled.querySelector('.connection-list')?.textContent).toContain('Eastmarch');
    expect(compiled.querySelector('.connection-list')?.textContent).not.toContain('Southmarch');

    compiled.querySelector<HTMLButtonElement>('[aria-label="Select Eastmarch"]')?.click();
    fixture.detectChanges();
    expect(page.selectedIds()).toEqual(['t3']);
    http.verify();
  });

  it('shows both connected territories and can reassign or delete the connection', async () => {
    const fixture = TestBed.createComponent(MapEditorPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaignId}`).flush(campaign);
    http.expectOne(`/api/campaigns/${campaignId}/map/graph`).flush({
      ...emptyGraph,
      territories: [
        namedSquare('t1', 1, 'Northmarch', 0.1),
        namedSquare('t2', 2, 'Southmarch', 0.4),
        namedSquare('t3', 3, 'Eastmarch', 0.7),
      ],
      adjacencies: [
        {
          id: 'ab',
          territoryAId: 't1',
          territoryBId: 't2',
          origin: 'Manual',
          marker: { x: 0.4, y: 0.2 },
        },
      ],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      onToolChange: (tool: string) => void;
      onAdjacencySelect: (id: string) => void;
      onAdjacencyHover: (id: string | null) => void;
      setAdjacencyEnd: (end: 'a' | 'b', territoryId: string) => void;
      deleteSelectedAdjacency: () => void;
      selectedAdjacencyId: () => string | null;
      adjacentTerritoryIds: () => string[];
      graph: () => { adjacencies: { id: string; territoryAId: string; territoryBId: string }[] };
    };
    const compiled = fixture.nativeElement as HTMLElement;

    page.onAdjacencySelect('ab');
    expect(page.selectedAdjacencyId()).toBeNull();

    page.onToolChange('select');
    page.onAdjacencyHover('ab');
    expect(page.adjacentTerritoryIds().sort()).toEqual(['t1', 't2']);

    page.onAdjacencySelect('ab');
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(compiled.querySelector('.territory-editor .section-toggle')?.textContent).toContain('Connection');
    expect(compiled.textContent).toContain('Northmarch');
    expect(compiled.textContent).toContain('Southmarch');
    expect(page.adjacentTerritoryIds().sort()).toEqual(['t1', 't2']);
    expect(compiled.querySelector<HTMLSelectElement>('#connection-territory-a')?.value).toBe('t1');
    expect(compiled.querySelector<HTMLSelectElement>('#connection-territory-b')?.value).toBe('t2');

    page.setAdjacencyEnd('b', 't3');
    fixture.detectChanges();
    expect(page.graph().adjacencies[0]).toMatchObject({ territoryAId: 't1', territoryBId: 't3' });
    expect(compiled.textContent).toContain('Eastmarch');

    page.deleteSelectedAdjacency();
    fixture.detectChanges();
    expect(page.graph().adjacencies).toHaveLength(0);
    expect(page.selectedAdjacencyId()).toBeNull();
    expect(compiled.querySelector('.territory-editor .section-toggle')?.textContent).toContain('Territory');
    http.verify();
  });

  it('deletes a connection with erase and ignores it while drawing', async () => {
    const fixture = TestBed.createComponent(MapEditorPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaignId}`).flush(campaign);
    http.expectOne(`/api/campaigns/${campaignId}/map/graph`).flush({
      ...emptyGraph,
      territories: [namedSquare('t1', 1, 'Northmarch', 0.1), namedSquare('t2', 2, 'Southmarch', 0.4)],
      adjacencies: [
        {
          id: 'ab',
          territoryAId: 't1',
          territoryBId: 't2',
          origin: 'Manual',
          marker: { x: 0.4, y: 0.2 },
        },
      ],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      onToolChange: (tool: string) => void;
      onAdjacencySelect: (id: string) => void;
      selectedAdjacencyId: () => string | null;
      graph: () => { adjacencies: { id: string }[] };
    };

    page.onToolChange('draw');
    page.onAdjacencySelect('ab');
    expect(page.selectedAdjacencyId()).toBeNull();
    expect(page.graph().adjacencies).toHaveLength(1);

    page.onToolChange('erase');
    page.onAdjacencySelect('ab');
    expect(page.graph().adjacencies).toHaveLength(0);
    http.verify();
  });

  it('collapses the territory editor and list and reports a failed save with an X', async () => {
    const fixture = TestBed.createComponent(MapEditorPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaignId}`).flush(campaign);
    http.expectOne(`/api/campaigns/${campaignId}/map/graph`).flush({
      ...emptyGraph,
      territories: [namedSquare('t1', 1, 'Northmarch', 0.1)],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const page = fixture.componentInstance as unknown as {
      onTerritorySelect: (event: { id: string; additive: boolean }) => void;
      colorRandom: () => void;
      save: () => Promise<boolean>;
    };
    page.onTerritorySelect({ id: 't1', additive: false });
    fixture.detectChanges();
    expect(compiled.querySelector('#territory-name')).toBeTruthy();

    const editorToggle = compiled.querySelector<HTMLButtonElement>('.territory-editor .section-toggle');
    const listToggle = compiled.querySelector<HTMLButtonElement>('.side-pane-toggle');
    expect(editorToggle?.getAttribute('aria-expanded')).toBe('true');
    expect(listToggle?.getAttribute('aria-expanded')).toBe('false');
    expect(compiled.querySelector('.territory-list')).toBeNull();
    listToggle?.click();
    fixture.detectChanges();
    expect(listToggle?.getAttribute('aria-expanded')).toBe('true');
    expect(compiled.querySelector('.territory-list')).toBeTruthy();
    editorToggle?.click();
    listToggle?.click();
    fixture.detectChanges();
    expect(editorToggle?.getAttribute('aria-expanded')).toBe('false');
    expect(listToggle?.getAttribute('aria-expanded')).toBe('false');
    expect(compiled.querySelector('#territory-name')).toBeNull();
    expect(compiled.querySelector('.territory-list')).toBeNull();
    page.colorRandom();
    const saving = page.save();
    http
      .expectOne(`/api/campaigns/${campaignId}/map/graph`)
      .flush({ title: 'Unable to save the map.' }, { status: 400, statusText: 'Bad Request' });
    await saving;
    fixture.detectChanges();
    expect(compiled.querySelector('.save-status.is-failure')).toBeTruthy();
    http.verify();
  });

  it('scrolls the expanded territory list to the topmost selected name', async () => {
    const scrollIntoView = vi.fn();
    HTMLElement.prototype.scrollIntoView = scrollIntoView;

    const fixture = TestBed.createComponent(MapEditorPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaignId}`).flush(campaign);
    http.expectOne(`/api/campaigns/${campaignId}/map/graph`).flush({
      ...emptyGraph,
      territories: [
        namedSquare('t1', 1, 'Northmarch', 0.1),
        namedSquare('t2', 2, 'Southmarch', 0.4),
        namedSquare('t3', 3, 'Eastmarch', 0.7),
      ],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      onToolChange: (tool: string) => void;
      onTerritorySelect: (event: { id: string; additive: boolean }) => void;
    };
    const compiled = fixture.nativeElement as HTMLElement;
    compiled.querySelector<HTMLButtonElement>('.side-pane-toggle')?.click();
    fixture.detectChanges();
    expect(compiled.querySelector('.territory-list')).toBeTruthy();

    page.onToolChange('select');
    page.onTerritorySelect({ id: 't2', additive: false });
    fixture.detectChanges();
    const south = compiled.querySelector<HTMLButtonElement>('[data-territory-id="t2"]');
    expect(scrollIntoView).toHaveBeenCalled();
    expect(scrollIntoView.mock.instances.at(-1)).toBe(south);

    scrollIntoView.mockClear();
    page.onTerritorySelect({ id: 't3', additive: true });
    fixture.detectChanges();
    const east = compiled.querySelector<HTMLButtonElement>('[data-territory-id="t3"]');
    expect(scrollIntoView.mock.instances.at(-1)).toBe(east);
    http.verify();
  });

  it('copies ownership from spawn and lists required subfactions while disabling random-spawn factions', async () => {
    const fixture = TestBed.createComponent(MapEditorPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaignId}`).flush({
      ...campaign,
      factions: [
        ...campaign.factions,
        {
          id: 'daemons',
          name: 'Daemons of Chaos',
          color: '#AD1457',
          subfactions: ['Khorne', 'Nurgle'],
          allyGroupName: null,
          requiresSubfaction: true,
          hasFlagImage: false,
        },
        {
          id: 'skaven',
          name: 'Skaven',
          color: '#78716C',
          subfactions: [],
          allyGroupName: null,
          requiresSubfaction: false,
          hasFlagImage: false,
          specialRuleIds: ['underground'],
        },
      ],
      specialRules: [{ id: 'underground', name: 'The Underground Network', text: '', effectKey: 'UndergroundNetwork' }],
    });
    http.expectOne(`/api/campaigns/${campaignId}/map/graph`).flush({
      ...emptyGraph,
      territories: [namedSquare('t1', 1, 'Northmarch', 0.1)],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      onTerritorySelect: (event: { id: string; additive: boolean }) => void;
      setSpawn: (value: string) => void;
      setOwner: (value: string) => void;
      ownerLocked: () => boolean;
      graph: () => {
        territories: {
          ownerFactionId: string | null;
          ownerSubfaction?: string | null;
          spawnFactionId: string | null;
          spawnSubfaction?: string | null;
        }[];
      };
    };
    page.onTerritorySelect({ id: 't1', additive: false });
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    const labels = [...compiled.querySelectorAll('#territory-spawn option')].map((option) => option.textContent.trim());
    expect(labels).toContain('Daemons of Chaos - Khorne');
    expect(labels).toContain('Daemons of Chaos - Nurgle');
    expect(labels).not.toContain('Daemons of Chaos');
    expect(compiled.querySelector('#territory-spawn option[value="skaven"]')?.hasAttribute('disabled')).toBe(true);

    page.setSpawn('daemons::Khorne');
    fixture.detectChanges();
    expect(page.graph().territories[0]?.ownerFactionId).toBe('daemons');
    expect(page.graph().territories[0]?.ownerSubfaction).toBe('Khorne');
    expect(page.graph().territories[0]?.spawnFactionId).toBe('daemons');
    expect(page.graph().territories[0]?.spawnSubfaction).toBe('Khorne');
    expect(compiled.querySelector('#territory-spawn option[value="daemons::Khorne"]')).toBeTruthy();
    expect(page.ownerLocked()).toBe(true);
    page.setOwner('north');
    expect(page.graph().territories[0]?.ownerFactionId).toBe('daemons');
    expect(compiled.querySelector('#territory-owner')?.getAttribute('disabled')).toBe('true');
    http.verify();
  });

  it('allows one spawn per required daemon subfaction without treating sibling gods as taken', async () => {
    const fixture = TestBed.createComponent(MapEditorPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaignId}`).flush({
      ...campaign,
      factions: [
        ...campaign.factions,
        {
          id: 'daemons',
          name: 'Daemons of Chaos',
          color: '#AD1457',
          subfactions: ['Khorne', 'Nurgle', 'Slaanesh', 'Tzeentch'],
          allyGroupName: null,
          requiresSubfaction: true,
          hasFlagImage: false,
        },
      ],
    });
    http.expectOne(`/api/campaigns/${campaignId}/map/graph`).flush({
      ...emptyGraph,
      territories: [namedSquare('t1', 1, 'Khornehold', 0.1), namedSquare('t2', 2, 'Nurglefen', 0.4)],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      onTerritorySelect: (event: { id: string; additive: boolean }) => void;
      setSpawn: (value: string) => void;
      save: () => Promise<boolean>;
      errorMessages: () => string[];
      graph: () => {
        territories: { spawnFactionId: string | null; spawnSubfaction?: string | null }[];
        adjacencies: unknown[];
      };
    };
    page.onTerritorySelect({ id: 't1', additive: false });
    page.setSpawn('daemons::Khorne');
    page.onTerritorySelect({ id: 't2', additive: false });
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('#territory-spawn option[value="daemons::Khorne"]')?.hasAttribute('disabled')).toBe(
      true,
    );
    expect(compiled.querySelector('#territory-spawn option[value="daemons::Nurgle"]')?.hasAttribute('disabled')).toBe(
      false,
    );

    page.setSpawn('daemons::Nurgle');
    expect(page.graph().territories.map((territory) => territory.spawnSubfaction)).toEqual(['Khorne', 'Nurgle']);

    const saving = page.save();
    const put = http.expectOne(`/api/campaigns/${campaignId}/map/graph`);
    const body = put.request.body as { territories: { spawnSubfaction: string | null }[] };
    expect(body.territories.map((territory) => territory.spawnSubfaction)).toEqual(['Khorne', 'Nurgle']);
    put.flush({
      ...emptyGraph,
      revision: 3,
      territories: page.graph().territories,
      adjacencies: page.graph().adjacencies,
    });
    await expect(saving).resolves.toBe(true);
    expect(page.errorMessages()).toEqual([]);
    http.verify();
  });

  it('remaps uploaded SVG catalog fields onto this campaign by name', async () => {
    const fixture = TestBed.createComponent(MapEditorPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaignId}`).flush(campaign);
    http.expectOne(`/api/campaigns/${campaignId}/map/graph`).flush(emptyGraph);
    await fixture.whenStable();
    fixture.detectChanges();

    const svg = serializeMapSvg(
      {
        territories: [
          {
            ...namedSquare('t1', 1, 'Coast', 0.1),
            terrainTypeId: 'source-plains',
            structureTypeId: 'source-town',
            ownerFactionId: 'source-north',
            spawnFactionId: 'source-north',
          },
        ],
        adjacencies: [],
      },
      {
        terrainTypes: [{ id: 'source-plains', name: 'Plains' }],
        structureTypes: [{ id: 'source-town', name: 'Town' }],
        factions: [{ id: 'source-north', name: 'North' }],
      },
    );
    const page = fixture.componentInstance as unknown as {
      onSvgFile: (event: Event) => Promise<void>;
      graph: () => { territories: MapTerritory[] };
      successMessage: () => string | null;
    };
    const input = document.createElement('input');
    Object.defineProperty(input, 'files', {
      configurable: true,
      value: [new File([svg], 'overlay.svg', { type: 'image/svg+xml' })],
    });
    await page.onSvgFile({ target: input } as unknown as Event);
    fixture.detectChanges();

    expect(page.graph().territories).toHaveLength(1);
    const imported = page.graph().territories[0];
    expect(imported.terrainTypeId).toBe('plains');
    expect(imported.structureTypeId).toBe('town');
    expect(imported.ownerFactionId).toBe('north');
    expect(imported.spawnFactionId).toBe('north');
    expect(page.successMessage()).toBe('Imported 1 territories from the SVG file.');
    http.verify();
  });
});

function namedSquare(id: string, displayNumber: number, name: string, x: number): MapTerritory {
  return {
    id,
    displayNumber,
    name,
    description: null,
    polygon: [
      { x, y: 0.1 },
      { x: x + 0.2, y: 0.1 },
      { x: x + 0.2, y: 0.3 },
      { x, y: 0.3 },
    ],
    terrainTypeId: 'plains',
    structureTypeId: null,
    structureCondition: 'Operational',
    overlayColor: null,
    ownerFactionId: null,
    spawnFactionId: null,
  };
}

function administratorProfile(): OwnProfile {
  return {
    id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    email: 'admin@example.test',
    username: 'admin',
    firstName: 'Ada',
    middleInitial: null,
    lastName: 'Admin',
    suffix: null,
    city: 'Halifax',
    region: null,
    country: 'Canada',
    displayNameMode: 'Username',
    timeZoneId: 'UTC',
    hasAvatar: false,
    createdUtc: '2026-08-13T00:00:00+00:00',
    updatedUtc: '2026-08-13T00:00:00+00:00',
    profileRevision: 1,
    emailConfirmed: true,
    isAdministrator: true,
    inAppNotificationsEnabled: true,
    emailNotificationsEnabled: true,
    preferredChatLanguage: 'English',
  };
}
