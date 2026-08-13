import { Component, computed, inject, signal, viewChild, type ElementRef } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { AuthService, readApiErrorMessages, readApiFieldErrors } from '../../core/auth/auth.service';
import {
  collectFormFailures,
  isControlInvalid,
  maxLength,
  minLength,
  optionalMiddleInitial,
  required,
  scrollAlertIntoView,
} from '../../core/forms/validators';
import { NAME_SUFFIXES, PROFILE_FIELD_LABELS } from '../../core/identity/identity-fields';
import { listCountries, listTimeZones, regionsForCountry } from '../../core/location/location';
import { FilterableComboboxComponent } from '../../shared/filterable-combobox/filterable-combobox.component';

const COMPLETE_FIELD_LABELS: Readonly<Record<string, string>> = {
  username: PROFILE_FIELD_LABELS['username'] ?? 'Username',
  firstName: PROFILE_FIELD_LABELS['firstName'] ?? 'First name',
  middleInitial: PROFILE_FIELD_LABELS['middleInitial'] ?? 'Middle initial',
  lastName: PROFILE_FIELD_LABELS['lastName'] ?? 'Last name',
  suffix: PROFILE_FIELD_LABELS['suffix'] ?? 'Suffix',
  country: PROFILE_FIELD_LABELS['country'] ?? 'Country',
  region: PROFILE_FIELD_LABELS['region'] ?? 'State or province',
  city: PROFILE_FIELD_LABELS['city'] ?? 'City',
  timeZoneId: PROFILE_FIELD_LABELS['timeZoneId'] ?? 'Time zone',
};

@Component({
  selector: 'app-complete-external-page',
  imports: [ReactiveFormsModule, RouterLink, FilterableComboboxComponent],
  templateUrl: './complete-external.page.html',
  styleUrl: './complete-external.page.css',
})
export class CompleteExternalPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(FormBuilder);
  private readonly formAlert = viewChild<ElementRef<HTMLElement>>('formAlert');

  protected readonly loading = signal(true);
  protected readonly submitting = signal(false);
  protected readonly errorMessages = signal<string[]>([]);
  protected readonly serverFields = signal<ReadonlySet<string>>(new Set());
  protected readonly provider = signal<string | null>(null);
  protected readonly countries = listCountries();
  protected readonly timeZones = listTimeZones();
  protected readonly suffixes = NAME_SUFFIXES;
  protected readonly form = this.formBuilder.nonNullable.group({
    username: ['', [required, minLength(3), maxLength(32)]],
    firstName: ['', [required, minLength(2), maxLength(50)]],
    middleInitial: ['', optionalMiddleInitial],
    lastName: ['', [required, minLength(2), maxLength(50)]],
    suffix: [''],
    city: ['', required],
    region: ['', required],
    country: ['', required],
    timeZoneId: ['', required],
    displayNameMode: this.formBuilder.nonNullable.control<'Username' | 'FullName'>('Username'),
  });
  protected readonly countryValue = toSignal(this.form.controls.country.valueChanges, {
    initialValue: this.form.controls.country.value,
  });
  protected readonly regionOptions = computed(() => regionsForCountry(this.countryValue()));

  constructor() {
    void this.loadPending();
  }

  protected isInvalid(name: string): boolean {
    return isControlInvalid(this.form, name, this.serverFields());
  }

  private async loadPending(): Promise<void> {
    try {
      const pending = await this.auth.getPendingExternalProfile();
      this.provider.set(pending.provider);
      this.form.patchValue({
        firstName: pending.firstName ?? '',
        lastName: pending.lastName ?? '',
      });
    } catch (error: unknown) {
      this.revealErrors(readApiErrorMessages(error, 'Finish signing in with the external provider first.'));
    } finally {
      this.loading.set(false);
    }
  }

  protected async submit(): Promise<void> {
    this.form.markAllAsTouched();
    this.serverFields.set(new Set());
    const failures = collectFormFailures(this.form, COMPLETE_FIELD_LABELS);
    if (failures.length > 0) {
      this.revealErrors(failures);
      return;
    }

    this.submitting.set(true);
    this.errorMessages.set([]);
    try {
      await this.auth.completeExternalRegistration(this.form.getRawValue());
      await this.router.navigateByUrl('/');
    } catch (error: unknown) {
      this.serverFields.set(new Set(readApiFieldErrors(error)));
      this.revealErrors(readApiErrorMessages(error, 'Unable to finish creating the account.'));
    } finally {
      this.submitting.set(false);
    }
  }

  private revealErrors(messages: readonly string[]): void {
    this.errorMessages.set([...messages]);
    queueMicrotask(() => scrollAlertIntoView(this.formAlert()?.nativeElement));
  }
}
