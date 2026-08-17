import { Injectable } from '@angular/core';

import { CHAT_LANGUAGES, DEFAULT_CHAT_LANGUAGE, isChatLanguage, type ChatLanguage } from './chat-languages';

export const SITE_CHAT_COOKIE_NAME = 'siteChat';
const COOKIE_MAX_AGE_SECONDS = 60 * 60 * 24 * 365;

export interface SiteChatPrefs {
  composeLanguage: ChatLanguage;
  visibleLanguages: ChatLanguage[];
}

export function defaultSiteChatPrefs(preferredLanguage?: string | null): SiteChatPrefs {
  return {
    composeLanguage: isChatLanguage(preferredLanguage) ? preferredLanguage : DEFAULT_CHAT_LANGUAGE,
    visibleLanguages: [...CHAT_LANGUAGES],
  };
}

@Injectable({ providedIn: 'root' })
export class SiteChatPrefsService {
  read(preferredLanguage?: string | null): SiteChatPrefs {
    return readStoredSiteChatPrefs(preferredLanguage) ?? defaultSiteChatPrefs(preferredLanguage);
  }

  write(prefs: SiteChatPrefs): void {
    writeStoredSiteChatPrefs(prefs);
  }
}

export function readStoredSiteChatPrefs(preferredLanguage?: string | null): SiteChatPrefs | null {
  const match = /(?:^|; )siteChat=([^;]*)/.exec(document.cookie);
  const defaults = defaultSiteChatPrefs(preferredLanguage);
  if (!match?.[1]) {
    return null;
  }

  try {
    const parsed = JSON.parse(decodeURIComponent(match[1])) as Partial<SiteChatPrefs>;
    const visible = Array.isArray(parsed.visibleLanguages)
      ? parsed.visibleLanguages.filter(isChatLanguage)
      : defaults.visibleLanguages;
    return {
      composeLanguage: isChatLanguage(parsed.composeLanguage) ? parsed.composeLanguage : defaults.composeLanguage,
      visibleLanguages: visible.length > 0 ? visible : [...CHAT_LANGUAGES],
    };
  } catch {
    return null;
  }
}

export function writeStoredSiteChatPrefs(prefs: SiteChatPrefs): void {
  document.cookie = `${SITE_CHAT_COOKIE_NAME}=${encodeURIComponent(JSON.stringify(prefs))}; Path=/; Max-Age=${COOKIE_MAX_AGE_SECONDS}; SameSite=Lax`;
}
