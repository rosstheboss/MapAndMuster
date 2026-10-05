import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import {
  provideRouter,
  Router,
  type ActivatedRouteSnapshot,
  type RouterStateSnapshot,
  type UrlTree,
} from '@angular/router';

import type { OwnProfile } from './auth.models';
import { AuthService } from './auth.service';
import { adminGuard, authGuard, guestGuard } from './auth.guard';

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

function routeWith(query: Record<string, string> = {}): ActivatedRouteSnapshot {
  return {
    queryParamMap: { get: (key: string) => query[key] ?? null },
  } as ActivatedRouteSnapshot;
}

function stateWith(url: string): RouterStateSnapshot {
  return { url } as RouterStateSnapshot;
}

describe('auth guards', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
  });

  it('sends a signed-out visitor to login with the page they asked for', async () => {
    const pending = TestBed.runInInjectionContext(() => authGuard(routeWith(), stateWith('/campaigns/abc')));
    TestBed.inject(HttpTestingController)
      .expectOne('/api/auth/me')
      .flush(null, { status: 401, statusText: 'Unauthorized' });
    const result = await pending;
    expect(TestBed.inject(Router).serializeUrl(result as UrlTree)).toBe('/login?returnUrl=%2Fcampaigns%2Fabc');
  });

  it('lets a signed-in visitor through', async () => {
    TestBed.inject(AuthService).currentUser.set(profile);
    const result = await TestBed.runInInjectionContext(() => authGuard(routeWith(), stateWith('/campaigns/abc')));
    expect(result).toBe(true);
  });

  it('sends an already signed-in visitor away from login to their return page', async () => {
    TestBed.inject(AuthService).currentUser.set(profile);
    const result = await TestBed.runInInjectionContext(() =>
      guestGuard(routeWith({ returnUrl: '/profile' }), stateWith('/login?returnUrl=%2Fprofile')),
    );
    expect(TestBed.inject(Router).serializeUrl(result as UrlTree)).toBe('/profile');
  });

  it('keeps guest preview on the login page', async () => {
    TestBed.inject(AuthService).currentUser.set({ ...profile, isGuestAccount: true });
    const result = await TestBed.runInInjectionContext(() => guestGuard(routeWith(), stateWith('/login')));
    expect(result).toBe(true);
  });

  it('sends a non-administrator home', async () => {
    TestBed.inject(AuthService).currentUser.set(profile);
    const result = await TestBed.runInInjectionContext(() => adminGuard(routeWith(), stateWith('/admin/test-users')));
    expect(TestBed.inject(Router).serializeUrl(result as UrlTree)).toBe('/');
  });
});
