import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component, signal } from '@angular/core';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { SaveCampaignPresetDialogComponent } from './save-campaign-preset-dialog.component';

@Component({
  imports: [SaveCampaignPresetDialogComponent],
  template: `
    <app-save-campaign-preset-dialog
      [open]="open()"
      [saving]="false"
      (confirmed)="name = $event"
      (closed)="open.set(false)"
    />
  `,
})
class HostComponent {
  readonly open = signal(false);
  name = '';
}

describe('SaveCampaignPresetDialogComponent', () => {
  it('lists The Hunt in Estalia even when no saved presets exist', async () => {
    await TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(HostComponent);
    fixture.componentInstance.open.set(true);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/campaign-presets').flush([]);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const input = compiled.querySelector<HTMLInputElement>('#savePresetName')!;
    input.dispatchEvent(new Event('focus'));
    input.value = 'hunt';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(compiled.querySelector('[role="dialog"]')?.getAttribute('aria-modal')).toBe('true');
    const options = [...compiled.querySelectorAll('[role="option"]')].map((item) => item.textContent.trim());
    expect(options).toEqual(['The Hunt in Estalia']);
    http.verify();
  });
});
