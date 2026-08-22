import { MARKER_MAX_PX, STROKE_SCREEN_PX } from './geometry';
import type { CampaignFaction, CampaignStructureType } from '../campaigns/campaign.models';
import {
  colorWithAlpha,
  drawMapDecorations,
  drawTerritoryOverlay,
  mapDownloadFilename,
  structureSymbolDataUrl,
  type MapExportDecorations,
} from './map-export';
import type { MapTerritory } from './map-graph.models';

describe('map export', () => {
  it('builds a download name from the campaign title', () => {
    expect(mapDownloadFilename('Border War')).toBe('border-war-map.png');
    expect(mapDownloadFilename('  ')).toBe('campaign-map.png');
  });

  it('adds fill opacity to hex overlay colors', () => {
    expect(colorWithAlpha('#2563EB', 0.32)).toBe('rgba(37, 99, 235, 0.32)');
  });

  it('draws unselected territory fills and strokes onto the canvas', () => {
    const { ctx, calls } = mockCanvas();
    drawTerritoryOverlay(ctx, 100, 50, [territory({ overlayColor: '#00AA00' })]);

    expect(calls).toContain('begin');
    expect(calls).toContain('move:0,0');
    expect(calls).toContain('line:100,0');
    expect(calls).toContain('line:100,50');
    expect(calls).toContain('close');
    expect(calls).toContain('fill');
    expect(calls).toContain('stroke');
    expect(ctx.fillStyle).toBe('rgba(0, 170, 0, 0.32)');
    expect(ctx.lineWidth).toBe(STROKE_SCREEN_PX);
    expect(calls.some((call) => call.startsWith('clip'))).toBe(false);
  });

  it('hatches spawn territories with diagonal stripes instead of a solid fill', () => {
    const { ctx, calls } = mockCanvas();
    drawTerritoryOverlay(ctx, 100, 50, [territory({ overlayColor: null, spawnFactionId: 'north' })], {
      factions: [faction({ id: 'north', color: '#334455' })],
      structures: [],
    });

    expect(calls).toContain('clip');
    expect(calls).toContain('scale');
    expect(calls).toContain('rotate');
    expect(calls.some((call) => call.startsWith('fillRect:'))).toBe(true);
    expect(calls).not.toContain('fill');
    expect(calls).toContain('stroke');
    expect(canvasFillStyle(ctx)).toContain('51, 68, 85');
  });

  it('uses overlay color for spawn stripes when the territory is colored', () => {
    const { ctx, calls } = mockCanvas();
    drawTerritoryOverlay(
      ctx,
      100,
      50,
      [territory({ overlayColor: '#00AA00', spawnFactionId: 'north' })],
      decorations(),
    );

    expect(calls).toContain('clip');
    expect(calls).not.toContain('fill');
    expect(canvasFillStyle(ctx)).toBe('rgba(0, 170, 0, 0.32)');
  });

  it('draws a faction color pennant for owned territories', async () => {
    const { ctx, calls } = mockCanvas();
    await drawMapDecorations(ctx, 100, 100, [territory({ ownerFactionId: 'north' })], decorations());

    expect(calls).toContain('fill');
    expect(ctx.fillStyle).toBe('#112233');
    expect(calls.some((call) => call.startsWith('line:'))).toBe(true);
    expect(calls.some((call) => call.startsWith('drawImage:'))).toBe(false);
  });

  it('draws faction flags at twice the on-map marker size', async () => {
    const { ctx, calls } = mockCanvas();
    await drawMapDecorations(ctx, 1000, 1000, [territory({ ownerFactionId: 'north' })], decorations());

    expect(pennantWidth(calls)).toBe(MARKER_MAX_PX * 2);
  });

  it('draws an uploaded faction logo instead of the color pennant', async () => {
    const { ctx, calls } = mockCanvas();
    const flag = document.createElement('canvas');
    flag.width = 20;
    flag.height = 10;
    const loaded: string[] = [];
    await drawMapDecorations(
      ctx,
      100,
      100,
      [territory({ ownerFactionId: 'north' })],
      {
        factions: [faction({ hasFlagImage: true })],
        structures: [],
        flagImageUrl: () => '/flags/north.png',
      },
      (url) => {
        loaded.push(url);
        return Promise.resolve(flag);
      },
    );

    expect(loaded).toEqual(['/flags/north.png']);
    expect(calls.some((call) => call.startsWith('drawImage:'))).toBe(true);
    expect(ctx.fillStyle).toBe('rgba(255, 255, 255, 0.92)');
  });

  it('draws a structure pin using the builtin symbol', async () => {
    const { ctx, calls } = mockCanvas();
    const symbol = document.createElement('canvas');
    symbol.width = 24;
    symbol.height = 24;
    const loaded: string[] = [];
    await drawMapDecorations(ctx, 100, 100, [territory({ structureTypeId: 'town' })], decorations(), (url) => {
      loaded.push(url);
      return Promise.resolve(symbol);
    });

    expect(loaded).toEqual([structureSymbolDataUrl('Town', false)]);
    expect(calls.some((call) => call.startsWith('drawImage:'))).toBe(true);
  });

  it('draws structures at three times the on-map marker size', async () => {
    const { ctx, calls } = mockCanvas();
    const symbol = document.createElement('canvas');
    symbol.width = 24;
    symbol.height = 24;
    await drawMapDecorations(ctx, 1000, 1000, [territory({ structureTypeId: 'town' })], decorations(), () =>
      Promise.resolve(symbol),
    );

    const inner = MARKER_MAX_PX * 3 * 0.8;
    expect(calls).toContain(`drawImage:${inner}x${inner}`);
  });

  it('omits destroyed structures from the download', async () => {
    const { ctx, calls } = mockCanvas();
    await drawMapDecorations(
      ctx,
      100,
      100,
      [territory({ structureTypeId: 'town', structureCondition: 'Destroyed' })],
      decorations(),
      () => Promise.reject(new Error('destroyed structures should not load an image')),
    );

    expect(calls.some((call) => call.startsWith('drawImage:'))).toBe(false);
    expect(calls).not.toContain('fill');
  });

  it('does not draw adjacency arrows while decorating the overlay', async () => {
    const { ctx, calls } = mockCanvas();
    await drawMapDecorations(
      ctx,
      100,
      100,
      [territory({ ownerFactionId: 'north', structureTypeId: 'town', spawnFactionId: 'north' })],
      decorations(),
      () => Promise.resolve(document.createElement('canvas')),
    );

    expect(calls.some((call) => call.includes('arrow'))).toBe(false);
    expect(calls.filter((call) => call === 'stroke').length).toBe(0);
  });
});

function decorations(): MapExportDecorations {
  return {
    factions: [faction()],
    structures: [structure()],
  };
}

function faction(overrides: Partial<CampaignFaction> = {}): CampaignFaction {
  return {
    id: 'north',
    name: 'North',
    color: '#112233',
    subfactions: [],
    allyGroupName: null,
    requiresSubfaction: false,
    hasFlagImage: false,
    ...overrides,
  };
}

function structure(overrides: Partial<CampaignStructureType> = {}): CampaignStructureType {
  return {
    id: 'town',
    name: 'Town',
    builtinSymbol: 'Town',
    hasImage: false,
    hasPillagedImage: false,
    isBuildable: true,
    isPillageable: true,
    isDestructible: true,
    missions: [],
    ...overrides,
  };
}

function territory(overrides: Partial<MapTerritory> = {}): MapTerritory {
  return {
    id: 't1',
    displayNumber: 1,
    name: 'Coast',
    description: null,
    polygon: [
      { x: 0, y: 0 },
      { x: 1, y: 0 },
      { x: 1, y: 1 },
      { x: 0, y: 1 },
    ],
    terrainTypeId: 'sea',
    structureTypeId: null,
    structureCondition: 'Operational',
    overlayColor: null,
    ownerFactionId: null,
    spawnFactionId: null,
    ...overrides,
  };
}

function canvasFillStyle(ctx: CanvasRenderingContext2D): string {
  expect(typeof ctx.fillStyle).toBe('string');
  return typeof ctx.fillStyle === 'string' ? ctx.fillStyle : '';
}

function pennantWidth(calls: string[]): number {
  const move = calls.find((call) => call.startsWith('move:'));
  const line = calls.find((call) => call.startsWith('line:'));
  expect(move).toBeTruthy();
  expect(line).toBeTruthy();
  const left = Number((move ?? '').split(':')[1]?.split(',')[0]);
  const right = Number((line ?? '').split(':')[1]?.split(',')[0]);
  return right - left;
}

function mockCanvas(): { ctx: CanvasRenderingContext2D; calls: string[] } {
  const calls: string[] = [];
  const ctx = {
    lineJoin: '',
    lineCap: '',
    lineWidth: 0,
    strokeStyle: '',
    fillStyle: '',
    beginPath: () => calls.push('begin'),
    moveTo: (x: number, y: number) => calls.push(`move:${x},${y}`),
    lineTo: (x: number, y: number) => calls.push(`line:${x},${y}`),
    closePath: () => calls.push('close'),
    fill: () => calls.push('fill'),
    stroke: () => calls.push('stroke'),
    save: () => calls.push('save'),
    restore: () => calls.push('restore'),
    clip: () => calls.push('clip'),
    rotate: () => calls.push('rotate'),
    scale: () => calls.push('scale'),
    translate: (x: number, y: number) => calls.push(`translate:${x},${y}`),
    fillRect: (x: number, y: number, width: number, height: number) =>
      calls.push(`fillRect:${x},${y},${width},${height}`),
    rect: (x: number, y: number, width: number, height: number) => calls.push(`rect:${x},${y},${width},${height}`),
    roundRect: (x: number, y: number, width: number, height: number) =>
      calls.push(`roundRect:${x},${y},${width},${height}`),
    drawImage: (...args: unknown[]) => {
      const destWidth = args[3];
      const destHeight = args[4];
      if (typeof destWidth === 'number' && typeof destHeight === 'number') {
        calls.push(`drawImage:${destWidth}x${destHeight}`);
        return;
      }

      calls.push(`drawImage:${args.length}`);
    },
  } as unknown as CanvasRenderingContext2D;
  return { ctx, calls };
}
