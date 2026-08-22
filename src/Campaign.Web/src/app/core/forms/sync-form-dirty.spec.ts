import { FormBuilder } from '@angular/forms';

import { syncDirtyFromBaseline, valuesEqual } from './sync-form-dirty';

describe('syncDirtyFromBaseline', () => {
  const formBuilder = new FormBuilder();

  it('marks reverted fields pristine and changed fields dirty', () => {
    const form = formBuilder.nonNullable.group({
      name: ['Border War'],
      links: formBuilder.array([formBuilder.nonNullable.group({ label: ['Notes'], url: ['https://example.test'] })]),
    });
    const baseline = structuredClone(form.getRawValue());

    form.controls.name.setValue('Frontier War');
    form.controls.links.at(0).controls.label.setValue('Guide');
    syncDirtyFromBaseline(form, baseline);
    expect(form.controls.name.dirty).toBe(true);
    expect(form.controls.links.at(0).controls.label.dirty).toBe(true);
    expect(form.controls.links.at(0).controls.url.dirty).toBe(false);
    expect(form.dirty).toBe(true);

    form.controls.name.setValue('Border War');
    form.controls.links.at(0).controls.label.setValue('Notes');
    syncDirtyFromBaseline(form, baseline);
    expect(form.controls.name.dirty).toBe(false);
    expect(form.dirty).toBe(false);
  });

  it('treats added array items as dirty', () => {
    const form = formBuilder.nonNullable.group({
      names: formBuilder.array([formBuilder.nonNullable.control('North')]),
    });
    const baseline = structuredClone(form.getRawValue());
    form.controls.names.push(formBuilder.nonNullable.control('South'));
    syncDirtyFromBaseline(form, baseline);
    expect(form.controls.names.dirty).toBe(true);
    expect(form.controls.names.at(1).dirty).toBe(true);
  });
});

describe('valuesEqual', () => {
  it('equates nullish values and serializable objects', () => {
    expect(valuesEqual(null, undefined)).toBe(true);
    expect(valuesEqual({ a: 1 }, { a: 1 })).toBe(true);
    expect(valuesEqual({ a: 1 }, { a: 2 })).toBe(false);
  });
});
