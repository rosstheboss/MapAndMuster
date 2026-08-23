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

const sourceCatalog = {
  terrainTypes: [{ id: 'plains', name: 'Plains' }],
  structureTypes: [{ id: 'town', name: 'Town' }],
  factions: [{ id: 'north', name: 'North', subfactions: ['Khorne'] }],
};

describe('map svg', () => {
  it('builds a download name from the campaign title', () => {
    expect(svgDownloadFilename('Border War')).toBe('border-war-overlay.svg');
  });

  it('round-trips overlay territories and adjacencies', () => {
    const svg = serializeMapSvg(graph);
    expect(svg).toContain('stroke-width="0.004"');
    const parsed = parseMapSvg(svg, { defaultTerrainTypeId: 'sea' });
    expect(parsed.errors).toEqual([]);
    expect(parsed.warnings).toEqual([]);
    expect(parsed.graph.territories).toHaveLength(1);
    expect(parsed.graph.territories[0]?.name).toBe('Coast');
    expect(parsed.graph.territories[0]?.terrainTypeId).toBe('plains');
    expect(parsed.graph.territories[0]?.structureCondition).toBe('Operational');
    expect(parsed.graph.adjacencies).toHaveLength(1);
    expect(parsed.graph.adjacencies[0]?.origin).toBe('Manual');
  });

  it('writes catalog names so another campaign can remap by name', () => {
    const svg = serializeMapSvg(
      {
        ...graph,
        territories: [
          {
            ...graph.territories[0],
            structureTypeId: 'town',
            structureCondition: 'Pillaged',
            ownerFactionId: 'north',
            ownerSubfaction: 'Khorne',
            spawnFactionId: 'north',
            spawnSubfaction: 'Khorne',
          },
        ],
      },
      sourceCatalog,
    );
    expect(svg).toContain('data-terrain-type-name="Plains"');
    expect(svg).toContain('data-structure-type-name="Town"');
    expect(svg).toContain('data-owner-faction-name="North"');
    expect(svg).toContain('data-spawn-faction-name="North"');
  });

  it('remaps terrain, structures, owners, and spawns onto another campaign catalog by name', () => {
    const svg = serializeMapSvg(
      {
        ...graph,
        territories: [
          {
            ...graph.territories[0],
            structureTypeId: 'town',
            structureCondition: 'Pillaged',
            ownerFactionId: 'north',
            ownerSubfaction: 'khorne',
            spawnFactionId: 'north',
            spawnSubfaction: 'khorne',
          },
        ],
      },
      sourceCatalog,
    );
    const parsed = parseMapSvg(svg, {
      defaultTerrainTypeId: 'target-sea',
      catalog: {
        terrainTypes: [
          { id: 'target-sea', name: 'Sea' },
          { id: 'target-plains', name: 'Plains' },
        ],
        structureTypes: [{ id: 'target-town', name: 'Town' }],
        factions: [{ id: 'target-north', name: 'North', subfactions: ['Khorne'] }],
      },
    });
    expect(parsed.errors).toEqual([]);
    expect(parsed.warnings).toEqual([]);
    expect(parsed.graph.territories[0]?.terrainTypeId).toBe('target-plains');
    expect(parsed.graph.territories[0]?.structureTypeId).toBe('target-town');
    expect(parsed.graph.territories[0]?.structureCondition).toBe('Pillaged');
    expect(parsed.graph.territories[0]?.ownerFactionId).toBe('target-north');
    expect(parsed.graph.territories[0]?.ownerSubfaction).toBe('Khorne');
    expect(parsed.graph.territories[0]?.spawnFactionId).toBe('target-north');
    expect(parsed.graph.territories[0]?.spawnSubfaction).toBe('Khorne');
  });

  it('keeps catalog identifiers that already exist on the target campaign', () => {
    const svg = serializeMapSvg(graph, sourceCatalog);
    const parsed = parseMapSvg(svg, {
      defaultTerrainTypeId: 'sea',
      catalog: {
        terrainTypes: [
          { id: 'plains', name: 'Plains' },
          { id: 'sea', name: 'Sea' },
        ],
        structureTypes: [],
        factions: [],
      },
    });
    expect(parsed.graph.territories[0]?.terrainTypeId).toBe('plains');
  });

  it('falls back unmatched terrain and omits unmatched structures and factions', () => {
    const svg = serializeMapSvg(
      {
        ...graph,
        territories: [
          {
            ...graph.territories[0],
            terrainTypeId: 'marsh-id',
            structureTypeId: 'fort-id',
            ownerFactionId: 'empire-id',
            spawnFactionId: 'empire-id',
          },
        ],
      },
      {
        terrainTypes: [{ id: 'marsh-id', name: 'Marsh' }],
        structureTypes: [{ id: 'fort-id', name: 'Fort' }],
        factions: [{ id: 'empire-id', name: 'Empire' }],
      },
    );
    const parsed = parseMapSvg(svg, {
      defaultTerrainTypeId: 'plains',
      catalog: {
        terrainTypes: [{ id: 'plains', name: 'Plains' }],
        structureTypes: [{ id: 'town', name: 'Town' }],
        factions: [{ id: 'north', name: 'North' }],
      },
    });
    expect(parsed.errors).toEqual([]);
    expect(parsed.graph.territories[0]?.terrainTypeId).toBe('plains');
    expect(parsed.graph.territories[0]?.structureTypeId).toBeNull();
    expect(parsed.graph.territories[0]?.ownerFactionId).toBeNull();
    expect(parsed.graph.territories[0]?.spawnFactionId).toBeNull();
    expect(parsed.warnings).toEqual([
      "Terrain type 'Marsh' is not in this campaign; those territories used Plains.",
      "Structure type 'Fort' is not in this campaign and was omitted.",
      "Faction 'Empire' is not in this campaign; ownership and spawns for it were omitted.",
    ]);
  });

  it('warns when an older SVG has foreign identifiers and no catalog names', () => {
    const svg = serializeMapSvg({
      ...graph,
      territories: [
        {
          ...graph.territories[0],
          terrainTypeId: 'foreign-plains',
          structureTypeId: 'foreign-town',
          ownerFactionId: 'foreign-north',
          spawnFactionId: 'foreign-north',
        },
      ],
    });
    const parsed = parseMapSvg(svg, {
      defaultTerrainTypeId: 'plains',
      catalog: {
        terrainTypes: [{ id: 'plains', name: 'Plains' }],
        structureTypes: [{ id: 'town', name: 'Town' }],
        factions: [{ id: 'north', name: 'North' }],
      },
    });
    expect(parsed.graph.territories[0]?.terrainTypeId).toBe('plains');
    expect(parsed.graph.territories[0]?.structureTypeId).toBeNull();
    expect(parsed.graph.territories[0]?.ownerFactionId).toBeNull();
    expect(parsed.warnings[0]).toContain('exported without catalog names');
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
