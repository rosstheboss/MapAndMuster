import { Component, computed, inject, signal, viewChild, type ElementRef } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import type { ExternalProvider } from '../../core/auth/auth.models';
import { AuthService, readApiErrorMessages, readApiFieldErrors } from '../../core/auth/auth.service';
import {
  collectFormFailures,
  emailAddress,
  isControlInvalid,
  matchingPasswords,
  maxLength,
  minLength,
  optionalMiddleInitial,
  passwordComplexity,
  required,
  scrollAlertIntoView,
} from '../../core/forms/validators';
import { NAME_SUFFIXES, REGISTER_FIELD_LABELS } from '../../core/identity/identity-fields';
import { listCountries, listTimeZones, regionsForCountry } from '../../core/location/location';
import { FilterableComboboxComponent } from '../../shared/filterable-combobox/filterable-combobox.component';

@Component({
  selector: 'app-register-page',
  imports: [ReactiveFormsModule, RouterLink, FilterableComboboxComponent],
  templateUrl: './register.page.html',
  styleUrl: './register.page.css',
})
export class RegisterPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(FormBuilder);
  private readonly formAlert = viewChild<ElementRef<HTMLElement>>('formAlert');

  protected readonly submitting = signal(false);
  protected readonly errorMessages = signal<string[]>([]);
  protected readonly serverFields = signal<ReadonlySet<string>>(new Set());
  protected readonly providers = signal<ExternalProvider[]>([]);
  protected readonly countries = listCountries();
  protected readonly timeZones = listTimeZones();
  protected readonly suffixes = NAME_SUFFIXES;
  protected readonly form = this.formBuilder.nonNullable.group(
    {
      email: ['', [required, emailAddress]],
      username: ['', [required, minLength(3), maxLength(32)]],
      password: ['', [required, passwordComplexity]],
      confirmPassword: ['', required],
      firstName: ['', [required, minLength(2), maxLength(50)]],
      middleInitial: ['', optionalMiddleInitial],
      lastName: ['', [required, minLength(2), maxLength(50)]],
      suffix: [''],
      city: ['', required],
      region: ['', required],
      country: ['', required],
      timeZoneId: ['', required],
      displayNameMode: this.formBuilder.nonNullable.control<'Username' | 'FullName'>('Username'),
    },
    { validators: matchingPasswords },
  );
  protected readonly countryValue = toSignal(this.form.controls.country.valueChanges, {
    initialValue: this.form.controls.country.value,
  });
  protected readonly regionOptions = computed(() => regionsForCountry(this.countryValue()));
  protected avatar: File | null = null;

  constructor() {
    void this.auth.getExternalProviders().then((providers) => this.providers.set(providers));
  }

  protected isInvalid(name: string): boolean {
    return isControlInvalid(this.form, name, this.serverFields());
  }

  protected onAvatarSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.avatar = input.files?.[0] ?? null;
  }

  protected startExternal(provider: string): void {
    this.auth.startExternalLogin(provider);
  }

  protected async submit(): Promise<void> {
    this.form.markAllAsTouched();
    this.serverFields.set(new Set());
    const failures = collectFormFailures(this.form, REGISTER_FIELD_LABELS);
    if (failures.length > 0) {
      this.revealErrors(failures);
      return;
    }

    this.submitting.set(true);
    this.errorMessages.set([]);
    const value = this.form.getRawValue();
    try {
      await this.auth.register({
        email: value.email,
        username: value.username,
        password: value.password,
        firstName: value.firstName,
        middleInitial: value.middleInitial,
        lastName: value.lastName,
        suffix: value.suffix,
        city: value.city,
        region: value.region,
        country: value.country,
        timeZoneId: value.timeZoneId,
        displayNameMode: value.displayNameMode,
        avatar: this.avatar,
      });
      await this.router.navigateByUrl('/login', { state: { registered: true } });
    } catch (error: unknown) {
      this.serverFields.set(new Set(readApiFieldErrors(error)));
      this.revealErrors(readApiErrorMessages(error, 'Unable to create the account.'));
    } finally {
      this.submitting.set(false);
    }
  }

  private revealErrors(messages: readonly string[]): void {
    this.errorMessages.set([...messages]);
    queueMicrotask(() => scrollAlertIntoView(this.formAlert()?.nativeElement));
  }
}
