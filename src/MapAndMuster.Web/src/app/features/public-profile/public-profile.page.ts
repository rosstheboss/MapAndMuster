import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';

import type { PublicProfile } from '../../core/auth/auth.models';
import { AuthService, readApiError } from '../../core/auth/auth.service';
import { internalReturnLink } from '../../core/navigation/internal-path';
import { statusLabel } from '../../core/campaigns/campaign-schedule';

@Component({
  selector: 'app-public-profile-page',
  imports: [RouterLink],
  templateUrl: './public-profile.page.html',
  styleUrl: './public-profile.page.css',
})
export class PublicProfilePage {
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly queryParams = toSignal(this.route.queryParamMap, {
    initialValue: this.route.snapshot.queryParamMap,
  });

  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly profile = signal<PublicProfile | null>(null);
  protected readonly isOwnProfile = computed(() => {
    const viewer = this.auth.currentUser()?.username.trim().toLowerCase();
    const player = this.profile()?.username.trim().toLowerCase();
    return !!viewer && !!player && viewer === player;
  });
  protected readonly returnLink = computed(() => internalReturnLink(this.queryParams().get('from')));

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

  protected campaignStatus(status: string): string {
    return statusLabel(status);
  }
}
