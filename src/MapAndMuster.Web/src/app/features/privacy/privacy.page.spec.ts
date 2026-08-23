import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { PrivacyPage } from './privacy.page';

describe('PrivacyPage', () => {
  it('explains what the site stores', async () => {
    await TestBed.configureTestingModule({
      imports: [PrivacyPage],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();

    const fixture = TestBed.createComponent(PrivacyPage);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Privacy');
    expect(compiled.textContent).toContain('email');
    expect(compiled.textContent).toContain('Google');
  });
});
