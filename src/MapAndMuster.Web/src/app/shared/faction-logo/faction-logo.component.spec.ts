import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { FactionLogoComponent } from './faction-logo.component';

const png =
  'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==';

describe('FactionLogoComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FactionLogoComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
  });

  it('shows the original image when tinting is off', () => {
    const fixture = TestBed.createComponent(FactionLogoComponent);
    fixture.componentRef.setInput('src', png);
    fixture.componentRef.setInput('color', '#DC2626');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('img')?.getAttribute('src')).toBe(png);
    expect(compiled.querySelector('.is-tinted')).toBeNull();
  });

  it('tints the logo with the faction color when enabled', () => {
    const fixture = TestBed.createComponent(FactionLogoComponent);
    fixture.componentRef.setInput('src', png);
    fixture.componentRef.setInput('color', '#DC2626');
    fixture.componentRef.setInput('tint', true);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const tinted = compiled.querySelector<HTMLElement>('.is-tinted');
    expect(compiled.querySelector('img')).toBeNull();
    expect(tinted).toBeTruthy();
    expect(tinted?.style.background).toBe('rgb(220, 38, 38)');
  });
});
