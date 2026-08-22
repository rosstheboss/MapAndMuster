import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { AuthService, readApiError } from '../../core/auth/auth.service';

@Component({
  selector: 'app-confirm-email-page',
  imports: [RouterLink],
  templateUrl: './confirm-email.page.html',
  styleUrl: './confirm-email.page.css',
})
export class ConfirmEmailPage {
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);

  protected readonly status = signal<'working' | 'success' | 'error'>('working');
  protected readonly errorMessage = signal<string | null>(null);

  constructor() {
    void this.confirm();
  }

  private async confirm(): Promise<void> {
    const userId = this.route.snapshot.queryParamMap.get('userId');
    const token = this.route.snapshot.queryParamMap.get('token');
    if (!userId || !token) {
      this.status.set('error');
      this.errorMessage.set('This confirmation link is missing information.');
      return;
    }

    try {
      await this.auth.confirmEmail(userId, token);
      this.status.set('success');
    } catch (error: unknown) {
      this.status.set('error');
      this.errorMessage.set(readApiError(error, 'This confirmation link is invalid.'));
    }
  }
}
