import { Component, computed, inject, signal, viewChild, type ElementRef } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';

import { AuthService, readApiErrorMessages, readApiFieldErrors } from '../../core/auth/auth.service';
import { FORM_SAVE_SUCCESS_MESSAGE } from '../../core/forms/form-messages';
import { FormSubmitOverlayService } from '../../core/forms/form-submit-overlay.service';
import {
  collectFormFailures,
  describeControlError,
  isControlInvalid,
  maxLength,
  minLength,
  optionalMiddleInitial,
  passwordComplexity,
  required,
  scrollAlertIntoView,
} from '../../core/forms/validators';
import { NAME_SUFFIXES, PROFILE_FIELD_LABELS } from '../../core/identity/identity-fields';
import { listCountries, listTimeZones, regionsForCountry } from '../../core/location/location';
import { FilterableComboboxComponent } from '../../shared/filterable-combobox/filterable-combobox.component';
import { InstantDatePipe } from '../../shared/time/instant-date.pipe';

@Component({
  selector: 'app-profile-page',
  imports: [ReactiveFormsModule, FilterableComboboxComponent, InstantDatePipe],
  templateUrl: './profile.page.html',
  styleUrl: './profile.page.css',
})
export class ProfilePage {
  private readonly auth = inject(AuthService);
  private readonly overlay = inject(FormSubmitOverlayService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly formAlert = viewChild<ElementRef<HTMLElement>>('formAlert');

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly uploading = signal(false);
  protected readonly errorMessages = signal<string[]>([]);
  protected readonly successMessage = signal<string | null>(null);
  protected readonly serverFields = signal<ReadonlySet<string>>(new Set());
  protected readonly createdUtc = signal<string | null>(null);
  protected readonly updatedUtc = signal<string | null>(null);
  protected readonly username = signal<string | null>(null);
  protected readonly hasAvatar = signal(false);
  protected readonly avatarCacheBust = signal(Date.now());
  protected readonly countries = listCountries();
  protected readonly timeZones = listTimeZones();
  protected readonly suffixes = NAME_SUFFIXES;
  protected profileRevision = 0;
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
    currentPassword: [''],
    newPassword: ['', passwordComplexity],
    confirmPassword: [''],
  });
  protected readonly countryValue = toSignal(this.form.controls.country.valueChanges, {
    initialValue: this.form.controls.country.value,
  });
  protected readonly timeZoneValue = toSignal(this.form.controls.timeZoneId.valueChanges, {
    initialValue: this.form.controls.timeZoneId.value,
  });
  protected readonly regionOptions = computed(() => regionsForCountry(this.countryValue()));

  constructor() {
    void this.loadProfile();
  }

  protected isInvalid(name: string): boolean {
    return isControlInvalid(this.form, name, this.serverFields());
  }

  private async loadProfile(): Promise<void> {
    try {
      const profile = await this.auth.getOwnProfile();
      this.form.patchValue({
        username: profile.username,
        firstName: profile.firstName,
        middleInitial: profile.middleInitial ?? '',
        lastName: profile.lastName,
        suffix: profile.suffix ?? '',
        city: profile.city,
        region: profile.region ?? '',
        country: profile.country,
        timeZoneId: profile.timeZoneId ?? '',
        displayNameMode: profile.displayNameMode,
      });
      this.profileRevision = profile.profileRevision;
      this.createdUtc.set(profile.createdUtc);
      this.updatedUtc.set(profile.updatedUtc);
      this.username.set(profile.username);
      this.hasAvatar.set(profile.hasAvatar);
    } catch (error: unknown) {
      this.revealErrors(readApiErrorMessages(error, 'Unable to load your profile.'));
    } finally {
      this.loading.set(false);
    }
  }

  protected avatarSrc(): string | null {
    const username = this.username();
    if (!username || !this.hasAvatar()) {
      return null;
    }

    return `${this.auth.avatarUrl(username)}?v=${this.avatarCacheBust()}`;
  }

  protected async save(): Promise<void> {
    this.form.markAllAsTouched();
    this.serverFields.set(new Set());
    this.successMessage.set(null);

    const failures = collectFormFailures(this.form, PROFILE_FIELD_LABELS).filter(
      (message) =>
        !message.startsWith('Current password') &&
        !message.startsWith('New password') &&
        !message.startsWith('Confirm password'),
    );
    failures.push(...this.collectPasswordChangeFailures());
    if (failures.length > 0) {
      this.revealErrors(failures);
      return;
    }

    this.saving.set(true);
    this.errorMessages.set([]);
    try {
      await this.overlay.run(async () => {
        const value = this.form.getRawValue();
        const profile = await this.auth.updateProfile(
          {
            username: value.username,
            firstName: value.firstName,
            middleInitial: value.middleInitial,
            lastName: value.lastName,
            suffix: value.suffix,
            city: value.city,
            region: value.region,
            country: value.country,
            timeZoneId: value.timeZoneId,
            displayNameMode: value.displayNameMode,
          },
          this.profileRevision,
        );
        this.profileRevision = profile.profileRevision;
        this.updatedUtc.set(profile.updatedUtc);
        this.username.set(profile.username);

        if (this.isChangingPassword()) {
          await this.auth.changePassword(value.currentPassword, value.newPassword);
          this.form.patchValue({ currentPassword: '', newPassword: '', confirmPassword: '' });
          this.form.controls.currentPassword.markAsUntouched();
          this.form.controls.newPassword.markAsUntouched();
          this.form.controls.confirmPassword.markAsUntouched();
        }
      });
      this.revealSuccess();
    } catch (error: unknown) {
      this.serverFields.set(new Set(readApiFieldErrors(error)));
      this.revealErrors(readApiErrorMessages(error, 'Unable to save your profile.'));
    } finally {
      this.saving.set(false);
    }
  }

  protected async onAvatarSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      return;
    }

    this.uploading.set(true);
    this.errorMessages.set([]);
    this.successMessage.set(null);
    try {
      await this.overlay.run(async () => {
        const profile = await this.auth.uploadAvatar(file);
        this.profileRevision = profile.profileRevision;
        this.updatedUtc.set(profile.updatedUtc);
        this.hasAvatar.set(profile.hasAvatar);
        this.avatarCacheBust.set(Date.now());
      });
      this.revealSuccess();
    } catch (error: unknown) {
      this.revealErrors(readApiErrorMessages(error, 'Unable to upload that picture.'));
    } finally {
      this.uploading.set(false);
      input.value = '';
    }
  }

  private isChangingPassword(): boolean {
    const value = this.form.getRawValue();
    return value.currentPassword.length > 0 || value.newPassword.length > 0 || value.confirmPassword.length > 0;
  }

  private collectPasswordChangeFailures(): string[] {
    if (!this.isChangingPassword()) {
      return [];
    }

    const failures: string[] = [];
    const current = this.form.controls.currentPassword;
    const next = this.form.controls.newPassword;
    const confirm = this.form.controls.confirmPassword;

    if (!String(current.value).length) {
      current.setErrors({ required: true });
      failures.push('Current password is not filled in.');
    }

    if (!String(next.value).length) {
      next.setErrors({ required: true });
      failures.push('New password is not filled in.');
    } else {
      const complexity = describeControlError(next, 'New password');
      if (complexity) {
        failures.push(complexity);
      }
    }

    if (!String(confirm.value).length) {
      confirm.setErrors({ required: true });
      failures.push('Confirm password is not filled in.');
    } else if (next.value !== confirm.value) {
      confirm.setErrors({ mismatch: true });
      failures.push('Confirm password does not match the password.');
    }

    return failures;
  }

  private revealErrors(messages: readonly string[]): void {
    this.successMessage.set(null);
    this.errorMessages.set([...messages]);
    queueMicrotask(() => scrollAlertIntoView(this.formAlert()?.nativeElement));
  }

  private revealSuccess(): void {
    this.errorMessages.set([]);
    this.successMessage.set(FORM_SAVE_SUCCESS_MESSAGE);
    queueMicrotask(() => scrollAlertIntoView(this.formAlert()?.nativeElement));
  }
}
