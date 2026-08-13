import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import type { ExternalProvider } from '../../core/auth/auth.models';
import { AuthService, readApiError } from '../../core/auth/auth.service';
import { emailAddress, maxLength, minLength, required } from '../../core/forms/validators';

@Component({
  selector: 'app-register-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register.page.html',
  styleUrl: './register.page.css',
})
export class RegisterPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly providers = signal<ExternalProvider[]>([]);
  protected readonly form = this.formBuilder.nonNullable.group({
    email: ['', [required, emailAddress]],
    username: ['', [required, minLength(3), maxLength(32)]],
    password: ['', [required, minLength(10)]],
    firstName: ['', required],
    middleInitial: [''],
    lastName: ['', required],
    city: ['', required],
    region: [''],
    country: ['', required],
    displayNameMode: this.formBuilder.nonNullable.control<'Username' | 'FullName'>('Username'),
  });
  protected avatar: File | null = null;

  constructor() {
    void this.auth.getExternalProviders().then((providers) => this.providers.set(providers));
  }

  protected onAvatarSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.avatar = input.files?.[0] ?? null;
  }

  protected startExternal(provider: string): void {
    this.auth.startExternalLogin(provider);
  }

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    const value = this.form.getRawValue();
    try {
      await this.auth.register({
        ...value,
        avatar: this.avatar,
      });
      await this.router.navigateByUrl('/login', { state: { registered: true } });
    } catch (error: unknown) {
      this.errorMessage.set(readApiError(error, 'Unable to create the account.'));
    } finally {
      this.submitting.set(false);
    }
  }
}
