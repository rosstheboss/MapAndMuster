import { Injectable, signal } from '@angular/core';

export type ColorTheme = 'light' | 'dark';

export const THEME_COOKIE_NAME = 'theme';
const THEME_MAX_AGE_SECONDS = 60 * 60 * 24 * 365;

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly dark = signal(false);

  readonly isDark = this.dark.asReadonly();

  constructor() {
    this.apply(readStoredTheme() === 'dark');
  }

  toggle(): void {
    this.set(this.dark() ? 'light' : 'dark');
  }

  set(theme: ColorTheme): void {
    this.apply(theme === 'dark');
    writeStoredTheme(theme);
  }

  private apply(isDark: boolean): void {
    this.dark.set(isDark);
    document.documentElement.dataset['theme'] = isDark ? 'dark' : 'light';
  }
}

export function readStoredTheme(): ColorTheme {
  const match = /(?:^|; )theme=(dark|light)/.exec(document.cookie);
  return match?.[1] === 'dark' ? 'dark' : 'light';
}

export function writeStoredTheme(theme: ColorTheme): void {
  document.cookie = `${THEME_COOKIE_NAME}=${theme}; Path=/; Max-Age=${THEME_MAX_AGE_SECONDS}; SameSite=Lax`;
}
