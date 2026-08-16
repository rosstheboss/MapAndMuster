import type { PlayLogEntry } from './campaign.models';

export const CAMPAIGN_LOG_POLL_MS = 3_000;
export const CAMPAIGN_LOG_COMPOSER_MIN_LINES = 1;
export const CAMPAIGN_LOG_COMPOSER_MAX_LINES = 5;

export interface CampaignLogMember {
  userId: string;
  username: string;
  displayName: string;
}

export interface CampaignLogSync {
  revision: number;
  log: PlayLogEntry[];
  canChat: boolean;
  mentionableMembers: CampaignLogMember[];
}

export function mergeCampaignLog<T extends CampaignLogSync>(current: T, incoming: CampaignLogSync): T {
  return {
    ...current,
    revision: incoming.revision,
    log: incoming.log,
    canChat: incoming.canChat,
    mentionableMembers: incoming.mentionableMembers,
  };
}

export function campaignLogComposerSize(
  scrollHeight: number,
  lineHeight: number,
  verticalChrome: number,
): { height: number; overflowY: 'hidden' | 'auto' } {
  const min = lineHeight * CAMPAIGN_LOG_COMPOSER_MIN_LINES + verticalChrome;
  const max = lineHeight * CAMPAIGN_LOG_COMPOSER_MAX_LINES + verticalChrome;
  return {
    height: Math.min(Math.max(scrollHeight, min), max),
    overflowY: scrollHeight > max ? 'auto' : 'hidden',
  };
}

export interface LogMessagePart {
  text: string;
  mention: boolean;
}

export function formatLogTimestamp(value: string, timeZone?: string | null): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return '';
  }

  const zone = timeZone?.trim() ? timeZone.trim() : 'UTC';
  try {
    return formatParts(date, zone);
  } catch {
    return formatParts(date, 'UTC');
  }
}

export function splitLogMessage(text: string, members: readonly CampaignLogMember[]): LogMessagePart[] {
  const tokens = mentionTokens(members);
  const parts: LogMessagePart[] = [];
  let index = 0;
  let buffer = '';
  const flush = (mention: boolean): void => {
    if (buffer.length === 0) {
      return;
    }

    parts.push({ text: buffer, mention });
    buffer = '';
  };

  while (index < text.length) {
    if (text[index] === '\\' && text[index + 1] === '@') {
      buffer += '@';
      index += 2;
      continue;
    }

    if (text[index] === '@' && isMentionStart(text, index)) {
      const remainder = text.slice(index + 1);
      const match = tokens
        .filter((token) => remainder.toLowerCase().startsWith(token.toLowerCase()))
        .sort((left, right) => right.length - left.length)[0];
      if (match) {
        flush(false);
        parts.push({ text: `@${text.slice(index + 1, index + 1 + match.length)}`, mention: true });
        index += 1 + match.length;
        continue;
      }
    }

    buffer += text[index];
    index += 1;
  }

  flush(false);
  return parts;
}

export function mentionQuery(text: string, cursor: number): { start: number; query: string } | null {
  const before = text.slice(0, cursor);
  const at = before.lastIndexOf('@');
  if (at < 0 || !isMentionStart(text, at)) {
    return null;
  }

  if (at > 0 && text[at - 1] === '\\') {
    return null;
  }

  const query = before.slice(at + 1);
  if (query.includes(' ') && !query.trim()) {
    return null;
  }

  return { start: at, query };
}

function isMentionStart(text: string, atIndex: number): boolean {
  if (atIndex < 0 || atIndex >= text.length || text[atIndex] !== '@') {
    return false;
  }

  if (atIndex > 0 && text[atIndex - 1] === '\\') {
    return false;
  }

  if (atIndex === 0) {
    return true;
  }

  return !/[A-Za-z0-9]/.test(text[atIndex - 1] ?? '');
}

function mentionTokens(members: readonly CampaignLogMember[]): string[] {
  const usernames = new Set(members.map((member) => member.username));
  const usernameKeys = new Set([...usernames].map((name) => name.toLowerCase()));
  const displays = new Map<string, number>();
  for (const member of members) {
    if (!member.displayName || usernameKeys.has(member.displayName.toLowerCase())) {
      continue;
    }

    displays.set(member.displayName, (displays.get(member.displayName) ?? 0) + 1);
  }

  return [...usernames, ...[...displays.entries()].filter(([, count]) => count === 1).map(([name]) => name)];
}

function formatParts(date: Date, timeZone: string): string {
  const parts = new Intl.DateTimeFormat('en-US', {
    timeZone,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hour12: true,
    timeZoneName: 'short',
  }).formatToParts(date);
  const read = (type: Intl.DateTimeFormatPartTypes): string => parts.find((part) => part.type === type)?.value ?? '';
  return `(${read('year')}-${read('month')}-${read('day')} ${read('hour')}:${read('minute')}:${read('second')} ${read('dayPeriod')} ${read('timeZoneName')})`;
}
