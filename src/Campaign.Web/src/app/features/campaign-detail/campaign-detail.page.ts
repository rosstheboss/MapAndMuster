import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { AuthService, readApiError } from '../../core/auth/auth.service';
import { CampaignService } from '../../core/campaigns/campaign.service';
import type { CampaignDetail } from '../../core/campaigns/campaign.models';
import { InstantDatePipe } from '../../shared/time/instant-date.pipe';
import { actionNumberAt, formatDuration, formatPhaseLabel, statusLabel } from '../../core/campaigns/campaign-schedule';

@Component({
  selector: 'app-campaign-detail-page',
  imports: [RouterLink, InstantDatePipe],
  templateUrl: './campaign-detail.page.html',
  styleUrl: './campaign-detail.page.css',
})
export class CampaignDetailPage {
  private readonly campaignsApi = inject(CampaignService);
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly campaign = signal<CampaignDetail | null>(null);
  protected readonly confirmingDelete = signal(false);
  protected readonly deleting = signal(false);

  constructor() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      void this.load(id);
    } else {
      this.error.set('The campaign was not found.');
      this.loading.set(false);
    }
  }

  protected mapSrc(): string | null {
    const campaign = this.campaign();
    if (!campaign?.hasMap) {
      return null;
    }

    return this.campaignsApi.mapUrl(campaign.id, campaign.revision);
  }

  protected timeZoneId(): string | null {
    return this.auth.currentUser()?.timeZoneId ?? null;
  }

  protected roleLabel(campaign: CampaignDetail): string {
    if (campaign.canManage && campaign.isParticipant) {
      return 'Manager and player';
    }

    if (campaign.canManage) {
      return 'Manager';
    }

    return 'Player';
  }

  protected statusText(campaign: CampaignDetail): string {
    return statusLabel(campaign.status);
  }

  protected roundLengthText(campaign: CampaignDetail): string {
    return formatDuration(campaign.roundLengthAmount, campaign.roundLengthUnit);
  }

  protected phaseText(campaign: CampaignDetail, index: number): string {
    const phase = campaign.phases[index];
    if (!phase) {
      return '';
    }

    return `${formatPhaseLabel(phase.kind, actionNumberAt(campaign.phases, index))} · ${formatDuration(phase.durationAmount, phase.durationUnit)}`;
  }

  protected currentPhaseText(campaign: CampaignDetail): string {
    if (campaign.currentRound === null || campaign.currentPhaseNumber === null || !campaign.currentPhaseKind) {
      return '';
    }

    const index = campaign.currentPhaseNumber - 1;
    return `Round ${campaign.currentRound} · ${formatPhaseLabel(campaign.currentPhaseKind, actionNumberAt(campaign.phases, index))}`;
  }

  protected requestDelete(): void {
    this.confirmingDelete.set(true);
  }

  protected cancelDelete(): void {
    this.confirmingDelete.set(false);
  }

  protected async confirmDelete(): Promise<void> {
    const campaign = this.campaign();
    if (!campaign) {
      return;
    }

    this.deleting.set(true);
    this.error.set(null);
    try {
      await this.campaignsApi.delete(campaign.id);
      await this.router.navigateByUrl('/campaigns');
    } catch (error: unknown) {
      this.error.set(readApiError(error, 'Unable to delete this campaign.'));
      this.confirmingDelete.set(false);
      this.deleting.set(false);
    }
  }

  private async load(id: string): Promise<void> {
    try {
      this.campaign.set(await this.campaignsApi.get(id));
    } catch (error: unknown) {
      this.error.set(readApiError(error, 'Unable to load this campaign.'));
    } finally {
      this.loading.set(false);
    }
  }
}
