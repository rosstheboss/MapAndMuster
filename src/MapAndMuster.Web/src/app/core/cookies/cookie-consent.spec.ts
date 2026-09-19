import { afterEach, describe, expect, it } from 'vitest';

import { clearPreferenceCookies, preferencesAllowed, readCookieConsent, writeCookieConsent } from './cookie-consent';

describe('cookie consent', () => {
  beforeEach(() => {
    document.cookie = 'cookie_consent=; Path=/; Max-Age=0; SameSite=Lax';
    document.cookie = 'theme=; Path=/; Max-Age=0; SameSite=Lax';
  });

  afterEach(() => {
    document.cookie = 'cookie_consent=; Path=/; Max-Age=0; SameSite=Lax';
    document.cookie = 'theme=; Path=/; Max-Age=0; SameSite=Lax';
  });

  it('treats missing or invalid cookies as no consent', () => {
    expect(readCookieConsent()).toBeNull();
    expect(preferencesAllowed()).toBe(false);
    document.cookie = 'cookie_consent=not-json; Path=/; SameSite=Lax';
    expect(readCookieConsent()).toBeNull();
  });

  it('stores a versioned preference choice', () => {
    writeCookieConsent({ version: 1, preferences: true });
    expect(readCookieConsent()).toEqual({ version: 1, preferences: true });
    expect(preferencesAllowed()).toBe(true);
  });

  it('clears preference cookies but keeps consent and auth cookies', () => {
    writeCookieConsent({ version: 1, preferences: false });
    document.cookie = 'theme=dark; Path=/; SameSite=Lax';
    document.cookie = 'mapandmuster.auth=session; Path=/; SameSite=Lax';
    clearPreferenceCookies();
    expect(document.cookie).not.toContain('theme=dark');
    expect(document.cookie).toContain('cookie_consent=');
    expect(document.cookie).toContain('mapandmuster.auth=session');
  });
});
