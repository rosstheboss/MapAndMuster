import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { ExternalLoginButtonsComponent } from './external-login-buttons.component';

describe('ExternalLoginButtonsComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ExternalLoginButtonsComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
  });

  it('renders branded Continue with Google', () => {
    const fixture = TestBed.createComponent(ExternalLoginButtonsComponent);
    fixture.componentRef.setInput('providers', [{ name: 'Google', displayName: 'Google' }]);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const button = compiled.querySelector('button.google');
    expect(button?.textContent).toContain('Continue with Google');
    expect(button?.querySelector('svg')).toBeTruthy();
  });

  it('emits the provider name', () => {
    const fixture = TestBed.createComponent(ExternalLoginButtonsComponent);
    fixture.componentRef.setInput('providers', [{ name: 'Discord', displayName: 'Discord' }]);
    fixture.detectChanges();

    const selected: string[] = [];
    fixture.componentInstance.selected.subscribe((name) => selected.push(name));
    compiledButton(fixture.nativeElement as HTMLElement).click();
    expect(selected).toEqual(['Discord']);
  });
});

function compiledButton(root: HTMLElement): HTMLButtonElement {
  const button = root.querySelector('button');
  if (!button) {
    throw new Error('Expected a provider button.');
  }

  return button;
}
