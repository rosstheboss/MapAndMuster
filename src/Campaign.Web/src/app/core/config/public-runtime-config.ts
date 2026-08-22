import { InjectionToken } from '@angular/core';

export interface PublicRuntimeConfig {
  apiBaseUrl: string;
}

export const PUBLIC_RUNTIME_CONFIG = new InjectionToken<PublicRuntimeConfig>('PUBLIC_RUNTIME_CONFIG', {
  factory: () => ({ apiBaseUrl: '' }),
});

export function apiUrl(path: string, apiBaseUrl = ''): string {
  const normalizedPath = path.startsWith('/') ? path : `/${path}`;
  const base = apiBaseUrl.trim().replace(/\/$/, '');
  return `${base}${normalizedPath}`;
}

export function normalizePublicRuntimeConfig(body: unknown): PublicRuntimeConfig {
  if (typeof body !== 'object' || body === null || !('apiBaseUrl' in body)) {
    return { apiBaseUrl: '' };
  }

  const apiBaseUrl = body.apiBaseUrl;
  if (typeof apiBaseUrl !== 'string') {
    return { apiBaseUrl: '' };
  }

  const trimmed = apiBaseUrl.trim().replace(/\/$/, '');
  if (trimmed.length === 0) {
    return { apiBaseUrl: '' };
  }

  if (!/^https?:\/\//i.test(trimmed)) {
    return { apiBaseUrl: '' };
  }

  return { apiBaseUrl: trimmed };
}

export async function loadPublicRuntimeConfig(fetcher: typeof fetch = fetch): Promise<PublicRuntimeConfig> {
  try {
    const response = await fetcher('/config.json', { cache: 'no-store' });
    if (!response.ok) {
      return { apiBaseUrl: '' };
    }

    return normalizePublicRuntimeConfig(await response.json());
  } catch {
    return { apiBaseUrl: '' };
  }
}
