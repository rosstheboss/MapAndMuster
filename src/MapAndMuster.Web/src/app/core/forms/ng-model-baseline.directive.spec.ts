import { Component, provideZonelessChangeDetection } from '@angular/core';
import { TestBed, type ComponentFixture } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';

import { NgModelBaselineDirective } from './ng-model-baseline.directive';

@Component({
  imports: [FormsModule, NgModelBaselineDirective],
  template: `
    <input id="name" [ngModel]="name" (ngModelChange)="name = $event" />
    <input id="spawn" type="checkbox" [ngModel]="spawn" (ngModelChange)="spawn = $event" />
  `,
})
class BaselineHost {
  name = 'Northmarch';
  spawn = true;
}

describe('NgModelBaselineDirective', () => {
  let fixture: ComponentFixture<BaselineHost>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BaselineHost],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
    fixture = TestBed.createComponent(BaselineHost);
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('clears dirty when a text field or checkbox returns to its original value', async () => {
    const name = element('#name');
    await setText(name, 'Changed');
    expect(name.classList.contains('ng-dirty')).toBe(true);

    await setText(name, 'Northmarch');
    expect(name.classList.contains('ng-dirty')).toBe(false);

    const spawn = element('#spawn');
    spawn.click();
    fixture.detectChanges();
    await fixture.whenStable();
    expect(spawn.checked).toBe(false);
    expect(spawn.classList.contains('ng-dirty')).toBe(true);

    spawn.click();
    fixture.detectChanges();
    await fixture.whenStable();
    expect(spawn.checked).toBe(true);
    expect(spawn.classList.contains('ng-dirty')).toBe(false);
  });

  function element(selector: string): HTMLInputElement {
    const input = (fixture.nativeElement as HTMLElement).querySelector(selector);
    if (!(input instanceof HTMLInputElement)) {
      throw new Error(`Missing ${selector}`);
    }

    return input;
  }

  async function setText(input: HTMLInputElement, value: string): Promise<void> {
    input.value = value;
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    await fixture.whenStable();
  }
});
