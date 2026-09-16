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

  it('starts collapsed and lists shared map marks', () => {
    const fixture = TestBed.createComponent(MapLegendComponent);
    fixture.componentRef.setInput('showYourForce', true);
    fixture.componentRef.setInput('showForceInBattle', true);
    fixture.componentRef.setInput('showPlayGlows', true);
    fixture.componentRef.setInput('structures', [
      {
        id: 'town',
        name: 'Town',
        builtinSymbol: 'Town',
        hasImage: false,
        hasPillagedImage: false,
      },
    ]);
    fixture.componentRef.setInput('pillageStructure', {
      id: 'town',
      name: 'Town',
      builtinSymbol: 'Town',
      hasImage: false,
      hasPillagedImage: false,
    });
    fixture.componentRef.setInput('items', [
      { name: 'Crown', builtinSymbol: 'Crown', color: '#C45C26', imageUrl: null },
    ]);
    fixture.componentRef.setInput('factions', [
      { id: 'north', name: 'North', color: '#111111', image: null, tint: false },
    ]);
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
    expect(compiled.textContent).toContain('Town');
    expect(compiled.textContent).toContain('Pillaged Town');
    expect(compiled.textContent).toContain('Crown');
    expect(compiled.textContent).toContain('North');
    expect(compiled.textContent).toContain('Selected territory');
    expect(compiled.textContent).toContain('Hidden relic nearby');
    expect(compiled.textContent).toContain('Teleporting');

    legend?.querySelector('summary')?.click();
    fixture.detectChanges();
    expect(legend?.open).toBe(true);
  });

  it('omits unused force and structure marks', () => {
    const fixture = TestBed.createComponent(MapLegendComponent);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Force');
    expect(compiled.textContent).not.toContain('Your force');
    expect(compiled.textContent).not.toContain('Force in battle');
    expect(compiled.textContent).not.toContain('Structure');
    expect(compiled.textContent).not.toContain('Pillaged');
    expect(compiled.textContent).not.toContain('Item objective');
  });
});
