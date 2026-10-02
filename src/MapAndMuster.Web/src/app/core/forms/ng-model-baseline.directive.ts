import { afterNextRender, DestroyRef, Directive, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NgModel } from '@angular/forms';

import { valuesEqual } from './sync-form-dirty';

/**
 * Angular marks an `ngModel` control dirty on the first edit and leaves it dirty after the value
 * returns. The dirty marker follows that flag, so capture the first rendered value and clear the
 * flag when the control comes back to it.
 */
@Directive({
  // Every template-driven control, so changing a value and changing it back clears dirty.
  // A prefixed attribute would have to be added to each control by hand.
  // eslint-disable-next-line @angular-eslint/directive-selector
  selector: '[ngModel]',
})
export class NgModelBaselineDirective {
  private readonly model = inject(NgModel);
  private readonly destroyRef = inject(DestroyRef);
  private baseline: unknown;
  private ready = false;

  constructor() {
    afterNextRender(() => {
      this.baseline = this.model.viewModel;
      this.ready = true;
    });

    this.model.update.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      if (!this.ready) {
        return;
      }

      if (valuesEqual(this.model.viewModel, this.baseline)) {
        this.model.control.markAsPristine();
      }
    });
  }
}
