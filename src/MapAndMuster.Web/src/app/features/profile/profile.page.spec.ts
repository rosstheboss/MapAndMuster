import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { FORM_SAVE_SUCCESS_MESSAGE } from '../../core/forms/form-messages';
import { ProfilePage } from './profile.page';

describe('ProfilePage', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProfilePage],
      providers: [provideZonelessChangeDetection(), provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  it('requires profile fields and treats password changes as optional', async () => {
    const fixture = TestBed.createComponent(ProfilePage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/profiles/me').flush({
      id: '11111111-1111-1111-1111-111111111111',
      email: 'ada@example.test',
      username: 'ada',
      firstName: 'Ada',
      middleInitial: null,
      lastName: 'Lovelace',
      suffix: null,
      city: 'Halifax',
      region: 'Nova Scotia',
      country: 'Canada',
      displayNameMode: 'Username',
      timeZoneId: 'America/Halifax',
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
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('#currentPassword')).toBeTruthy();
    expect(compiled.querySelector('#newPassword')).toBeTruthy();
    expect(compiled.querySelector('#confirmPassword')).toBeTruthy();
    expect(compiled.querySelector('#suffix')).toBeTruthy();
    expect(compiled.querySelector('app-theme-toggle button')?.getAttribute('aria-pressed')).toBe('false');
    expect(compiled.querySelector('#preferredChatLanguage')).toBeTruthy();
    expect(compiled.querySelector('#dateTimeDisplayFormat')).toBeTruthy();
    expect(compiled.querySelector('a[href="/users/ada"]')?.textContent.trim()).toBe('View public profile');
    expect(compiled.querySelector('.field-row-identity')).toBeTruthy();
    expect(compiled.querySelector('.field-row-location')).toBeTruthy();
    expect(compiled.querySelector('.form-sticky-actions')).toBeTruthy();
    expect(compiled.textContent).toContain('Choose image');

    const page = fixture.componentInstance as unknown as {
      form: {
        controls: {
          firstName: { setValue: (value: string) => void };
          city: { setValue: (value: string) => void };
        };
      };
      save: () => Promise<void>;
    };
    page.form.controls.firstName.setValue('A');
    page.form.controls.city.setValue('');
    await page.save();
    fixture.detectChanges();
    const lines = [...compiled.querySelectorAll('.error-banner p')].map((node) => node.textContent.trim());
    expect(lines).toContain('First name is too short (minimum 2 characters).');
    expect(lines).toContain('City is not filled in.');
    expect(lines.length).toBe(2);
    http.verify();
  });

  it('shows a green success banner after saving changes', async () => {
    const fixture = TestBed.createComponent(ProfilePage);
    const http = TestBed.inject(HttpTestingController);
    const profile = {
      id: '11111111-1111-1111-1111-111111111111',
      email: 'ada@example.test',
      username: 'ada',
      firstName: 'Ada',
      middleInitial: null,
      lastName: 'Lovelace',
      suffix: null,
      city: 'Halifax',
      region: 'Nova Scotia',
      country: 'Canada',
      displayNameMode: 'Username',
      timeZoneId: 'America/Halifax',
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
    http.expectOne('/api/profiles/me').flush(profile);
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as { save: () => Promise<void> };
    const pending = page.save();
    const request = http.expectOne('/api/profiles/me');
    expect(request.request.method).toBe('PUT');
    expect((request.request.body as { preferredChatLanguage: string }).preferredChatLanguage).toBe('English');
    expect((request.request.body as { dateTimeDisplayFormat: string }).dateTimeDisplayFormat).toBe('MonthDayYear12h');
    request.flush({ ...profile, profileRevision: 2, updatedUtc: '2026-08-14T00:00:00+00:00' });
    await pending;
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const banner = compiled.querySelector('.success-banner');
    expect(banner).toBeTruthy();
    expect(banner?.classList.contains('error-banner')).toBe(false);
    expect(banner?.textContent).toContain(FORM_SAVE_SUCCESS_MESSAGE);
    http.verify();
  });
});
