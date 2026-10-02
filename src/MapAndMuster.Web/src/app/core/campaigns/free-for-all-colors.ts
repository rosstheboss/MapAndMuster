const PALETTE = [
  '#2563EB',
  '#DC2626',
  '#16A34A',
  '#CA8A04',
  '#7C3AED',
  '#EA580C',
  '#0891B2',
  '#BE185D',
  '#4B5563',
  '#65A30D',
  '#C026D3',
  '#0F766E',
];

export interface FreeForAllColorPlayer {
  userId: string;
  factionId: string;
  subfaction?: string | null;
  factionColor: string;
  subfactionColor?: string | null;
}

/** A lone player of a faction or subfaction keeps that color. Later duplicates get another color. */
export function freeForAllPlayerColors(players: readonly FreeForAllColorPlayer[]): Map<string, string> {
  const used = new Set<string>();
  const result = new Map<string, string>();
  const groups = new Map<string, FreeForAllColorPlayer[]>();
  for (const player of players) {
    const key = `${player.factionId}\u0000${player.subfaction?.trim() ?? ''}`;
    const group = groups.get(key) ?? [];
    group.push(player);
    groups.set(key, group);
  }

  const unique: FreeForAllColorPlayer[] = [];
  const duplicates: FreeForAllColorPlayer[] = [];
  for (const group of groups.values()) {
    const ordered = [...group].sort((left, right) => left.userId.localeCompare(right.userId));
    const [first, ...rest] = ordered;
    unique.push(first);

    duplicates.push(...rest);
  }

  for (const player of unique.sort((left, right) => left.userId.localeCompare(right.userId))) {
    result.set(player.userId, take(player.subfactionColor ?? player.factionColor, used));
  }

  for (const player of duplicates.sort((left, right) => left.userId.localeCompare(right.userId))) {
    result.set(player.userId, take(player.subfactionColor ?? player.factionColor, used));
  }

  return result;
}

export function minimumFreeForAllSpawns(playerSlotCount: number): number {
  if (playerSlotCount <= 0) {
    return 0;
  }

  return Math.ceil(playerSlotCount / 4);
}

export function freeForAllSpawnWarning(
  playerSlotCount: number,
  spawnCount: number,
  isFreeForAll: boolean,
): string | null {
  if (!isFreeForAll) {
    return null;
  }

  const required = minimumFreeForAllSpawns(playerSlotCount);
  if (spawnCount >= required) {
    return null;
  }

  return `Free-for-all needs at least ${required} spawn locations (one quarter of ${playerSlotCount} players, rounded up) and currently has ${spawnCount}. The campaign will not start until this is fixed.`;
}

function take(preferred: string, used: Set<string>): string {
  const color = preferred.trim();
  if (color && !used.has(color.toLowerCase())) {
    used.add(color.toLowerCase());
    return color;
  }

  const next = PALETTE.find((item) => !used.has(item.toLowerCase()));
  if (next) {
    used.add(next.toLowerCase());
    return next;
  }

  const generated = `#${used.size.toString(16).padStart(6, '0')}`;
  used.add(generated.toLowerCase());
  return generated;
}
