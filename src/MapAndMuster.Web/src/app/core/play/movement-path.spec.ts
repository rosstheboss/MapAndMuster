import {
  decodeMovePath,
  encodeMovePath,
  nextMoveSteps,
  pathIsComplete,
  shortestPath,
  stoppingPaths,
} from './movement-path';

describe('movement paths', () => {
  const force = {
    territoryId: 't1',
    moveTargets: ['t2', 't3', 't4'],
    moveHops: [
      { viaTerritoryId: 't2', targetTerritoryId: 't4' },
      { viaTerritoryId: 't3', targetTerritoryId: 't4' },
    ],
    movementSpeed: 2,
  };

  it('offers the next adjacent step and finishes only on a complete path', () => {
    const paths = stoppingPaths(force, () => false);
    expect(nextMoveSteps(paths, [])).toEqual(['t2', 't3']);
    expect(pathIsComplete(paths, ['t2'])).toBe(true);
    expect(nextMoveSteps(paths, ['t2'])).toEqual(['t4']);
    expect(pathIsComplete(paths, ['t2', 't4'])).toBe(true);
    expect(nextMoveSteps(paths, ['t2', 't4'])).toEqual([]);
    expect(encodeMovePath(['t2', 't4'])).toEqual({
      targetTerritoryId: 't4',
      viaTerritoryId: 't2',
      viaPath: [],
    });
    expect(decodeMovePath(encodeMovePath(['t2', 't3', 't4']))).toEqual(['t2', 't3', 't4']);
  });

  it('finds a retreat path that does not cross a locked battle', () => {
    const neighbors: Record<string, string[]> = {
      t1: ['locked', 't2'],
      locked: ['t1', 't3'],
      t2: ['t1', 't3'],
      t3: ['locked', 't2'],
    };
    expect(shortestPath('t1', 't3', (id) => neighbors[id] ?? [], 2, new Set(['locked']))).toEqual(['t2', 't3']);
  });
});
