import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { AuthService, readApiError } from '../../core/auth/auth.service';
import { maxLength, minLength, required } from '../../core/forms/validators';

@Component({
  selector: 'app-complete-external-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './complete-external.page.html',
  styleUrl: './complete-external.page.css',
})
export class CompleteExternalPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly loading = signal(true);
  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly provider = signal<string | null>(null);
  protected readonly form = this.formBuilder.nonNullable.group({
    username: ['', [required, minLength(3), maxLength(32)]],
    firstName: ['', required],
    middleInitial: [''],
    lastName: ['', required],
    city: ['', required],
    region: [''],
    country: ['', required],
    displayNameMode: this.formBuilder.nonNullable.control<'Username' | 'FullName'>('Username'),
  });

  constructor() {
    void this.loadPending();
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
      this.errorMessage.set(readApiError(error, 'Finish signing in with the external provider first.'));
    } finally {
      this.loading.set(false);
    }
  }

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    try {
      await this.auth.completeExternalRegistration(this.form.getRawValue());
      await this.router.navigateByUrl('/');
    } catch (error: unknown) {
      this.errorMessage.set(readApiError(error, 'Unable to finish creating the account.'));
    } finally {
      this.submitting.set(false);
    }
  }
}
