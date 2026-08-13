import { Validators, type ValidatorFn } from '@angular/forms';

export const required: ValidatorFn = (control) => Validators.required(control);

export const emailAddress: ValidatorFn = (control) => Validators.email(control);

export function minLength(length: number): ValidatorFn {
  return (control) => Validators.minLength(length)(control);
}

export function maxLength(length: number): ValidatorFn {
  return (control) => Validators.maxLength(length)(control);
}
