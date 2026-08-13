import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import type { PublicProfile } from '../../core/auth/auth.models';
import { AuthService, readApiError } from '../../core/auth/auth.service';

@Component({
  selector: 'app-public-profile-page',
  templateUrl: './public-profile.page.html',
  styleUrl: './public-profile.page.css',
})
export class PublicProfilePage {
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);

  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly profile = signal<PublicProfile | null>(null);

  constructor() {
    void this.loadProfile();
  }

  private async loadProfile(): Promise<void> {
    const username = this.route.snapshot.paramMap.get('username');
    if (!username) {
      this.errorMessage.set('That profile was not found.');
      this.loading.set(false);
      return;
    }

    try {
      this.profile.set(await this.auth.getPublicProfile(username));
    } catch (error: unknown) {
      this.errorMessage.set(readApiError(error, 'That profile was not found.'));
    } finally {
      this.loading.set(false);
    }
  }

  protected avatarSrc(username: string): string {
    return this.auth.avatarUrl(username);
  }
}
