import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { AuthService, readApiError } from '../../core/auth/auth.service';
import { GUEST_PREVIEW_CAMPAIGN_ID } from '../../core/auth/guest-preview';
import { CampaignService } from '../../core/campaigns/campaign.service';
import type { CampaignListItem } from '../../core/campaigns/campaign.models';
import { CampaignListComponent } from '../../shared/campaign-list/campaign-list.component';
import { GuestPreviewBannerComponent } from '../../shared/guest-preview-banner/guest-preview-banner.component';

@Component({
  selector: 'app-campaigns-page',
  imports: [RouterLink, CampaignListComponent, GuestPreviewBannerComponent],
  templateUrl: './campaigns.page.html',
  styleUrl: './campaigns.page.css',
})
export class CampaignsPage {
  private readonly campaignsApi = inject(CampaignService);
  protected readonly auth = inject(AuthService);
  protected readonly previewCampaignId = GUEST_PREVIEW_CAMPAIGN_ID;
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly campaigns = signal<CampaignListItem[]>([]);

  constructor() {
    void this.load();
  }

  protected reload(): void {
    void this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      this.campaigns.set(await this.campaignsApi.list());
    } catch (error: unknown) {
      this.error.set(readApiError(error, 'Unable to load your campaigns.'));
    } finally {
      this.loading.set(false);
    }
  }
}
