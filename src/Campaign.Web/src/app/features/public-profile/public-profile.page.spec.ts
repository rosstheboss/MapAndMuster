import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';

import type { OwnProfile, PublicProfile } from '../../core/auth/auth.models';
import { AuthService } from '../../core/auth/auth.service';
import { PublicProfilePage } from './public-profile.page';

const ownAccount: OwnProfile = {
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

const publicProfile = (username: string): PublicProfile => ({
  username,
  displayName: username,
  showsFullName: false,
  city: 'Halifax',
  region: 'Nova Scotia',
  country: 'Canada',
  hasAvatar: false,
  campaigns: [
    {
      id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
      name: 'Border War',
      status: 'Scheduled',
      isPrivate: false,
    },
  ],
});

describe('PublicProfilePage', () => {
  async function createPage(
    username: string,
    from?: string,
  ): Promise<{
    compiled: HTMLElement;
    http: HttpTestingController;
  }> {
    await TestBed.configureTestingModule({
      imports: [PublicProfilePage],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: convertToParamMap({ username }),
              queryParamMap: convertToParamMap(from ? { from } : {}),
            },
            queryParamMap: of(convertToParamMap(from ? { from } : {})),
          },
        },
      ],
    }).compileComponents();

    const auth = TestBed.inject(AuthService);
    auth.currentUser.set(ownAccount);
    const fixture = TestBed.createComponent(PublicProfilePage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/profiles/${username}`).flush(publicProfile(username));
    await fixture.whenStable();
    fixture.detectChanges();
    return { compiled: fixture.nativeElement as HTMLElement, http };
  }

  it('links back to edit profile when the viewer is looking at their own page', async () => {
    const { compiled, http } = await createPage('ada');
    expect(compiled.textContent).toContain('Username: ada');
    expect(compiled.querySelector('a[href="/profile"]')?.textContent.trim()).toBe('Back to edit profile');
    http.verify();
  });

  it('does not offer edit-profile navigation on another player page', async () => {
    const { compiled, http } = await createPage('northplayer');
    expect(compiled.textContent).toContain('Username: northplayer');
    expect(compiled.querySelector('a[href="/profile"]')).toBeNull();
    expect(compiled.textContent).toContain('Border War');
    expect(compiled.textContent).toContain('Scores and rankings are not available yet.');
    http.verify();
  });

  it('offers a back link to the screen that opened the profile', async () => {
    const { compiled, http } = await createPage('northplayer', '/campaigns/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa');
    expect(
      compiled.querySelector('a[href="/campaigns/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"]')?.textContent.trim(),
    ).toBe('Back');
    http.verify();
  });
});
