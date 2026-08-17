import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { AuthService, readApiError, readApiErrorMessages } from './auth.service';

describe('AuthService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting()],
    });
  });

  it('stores the current user after login', async () => {
    const service = TestBed.inject(AuthService);
    const http = TestBed.inject(HttpTestingController);
    const loginPromise = service.login('ada@example.test', 'Correct-Horse-1');
    const request = http.expectOne('/api/auth/login');
    request.flush({
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
    });

    await loginPromise;
    expect(service.currentUser()?.username).toBe('ada');
    http.verify();
  });

  it('reads API error messages', () => {
    const error = new HttpErrorResponse({
      status: 401,
      error: { code: 'auth.invalid_credentials', message: 'Email or password is incorrect.' },
    });
    expect(readApiError(error, 'fallback')).toBe('Email or password is incorrect.');
  });

  it('keeps each API field error as its own message', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: {
        code: 'validation.failed',
        message: 'Username is already taken.\nFirst name is too short (minimum 2 characters).',
        errors: [
          { field: 'username', code: 'username.taken', message: 'Username is already taken.' },
          { field: 'firstName', code: 'name.too_short', message: 'First name is too short (minimum 2 characters).' },
        ],
      },
    });
    expect(readApiErrorMessages(error, 'fallback')).toEqual([
      'Username is already taken.',
      'First name is too short (minimum 2 characters).',
    ]);
  });
});
