import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';

import { AuthService, readApiError } from '../../core/auth/auth.service';

@Component({
  selector: 'app-home-page',
  imports: [RouterLink, DatePipe],
  templateUrl: './home.page.html',
  styleUrl: './home.page.css',
})
export class HomePage {
  private readonly router = inject(Router);
  protected readonly auth = inject(AuthService);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly loggingOut = signal(false);

  protected async logout(): Promise<void> {
    this.loggingOut.set(true);
    this.errorMessage.set(null);
    try {
      await this.auth.logout();
      await this.router.navigateByUrl('/login');
    } catch (error: unknown) {
      this.errorMessage.set(readApiError(error, 'Unable to sign out.'));
      this.loggingOut.set(false);
    }
  }
}
