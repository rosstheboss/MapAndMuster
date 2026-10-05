import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter, Router } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';
import { peekReturnUrl } from '../../core/auth/return-url';
import { LoginPage } from './login.page';

const signedIn = {
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

describe('LoginPage', () => {
  let returnUrl: string | null = null;

  beforeEach(async () => {
    returnUrl = null;
    sessionStorage.clear();
    await TestBed.configureTestingModule({
      imports: [LoginPage],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: ActivatedRoute,
          useFactory: () => ({
            snapshot: {
              queryParamMap: {
                get: (key: string) => (key === 'returnUrl' ? returnUrl : null),
              },
            },
          }),
        },
      ],
    }).compileComponents();
  });

  it('renders the sign-in form', async () => {
    const fixture = TestBed.createComponent(LoginPage);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/auth/external-providers').flush([]);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Sign in');
    expect(compiled.querySelector('#email')).toBeTruthy();
    expect(compiled.querySelector('#password')).toBeTruthy();
    expect(compiled.querySelector('#password')?.getAttribute('type')).toBe('password');
    expect(compiled.querySelector('[aria-label="Show password"]')).toBeTruthy();
    expect(compiled.querySelector('button[type="submit"]')?.textContent).toContain('Sign in');
    expect(compiled.textContent).toContain('Login as Guest');
    http.verify();
  });

  it('renders a branded Google button when the provider is configured', async () => {
    const fixture = TestBed.createComponent(LoginPage);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/auth/external-providers').flush([{ name: 'Google', displayName: 'Google' }]);
    await new Promise((resolve) => setTimeout(resolve, 0));
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const google = compiled.querySelector('button.google');
    expect(google?.textContent).toContain('Continue with Google');
    expect(google?.querySelector('svg')).toBeTruthy();
    http.verify();
  });

  it('returns to the requested page after sign-in and guest preview', async () => {
    returnUrl = '/campaigns/abc';
    const fixture = TestBed.createComponent(LoginPage);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/auth/external-providers').flush([]);
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    const page = fixture.componentInstance as unknown as {
      form: {
        controls: { email: { setValue: (value: string) => void }; password: { setValue: (value: string) => void } };
      };
      submit: () => Promise<void>;
      loginAsGuest: () => Promise<void>;
    };
    page.form.controls.email.setValue('ada@example.test');
    page.form.controls.password.setValue('Correct-Horse-1');
    const signingIn = page.submit();
    http.expectOne('/api/auth/login').flush(signedIn);
    await signingIn;
    expect(navigate).toHaveBeenCalledWith('/campaigns/abc');

    navigate.mockClear();
    const guest = page.loginAsGuest();
    http.expectOne('/api/auth/guest-login').flush({ ...signedIn, username: 'Guest001', isGuestAccount: true });
    await guest;
    expect(navigate).toHaveBeenCalledWith('/campaigns/abc');
    http.verify();
  });

  it('goes home when the return address is unsafe', async () => {
    returnUrl = 'https://evil.example/campaigns';
    const fixture = TestBed.createComponent(LoginPage);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/auth/external-providers').flush([]);
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    const page = fixture.componentInstance as unknown as {
      form: {
        controls: { email: { setValue: (value: string) => void }; password: { setValue: (value: string) => void } };
      };
      submit: () => Promise<void>;
    };
    page.form.controls.email.setValue('ada@example.test');
    page.form.controls.password.setValue('Correct-Horse-1');
    const signingIn = page.submit();
    http.expectOne('/api/auth/login').flush(signedIn);
    await signingIn;
    expect(navigate).toHaveBeenCalledWith('/');
    expect((fixture.nativeElement as HTMLElement).querySelector('a[href="/register"]')?.textContent).toContain(
      'Create one',
    );
    http.verify();
  });

  it('remembers the return address for an existing external login', async () => {
    returnUrl = '/campaigns/abc';
    const fixture = TestBed.createComponent(LoginPage);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/auth/external-providers').flush([{ name: 'Google', displayName: 'Google' }]);
    await new Promise((resolve) => setTimeout(resolve, 0));
    fixture.detectChanges();
    vi.spyOn(TestBed.inject(AuthService), 'startExternalLogin').mockImplementation(() => undefined);
    const google = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('button.google');
    expect(google).toBeTruthy();
    google?.click();
    expect(peekReturnUrl()).toBe('/campaigns/abc');
    http.verify();
  });
});
