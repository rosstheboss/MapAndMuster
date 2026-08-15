import { Component, inject, signal } from '@angular/core';

import { readApiError } from '../../core/auth/auth.service';
import { CampaignService } from '../../core/campaigns/campaign.service';
import type { CampaignListItem } from '../../core/campaigns/campaign.models';
import { CampaignListComponent } from '../../shared/campaign-list/campaign-list.component';

@Component({
  selector: 'app-all-campaigns-page',
  imports: [CampaignListComponent],
  templateUrl: './all-campaigns.page.html',
  styleUrl: './all-campaigns.page.css',
})
export class AllCampaignsPage {
  private readonly campaignsApi = inject(CampaignService);
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
      this.campaigns.set(await this.campaignsApi.listAll());
    } catch (error: unknown) {
      this.error.set(readApiError(error, 'Unable to load campaigns.'));
    } finally {
      this.loading.set(false);
    }
  }
}
