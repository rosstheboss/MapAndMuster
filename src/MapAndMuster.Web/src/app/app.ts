import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { AuthService, readApiError } from './core/auth/auth.service';
import { FormSubmitOverlayService } from './core/forms/form-submit-overlay.service';
import { AppDialogService } from './shared/dialog/dialog.service';
import { FormSubmitOverlayComponent } from './shared/form-submit-overlay/form-submit-overlay.component';
import { IconComponent } from './shared/icon/icon.component';
import { ThemeToggleComponent } from './shared/theme-toggle/theme-toggle.component';

@Component({
  selector: 'app-root',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    IconComponent,
    FormSubmitOverlayComponent,
    ThemeToggleComponent,
  ],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  private readonly router = inject(Router);
  protected readonly auth = inject(AuthService);
  protected readonly submitOverlay = inject(FormSubmitOverlayService);
  protected readonly dialogs = inject(AppDialogService);
  protected readonly loggingOut = signal(false);
  protected readonly returning = signal(false);
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

  protected async stopImpersonation(): Promise<void> {
    this.returning.set(true);
    this.navError.set(null);
    try {
      await this.auth.stopImpersonation();
      await this.router.navigateByUrl('/admin/test-users');
    } catch (error: unknown) {
      this.navError.set(readApiError(error, 'Unable to return to the administrator account.'));
    } finally {
      this.returning.set(false);
    }
  }
}
