import { Injectable } from '@angular/core';

import type { CampaignPointStanding } from './campaign.models';

export type MapHighlightMode = 'configured' | 'faction' | 'alliance';

export type StandingsSortColumn =
  'displayName' | 'faction' | 'allyGroup' | 'territory' | 'battles' | 'public' | 'other' | 'total';

export interface StandingsSort {
  column: StandingsSortColumn;
  direction: 'asc' | 'desc';
}

export interface CampaignViewPrefs {
  highlightMode: MapHighlightMode;
  sections: Record<string, boolean>;
  standingsSort: StandingsSort;
  chatChannelKey: string;
  chatScrollTop: number;
}

const COOKIE_MAX_AGE_SECONDS = 60 * 60 * 24 * 365;
const COOKIE_PREFIX = 'cv-';

export const DEFAULT_STANDINGS_SORT: StandingsSort = { column: 'total', direction: 'desc' };

export function defaultCampaignViewPrefs(): CampaignViewPrefs {
  return {
    highlightMode: 'configured',
    sections: {},
    standingsSort: { ...DEFAULT_STANDINGS_SORT },
    chatChannelKey: 'Public:',
    chatScrollTop: 0,
  };
}

@Injectable({ providedIn: 'root' })
export class CampaignViewPrefsService {
  read(campaignId: string): CampaignViewPrefs {
    return readStoredPrefs(campaignId) ?? defaultCampaignViewPrefs();
  }

  write(campaignId: string, prefs: CampaignViewPrefs): void {
    writeStoredPrefs(campaignId, prefs);
  }
}

export function cookieNameFor(campaignId: string): string {
  return `${COOKIE_PREFIX}${campaignId}`;
}

export function readStoredPrefs(campaignId: string): CampaignViewPrefs | null {
  const name = cookieNameFor(campaignId);
  const match = new RegExp(`(?:^|; )${name}=([^;]*)`).exec(document.cookie);
  if (!match?.[1]) {
    return null;
  }

  try {
    const parsed = JSON.parse(decodeURIComponent(match[1])) as Partial<CampaignViewPrefs>;
    const defaults = defaultCampaignViewPrefs();
    return {
      highlightMode: isHighlightMode(parsed.highlightMode) ? parsed.highlightMode : defaults.highlightMode,
      sections: parsed.sections && typeof parsed.sections === 'object' ? parsed.sections : {},
      standingsSort: isStandingsSort(parsed.standingsSort) ? parsed.standingsSort : defaults.standingsSort,
      chatChannelKey: typeof parsed.chatChannelKey === 'string' ? parsed.chatChannelKey : defaults.chatChannelKey,
      chatScrollTop: Number.isFinite(parsed.chatScrollTop) ? Number(parsed.chatScrollTop) : 0,
    };
  } catch {
    return null;
  }
}

export function writeStoredPrefs(campaignId: string, prefs: CampaignViewPrefs): void {
  document.cookie = `${cookieNameFor(campaignId)}=${encodeURIComponent(JSON.stringify(prefs))}; Path=/; Max-Age=${COOKIE_MAX_AGE_SECONDS}; SameSite=Lax`;
}

export function sortStandings(rows: readonly CampaignPointStanding[], sort: StandingsSort): CampaignPointStanding[] {
  const direction = sort.direction === 'asc' ? 1 : -1;
  return [...rows].sort((left, right) => {
    const compared = compareStandingValue(left, right, sort.column);
    if (compared !== 0) {
      return compared * direction;
    }

    return left.displayName.localeCompare(right.displayName, undefined, { sensitivity: 'base' });
  });
}

export function nextStandingsSort(current: StandingsSort, column: StandingsSortColumn): StandingsSort {
  if (current.column === column) {
    return { column, direction: current.direction === 'asc' ? 'desc' : 'asc' };
  }

  const numeric =
    column === 'territory' || column === 'battles' || column === 'public' || column === 'other' || column === 'total';
  return { column, direction: numeric ? 'desc' : 'asc' };
}

function compareStandingValue(
  left: CampaignPointStanding,
  right: CampaignPointStanding,
  column: StandingsSortColumn,
): number {
  switch (column) {
    case 'displayName':
      return left.displayName.localeCompare(right.displayName, undefined, { sensitivity: 'base' });
    case 'faction':
      return (left.factionName ?? '').localeCompare(right.factionName ?? '', undefined, { sensitivity: 'base' });
    case 'allyGroup':
      return (left.allyGroupName ?? '').localeCompare(right.allyGroupName ?? '', undefined, {
        sensitivity: 'base',
      });
    case 'territory':
      return left.territoryAndStructurePoints - right.territoryAndStructurePoints;
    case 'battles':
      return left.battlesWonPoints - right.battlesWonPoints;
    case 'public':
      return left.publicObjectivePoints - right.publicObjectivePoints;
    case 'other':
      return left.otherPoints - right.otherPoints;
    case 'total':
      return left.total - right.total;
  }
}

function isHighlightMode(value: unknown): value is MapHighlightMode {
  return value === 'configured' || value === 'faction' || value === 'alliance';
}

function isStandingsSort(value: unknown): value is StandingsSort {
  if (!value || typeof value !== 'object') {
    return false;
  }

  const sort = value as { column?: unknown; direction?: unknown };
  const columns: readonly StandingsSortColumn[] = [
    'displayName',
    'faction',
    'allyGroup',
    'territory',
    'battles',
    'public',
    'other',
    'total',
  ];
  return columns.some((column) => column === sort.column) && (sort.direction === 'asc' || sort.direction === 'desc');
}
