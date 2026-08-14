import { Component, effect, inject, viewChild, type ElementRef } from '@angular/core';

import { FormSubmitOverlayService } from '../../core/forms/form-submit-overlay.service';

@Component({
  selector: 'app-form-submit-overlay',
  templateUrl: './form-submit-overlay.component.html',
  styleUrl: './form-submit-overlay.component.css',
})
export class FormSubmitOverlayComponent {
  protected readonly overlay = inject(FormSubmitOverlayService);
  private readonly dialog = viewChild<ElementRef<HTMLElement>>('dialog');

  constructor() {
    effect(() => {
      if (!this.overlay.busy()) {
        return;
      }

      queueMicrotask(() => this.dialog()?.nativeElement.focus());
    });
  }
}
