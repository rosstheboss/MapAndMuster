export const COOKIE_CONSENT_NAME = 'cookie_consent';
export const COOKIE_CONSENT_VERSION = 1;
const CONSENT_MAX_AGE_SECONDS = 60 * 60 * 24 * 180;

export interface CookieConsent {
  version: number;
  preferences: boolean;
}

export function readCookieConsent(): CookieConsent | null {
  const match = /(?:^|; )cookie_consent=([^;]*)/.exec(document.cookie);
  if (!match?.[1]) {
    return null;
  }

  try {
    const parsed = JSON.parse(decodeURIComponent(match[1])) as Partial<CookieConsent>;
    if (parsed.version !== COOKIE_CONSENT_VERSION || typeof parsed.preferences !== 'boolean') {
      return null;
    }

    return { version: COOKIE_CONSENT_VERSION, preferences: parsed.preferences };
  } catch {
    return null;
  }
}

export function preferencesAllowed(): boolean {
  return readCookieConsent()?.preferences === true;
}

export function writeCookieConsent(consent: CookieConsent): void {
  document.cookie = `${COOKIE_CONSENT_NAME}=${encodeURIComponent(JSON.stringify(consent))}; Path=/; Max-Age=${CONSENT_MAX_AGE_SECONDS}; SameSite=Lax`;
}

export function clearPreferenceCookies(): void {
  for (const part of document.cookie.split(';')) {
    const name = part.trim().split('=')[0];
    if (!name || name === COOKIE_CONSENT_NAME || name === 'mapandmuster.auth' || name === 'campaign.external') {
      continue;
    }

    document.cookie = `${name}=; Path=/; Max-Age=0; SameSite=Lax`;
  }

  try {
    const keys: string[] = [];
    for (let index = 0; index < localStorage.length; index += 1) {
      const key = localStorage.key(index);
      if (key?.startsWith('map-editor-color-mode:')) {
        keys.push(key);
      }
    }

    for (const key of keys) {
      localStorage.removeItem(key);
    }
  } catch {
    // Private mode should not block withdrawing consent.
  }
}
