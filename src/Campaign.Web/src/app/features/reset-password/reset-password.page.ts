import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { AuthService, readApiError } from '../../core/auth/auth.service';
import { minLength, required } from '../../core/forms/validators';

@Component({
  selector: 'app-reset-password-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './reset-password.page.html',
  styleUrl: './reset-password.page.css',
})
export class ResetPasswordPage {
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly form = this.formBuilder.nonNullable.group({
    password: ['', [required, minLength(10)]],
  });

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const userId = this.route.snapshot.queryParamMap.get('userId');
    const token = this.route.snapshot.queryParamMap.get('token');
    if (!userId || !token) {
      this.errorMessage.set('This reset link is missing information.');
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    try {
      await this.auth.resetPassword(userId, token, this.form.controls.password.value);
      await this.router.navigateByUrl('/login');
    } catch (error: unknown) {
      this.errorMessage.set(readApiError(error, 'Unable to reset that password.'));
    } finally {
      this.submitting.set(false);
    }
  }
}
