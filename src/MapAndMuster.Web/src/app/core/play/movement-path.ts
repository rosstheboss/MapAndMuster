export interface MoveHopLike {
  viaTerritoryId: string;
  targetTerritoryId: string;
  intermediateTerritoryIds?: readonly string[];
}

export interface MovePathForce {
  territoryId: string;
  moveTargets: readonly string[];
  moveHops?: readonly MoveHopLike[];
  escapeMoveTargets?: readonly string[];
  movementSpeed?: number;
}

export interface EncodedMovePath {
  targetTerritoryId: string;
  viaTerritoryId: string;
  viaPath: string[];
}

export function stoppingPaths(force: MovePathForce, isAdjacentToOrigin: (territoryId: string) => boolean): string[][] {
  const hopPaths = (force.moveHops ?? []).map((hop) => hopSteps(hop)).filter((path) => path.length > 0);
  const later = new Set<string>();
  for (const path of hopPaths) {
    for (const id of path.slice(1)) {
      later.add(id);
    }
  }

  const paths: string[][] = [];
  const seen = new Set<string>();
  const add = (path: string[]): void => {
    const key = path.join('|');
    if (path.length === 0 || seen.has(key)) {
      return;
    }

    seen.add(key);
    paths.push(path);
  };

  for (const path of hopPaths) {
    add(path);
    for (let end = 1; end < path.length; end += 1) {
      const prefix = path.slice(0, end);
      const last = prefix[prefix.length - 1];
      if (last && force.moveTargets.includes(last)) {
        add(prefix);
      }
    }
  }

  for (const target of force.moveTargets) {
    if (isAdjacentToOrigin(target) || !later.has(target)) {
      add([target]);
    }
  }

  return paths;
}

export function nextMoveSteps(paths: readonly (readonly string[])[], taken: readonly string[]): string[] {
  const used = new Set(taken);
  const next = new Set<string>();
  for (const path of paths) {
    const matches = taken.every((id, index) => path[index] === id);
    const step = matches ? path[taken.length] : undefined;
    if (step && !used.has(step)) {
      next.add(step);
    }
  }

  return [...next];
}

export function pathIsComplete(paths: readonly (readonly string[])[], taken: readonly string[]): boolean {
  return (
    taken.length > 0 &&
    paths.some((path) => path.length === taken.length && path.every((id, index) => id === taken[index]))
  );
}

export function encodeMovePath(steps: readonly string[]): EncodedMovePath {
  if (steps.length === 0) {
    return { targetTerritoryId: '', viaTerritoryId: '', viaPath: [] };
  }

  const targetTerritoryId = steps[steps.length - 1] ?? '';
  if (steps.length === 1) {
    return { targetTerritoryId, viaTerritoryId: '', viaPath: [] };
  }

  return {
    targetTerritoryId,
    viaTerritoryId: steps[0] ?? '',
    viaPath: steps.slice(1, -1),
  };
}

export function decodeMovePath(draft: EncodedMovePath): string[] {
  if (!draft.targetTerritoryId) {
    return [];
  }

  const steps: string[] = [];
  if (draft.viaTerritoryId) {
    steps.push(draft.viaTerritoryId);
  }

  for (const id of draft.viaPath) {
    if (id && id !== steps.at(-1) && id !== draft.targetTerritoryId) {
      steps.push(id);
    }
  }

  if (draft.targetTerritoryId !== steps.at(-1)) {
    steps.push(draft.targetTerritoryId);
  }

  return steps;
}

export function shortestPath(
  originId: string,
  targetId: string,
  neighbors: (territoryId: string) => readonly string[],
  maxSteps: number,
  blocked: ReadonlySet<string>,
): string[] | null {
  if (!targetId || originId === targetId || maxSteps < 1 || blocked.has(targetId)) {
    return null;
  }

  const queue: { id: string; steps: string[] }[] = [{ id: originId, steps: [] }];
  const seen = new Set<string>([originId]);
  while (queue.length > 0) {
    const current = queue.shift();
    if (!current || current.steps.length >= maxSteps) {
      continue;
    }

    for (const next of neighbors(current.id)) {
      if (seen.has(next) || blocked.has(next)) {
        continue;
      }

      const steps = [...current.steps, next];
      if (next === targetId) {
        return steps;
      }

      seen.add(next);
      queue.push({ id: next, steps });
    }
  }

  return null;
}

function hopSteps(hop: MoveHopLike): string[] {
  return [hop.viaTerritoryId, ...(hop.intermediateTerritoryIds ?? []), hop.targetTerritoryId].filter(
    (id) => id.length > 0,
  );
}
