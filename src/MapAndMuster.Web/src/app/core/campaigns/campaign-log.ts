import type { ChatChannel, PlayLogEntry } from './campaign.models';

export const CAMPAIGN_LOG_POLL_MS = 3_000;
export const CAMPAIGN_LOG_COMPOSER_MIN_LINES = 1;
export const CAMPAIGN_LOG_COMPOSER_MAX_LINES = 5;

export type CampaignLogExportFormat = 'txt' | 'csv';

export interface CampaignLogExportRequest {
  includePublicChat: boolean;
  includeGameLog: boolean;
  format: CampaignLogExportFormat;
}

export interface CampaignLogMember {
  userId: string;
  username: string;
  displayName: string;
}

export interface CampaignLogSync {
  revision: number;
  log: PlayLogEntry[];
  canChat: boolean;
  canInspectPrivateChat?: boolean;
  mentionableMembers: CampaignLogMember[];
  chatChannels?: ChatChannel[];
}

export function mergeCampaignLog<T extends CampaignLogSync>(current: T, incoming: CampaignLogSync): T {
  return {
    ...current,
    revision: incoming.revision,
    log: incoming.log,
    canChat: incoming.canChat,
    canInspectPrivateChat: incoming.canInspectPrivateChat,
    mentionableMembers: incoming.mentionableMembers,
    chatChannels: incoming.chatChannels,
  };
}

export function filterCampaignLog(
  entries: readonly PlayLogEntry[],
  showPublicChat: boolean,
  showPrivateChat: boolean,
  showGameLog: boolean,
): PlayLogEntry[] {
  return entries.filter((entry) => {
    if (entry.kind === 'PlayerChat') {
      return entry.isPrivate ? showPrivateChat : showPublicChat;
    }

    return showGameLog;
  });
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
  username?: string | null;
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
  const tokens = mentionTargets(members);
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
        .filter((target) => remainder.toLowerCase().startsWith(target.token.toLowerCase()))
        .sort((left, right) => right.token.length - left.token.length)
        .at(0);
      if (match) {
        flush(false);
        parts.push({
          text: `@${text.slice(index + 1, index + 1 + match.token.length)}`,
          mention: true,
          username: match.username,
        });
        index += 1 + match.token.length;
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

function mentionTargets(members: readonly CampaignLogMember[]): { token: string; username: string }[] {
  const usernames = new Set(members.map((member) => member.username));
  const usernameKeys = new Set([...usernames].map((name) => name.toLowerCase()));
  const targets = members.map((member) => ({ token: member.username, username: member.username }));
  const displays = new Map<string, CampaignLogMember[]>();
  for (const member of members) {
    if (!member.displayName || usernameKeys.has(member.displayName.toLowerCase())) {
      continue;
    }

    const listed = displays.get(member.displayName) ?? [];
    listed.push(member);
    displays.set(member.displayName, listed);
  }

  for (const [name, listed] of displays) {
    if (listed.length === 1 && listed[0]) {
      targets.push({ token: name, username: listed[0].username });
    }
  }

  return targets;
}

export function recipientSearchTexts(channel: ChatChannel, members: readonly CampaignLogMember[]): string[] {
  const texts = [channel.label];
  if (channel.kind === 'Public') {
    texts.push('Everyone', 'public');
  }

  if (channel.kind === 'Direct' && channel.targetId) {
    const member = members.find((item) => item.userId === channel.targetId);
    if (member) {
      texts.push(member.username, member.displayName);
    }
  }

  return texts;
}

export function filterChatRecipients(
  channels: readonly ChatChannel[],
  members: readonly CampaignLogMember[],
  query: string,
): ChatChannel[] {
  const needle = query.trim().toLowerCase();
  if (needle.length === 0) {
    return [...channels];
  }

  return channels.filter((channel) =>
    recipientSearchTexts(channel, members).some((text) => text.toLowerCase().includes(needle)),
  );
}

export function matchChatRecipient(
  channels: readonly ChatChannel[],
  members: readonly CampaignLogMember[],
  query: string,
): ChatChannel | null {
  const needle = query.trim().toLowerCase();
  if (needle.length === 0) {
    return null;
  }

  const exact = channels.filter((channel) =>
    recipientSearchTexts(channel, members).some((text) => text.toLowerCase() === needle),
  );
  if (exact.length === 1) {
    return exact[0];
  }

  const filtered = filterChatRecipients(channels, members, query);
  return filtered.length === 1 ? filtered[0] : null;
}

export function recipientFieldLabel(channel: ChatChannel, members: readonly CampaignLogMember[]): string {
  if (channel.kind === 'Public') {
    return 'Everyone';
  }

  if (channel.kind === 'Direct' && channel.targetId) {
    const member = members.find((item) => item.userId === channel.targetId);
    if (member) {
      return member.username;
    }
  }

  return channel.label;
}

export function recipientSuggestionLabel(channel: ChatChannel, members: readonly CampaignLogMember[]): string {
  if (channel.kind === 'Public') {
    return 'Everyone';
  }

  if (channel.kind === 'Direct' && channel.targetId) {
    const member = members.find((item) => item.userId === channel.targetId);
    if (member) {
      if (member.displayName.toLowerCase() === member.username.toLowerCase()) {
        return member.username;
      }

      return `${member.displayName} (${member.username})`;
    }
  }

  return channel.label;
}

export function filenameFromContentDisposition(header: string | null, fallback: string): string {
  if (!header) {
    return fallback;
  }

  const utf = /filename\*=UTF-8''([^;]+)/i.exec(header);
  if (utf?.[1]) {
    try {
      return decodeURIComponent(utf[1]);
    } catch {
      return utf[1];
    }
  }

  const quoted = /filename="([^"]+)"/i.exec(header);
  if (quoted?.[1]) {
    return quoted[1];
  }

  const plain = /filename=([^;]+)/i.exec(header);
  const value = plain?.[1]?.trim();
  return value && value.length > 0 ? value : fallback;
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
