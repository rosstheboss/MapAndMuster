import { colorWithAlpha, drawTerritoryOverlay, mapDownloadFilename } from './map-export';
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
    } as unknown as CanvasRenderingContext2D;
    const territories: MapTerritory[] = [
      {
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
        overlayColor: '#00AA00',
        ownerFactionId: null,
        spawnFactionId: null,
      },
    ];

    drawTerritoryOverlay(ctx, 100, 50, territories);

    expect(calls).toContain('begin');
    expect(calls).toContain('move:0,0');
    expect(calls).toContain('line:100,0');
    expect(calls).toContain('line:100,50');
    expect(calls).toContain('close');
    expect(calls).toContain('fill');
    expect(calls).toContain('stroke');
    expect(ctx.fillStyle).toBe('rgba(0, 170, 0, 0.32)');
  });
});
