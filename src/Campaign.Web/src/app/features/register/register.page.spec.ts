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
    expect(compiled.querySelector('#region')).toBeTruthy();
    http.verify();
  });
});
