import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { FormSubmitOverlayService } from './form-submit-overlay.service';

describe('FormSubmitOverlayService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection()],
    });
  });

  it('shows busy while work runs and clears after success or failure', async () => {
    const overlay = TestBed.inject(FormSubmitOverlayService);
    expect(overlay.busy()).toBe(false);

    let release: (() => void) | undefined;
    const pending = overlay.run(
      () =>
        new Promise<string>((resolve) => {
          release = () => resolve('ok');
        }),
    );
    expect(overlay.busy()).toBe(true);
    release!();
    await expect(pending).resolves.toBe('ok');
    expect(overlay.busy()).toBe(false);

    await expect(overlay.run(() => Promise.reject(new Error('save failed')))).rejects.toThrow('save failed');
    expect(overlay.busy()).toBe(false);
  });
});
