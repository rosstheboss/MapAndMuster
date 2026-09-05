import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { HomeBoardService, storedNotificationRouteId } from './home-board.service';

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
