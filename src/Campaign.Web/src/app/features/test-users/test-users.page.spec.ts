import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { TestUsersPage } from './test-users.page';

describe('TestUsersPage', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestUsersPage],
      providers: [provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();
  });

  it('lists seeded test users and can switch to one', async () => {
    const fixture = TestBed.createComponent(TestUsersPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/auth/test-users').flush([{ id: 't1', number: 1, username: 'test1', displayName: 'Test 1' }]);
    await Promise.resolve();
    await fixture.whenStable();
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Test 1');

    const button = [...compiled.querySelectorAll('button')].find((item) =>
      item.textContent.includes('Test as this user'),
    );
    expect(button).toBeTruthy();
    button!.click();
    fixture.detectChanges();
    const impersonate = http.expectOne('/api/auth/test-users/t1/impersonate');
    impersonate.flush({
      id: 't1',
      email: 'test1@users.invalid',
      username: 'test1',
      firstName: 'Test',
      middleInitial: null,
      lastName: 'Account',
      suffix: null,
      city: 'Testville',
      region: 'Testshire',
      country: 'Testland',
      displayNameMode: 'Username',
      timeZoneId: null,
      hasAvatar: false,
      createdUtc: '2026-08-17T00:00:00+00:00',
      updatedUtc: '2026-08-17T00:00:00+00:00',
      profileRevision: 1,
      emailConfirmed: true,
      isAdministrator: false,
      inAppNotificationsEnabled: true,
      emailNotificationsEnabled: false,
      preferredChatLanguage: 'English',
      isTestAccount: true,
      testAccountNumber: 1,
      isImpersonating: true,
    });
    http.verify();
  });
});
