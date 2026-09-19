import { inject, Injectable, signal } from '@angular/core';

import { ThemeService } from '../theme/theme.service';
import {
  clearPreferenceCookies,
  preferencesAllowed,
  readCookieConsent,
  writeCookieConsent,
  type CookieConsent,
} from './cookie-consent';

@Injectable({ providedIn: 'root' })
export class CookieConsentService {
  private readonly theme = inject(ThemeService);
  private readonly consentState = signal<CookieConsent | null>(readCookieConsent());
  readonly consent = this.consentState.asReadonly();
  readonly needsDecision = signal(readCookieConsent() === null);
  readonly settingsOpen = signal(false);

  allowsPreferences(): boolean {
    return this.consentState()?.preferences === true;
  }

  acceptAll(): void {
    this.store({ version: 1, preferences: true });
    this.persistCurrentTheme();
  }

  rejectNonEssential(): void {
    this.store({ version: 1, preferences: false });
    clearPreferenceCookies();
    this.theme.set('light');
  }

  openSettings(): void {
    this.settingsOpen.set(true);
  }

  closeSettings(): void {
    this.settingsOpen.set(false);
  }

  saveSettings(preferences: boolean): void {
    this.store({ version: 1, preferences });
    if (!preferences) {
      clearPreferenceCookies();
      this.theme.set('light');
    } else {
      this.persistCurrentTheme();
    }

    this.settingsOpen.set(false);
  }

  private persistCurrentTheme(): void {
    this.theme.set(this.theme.isDark() ? 'dark' : 'light');
  }

  private store(consent: CookieConsent): void {
    writeCookieConsent(consent);
    this.consentState.set(consent);
    this.needsDecision.set(false);
  }
}

export { preferencesAllowed, readCookieConsent };
