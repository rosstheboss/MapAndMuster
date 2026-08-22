import { apiUrl, loadPublicRuntimeConfig, normalizePublicRuntimeConfig } from './public-runtime-config';

describe('public runtime config', () => {
  it('joins an API origin with a relative path', () => {
    expect(apiUrl('/api/auth/me', 'https://api.example.test/')).toBe('https://api.example.test/api/auth/me');
    expect(apiUrl('/api/auth/me', '')).toBe('/api/auth/me');
  });

  it('accepts only http(s) API origins', () => {
    expect(normalizePublicRuntimeConfig({ apiBaseUrl: 'https://api.example.test/' })).toEqual({
      apiBaseUrl: 'https://api.example.test',
    });
    expect(normalizePublicRuntimeConfig({ apiBaseUrl: 'javascript:alert(1)' })).toEqual({ apiBaseUrl: '' });
    expect(normalizePublicRuntimeConfig({ apiBaseUrl: '' })).toEqual({ apiBaseUrl: '' });
  });

  it('loads config.json and falls back to same-origin when fetch fails', async () => {
    const loaded = await loadPublicRuntimeConfig(() =>
      Promise.resolve(new Response(JSON.stringify({ apiBaseUrl: 'https://api.example.test' }), { status: 200 })),
    );
    expect(loaded).toEqual({ apiBaseUrl: 'https://api.example.test' });

    const missing = await loadPublicRuntimeConfig(() => Promise.resolve(new Response('', { status: 404 })));
    expect(missing).toEqual({ apiBaseUrl: '' });

    const failed = await loadPublicRuntimeConfig(() => Promise.reject(new Error('offline')));
    expect(failed).toEqual({ apiBaseUrl: '' });
  });
});
