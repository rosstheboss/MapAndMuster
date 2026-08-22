import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import {
  provideBrowserGlobalErrorListeners,
  provideZonelessChangeDetection,
  type ApplicationConfig,
} from '@angular/core';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { apiBaseInterceptor } from './core/config/api-base.interceptor';
import { PUBLIC_RUNTIME_CONFIG, type PublicRuntimeConfig } from './core/config/public-runtime-config';

export function createAppConfig(runtimeConfig: PublicRuntimeConfig): ApplicationConfig {
  return {
    providers: [
      provideBrowserGlobalErrorListeners(),
      provideZonelessChangeDetection(),
      { provide: PUBLIC_RUNTIME_CONFIG, useValue: runtimeConfig },
      provideHttpClient(withFetch(), withInterceptors([apiBaseInterceptor])),
      provideRouter(routes),
    ],
  };
}

export const appConfig = createAppConfig({ apiBaseUrl: '' });
