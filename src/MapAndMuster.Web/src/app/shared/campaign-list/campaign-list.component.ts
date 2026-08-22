import { Component, computed, inject, input, output, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';

import { AuthService, readApiError } from '../../core/auth/auth.service';
import type { CampaignListItem } from '../../core/campaigns/campaign.models';
import { CampaignService } from '../../core/campaigns/campaign.service';
import { statusLabel } from '../../core/campaigns/campaign-schedule';
import { FormSubmitOverlayService } from '../../core/forms/form-submit-overlay.service';
import { formatLocation } from '../../core/location/location';
import { PhaseCountdownComponent } from '../phase-countdown/phase-countdown.component';
import { InstantDatePipe } from '../time/instant-date.pipe';
import { groupCampaigns } from './campaign-list.grouping';

@Component({
  selector: 'app-campaign-list',
  imports: [RouterLink, InstantDatePipe, PhaseCountdownComponent],
  templateUrl: './campaign-list.component.html',
  styleUrl: './campaign-list.component.css',
})
export class CampaignListComponent {
  private readonly campaignsApi = inject(CampaignService);
  private readonly overlay = inject(FormSubmitOverlayService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly campaigns = input.required<readonly CampaignListItem[]>();
  readonly allowDuplicate = input(false);
  readonly membershipChanged = output<void>();

  private readonly openCampaigns = signal<ReadonlySet<string>>(new Set());
  private readonly closedGroups = signal<ReadonlySet<string>>(new Set());
  protected readonly joiningCampaign = signal<CampaignListItem | null>(null);
  protected readonly joinPassword = signal('');
  protected readonly actionError = signal<string | null>(null);
  protected readonly timeZoneId = computed(() => this.auth.currentUser()?.timeZoneId ?? 'UTC');
  protected readonly groupedCampaigns = computed(() => groupCampaigns(this.campaigns()));

  protected isOpen(campaignId: string): boolean {
    return this.openCampaigns().has(campaignId);
  }

  protected isGroupOpen(groupId: string): boolean {
    return !this.closedGroups().has(groupId);
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

  protected toggleGroup(groupId: string): void {
    this.closedGroups.update((current) => {
      const next = new Set(current);
      if (next.has(groupId)) {
        next.delete(groupId);
      } else {
        next.add(groupId);
      }

      return next;
    });
  }

  protected statusText(status: string): string {
    return statusLabel(status);
  }

  protected locationText(campaign: CampaignListItem): string | null {
    return formatLocation(campaign.city, campaign.region, campaign.country);
  }

  protected roleLabel(campaign: CampaignListItem): string | null {
    if (campaign.canManage) {
      return 'Manager';
    }

    if (campaign.isParticipant) {
      return 'Player';
    }

    return null;
  }

  protected requestJoin(campaign: CampaignListItem): void {
    this.actionError.set(null);
    if (campaign.isPrivate) {
      this.joiningCampaign.set(campaign);
      this.joinPassword.set('');
      return;
    }

    void this.join(campaign, null);
  }

  protected cancelJoin(): void {
    this.joiningCampaign.set(null);
    this.joinPassword.set('');
  }

  protected onJoinPasswordInput(event: Event): void {
    const target = event.target;
    this.joinPassword.set(target instanceof HTMLInputElement ? target.value : '');
  }

  protected confirmJoin(): void {
    const campaign = this.joiningCampaign();
    if (!campaign) {
      return;
    }

    void this.join(campaign, this.joinPassword());
  }

  protected async leave(campaign: CampaignListItem): Promise<void> {
    this.actionError.set(null);
    try {
      await this.overlay.run(() => this.campaignsApi.leave(campaign.id));
      this.membershipChanged.emit();
    } catch (error: unknown) {
      this.actionError.set(readApiError(error, 'Unable to leave this campaign.'));
    }
  }

  protected async duplicate(campaign: CampaignListItem): Promise<void> {
    this.actionError.set(null);
    try {
      const created = await this.overlay.run(() => this.campaignsApi.duplicate(campaign.id));
      this.membershipChanged.emit();
      await this.router.navigate(['/campaigns', created.id, 'edit']);
    } catch (error: unknown) {
      this.actionError.set(readApiError(error, 'Unable to duplicate this campaign.'));
    }
  }

  private async join(campaign: CampaignListItem, joinPassword: string | null): Promise<void> {
    this.actionError.set(null);
    const password = joinPassword?.trim() ?? '';
    try {
      await this.overlay.run(() => this.campaignsApi.join(campaign.id, password.length === 0 ? null : password));
      this.joiningCampaign.set(null);
      this.joinPassword.set('');
      this.membershipChanged.emit();
    } catch (error: unknown) {
      this.actionError.set(readApiError(error, 'Unable to join this campaign.'));
    }
  }
}
