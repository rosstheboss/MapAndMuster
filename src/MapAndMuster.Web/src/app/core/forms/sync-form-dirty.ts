import { FormArray, FormControl, FormGroup, type AbstractControl } from '@angular/forms';

export function valuesEqual(left: unknown, right: unknown): boolean {
  if (Object.is(left, right)) {
    return true;
  }

  if ((left === null || left === undefined) && (right === null || right === undefined)) {
    return true;
  }

  if (typeof left !== typeof right) {
    return JSON.stringify(left ?? null) === JSON.stringify(right ?? null);
  }

  if (typeof left !== 'object') {
    return left === right;
  }

  return JSON.stringify(left) === JSON.stringify(right);
}

export function syncDirtyFromBaseline(control: AbstractControl, baseline: unknown): void {
  if (control instanceof FormControl) {
    if (valuesEqual(control.value, baseline)) {
      control.markAsPristine({ onlySelf: true });
    } else {
      control.markAsDirty({ onlySelf: true });
    }

    return;
  }

  if (control instanceof FormGroup) {
    const record = isRecord(baseline) ? baseline : {};
    for (const [key, child] of Object.entries(control.controls)) {
      syncDirtyFromBaseline(child, record[key]);
    }

    markComposite(control);
    return;
  }

  if (control instanceof FormArray) {
    const items = Array.isArray(baseline) ? baseline : [];
    control.controls.forEach((child, index) => {
      if (index >= items.length) {
        markSubtreeDirty(child);
        return;
      }

      syncDirtyFromBaseline(child, items[index]);
    });

    if (control.length !== items.length) {
      control.markAsDirty({ onlySelf: true });
      return;
    }

    markComposite(control);
  }
}

function markSubtreeDirty(control: AbstractControl): void {
  if (control instanceof FormControl) {
    control.markAsDirty({ onlySelf: true });
    return;
  }

  if (control instanceof FormGroup || control instanceof FormArray) {
    for (const child of Object.values(control.controls)) {
      markSubtreeDirty(child);
    }

    control.markAsDirty({ onlySelf: true });
  }
}

function markComposite(control: FormGroup | FormArray): void {
  const children = Object.values(control.controls);
  if (children.some((child) => child.dirty)) {
    control.markAsDirty({ onlySelf: true });
    return;
  }

  control.markAsPristine({ onlySelf: true });
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return !!value && typeof value === 'object' && !Array.isArray(value);
}
