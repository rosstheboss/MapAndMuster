import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import type { ExternalProvider } from '../../core/auth/auth.models';
import { AuthService, readApiError } from '../../core/auth/auth.service';
import { FormSubmitOverlayService } from '../../core/forms/form-submit-overlay.service';
import { emailAddress, required } from '../../core/forms/validators';
import { ExternalLoginButtonsComponent } from '../../shared/external-login-buttons/external-login-buttons.component';

@Component({
  selector: 'app-login-page',
  imports: [ReactiveFormsModule, RouterLink, ExternalLoginButtonsComponent],
  templateUrl: './login.page.html',
  styleUrl: './login.page.css',
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly overlay = inject(FormSubmitOverlayService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly infoMessage = signal<string | null>(null);
  protected readonly providers = signal<ExternalProvider[]>([]);
  protected readonly form = this.formBuilder.nonNullable.group({
    email: ['', [required, emailAddress]],
    password: ['', required],
  });

  constructor() {
    const error = this.route.snapshot.queryParamMap.get('error');
    if (error === 'link-required') {
      this.errorMessage.set(
        'An account with that email already exists. Sign in, then link the provider from your profile.',
      );
    } else if (error === 'external') {
      this.errorMessage.set('External sign-in did not complete. Try again or use email.');
    }

    const navigationState = history.state as { registered?: unknown } | null;
    if (navigationState?.registered === true) {
      this.infoMessage.set('Check your email to confirm the account, then sign in.');
    }

    void this.auth.getExternalProviders().then((providers) => this.providers.set(providers));
  }

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    try {
      await this.overlay.run(async () => {
        await this.auth.login(this.form.controls.email.value, this.form.controls.password.value);
        await this.router.navigateByUrl('/');
      });
    } catch (error: unknown) {
      this.errorMessage.set(readApiError(error, 'Unable to sign in.'));
    } finally {
      this.submitting.set(false);
    }
  }

  protected startExternal(provider: string): void {
    this.auth.startExternalLogin(provider);
  }
}
