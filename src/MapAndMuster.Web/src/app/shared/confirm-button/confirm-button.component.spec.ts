import { Component } from '@angular/core';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { ConfirmButtonComponent } from './confirm-button.component';

@Component({
  imports: [ConfirmButtonComponent],
  template: `
    <app-confirm-button
      [label]="label"
      [confirmLabel]="confirmLabel"
      [appearance]="appearance"
      [confirmAppearance]="confirmAppearance"
      (confirmed)="confirmed = true"
    />
  `,
})
class HostComponent {
  label = 'Leave';
  confirmLabel = 'Confirm leave';
  appearance = 'button-secondary';
  confirmAppearance = 'button-danger';
  confirmed = false;
}

describe('ConfirmButtonComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
  });

  it('does not emit until the second click', () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    const button = compiled.querySelector('button')!;

    expect(button.textContent.trim()).toBe('Leave');
    expect(button.className).toContain('button-secondary');
    button.click();
    fixture.detectChanges();
    expect(fixture.componentInstance.confirmed).toBe(false);
    expect(button.textContent.trim()).toBe('Confirm leave');
    expect(button.className).toContain('button-danger');
    expect(button.getAttribute('aria-pressed')).toBe('true');

    button.click();
    fixture.detectChanges();
    expect(fixture.componentInstance.confirmed).toBe(true);
    expect(button.textContent.trim()).toBe('Leave');
  });
});
