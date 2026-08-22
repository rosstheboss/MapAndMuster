import type { CampaignListItem } from '../../core/campaigns/campaign.models';

export interface CampaignListGroup {
  id: 'active' | 'upcoming' | 'completed';
  title: string;
  campaigns: CampaignListItem[];
}

export function groupCampaigns(items: readonly CampaignListItem[]): CampaignListGroup[] {
  return [
    {
      id: 'active',
      title: 'Active campaigns',
      campaigns: sortBySoonestEnd(items.filter((item) => item.status === 'InProgress')),
    },
    {
      id: 'upcoming',
      title: 'Upcoming campaigns',
      campaigns: sortBySoonestStart(items.filter((item) => item.status === 'Scheduled')),
    },
    {
      id: 'completed',
      title: 'Completed campaigns',
      campaigns: sortByLatestEnd(items.filter((item) => item.status === 'Completed')),
    },
  ];
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
