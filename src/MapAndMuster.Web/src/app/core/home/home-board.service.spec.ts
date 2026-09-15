import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import {
  HomeBoardService,
  sortNotificationsNewestFirst,
  storedNotificationRouteId,
  type HomeAttentionItem,
} from './home-board.service';

function noticeItem(
  overrides: Partial<HomeAttentionItem> & Pick<HomeAttentionItem, 'id' | 'title' | 'createdUtc'>,
): HomeAttentionItem {
  return {
    kind: 'CampaignChat',
    campaignId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    campaignName: 'Border War',
    body: 'You were mentioned.',
    path: '/campaigns/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    ...overrides,
  };
}

describe('HomeBoardService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting()],
    });
  });

  it('turns compact stored notice ids into dashed route ids', () => {
    expect(storedNotificationRouteId('aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa')).toBe('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa');
    expect(storedNotificationRouteId('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa')).toBe(
      'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    );
    expect(storedNotificationRouteId('orders:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa')).toBeNull();
  });

  it('posts dashed ids when marking a compact stored notice read', async () => {
    const board = TestBed.inject(HomeBoardService);
    const http = TestBed.inject(HttpTestingController);
    const pending = board.markRead('aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa');
    http.expectOne('/api/notifications/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/read').flush(null);
    await pending;
    http.verify();
  });

  it('returns unread notices newest first', async () => {
    const board = TestBed.inject(HomeBoardService);
    const http = TestBed.inject(HttpTestingController);
    const pending = board.listNotifications();
    http
      .expectOne('/api/notifications')
      .flush([
        noticeItem({ id: 'older', title: 'Older mention', createdUtc: '2026-08-14T00:00:00+00:00' }),
        noticeItem({ id: 'newest', title: 'Newest mention', createdUtc: '2026-08-16T12:00:00+00:00' }),
        noticeItem({ id: 'middle', title: 'Middle mention', createdUtc: '2026-08-15T08:00:00+00:00' }),
      ]);
    await expect(pending).resolves.toEqual([
      expect.objectContaining({ title: 'Newest mention' }),
      expect.objectContaining({ title: 'Middle mention' }),
      expect.objectContaining({ title: 'Older mention' }),
    ]);
    http.verify();
  });

  it('sorts notices with the same instant by id descending', () => {
    expect(
      sortNotificationsNewestFirst([
        noticeItem({ id: 'a', title: 'First', createdUtc: '2026-08-16T12:00:00+00:00' }),
        noticeItem({ id: 'c', title: 'Third', createdUtc: '2026-08-16T12:00:00+00:00' }),
        noticeItem({ id: 'b', title: 'Second', createdUtc: '2026-08-16T12:00:00+00:00' }),
      ]).map((item) => item.id),
    ).toEqual(['c', 'b', 'a']);
  });

  it('falls back to per-notice reads when dismiss-all is missing', async () => {
    const board = TestBed.inject(HomeBoardService);
    const http = TestBed.inject(HttpTestingController);
    const pending = board.markAllRead(['aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa', 'orders:1']);
    http.expectOne('/api/notifications/read-all').flush('Not found', { status: 404, statusText: 'Not Found' });
    await Promise.resolve();
    http.expectOne('/api/notifications/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/read').flush(null);
    await pending;
    http.verify();
  });
});
