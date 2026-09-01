import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { MapLegendComponent } from './map-legend.component';

describe('MapLegendComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MapLegendComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
  });

  it('starts collapsed and lists the shared map marks', () => {
    const fixture = TestBed.createComponent(MapLegendComponent);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const legend = compiled.querySelector<HTMLDetailsElement>('.map-legend');
    expect(legend).toBeTruthy();
    expect(legend?.open).toBe(false);
    expect(compiled.textContent).toContain('Ownership tint');
    expect(compiled.textContent).toContain('Spawn location');
    expect(compiled.textContent).toContain('Force');
    expect(compiled.textContent).toContain('Your force');
    expect(compiled.textContent).toContain('Force in battle');
    expect(compiled.textContent).toContain('Structure');
    expect(compiled.textContent).toContain('Pillaged structure');
    expect(compiled.textContent).toContain('Item objective');

    legend?.querySelector('summary')?.click();
    fixture.detectChanges();
    expect(legend?.open).toBe(true);
  });
});
