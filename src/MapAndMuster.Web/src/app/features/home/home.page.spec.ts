import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import type { OwnProfile } from '../../core/auth/auth.models';
import { AuthService } from '../../core/auth/auth.service';
import type { CampaignListItem } from '../../core/campaigns/campaign.models';
import { HomePage } from './home.page';

function campaignItem(
  overrides: Partial<CampaignListItem> & Pick<CampaignListItem, 'id' | 'name' | 'status' | 'startsUtc' | 'endsUtc'>,
): CampaignListItem {
  return {
    description: null,
    playerSlotCount: 8,
    occupiedPlayerSlots: 4,
    isPrivate: false,
    isPubliclyViewable: true,
    canManage: false,
    isParticipant: true,
    canView: true,
    canJoin: false,
    canLeave: false,
    city: null,
    region: null,
    country: null,
    currentRound: null,
    currentPhaseLabel: null,
    currentPhaseKind: null,
    currentPhaseEndsUtc: null,
    canPlay: false,
    canChooseFaction: false,
    isCommitted: false,
    ...overrides,
  };
}

function flushHomeBoard(
  http: HttpTestingController,
  options?: { campaigns?: CampaignListItem[]; notifications?: unknown[]; news?: unknown },
): void {
  http.expectOne('/api/notifications').flush(options?.notifications ?? []);
  http
    .expectOne((request) => request.url === '/api/news')
    .flush(options?.news ?? { page: 1, totalPages: 0, articles: [], article: null });
  http.expectOne('/api/campaigns').flush(options?.campaigns ?? []);
}

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
  preferredChatLanguage: 'English',
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
    flushHomeBoard(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).not.toContain('Edit profile');
    expect(compiled.textContent).not.toContain('View public profile');
    expect(compiled.textContent).toContain('Needs your attention');
    expect(compiled.textContent).toContain('You are not in a running campaign.');
    expect(compiled.querySelector('a[href="/campaigns/all"]')?.textContent).toContain('Join campaign');
    expect(compiled.querySelector('a[href="/campaigns/new"]')?.textContent).toContain('Create a campaign');
    expect(compiled.textContent).toContain('No new notifications.');
    expect(compiled.textContent).toContain('No news has been published yet.');
    const discord = compiled.querySelector<HTMLAnchorElement>('.discord-invite a');
    expect(discord?.getAttribute('href')).toBe('https://discord.gg/ATVt97DMnx');
    expect(discord?.textContent).toContain('Join the Discord server');
    const headings = [...compiled.querySelectorAll('h2')].map((node) => node.textContent.trim());
    expect(headings).toEqual(['Needs your attention', 'Notifications', 'News']);
    http.verify();
  });

  it('lists attention items that open a campaign', async () => {
    const auth = TestBed.inject(AuthService);
    auth.currentUser.set(profile);

    const fixture = TestBed.createComponent(HomePage);
    const http = TestBed.inject(HttpTestingController);
    flushHomeBoard(http, {
      notifications: [
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
      ],
      news: {
        page: 1,
        totalPages: 1,
        articles: [
          {
            id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
            title: 'Season opening',
            bodyMarkdown: 'Hello **players**.',
            bodyHtml: '<p>Hello <strong>players</strong>.</p>',
            publishedUtc: '2026-08-16T00:00:00+00:00',
            updatedUtc: '2026-08-16T00:00:00+00:00',
          },
        ],
        article: {
          id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
          title: 'Season opening',
          bodyMarkdown: 'Hello **players**.',
          bodyHtml: '<p>Hello <strong>players</strong>.</p>',
          publishedUtc: '2026-08-16T00:00:00+00:00',
          updatedUtc: '2026-08-16T00:00:00+00:00',
        },
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

  it('pages notifications five at a time and can dismiss them', async () => {
    const auth = TestBed.inject(AuthService);
    auth.currentUser.set(profile);

    const fixture = TestBed.createComponent(HomePage);
    const http = TestBed.inject(HttpTestingController);
    flushHomeBoard(http, {
      notifications: Array.from({ length: 6 }, (_, index) => ({
        id: `11111111-1111-1111-1111-11111111111${index}`,
        kind: 'CampaignChat',
        campaignId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
        campaignName: 'Border War',
        title: `Notice ${index + 1}`,
        body: `Body ${index + 1}`,
        path: '/campaigns/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
        createdUtc: '2026-08-16T00:00:00+00:00',
      })),
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Notice 1');
    expect(compiled.textContent).toContain('Notice 5');
    expect(compiled.textContent).not.toContain('Notice 6');
    expect(compiled.textContent).toContain('Dismiss all');
    const next = [...compiled.querySelectorAll('button')].find((button) => button.textContent.trim() === 'Next');
    next?.dispatchEvent(new Event('click'));
    fixture.detectChanges();
    expect(compiled.textContent).toContain('Notice 6');
    expect(compiled.textContent).not.toContain('Notice 1');
    const dismiss = [...compiled.querySelectorAll('button')].find((button) => button.textContent.trim() === 'Dismiss');
    dismiss?.dispatchEvent(new Event('click'));
    const read = http.expectOne((request) => request.url.endsWith('/read') && request.method === 'POST');
    read.flush(null);
    await fixture.whenStable();
    fixture.detectChanges();
    expect(compiled.textContent).not.toContain('Notice 6');
    http.verify();
  });

  it('dismisses every notification from the heading button', async () => {
    const auth = TestBed.inject(AuthService);
    auth.currentUser.set(profile);

    const fixture = TestBed.createComponent(HomePage);
    const http = TestBed.inject(HttpTestingController);
    flushHomeBoard(http, {
      notifications: Array.from({ length: 2 }, (_, index) => ({
        id: `11111111-1111-1111-1111-11111111111${index}`,
        kind: 'CampaignChat',
        campaignId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
        campaignName: 'Border War',
        title: `Notice ${index + 1}`,
        body: `Body ${index + 1}`,
        path: '/campaigns/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
        createdUtc: '2026-08-16T00:00:00+00:00',
      })),
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const dismissAll = [...compiled.querySelectorAll('button')].find(
      (button) => button.textContent.trim() === 'Dismiss all',
    );
    dismissAll?.dispatchEvent(new Event('click'));
    http.expectOne((request) => request.url === '/api/notifications/read-all' && request.method === 'POST').flush(null);
    await fixture.whenStable();
    fixture.detectChanges();
    expect(compiled.textContent).toContain('No new notifications.');
    http.verify();
  });

  it('marks compact notification ids read so dismiss survives a reload', async () => {
    const auth = TestBed.inject(AuthService);
    auth.currentUser.set(profile);

    const fixture = TestBed.createComponent(HomePage);
    const http = TestBed.inject(HttpTestingController);
    flushHomeBoard(http, {
      notifications: [
        {
          id: 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa',
          kind: 'CampaignChat',
          campaignId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
          campaignName: 'Border War',
          title: 'Compact notice',
          body: 'A stored mention.',
          path: '/campaigns/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
          createdUtc: '2026-08-16T00:00:00+00:00',
        },
      ],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const dismiss = [...compiled.querySelectorAll('button')].find((button) => button.textContent.trim() === 'Dismiss');
    dismiss?.dispatchEvent(new Event('click'));
    const read = http.expectOne(
      (request) =>
        request.method === 'POST' && request.url === '/api/notifications/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/read',
    );
    read.flush(null);
    await fixture.whenStable();
    fixture.detectChanges();
    expect(compiled.textContent).toContain('No new notifications.');
    http.verify();
  });

  it('shows two news articles then pages to older items', async () => {
    const auth = TestBed.inject(AuthService);
    auth.currentUser.set(profile);

    const fixture = TestBed.createComponent(HomePage);
    const http = TestBed.inject(HttpTestingController);
    flushHomeBoard(http, {
      news: {
        page: 1,
        totalPages: 2,
        articles: [
          {
            id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
            title: 'Newest bulletin',
            bodyMarkdown: 'First.',
            bodyHtml: '<p>First.</p>',
            publishedUtc: '2026-08-16T00:00:00+00:00',
            updatedUtc: '2026-08-16T00:00:00+00:00',
          },
          {
            id: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
            title: 'Second bulletin',
            bodyMarkdown: 'Second.',
            bodyHtml: '<p>Second.</p>',
            publishedUtc: '2026-08-15T00:00:00+00:00',
            updatedUtc: '2026-08-15T00:00:00+00:00',
          },
        ],
      },
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Newest bulletin');
    expect(compiled.textContent).toContain('Second bulletin');
    expect(compiled.textContent).not.toContain('Older bulletin');
    const next = [...compiled.querySelectorAll('button')].find((button) => button.textContent.trim() === 'Next');
    next?.dispatchEvent(new Event('click'));
    http
      .expectOne((request) => request.url === '/api/news' && request.params.get('page') === '2')
      .flush({
        page: 2,
        totalPages: 2,
        articles: [
          {
            id: 'dddddddd-dddd-dddd-dddd-dddddddddddd',
            title: 'Older bulletin',
            bodyMarkdown: 'Older.',
            bodyHtml: '<p>Older.</p>',
            publishedUtc: '2026-08-14T00:00:00+00:00',
            updatedUtc: '2026-08-14T00:00:00+00:00',
          },
        ],
      });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(compiled.textContent).toContain('Older bulletin');
    expect(compiled.textContent).not.toContain('Newest bulletin');
    http.verify();
  });

  it('lists in-progress campaigns above notifications', async () => {
    const auth = TestBed.inject(AuthService);
    auth.currentUser.set(profile);

    const fixture = TestBed.createComponent(HomePage);
    const http = TestBed.inject(HttpTestingController);
    flushHomeBoard(http, {
      campaigns: [
        campaignItem({
          id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
          name: 'Current War',
          status: 'InProgress',
          startsUtc: '2098-01-01T12:00:00+00:00',
          endsUtc: '2099-06-01T12:00:00+00:00',
          currentRound: 3,
          currentPhaseLabel: 'Action 1',
          currentPhaseKind: 'Action',
          currentPhaseEndsUtc: '2099-05-02T12:00:00+00:00',
          canPlay: true,
        }),
        campaignItem({
          id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
          name: 'Border War',
          status: 'Scheduled',
          startsUtc: '2099-01-05T12:00:00+00:00',
          endsUtc: '2099-03-02T12:00:00+00:00',
          canChooseFaction: true,
        }),
      ],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const attention = compiled.querySelector('#attention-heading')?.closest('section');
    expect(attention?.textContent).toContain('Current War');
    expect(attention?.textContent).toContain('Round 3 · Action 1');
    expect(attention?.textContent).toContain('Phase ends in');
    expect(attention?.textContent).toContain('Not committed');
    expect(attention?.textContent).not.toContain('Border War');
    expect(
      compiled.querySelector('a.notice-item[href="/campaigns/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"]'),
    ).toBeTruthy();
    expect(compiled.textContent).not.toContain('You are not in a running campaign.');
    http.verify();
  });

  it('points a player with only upcoming campaigns at Your campaigns', async () => {
    const auth = TestBed.inject(AuthService);
    auth.currentUser.set(profile);

    const fixture = TestBed.createComponent(HomePage);
    const http = TestBed.inject(HttpTestingController);
    flushHomeBoard(http, {
      campaigns: [
        campaignItem({
          id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
          name: 'Border War',
          status: 'Scheduled',
          startsUtc: '2099-01-05T12:00:00+00:00',
          endsUtc: '2099-03-02T12:00:00+00:00',
          canChooseFaction: true,
        }),
      ],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('None of your campaigns are in progress right now.');
    expect(compiled.querySelector('a[href="/campaigns"]')?.textContent).toContain('View your campaigns');
    http.verify();
  });
});
