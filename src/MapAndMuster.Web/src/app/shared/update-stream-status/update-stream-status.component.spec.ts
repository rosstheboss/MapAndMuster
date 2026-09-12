import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { UpdateStreamStatusComponent } from './update-stream-status.component';

describe('UpdateStreamStatusComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UpdateStreamStatusComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
  });

  it('announces Live when the stream is open', () => {
    const fixture = TestBed.createComponent(UpdateStreamStatusComponent);
    fixture.componentRef.setInput('state', 'open');
    fixture.detectChanges();

    const chip = (fixture.nativeElement as HTMLElement).querySelector('.update-stream-status');
    expect(chip).toBeTruthy();
    expect(chip!.textContent).toContain('Live');
    expect(chip!.classList.contains('is-live')).toBe(true);
    expect(chip!.getAttribute('aria-label')).toContain('Updates arrive as they happen');
  });

  it('announces Reconnecting when the stream has dropped', () => {
    const fixture = TestBed.createComponent(UpdateStreamStatusComponent);
    fixture.componentRef.setInput('state', 'disconnected');
    fixture.detectChanges();

    const chip = (fixture.nativeElement as HTMLElement).querySelector('.update-stream-status');
    expect(chip).toBeTruthy();
    expect(chip!.textContent).toContain('Reconnecting');
    expect(chip!.classList.contains('is-reconnect')).toBe(true);
    expect(chip!.getAttribute('title')).toContain('every minute');
  });
});
