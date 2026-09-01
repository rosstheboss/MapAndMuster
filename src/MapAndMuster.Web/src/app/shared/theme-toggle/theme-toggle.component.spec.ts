import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { THEME_COOKIE_NAME, ThemeService } from '../../core/theme/theme.service';
import { ThemeToggleComponent } from './theme-toggle.component';

describe('ThemeToggleComponent', () => {
  beforeEach(async () => {
    clearThemeCookie();
    document.documentElement.removeAttribute('data-theme');
    await TestBed.configureTestingModule({
      imports: [ThemeToggleComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
  });

  afterEach(() => {
    clearThemeCookie();
    document.documentElement.removeAttribute('data-theme');
  });

  it('pairs the sun with light mode and the moon with dark mode', async () => {
    TestBed.inject(ThemeService).set('light');
    const fixture = TestBed.createComponent(ThemeToggleComponent);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const button = compiled.querySelector('button')!;
    expect(button.textContent).toContain('Switch to dark mode');
    expect(button.getAttribute('aria-label')).toBe('Switch to dark mode');
    expect(button.getAttribute('aria-pressed')).toBe('false');
    expect(button.querySelector('svg circle')).toBeTruthy();

    button.click();
    fixture.detectChanges();

    expect(TestBed.inject(ThemeService).isDark()).toBe(true);
    expect(button.textContent).toContain('Switch to light mode');
    expect(button.getAttribute('aria-label')).toBe('Switch to light mode');
    expect(button.getAttribute('aria-pressed')).toBe('true');
    expect(button.querySelector('svg circle')).toBeNull();
    expect(button.querySelector('svg path')).toBeTruthy();
  });
});

function clearThemeCookie(): void {
  document.cookie = `${THEME_COOKIE_NAME}=; Path=/; Max-Age=0; SameSite=Lax`;
}
