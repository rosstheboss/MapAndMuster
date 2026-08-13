import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { AuthService, readApiError } from './core/auth/auth.service';
import { IconComponent } from './shared/icon/icon.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, IconComponent],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  private readonly router = inject(Router);
  protected readonly auth = inject(AuthService);
  protected readonly loggingOut = signal(false);
  protected readonly navError = signal<string | null>(null);

  protected async logout(): Promise<void> {
    this.loggingOut.set(true);
    this.navError.set(null);
    try {
      await this.auth.logout();
      await this.router.navigateByUrl('/login');
    } catch (error: unknown) {
      this.navError.set(readApiError(error, 'Unable to sign out.'));
      this.loggingOut.set(false);
    }
  }
}
