import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { AuthService, readApiError } from './auth.service';

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
});
