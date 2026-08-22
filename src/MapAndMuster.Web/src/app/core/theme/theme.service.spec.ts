import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';

import { THEME_COOKIE_NAME, ThemeService, writeStoredTheme } from './theme.service';

describe('ThemeService', () => {
  beforeEach(() => {
    clearThemeCookie();
    document.documentElement.removeAttribute('data-theme');
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), ThemeService],
    });
  });

  afterEach(() => {
    clearThemeCookie();
    document.documentElement.removeAttribute('data-theme');
  });

  it('defaults to light mode when no cookie is stored', () => {
    const theme = TestBed.inject(ThemeService);
    expect(theme.isDark()).toBe(false);
    expect(document.documentElement.dataset['theme']).toBe('light');
  });

  it('restores dark mode from the theme cookie', () => {
    writeStoredTheme('dark');
    const theme = TestBed.inject(ThemeService);
    expect(theme.isDark()).toBe(true);
    expect(document.documentElement.dataset['theme']).toBe('dark');
  });

  it('toggles dark mode and persists it in a cookie', () => {
    const theme = TestBed.inject(ThemeService);
    theme.toggle();
    expect(theme.isDark()).toBe(true);
    expect(document.documentElement.dataset['theme']).toBe('dark');
    expect(document.cookie).toContain(`${THEME_COOKIE_NAME}=dark`);

    theme.toggle();
    expect(theme.isDark()).toBe(false);
    expect(document.documentElement.dataset['theme']).toBe('light');
    expect(document.cookie).toContain(`${THEME_COOKIE_NAME}=light`);
  });
});

function clearThemeCookie(): void {
  document.cookie = `${THEME_COOKIE_NAME}=; Path=/; Max-Age=0; SameSite=Lax`;
}
