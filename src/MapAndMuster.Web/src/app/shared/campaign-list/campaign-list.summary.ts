import type { CampaignListItem } from '../../core/campaigns/campaign.models';
import { groupCampaigns } from './campaign-list.grouping';

export function campaignRoundPhaseText(campaign: CampaignListItem): string | null {
  if (campaign.status !== 'InProgress' || campaign.currentRound === null || !campaign.currentPhaseLabel) {
    return null;
  }

  return `Round ${campaign.currentRound} · ${campaign.currentPhaseLabel}`;
}

export function campaignPlayerCountText(campaign: CampaignListItem): string {
  return `${campaign.occupiedPlayerSlots} of ${campaign.playerSlotCount} players`;
}

export function campaignCommitLabel(campaign: CampaignListItem): 'Committed' | 'Not committed' | null {
  if (
    campaign.status !== 'InProgress' ||
    !campaign.isParticipant ||
    campaign.canChooseFaction ||
    campaign.currentPhaseKind !== 'Action'
  ) {
    return null;
  }

  return campaign.isCommitted ? 'Committed' : 'Not committed';
}

export function campaignRemainingSetupLabel(campaign: CampaignListItem): string | null {
  return campaign.canChooseFaction ? 'Choose your faction' : null;
}

export function campaignCanOpen(campaign: CampaignListItem): boolean {
  return campaign.canView || campaign.canPlay;
}

export function campaignAttentionItems(campaigns: readonly CampaignListItem[]): CampaignListItem[] {
  return groupCampaigns(campaigns).find((group) => group.id === 'active')?.campaigns ?? [];
}
