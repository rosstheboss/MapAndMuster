import { parseMapSvg, serializeMapSvg, svgDownloadFilename } from './map-svg';
import type { MapGraph } from './map-graph.models';

const graph: MapGraph = {
  territories: [
    {
      id: 't1',
      displayNumber: 1,
      name: 'Coast',
      description: 'A shore.',
      polygon: [
        { x: 0.1, y: 0.1 },
        { x: 0.4, y: 0.1 },
        { x: 0.4, y: 0.4 },
        { x: 0.1, y: 0.4 },
      ],
      terrainTypeId: 'plains',
      structureTypeId: null,
      structureCondition: 'Operational',
      overlayColor: '#00AA00',
      ownerFactionId: null,
      spawnFactionId: null,
    },
  ],
  adjacencies: [
    {
      id: 'a1',
      territoryAId: 't1',
      territoryBId: 't2',
      origin: 'Manual',
      marker: { x: 0.4, y: 0.25 },
    },
  ],
};

describe('map svg', () => {
  it('builds a download name from the campaign title', () => {
    expect(svgDownloadFilename('Border War')).toBe('border-war-overlay.svg');
  });

  it('round-trips overlay territories and adjacencies', () => {
    const svg = serializeMapSvg(graph);
    const parsed = parseMapSvg(svg, { defaultTerrainTypeId: 'sea' });
    expect(parsed.errors).toEqual([]);
    expect(parsed.graph.territories).toHaveLength(1);
    expect(parsed.graph.territories[0]?.name).toBe('Coast');
    expect(parsed.graph.territories[0]?.terrainTypeId).toBe('plains');
    expect(parsed.graph.territories[0]?.structureCondition).toBe('Operational');
    expect(parsed.graph.adjacencies).toHaveLength(1);
    expect(parsed.graph.adjacencies[0]?.origin).toBe('Manual');
  });

  it('round-trips a pillaged structure condition', () => {
    const svg = serializeMapSvg({
      ...graph,
      territories: [
        {
          ...graph.territories[0],
          structureTypeId: 'town',
          structureCondition: 'Pillaged',
        },
      ],
    });
    const parsed = parseMapSvg(svg, { defaultTerrainTypeId: 'sea' });
    expect(parsed.errors).toEqual([]);
    expect(parsed.graph.territories[0]?.structureTypeId).toBe('town');
    expect(parsed.graph.territories[0]?.structureCondition).toBe('Pillaged');
  });

  it('creates territories from generic SVG polygons', () => {
    const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
      <polygon points="10,10 40,10 40,40 10,40" />
    </svg>`;
    const parsed = parseMapSvg(svg, { defaultTerrainTypeId: 'plains' });
    expect(parsed.errors).toEqual([]);
    expect(parsed.graph.territories).toHaveLength(1);
    expect(parsed.graph.territories[0]?.terrainTypeId).toBe('plains');
    expect(parsed.graph.territories[0]?.polygon[0]?.x).toBeCloseTo(0.1);
  });
});
