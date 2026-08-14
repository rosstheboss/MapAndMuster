import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { readApiError } from '../../core/auth/auth.service';
import { CampaignService } from '../../core/campaigns/campaign.service';
import type { CampaignListItem } from '../../core/campaigns/campaign.models';
import { InstantDatePipe } from '../../shared/time/instant-date.pipe';

@Component({
  selector: 'app-campaigns-page',
  imports: [RouterLink, InstantDatePipe],
  templateUrl: './campaigns.page.html',
  styleUrl: './campaigns.page.css',
})
export class CampaignsPage {
  private readonly campaignsApi = inject(CampaignService);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly campaigns = signal<CampaignListItem[]>([]);
  private readonly openCampaigns = signal<ReadonlySet<string>>(new Set());

  protected readonly groupedCampaigns = computed(() => {
    const items = this.campaigns();
    return [
      {
        id: 'active' as const,
        title: 'Active campaigns',
        campaigns: sortBySoonestEnd(items.filter((item) => item.status === 'InProgress')),
      },
      {
        id: 'upcoming' as const,
        title: 'Upcoming campaigns',
        campaigns: sortBySoonestStart(items.filter((item) => item.status === 'Scheduled')),
      },
      {
        id: 'completed' as const,
        title: 'Completed campaigns',
        campaigns: sortByLatestEnd(items.filter((item) => item.status === 'Completed')),
      },
    ];
  });

  constructor() {
    void this.load();
  }

  protected isOpen(campaignId: string): boolean {
    return this.openCampaigns().has(campaignId);
  }

  protected toggleCampaign(campaignId: string): void {
    this.openCampaigns.update((current) => {
      const next = new Set(current);
      if (next.has(campaignId)) {
        next.delete(campaignId);
      } else {
        next.add(campaignId);
      }

      return next;
    });
  }

  protected statusLabel(status: string): string {
    return status === 'InProgress' ? 'In progress' : status;
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

function sortBySoonestEnd(items: readonly CampaignListItem[]): CampaignListItem[] {
  return [...items].sort((left, right) => utcTime(left.endsUtc) - utcTime(right.endsUtc));
}

function sortBySoonestStart(items: readonly CampaignListItem[]): CampaignListItem[] {
  return [...items].sort((left, right) => utcTime(left.startsUtc) - utcTime(right.startsUtc));
}

function sortByLatestEnd(items: readonly CampaignListItem[]): CampaignListItem[] {
  return [...items].sort((left, right) => utcTime(right.endsUtc) - utcTime(left.endsUtc));
}

function utcTime(value: string): number {
  const time = Date.parse(value);
  return Number.isNaN(time) ? 0 : time;
}
