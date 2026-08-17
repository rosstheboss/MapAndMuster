import { Validators, type AbstractControl, type FormGroup, type ValidatorFn } from '@angular/forms';

import { RESERVED_USERNAMES } from '../identity/identity-fields';

export const required: ValidatorFn = (control) => Validators.required(control);

export const emailAddress: ValidatorFn = (control) => Validators.email(control);

export function minLength(length: number): ValidatorFn {
  return (control) => Validators.minLength(length)(control);
}

export function maxLength(length: number): ValidatorFn {
  return (control) => Validators.maxLength(length)(control);
}

export const PASSWORD_MIN_LENGTH = 12;

export const passwordComplexity: ValidatorFn = (control) => {
  const value = String(control.value ?? '');
  if (value.length === 0) {
    return null;
  }

  const problems: string[] = [];
  if (value.length < PASSWORD_MIN_LENGTH) {
    problems.push(`at least ${PASSWORD_MIN_LENGTH} characters`);
  }

  if (!/[A-Z]/.test(value)) {
    problems.push('an uppercase letter');
  }

  if (!/[a-z]/.test(value)) {
    problems.push('a lowercase letter');
  }

  if (!/\d/.test(value)) {
    problems.push('a number');
  }

  if (!/[^A-Za-z0-9]/.test(value)) {
    problems.push('a special character');
  }

  return problems.length > 0 ? { passwordComplexity: { problems } } : null;
};

export const optionalMiddleInitial: ValidatorFn = (control) => {
  const value = String(control.value ?? '').trim();
  if (value.length === 0) {
    return null;
  }

  if (value.length !== 1 || !/^[A-Za-z]$/.test(value)) {
    return { middleInitial: true };
  }

  return null;
};

export const reservedUsername: ValidatorFn = (control) => {
  const value = String(control.value ?? '').trim();
  if (value.length === 0) {
    return null;
  }

  return RESERVED_USERNAMES.has(value.toLowerCase()) ? { reservedUsername: true } : null;
};

export function minValue(minimum: number): ValidatorFn {
  return (control) => Validators.min(minimum)(control);
}

export function maxValue(maximum: number): ValidatorFn {
  return (control) => Validators.max(maximum)(control);
}

export const httpUrl: ValidatorFn = (control) => {
  const value = String(control.value ?? '').trim();
  if (value.length === 0) {
    return null;
  }

  try {
    const parsed = new URL(value);
    if (parsed.protocol === 'http:' || parsed.protocol === 'https:') {
      return null;
    }
  } catch {
    return { httpUrl: true };
  }

  return { httpUrl: true };
};

export const matchingPasswords: ValidatorFn = (control) => {
  const password = control.get('password')?.value as string | undefined;
  const confirm = control.get('confirmPassword')?.value as string | undefined;
  if (!password || !confirm) {
    return null;
  }

  return password === confirm ? null : { passwordMismatch: true };
};

export function describeControlError(control: AbstractControl | null, label: string): string | null {
  if (!control?.errors) {
    return null;
  }

  if (control.hasError('required')) {
    return `${label} is not filled in.`;
  }

  if (control.hasError('email')) {
    return `${label} is invalid.`;
  }

  const min = control.getError('minlength') as { requiredLength?: number } | null;
  if (min?.requiredLength) {
    return `${label} is too short (minimum ${min.requiredLength} characters).`;
  }

  const max = control.getError('maxlength') as { requiredLength?: number } | null;
  if (max?.requiredLength) {
    return `${label} is too long (maximum ${max.requiredLength} characters).`;
  }

  const minBound = control.getError('min') as { min?: number } | null;
  if (minBound?.min !== undefined) {
    return `${label} must be at least ${minBound.min}.`;
  }

  const maxBound = control.getError('max') as { max?: number } | null;
  if (maxBound?.max !== undefined) {
    return `${label} must be at most ${maxBound.max}.`;
  }

  if (control.hasError('httpUrl')) {
    return `${label} must be an http or https address.`;
  }

  const complexity = control.getError('passwordComplexity') as { problems?: string[] } | null;
  if (complexity?.problems && complexity.problems.length > 0) {
    return `${label} must contain ${joinList(complexity.problems)}.`;
  }

  if (control.hasError('middleInitial')) {
    return `${label} must be a single alphabetical character.`;
  }

  if (control.hasError('reservedUsername')) {
    return 'That username is reserved.';
  }

  return `${label} is invalid.`;
}

export function collectFormFailures(form: FormGroup, labels: Readonly<Record<string, string>>): string[] {
  const failures: string[] = [];
  for (const [name, label] of Object.entries(labels)) {
    const message = describeControlError(form.get(name), label);
    if (message) {
      failures.push(message);
    }
  }

  if (form.hasError('passwordMismatch')) {
    failures.push('Confirm password does not match the password.');
  }

  return failures;
}

export function scrollAlertIntoView(element: HTMLElement | null | undefined): void {
  if (!element) {
    return;
  }

  element.scrollIntoView({ behavior: 'smooth', block: 'center', inline: 'nearest' });
  element.focus();
}

export function joinList(parts: readonly string[]): string {
  if (parts.length === 1) {
    return parts[0] ?? '';
  }

  if (parts.length === 2) {
    return `${parts[0]} and ${parts[1]}`;
  }

  return `${parts.slice(0, -1).join(', ')}, and ${parts[parts.length - 1]}`;
}

export function isControlInvalid(form: FormGroup, name: string, serverFields: ReadonlySet<string>): boolean {
  if (serverFields.has(name)) {
    return true;
  }

  const control = form.get(name);
  if (!control?.touched) {
    return false;
  }

  if (name === 'confirmPassword' && form.hasError('passwordMismatch')) {
    return true;
  }

  return control.invalid;
}
