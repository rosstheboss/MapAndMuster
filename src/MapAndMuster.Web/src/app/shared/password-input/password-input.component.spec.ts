import { Component, provideZonelessChangeDetection } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { TestBed } from '@angular/core/testing';

import { PasswordInputComponent } from './password-input.component';

@Component({
  imports: [ReactiveFormsModule, PasswordInputComponent],
  template: `
    <label for="secret">Password</label>
    <app-password-input>
      <input id="secret" type="password" [formControl]="control" autocomplete="current-password" />
    </app-password-input>
  `,
})
class HostComponent {
  readonly control = new FormControl('Correct-Horse-1', { nonNullable: true });
}

describe('PasswordInputComponent', () => {
  it('shows and hides the password without changing the value', async () => {
    await TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();

    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const input = compiled.querySelector<HTMLInputElement>('#secret')!;
    const toggle = compiled.querySelector<HTMLButtonElement>('.password-toggle')!;
    expect(input.type).toBe('password');
    expect(input.value).toBe('Correct-Horse-1');
    expect(toggle.getAttribute('aria-label')).toBe('Show password');
    expect(toggle.getAttribute('aria-pressed')).toBe('false');
    expect(toggle.getAttribute('aria-controls')).toBe('secret');
    expect(toggle.getAttribute('type')).toBe('button');

    toggle.click();
    fixture.detectChanges();

    expect(input.type).toBe('text');
    expect(input.value).toBe('Correct-Horse-1');
    expect(fixture.componentInstance.control.value).toBe('Correct-Horse-1');
    expect(toggle.getAttribute('aria-label')).toBe('Hide password');
    expect(toggle.getAttribute('aria-pressed')).toBe('true');

    toggle.click();
    fixture.detectChanges();

    expect(input.type).toBe('password');
    expect(toggle.getAttribute('aria-label')).toBe('Show password');
    expect(toggle.getAttribute('aria-pressed')).toBe('false');
  });
});
