import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { CampaignMapViewComponent, TERRITORY_HOVER_INTENT_MS } from './campaign-map-view.component';

const png =
  'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==';

const territory = {
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
  terrainTypeId: null,
  structureTypeId: null,
  structureCondition: 'Operational',
  overlayColor: '#2563EB',
  ownerFactionId: null,
  spawnFactionId: null,
};

describe('CampaignMapViewComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CampaignMapViewComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
  });

  it('pans on right-click drag without selecting or emitting a map point', () => {
    const fixture = TestBed.createComponent(CampaignMapViewComponent);
    fixture.componentRef.setInput('imageUrl', png);
    fixture.componentRef.setInput('territories', [territory]);
    fixture.componentRef.setInput('interactive', true);
    const mapPoint = vi.fn();
    const territorySelect = vi.fn();
    const backgroundSelect = vi.fn();
    fixture.componentInstance.mapPoint.subscribe(mapPoint);
    fixture.componentInstance.territorySelect.subscribe(territorySelect);
    fixture.componentInstance.backgroundSelect.subscribe(backgroundSelect);
    fixture.detectChanges();

    prepareOverflowingMap(fixture.componentInstance);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const svg = compiled.querySelector('svg')!;
    svg.dispatchEvent(pointer('pointerdown', { button: 2, clientX: 80, clientY: 60 }));
    svg.dispatchEvent(pointer('pointermove', { button: 2, buttons: 2, clientX: 120, clientY: 90 }));
    fixture.detectChanges();

    expect(mapPoint).not.toHaveBeenCalled();
    expect(territorySelect).not.toHaveBeenCalled();
    expect(backgroundSelect).not.toHaveBeenCalled();
    expect(compiled.querySelector('.map-viewport')?.classList.contains('is-panning')).toBe(true);
    expect(canvasTransform(compiled)).toContain('translate(-160px, -170px)');
  });

  it('suppresses the browser context menu on the map', () => {
    const fixture = TestBed.createComponent(CampaignMapViewComponent);
    fixture.componentRef.setInput('imageUrl', png);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const viewport = compiled.querySelector('.map-viewport')!;
    const event = new MouseEvent('contextmenu', { bubbles: true, cancelable: true });
    viewport.dispatchEvent(event);
    expect(event.defaultPrevented).toBe(true);
  });

  it('renders a force marker for a force in a territory', () => {
    const fixture = TestBed.createComponent(CampaignMapViewComponent);
    fixture.componentRef.setInput('imageUrl', png);
    fixture.componentRef.setInput('territories', [territory]);
    fixture.componentRef.setInput('factions', [
      {
        id: 'north',
        name: 'North',
        color: '#2563EB',
        subfactions: [],
        allyGroupName: null,
        requiresSubfaction: false,
        hasFlagImage: false,
      },
    ]);
    fixture.componentRef.setInput('forces', [
      {
        id: 'force-1',
        territoryId: 't1',
        factionId: 'north',
        isMine: true,
        inBattle: false,
        label: 'North force in Coast',
      },
    ]);
    fixture.detectChanges();

    const pin = (fixture.nativeElement as HTMLElement).querySelector('.force-pin.is-mine');
    expect(pin).toBeTruthy();
    expect(pin?.getAttribute('aria-label')).toBe('North force in Coast');
  });

  it('renders an item objective marker in a territory', () => {
    const fixture = TestBed.createComponent(CampaignMapViewComponent);
    fixture.componentRef.setInput('imageUrl', png);
    fixture.componentRef.setInput('territories', [territory]);
    fixture.componentRef.setInput('items', [
      {
        id: 'item-1',
        territoryId: 't1',
        name: 'Crown',
        carried: false,
        hidden: false,
      },
    ]);
    fixture.detectChanges();

    const pin = (fixture.nativeElement as HTMLElement).querySelector('.item-pin');
    expect(pin).toBeTruthy();
    expect(pin?.getAttribute('aria-label')).toBe('Crown');
  });

  it('fills owned territories with faction or alliance colors', () => {
    const owned = { ...territory, ownerFactionId: 'f1' };
    const fixture = TestBed.createComponent(CampaignMapViewComponent);
    fixture.componentRef.setInput('imageUrl', png);
    fixture.componentRef.setInput('territories', [owned]);
    fixture.componentRef.setInput('factions', [
      {
        id: 'f1',
        name: 'North',
        color: '#DC2626',
        subfactions: [],
        allyGroupName: 'Pact',
        requiresSubfaction: false,
        hasFlagImage: false,
      },
    ]);
    fixture.componentRef.setInput('allyGroups', [{ id: 'a1', name: 'Pact', color: '#111111' }]);
    fixture.componentRef.setInput('colorMode', 'faction');
    fixture.detectChanges();

    const polygon = (): Element | null => (fixture.nativeElement as HTMLElement).querySelector('polygon.territory');
    expect(polygon()?.getAttribute('fill')).toBe('#DC2626');

    fixture.componentRef.setInput('colorMode', 'alliance');
    fixture.detectChanges();
    expect(polygon()?.getAttribute('fill')).toBe('#111111');

    fixture.componentRef.setInput('brokenAllyFactionIds', ['f1']);
    fixture.detectChanges();
    expect(polygon()?.getAttribute('fill')).toBe('#DC2626');
  });

  it('shows carried item objectives on the possessing force instead of a ground pin', () => {
    const fixture = TestBed.createComponent(CampaignMapViewComponent);
    fixture.componentRef.setInput('imageUrl', png);
    fixture.componentRef.setInput('territories', [territory]);
    fixture.componentRef.setInput('forces', [
      {
        id: 'force-1',
        territoryId: 't1',
        factionId: 'f1',
        isMine: true,
        inBattle: false,
        label: 'North force in Coast',
        heldItems: [{ name: 'Crown', builtinSymbol: 'Crown', color: '#C45C26', imageUrl: null }],
      },
    ]);
    fixture.componentRef.setInput('items', [
      {
        id: 'item-1',
        territoryId: 't1',
        name: 'Crown',
        carried: true,
        hidden: false,
        builtinSymbol: 'Crown',
      },
    ]);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.item-pin')).toBeNull();
    expect(compiled.querySelector('.held-item')).toBeTruthy();
    expect(compiled.querySelector('.force-pin')?.getAttribute('aria-label')).toContain('Crown');
  });

  it('does not pan on left-click drag', () => {
    const fixture = TestBed.createComponent(CampaignMapViewComponent);
    fixture.componentRef.setInput('imageUrl', png);
    fixture.componentRef.setInput('territories', [territory]);
    fixture.componentRef.setInput('interactive', true);
    fixture.detectChanges();
    prepareOverflowingMap(fixture.componentInstance);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const svg = compiled.querySelector('svg')!;
    svg.dispatchEvent(pointer('pointerdown', { button: 0, clientX: 80, clientY: 60 }));
    svg.dispatchEvent(pointer('pointermove', { button: 0, buttons: 1, clientX: 120, clientY: 90 }));
    fixture.detectChanges();

    expect(compiled.querySelector('.map-viewport')?.classList.contains('is-panning')).toBe(false);
    expect(canvasTransform(compiled)).toContain('translate(-200px, -200px)');
  });

  it('draws a selection box and reports intersecting territories', () => {
    const fixture = TestBed.createComponent(CampaignMapViewComponent);
    fixture.componentRef.setInput('imageUrl', png);
    fixture.componentRef.setInput('territories', [territory, squareTerritory('t2', 0.6, 0.1)]);
    fixture.componentRef.setInput('interactive', true);
    fixture.componentRef.setInput('marqueeSelect', true);
    const marquee = vi.fn();
    fixture.componentInstance.territoryMarquee.subscribe(marquee);
    fixture.detectChanges();
    prepareOverflowingMap(fixture.componentInstance);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const svg = compiled.querySelector('svg')!;
    Object.defineProperty(svg, 'getBoundingClientRect', {
      value: () => ({ left: 0, top: 0, width: 1000, height: 800, right: 1000, bottom: 800 }),
    });
    svg.dispatchEvent(pointer('pointerdown', { button: 0, clientX: 50, clientY: 50 }));
    svg.dispatchEvent(pointer('pointermove', { button: 0, buttons: 1, clientX: 450, clientY: 350 }));
    fixture.detectChanges();
    expect(compiled.querySelector('.selection-marquee')).toBeTruthy();
    svg.dispatchEvent(pointer('pointerup', { button: 0, clientX: 450, clientY: 350 }));
    fixture.detectChanges();

    expect(marquee).toHaveBeenCalledWith({ ids: ['t1'], additive: false });
    expect(compiled.querySelector('.selection-marquee')).toBeNull();
  });

  it('fits the map on the first image load only', () => {
    globalThis.ResizeObserver = class {
      observe(): void {
        return;
      }
      disconnect(): void {
        return;
      }
      unobserve(): void {
        return;
      }
    };
    const fixture = TestBed.createComponent(CampaignMapViewComponent);
    fixture.componentRef.setInput('imageUrl', png);
    fixture.detectChanges();
    const view = fixture.componentInstance as unknown as {
      hasFittedImage: boolean;
      viewportSize: { set(value: { width: number; height: number }): void };
      onImageLoad: (event: Event) => void;
      zoom: { set(value: number): void; (): number };
      fitToPanel: { set(value: boolean): void; (): boolean };
    };
    view.viewportSize.set({ width: 400, height: 300 });
    const image = { naturalWidth: 1000, naturalHeight: 800 } as HTMLImageElement;
    view.onImageLoad({ target: image } as unknown as Event);
    expect(view.hasFittedImage).toBe(true);
    view.fitToPanel.set(false);
    view.zoom.set(2);
    view.onImageLoad({ target: image } as unknown as Event);
    expect(view.zoom()).toBe(2);
    expect(view.fitToPanel()).toBe(false);
  });

  it('keeps connection arrows black without hover size or outline changes', () => {
    const fixture = TestBed.createComponent(CampaignMapViewComponent);
    fixture.componentRef.setInput('imageUrl', png);
    fixture.componentRef.setInput('territories', [
      squareTerritory('t1', 0.1, 0.1),
      squareTerritory('t2', 0.4, 0.1),
      squareTerritory('t3', 0.7, 0.1),
    ]);
    fixture.componentRef.setInput('adjacencies', [
      {
        id: 'ab',
        territoryAId: 't1',
        territoryBId: 't2',
        origin: 'Manual',
        marker: { x: 0.35, y: 0.2 },
      },
      {
        id: 'ac',
        territoryAId: 't1',
        territoryBId: 't3',
        origin: 'Manual',
        marker: { x: 0.55, y: 0.2 },
      },
    ]);
    fixture.componentRef.setInput('showAdjacencies', true);
    fixture.componentRef.setInput('hoveredAdjacencyId', 'ab');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const groups = [...compiled.querySelectorAll('.adjacency')];
    expect(groups).toHaveLength(2);
    const hovered = groups.find((group) => group.getAttribute('data-id') === 'ab');
    const other = groups.find((group) => group.getAttribute('data-id') === 'ac');
    expect(hovered?.classList.contains('is-highlighted')).toBe(false);
    expect(other?.classList.contains('is-highlighted')).toBe(false);
    expect(hovered?.querySelector('.adjacency-visual')?.getAttribute('transform')).toBeNull();
    expect(other?.querySelector('.adjacency-visual')?.getAttribute('transform')).toBeNull();
    expect(hovered?.querySelector('.adjacency-outline')).toBeNull();
    expect(hovered?.querySelectorAll('.adjacency-hit-head').length).toBe(2);
    expect(hovered?.classList.contains('is-interactive')).toBe(false);
  });

  it('uses a full highlight for selection and a half highlight for hover and connections', () => {
    const fixture = TestBed.createComponent(CampaignMapViewComponent);
    fixture.componentRef.setInput('imageUrl', png);
    fixture.componentRef.setInput('territories', [
      squareTerritory('t1', 0.1, 0.1),
      squareTerritory('t2', 0.4, 0.1),
      squareTerritory('t3', 0.7, 0.1),
    ]);
    fixture.componentRef.setInput('selectedTerritoryIds', ['t1']);
    fixture.componentRef.setInput('hoveredTerritoryId', 't2');
    fixture.componentRef.setInput('adjacentTerritoryIds', ['t2', 't3']);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const polygon = (id: string): Element | null => compiled.querySelector(`.territory[data-id="${id}"]`);
    expect(polygon('t1')?.classList.contains('is-selected')).toBe(true);
    expect(polygon('t1')?.classList.contains('is-half-highlighted')).toBe(false);
    expect(polygon('t2')?.classList.contains('is-selected')).toBe(false);
    expect(polygon('t2')?.classList.contains('is-half-highlighted')).toBe(true);
    expect(polygon('t3')?.classList.contains('is-half-highlighted')).toBe(true);
  });

  it('lets a full selection override a hovered half highlight', () => {
    const fixture = TestBed.createComponent(CampaignMapViewComponent);
    fixture.componentRef.setInput('imageUrl', png);
    fixture.componentRef.setInput('territories', [squareTerritory('t1', 0.1, 0.1)]);
    fixture.componentRef.setInput('selectedTerritoryIds', ['t1']);
    fixture.componentRef.setInput('hoveredTerritoryId', 't1');
    fixture.componentRef.setInput('adjacentTerritoryIds', ['t1']);
    fixture.detectChanges();

    const polygon = (fixture.nativeElement as HTMLElement).querySelector('.territory[data-id="t1"]');
    expect(polygon?.classList.contains('is-selected')).toBe(true);
    expect(polygon?.classList.contains('is-half-highlighted')).toBe(false);
  });

  it('dims unrelated territories and paints selected connection arrows white', () => {
    const fixture = TestBed.createComponent(CampaignMapViewComponent);
    fixture.componentRef.setInput('imageUrl', png);
    fixture.componentRef.setInput('territories', [
      squareTerritory('t1', 0.1, 0.1),
      squareTerritory('t2', 0.4, 0.1),
      squareTerritory('t3', 0.7, 0.1),
    ]);
    fixture.componentRef.setInput('adjacencies', [
      {
        id: 'ab',
        territoryAId: 't1',
        territoryBId: 't2',
        origin: 'Manual',
        marker: { x: 0.35, y: 0.2 },
      },
      {
        id: 'bc',
        territoryAId: 't2',
        territoryBId: 't3',
        origin: 'Manual',
        marker: { x: 0.65, y: 0.2 },
      },
    ]);
    fixture.componentRef.setInput('showAdjacencies', true);
    fixture.componentRef.setInput('focusSelectedTerritories', true);
    fixture.componentRef.setInput('selectedTerritoryIds', ['t1']);
    fixture.componentRef.setInput('adjacentTerritoryIds', ['t2']);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const polygon = (id: string): Element | null => compiled.querySelector(`.territory[data-id="${id}"]`);
    expect(polygon('t1')?.classList.contains('is-dimmed')).toBe(false);
    expect(polygon('t2')?.classList.contains('is-dimmed')).toBe(false);
    expect(polygon('t3')?.classList.contains('is-dimmed')).toBe(true);
    expect(compiled.querySelector('.adjacency[data-id="ab"] .adjacency-outline')).toBeNull();
    expect(compiled.querySelector('.adjacency[data-id="ab"]')?.classList.contains('is-from-selection')).toBe(false);
    expect(compiled.querySelector('.adjacency[data-id="bc"]')?.classList.contains('is-from-selection')).toBe(false);
  });

  it('hides overlay polygons and connections when Show Overlay is off', () => {
    const fixture = TestBed.createComponent(CampaignMapViewComponent);
    fixture.componentRef.setInput('imageUrl', png);
    fixture.componentRef.setInput('territories', [squareTerritory('t1', 0.1, 0.1), squareTerritory('t2', 0.4, 0.1)]);
    fixture.componentRef.setInput('adjacencies', [
      {
        id: 'ab',
        territoryAId: 't1',
        territoryBId: 't2',
        origin: 'Manual',
        marker: { x: 0.35, y: 0.2 },
      },
    ]);
    fixture.componentRef.setInput('showAdjacencies', true);
    fixture.componentRef.setInput('layerToggles', true);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelectorAll('.territory')).toHaveLength(2);
    expect(compiled.querySelectorAll('.adjacency')).toHaveLength(1);

    fixture.componentRef.setInput('showOverlay', false);
    fixture.detectChanges();
    expect(compiled.querySelector('.territory')).toBeNull();
    expect(compiled.querySelector('.adjacency')).toBeNull();
    expect(compiled.querySelector<HTMLInputElement>('.layer-toggle input')?.checked).toBe(false);
  });

  it('shows a green check when a move can be dropped and a red X when it cannot', () => {
    const fixture = TestBed.createComponent(CampaignMapViewComponent);
    fixture.componentRef.setInput('imageUrl', png);
    fixture.componentRef.setInput('territories', [squareTerritory('t1', 0.1, 0.1), squareTerritory('t2', 0.4, 0.1)]);
    fixture.componentRef.setInput('selectedTerritoryIds', ['t1', 't2']);
    fixture.componentRef.setInput('movePlacement', 'valid');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const polygon = (id: string): Element | null => compiled.querySelector(`.territory[data-id="${id}"]`);
    expect(polygon('t1')?.classList.contains('is-move-valid')).toBe(true);
    expect(polygon('t2')?.classList.contains('is-move-valid')).toBe(true);
    expect(compiled.querySelector('.move-drop-marker.is-valid')?.getAttribute('aria-label')).toBe('Drop is allowed');
    expect(compiled.querySelector('.move-drop-marker app-icon')).toBeTruthy();

    fixture.componentRef.setInput('movePlacement', 'invalid');
    fixture.detectChanges();
    expect(polygon('t1')?.classList.contains('is-move-invalid')).toBe(true);
    expect(polygon('t2')?.classList.contains('is-move-invalid')).toBe(true);
    expect(compiled.querySelector('.move-drop-marker.is-invalid')?.getAttribute('aria-label')).toBe(
      'Drop is not allowed',
    );
  });

  it('lets connection arrows intercept clicks only when they are interactive', () => {
    const fixture = TestBed.createComponent(CampaignMapViewComponent);
    fixture.componentRef.setInput('imageUrl', png);
    fixture.componentRef.setInput('territories', [squareTerritory('t1', 0.1, 0.1), squareTerritory('t2', 0.4, 0.1)]);
    fixture.componentRef.setInput('adjacencies', [
      {
        id: 'ab',
        territoryAId: 't1',
        territoryBId: 't2',
        origin: 'Manual',
        marker: { x: 0.35, y: 0.2 },
      },
    ]);
    fixture.componentRef.setInput('showAdjacencies', true);
    fixture.componentRef.setInput('interactive', true);
    const adjacencySelect = vi.fn();
    fixture.componentInstance.adjacencySelect.subscribe(adjacencySelect);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const hit = compiled.querySelector('.adjacency-hit')!;
    expect(compiled.querySelector('.adjacency')?.classList.contains('is-interactive')).toBe(false);
    hit.dispatchEvent(pointer('pointerdown', { button: 0, clientX: 10, clientY: 10 }));
    expect(adjacencySelect).not.toHaveBeenCalled();

    fixture.componentRef.setInput('adjacenciesInteractive', true);
    fixture.detectChanges();
    expect(compiled.querySelector('.adjacency')?.classList.contains('is-interactive')).toBe(true);
    compiled
      .querySelector('.adjacency-hit')
      ?.dispatchEvent(pointer('pointerdown', { button: 0, clientX: 10, clientY: 10 }));
    expect(adjacencySelect).toHaveBeenCalledWith('ab');
  });

  it('fills spawn territories with 5-pixel diagonal stripes and lifts hovered unselected territories', () => {
    const fixture = TestBed.createComponent(CampaignMapViewComponent);
    fixture.componentRef.setInput('imageUrl', png);
    fixture.componentRef.setInput('territories', [
      squareTerritory('t1', 0.1, 0.1),
      { ...squareTerritory('t2', 0.4, 0.1), spawnFactionId: '1' },
    ]);
    fixture.componentRef.setInput('hoveredTerritoryId', 't1');
    fixture.detectChanges();
    prepareOverflowingMap(fixture.componentInstance);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const spawn = compiled.querySelector('.territory[data-id="t2"]');
    const hovered = compiled.querySelector('.territory[data-id="t1"]');
    expect(spawn?.classList.contains('is-spawn')).toBe(true);
    expect(spawn?.getAttribute('fill')).toContain('url(#spawn-stripe-');
    expect(compiled.querySelector('pattern')?.getAttribute('patternTransform')).toBe('rotate(45)');
    const hoveredVisual = hovered?.closest('.territory-visual');
    const spawnVisual = spawn?.closest('.territory-visual');
    expect(hoveredVisual?.classList.contains('is-lifted')).toBe(true);
    expect(spawnVisual?.classList.contains('is-lifted')).toBe(false);
    const hoveredLift = Number(/translate\(0 ([-\d.]+)\)/.exec(hoveredVisual?.getAttribute('transform') ?? '')?.[1]);
    const spawnLift = Number(/translate\(0 ([-\d.]+)\)/.exec(spawnVisual?.getAttribute('transform') ?? '')?.[1]);
    expect(hoveredLift).toBeLessThan(0);
    expect(hoveredLift).toBeGreaterThan(-0.05);
    expect(spawnLift).toBe(0);
    expect(compiled.querySelector('.territory-hit[data-id="t1"]')?.closest('.territory-visual')).toBeNull();
  });

  it('animates only the territory whose hover lift is changing', () => {
    vi.useFakeTimers();
    try {
      const fixture = TestBed.createComponent(CampaignMapViewComponent);
      fixture.componentRef.setInput('imageUrl', png);
      fixture.componentRef.setInput('territories', [squareTerritory('t1', 0.1, 0.1), squareTerritory('t2', 0.4, 0.1)]);
      fixture.componentInstance.territoryHover.subscribe((id) => {
        fixture.componentRef.setInput('hoveredTerritoryId', id);
      });
      fixture.detectChanges();

      const compiled = fixture.nativeElement as HTMLElement;
      const visuals = (): HTMLElement[] => [...compiled.querySelectorAll<HTMLElement>('.territory-visual')];
      expect(visuals().every((visual) => !visual.classList.contains('is-hover-motion'))).toBe(true);
      expect(visuals().every((visual) => visual.style.transitionDuration === '')).toBe(true);

      compiled
        .querySelector('.territory-hit[data-id="t1"]')!
        .dispatchEvent(pointer('pointerenter', { bubbles: false }));
      vi.advanceTimersByTime(TERRITORY_HOVER_INTENT_MS);
      fixture.detectChanges();

      const moving = compiled.querySelector('.territory[data-id="t1"]')?.closest('.territory-visual');
      const idle = compiled.querySelector('.territory[data-id="t2"]')?.closest('.territory-visual');
      expect(moving?.classList.contains('is-hover-motion')).toBe(true);
      expect(idle?.classList.contains('is-hover-motion')).toBe(false);

      compiled
        .querySelector('.map-viewport')!
        .dispatchEvent(new WheelEvent('wheel', { deltaY: 100, bubbles: true, cancelable: true }));
      fixture.detectChanges();
      expect(visuals().every((visual) => !visual.classList.contains('is-hover-motion'))).toBe(true);
    } finally {
      vi.useRealTimers();
    }
  });

  it('waits before starting a territory hover so brief pointer jitter does not flicker', () => {
    vi.useFakeTimers();
    try {
      const fixture = TestBed.createComponent(CampaignMapViewComponent);
      fixture.componentRef.setInput('imageUrl', png);
      fixture.componentRef.setInput('territories', [squareTerritory('t1', 0.1, 0.1)]);
      const hover = vi.fn();
      fixture.componentInstance.territoryHover.subscribe(hover);
      fixture.detectChanges();

      const hit = (fixture.nativeElement as HTMLElement).querySelector('.territory-hit[data-id="t1"]')!;
      hit.dispatchEvent(pointer('pointerenter', { bubbles: false }));
      expect(hover).not.toHaveBeenCalled();
      vi.advanceTimersByTime(TERRITORY_HOVER_INTENT_MS - 1);
      expect(hover).not.toHaveBeenCalled();
      vi.advanceTimersByTime(1);
      expect(hover).toHaveBeenCalledWith('t1');
    } finally {
      vi.useRealTimers();
    }
  });

  it('cancels a pending territory hover when the pointer leaves before the delay', () => {
    vi.useFakeTimers();
    try {
      const fixture = TestBed.createComponent(CampaignMapViewComponent);
      fixture.componentRef.setInput('imageUrl', png);
      fixture.componentRef.setInput('territories', [squareTerritory('t1', 0.1, 0.1)]);
      const hover = vi.fn();
      fixture.componentInstance.territoryHover.subscribe(hover);
      fixture.detectChanges();

      const hit = (fixture.nativeElement as HTMLElement).querySelector('.territory-hit[data-id="t1"]')!;
      hit.dispatchEvent(pointer('pointerenter', { bubbles: false }));
      hit.dispatchEvent(pointer('pointerleave', { bubbles: false }));
      vi.advanceTimersByTime(TERRITORY_HOVER_INTENT_MS);
      expect(hover).not.toHaveBeenCalled();
    } finally {
      vi.useRealTimers();
    }
  });

  it('waits before clearing a territory hover and cancels that wait if the pointer returns', () => {
    vi.useFakeTimers();
    try {
      const fixture = TestBed.createComponent(CampaignMapViewComponent);
      fixture.componentRef.setInput('imageUrl', png);
      fixture.componentRef.setInput('territories', [squareTerritory('t1', 0.1, 0.1)]);
      fixture.componentRef.setInput('hoveredTerritoryId', 't1');
      const hover = vi.fn();
      fixture.componentInstance.territoryHover.subscribe(hover);
      fixture.detectChanges();

      const hit = (fixture.nativeElement as HTMLElement).querySelector('.territory-hit[data-id="t1"]')!;
      hit.dispatchEvent(pointer('pointerleave', { bubbles: false }));
      expect(hover).not.toHaveBeenCalled();
      hit.dispatchEvent(pointer('pointerenter', { bubbles: false }));
      vi.advanceTimersByTime(TERRITORY_HOVER_INTENT_MS);
      expect(hover).not.toHaveBeenCalled();

      hit.dispatchEvent(pointer('pointerleave', { bubbles: false }));
      vi.advanceTimersByTime(TERRITORY_HOVER_INTENT_MS);
      expect(hover).toHaveBeenCalledWith(null);
    } finally {
      vi.useRealTimers();
    }
  });
});

function squareTerritory(id: string, x: number, y: number): typeof territory {
  return {
    id,
    displayNumber: 1,
    name: id,
    description: null,
    polygon: [
      { x, y },
      { x: x + 0.2, y },
      { x: x + 0.2, y: y + 0.2 },
      { x, y: y + 0.2 },
    ],
    terrainTypeId: null,
    structureTypeId: null,
    structureCondition: 'Operational',
    overlayColor: '#2563EB',
    ownerFactionId: null,
    spawnFactionId: null,
  };
}

function pointer(type: string, init: PointerEventInit): PointerEvent {
  return new PointerEvent(type, { bubbles: true, pointerId: 1, ...init });
}

function canvasTransform(root: HTMLElement): string {
  return root.querySelector<HTMLElement>('.map-canvas')!.style.transform;
}

function prepareOverflowingMap(component: CampaignMapViewComponent): void {
  const view = component as unknown as {
    imageSize: { set(value: { width: number; height: number }): void };
    viewportSize: { set(value: { width: number; height: number }): void };
    fitToPanel: { set(value: boolean): void };
    zoom: { set(value: number): void };
    panX: { set(value: number): void };
    panY: { set(value: number): void };
  };
  view.imageSize.set({ width: 1000, height: 800 });
  view.viewportSize.set({ width: 400, height: 300 });
  view.fitToPanel.set(false);
  view.zoom.set(1);
  view.panX.set(-200);
  view.panY.set(-200);
}
