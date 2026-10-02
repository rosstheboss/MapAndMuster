import type { CampaignListItem } from '../../core/campaigns/campaign.models';

export interface CampaignListFilters {
  visibility: 'any' | 'public' | 'private';
  country: string;
  region: string;
  city: string;
  startsAfter: string;
  endsBefore: string;
  gameSystem: string;
  roundLength: string;
  roundCount: string;
  alliance: 'any' | 'free-for-all' | 'allied';
  participants: string[];
  managers: string[];
  title: string;
}

export const emptyCampaignListFilters = (): CampaignListFilters => ({
  visibility: 'any',
  country: '',
  region: '',
  city: '',
  startsAfter: '',
  endsBefore: '',
  gameSystem: '',
  roundLength: '',
  roundCount: '',
  alliance: 'any',
  participants: [],
  managers: [],
  title: '',
});

export function campaignMatchesFilters(campaign: CampaignListItem, filters: CampaignListFilters): boolean {
  if (filters.visibility === 'public' && campaign.isPrivate) {
    return false;
  }

  if (filters.visibility === 'private' && !campaign.isPrivate) {
    return false;
  }

  if (!matchesText(campaign.country, filters.country)) {
    return false;
  }

  if (!matchesText(campaign.region, filters.region)) {
    return false;
  }

  if (!matchesText(campaign.city, filters.city)) {
    return false;
  }

  if (filters.startsAfter && Date.parse(campaign.startsUtc) < Date.parse(filters.startsAfter)) {
    return false;
  }

  if (filters.endsBefore && Date.parse(campaign.endsUtc) > Date.parse(filters.endsBefore)) {
    return false;
  }

  if (filters.gameSystem && (campaign.gameSystem ?? '') !== filters.gameSystem) {
    return false;
  }

  if (filters.alliance === 'free-for-all' && !campaign.isFreeForAll) {
    return false;
  }

  if (filters.alliance === 'allied' && campaign.isFreeForAll) {
    return false;
  }

  if (filters.roundCount && String(campaign.roundCount ?? '') !== filters.roundCount) {
    return false;
  }

  if (filters.roundLength) {
    const label = `${campaign.roundLengthAmount ?? ''} ${campaign.roundLengthUnit ?? ''}`.trim();
    if (label !== filters.roundLength) {
      return false;
    }
  }

  if (filters.title && !campaign.name.toLowerCase().includes(filters.title.trim().toLowerCase())) {
    return false;
  }

  if (filters.managers.length > 0) {
    const manager = (campaign.managerUsername ?? '').toLowerCase();
    if (!filters.managers.some((name) => manager === name.toLowerCase())) {
      return false;
    }
  }

  if (filters.participants.length > 0) {
    const names = (campaign.publicParticipantUsernames ?? []).map((name) => name.toLowerCase());
    if (!filters.participants.some((name) => names.includes(name.toLowerCase()))) {
      return false;
    }
  }

  return true;
}

function matchesText(value: string | null, filter: string): boolean {
  const wanted = filter.trim();
  if (!wanted || wanted === 'any') {
    return true;
  }

  if (wanted === 'unspecified') {
    return !value;
  }

  return (value ?? '').toLowerCase() === wanted.toLowerCase();
}
