import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';

import { AuthService, readApiError } from '../../core/auth/auth.service';
import { maxLength, minLength, required } from '../../core/forms/validators';
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
  private readonly formBuilder = inject(FormBuilder);

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly uploading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly successMessage = signal<string | null>(null);
  protected readonly createdUtc = signal<string | null>(null);
  protected readonly updatedUtc = signal<string | null>(null);
  protected readonly username = signal<string | null>(null);
  protected readonly hasAvatar = signal(false);
  protected readonly avatarCacheBust = signal(Date.now());
  protected readonly countries = listCountries();
  protected readonly timeZones = listTimeZones();
  protected profileRevision = 0;
  protected readonly form = this.formBuilder.nonNullable.group({
    username: ['', [required, minLength(3), maxLength(32)]],
    firstName: ['', required],
    middleInitial: [''],
    lastName: ['', required],
    city: ['', required],
    region: [''],
    country: ['', required],
    timeZoneId: [''],
    displayNameMode: this.formBuilder.nonNullable.control<'Username' | 'FullName'>('Username'),
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

  private async loadProfile(): Promise<void> {
    try {
      const profile = await this.auth.getOwnProfile();
      this.form.patchValue({
        username: profile.username,
        firstName: profile.firstName,
        middleInitial: profile.middleInitial ?? '',
        lastName: profile.lastName,
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
      this.errorMessage.set(readApiError(error, 'Unable to load your profile.'));
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
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);
    try {
      const profile = await this.auth.updateProfile(this.form.getRawValue(), this.profileRevision);
      this.profileRevision = profile.profileRevision;
      this.updatedUtc.set(profile.updatedUtc);
      this.username.set(profile.username);
      this.successMessage.set('Profile saved.');
    } catch (error: unknown) {
      this.errorMessage.set(readApiError(error, 'Unable to save your profile.'));
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
    this.errorMessage.set(null);
    this.successMessage.set(null);
    try {
      const profile = await this.auth.uploadAvatar(file);
      this.profileRevision = profile.profileRevision;
      this.updatedUtc.set(profile.updatedUtc);
      this.hasAvatar.set(profile.hasAvatar);
      this.avatarCacheBust.set(Date.now());
      this.successMessage.set('Profile picture updated.');
    } catch (error: unknown) {
      this.errorMessage.set(readApiError(error, 'Unable to upload that picture.'));
    } finally {
      this.uploading.set(false);
      input.value = '';
    }
  }
}
