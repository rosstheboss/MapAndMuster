import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { apiBaseInterceptor } from './api-base.interceptor';
import { PUBLIC_RUNTIME_CONFIG } from './public-runtime-config';

describe('apiBaseInterceptor', () => {
  it('leaves same-origin API paths unchanged when apiBaseUrl is empty', () => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        { provide: PUBLIC_RUNTIME_CONFIG, useValue: { apiBaseUrl: '' } },
        provideHttpClient(withInterceptors([apiBaseInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    const http = TestBed.inject(HttpClient);
    const controller = TestBed.inject(HttpTestingController);
    http.get('/api/auth/me').subscribe();
    controller.expectOne('/api/auth/me').flush({});
    controller.verify();
  });

  it('prefixes relative API paths with the public API origin', () => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        { provide: PUBLIC_RUNTIME_CONFIG, useValue: { apiBaseUrl: 'https://api.example.test' } },
        provideHttpClient(withInterceptors([apiBaseInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    const http = TestBed.inject(HttpClient);
    const controller = TestBed.inject(HttpTestingController);
    http.get('/api/auth/me').subscribe();
    controller.expectOne('https://api.example.test/api/auth/me').flush({});
    controller.verify();
  });
});
