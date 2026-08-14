import { Component, inject, signal, viewChild, type ElementRef } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { AuthService, readApiErrorMessages, readApiFieldErrors } from '../../core/auth/auth.service';
import { FormSubmitOverlayService } from '../../core/forms/form-submit-overlay.service';
import {
  collectFormFailures,
  isControlInvalid,
  matchingPasswords,
  passwordComplexity,
  required,
  scrollAlertIntoView,
} from '../../core/forms/validators';

@Component({
  selector: 'app-reset-password-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './reset-password.page.html',
  styleUrl: './reset-password.page.css',
})
export class ResetPasswordPage {
  private readonly auth = inject(AuthService);
  private readonly overlay = inject(FormSubmitOverlayService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(FormBuilder);
  private readonly formAlert = viewChild<ElementRef<HTMLElement>>('formAlert');

  protected readonly submitting = signal(false);
  protected readonly errorMessages = signal<string[]>([]);
  protected readonly serverFields = signal<ReadonlySet<string>>(new Set());
  protected readonly form = this.formBuilder.nonNullable.group(
    {
      password: ['', [required, passwordComplexity]],
      confirmPassword: ['', required],
    },
    { validators: matchingPasswords },
  );

  protected isInvalid(name: string): boolean {
    return isControlInvalid(this.form, name, this.serverFields());
  }

  protected async submit(): Promise<void> {
    this.form.markAllAsTouched();
    this.serverFields.set(new Set());
    const failures = collectFormFailures(this.form, {
      password: 'Password',
      confirmPassword: 'Confirm password',
    });
    if (failures.length > 0) {
      this.revealErrors(failures);
      return;
    }

    const userId = this.route.snapshot.queryParamMap.get('userId');
    const token = this.route.snapshot.queryParamMap.get('token');
    if (!userId || !token) {
      this.revealErrors(['This reset link is missing information.']);
      return;
    }

    this.submitting.set(true);
    this.errorMessages.set([]);
    try {
      await this.overlay.run(async () => {
        await this.auth.resetPassword(userId, token, this.form.controls.password.value);
        await this.router.navigateByUrl('/login');
      });
    } catch (error: unknown) {
      this.serverFields.set(new Set(readApiFieldErrors(error)));
      this.revealErrors(readApiErrorMessages(error, 'Unable to reset that password.'));
    } finally {
      this.submitting.set(false);
    }
  }

  private revealErrors(messages: readonly string[]): void {
    this.errorMessages.set([...messages]);
    queueMicrotask(() => scrollAlertIntoView(this.formAlert()?.nativeElement));
  }
}
