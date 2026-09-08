import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { TraitorMarkComponent } from './traitor-mark.component';

describe('TraitorMarkComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TraitorMarkComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
  });

  it('names the betrayed player, faction, and subfaction on hover', () => {
    const fixture = TestBed.createComponent(TraitorMarkComponent);
    fixture.componentRef.setInput('victims', [
      {
        displayName: 'Jean',
        factionName: 'Kingdom of Bretonnia',
        subfaction: 'Errantry Crusade',
      },
    ]);
    fixture.detectChanges();

    const mark = fixture.nativeElement as HTMLElement;
    expect(mark.querySelector('.traitor-mark')?.getAttribute('aria-label')).toBe(
      'Betrayed Jean · Kingdom of Bretonnia (Errantry Crusade)',
    );
  });
});
