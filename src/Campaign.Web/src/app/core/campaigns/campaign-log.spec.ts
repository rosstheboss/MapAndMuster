import {
  campaignLogComposerSize,
  filenameFromContentDisposition,
  filterChatRecipients,
  formatLogTimestamp,
  matchChatRecipient,
  mentionQuery,
  mergeCampaignLog,
  recipientFieldLabel,
  recipientSuggestionLabel,
  splitLogMessage,
  type CampaignLogSync,
} from './campaign-log';
import type { ChatChannel } from './campaign.models';

describe('campaign log formatting', () => {
  it('formats a timestamp in the viewer time zone', () => {
    expect(formatLogTimestamp('2026-08-15T20:45:23-04:00', 'America/New_York')).toBe('(2026-08-15 08:45:23 PM EDT)');
  });

  it('highlights member mentions and treats escaped at-signs as text', () => {
    const parts = splitLogMessage('Hi @southplayer and \\@stranger', [
      { userId: '1', username: 'northplayer', displayName: 'northplayer' },
      { userId: '2', username: 'southplayer', displayName: 'Ada Lovelace' },
    ]);
    expect(parts).toEqual([
      { text: 'Hi ', mention: false },
      { text: '@southplayer', mention: true, username: 'southplayer' },
      { text: ' and @stranger', mention: false },
    ]);
  });

  it('finds an in-progress mention query for autocomplete', () => {
    expect(mentionQuery('Hello @nor', 10)).toEqual({ start: 6, query: 'nor' });
    expect(mentionQuery('Write ada@example.test', 22)).toBeNull();
  });

  it('replaces log fields without dropping the rest of the campaign snapshot', () => {
    const current: CampaignLogSync & { name: string } = {
      revision: 1,
      log: [],
      canChat: true,
      mentionableMembers: [],
      name: 'Border War',
    };
    const next = mergeCampaignLog(current, {
      revision: 2,
      log: [
        {
          id: 'log-1',
          occurredUtc: '2026-08-15T20:46:23-04:00',
          kind: 'PlayerChat',
          originator: 'northplayer',
          summary: 'Ready to play',
          territoryId: null,
          forceId: null,
          battleId: null,
          isSystemAdjustment: false,
        },
      ],
      canChat: true,
      mentionableMembers: [{ userId: '1', username: 'northplayer', displayName: 'northplayer' }],
    });
    expect(next.name).toBe('Border War');
    expect(next.revision).toBe(2);
    expect(next.log[0]?.summary).toBe('Ready to play');
  });

  it('grows the composer from one to five lines and then scrolls', () => {
    const line = 20;
    const chrome = 16;
    expect(campaignLogComposerSize(10, line, chrome)).toEqual({ height: 36, overflowY: 'hidden' });
    expect(campaignLogComposerSize(76, line, chrome)).toEqual({ height: 76, overflowY: 'hidden' });
    expect(campaignLogComposerSize(116, line, chrome)).toEqual({ height: 116, overflowY: 'hidden' });
    expect(campaignLogComposerSize(140, line, chrome)).toEqual({ height: 116, overflowY: 'auto' });
  });
});

describe('chat recipient matching', () => {
  const members = [
    { userId: '1', username: 'northplayer', displayName: 'northplayer' },
    { userId: '2', username: 'bobisthebest', displayName: 'Bob' },
  ];
  const channels: ChatChannel[] = [
    { kind: 'Public', targetId: null, label: 'Everyone' },
    { kind: 'Direct', targetId: '2', label: 'Bob' },
    { kind: 'Faction', targetId: 'north', label: 'North' },
  ];

  it('filters recipients by username, display name, and Everyone', () => {
    expect(filterChatRecipients(channels, members, 'bobis').map((channel) => channel.kind)).toEqual(['Direct']);
    expect(filterChatRecipients(channels, members, 'eve').map((channel) => channel.label)).toEqual(['Everyone']);
    expect(filterChatRecipients(channels, members, 'public').map((channel) => channel.kind)).toEqual(['Public']);
  });

  it('resolves a unique typed recipient including Everyone', () => {
    expect(matchChatRecipient(channels, members, 'Everyone')?.kind).toBe('Public');
    expect(matchChatRecipient(channels, members, 'bobisthebest')?.targetId).toBe('2');
    expect(recipientFieldLabel(channels[1], members)).toBe('bobisthebest');
    expect(recipientSuggestionLabel(channels[1], members)).toBe('Bob (bobisthebest)');
  });
});

describe('campaign log download filename', () => {
  it('reads a UTF-8 content-disposition file name', () => {
    expect(
      filenameFromContentDisposition(
        "attachment; filename=border-war-log.txt; filename*=UTF-8''border-war-log.txt",
        'campaign-log.txt',
      ),
    ).toBe('border-war-log.txt');
  });
});
