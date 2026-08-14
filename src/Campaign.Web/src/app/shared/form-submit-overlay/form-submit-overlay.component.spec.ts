import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { FormSubmitOverlayService } from '../../core/forms/form-submit-overlay.service';
import { FormSubmitOverlayComponent } from './form-submit-overlay.component';

describe('FormSubmitOverlayComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FormSubmitOverlayComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
  });

  it('shows a modal while a form submit is in progress and hides it afterward', async () => {
    const overlay = TestBed.inject(FormSubmitOverlayService);
    const fixture = TestBed.createComponent(FormSubmitOverlayComponent);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.querySelector('[role="alertdialog"]')).toBeNull();

    let release: (() => void) | undefined;
    const pending = overlay.run(
      () =>
        new Promise<void>((resolve) => {
          release = () => resolve();
        }),
    );
    fixture.detectChanges();

    const dialog = compiled.querySelector('[role="alertdialog"]');
    expect(dialog).toBeTruthy();
    expect(dialog?.textContent).toContain('Saving');
    expect(dialog?.textContent).toContain('Please wait while your changes are saved.');

    release!();
    await pending;
    fixture.detectChanges();
    expect(compiled.querySelector('[role="alertdialog"]')).toBeNull();
  });
});
