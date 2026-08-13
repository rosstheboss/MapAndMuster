import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { ProfilePage } from './profile.page';

describe('ProfilePage', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProfilePage],
      providers: [provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting()],
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
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('#currentPassword')).toBeTruthy();
    expect(compiled.querySelector('#newPassword')).toBeTruthy();
    expect(compiled.querySelector('#confirmPassword')).toBeTruthy();
    expect(compiled.querySelector('#suffix')).toBeTruthy();

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
});
