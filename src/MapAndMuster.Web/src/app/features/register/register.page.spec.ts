import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { RegisterPage } from './register.page';

describe('RegisterPage', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RegisterPage],
      providers: [provideZonelessChangeDetection(), provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  it('renders required signup fields', async () => {
    const fixture = TestBed.createComponent(RegisterPage);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/auth/external-providers').flush([]);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Create an account');
    expect(compiled.querySelector('#username')).toBeTruthy();
    expect(compiled.querySelector('#firstName')).toBeTruthy();
    expect(compiled.querySelector('#country')).toBeTruthy();
    expect(compiled.querySelector('#timeZoneId')).toBeTruthy();
    expect(compiled.querySelector('#confirmPassword')).toBeTruthy();
    expect(compiled.querySelector('#suffix')).toBeTruthy();
    expect(compiled.querySelector('.required-marker')).toBeTruthy();
    expect(compiled.querySelector('.field-row-identity')).toBeTruthy();
    expect(compiled.querySelector('.field-row-location')).toBeTruthy();
    expect(compiled.textContent).toContain('Choose image');
    expect(compiled.querySelector('a[href="/terms"]')?.textContent).toContain('Terms of Service');
    expect(compiled.querySelector('a[href="/privacy"]')?.textContent).toContain('Privacy');

    const form = fixture.componentInstance as unknown as { submit: () => Promise<void> };
    await form.submit();
    fixture.detectChanges();
    const lines = [...compiled.querySelectorAll('.error-banner p')].map((node) => node.textContent.trim());
    expect(lines).toContain('Email is not filled in.');
    expect(lines).toContain('Username is not filled in.');
    expect(lines).toContain('Password is not filled in.');
    expect(lines).toContain('First name is not filled in.');
    expect(lines.length).toBeGreaterThan(1);
    http.verify();
  });
});
