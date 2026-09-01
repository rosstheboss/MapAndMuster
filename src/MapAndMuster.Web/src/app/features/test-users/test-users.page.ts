import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';

import { AuthService, readApiError } from '../../core/auth/auth.service';
import type { TestAccount } from '../../core/auth/auth.models';
import { FormSubmitOverlayService } from '../../core/forms/form-submit-overlay.service';

@Component({
  selector: 'app-test-users-page',
  imports: [RouterLink],
  templateUrl: './test-users.page.html',
  styleUrl: './test-users.page.css',
})
export class TestUsersPage {
  private readonly auth = inject(AuthService);
  private readonly overlay = inject(FormSubmitOverlayService);
  private readonly router = inject(Router);

  protected readonly users = signal<TestAccount[]>([]);
  protected readonly filter = signal('');
  protected readonly error = signal<string | null>(null);
  protected readonly loading = signal(true);
  protected readonly filteredUsers = computed(() => {
    const needle = this.filter().trim().toLowerCase();
    const users = this.users();
    if (!needle) {
      return users;
    }

    return users.filter(
      (user) => user.displayName.toLowerCase().includes(needle) || user.username.toLowerCase().includes(needle),
    );
  });

  constructor() {
    void this.load();
  }

  protected onFilter(event: Event): void {
    this.filter.set((event.target as HTMLInputElement).value);
  }

  protected isCurrentTestAccount(user: TestAccount): boolean {
    const current = this.auth.currentUser();
    return !!current?.isImpersonating && current.id === user.id;
  }

  protected async impersonate(user: TestAccount): Promise<void> {
    this.error.set(null);
    try {
      await this.overlay.run(() => this.auth.impersonateTestUser(user.id));
      await this.router.navigateByUrl('/');
    } catch (error: unknown) {
      this.error.set(readApiError(error, 'Unable to switch to that test user.'));
    }
  }

  private async load(): Promise<void> {
    try {
      this.users.set(await this.auth.listTestUsers());
    } catch (error: unknown) {
      this.error.set(readApiError(error, 'Unable to load test users.'));
    } finally {
      this.loading.set(false);
    }
  }
}
