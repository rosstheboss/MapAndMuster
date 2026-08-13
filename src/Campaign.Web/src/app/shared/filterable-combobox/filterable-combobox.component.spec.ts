import { Component, provideZonelessChangeDetection } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { TestBed } from '@angular/core/testing';

import { FilterableComboboxComponent } from './filterable-combobox.component';

@Component({
  imports: [ReactiveFormsModule, FilterableComboboxComponent],
  template: `
    <label for="country">Country</label>
    <app-filterable-combobox inputId="country" [formControl]="control" [options]="options" />
  `,
})
class HostComponent {
  readonly control = new FormControl('Canada', { nonNullable: true });
  readonly options = ['Canada', 'United States', 'Australia'];
}

describe('FilterableComboboxComponent', () => {
  it('filters options as the user types', async () => {
    await TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();

    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    const input = compiled.querySelector<HTMLInputElement>('#country')!;
    input.dispatchEvent(new Event('focus'));
    input.value = 'uni';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const options = [...compiled.querySelectorAll('[role="option"]')].map((item) => item.textContent.trim());
    expect(options).toEqual(['United States']);
    expect(fixture.componentInstance.control.value).toBe('uni');
  });
});
