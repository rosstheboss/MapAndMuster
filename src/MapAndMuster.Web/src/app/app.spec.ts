import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideZonelessChangeDetection(), provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render the application brand', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    const banner = compiled.querySelector('.app-banner-image');
    expect(banner?.getAttribute('src')).toBe('/brand/banner.png');
    expect(banner?.getAttribute('alt')).toBe('Map & Muster');
    expect(compiled.querySelector('.app-brand')?.getAttribute('href')).toBe('/');
    const footerLogo = compiled.querySelector('.app-footer-logo');
    expect(footerLogo?.getAttribute('src')).toBe('/brand/micrologo.jpg');
    expect(footerLogo?.getAttribute('alt')).toBe('Map & Muster');
    expect(compiled.querySelector('.skip-link')?.textContent).toContain('Skip to content');
    const themeToggle = compiled.querySelector('app-theme-toggle button');
    expect(themeToggle?.getAttribute('aria-label')).toBe('Switch to dark mode');
    expect(themeToggle?.textContent).toContain('Light mode');
    expect(themeToggle?.querySelector('svg circle')).toBeTruthy();
  });

  it('renders the main navigation under the banner', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    const nav = compiled.querySelector('nav[aria-label="Main"]');
    expect(nav?.textContent).toContain('Home');
    expect(nav?.textContent).toContain('Your Campaigns');
    expect(nav?.textContent).toContain('All Campaigns');
    expect(nav?.textContent).toContain('Profile');
    expect(nav?.textContent).toContain('Sign in');
    expect(nav?.querySelector('app-theme-toggle')).toBeTruthy();
    const footerLinks = [...compiled.querySelectorAll('.app-footer-links a')].map((node) => node.textContent.trim());
    expect(footerLinks).toContain('Privacy');
    expect(footerLinks).toContain('Terms');
  });
});
