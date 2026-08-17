import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import type { OwnProfile } from '../../core/auth/auth.models';
import { AuthService } from '../../core/auth/auth.service';
import { HomePage } from './home.page';

const profile: OwnProfile = {
  id: '11111111-1111-1111-1111-111111111111',
  email: 'ada@example.test',
  username: 'ada',
  firstName: 'Ada',
  middleInitial: null,
  lastName: 'Lovelace',
  suffix: null,
  city: 'Halifax',
  region: null,
  country: 'Canada',
  displayNameMode: 'Username',
  timeZoneId: null,
  hasAvatar: false,
  createdUtc: '2026-08-13T00:00:00+00:00',
  updatedUtc: '2026-08-13T00:00:00+00:00',
  profileRevision: 1,
  emailConfirmed: true,
  isAdministrator: false,
  inAppNotificationsEnabled: true,
  emailNotificationsEnabled: true,
};

describe('HomePage', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HomePage],
      providers: [provideZonelessChangeDetection(), provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  it('shows an empty notification board above news', async () => {
    const auth = TestBed.inject(AuthService);
    auth.currentUser.set(profile);

    const fixture = TestBed.createComponent(HomePage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/notifications').flush([]);
    http.expectOne((request) => request.url === '/api/news').flush({ page: 1, totalPages: 0, article: null });
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).not.toContain('Edit profile');
    expect(compiled.textContent).not.toContain('View public profile');
    expect(compiled.textContent).toContain('No new notifications.');
    expect(compiled.textContent).toContain('No news has been published yet.');
    const headings = [...compiled.querySelectorAll('h2')].map((node) => node.textContent.trim());
    expect(headings).toEqual(['Notifications', 'News']);
    http.verify();
  });

  it('lists attention items that open a campaign', async () => {
    const auth = TestBed.inject(AuthService);
    auth.currentUser.set(profile);

    const fixture = TestBed.createComponent(HomePage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/notifications').flush([
      {
        id: 'orders-1',
        kind: 'ActionRequired',
        campaignId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
        campaignName: 'Border War',
        title: 'Orders needed',
        body: 'Submit and commit orders.',
        path: '/campaigns/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
        createdUtc: '2026-08-16T00:00:00+00:00',
      },
    ]);
    http
      .expectOne((request) => request.url === '/api/news')
      .flush({
        page: 1,
        totalPages: 1,
        article: {
          id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
          title: 'Season opening',
          bodyMarkdown: 'Hello **players**.',
          bodyHtml: '<p>Hello <strong>players</strong>.</p>',
          publishedUtc: '2026-08-16T00:00:00+00:00',
          updatedUtc: '2026-08-16T00:00:00+00:00',
        },
      });
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Orders needed');
    expect(compiled.textContent).toContain('Border War');
    expect(compiled.textContent).toContain('Season opening');
    http.verify();
  });
});
