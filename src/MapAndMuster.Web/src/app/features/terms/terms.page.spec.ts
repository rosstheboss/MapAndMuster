import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { TermsPage } from './terms.page';

describe('TermsPage', () => {
  it('explains how the site may be used', async () => {
    await TestBed.configureTestingModule({
      imports: [TermsPage],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();

    const fixture = TestBed.createComponent(TermsPage);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Terms of Service');
    expect(compiled.textContent).toContain('campaign');
    expect(compiled.textContent).toContain('Google');
  });
});
